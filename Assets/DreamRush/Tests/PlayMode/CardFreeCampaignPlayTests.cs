using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DreamRush.Campaign;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.Progression;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DreamRush.Tests
{
    // The namespace-wide PlayModeSaveIsolation fixture guards the real save.
    public class CardFreeCampaignPlayTests
    {
        bool previousTitle, previousMute, previousCinema;
        readonly List<FlowState> states = new List<FlowState>();

        [SetUp]
        public void Setup()
        {
            previousTitle = TitleScreen.Enabled;
            previousMute = Audio.AudioManager.Muted;
            previousCinema = DreamCinematic.SkipForGameplayProbe;
            TitleScreen.Enabled = false;
            Audio.AudioManager.Muted = true;
            DreamCinematic.SkipForGameplayProbe = true;
            Time.timeScale = 1;
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) Object.DestroyImmediate(GameFlowManager.Get().gameObject);
            var flow = GameFlowManager.Get();
            flow.SequenceSpeed = 20;
            flow.StateChanged += states.Add;
            states.Clear();
            Application.logMessageReceivedThreaded += ExpectOnlyExternalMcpAuthorizationError;
        }

        [TearDown]
        public void Cleanup()
        {
            Application.logMessageReceivedThreaded -= ExpectOnlyExternalMcpAuthorizationError;
            if (GameFlowManager.Exists)
            {
                GameFlowManager.Get().StateChanged -= states.Add;
                GameFlowManager.Get().ResetForTests();
                GameFlowManager.Get().SequenceSpeed = 1;
            }
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            TitleScreen.Enabled = previousTitle;
            Audio.AudioManager.Muted = previousMute;
            DreamCinematic.SkipForGameplayProbe = previousCinema;
            Time.timeScale = 1;
        }

        // This editor connector is already unauthorized independently of the game.
        // Allow that exact infrastructure error; every game error still fails the test.
        static void ExpectOnlyExternalMcpAuthorizationError(string message, string stack, LogType type)
        {
            if (type == LogType.Error && message.Contains("[AI]") && message.Contains("McpManagerClientHub")
                && message.Contains("Server forcefully disconnected this plugin. Reason: Authorization failed."))
                LogAssert.Expect(LogType.Error, message);
        }

        static IEnumerator Until(Func<bool> ready, float timeout, string label)
        {
            float started = Time.realtimeSinceStartup;
            while (!ready())
            {
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(timeout), label);
                yield return null;
            }
        }

        static void AssertNoCardUI()
        {
            var ui = UIRoot.Get();
            var names = ui.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
            CollectionAssert.DoesNotContain(names, "CardSelect");
            CollectionAssert.DoesNotContain(names, "CardSlot");
            CollectionAssert.DoesNotContain(names, "DreamLaw");
        }

        [UnityTest]
        public IEnumerator Sleep_SelectWorld_EntersDirectly_AndWakesNormally()
        {
            SceneManager.LoadScene(GameFlowManager.HubScene);
            yield return null;
            yield return null;
            var flow = GameFlowManager.Get();
            var save = SaveManager.GetOrCreate();
            int started = save.Data.diary.runsStarted, lost = save.Data.diary.runsLost;
            Assert.That(flow.BeginSleep(Object.FindFirstObjectByType<BedInteractable>(),
                Object.FindFirstObjectByType<PlayerMotor3D>()), Is.True);
            yield return Until(() => GalaxySelect.Current != null, 20, "sleep reaches Galaxy");
            GalaxySelect.Current.Select(0, true);
            Assert.That(GalaxySelect.Current.Confirm(), Is.True);
            yield return Until(() => SceneManager.GetActiveScene().name == CampaignRoute.Scenes[0]
                && flow.State == FlowState.Dream && DreamHUD.Current != null && DreamHUD.Current.Root != null,
                20, "world starts without any card selection");
            yield return new WaitForSecondsRealtime(0.2f);
            AssertNoCardUI();
            var bridge = Type.GetType("DreamRush.DevBridge.DevBridge, DreamRush.DevBridge");
            bridge?.GetMethod("CaptureCamera", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(null, new object[] { System.IO.Path.GetFullPath("DevBridge/card-free-world1.png"), true, 1280, 720 });
            Assert.That(flow.CurrentRun.LevelScene, Is.EqualTo(CampaignRoute.Scenes[0]));
            Assert.That(save.Data.diary.runsStarted, Is.EqualTo(started + 1));
            CollectionAssert.Contains(states, FlowState.LevelSelect);
            CollectionAssert.Contains(states, FlowState.LoadingDream);
            Assert.That(flow.EndDream(false, null, "card-free flow regression"), Is.True);
            yield return Until(() => SceneManager.GetActiveScene().name == GameFlowManager.HubScene
                && flow.State == FlowState.Hub && flow.CurrentRun == null, 20, "wake returns home");
            Assert.That(save.Data.diary.runsLost, Is.EqualTo(lost + 1));
            AssertNoCardUI();
        }

        [UnityTest]
        public IEnumerator AllFiveUsedWorlds_HavePlayerAndHUD_WithoutCardInputOrMissingScripts()
        {
            foreach (var scene in CampaignRoute.Scenes)
            {
                SceneManager.LoadScene(scene);
                yield return null;
                yield return null;
                yield return Until(() => DreamHUD.Current != null && DreamHUD.Current.Root != null,
                    10, scene + " HUD starts");
                var input = Object.FindFirstObjectByType<PlayerInputReader>();
                Assert.That(input, Is.Not.Null, scene);
                Assert.That(input.Actions.FindAction("UseCard"), Is.Null, scene);
                AssertNoCardUI();
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                        foreach (var component in child.GetComponents<MonoBehaviour>())
                            Assert.That(component, Is.Not.Null, scene + ": missing script on " + child.name);
                if (scene == "Dream_World1")
                    Assert.That(Object.FindObjectsByType<World1.CognitiveTrialNode>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(6), "all authored problem-solving nodes deserialize");
                Assert.That(GameFlowManager.Get().State, Is.EqualTo(FlowState.Dream), scene);
            }
            Assert.That(Resources.Load("DreamRush/CardLibrary"), Is.Null);
        }

        [Test]
        public void LegacyPillowSave_CannotBuyRemovedUpgrade_OrExposeRerolls()
        {
            var progression = new ProgressionData { dreamMemory = 100, pillowLevel = 1 };
            CollectionAssert.AreEquivalent(new[] { BedUpgrade.CloudMattress, BedUpgrade.ComfortBlanket },
                BedUpgrades.All.Select(info => info.Id));
            Assert.That(BedUpgrades.TryBuy(progression, (BedUpgrade)1), Is.False);
            Assert.That(progression.dreamMemory, Is.EqualTo(100));
            Assert.That(BedUpgrades.BonusStability(progression), Is.Zero);
            Assert.That(BedUpgrades.StartShields(progression), Is.Zero);
        }
    }
}

