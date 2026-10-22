using System.Collections;
using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Player;
using DreamRush.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>
    /// Simulation tests: the real motor + CharacterController against real colliders, stepped with a fixed dt
    /// so results are deterministic. IDs refer to docs/TEST_PLAN.md.
    /// </summary>
    public class PlayerMotorPlayModeTests
    {
        const float Dt = 1f / 60f;
        readonly List<GameObject> _spawned = new List<GameObject>();
        PlayerMotorConfig _config;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMotorConfig>();
            yield return TestScenes.Isolate();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned) if (go != null) Object.Destroy(go);
            _spawned.Clear();
            Object.Destroy(_config);
        }

        GameObject Box(Vector3 center, Vector3 size, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetPositionAndRotation(center, rot ?? Quaternion.identity);
            go.transform.localScale = size;
            _spawned.Add(go);
            return go;
        }

        PlayerMotor3D SpawnPlayer(Vector3 pos)
        {
            var go = PlayerRigBuilder.BuildPlayer(_config, withInput: false);
            _spawned.Add(go);
            var motor = go.GetComponent<PlayerMotor3D>();
            motor.AutoStep = false;
            motor.CameraTransform = null; // world-relative input: Move.y = +Z
            motor.Teleport(pos, Quaternion.identity);
            Physics.SyncTransforms();
            return motor;
        }

        static void Run(PlayerMotor3D m, int frames, PlayerCommand cmd)
        {
            for (int i = 0; i < frames; i++) m.Step(Dt, cmd);
        }

        static PlayerCommand Cmd(Vector2 move = default, bool press = false, bool hold = false, bool sprint = false) =>
            new PlayerCommand { Move = move, JumpPressed = press, JumpHeld = hold, Sprint = sprint };

        /// <summary>Jumps from rest and returns apex height above take-off.</summary>
        static float MeasureJump(PlayerMotor3D m, int holdFrames)
        {
            float y0 = m.transform.position.y, max = y0;
            for (int i = 0; i < 300; i++)
            {
                m.Step(Dt, Cmd(press: i == 0, hold: i < holdFrames));
                max = Mathf.Max(max, m.transform.position.y);
                if (i > 5 && m.IsGrounded) break;
            }
            return max - y0;
        }

        PlayerMotor3D Grounded(Vector3 pos)
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60));
            var m = SpawnPlayer(pos);
            Run(m, 40, Cmd());
            Assert.IsTrue(m.IsGrounded, "player should settle on the ground");
            return m;
        }

        [UnityTest] // MOV-06
        public IEnumerator FullJump_ReachesConfiguredHeight()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            float h = MeasureJump(m, 999);
            Debug.Log($"[MOV-06] full jump apex = {h:F3} (target {_config.jumpHeight})");
            Assert.That(h, Is.EqualTo(_config.jumpHeight).Within(0.1f));
            Assert.IsTrue(m.IsGrounded, "lands again");
        }

        [UnityTest] // MOV-07
        public IEnumerator ShortTap_IsClearlyLowerThanFullJump()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            float full = MeasureJump(m, 999);
            Run(m, 10, Cmd());
            float tap = MeasureJump(m, 3);
            Debug.Log($"[MOV-07] tap apex = {tap:F3}, full = {full:F3}, ratio {tap / full:F2}");
            Assert.That(tap, Is.LessThan(full * 0.7f));
            Assert.That(tap, Is.GreaterThan(0.5f), "a tap still gives a useful hop");
        }

        [UnityTest] // MOV-04
        public IEnumerator SecondPressInAir_DoesNotDoubleJump()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            float y0 = m.transform.position.y, max = y0;
            for (int i = 0; i < 240; i++)
            {
                bool press = i == 0 || i == 20 || i == 35; // re-press at/after apex
                m.Step(Dt, Cmd(press: press, hold: true));
                max = Mathf.Max(max, m.transform.position.y);
                if (i > 5 && m.IsGrounded) break;
            }
            Assert.That(max - y0, Is.LessThan(_config.jumpHeight + 0.1f));
        }

        [UnityTest] // MOV-02 (with real colliders)
        public IEnumerator CoyoteJump_WorksJustAfterWalkingOffLedge_AndNotLater()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60));
            Box(new Vector3(0, 1.5f, 0), new Vector3(4, 3, 4)); // top at y=3, edge at z=2
            var m = SpawnPlayer(new Vector3(0, 3.3f, 0));
            Run(m, 40, Cmd());
            Assert.IsTrue(m.IsGrounded);
            yield return null;

            // walk toward +Z until we leave the ledge
            int guard = 0;
            while (m.IsGrounded && guard++ < 200) m.Step(Dt, Cmd(Vector2.up));
            Assert.IsFalse(m.IsGrounded, "walked off the ledge");
            Run(m, 4, Cmd(Vector2.up)); // ~0.067 s after leaving
            m.Step(Dt, Cmd(Vector2.up, press: true, hold: true));
            Assert.That(m.VerticalVelocity, Is.GreaterThan(5f), "coyote jump should fire");

            // Late attempt
            Run(m, 200, Cmd());
            Assert.IsTrue(m.IsGrounded);
            m.Teleport(new Vector3(0, 3.3f, 0), Quaternion.identity);
            Run(m, 40, Cmd());
            guard = 0;
            while (m.IsGrounded && guard++ < 200) m.Step(Dt, Cmd(Vector2.up));
            Run(m, 12, Cmd(Vector2.up)); // 0.2 s
            m.Step(Dt, Cmd(Vector2.up, press: true, hold: true));
            Assert.That(m.VerticalVelocity, Is.LessThan(0f), "too late — no jump");
        }

        [UnityTest] // MOV-03 + super-jump regression (R-003)
        public IEnumerator BufferedJump_FiresOnLanding_WithNormalHeight()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            float groundY = m.transform.position.y;
            m.Teleport(new Vector3(0, groundY + 4f, 0), Quaternion.identity);
            yield return null;

            bool pressed = false, jumped = false;
            int landFrame = -1;
            m.Jumped += () => jumped = true;
            for (int i = 0; i < 200 && landFrame < 0; i++)
            {
                float above = m.transform.position.y - groundY;
                bool press = !pressed && m.VerticalVelocity < 0f && above < Mathf.Abs(m.VerticalVelocity) * 0.08f;
                if (press) pressed = true;
                m.Step(Dt, Cmd(press: press, hold: true));
                if (jumped) landFrame = i;
            }
            Assert.IsTrue(pressed, "test pressed jump before landing");
            Assert.IsTrue(jumped, "buffered jump fired on landing");

            float max = m.transform.position.y;
            for (int i = 0; i < 200; i++)
            {
                m.Step(Dt, Cmd(hold: true));
                max = Mathf.Max(max, m.transform.position.y);
                if (m.IsGrounded) break;
            }
            float h = max - groundY;
            Debug.Log($"[MOV-03] buffered jump apex = {h:F3}");
            Assert.That(h, Is.EqualTo(_config.jumpHeight).Within(0.15f), "no super jump");
        }

        [UnityTest] // MOV-08
        public IEnumerator Walk_ReachesSpeedQuickly_AndStopsQuickly()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            int frames = 0;
            while (m.PlanarVelocity.magnitude < _config.walkSpeed - 0.01f && frames < 120) { m.Step(Dt, Cmd(Vector2.up)); frames++; }
            float tAccel = frames * Dt;
            Run(m, 30, Cmd(Vector2.up));
            Assert.That(m.PlanarVelocity.magnitude, Is.EqualTo(_config.walkSpeed).Within(0.05f));

            frames = 0;
            while (m.PlanarVelocity.magnitude > 0.05f && frames < 120) { m.Step(Dt, Cmd()); frames++; }
            float tStop = frames * Dt;
            Debug.Log($"[MOV-08] time to walk speed {tAccel:F3}s, stop {tStop:F3}s");
            Assert.That(tAccel, Is.LessThan(0.3f));
            Assert.That(tStop, Is.LessThan(0.25f));
        }

        [UnityTest]
        public IEnumerator Sprint_IsFasterThanWalk()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            Run(m, 60, Cmd(Vector2.up, sprint: true));
            Assert.That(m.PlanarVelocity.magnitude, Is.EqualTo(_config.sprintSpeed).Within(0.05f));
        }

        [UnityTest] // MOV-09
        public IEnumerator PushingIntoWall_DoesNotStoreSpeed()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            Box(new Vector3(0, 2, 2f), new Vector3(10, 4, 1)); // wall face at z=1.5
            Physics.SyncTransforms();
            yield return null;
            Run(m, 90, Cmd(Vector2.up));
            Assert.That(m.transform.position.z, Is.LessThan(1.5f));
            Assert.That(m.PlanarVelocity.z, Is.LessThan(0.5f), "velocity into the wall must be cancelled");
            // release: should not shoot anywhere
            Run(m, 10, Cmd());
            Assert.That(m.PlanarVelocity.magnitude, Is.LessThan(0.3f));
        }

        [UnityTest] // MOV-10
        public IEnumerator WalkingDown35DegreeSlope_StaysGrounded()
        {
            Box(new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60));
            const float angle = 35f, length = 8f, t = 0.4f;
            float a = angle * Mathf.Deg2Rad;
            // Slope rises toward -Z (we walk down toward +Z)
            Box(new Vector3(0, length * 0.5f * Mathf.Sin(a) - t * 0.5f * Mathf.Cos(a), -(t * 0.5f * Mathf.Sin(a) + length * 0.5f * Mathf.Cos(a))),
                new Vector3(3, t, length), Quaternion.Euler(angle, 0, 0));
            float topY = length * Mathf.Sin(a), topZ = -length * Mathf.Cos(a);
            Box(new Vector3(0, topY * 0.5f, topZ - 1.5f), new Vector3(3, topY, 3)); // plateau
            var m = SpawnPlayer(new Vector3(0, topY + 0.3f, topZ - 1.5f));
            Run(m, 40, Cmd());
            Assert.IsTrue(m.IsGrounded);
            yield return null;

            int airborneFrames = 0, frames = 0;
            while (m.transform.position.z < 1f && frames++ < 400)
            {
                m.Step(Dt, Cmd(Vector2.up));
                if (!m.IsGrounded) airborneFrames++;
            }
            Debug.Log($"[MOV-10] airborne frames on 35° descent: {airborneFrames}/{frames}");
            Assert.That(airborneFrames, Is.LessThanOrEqualTo(3));
        }

        [UnityTest] // MOV-11
        public IEnumerator CeilingBonk_StartsFallingImmediately()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            float groundY = m.transform.position.y;
            Box(new Vector3(0, groundY + 1.8f + 0.25f, 0), new Vector3(6, 0.5f, 6)); // ceiling underside at +1.8
            Physics.SyncTransforms();
            yield return null;
            bool bonked = false;
            float max = groundY;
            for (int i = 0; i < 120; i++)
            {
                float vyBefore = m.VerticalVelocity;
                m.Step(Dt, Cmd(press: i == 0, hold: true));
                max = Mathf.Max(max, m.transform.position.y);
                if (i > 0 && vyBefore > 0f && m.VerticalVelocity <= 0f) { bonked = true; break; }
            }
            Assert.IsTrue(bonked);
            Assert.That(max - groundY, Is.LessThan(0.7f), "head stopped by ceiling");
        }

        [UnityTest] // MOV-14 (found in manual play: grounded flicker at editor frame rates)
        public IEnumerator StandingStill_StaysGrounded_AtHighAndLowFrameRates()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            yield return null;
            foreach (float fps in new[] { 30f, 60f, 144f, 500f, 1000f })
            {
                int air = 0, n = Mathf.RoundToInt(fps * 2f);
                for (int i = 0; i < n; i++)
                {
                    m.Step(1f / fps, Cmd());
                    if (!m.IsGrounded) air++;
                }
                Assert.That(air, Is.EqualTo(0), $"ungrounded frames at {fps} fps");
            }
            // walking at high frame rate too
            int airWalk = 0;
            for (int i = 0; i < 1000; i++) { m.Step(1f / 500f, Cmd(Vector2.up)); if (!m.IsGrounded) airWalk++; }
            Assert.That(airWalk, Is.EqualTo(0), "ungrounded frames while walking at 500 fps");
        }

        [UnityTest] // MOV-12
        public IEnumerator BlobShadow_StaysOnGroundWhileAirborne()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            var shadow = m.GetComponentInChildren<BlobShadow>();
            Assert.IsNotNull(shadow);
            yield return null;
            for (int i = 0; i < 20; i++) m.Step(Dt, Cmd(press: i == 0, hold: true));
            Assert.IsFalse(m.IsGrounded);
            shadow.UpdateShadow();
            Assert.IsTrue(shadow.HasGround);
            Assert.That(shadow.GroundPoint.y, Is.EqualTo(0f).Within(0.05f));
        }

        [UnityTest] // MOV-13
        public IEnumerator Camera_PullsInFrontOfWall()
        {
            var m = Grounded(new Vector3(0, 0.3f, 0));
            Box(new Vector3(0, 2, -2.5f), new Vector3(10, 4, 0.5f)); // wall behind the player
            Physics.SyncTransforms();
            var camGo = new GameObject("TestCam");
            _spawned.Add(camGo);
            camGo.AddComponent<Camera>();
            var cam = camGo.AddComponent<ThirdPersonCameraController>();
            cam.target = m.transform;
            cam.lockCursor = false;
            cam.Yaw = 0f;
            yield return null;
            yield return null;
            Assert.That(cam.CurrentDistance, Is.LessThan(cam.distance - 1f), "camera should pull in");
            Assert.That(camGo.transform.position.z, Is.GreaterThan(-2.25f), "camera stays in front of the wall face");
        }
    }
}
