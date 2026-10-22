using DreamRush.Core;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    public class ReplayRecordTests
    {
        [Test]
        public void RecordClear_PreservesBestTimeAndStarsIndependentlyPerWorld()
        {
            var p = new ProgressionData();
            Assert.IsTrue(p.RecordClear("Dream_World1", 120f, 3));
            Assert.IsFalse(p.RecordClear("Dream_World1", 180f, 7));
            Assert.IsTrue(p.RecordClear("Dream_World1", 100f, 2));
            Assert.IsTrue(p.RecordClear("Dream_Openworld", 300f, 0));
            var w = p.FindLevelRecord("Dream_World1");
            Assert.That(w.clears, Is.EqualTo(3));
            Assert.That(w.bestSeconds, Is.EqualTo(100f));
            Assert.That(w.bestStars, Is.EqualTo(7));
            Assert.That(p.FindLevelRecord("Dream_Openworld").clears, Is.EqualTo(1));
        }

        [Test]
        public void RecordClear_RejectsInvalidRunsWithoutCreatingRecords()
        {
            var p = new ProgressionData();
            Assert.IsFalse(p.RecordClear(null, 10f, 0));
            Assert.IsFalse(p.RecordClear("World", float.NaN, 0));
            Assert.IsFalse(p.RecordClear("World", float.PositiveInfinity, 0));
            Assert.IsFalse(p.RecordClear("World", 0f, 0));
            Assert.IsFalse(p.RecordClear("World", -1f, 0));
            Assert.That(p.levelRecords, Is.Empty);
        }

        [Test]
        public void SaveRecords_RoundTripWithoutChangingExistingProgress()
        {
            var save = new SaveData();
            save.progression.dreamMemory = 71;
            save.progression.mattressLevel = 2;
            save.progression.RecordClear("Dream_MidnightPalace", 456f, 0);
            var loaded = SaveManager.Deserialize(JsonUtility.ToJson(save));
            Assert.That(loaded.progression.dreamMemory, Is.EqualTo(71));
            Assert.That(loaded.progression.mattressLevel, Is.EqualTo(2));
            Assert.That(loaded.progression.FindLevelRecord("Dream_MidnightPalace").bestSeconds, Is.EqualTo(456f));
        }

        [Test]
        public void OldSaveWithoutRecords_RemainsCompatible()
        {
            var loaded = SaveManager.Deserialize("{\"version\":1,\"progression\":{\"dreamMemory\":42}}");
            Assert.That(loaded.progression.dreamMemory, Is.EqualTo(42));
            Assert.IsNotNull(loaded.progression.levelRecords);
            Assert.IsTrue(loaded.progression.RecordClear("Dream_World1", 120f, 1));
        }
    }
}
