using DreamRush.Core;
using DreamRush.Flow;
using NUnit.Framework;
using UnityEngine;
namespace DreamRush.Tests
{
    public class WorldAccessTests
    {
        [Test] public void FreshSave_RequiresWorldOneClear() {
            var p = new ProgressionData();
            Assert.IsTrue(WorldAccess.CanEnter("Dream_World1", false, p));
            Assert.IsFalse(WorldAccess.CanEnter("Dream_MidnightPalace", false, p));
            Assert.IsFalse(WorldAccess.CanEnter("Dream_MidnightPalace", false, null));
        }
        [Test] public void OtherBosses_AndPalaceClear_DoNotUnlockWorldTwo() {
            var p = new ProgressionData(); p.RecordClear("Dream_Chair", 30, 0); p.RecordClear("Dream_MidnightPalace", 90, 0);
            Assert.IsFalse(WorldAccess.CanEnter("Dream_MidnightPalace", false, p));
        }
        [Test] public void RealWorldOneClear_PersistsUnlock_WithoutChangingCurrency() {
            var s = new SaveData(); s.progression.dreamMemory = 73; s.progression.RecordClear("Dream_World1", 180, 2);
            var loaded = SaveManager.Deserialize(JsonUtility.ToJson(s));
            Assert.IsTrue(WorldAccess.CanEnter("Dream_MidnightPalace", false, loaded.progression));
            Assert.That(loaded.progression.dreamMemory, Is.EqualTo(73));
            Assert.IsFalse(WorldAccess.CanEnter("Future", true, loaded.progression));
            Assert.IsFalse(WorldAccess.CanEnter("", false, loaded.progression));
        }
        [Test] public void OldSave_OrZeroClearRecord_DoesNotInventCompletion() {
            var s = SaveManager.Deserialize("{\"version\":1,\"diary\":{\"runsWon\":4}}");
            Assert.IsFalse(WorldAccess.CanEnter("Dream_MidnightPalace", false, s.progression));
            s.progression.levelRecords.Add(new LevelRecord { scene = "Dream_World1", clears = 0 });
            Assert.IsFalse(WorldAccess.CanEnter("Dream_MidnightPalace", false, s.progression));
        }
    }
}
