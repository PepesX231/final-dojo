using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamRush.Boss;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M5: chair AI against the real arena NavMesh, stepped deterministically (boss-encounter-design skill).</summary>
    public class BossChairPlayModeTests
    {
        const float Dt = 1f / 60f;
        PlayerMotor3D _m;
        BossChairController _chair;
        BossEncounter _enc;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null;
            _enc = Object.FindFirstObjectByType<BossEncounter>();
            _enc.skipReveal = true;
            yield return null; yield return null; yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _chair = Object.FindFirstObjectByType<BossChairController>();
            Assert.IsNotNull(_chair.Selector, "chair initialised (NavMesh built)");
            Assert.That(_chair.Selector.Candidates.Count, Is.GreaterThan(40), "arena candidates on NavMesh");
            _m.AutoStep = false;
            _m.CameraTransform = null;
            _chair.AutoStep = false;
            Object.Destroy(_enc); // isolate the AI: no victory hand-off / scene changes during stepped tests
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            Time.timeScale = 1f;
        }

        void Place(Vector3 player, Vector3 chair)
        {
            _m.Teleport(player, Quaternion.identity);
            for (int i = 0; i < 20; i++) _m.Step(Dt, default);
            _chair.ResetForSeed(1, chair);
        }

        bool InArena(Vector3 p) => _chair.arenaBounds.Contains(new Vector3(p.x, _chair.arenaBounds.center.y, p.z));

        [UnityTest] // CHAIR-01: notices and flees, staying in the arena
        public IEnumerator Chair_FleesFromApproachingPlayer()
        {
            Place(new Vector3(0, 0.1f, 74f), new Vector3(0, 0, 79f));
            float d0 = Vector3.Distance(_m.transform.position, _chair.transform.position);
            for (int i = 0; i < 90; i++) { _chair.Step(Dt); if (i % 10 == 0) yield return null; }
            float d1 = Vector3.Distance(_m.transform.position, _chair.transform.position);
            Debug.Log($"[CHAIR-01] distance {d0:F1} → {d1:F1}, state {_chair.State}");
            Assert.That(d1, Is.GreaterThan(d0 + 2f), "moved away from the player");
            Assert.IsTrue(InArena(_chair.transform.position));
        }

        [UnityTest] // CHAIR-02 + §15.9: seat validation, 2 s continuous, victory once
        public IEnumerator Sitting_RequiresOpening_AndTwoSeconds_VictoryOnce()
        {
            Place(new Vector3(0, 0.1f, 80f), new Vector3(0, 0, 81.2f));
            Assert.IsFalse(_chair.CanSit(_m, out var why), "fleeing chair can't be sat on");
            Assert.That(why, Is.EqualTo("chair won't hold still"));
            Assert.IsTrue(_chair.ApplyStatus(ChairState.Stumble, 3f, "Obstacle"));
            Assert.IsTrue(_chair.CanSit(_m, out _));
            int defeats = 0; string method = null;
            _chair.Defeated += mth => { defeats++; method = mth; };
            Assert.IsTrue(_chair.BeginSitting(_m));
            for (int i = 0; i < 114; i++) _chair.Step(Dt); // 1.9 s
            Assert.That(_chair.State, Is.EqualTo(ChairState.Sitting), "not yet");
            for (int i = 0; i < 10; i++) _chair.Step(Dt);  // > 2.0 s
            Assert.That(_chair.State, Is.EqualTo(ChairState.Defeated));
            for (int i = 0; i < 60; i++) _chair.Step(Dt);
            Assert.That(defeats, Is.EqualTo(1), "victory triggers only once");
            Assert.That(method, Is.EqualTo("Obstacle"));
            yield return null;
        }

        [UnityTest] // airborne / too far / out-of-reach seat checks
        public IEnumerator SeatValidation_RejectsBadContexts()
        {
            Place(new Vector3(0, 0.1f, 76f), new Vector3(0, 0, 82f));
            _chair.ApplyStatus(ChairState.Stumble, 3f, "Obstacle");
            Assert.IsFalse(_chair.CanSit(_m, out var r1)); Assert.That(r1, Is.EqualTo("too far"));
            _m.Teleport(new Vector3(0, 0.1f, 80.8f), Quaternion.identity);
            for (int i = 0; i < 10; i++) _m.Step(Dt, default);
            _m.Step(Dt, new PlayerCommand { JumpPressed = true, JumpHeld = true });
            _m.Step(Dt, new PlayerCommand { JumpHeld = true });
            Assert.IsFalse(_chair.CanSit(_m, out var r2)); Assert.That(r2, Is.EqualTo("player airborne"));
            yield return null;
        }

        [UnityTest] // §16.6 shortcut: land in front of the fleeing chair from above → startled stumble
        public IEnumerator ShortcutLanding_InFront_CausesStumble()
        {
            Place(new Vector3(0, 0.1f, 72f), new Vector3(0, 0, 80f));
            _chair.transform.rotation = Quaternion.LookRotation(Vector3.back);
            _m.Teleport(new Vector3(0, 3f, 77.6f), Quaternion.identity);
            // fall from 3 m (as if dropping off the balcony)
            for (int i = 0; i < 120 && !_m.IsGrounded; i++) { _m.Step(Dt, default); _chair.TrackPlayerAir(); }
            Assert.IsTrue(_m.IsGrounded);
            Assert.That(_chair.State, Is.EqualTo(ChairState.Stumble));
            Assert.That(_chair.OpeningCause, Is.EqualTo("Shortcut"));
            yield return null;
        }

        [UnityTest] // §15.8 anti-softlock
        public IEnumerator ChairOutsideArena_IsRepositioned_Visibly()
        {
            Place(new Vector3(0, 0.1f, 74f), new Vector3(0, 0, 80f));
            Vector3 from = Vector3.zero, to = Vector3.zero;
            _chair.Repositioned += (a, b) => { from = a; to = b; };
            _chair.transform.position = new Vector3(40f, 0f, 40f);
            _chair.Step(Dt);
            Assert.That(_chair.SoftlockRecoveries, Is.EqualTo(1));
            Assert.IsTrue(InArena(_chair.transform.position));
            Assert.That(Vector3.Distance(to, _m.transform.position), Is.GreaterThan(6f), "reappears away from the player, not in front");
            yield return null;
        }

        [UnityTest] // CHAIR-03: 50 seeded chases with a simple chasing bot — no softlocks, chair stays valid, baseline win is possible
        public IEnumerator BotChase_50Seeds_NoSoftlock_BaselineWinnable()
        {
            const int Seeds = 50; const float Limit = 180f;
            int wins = 0, invalidFrames = 0, softlocks = 0;
            var times = new List<float>(); var methods = new Dictionary<string, int>();
            var sb = new StringBuilder();
            for (int seed = 0; seed < Seeds; seed++)
            {
                var rnd = new System.Random(seed * 31 + 7);
                Vector3 chairStart = new Vector3(rnd.Next(-10, 11), 0, rnd.Next(76, 90));
                _m.Teleport(new Vector3(rnd.Next(-6, 7), 0.1f, 71.5f), Quaternion.identity);
                for (int i = 0; i < 10; i++) _m.Step(Dt, default);
                _chair.ResetForSeed(seed, chairStart);
                bool won = false; string method = null;
                System.Action<string> onWin = mth => { won = true; method = mth; };
                _chair.Defeated += onWin;
                float t = 0f; int openings = 0, jumpHold = 0;
                for (int step = 0; t < Limit && !won; step++, t += Dt)
                {
                    PlayerCommand cmd = default;
                    if (_chair.State != ChairState.Sitting)
                    {
                        Vector3 to = _chair.transform.position - _m.transform.position; to.y = 0;
                        cmd.Move = new Vector2(to.x, to.z).normalized;
                        cmd.Sprint = to.magnitude > 2.5f;
                        // hop onto cushions / platforms when something is in the way
                        Vector3 fwd = new Vector3(cmd.Move.x, 0, cmd.Move.y);
                        if (_m.IsGrounded && jumpHold <= 0 && fwd.sqrMagnitude > 0.1f &&
                            Physics.Raycast(_m.transform.position + Vector3.up * 0.35f, fwd, 0.9f, ~0, QueryTriggerInteraction.Ignore))
                        { cmd.JumpPressed = true; jumpHold = 14; }
                        cmd.JumpHeld = jumpHold-- > 0;
                        if (_chair.IsSitCompatible && _chair.CanSit(_m, out _)) _chair.BeginSitting(_m);
                        _m.Step(Dt, cmd);
                    }
                    _chair.Step(Dt);
                    Vector3 cp = _chair.transform.position;
                    if (!InArena(cp) || !NavMesh.SamplePosition(cp, out _, 0.8f, NavMesh.AllAreas)) invalidFrames++;
                    if (step % 1200 == 0) yield return null;
                }
                _chair.Defeated -= onWin;
                openings = _chair.OpeningsCount;
                softlocks += _chair.SoftlockRecoveries;
                if (won) { wins++; times.Add(t); methods[method] = methods.TryGetValue(method, out var n) ? n + 1 : 1; }
                sb.Append($"seed {seed}: {(won ? $"WIN {t:F0}s ({method})" : "timeout")} openings={openings} recov={_chair.SoftlockRecoveries}; ");
            }
            float avg = times.Count > 0 ? times.Average() : -1f;
            Debug.Log($"[CHAIR-03] wins {wins}/{Seeds}, avg {avg:F1}s, min {(times.Count > 0 ? times.Min() : -1):F0}s, max {(times.Count > 0 ? times.Max() : -1):F0}s, " +
                      $"methods {string.Join(",", methods.Select(kv => kv.Key + "=" + kv.Value))}, invalidFrames {invalidFrames}, softlockRecoveries {softlocks}\n{sb}");
            Assert.That(invalidFrames, Is.EqualTo(0), "chair always inside the arena and on the NavMesh");
            Assert.That(wins, Is.GreaterThanOrEqualTo(Seeds * 9 / 10), "baseline (no card) victory reachable by a simple chaser in ≥90% of seeds");
            times.Sort();
            float median = times[times.Count / 2];
            // difficulty envelope for a PERFECT chaser (humans are slower; GDD target 2–4 min first encounter)
            Assert.That(median, Is.InRange(20f, 150f), "bot median win time inside the tuning envelope");
        }
    }

    /// <summary>Reveal runs in real time (AutoStep), separate fixture so skipReveal stays false.</summary>
    public class BossRevealPlayModeTests
    {
        [UnityTest]
        public IEnumerator EnteringArena_PlaysRevealKick_ThenFightStarts()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null; yield return null; yield return null;
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            var chair = Object.FindFirstObjectByType<BossChairController>();
            Assert.That(enc.CurrentPhase, Is.EqualTo(BossEncounter.Phase.Waiting));
            m.Teleport(new Vector3(0, 0.1f, 73f), Quaternion.identity);
            float t = 0f; bool launched = false;
            while (enc.CurrentPhase != BossEncounter.Phase.Fight && t < 8f)
            {
                t += Time.deltaTime;
                if (!m.IsGrounded && m.VerticalVelocity > 2f && enc.CurrentPhase == BossEncounter.Phase.Reveal) launched = true;
                yield return null;
            }
            Assert.That(enc.CurrentPhase, Is.EqualTo(BossEncounter.Phase.Fight));
            Assert.IsTrue(launched, "the chair kicked the apple's pinky toe (knockback)");
            Assert.That(chair.State, Is.Not.EqualTo(ChairState.Dormant));
            Assert.That(DreamHUD.Current.ObjectiveText, Is.EqualTo("SIT ON THE CHAIR"));
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            GameFlowManager.Get().ResetForTests();
        }

        [UnityTest] // pitch: stumbling costs Sleep Stability; at zero mom wakes you
        public IEnumerator StabilityDepleted_EndsDreamAsDefeat()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null; yield return null; yield return null;
            var s = SleepStability.Current;
            Assert.IsNotNull(s);
            int max = s.Max;
            s.Shields = 0;
            for (int i = 0; i < max; i++) s.Damage("test", ignoreInvulnerability: true);
            Assert.IsTrue(s.Depleted);
            yield return null;
            Assert.That(GameFlowManager.Get().State, Is.EqualTo(FlowState.WakingUp));
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            GameFlowManager.Get().ResetForTests();
        }
    }
}
