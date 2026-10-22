using DreamRush.Core;
using DreamRush.Progression;
using NUnit.Framework;

namespace DreamRush.Tests
{
    /// <summary>GDD §21.3–21.4: three first-playable bed upgrades — costs deduct once, caps hold, effects apply.</summary>
    public class BedUpgradeTests
    {
        [Test]
        public void Buy_DeductsOnce_AndRaisesLevel()
        {
            var p = new ProgressionData { dreamMemory = 50 };
            Assert.IsTrue(BedUpgrades.TryBuy(p, BedUpgrade.CloudMattress));
            Assert.That(p.mattressLevel, Is.EqualTo(1));
            Assert.That(p.dreamMemory, Is.EqualTo(30));
            Assert.That(BedUpgrades.BonusStability(p), Is.EqualTo(1));
        }

        [Test]
        public void Buy_FailsWithoutMemory_AndChangesNothing()
        {
            var p = new ProgressionData { dreamMemory = 19 };
            Assert.IsFalse(BedUpgrades.TryBuy(p, BedUpgrade.CloudMattress));
            Assert.That(p.mattressLevel, Is.EqualTo(0));
            Assert.That(p.dreamMemory, Is.EqualTo(19));
        }

        [Test]
        public void Caps_AreRespected()
        {
            var p = new ProgressionData { dreamMemory = 1000 };
            for (int i = 0; i < 10; i++) BedUpgrades.TryBuy(p, BedUpgrade.CloudMattress);
            for (int i = 0; i < 10; i++) BedUpgrades.TryBuy(p, BedUpgrade.LuckyPillow);
            for (int i = 0; i < 10; i++) BedUpgrades.TryBuy(p, BedUpgrade.ComfortBlanket);
            Assert.That(p.mattressLevel, Is.EqualTo(3));
            Assert.That(p.pillowLevel, Is.EqualTo(1));
            Assert.That(p.blanketLevel, Is.EqualTo(1));
            Assert.That(p.dreamMemory, Is.EqualTo(1000 - (20 + 35 + 55) - 30 - 25));
            Assert.That(BedUpgrades.NextCost(p, BedUpgrade.CloudMattress), Is.EqualTo(-1));
            Assert.That(BedUpgrades.RerollsPerRun(p), Is.EqualTo(1));
            Assert.That(BedUpgrades.StartShields(p), Is.EqualTo(1));
        }

        [Test]
        public void CorruptLevels_AreClampedForEffects()
        {
            var p = new ProgressionData { mattressLevel = 99 };
            Assert.That(BedUpgrades.BonusStability(p), Is.EqualTo(3));
        }
    }
}
