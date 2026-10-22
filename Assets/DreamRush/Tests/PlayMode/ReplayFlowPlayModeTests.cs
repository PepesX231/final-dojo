using System.Collections;
using DreamRush.Flow;
using DreamRush.Core;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace DreamRush.Tests
{
    public class ReplayFlowPlayModeTests
    {
        SaveManager _previousSave;
        string _path;
        [SetUp]
        public void SetUp()
        {
            _previousSave = SaveManager.Instance;
            _path = Path.Combine(Path.GetTempPath(), "dreamrush_replay_" + System.Guid.NewGuid().ToString("N") + ".json");
            SaveManager.SetInstance(new SaveManager(_path));
        }

        [UnityTest]
        public IEnumerator ReplayClock_CountsDreamTimeButExcludesPauseAndHub()
        {
            var flow = GameFlowManager.Get();
            flow.ResetForTests();
            Time.timeScale = 1f;
            flow.NotifySceneStarted(SceneKind.Dream);
            yield return new WaitForSecondsRealtime(0.1f);
            float beforePause = flow.CurrentRun.ElapsedSeconds;
            Assert.That(beforePause, Is.GreaterThan(0f));
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(flow.CurrentRun.ElapsedSeconds, Is.EqualTo(beforePause));
            Time.timeScale = 1f;
            flow.SetState(FlowState.Hub);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(flow.CurrentRun.ElapsedSeconds, Is.EqualTo(beforePause));
        }

        [UnityTest]
        public IEnumerator Victory_PersistsOneRecordAndRejectsDuplicateCompletion()
        {
            var flow = GameFlowManager.Get();
            flow.ResetForTests();
            flow.SequenceSpeed = 20f;
            Time.timeScale = 1f;
            UI.TitleScreen.Enabled = false;
            SceneManager.LoadScene("Dream_Chair");
            yield return null; yield return null;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.IsTrue(flow.EndDream(true, "Exhaustion", null));
            Assert.IsFalse(flow.EndDream(true, "Exhaustion", null));
            float wait = 0f;
            while (flow.State != FlowState.Hub && wait < 8f) { wait += Time.unscaledDeltaTime; yield return null; }
            Assert.That(flow.State, Is.EqualTo(FlowState.Hub));
            var record = new SaveManager(_path).Load().progression.FindLevelRecord("Dream_Chair");
            Assert.IsNotNull(record);
            Assert.That(record.clears, Is.EqualTo(1));
            Assert.That(record.bestSeconds, Is.GreaterThan(0f));
            Assert.IsTrue(flow.LastRun.NewPersonalBest);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            if (GameFlowManager.Exists) GameFlowManager.Get().SequenceSpeed = 1f;
            if (UI.UIRoot.Exists) UI.UIRoot.Get().CloseAll();
            SaveManager.SetInstance(_previousSave);
            if (File.Exists(_path)) File.Delete(_path);
            if (File.Exists(_path + ".tmp")) File.Delete(_path + ".tmp");
        }
    }
}
