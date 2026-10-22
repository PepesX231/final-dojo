using System.Collections;
using DreamRush.Combat;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>Count Midnight encounter regressions in the actual used Palace scene. Uses staged encounter positioning and dev states, not a full route clear.</summary>
    public class PalaceBossUsedScenePlayTests
    {
        PlayerMotor3D _m; PlayerCombat _pc; CountMidnight _boss; SleepStability _st;
        int _attackFrame = -1, _rollFrame = -1, _jumpFrame = -1; bool _attackHeld, _jumpHeld; Vector2 _move;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Dream_MidnightPalace");
            yield return null; yield return null;
            yield return new WaitForSeconds(5.5f);
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _pc = _m.GetComponent<PlayerCombat>();
            _boss = Object.FindFirstObjectByType<CountMidnight>();
            _st = Object.FindFirstObjectByType<SleepStability>();
            Assert.IsNotNull(_pc, "Pip has the combat kit"); Assert.IsNotNull(_boss, "boss"); Assert.IsNotNull(_st);
            _m.CameraTransform = null;
            _attackFrame = _rollFrame = _jumpFrame = -1; _attackHeld = _jumpHeld = false; _move = Vector2.zero;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand
            {
                Move = _move, JumpPressed = Time.frameCount == _jumpFrame, JumpHeld = _jumpHeld,
                AttackPressed = Time.frameCount == _attackFrame, AttackHeld = _attackHeld, RollPressed = Time.frameCount == _rollFrame,
            });
            float t = 0; while (!_m.IsGrounded && t < 5f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            BossBarUI.Hide();
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.unscaledDeltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        IEnumerator FaceBossAt(float dist)
        {
            Vector3 p = _boss.transform.position + _boss.transform.forward * dist; p.y = _boss.transform.position.y+0.2f;
            _m.Teleport(p, Quaternion.LookRotation(-_boss.transform.forward));
            yield return Until(() => _m.IsGrounded, 2f, "grounded");
        }

        IEnumerator BehindBossAt(float dist)
        {
            _boss.DevHold = true;
            Vector3 p = _boss.transform.position - _boss.transform.forward * dist; p.y = _boss.transform.position.y+0.2f;
            _m.Teleport(p, Quaternion.LookRotation(_boss.transform.forward));
            yield return Until(() => _m.IsGrounded, 2f, "grounded");
        }

        static void Punch(IHittable h) => h.TakeHit(new HitInfo { damage = 24, posture = 10, direction = Vector3.forward });

        [UnityTest] // CM-01 walking into the arena wakes him: intro card → fight, boss bar on screen, lock-on finds him
        public IEnumerator Boss_WakesOnApproach_BarAndLockOn()
        {
            _boss.AutoFight = true;
            Assert.That(_boss.Current, Is.EqualTo(CountMidnight.Mode.Waiting));
            yield return FaceBossAt(9f);
            yield return Until(() => _boss.Current == CountMidnight.Mode.Fight || _boss.Current == CountMidnight.Mode.Attacking, 6f, "fight");
            Assert.IsNotNull(BossBarUI.Current, "boss bar");
            _boss.AutoFight = false;
            _pc.ToggleLock();
            Assert.IsTrue(_pc.Locked, "lock-on");
            Assert.That(_pc.Target.gameObject, Is.SameAs(_boss.gameObject));
        }

        [UnityTest] // CM-02 (M19) wound up: front punches CLANG (no damage); punches on his back unwind the key and do land
        public IEnumerator WindingKey_FrontClangs_BackUnwinds()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevHold = true;
            yield return FaceBossAt(2.6f);
            float hp0 = _boss.HP;
            for (int i = 0; i < 3; i++) { _attackFrame = Time.frameCount + 1; yield return new WaitForSeconds(0.2f); }
            yield return new WaitForSeconds(0.5f);
            Assert.That(_boss.Clangs, Is.GreaterThanOrEqualTo(2), "front hits clang off the armour");
            Assert.That(_boss.HP, Is.EqualTo(hp0).Within(0.01f), "no damage from the front while wound");
            yield return BehindBossAt(2.4f);
            float w0 = _boss.Wind, hp1 = _boss.HP, st0 = _pc.Stamina;
            for (int i = 0; i < 3; i++) { _attackFrame = Time.frameCount + 1; yield return new WaitForSeconds(0.18f); }
            yield return new WaitForSeconds(0.5f);
            Debug.Log($"[CM-02] clangs={_boss.Clangs} wind {w0}->{_boss.Wind} hp {hp1}->{_boss.HP} stamina {st0}->{_pc.Stamina:F0}");
            Assert.That(_boss.Wind, Is.LessThan(w0 - 0.9f), "the key unwinds");
            Assert.That(_boss.HP, Is.LessThan(hp1 - 30f), "and the hits land");
        }

        [UnityTest] // CM-03 the low minute sweep hits a standing Pip but a jump clears it; a timed roll dodges a cleave
        public IEnumerator Sweep_JumpClears_RollDodges()
        {
            _boss.AutoFight = false; _boss.DevBeginFight();
            yield return FaceBossAt(4f);
            _st.Shields = 0;   // a bed-upgrade blanket would soak the first hit
            int v0 = _st.Value, h0 = _boss.PlayerHits;
            yield return _boss.DoAttack("Sweep");
            Assert.That(_boss.PlayerHits, Is.EqualTo(h0 + 1), "standing in the sweep gets hit");
            Assert.That(_st.Value, Is.EqualTo(v0 - 1), "and costs a moon");
            yield return new WaitForSeconds(1.4f);   // stability i-frames
            yield return FaceBossAt(4f);
            int v1 = _st.Value, h1 = _boss.PlayerHits;
            var co = _boss.DoAttack("Sweep");
            yield return new WaitForSeconds(0.42f);
            _jumpFrame = Time.frameCount + 1; _jumpHeld = true;
            yield return co;
            _jumpHeld = false;
            Assert.That(_boss.PlayerHits, Is.EqualTo(h1), "jumped over the sweep");
            yield return new WaitForSeconds(1.4f);
            yield return FaceBossAt(4.5f);
            int v2 = _st.Value, h2 = _boss.PlayerHits;
            co = _boss.DoAttack("CleaveFast");
            float t = 0; while (t < 0.78f) { t += Time.deltaTime; yield return null; }
            _move = new Vector2(1f, 0f); _rollFrame = Time.frameCount + 1;
            yield return co;
            _move = Vector2.zero;
            Debug.Log($"[CM-03] perfect dodges={_pc.PerfectDodges} moons {v2}->{_st.Value}");
            Assert.That(_boss.PlayerHits, Is.EqualTo(h2), "rolled through / out of the cleave");
        }

        [UnityTest] // CM-04 posture break → he kneels → a hit on the dial is a CRITICAL
        public IEnumerator PostureBreak_KneelAndCritical()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevSetWind(0f);   // armour off: this test is about posture / phases
            yield return FaceBossAt(2.6f);
            _boss.DevSetPosture(_boss.maxPosture - 1f);
            _attackFrame = Time.frameCount + 1;
            yield return Until(() => _boss.Current == CountMidnight.Mode.Staggered, 2f, "stagger");
            yield return new WaitForSeconds(0.7f);
            float hp = _boss.HP;
            _attackFrame = Time.frameCount + 1;
            yield return new WaitForSeconds(0.4f);
            Debug.Log($"[CM-04] critical: hp {hp:F0} -> {_boss.HP:F0}");
            Assert.That(_boss.HP, Is.LessThan(hp - _boss.criticalDamage + 1f), "critical damage");
        }

        [UnityTest] // CM-05 at 50 % he STOPS THE CLOCK: grey world, Undertale bullets (with gaps), then phase 2
        public IEnumerator HalfHP_StopTheClock_BulletsThenPhase2()
        {
            _st.AddMax(12); // Phase-transition probe health buffer; this is not a normal-health survival claim.
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevSetWind(0f);   // armour off: this test is about posture / phases
            yield return FaceBossAt(2.6f);
            _boss.DevSetHP(_boss.maxHP * 0.5f + 5f);
            _attackFrame = Time.frameCount + 1;
            yield return Until(() => _boss.Current == CountMidnight.Mode.TimeStop, 2f, "time stop");
            _m.Teleport(_boss.arenaCenter+new Vector3(0, 0.3f, -16f), Quaternion.identity);   // stand at the rim; the patterns must still be survivable-ish
            Time.timeScale = 3f;
            yield return Until(() => _boss.TimeStopWeight > 0.9f, 4f, "grey world");
            yield return Until(() => _boss.Phase == 2 && _boss.Current == CountMidnight.Mode.Fight, 30f, "phase 2");
            Time.timeScale = 1f;
            Debug.Log($"[CM-05] bullets={_boss.bullets.Spawned} hits on a standing Pip={_boss.bullets.PlayerHits} moons={_st.Value}");
            Assert.That(_boss.bullets.Spawned, Is.GreaterThan(150), "a real bullet hell");
            Assert.That(_boss.TimeStopWeight, Is.LessThan(0.05f), "colour back");
        }

        [UnityTest] // CM-06 0 HP → SNOOZE IT: landing on his dial ends the dream
        public IEnumerator ZeroHP_LandOnDial_Snooze()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevSetWind(0f);   // armour off: this test is about posture / phases
            yield return FaceBossAt(2.6f);
            bool won = false; _boss.Victory += () => won = true;
            _boss.DevSetHP(5f);
            _attackFrame = Time.frameCount + 1;
            yield return Until(() => _boss.Current == CountMidnight.Mode.Finisher, 2f, "finisher");
            yield return new WaitForSeconds(0.6f);
            _m.Teleport(_boss.head.position + Vector3.up * 1.6f, Quaternion.identity);   // drop onto the dial
            yield return Until(() => won, 5f, "snooze");
            Assert.That(_boss.Current, Is.EqualTo(CountMidnight.Mode.Defeated));
        }
    
        [UnityTest] // CM-07 (M19) unwind the key fully from behind → he RUNS DOWN (kneels) → dial critical → he winds back up
        public IEnumerator UnwindFully_RunsDown_ThenRewinds()
        {
            _boss.AutoFight = false; _boss.DevBeginFight();
            yield return BehindBossAt(2.4f);
            _boss.DevSetWind(0.4f);
            yield return new WaitForSeconds(0.2f);
            _attackFrame = Time.frameCount + 1;
            yield return new WaitForSeconds(0.4f);
            Debug.Log($"[CM-07] after punch: wind={_boss.Wind} unwinds={_boss.Unwinds} clangs={_boss.Clangs} hits={_pc.HitsLanded} state={_boss.Current} pip→boss={Vector3.Distance(_m.transform.position, _boss.transform.position):F2} dot={Vector3.Dot(_boss.transform.forward, (_m.transform.position - _boss.transform.position).normalized):F2}");
            yield return Until(() => _boss.Current == CountMidnight.Mode.Staggered, 2f, "run down → kneel");
            Assert.IsFalse(_boss.Wound);
            yield return new WaitForSeconds(0.7f);
            float hp = _boss.HP;
            _m.Teleport(_boss.head.position + Vector3.up * 1.6f, Quaternion.identity);   // drop on the dial
            yield return Until(() => _boss.HP < hp - _boss.criticalDamage + 1f, 3f, "critical on the dial");
            yield return Until(() => _boss.Wound && Mathf.Approximately(_boss.Wind, _boss.maxWind), 6f, "winds himself back up");
        }

        [UnityTest] // CM-08 (M19) ringing a bell with him inside its bubble freezes him mid-attack; front punches land while frozen
        public IEnumerator Bell_FreezesHim_ArmourOpen()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevHold = true;
            Assert.That(ArenaBell.All.Count, Is.EqualTo(4), "four bells on the dial");
            var bell = ArenaBell.All[0];
            Vector3 near = bell.transform.position + (_boss.arenaCenter - bell.transform.position).normalized * 3f; near.y = _boss.transform.position.y;
            _boss.transform.position = near;
            var co = _boss.DoAttack("CleaveDelayed");
            yield return new WaitForSeconds(0.3f);
            Punch(bell);
            Assert.That(_boss.Current, Is.EqualTo(CountMidnight.Mode.Frozen), "frozen mid-windup");
            Assert.IsFalse(bell.Ready, "bell recharging");
            yield return FaceBossAt(2.6f);
            float hp = _boss.HP, w = _boss.Wind;
            _attackFrame = Time.frameCount + 1; yield return new WaitForSeconds(0.4f);
            Debug.Log($"[CM-08] frozen hit: hp {hp:F0}->{_boss.HP:F0} wind {w}->{_boss.Wind}");
            Assert.That(_boss.HP, Is.LessThan(hp - 10f), "front hit lands while frozen");
            Assert.That(_boss.Wind, Is.LessThan(w), "and unwinds the key");
            yield return Until(() => _boss.Current == CountMidnight.Mode.Fight, 5f, "thaws");
        }

        [UnityTest] // CM-09 (M19) after a cleave the hour hand sticks in the dial — punching it unwinds him
        public IEnumerator StuckHourHand_PunchUnwinds()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevHold = true;
            yield return FaceBossAt(11f);
            _st.Shields = 3;
            _boss.DoAttack("CleaveFast");
            yield return Until(() => _boss.HandStuck, 3f, "hand stuck");
            var wp = GameObject.Find("StuckHourHand").GetComponent<BossWeakPoint>();
            float w = _boss.Wind;
            Punch(wp); yield return null; Punch(wp); yield return null; Punch(wp);
            yield return null;
            Debug.Log($"[CM-09] wind {w} -> {_boss.Wind}");
            Assert.That(_boss.Wind, Is.LessThan(w - 1f), "three punches on the stuck hand ≈ a full turn");
            yield return Until(() => !_boss.HandStuck, 2f, "he pulls it free");
        }

        [UnityTest] // CM-10 (M19) a golden numeral punched back flies into his face
        public IEnumerator GoldenNumeral_Reflects()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevHold = true;
            yield return FaceBossAt(9f);
            Vector3 dir = (_m.transform.position - _boss.transform.position); dir.y = 0; dir.Normalize();
            _boss.bullets.FireGolden(_boss.transform.position + dir * 2f, dir * 6.5f, _boss);
            yield return new WaitForSeconds(0.2f);
            var orb = GameObject.Find("Bullet_Golden").GetComponent<BossWeakPoint>();
            float hp = _boss.HP;
            Punch(orb);
            yield return Until(() => _boss.Reflected == 1, 3f, "reflected orb hits him");
            Assert.That(_boss.HP, Is.LessThan(hp - 20f));
        }

        [UnityTest] // CM-11 (M19) MIDNIGHT STRIKES: standing by a lit numeral is safe; anywhere else gets struck
        public IEnumerator MidnightStrikes_SafePads()
        {
            _boss.AutoFight = false; _boss.DevBeginFight(); _boss.DevHold = true;
            _st.Shields = 0;
            yield return FaceBossAt(8f);
            var co = _boss.DoAttack("Strikes");
            yield return Until(() => _boss.StrikePads.Count == 2, 2f, "two safe numerals light up");
            Vector3 pad = _boss.StrikePads[0].transform.position;
            _m.Teleport(pad + Vector3.up * 0.3f, Quaternion.identity);
            yield return co;
            Assert.That(_boss.StrikesSurvived, Is.EqualTo(1), "safe on the lit numeral");
            yield return new WaitForSeconds(1.5f);
            int h = _boss.PlayerHits;
            _m.Teleport(_boss.arenaCenter + new Vector3(0, 0.3f, -3f), Quaternion.identity);
            yield return _boss.DoAttack("Strikes");
            Assert.That(_boss.PlayerHits, Is.EqualTo(h + 1), "struck in the open");
        }

        [UnityTest] // CM-12 (M19) loiter behind him and he guards the key with a spinning backhand
        public IEnumerator Backhand_GuardsTheKey()
        {
            _boss.AutoFight = true; _boss.DevBeginFight();
            _st.Shields = 0;
            yield return BehindBossAt(3f);
            _boss.DevHold = false;
            int h = _boss.PlayerHits;
            yield return Until(() => _boss.LastAttack == "Backhand", 6f, "backhand");
            yield return Until(() => _boss.Current == CountMidnight.Mode.Fight, 3f, "recovers");
            Assert.That(_boss.PlayerHits, Is.GreaterThan(h), "the backhand reaches behind him");
        }
    }
}
