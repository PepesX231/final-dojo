using System.Collections;
using System.Collections.Generic;
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
    /// <summary>M13 level 2 — MIDNIGHT PALACE: content, the 10-second novelty rule, rails, ambush, hours → gate, climb.</summary>
    public class MidnightPalacePlayModeTests
    {
        PlayerMotor3D _m;
        Vector2 _move; bool _sprint; int _jumpFrame = -1; bool _jumpHeld;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false;
            Time.timeScale = 1f;
            TickMinion.ResetCounters();
            SceneManager.LoadScene("Dream_MidnightPalace");
            yield return null; yield return null;
            if(Object.FindFirstObjectByType<DreamRush.Campaign.ExistingWorldArrival>()!=null&&!DreamRush.Campaign.DreamCinematic.SkipForGameplayProbe)yield return new WaitForSeconds(5.5f);
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _m.CameraTransform = null;
            _move = Vector2.zero; _sprint = false; _jumpFrame = -1; _jumpHeld = false;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand { Move = _move, Sprint = _sprint, JumpPressed = Time.frameCount == _jumpFrame, JumpHeld = _jumpHeld });
            float t = 0; while (!_m.IsGrounded && t < 10f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        [UnityTest] // MP-01 the city has its pieces: 3 hours, a closed gate, rails, minions, wonders, the boss in the sky, Pip's phantom outfit
        public IEnumerator Content_IsAllThere()
        {
            var pd = PalaceDirector.Current;
            Assert.IsNotNull(pd); Assert.That(pd.hours.Count, Is.EqualTo(3));
            Assert.IsTrue(pd.towerGate.activeSelf, "gate closed at start");
            Assert.That(GrindRail.All.Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(Object.FindObjectsByType<TickMinion>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(10));
            Assert.That(WonderDirector.Current.events.Count, Is.GreaterThanOrEqualTo(10));
            Assert.That(pd.boss.transform.position.y, Is.GreaterThan(85f), "the dial floats above the tower");
            yield return new WaitForSeconds(0.3f);
            // M18: no phantom mask any more — Pip's face shows
            Assert.That(Shader.GetGlobalFloat("_DR_Ink"), Is.GreaterThan(0.5f), "Persona ink pass on");
        }

        [UnityTest] // MP-02 THE RULE: a bot running the main route meets something new at least every 10 s
        public IEnumerator Novelty_EveryTenSeconds_AlongTheRoute()
        {
            var wd = WonderDirector.Current;
            wd.ResetMeasure();
            var route = new List<Vector3> { new Vector3(0, 0, -70), new Vector3(80, 0, -70), new Vector3(80, 0, 30), new Vector3(30, 0, 30), new Vector3(-20, 0, 30), new Vector3(-70, 0, 30), new Vector3(-70, 0, -70), new Vector3(0, 0, -70) };  // streets only (roads at -120 + 50k)
            _sprint = true;
            float t = 0; int wp = 0;
            while (t < 60f && wp < route.Count)
            {
                Vector3 to = route[wp] - _m.transform.position; to.y = 0;
                if (to.magnitude < 4f) { wp++; continue; }
                _move = new Vector2(to.x, to.z).normalized;
                t += Time.deltaTime; yield return null;
            }
            _move = Vector2.zero;
            Debug.Log($"[MP-02] {t:F0}s, waypoints {wp}/{route.Count}, novelties {wd.Count}, max gap {wd.MaxGap:F1}s :: {wd.Log}");
            Assert.That(wd.MaxGap, Is.LessThanOrEqualTo(10f), "never more than 10 s without something new");
            Assert.That(wd.Count, Is.GreaterThanOrEqualTo((int)(t / 10f)));
            Assert.That(wp, Is.GreaterThanOrEqualTo(4), "the bot actually travelled the streets (not stuck on a wall)");
        }

        [UnityTest] // MP-03 landing on a rail grinds it at speed; jumping hops off
        public IEnumerator Rail_SnapsGrindsAndHopsOff()
        {
            var rail = GrindRail.All.First(r => r.name.StartsWith("Rail0"));
            var gc = _m.GetComponent<GrindController>();
            Vector3 p = rail.PointAt(1.5f, out var tan);
            _m.Teleport(p + Vector3.up * 1.2f, Quaternion.LookRotation(new Vector3(tan.x, 0, tan.z)));
            yield return Until(() => gc.Grinding, 2f, "snap onto the rail");
            Vector3 a = _m.transform.position;
            yield return new WaitForSeconds(0.4f);
            float speed = Vector3.Distance(a, _m.transform.position) / 0.4f;
            Debug.Log($"[MP-03] grind speed {speed:F1} m/s");
            Assert.That(speed, Is.GreaterThan(10f), "fast");
            _jumpFrame = Time.frameCount + 1; _jumpHeld = true;
            yield return Until(() => !gc.Grinding, 1f, "hop off");
            _jumpHeld = false;
            Assert.That(_m.VerticalVelocity, Is.GreaterThan(3f), "jumped off with lift");
        }

        [UnityTest] // MP-04 shadow ambush: mixed waves drop in (every wave a different shape), clearing them reveals the stolen hour
        public IEnumerator Ambush_WavesThenHour()
        {
            var amb = Object.FindFirstObjectByType<AmbushChallenge>();
            // M17: break the fountain seal first (crates on plates — PZ-05 tests the punching itself)
            if (amb.requires != null)
                foreach (var plate in amb.requires.plates)
                {
                    var crate = Object.FindObjectsByType<PushCrate>(FindObjectsSortMode.None).OrderBy(c => Vector3.Distance(c.transform.position, plate.transform.position)).First();
                    crate.transform.position = plate.transform.position + Vector3.up * 1.2f;
                }
            yield return new WaitForSeconds(0.3f);
            _m.Teleport(amb.transform.position + new Vector3(5, 0.3f, 0), Quaternion.identity);
            yield return Until(() => amb.Started, 2f, "ambush starts");
            Assert.That(amb.WaveCount, Is.GreaterThanOrEqualTo(3));
            _m.Teleport(amb.transform.position + new Vector3(60, 0.3f, 0), Quaternion.identity);   // watch from afar (keep the moons)
            var kinds = new HashSet<System.Type>();
            for (int w = 0; w < amb.WaveCount; w++)
            {
                yield return new WaitForSeconds(2.6f);
                var list = EnemyBase.All.Where(x => x != null && !x.Dead && Vector3.Distance(x.transform.position, amb.transform.position) < 16f).ToList();
                Assert.That(list.Count, Is.GreaterThanOrEqualTo(3), "wave " + (w + 1));
                foreach (var e in list) { kinds.Add(e.GetType()); for (int k = 0; k < 8 && !e.Dead; k++) e.TakeHit(new HitInfo { damage = 40, heavy = true, direction = Vector3.forward, point = e.transform.position }); }
                yield return new WaitForSeconds(0.4f);
            }
            yield return Until(() => amb.Cleared, 4f, "cleared");
            Assert.IsFalse(amb.reward.hiddenUntilUnlocked, "hour revealed");
            Debug.Log("[MP-04] enemy types met: " + string.Join(", ", kinds.Select(k => k.Name)));
            Assert.That(kinds.Count, Is.GreaterThanOrEqualTo(3), "the ambush mixes enemy types");
        }

        [UnityTest] // MP-05 3 hours open the gate; the gear spiral is climbable (every step reachable with a double jump)
        public IEnumerator Hours_OpenGate_ClimbIsReachable()
        {
            var pd = PalaceDirector.Current;
            foreach (var h in pd.hours) h.Collect();
            yield return null;
            Assert.That(pd.Hours, Is.EqualTo(3));
            Assert.IsTrue(pd.GateOpen); Assert.IsFalse(pd.towerGate.activeSelf);
            var gears = GameObject.Find("ClockTower").transform.Cast<Transform>().Where(t => t.name == "GEAR_Platform").Select(t => t.position).OrderBy(p => p.y).ToList();
            Assert.That(gears.Count, Is.GreaterThanOrEqualTo(14));
            for (int i = 1; i < gears.Count; i++)
            {
                float dy = gears[i].y - gears[i - 1].y; float dx = new Vector2(gears[i].x - gears[i - 1].x, gears[i].z - gears[i - 1].z).magnitude - 2 * 2.9f;
                Assert.That(dy, Is.LessThan(3.2f), "rise " + i); Assert.That(dx, Is.LessThan(4f), "gap " + i);
            }
            var sky = GrindRail.All.First(r => r.name == "SkyRail");
            Vector3 end = sky.points[sky.points.Count - 1];
            Assert.That(Mathf.Abs(end.y - pd.arenaCenter.position.y), Is.LessThan(4f), "the sky rail ends at the dial");
        }
    }
}
