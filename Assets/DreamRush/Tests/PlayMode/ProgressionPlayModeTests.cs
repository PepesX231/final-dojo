using System.Collections;
using System.IO;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.Progression;
using DreamRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M7: bed upgrades in the hub and toy-car hazards in the arena.</summary>
    public class ProgressionPlayModeTests
    {
        string _savePath;
        SaveManager _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = SaveManager.Instance;
            _savePath = Path.Combine(Path.GetTempPath(), "dreamrush_prog_" + System.Guid.NewGuid().ToString("N") + ".json");
            var sm = new SaveManager(_savePath);
            sm.Load();
            SaveManager.SetInstance(sm);
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            SaveManager.SetInstance(_previous);
            foreach (var p in new[] { _savePath, _savePath + ".tmp" }) if (File.Exists(p)) File.Delete(p);
        }

        [UnityTest] // §21.4: buy through the panel → saved, bed looks change, next run gets the bonus
        public IEnumerator BedUpgradePanel_Buys_SavesAndShowsOnBed()
        {
            SceneManager.LoadScene(GameFlowManager.HubScene);
            yield return null; yield return null;
            var save = SaveManager.Instance;
            save.Data.progression.dreamMemory = 60;
            var visuals = Object.FindFirstObjectByType<BedVisuals>();
            Assert.IsNotNull(visuals, "bed has BedVisuals");
            visuals.Refresh();
            Assert.IsFalse(visuals.transform.Find("Upgrade_Mattress_1").gameObject.activeSelf);

            var panel = ModalPanel.Create<BedUpgradePanel>("UPGRADE BED", new Vector2(1180, 820));
            UIRoot.Get().Push(panel);
            Assert.IsTrue(panel.Buy(BedUpgrade.CloudMattress));
            Assert.IsTrue(panel.Buy(BedUpgrade.ComfortBlanket));
            Assert.That(save.Data.progression.dreamMemory, Is.EqualTo(15));
            Assert.IsTrue(visuals.transform.Find("Upgrade_Mattress_1").gameObject.activeSelf, "mattress puffs appear");
            Assert.IsTrue(visuals.transform.Find("Upgrade_Blanket").gameObject.activeSelf, "quilt appears");
            Assert.IsFalse(visuals.transform.Find("Upgrade_Pillow").gameObject.activeSelf);

            var reloaded = new SaveManager(_savePath); reloaded.Load();
            Assert.That(reloaded.Data.progression.mattressLevel, Is.EqualTo(1), "purchase persisted");
            Assert.That(reloaded.Data.progression.blanketLevel, Is.EqualTo(1));
            UIRoot.Get().CloseAll();

            // next dream: stability max 4 and one blanket shield
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null; yield return null;
            var st = Object.FindFirstObjectByType<SleepStability>();
            Assert.That(st.Max, Is.EqualTo(4));
            Assert.That(st.Shields, Is.EqualTo(1));
        }

        [UnityTest] // pitch: a toy car knocks you back and costs a Sleep moon; jumping over it is safe
        public IEnumerator ToyCar_HitCostsStability_JumpingIsSafe()
        {
            SceneManager.LoadScene(GameFlowManager.DreamScene);
            yield return null; yield return null; yield return null;
            Object.Destroy(Object.FindFirstObjectByType<Boss.BossEncounter>());
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            m.AutoStep = false;
            var st = Object.FindFirstObjectByType<SleepStability>();
            var cars = Object.FindObjectsByType<RollingToyHazard>(FindObjectsSortMode.None);
            Assert.That(cars.Length, Is.GreaterThanOrEqualTo(2), "arena has toy cars");
            foreach (var c in cars) c.AutoStep = false;
            var car = cars[0];
            car.ForceActive = true;
            yield return null;

            // airborne above the car: no hit
            car.Step(0.01f);
            m.Teleport(car.transform.position + Vector3.up * 1.0f, Quaternion.identity);
            car.Step(0.01f);
            Assert.That(car.Hits, Is.EqualTo(0), "jumping over is safe");

            int before = st.Value;
            m.Teleport(car.transform.position + car.transform.forward * 0.3f + Vector3.up * 0.05f, Quaternion.identity);
            car.Step(0.01f);
            Assert.That(car.Hits, Is.EqualTo(1));
            Assert.That(st.Value, Is.EqualTo(before - 1), "costs one Sleep moon");
            Assert.That(m.VerticalVelocity, Is.GreaterThan(3f), "knocked back");
            car.Step(0.01f);
            Assert.That(car.Hits, Is.EqualTo(1), "no double hit (cooldown)");
        }
    }
}
