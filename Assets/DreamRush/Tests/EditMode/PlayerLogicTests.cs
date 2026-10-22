using DreamRush.Player;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    public class JumpMathTests
    {
        [Test] // MOV-01
        public void InitialVelocity_IsSqrtTwoGH()
        {
            Assert.That(JumpMath.InitialVelocity(2.1f, 24f), Is.EqualTo(Mathf.Sqrt(2f * 24f * 2.1f)).Within(1e-4f));
            Assert.That(JumpMath.InitialVelocity(0f, 24f), Is.EqualTo(0f));
        }

        [Test]
        public void IntegratedApex_MatchesHeight_AtSeveralFrameRates()
        {
            // Mirrors the motor's averaged-velocity integration while rising with jump held.
            foreach (float fps in new[] { 30f, 60f, 144f })
            {
                float dt = 1f / fps, g = 24f, h = 2.1f;
                float vy = JumpMath.InitialVelocity(h, g), y = 0f, max = 0f;
                for (int i = 0; i < 1000 && vy > 0f; i++)
                {
                    float start = vy;
                    vy -= g * dt;
                    y += 0.5f * (start + vy) * dt;
                    max = Mathf.Max(max, y);
                }
                Assert.That(max, Is.EqualTo(h).Within(0.02f), $"fps {fps}");
            }
        }
    }

    public class JumpAssistTests
    {
        const float Dt = 1f / 60f;

        [Test] // MOV-02
        public void Coyote_AllowsJumpShortlyAfterLeavingGround()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, true, false);
            for (int i = 0; i < 6; i++) a.Tick(Dt, false, false); // 0.1 s airborne
            a.Tick(Dt, false, true);                                 // 0.117 s
            Assert.IsTrue(a.TryConsumeJump());
        }

        [Test] // MOV-02
        public void Coyote_ExpiresAfterWindow()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, true, false);
            for (int i = 0; i < 9; i++) a.Tick(Dt, false, false); // 0.15 s
            a.Tick(Dt, false, true);
            Assert.IsFalse(a.TryConsumeJump());
        }

        [Test] // MOV-03
        public void Buffer_JumpPressedBeforeLanding_FiresOnLanding()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, false, true);                          // pressed in the air
            Assert.IsFalse(a.TryConsumeJump(), "no ground yet");
            for (int i = 0; i < 6; i++) a.Tick(Dt, false, false); // 0.1 s later
            a.Tick(Dt, true, false);                              // lands
            Assert.IsTrue(a.TryConsumeJump());
        }

        [Test] // MOV-03
        public void Buffer_ExpiresAfterWindow()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, false, true);
            for (int i = 0; i < 10; i++) a.Tick(Dt, false, false); // 0.183 s
            a.Tick(Dt, true, false);
            Assert.IsFalse(a.TryConsumeJump());
        }

        [Test] // MOV-04
        public void NoDoubleJump_InAir()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, true, true);
            Assert.IsTrue(a.TryConsumeJump());
            a.Tick(Dt, false, false);
            a.Tick(Dt, false, true);
            Assert.IsFalse(a.TryConsumeJump(), "second press in the air must not jump");
        }

        [Test]
        public void HoldingJumpWithoutNewPress_DoesNotRepeat()
        {
            var a = new JumpAssist(0.12f, 0.14f);
            a.Tick(Dt, true, true);
            Assert.IsTrue(a.TryConsumeJump());
            for (int i = 0; i < 60; i++) a.Tick(Dt, i > 30, false); // lands later, no new press
            Assert.IsFalse(a.TryConsumeJump());
        }
    }

    public class MovementMathTests
    {
        [Test] // MOV-05
        public void CameraRelative_ForwardFollowsCameraYaw()
        {
            for (int yaw = 0; yaw < 360; yaw += 15)
            {
                var rot = Quaternion.Euler(25f, yaw, 0f);
                Vector3 dir = MovementMath.CameraRelative(Vector2.up, rot * Vector3.forward, rot * Vector3.right, Vector3.up);
                Vector3 expected = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Assert.That(Vector3.Angle(dir, expected), Is.LessThan(0.5f), $"yaw {yaw}");
                Assert.That(dir.y, Is.EqualTo(0f).Within(1e-5f));
            }
        }

        [Test] // MOV-05
        public void CameraRelative_RightIsCameraRight()
        {
            var rot = Quaternion.Euler(30f, 70f, 0f);
            Vector3 dir = MovementMath.CameraRelative(Vector2.right, rot * Vector3.forward, rot * Vector3.right, Vector3.up);
            Assert.That(Vector3.Angle(dir, Quaternion.Euler(0, 70f, 0) * Vector3.right), Is.LessThan(0.5f));
        }

        [Test]
        public void CameraRelative_StraightDownCamera_StillValid()
        {
            var rot = Quaternion.Euler(90f, 0f, 0f);
            Vector3 dir = MovementMath.CameraRelative(Vector2.up, rot * Vector3.forward, rot * Vector3.right, Vector3.up);
            Assert.That(Vector3.Angle(dir, Vector3.forward), Is.LessThan(0.5f));
        }

        [Test]
        public void CameraRelative_PreservesAnalogMagnitude()
        {
            Vector3 dir = MovementMath.CameraRelative(new Vector2(0f, 0.4f), Vector3.forward, Vector3.right, Vector3.up);
            Assert.That(dir.magnitude, Is.EqualTo(0.4f).Within(1e-4f));
            Vector3 big = MovementMath.CameraRelative(new Vector2(1f, 1f), Vector3.forward, Vector3.right, Vector3.up);
            Assert.That(big.magnitude, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Acceleration_UsesTurnaroundWhenReversing()
        {
            var c = ScriptableObject.CreateInstance<PlayerMotorConfig>();
            float a = MovementMath.SelectAcceleration(c, true, Vector3.forward * 5f, Vector3.back * 5f);
            Assert.That(a, Is.EqualTo(c.turnaroundDeceleration));
            Assert.That(MovementMath.SelectAcceleration(c, true, Vector3.zero, Vector3.forward), Is.EqualTo(c.groundAcceleration));
            Assert.That(MovementMath.SelectAcceleration(c, true, Vector3.forward, Vector3.zero), Is.EqualTo(c.groundDeceleration));
            Assert.That(MovementMath.SelectAcceleration(c, false, Vector3.zero, Vector3.forward), Is.EqualTo(c.airAcceleration));
            Object.DestroyImmediate(c);
        }

        [Test]
        public void Gravity_FallAndJumpCutMultipliers()
        {
            var c = ScriptableObject.CreateInstance<PlayerMotorConfig>();
            Assert.That(MovementMath.SelectGravity(c, 5f, true, true), Is.EqualTo(c.gravity));
            Assert.That(MovementMath.SelectGravity(c, 5f, true, false), Is.EqualTo(c.gravity * c.jumpCutGravityMultiplier));
            Assert.That(MovementMath.SelectGravity(c, -1f, false, true), Is.EqualTo(c.gravity * c.fallGravityMultiplier));
            Object.DestroyImmediate(c);
        }

        [Test]
        public void Config_SprintFasterThanWalk_DefaultsMatchGdd()
        {
            var c = ScriptableObject.CreateInstance<PlayerMotorConfig>();
            Assert.That(c.sprintSpeed, Is.GreaterThan(c.walkSpeed));
            Assert.That(c.walkSpeed, Is.EqualTo(5.5f));
            Assert.That(c.jumpHeight, Is.EqualTo(2.1f));
            Assert.That(c.coyoteTime, Is.EqualTo(0.12f));
            Assert.That(c.jumpBuffer, Is.EqualTo(0.14f));
            Object.DestroyImmediate(c);
        }
    }
}
