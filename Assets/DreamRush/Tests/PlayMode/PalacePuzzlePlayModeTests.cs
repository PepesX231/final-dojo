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
    /// <summary>M17 level-2 content: neon circuit, monorail ride, freeze bells, rewind stairs, crate seal, sunset clock.</summary>
    public class PalacePuzzlePlayModeTests
    {
        PlayerMotor3D _m; Vector2 _move; int _jumpFrame = -1;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false; Time.timeScale = 1f;
            TickMinion.ResetCounters();
            SceneManager.LoadScene("Dream_MidnightPalace");
            yield return null; yield return null;
            if(Object.FindFirstObjectByType<DreamRush.Campaign.ExistingWorldArrival>()!=null&&!DreamRush.Campaign.DreamCinematic.SkipForGameplayProbe)yield return new WaitForSeconds(5.5f);
            _m = Object.FindFirstObjectByType<PlayerMotor3D>();
            _m.CameraTransform = null; _move = Vector2.zero;
            _m.CommandSource = new FuncCommandSource(() => new PlayerCommand { Move = _move, JumpPressed = Time.frameCount == _jumpFrame, JumpHeld = Time.frameCount - _jumpFrame < 10 });
            float t = 0; while (!_m.IsGrounded && t < 10f) { t += Time.deltaTime; yield return null; }
        }

        [TearDown]
        public void TearDown() { Time.timeScale = 1f; if (UIRoot.Exists) UIRoot.Get().CloseAll(); if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests(); }

        static IEnumerator Until(System.Func<bool> c, float timeout, string what)
        {
            float t = 0; while (!c()) { t += Time.deltaTime; if (t > timeout) Assert.Fail("timed out: " + what); yield return null; }
        }

        static CircuitPuzzle Circuit(string name) => Object.FindObjectsByType<CircuitPuzzle>(FindObjectsSortMode.None).First(c => c.name == "Circuit_" + name);

        [UnityTest] // PZ-01 the south blackout: starts unsolved; turning tiles to the solution powers the sign → the station lift appears
        public IEnumerator Circuit_SolvesAndUnlocksTheLift()
        {
            var pz = Circuit("South"); var reward = PalaceDirector.Current.blackout;
            Assert.IsFalse(pz.Solved, "starts dark"); Assert.IsFalse(reward.show[0].activeSelf, "lift hidden");
            // punch each tile until it reaches turn 0 (the built solution)
            foreach (var t in pz.tiles) { int guard = 0; while (t.turns != 0 && !pz.Solved && guard++ < 4) { t.TakeHit(new HitInfo { damage = 1, direction = Vector3.forward }); yield return new WaitForSeconds(0.22f); } }
            yield return Until(() => pz.Solved, 2f, "circuit solved");
            Assert.IsTrue(reward.Solved);
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(reward.show[0].activeSelf, "the lift to the station is there");
            Assert.That(pz.tiles.Count(t => t.Powered), Is.GreaterThanOrEqualTo(5), "the powered path lights up");
        }

        [UnityTest] // PZ-02 the monorail stops at a station and carries Pip on its roof
        public IEnumerator Train_StopsAndCarries()
        {
            var td = Object.FindFirstObjectByType<TrainDriver>();
            Assert.IsNotNull(td); Assert.That(td.cars.Count, Is.EqualTo(4)); Assert.That(td.stops.Count, Is.EqualTo(2));
            yield return Until(() => td.AtStation, 60f, "train reaches a station");
            var roof = td.cars[1].GetComponent<BoxCollider>().bounds;
            _m.Teleport(new Vector3(roof.center.x, roof.max.y + 0.3f, roof.center.z), Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            Vector3 start = _m.transform.position;
            yield return Until(() => !td.AtStation, 12f, "train departs");
            yield return new WaitForSeconds(2.5f);
            Assert.That(Vector3.Distance(_m.transform.position, start), Is.GreaterThan(12f), "Pip rode along");
            Assert.That(_m.transform.position.y, Is.GreaterThan(roof.max.y - 0.5f), "still on the roof");
        }

        [UnityTest] // PZ-03 the stop-the-clock bell freezes the clock-hand fans
        public IEnumerator Bell_FreezesFans()
        {
            var bell = Object.FindObjectsByType<FreezeBell>(FindObjectsSortMode.None).OrderBy(b => b.transform.position.x).First();
            var fan = SpinHazard.All.OrderBy(h => Vector3.Distance(h.transform.position, bell.transform.position)).First();
            Quaternion a = fan.arm.rotation; yield return new WaitForSeconds(0.3f);
            Assert.That(Quaternion.Angle(a, fan.arm.rotation), Is.GreaterThan(5f), "fan spins");
            bell.Ring(); yield return null;
            a = fan.arm.rotation; yield return new WaitForSeconds(1f);
            Assert.That(Quaternion.Angle(a, fan.arm.rotation), Is.LessThan(0.5f), "frozen");
            Assert.IsTrue(fan.Frozen);
        }

        [UnityTest] // PZ-04 the rewind lever pulls the gear-tower stairs into place for a while, then they crumble again
        public IEnumerator Rewind_BuildsThenCrumbles()
        {
            var br = Object.FindFirstObjectByType<RewindBridge>(); var lever = Object.FindFirstObjectByType<RewindLever>();
            var p0 = br.pieces[0];
            yield return null;
            float y0 = p0.position.y;
            lever.TakeHit(new HitInfo { damage = 1, direction = Vector3.forward });
            yield return new WaitForSeconds(1.2f);
            Assert.IsTrue(br.Built, "stairs rebuilt"); Assert.That(p0.position.y, Is.GreaterThan(y0 + 3f), "the first slab flew back up");
            yield return Until(() => !br.Built, br.hold + 3f, "the rewind runs out");
        }

        [UnityTest] // PZ-05 the fountain seal: the ambush cannot start until three crates sit on three plates
        public IEnumerator Crates_BreakTheSeal_ThenAmbushStarts()
        {
            var amb = Object.FindFirstObjectByType<AmbushChallenge>(); var group = amb.requires;
            Assert.IsNotNull(group);
            _m.Teleport(amb.transform.position + new Vector3(3, 0.3f, 0), Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(amb.Started, "sealed: no ambush yet");
            foreach (var plate in group.plates)
            {
                var crate = Object.FindObjectsByType<PushCrate>(FindObjectsSortMode.None).OrderBy(c => Vector3.Distance(c.transform.position, plate.transform.position)).First();
                for (int i = 0; i < 4 && !plate.Pressed; i++)
                {
                    Vector3 d = plate.transform.position - crate.transform.position; d.y = 0;
                    crate.TakeHit(new HitInfo { damage = 1, direction = d.normalized }); yield return new WaitForSeconds(0.5f);
                }
                Assert.IsTrue(plate.Pressed, "crate on plate " + plate.name);
            }
            yield return Until(() => group.Solved, 2f, "seal broken");
            yield return Until(() => amb.Started, 3f, "ambush starts once the seal is gone");
        }

        [UnityTest] // PZ-06 the sky starts at golden sunset and moves toward midnight as hours return
        public IEnumerator SkyClock_SunsetToMidnight()
        {
            var clock = Object.FindFirstObjectByType<SkyClock>(); var pd = PalaceDirector.Current;
            Assert.That(clock.T, Is.LessThan(0.05f), "starts at sunset");
            Color dusk = RenderSettings.fogColor;
            pd.OnHourCollected(pd.hours[0]);
            yield return new WaitForSeconds(3.5f);
            Assert.That(clock.T, Is.GreaterThan(0.1f), "time moves on with the hour");
            Assert.That(RenderSettings.fogColor.r, Is.LessThan(dusk.r), "the haze darkens");
        }
    }
}
