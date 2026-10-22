using System.IO;
using DreamRush.Core;
using DreamRush.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    public class SaveManagerTests
    {
        string _path;

        [SetUp] public void SetUp() => _path = Path.Combine(Path.GetTempPath(), "dreamrush_test_" + System.Guid.NewGuid().ToString("N") + ".json");
        [TearDown] public void TearDown() { foreach (var p in new[] { _path, _path + ".tmp", _path + ".corrupt" }) if (File.Exists(p)) File.Delete(p); }

        [Test] // SAVE-01
        public void SaveAndLoad_RoundTrips()
        {
            var m = new SaveManager(_path);
            m.Load();
            m.Data.progression.dreamMemory = 42;
            m.Data.settings.invertY = true;
            m.Data.diary.Discover("Exhaustion");
            m.Save();
            var m2 = new SaveManager(_path);
            var d = m2.Load();
            Assert.That(d.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(d.progression.dreamMemory, Is.EqualTo(42));
            Assert.IsTrue(d.settings.invertY);
            CollectionAssert.Contains(d.diary.discoveredVictories, "Exhaustion");
        }

        [Test] // SAVE-01 (atomic overwrite)
        public void SavingTwice_ReplacesFile_NoTempLeft()
        {
            var m = new SaveManager(_path);
            m.Save();
            m.Data.progression.dreamMemory = 7;
            m.Save();
            Assert.IsFalse(File.Exists(_path + ".tmp"));
            Assert.That(new SaveManager(_path).Load().progression.dreamMemory, Is.EqualTo(7));
        }

        [Test]
        public void Version0File_IsMigrated_WithDefaults()
        {
            File.WriteAllText(_path, "{\"progression\":{\"dreamMemory\":5}}");
            var d = new SaveManager(_path).Load();
            Assert.That(d.version, Is.EqualTo(1));
            Assert.That(d.progression.dreamMemory, Is.EqualTo(5));
            Assert.IsNotNull(d.settings);
            Assert.IsNotNull(d.diary.discoveredVictories);
        }

        [Test]
        public void CorruptFile_FallsBackToFresh_AndKeepsBackup()
        {
            File.WriteAllText(_path, "{ this is not json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var d = new SaveManager(_path).Load();
            Assert.That(d.progression.dreamMemory, Is.EqualTo(0));
            Assert.IsTrue(File.Exists(_path + ".corrupt"));
        }

        [Test]
        public void Settings_AreClamped()
        {
            var s = new SettingsData { mouseSensitivity = 99f, masterVolume = -3f, textScale = 9f };
            s.Clamp();
            Assert.That(s.mouseSensitivity, Is.EqualTo(0.5f));
            Assert.That(s.masterVolume, Is.EqualTo(0f));
            Assert.That(s.textScale, Is.EqualTo(1.6f));
        }

        [Test]
        public void Diary_DiscoverOnlyOnce()
        {
            var d = new DiaryData();
            Assert.IsTrue(d.Discover("Rooted"));
            Assert.IsFalse(d.Discover("Rooted"));
            Assert.IsFalse(d.Discover(null));
        }
    }

    public class InteractionScoringTests
    {
        [Test] // HUB-01
        public void NearerAndInFront_ScoresHigher()
        {
            float near = InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(0, 0, 1f), 2f, 75f);
            float far = InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(0, 0, 1.8f), 2f, 75f);
            float side = InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(1f, 0, 0.3f), 2f, 75f);
            Assert.That(near, Is.GreaterThan(far));
            Assert.That(near, Is.GreaterThan(side));
        }

        [Test] // HUB-01
        public void BehindOrOutOfRange_IsRejected()
        {
            Assert.That(InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(0, 0, -1f), 2f, 75f), Is.EqualTo(float.NegativeInfinity));
            Assert.That(InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(0, 0, 3f), 2f, 75f), Is.EqualTo(float.NegativeInfinity));
        }

        [Test]
        public void VeryClose_IgnoresAngle()
        {
            Assert.That(InteractionScoring.Score(Vector3.zero, Vector3.forward, new Vector3(0, 0, -0.2f), 2f, 75f), Is.GreaterThan(float.NegativeInfinity));
        }
    }
}
