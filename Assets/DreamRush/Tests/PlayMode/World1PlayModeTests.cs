using System.Collections;
using System.Linq;
using DreamRush.Boss;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>World 1 (Dream_World1): chapter flow and the new mechanics, run in the real scene.</summary>
    public class World1PlayModeTests
    {
        bool _previousCinematic;
        const string Scene = "Dream_World1";
        PlayerMotor3D _m;
        ChapterDirector _dir;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousCinematic = Campaign.DreamCinematic.SkipForGameplayProbe; Campaign.DreamCinematic.SkipForGameplayProbe = true;
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(Scene);
            yield return null; yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _dir = ChapterDirector.Current;
            Assert.IsNotNull(_m, "player"); Assert.IsNotNull(_dir, "chapter director");
            // the dream arrival floats Pip down onto the first page (normal gravity resumes on landing)
            float t = 0f;
            while (!_m.IsGrounded && t < 8f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(_m.IsGrounded, "landed in chapter 1");
        }

        [TearDown]
        public void TearDown()
        {
            Campaign.DreamCinematic.SkipForGameplayProbe = _previousCinematic;
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            Time.timeScale = 1f;
        }

        static IEnumerator Until(System.Func<bool> cond, float timeout, string what)
        {
            float t = 0f;
            while (!cond()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        [UnityTest] // W1-01
        public IEnumerator Scene_HasFiveChapters_StarsCheckpointsAndBoss()
        {
            Assert.That(_dir.chapters.Count, Is.EqualTo(5));
            Assert.That(_dir.chapters.All(c => c.spawn != null), "every chapter has a spawn");
            Assert.That(Object.FindObjectsByType<DreamStar>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(Object.FindObjectsByType<PagePortal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(4));
            Assert.IsNotNull(Object.FindFirstObjectByType<ChairSkills>(), "boss has World 1 skills");
            Assert.That(_dir.Index, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest] // W1-02 pop-up stairs rise when Pip walks up, and lead onto the plateau
        public IEnumerator PopUpStairs_RiseOnApproach_AndAreClimbable()
        {
            var stairs = Object.FindObjectsByType<PopUpPage>(FindObjectsSortMode.None).Where(p => p.name.StartsWith("B_PopStair")).OrderBy(p => p.name).ToArray();
            Assert.That(stairs.Length, Is.EqualTo(4));
            Assert.IsFalse(stairs[0].IsUp, "down before approach");
            _m.Teleport(new Vector3(0, 0.3f, 24.5f), Quaternion.identity);
            yield return Until(() => stairs[0].IsUp && stairs[1].IsUp, 3f, "first stairs up");
            // bot climbs: forward + jump at each riser
            _m.CameraTransform = null;
            _m.CommandSource = new FuncCommandSource(() =>
            {
                Vector3 p = _m.transform.position;
                bool riser = Physics.Raycast(p + Vector3.up * 0.3f, Vector3.forward, 1.1f, ~0, QueryTriggerInteraction.Ignore);
                return new PlayerCommand { Move = new Vector2(Mathf.Clamp(-p.x, -1, 1) * 0.3f, 1f), JumpPressed = riser && _m.IsGrounded, JumpHeld = true };
            });
            yield return Until(() => _m.transform.position.z > 39f && _m.transform.position.y > 3.0f && _m.IsGrounded, 12f, "reach the plateau");
            Debug.Log($"[W1-02] plateau reached at {_m.transform.position}");
            _m.CommandSource = null;
        }

        [UnityTest] // W1-03 launch pads land you on the target island
        public IEnumerator LaunchPad_ArcLandsOnTarget()
        {
            _dir.DevJump(2);
            yield return null;
            var pad = Object.FindObjectsByType<LaunchPad>(FindObjectsSortMode.None).First(p => p.name == "P0");
            _m.Teleport(pad.transform.position + Vector3.up * 0.6f, Quaternion.identity);
            yield return Until(() => pad.Launches > 0, 3f, "launch");
            yield return new WaitForSeconds(0.2f);
            yield return Until(() => _m.IsGrounded, 5f, "landing");
            Vector3 d = _m.transform.position - pad.target.position; d.y = 0;
            Debug.Log($"[W1-03] landed {d.magnitude:F2} m from target, y={_m.transform.position.y:F2}");
            Assert.That(d.magnitude, Is.LessThan(2.0f));
            Assert.That(_m.transform.position.y, Is.GreaterThan(pad.target.position.y - 0.6f));
        }

        [UnityTest] // W1-04 a page-portal takes you to the next chapter with its look
        public IEnumerator PagePortal_MovesToNextChapter()
        {
            var portal = Object.FindObjectsByType<PagePortal>(FindObjectsSortMode.None).First(p => p.toChapter == 1);
            _m.Teleport(portal.transform.position + Vector3.up * 0.3f, Quaternion.identity);
            yield return Until(() => _dir.Index == 1, 6f, "chapter 2");
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(_m.transform.position, _dir.chapters[1].spawn.position), Is.LessThan(3f));
            Assert.That(_dir.chapters[1].sideRail, "chapter 2 uses the side rail camera");
            Assert.That(_dir.chapters[1].lookRoot.activeSelf && !_dir.chapters[0].lookRoot.activeSelf, "only the active chapter's look is on");
        }

        [UnityTest] // W1-05 the chase fails (and resets) if the chair gets too far ahead
        public IEnumerator Chase_GotAway_ResetsWhenPlayerIdles()
        {
            _dir.DevJump(3);
            var runner = Object.FindFirstObjectByType<ChaseRunner>();
            runner.failSeconds = 1.5f; runner.maxLead = 12f;
            _m.Teleport(runner.transform.position + new Vector3(0, 0.3f, -5f), Quaternion.identity);
            yield return Until(() => runner.Running, 3f, "chase starts");
            yield return Until(() => runner.Fails > 0, 12f, "got away");
            yield return Until(() => !TransitionOverlay.Get().IsCovered && runner.transform.position.z < 1215f, 6f, "reset");
            Assert.That(Vector3.Distance(_m.transform.position, runner.playerRestart.position), Is.LessThan(2.5f));
        }

        [UnityTest] // W1-06 boss skills hit a careless player (stomp ring, cushion volley) — telegraphed, fair
        public IEnumerator BossSkills_StompAndVolley_HitStandingPlayer()
        {
            _dir.DevJump(4);
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            var skills = Object.FindFirstObjectByType<ChairSkills>();
            skills.AutoRun = false;
            var chair = enc.chair;
            _m.Teleport(new Vector3(0, 0.3f, enc.revealTriggerZ + 2f), Quaternion.identity);
            yield return Until(() => enc.CurrentPhase == BossEncounter.Phase.Fight, 10f, "fight");
            // stomp: stand 4 m away on the ground
            chair.DevHold = true;
            _m.Teleport(chair.transform.position + new Vector3(4f, 0.3f, 0f), Quaternion.identity);
            yield return Until(() => _m.IsGrounded, 2f, "grounded");
            int before = skills.PlayerHits;
            yield return skills.StartCoroutine(skills.Stomp());
            Assert.That(skills.PlayerHits, Is.GreaterThan(before), "the shockwave hits a grounded player");
            // volley: stand still → at least one cushion lands on you
            yield return new WaitForSeconds(1.5f);
            _m.Teleport(chair.transform.position + new Vector3(-6f, 0.3f, 0f), Quaternion.identity);
            yield return Until(() => _m.IsGrounded, 2f, "grounded 2");
            before = skills.PlayerHits;
            yield return skills.StartCoroutine(skills.Volley(3));
            Assert.That(skills.PlayerHits, Is.GreaterThan(before), "a cushion lands on a stationary player");
            Debug.Log($"[W1-06] skills used={skills.SkillsUsed} hits={skills.PlayerHits}");
        }

        IEnumerator EnterBossFight(BossEncounter enc)
        {
            _dir.DevJump(4);
            Object.FindFirstObjectByType<ChairSkills>().AutoRun = false;
            _m.Teleport(new Vector3(0, 0.3f, enc.revealTriggerZ + 2f), Quaternion.identity);
            yield return Until(() => enc.CurrentPhase == BossEncounter.Phase.Fight, 10f, "fight");
        }

        [UnityTest] // W1-07 P3 Throne Climb: leap to the top, books slide out as stairs, every step is jumpable from the one below
        public IEnumerator ThroneClimb_LeapStairsAndEveryStepReachable()
        {
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            yield return EnterBossFight(enc);
            var climb = Object.FindFirstObjectByType<ThroneClimb>();
            Assert.IsNotNull(climb, "Ch5 has the Throne Climb");
            climb.AutoThrow = false;
            Assert.That(climb.steps.Count, Is.EqualTo(8));
            Assert.That(climb.steps.All(st => !st.GetComponentInChildren<Collider>().enabled), "stairs tucked away before P3");
            yield return climb.StartCoroutine(climb.Run());
            var chair = enc.chair;
            Assert.That(chair.State, Is.EqualTo(ChairState.Perched));
            Assert.That(chair.transform.position.y, Is.EqualTo(climb.transform.position.y + climb.topHeight).Within(0.05f), "chair on the tower top");
            Assert.IsFalse(climb.throneDecor.activeSelf, "old throne kicked away");
            Assert.That(climb.steps.All(st => st.GetComponentInChildren<Collider>().enabled), "stairs out");
            // nothing makes it leave the top on its own (softlock recovery would teleport it to the floor)
            yield return new WaitForSeconds(1.5f);
            Assert.That(chair.State, Is.EqualTo(ChairState.Perched));
            Assert.That(chair.transform.position.y, Is.GreaterThan(climb.transform.position.y + climb.topHeight - 0.1f));

            // physical climb: from each step, jump toward the next (which keeps rotating) and land on it
            _m.CameraTransform = null;
            int jumpFrame = -1; bool held = false; Transform target = null;
            _m.CommandSource = new FuncCommandSource(() =>
            {
                Vector2 mv = Vector2.zero;
                if (target != null)
                {
                    Vector3 d = target.position - _m.transform.position; d.y = 0;
                    if (d.magnitude > 0.35f) mv = new Vector2(d.x, d.z).normalized;
                }
                return new PlayerCommand { Move = mv, JumpPressed = Time.frameCount == jumpFrame, JumpHeld = held };
            });
            for (int i = 0; i < climb.steps.Count; i++)
            {
                if (i == 0) { _m.Teleport(climb.steps[0].position + Vector3.up * 0.4f, Quaternion.identity); target = null; yield return Until(() => _m.IsGrounded, 2f, "on step 0"); continue; }
                target = climb.steps[i];
                float topY = target.GetComponentInChildren<Collider>().bounds.max.y;
                yield return null; yield return null;
                jumpFrame = Time.frameCount + 1; held = true;
                yield return new WaitForSeconds(0.15f);
                yield return Until(() => _m.IsGrounded || _m.transform.position.y < topY - 2.5f, 3f, "landing on step " + i);
                held = false;
                Vector3 off = _m.transform.position - target.position; off.y = 0;
                Debug.Log($"[W1-07] step {i}: y={_m.transform.position.y:F2} (top {topY:F2}) off={off.magnitude:F2}");
                Assert.That(_m.transform.position.y, Is.EqualTo(topY).Within(0.3f), "landed on step " + i);
                yield return new WaitForSeconds(0.3f);   // ride the step a moment
            }
            // last hop: onto the tower top → the chair is spent
            target = chair.transform;
            jumpFrame = Time.frameCount + 1; held = true;
            yield return new WaitForSeconds(0.15f);
            yield return Until(() => climb.PlayerAtTop, 3f, "reach the top");
            held = false; target = null;
            yield return null;
            Assert.That(chair.State, Is.EqualTo(ChairState.Exhausted), "the chair is spent once you reach the top");
        }

        [UnityTest] // W1-08 Throne pillow hits a careless climber; reaching the top + sitting 2 s wins the boss
        public IEnumerator ThroneClimb_PillowHitsAndSitAtTopWins()
        {
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            yield return EnterBossFight(enc);
            var climb = Object.FindFirstObjectByType<ThroneClimb>();
            var skills = Object.FindFirstObjectByType<ChairSkills>();
            climb.AutoThrow = false;
            yield return climb.StartCoroutine(climb.Run());
            _m.Teleport(climb.steps[3].position + Vector3.up * 0.4f, Quaternion.identity);
            yield return Until(() => _m.IsGrounded, 2f, "on step 3");
            int before = skills.PlayerHits;
            yield return climb.StartCoroutine(climb.ThrowPillow());
            Assert.That(skills.PlayerHits, Is.GreaterThan(before), "a pillow lands on a climber who stands still");
            Assert.That(climb.Throws, Is.EqualTo(1));
            // top + sit
            yield return new WaitForSeconds(1.5f);
            var chair = enc.chair;
            _m.Teleport(climb.transform.position + Vector3.up * (climb.topHeight + 0.3f) + chair.transform.forward * 1.2f, Quaternion.identity);
            yield return Until(() => _m.IsGrounded, 2f, "on top");
            yield return Until(() => chair.State == ChairState.Exhausted, 2f, "chair spent");
            Assert.IsTrue(enc.TrySit(_m), "can sit on the chair at the top");
            yield return Until(() => enc.CurrentPhase == BossEncounter.Phase.Victory, 4f, "victory");
            Assert.That(enc.VictoryMethod, Is.EqualTo("ThroneClimb"));
        }
    }
}
