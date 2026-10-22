using System.Collections;
using DreamRush.Dream;
using DreamRush.Enemies;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    public class FuncCommandSource : IPlayerCommandSource
    {
        readonly System.Func<PlayerCommand> _f;
        public FuncCommandSource(System.Func<PlayerCommand> f) { _f = f; }
        public PlayerCommand ReadCommand() => _f();
    }

    /// <summary>M4: the Dream_Chair graybox is navigable with the real motor (scripted "bot" input).</summary>
    public class DreamChairLevelTests
    {
        const float Dt = 1f / 60f;
        PlayerMotor3D _m;
        DreamLevelManager _level;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null;
            yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _level = DreamLevelManager.Current;
            Assert.IsNotNull(_m); Assert.IsNotNull(_level);
            _m.AutoStep = false;
            _m.CameraTransform = null; // world-relative input: +Z forward
            // let the gentle arrival finish
            for (int i = 0; i < 600 && !_m.IsGrounded; i++) { _m.Step(Dt, default); if (i % 4 == 0) yield return null; }
            Assert.IsTrue(_m.IsGrounded, "landed on the arrival carpet");
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        /// <summary>Hold forward toward x=targetX; jump at ledges (no ground ahead) and at knee-high obstacles.</summary>
        PlayerCommand BotCommand(float targetX, ref int jumpHold)
        {
            Vector3 p = _m.transform.position;
            Vector2 move = new Vector2(Mathf.Clamp(targetX - p.x, -1f, 1f) * 0.6f, 1f).normalized;
            bool press = false;
            if (_m.IsGrounded && jumpHold <= 0)
            {
                Vector3 ahead = p + Vector3.forward * 0.9f + Vector3.up * 1.0f;
                bool groundAhead = Physics.Raycast(ahead, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
                bool obstacle = Physics.Raycast(p + Vector3.up * 0.35f, Vector3.forward, 1.0f, ~0, QueryTriggerInteraction.Ignore);
                if (!groundAhead || obstacle) { press = true; jumpHold = 14; }
            }
            bool hold = jumpHold-- > 0;
            return new PlayerCommand { Move = move, JumpPressed = press, JumpHeld = hold };
        }

        [UnityTest] // LVL-01: arrival → tutorial (walk, ottoman, cushion gaps) → stool room without falling
        public IEnumerator Tutorial_IsTraversable_WithoutFalling()
        {
            int jumpHold = 0, steps = 0;
            while (_m.transform.position.z < 39f && steps < 60 * 40)
            {
                for (int k = 0; k < 4; k++, steps++) _m.Step(Dt, BotCommand(0f, ref jumpHold));
                yield return null;
            }
            Debug.Log($"[LVL-01] reached z={_m.transform.position.z:F1} in {steps * Dt:F1}s sim, falls={_level.FallCount}");
            Assert.That(_m.transform.position.z, Is.GreaterThanOrEqualTo(39f), "reached the stool room");
            Assert.That(_level.FallCount, Is.EqualTo(0), "no falls into the dream void");
            Assert.That(_level.CurrentSection.sectionId, Is.EqualTo("C"));
        }

        [UnityTest] // LVL-02: the little stool flees but is catchable by a walking player; catching unfolds the bridge
        public IEnumerator LittleStool_IsCatchable_AndOpensBridge()
        {
            var stool = Object.FindFirstObjectByType<LittleStoolController>();
            var bridge = Object.FindFirstObjectByType<PaperBridge>();
            Assert.IsNotNull(stool); Assert.IsNotNull(bridge);
            Assert.IsFalse(bridge.IsOpen);
            _m.Teleport(new Vector3(-3f, 0.1f, 39f), Quaternion.identity);
            bool fled = false;
            Vector3 stoolStart = stool.transform.position;
            float t = 0f;
            // real-time chase (motor auto-steps with Time.deltaTime, same clock as the stool)
            var chaser = new FuncCommandSource(() =>
            {
                Vector3 to = stool.transform.position - _m.transform.position; to.y = 0;
                return new PlayerCommand { Move = new Vector2(to.x, to.z).normalized };
            });
            _m.CommandSource = chaser;
            _m.AutoStep = true;
            while (stool.State != LittleStoolController.StoolState.Caught && t < 20f)
            {
                t += Time.deltaTime;
                if (stool.State == LittleStoolController.StoolState.Flee) fled = true;
                yield return null;
            }
            _m.AutoStep = false;
            Debug.Log($"[LVL-02] stool caught={stool.State == LittleStoolController.StoolState.Caught} after ~{t:F1}s (fled={fled}, moved {(stool.transform.position - stoolStart).magnitude:F1} m)");
            Assert.IsTrue(fled, "stool ran away when approached");
            Assert.That(stool.State, Is.EqualTo(LittleStoolController.StoolState.Caught), "catchable within 20 s by walking");

            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(bridge.IsOpen, "paper bridge unfolded");

            // cross the bridge into the card room
            _m.Teleport(new Vector3(0f, 0.1f, 49f), Quaternion.identity);
            int jumpHold = 0;
            for (int i = 0; i < 60 * 6 && _m.transform.position.z < 60f; i += 4)
            {
                for (int k = 0; k < 4; k++) _m.Step(Dt, new PlayerCommand { Move = Vector2.up });
                yield return null;
            }
            Assert.That(_m.transform.position.z, Is.GreaterThan(59f), "crossed the bridge");
            Assert.That(_level.FallCount, Is.EqualTo(0));
            Assert.That(_m.transform.position.y, Is.EqualTo(0f).Within(0.2f));
            _ = jumpHold;
        }

        [UnityTest] // LVL-03: falling into the dream void respawns at the last checkpoint and reports the fall
        public IEnumerator FallingIntoVoid_RespawnsAtCheckpoint()
        {
            bool fell = false;
            _level.Fell += () => fell = true;
            _m.Teleport(new Vector3(0f, 0.1f, 14f), Quaternion.identity); // reach checkpoint B
            yield return null; yield return null;
            Assert.That(_level.LastCheckpoint.order, Is.EqualTo(1));
            _m.Teleport(new Vector3(30f, -10f, 14f), Quaternion.identity);
            yield return null; yield return null;
            Assert.IsTrue(fell);
            Assert.That(_level.FallCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_m.transform.position, _level.LastCheckpoint.transform.position), Is.LessThan(0.5f));
        }

        [UnityTest] // LVL-04: arena exists with the GDD §14.2 features and a reachable balcony shortcut
        public IEnumerator Arena_HasRequiredFeatures()
        {
            foreach (var n in new[] { "A_LowPlatform", "B_Balcony", "D_Cushion1", "E_MovableBlock", "Obstacle_Sofa", "BOSS_Chair" })
                Assert.IsNotNull(GameObject.Find(n), n);
            // climb B steps → balcony with the bot (jump at each step)
            _m.Teleport(new Vector3(14.3f, 0.1f, 79f), Quaternion.identity);
            int jumpHold = 0;
            for (int i = 0; i < 60 * 8 && _m.transform.position.z < 88.5f; i += 4)
            {
                for (int k = 0; k < 4; k++) _m.Step(Dt, BotCommand(14.3f, ref jumpHold));
                yield return null;
                if (_m.transform.position.z > 87.6f) break;
            }
            for (int i = 0; i < 60; i++) _m.Step(Dt, new PlayerCommand { Move = Vector2.up * 0.3f });
            Assert.IsTrue(_m.IsGrounded);
            Assert.That(_m.transform.position.y, Is.GreaterThan(1.9f), "standing on the 2 m balcony");
        }
    }
}
