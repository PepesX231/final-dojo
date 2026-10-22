using System.Collections;
using System.IO;
using System.Linq;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M2/M3 tests against the real built scenes (BedroomHub, Dream_Chair must be in Build Settings).</summary>
    public class HubFlowPlayModeTests
    {
        string _savePath;
        SaveManager _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = SaveManager.Instance;
            _savePath = Path.Combine(Path.GetTempPath(), "dreamrush_flowtest_" + System.Guid.NewGuid().ToString("N") + ".json");
            var sm = new SaveManager(_savePath);
            sm.Load();
            SaveManager.SetInstance(sm);
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            GameFlowManager.Get().SequenceSpeed = 8f;
            GameFlowManager.Get().CardDrawStep = null; // card selection is covered by CardSystem tests
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (TitleScreen.Current != null) Object.DestroyImmediate(TitleScreen.Current.gameObject);
            Hub.LobbyTheme.OverrideId = "";
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) { GameFlowManager.Get().ResetForTests(); GameFlowManager.Get().SequenceSpeed = 1f; }
            SaveManager.SetInstance(_previous);
            foreach (var p in new[] { _savePath, _savePath + ".tmp" }) if (File.Exists(p)) File.Delete(p);
            Time.timeScale = 1f;
        }

        static IEnumerator WaitUntil(System.Func<bool> cond, float timeout, string what)
        {
            float t = 0f;
            while (!cond())
            {
                t += Time.unscaledDeltaTime;
                if (t > timeout) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        IEnumerator LoadHub()
        {
            SceneManager.LoadScene(GameFlowManager.HubScene);
            yield return null;
            yield return null;
            yield return WaitUntil(() => GameFlowManager.Get().State == FlowState.Hub, 5f, "hub state");
        }

        [UnityTest] // HUB-02: bed is reachable as an interaction from the bed side
        public IEnumerator Hub_BedIsSelectableNextToBed_AndLampOpensSettings()
        {
            yield return LoadHub();
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            var bed = Object.FindFirstObjectByType<BedInteractable>();
            var sys = motor.GetComponent<InteractionSystem>();
            Assert.IsNotNull(bed); Assert.IsNotNull(sys);
            motor.Teleport(bed.wakePose.position, Quaternion.LookRotation(bed.transform.position - bed.wakePose.position));
            yield return null; yield return null;
            Assert.AreSame(bed, sys.Current, "bed should be the selected interactable");
            Assert.IsTrue(UIRoot.Get().PromptVisible);
            StringAssert.Contains("GO TO SLEEP", UIRoot.Get().PromptText);

            // lamp → settings blocks input, closing restores it
            var lamp = Object.FindObjectsByType<MenuInteractable>(FindObjectsSortMode.None).First(m => m.kind == MenuKind.Settings);
            Vector3 lampPos = lamp.transform.position; lampPos.y = 0.05f;
            Vector3 stand = lampPos + new Vector3(-1.1f, 0, -0.4f);
            motor.Teleport(stand, Quaternion.LookRotation(lampPos - stand));
            yield return null; yield return null;
            Assert.AreSame(lamp, sys.Current, "lamp should be selected");
            Assert.IsTrue(sys.TryInteract());
            yield return null;
            var input = motor.GetComponent<PlayerInputReader>();
            Assert.IsTrue(UIRoot.Get().AnyPanelOpen);
            Assert.IsInstanceOf<SettingsPanel>(UIRoot.Get().TopPanel);
            Assert.IsTrue(input.GameplayBlocked, "movement disabled while menu is open");
            UIRoot.Get().TopPanel.Close();
            yield return null;
            Assert.IsFalse(UIRoot.Get().AnyPanelOpen);
            Assert.IsFalse(input.GameplayBlocked, "control restored after closing");
        }

        [UnityTest] // FLOW-01: Hub → Dream → Hub ×5 without leaking state
        public IEnumerator SleepWakeLoop_FiveTimes_NoLeaks()
        {
            yield return LoadHub();
            for (int i = 0; i < 5; i++)
            {
                var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
                var bed = Object.FindFirstObjectByType<BedInteractable>();
                Assert.IsTrue(GameFlowManager.Get().BeginSleep(bed, motor), $"run {i}: sleep accepted");
                Assert.IsFalse(GameFlowManager.Get().BeginSleep(bed, motor), "double sleep rejected");
                yield return WaitUntil(() => GameFlowManager.Get().State == FlowState.Dream && SceneManager.GetActiveScene().name == GameFlowManager.DreamScene, 15f, "dream");
                Assert.IsNotNull(GameFlowManager.Get().CurrentRun);
                yield return new WaitForSecondsRealtime(0.2f);

                bool win = i % 2 == 0;
                Assert.IsTrue(GameFlowManager.Get().EndDream(win, win ? "Exhaustion" : null, win ? null : "test"));
                Assert.IsFalse(GameFlowManager.Get().EndDream(win, null, null), "victory/defeat triggers only once");
                yield return WaitUntil(() => GameFlowManager.Get().State == FlowState.Hub && SceneManager.GetActiveScene().name == GameFlowManager.HubScene, 15f, "back in hub");
                yield return WaitUntil(() => UIRoot.Get().TopPanel is RunSummaryPanel, 5f, "run summary");

                Assert.IsNull(GameFlowManager.Get().CurrentRun, "run state cleared");
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(Object.FindObjectsByType<GameFlowManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<UIRoot>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<PlayerMotor3D>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(InteractionSystem.All.Count(x => x == null || (x is Object o && o == null)), Is.EqualTo(0));

                // woke up next to the bed, able to move again
                var m2 = Object.FindFirstObjectByType<PlayerMotor3D>();
                var bed2 = Object.FindFirstObjectByType<BedInteractable>();
                Assert.That(Vector3.Distance(m2.transform.position, bed2.wakePose.position), Is.LessThan(0.5f), "placed at wake pose");
                UIRoot.Get().CloseAll();
                yield return null;
                Assert.IsFalse(m2.GetComponent<PlayerInputReader>().GameplayBlocked);
                Assert.IsTrue(m2.AutoStep && m2.Controller.enabled, "motor running again");
            }
            var d = SaveManager.Instance.Data;
            Assert.That(d.diary.runsStarted, Is.EqualTo(5));
            Assert.That(d.diary.runsWon, Is.EqualTo(3));
            Assert.That(d.diary.runsLost, Is.EqualTo(2));
            CollectionAssert.Contains(d.diary.discoveredVictories, "Exhaustion");
            Assert.That(d.progression.dreamMemory, Is.EqualTo(3 * 10 + 5 + 2 * 2), "memory: 10/win, +5 first discovery, 2/loss");
            Assert.IsTrue(File.Exists(_savePath), "progress persisted");
        }

        [UnityTest] // LOB-04 the game boots straight into the lobby (no title / press-start), themed by the clock; WALK frees Pip, TAB brings it back
        public IEnumerator Lobby_IsTheEntryScreen_WalkAndBack()
        {
            GameFlowManager.Get().SequenceSpeed = 1f;
            TitleScreen.Enabled = true;
            yield return LoadHub();
            yield return WaitUntil(() => TitleScreen.Current != null, 3f, "lobby on boot");
            var lobby = TitleScreen.Current;
            Assert.That(lobby.ItemCount, Is.EqualTo(6));
            Assert.That(lobby.SelectedKey, Is.EqualTo("DREAM"), "DREAM is the default choice");
            Assert.IsNotNull(Hub.LobbyThemeDirector.Current, "theme director in the room");
            Assert.That(Hub.LobbyThemeDirector.Current.Theme, Is.SameAs(Hub.LobbyTheme.Current));
            Assert.IsNotNull(RenderSettings.skybox, "dream sky");
            var motor = Object.FindFirstObjectByType<PlayerMotor3D>();
            var bridge = motor.GetComponent<PlayerUIBridge>();
            Assert.IsTrue(bridge.CutsceneLock, "Pip waits while the menu is up");
            Assert.That(Vector3.Distance(motor.transform.position, GameObject.Find("LobbyPipSpot").transform.position), Is.LessThan(0.6f), "Pip on the rug");
            foreach (var k in new[] { "WALK", "BED", "DIARY", "SETTINGS", "QUIT", "DREAM" })
            {
                lobby.SelectKey(k);
                Assert.IsNotNull(GameObject.Find("LobbyShot_" + k), "camera shot for " + k);
                yield return null;
            }
            lobby.SelectKey("WALK");
            lobby.ActivateKey("WALK");
            yield return WaitUntil(() => !bridge.CutsceneLock, 2f, "free roam");
            Assert.IsTrue(lobby.Walking);
            lobby.BackToLobby();
            yield return null; yield return null;
            Assert.IsNotNull(TitleScreen.Current, "lobby back");
            Assert.That(TitleScreen.Current, Is.Not.SameAs(lobby));
            Assert.IsTrue(bridge.CutsceneLock);
        }

        [UnityTest] // LOB-05 DREAM from the lobby goes to sleep (P5 slash card, then the bed sequence)
        public IEnumerator Lobby_DreamStartsTheSleepSequence()
        {
            GameFlowManager.Get().SequenceSpeed = 1f;
            TitleScreen.Enabled = true;
            yield return LoadHub();
            yield return WaitUntil(() => TitleScreen.Current != null, 3f, "lobby on boot");
            yield return new WaitForSecondsRealtime(0.8f);
            TitleScreen.Current.ActivateKey("DREAM");
            yield return WaitUntil(() => GameFlowManager.Get().State == FlowState.FallingAsleep, 4f, "falling asleep");
            yield return WaitUntil(() => TitleScreen.Current == null, 2f, "lobby gone");
        }
    }
}
