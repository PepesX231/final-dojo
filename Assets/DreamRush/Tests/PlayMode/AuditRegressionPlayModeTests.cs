using System.Collections;
using System.Linq;
using System.IO;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.OpenWorld;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    public class AuditRegressionPlayModeTests
    {
        SaveManager _previousSave;
        string _temporarySave;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousSave = SaveManager.Instance;
            _temporarySave = Path.Combine(Path.GetTempPath(), "dreamrush_audit_" + System.Guid.NewGuid().ToString("N") + ".json");
            SaveManager.SetInstance(new SaveManager(_temporarySave));
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            if (GameFlowManager.Exists) GameFlowManager.Get().SequenceSpeed = 1f;
            TitleScreen.Enabled = false;
            Time.timeScale = 1f;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            if (GameFlowManager.Exists) GameFlowManager.Get().SequenceSpeed = 1f;
            SaveManager.SetInstance(_previousSave);
            if (File.Exists(_temporarySave)) File.Delete(_temporarySave);
            if (File.Exists(_temporarySave + ".tmp")) File.Delete(_temporarySave + ".tmp");
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator World1_StarTotalMatchesAuthoredCollectibles()
        {
            SceneManager.LoadScene("Dream_World1");
            yield return null; yield return null;
            int authored = Object.FindObjectsByType<DreamStar>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.That(authored, Is.GreaterThan(0));
            Assert.That(DreamStar.TotalInLevel, Is.EqualTo(authored), "HUD denominator must survive chapter director initialization");
        }

        [UnityTest]
        public IEnumerator World1_CollectingSameStarTwiceOnlyAwardsOnce()
        {
            SceneManager.LoadScene("Dream_World1");
            yield return null; yield return null;
            var star = Object.FindObjectsByType<DreamStar>(FindObjectsSortMode.None).First(s => !s.Collected);
            int before = DreamStar.CollectedThisRun;
            star.Collect(); star.Collect();
            Assert.That(DreamStar.CollectedThisRun, Is.EqualTo(before + 1));
        }

        [UnityTest]
        public IEnumerator Dreamland_ReloadResetsScoreAndAuthoredTotal()
        {
            SceneManager.LoadScene("Dream_Openworld");
            yield return null; yield return null;
            Object.FindObjectsByType<DreamStar>(FindObjectsSortMode.None).First(s => !s.Collected).Collect();
            SceneManager.LoadScene("Dream_Openworld");
            yield return null; yield return null;
            Assert.That(DreamStar.CollectedThisRun, Is.Zero, "a new dream must not inherit the previous dream's score");
            int authored = Object.FindObjectsByType<DreamStar>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.That(DreamStar.TotalInLevel, Is.EqualTo(authored));
        }

        [UnityTest]
        public IEnumerator OpenWorld_OverlappingRegionsDoNotRestartTitleEveryFrame()
        {
            SceneManager.LoadScene("Dream_Openworld");
            yield return null; yield return null;
            var director = OpenWorldDirector.Current;
            var player = Object.FindFirstObjectByType<PlayerMotor3D>();
            director.regions.Clear();
            for (int i = 0; i < 2; i++)
                director.regions.Add(new OpenWorldDirector.Region { name = "AUDIT " + i, thai = "overlap", center = player.transform.position, radius = 1000f });
            BossIntroCard.Show("AUDIT", "region stability", chapterCard: true);
            yield return null;
            var title = BossIntroCard.Current;
            Assert.IsNotNull(title);
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                Assert.That(BossIntroCard.Current, Is.SameAs(title), "standing still in an overlap must not recreate the title and its UI hierarchy");
            }
        }

        [UnityTest]
        public IEnumerator Camera_SnapAfterTeleportResetsVerticalDeadzone()
        {
            SceneManager.LoadScene("Test_MovementGym");
            yield return null; yield return null;
            var player = Object.FindFirstObjectByType<PlayerMotor3D>();
            var camera = Object.FindFirstObjectByType<DreamRush.CameraSystem.ThirdPersonCameraController>();
            player.AutoStep = false;
            camera.verticalDeadzone = 2f;
            camera.lockCursor = false;
            camera.InputEnabled = false;
            yield return null;
            player.Teleport(new Vector3(300f, 100f, 300f), Quaternion.identity);
            camera.SnapToTarget();
            float snappedY = camera.transform.position.y;
            for (int i = 0; i < 12; i++) yield return null;
            Assert.That(Mathf.Abs(camera.transform.position.y - snappedY), Is.LessThan(0.5f), "camera must not ease back toward the old floor after a teleport");
        }

        [UnityTest]
        public IEnumerator PalaceCompletion_AttributesDiaryToCountMidnight()
        {
            SceneManager.LoadScene("Test_BossMidnight");
            yield return null; yield return null;
            var flow = GameFlowManager.Get();
            flow.SequenceSpeed = 20f;
            Assert.IsTrue(flow.EndDream(true, "CountMidnight", null));
            float elapsed = 0f;
            while (flow.State != FlowState.Hub && elapsed < 8f) { elapsed += Time.unscaledDeltaTime; yield return null; }
            Assert.That(flow.State, Is.EqualTo(FlowState.Hub));
            CollectionAssert.Contains(SaveManager.GetOrCreate().Data.diary.bossesMet, "CountMidnight");
            CollectionAssert.DoesNotContain(SaveManager.GetOrCreate().Data.diary.bossesMet, "Chair");
        }
    }
}
