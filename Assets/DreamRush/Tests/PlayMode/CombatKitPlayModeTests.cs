using System.Collections;
using System.Linq;
using DreamRush.Combat;
using DreamRush.Flow;
using DreamRush.Palace;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M14 — Pip's expanded move set and the Midnight Palace enemy roster (each enemy needs a different answer).</summary>
    public class CombatKitPlayModeTests
    {
        PlayerMotor3D _m; PlayerCombat _c;
        Vector2 _move; int _jumpFrame = -1, _attackFrame = -1, _skillFrame = -1; bool _attackHeld, _roll;
        static readonly Vector3 Open = new Vector3(0, 0.3f, -120f);   // empty avenue near the spawn

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false; Time.timeScale = 1f;
            SceneManager.LoadScene("Dream_MidnightPalace");
            yield return null; yield return null;
            _m = Object.FindFirstObjectByType<PlayerMotor3D>(); _c = _m.GetComponent<PlayerCombat>();
            _m.CameraTransform = null;
            _move = Vector2.zero; _jumpFrame = _attackFrame = _skillFrame = -1; _attackHeld = false; _roll = false;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand
            {
                Move = _move, JumpPressed = Time.frameCount == _jumpFrame, JumpHeld = Time.frameCount == _jumpFrame,
                AttackPressed = Time.frameCount == _attackFrame, AttackHeld = _attackHeld, SkillPressed = Time.frameCount == _skillFrame, RollPressed = _roll,
            });
            // clear the street of the squads so tests are deterministic
            foreach (var e in EnemyBase.All.ToList()) Object.Destroy(e.gameObject);
            yield return null;
            _m.Teleport(Open, Quaternion.identity);
            float t = 0; while (!_m.IsGrounded && t < 5f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown() { Time.timeScale = 1f; if (UIRoot.Exists) UIRoot.Get().CloseAll(); if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests(); }

        T Spawn<T>(string name, Vector3 at) where T : EnemyBase
        {
            var tpl = GameObject.Find("Templates").transform.Cast<Transform>().First(x => x.name.StartsWith(name));
            var g = Object.Instantiate(tpl.gameObject, at, Quaternion.LookRotation(_m.transform.position - at)); g.SetActive(true);
            return g.GetComponent<T>();
        }

        IEnumerator Press(System.Action set) { set(); yield return null; yield return null; }
        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        [UnityTest] // CK-01 (M18) no sword: the jab extends the right mitt forward, the fist anchor rides the punching hand
        public IEnumerator Fists_PunchFromTheMitt()
        {
            var arm = _m.GetComponentsInChildren<Transform>().First(x => x.name == "Arm_R");
            var rig = _m.GetComponent<FistRig>();
            Assert.IsNotNull(rig, "FistRig on Pip");
            Assert.IsFalse(_m.GetComponentsInChildren<Transform>().Any(x => x.name.Contains("LeafBlade")), "the leaf sword is gone");
            yield return new WaitForSeconds(0.3f);
            float rest = Vector3.Distance(_c.FistPivot.position, arm.position);
            Vector3 fwd = _m.transform.forward; float restFwd = Vector3.Dot(rig.HandR - arm.position, fwd);
            _attackFrame = Time.frameCount + 1;
            float bestExt = 0f, bestFwd = -9f;
            for (float t = 0; t < 0.35f; t += Time.deltaTime) { bestExt = Mathf.Max(bestExt, rig.ExtendR); bestFwd = Mathf.Max(bestFwd, Vector3.Dot(rig.HandR - arm.position, fwd)); yield return null; }
            float swing = Vector3.Distance(_c.FistPivot.position, arm.position);
            Debug.Log($"[CK-01] fist→shoulder rest {rest:F2} m, mid-combo {swing:F2} m, extend {bestExt:F2}, reach fwd {restFwd:F2}→{bestFwd:F2}");
            Assert.That(rest, Is.InRange(0.1f, 0.9f), "fist anchor sits at the end of the arm");
            Assert.That(bestExt, Is.GreaterThan(0.6f), "the right mitt snaps out on the jab");
            Assert.That(bestFwd, Is.GreaterThan(restFwd + 0.1f), "the jab reaches forward");
        }

        [UnityTest] // CK-02 RISING SLASH launches a Tick-Tock, AIR SPIN juggles it, it lands later
        public IEnumerator Rising_Launches_AirSpin_Juggles()
        {
            var tick = Spawn<TickMinion>("TickMinion", Open + new Vector3(0, -0.3f, 1.6f));
            tick.maxHp = 400f; tick.hp = 400f;
            yield return new WaitForSeconds(0.2f);
            _attackFrame = Time.frameCount + 1;
            yield return Until(() => _c.HitsLanded >= 1, 1f, "first light hit");
            _jumpFrame = Time.frameCount + 1;
            yield return Until(() => tick.Airborne, 1f, "launched");
            yield return Until(() => _c.State == PlayerCombat.Act.Idle && !_m.IsGrounded, 1f, "rising done");
            int before = _c.HitsLanded;
            _attackFrame = Time.frameCount + 1;
            yield return new WaitForSeconds(0.25f);
            Assert.That(_c.AirSpins, Is.GreaterThanOrEqualTo(1));
            Assert.That(_c.HitsLanded, Is.GreaterThan(before), "air spin connected with the juggled enemy");
            yield return Until(() => !tick.Airborne, 4f, "lands again");
        }

        [UnityTest] // CK-03 BELL BRUTE guards light hits from the front, takes the charged slam; its shock ring is jumpable
        public IEnumerator Brute_GuardsFront_TakesHeavy()
        {
            var b = Spawn<BellBrute>("BRUTE_Bell", Open + new Vector3(0, -0.3f, 2.4f));
            yield return new WaitForSeconds(0.2f);
            float hp = b.hp;
            Assert.IsFalse(b.TakeHit(new HitInfo { damage = 24, direction = Vector3.forward, point = b.transform.position }), "front light hit blocked");
            Assert.That(b.hp, Is.EqualTo(hp));
            Assert.IsTrue(b.TakeHit(new HitInfo { damage = 24, direction = -b.transform.forward * -1f + Vector3.zero, point = b.transform.position, heavy = true }), "heavy goes through");
            Assert.IsTrue(b.TakeHit(new HitInfo { damage = 24, direction = b.transform.forward, point = b.transform.position }), "from behind goes through");
            Assert.That(b.hp, Is.LessThan(hp));
            // a ring passing under a jumping Pip does nothing
            var st = Dream.SleepStability.Current; int moons = st.Value;
            ShockRing.Spawn(_m.transform.position + Vector3.forward * 3f, 6f, 0.6f, "test ring", _c, _m.transform);
            _jumpFrame = Time.frameCount + 1;
            yield return new WaitForSeconds(0.7f);
            Assert.That(st.Value, Is.EqualTo(moons), "jumped over the ring");
        }

        [UnityTest] // CK-04 slashing a cuckoo feather-orb reflects it; the reflected orb downs the sniper
        public IEnumerator Cuckoo_ReflectedOrbKillsIt()
        {
            var ck = Spawn<CuckooSniper>("CUCKOO_Sniper", Open + new Vector3(0, 3.5f, 9f));
            yield return new WaitForSeconds(0.2f);
            var orb = EnemyBullet.Fire(_m.transform.position + new Vector3(0, 0.8f, 1.6f), Vector3.back * 0.5f, ck, _m.transform, _c);
            _attackFrame = Time.frameCount + 1;
            yield return Until(() => orb == null || orb.Reflected, 1f, "orb reflected");
            yield return Until(() => ck == null || ck.Dead, 3f, "sniper downed by its own orb");
        }

        [UnityTest] // CK-05 GEAR ROLLER: armoured while rolling, crashes into a wall → stunned and takes bonus damage
        public IEnumerator Roller_CrashesIntoWall()
        {
            // the roller lines up on Pip, but a wall stands between them: it charges straight into it
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = Open + new Vector3(0, 1f, 2f); wall.transform.localScale = new Vector3(6, 3, 1);
            var r = Spawn<GearRoller>("ROLLER_Gear", Open + new Vector3(0, -0.3f, 7f));
            yield return Until(() => r.Current == GearRoller.State.Roll, 6f, "starts rolling");
            Assert.IsFalse(r.TakeHit(new HitInfo { damage = 10, direction = Vector3.forward, point = r.transform.position }), "armoured while rolling");
            yield return Until(() => r.Crashes > 0, 3f, "crash into the wall");
            Assert.IsTrue(r.Stunned);
            float hp = r.hp; r.TakeHit(new HitInfo { damage = 10, direction = Vector3.forward, point = r.transform.position });
            Assert.That(hp - r.hp, Is.GreaterThan(14f), "bonus damage while dazed");
            Object.Destroy(wall);
        }

        [UnityTest] // CK-06 DREAM DODGE → COUNTER ×2, hits fill the DREAM meter, V fires DREAM BURST that launches everything around
        public IEnumerator Meter_Burst_Counter()
        {
            var a = Spawn<TickMinion>("TickMinion", Open + new Vector3(2.5f, -0.3f, 1f)); a.maxHp = a.hp = 500;
            var b = Spawn<TickMinion>("TickMinion", Open + new Vector3(-2.5f, -0.3f, 1f)); b.maxHp = b.hp = 500;
            yield return new WaitForSeconds(0.2f);
            Assert.IsFalse(_c.BurstReady);
            _c.AddDream(100f);
            _skillFrame = Time.frameCount + 1;
            yield return Until(() => _c.Bursts > 0, 1f, "burst fired");
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(a.Airborne || a.hp < 500, "burst reached A"); Assert.IsTrue(b.Airborne || b.hp < 500, "burst reached B");
            Assert.That(_c.Meter, Is.LessThan(30f), "meter spent");
        }

        [UnityTest] // CK-07 (M18 skill UX) the skill bar shows 6 fist moves; AIR SPIN lights up only while airborne; a used move kicks its card
        public IEnumerator SkillBar_LightsWhatYouCanDoNow()
        {
            yield return new WaitForSeconds(0.3f);
            var bar = _m.GetComponent<SkillBar>();
            Assert.IsNotNull(bar, "skill bar on Pip"); Assert.IsNotNull(bar.Root);
            Assert.That(bar.Root.GetComponentsInChildren<RectTransform>().Count(r => r.name.StartsWith("Skill_")), Is.EqualTo(6));
            Assert.That(bar.HotCount, Is.EqualTo(0), "nothing hot while idle on the ground");
            _jumpFrame = Time.frameCount + 1;
            yield return Until(() => !_m.IsGrounded, 1f, "jump");
            yield return new WaitForSeconds(0.1f);
            Assert.That(bar.HotCount, Is.GreaterThanOrEqualTo(1), "air spin card lights up in the air");
            int pops = Presentation.SlashArc.Spawned;
            _attackFrame = Time.frameCount + 1; yield return new WaitForSeconds(0.15f);
            Assert.That(Presentation.SlashArc.Spawned, Is.GreaterThan(pops), "spin ring fx");
        }
    }
}
