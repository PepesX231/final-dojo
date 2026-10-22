using System.Collections.Generic;
using System.Linq;
using DreamRush.Cards;
using DreamRush.DreamLogic;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    public class CardLogicTests
    {
        List<CardDefinition> Pool(int n)
        {
            var l = new List<CardDefinition>();
            for (int i = 0; i < n; i++) { var c = ScriptableObject.CreateInstance<CardDefinition>(); c.cardId = "C" + i; l.Add(c); }
            return l;
        }

        [Test] // CARD-D1: exactly three distinct cards (GDD §30)
        public void DrawThree_IsDistinct_AndFromPool()
        {
            var pool = Pool(6);
            for (int seed = 0; seed < 200; seed++)
            {
                var three = CardDrawManager.DrawThree(pool, seed);
                Assert.That(three.Count, Is.EqualTo(3));
                Assert.That(three.Distinct().Count(), Is.EqualTo(3));
                CollectionAssert.IsSubsetOf(three, pool);
            }
            foreach (var c in pool) Object.DestroyImmediate(c);
        }

        [Test]
        public void DrawThree_IsSeeded_AndVaries()
        {
            var pool = Pool(6);
            CollectionAssert.AreEqual(CardDrawManager.DrawThree(pool, 42), CardDrawManager.DrawThree(pool, 42));
            var seen = new HashSet<string>();
            for (int s = 0; s < 50; s++) seen.Add(string.Join(",", CardDrawManager.DrawThree(pool, s).Select(c => c.cardId)));
            Assert.That(seen.Count, Is.GreaterThan(10), "different runs offer different hands");
            // every card appears across seeds (controlled but fair)
            var appeared = new HashSet<string>();
            for (int s = 0; s < 50; s++) foreach (var c in CardDrawManager.DrawThree(pool, s)) appeared.Add(c.cardId);
            Assert.That(appeared.Count, Is.EqualTo(6));
            foreach (var c in pool) Object.DestroyImmediate(c);
        }

        [Test]
        public void SmallPool_DrawsWhatExists()
        {
            var pool = Pool(2);
            Assert.That(CardDrawManager.DrawThree(pool, 1).Count, Is.EqualTo(2));
            foreach (var c in pool) Object.DestroyImmediate(c);
        }

        [Test] // §18: actions map to properties, not to specific objects
        public void Actions_RequireProperties()
        {
            Assert.That(EffectContext.RequiredProperty(EffectAction.Root), Is.EqualTo(DreamProperty.Rootable));
            Assert.That(EffectContext.RequiredProperty(EffectAction.Freeze), Is.EqualTo(DreamProperty.Freezeable));
            Assert.That(EffectContext.RequiredProperty(EffectAction.Pull), Is.EqualTo(DreamProperty.Pullable));
            Assert.That(EffectContext.RequiredProperty(EffectAction.ChangeBehavior), Is.EqualTo(DreamProperty.SocialResponsive));
            Assert.That(EffectContext.RequiredProperty(EffectAction.CreatePlatform), Is.EqualTo(DreamProperty.None));
        }

        [Test] // §15.9 win condition
        public void SitWinCondition_NeedsFullContinuousTime()
        {
            var w = new SitForSecondsWinCondition();
            Assert.IsFalse(w.IsSatisfied(new BossContext { SitSeconds = 1.99f, RequiredSitSeconds = 2f, SitStateValid = true }));
            Assert.IsTrue(w.IsSatisfied(new BossContext { SitSeconds = 2f, RequiredSitSeconds = 2f, SitStateValid = true }));
            Assert.IsFalse(w.IsSatisfied(new BossContext { SitSeconds = 5f, RequiredSitSeconds = 2f, SitStateValid = false }));
        }
    }
}
