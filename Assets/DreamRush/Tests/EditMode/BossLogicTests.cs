using System.Collections.Generic;
using DreamRush.Boss;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    public class ChairFatigueTests
    {
        [Test]
        public void RunningAccumulates_UntilExhausted_ThenRecoverTo()
        {
            var f = new ChairFatigue(18f);
            for (int i = 0; i < 17 * 60; i++) f.Run(1f / 60f, 1f);
            Assert.IsFalse(f.IsExhausted);
            for (int i = 0; i < 70; i++) f.Run(1f / 60f, 1f);
            Assert.IsTrue(f.IsExhausted);
            f.RecoverTo(0.25f);
            Assert.That(f.Value, Is.EqualTo(4.5f).Within(1e-3f));
            f.Rest(10f, 1.5f);
            Assert.That(f.Value, Is.EqualTo(0f));
        }

        [Test]
        public void PanicTiresTwiceAsFast()
        {
            var a = new ChairFatigue(18f); var b = new ChairFatigue(18f);
            a.Run(5f, 1f); b.Run(5f, 2f);
            Assert.That(b.Value, Is.EqualTo(a.Value * 2f).Within(1e-4f));
        }

        [Test] // pitch: player stumbling gives the chair its breath back
        public void Refresh_ReducesFatigue()
        {
            var f = new ChairFatigue(18f);
            f.Run(10f, 1f);
            f.Refresh(0.3f);
            Assert.That(f.Value, Is.EqualTo(10f - 5.4f).Within(1e-4f));
        }
    }

    public class EscapeSelectorTests
    {
        ChairAIConfig _cfg;
        List<EscapeDestinationSelector.Candidate> _grid;
        readonly HashSet<Vector3> _blocked = new HashSet<Vector3>();

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<ChairAIConfig>();
            _grid = new List<EscapeDestinationSelector.Candidate>();
            for (int x = -12; x <= 12; x += 2)
            for (int z = -12; z <= 12; z += 2)
                _grid.Add(new EscapeDestinationSelector.Candidate { Position = new Vector3(x, 0, z), Openness = 3f });
            _blocked.Clear();
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_cfg);

        bool StraightPath(Vector3 a, Vector3 b, List<Vector3> corners, out float length)
        {
            length = Vector3.Distance(a, b);
            if (_blocked.Contains(b)) return false;
            corners.Add(a); corners.Add(b);
            return true;
        }

        [Test] // CHAIR-05a: never picks invalid / unreachable / too-close destinations
        public void ChosenDestination_RespectsAllRejectionRules()
        {
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 1);
            Vector3 chair = Vector3.zero, player = new Vector3(0, 0, -3);
            _blocked.Add(new Vector3(0, 0, 8)); // the "best" straight-away spot is unreachable
            for (int i = 0; i < 200; i++)
            {
                var r = sel.Choose(chair, player, i * 0.01f);
                Assert.IsTrue(r.Found);
                float dc = r.Destination.magnitude, dp = Vector3.Distance(r.Destination, player);
                Assert.That(dc, Is.InRange(_cfg.minEscapeDistance, _cfg.maxEscapeDistance));
                Assert.That(dp, Is.GreaterThanOrEqualTo(_cfg.minDistanceFromPlayer));
                Assert.AreNotEqual(new Vector3(0, 0, 8), r.Destination, "unreachable destination chosen");
            }
        }

        [Test] // fleeing goes away from the player on average
        public void Destinations_AreMostlyAwayFromPlayer()
        {
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 7);
            Vector3 chair = Vector3.zero, player = new Vector3(-3, 0, 0);
            int away = 0;
            for (int i = 0; i < 100; i++) if (sel.Choose(chair, player, 0f).Destination.x > 0f) away++;
            Assert.That(away, Is.GreaterThanOrEqualTo(85));
        }

        [Test] // §15.6 fairness: not always the single optimal choice
        public void WeightedPick_IsNotAlwaysTheSameDestination()
        {
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 3);
            var seen = new HashSet<Vector3>();
            for (int i = 0; i < 50; i++) seen.Add(sel.Choose(Vector3.zero, new Vector3(-3, 0, 0), 0f).Destination);
            Assert.That(seen.Count, Is.GreaterThan(1));
        }

        [Test] // recently visited destinations are penalised
        public void RecentDestination_IsAvoided()
        {
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 11) { Weight_Recent = 5f };
            var first = sel.Choose(Vector3.zero, new Vector3(-3, 0, 0), 0f).Destination;
            sel.Remember(first, 0f);
            int repeats = 0;
            for (int i = 0; i < 40; i++) if (sel.Choose(Vector3.zero, new Vector3(-3, 0, 0), 0.5f).Destination == first) repeats++;
            Assert.That(repeats, Is.EqualTo(0));
        }

        [Test]
        public void SameSeed_SameChoices()
        {
            var a = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 99);
            var b = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 99);
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(a.Choose(Vector3.zero, new Vector3(-3, 0, 1), i).Destination, b.Choose(Vector3.zero, new Vector3(-3, 0, 1), i).Destination);
        }

        [Test] // cornered → no destination (drives the Obstacle opening)
        public void AllBlocked_ReturnsNotFound()
        {
            foreach (var c in _grid) _blocked.Add(c.Position);
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 1);
            Assert.IsFalse(sel.Choose(Vector3.zero, new Vector3(-3, 0, 0), 0f).Found);
        }

        [Test] // paths that run past the player are rejected
        public void PathThroughPlayer_IsRejected()
        {
            var sel = new EscapeDestinationSelector(_grid, StraightPath, _cfg, 5);
            Vector3 chair = Vector3.zero, player = new Vector3(4, 0, 0);
            for (int i = 0; i < 50; i++)
            {
                var d = sel.Choose(chair, player, 0f).Destination;
                Assert.IsFalse(d.x > 5f && Mathf.Abs(d.z) < 1.5f, "chose a route straight through the player: " + d);
            }
        }
    }
}
