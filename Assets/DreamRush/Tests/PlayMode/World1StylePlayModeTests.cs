using System.Collections;
using System.Linq;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.Palace;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DreamRush.Tests
{
    /// <summary>M14 — World 1 in the Densha-anime look: the style pack, real post volumes, train-track rails.</summary>
    public class World1StylePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests();
            TitleScreen.Enabled = false; Time.timeScale = 1f;
            SceneManager.LoadScene("Dream_World1");
            yield return null; yield return null;
        }

        [TearDown]
        public void TearDown() { if (GameFlowManager.Exists) GameFlowManager.Get().ResetForTests(); }

        [UnityTest] // W1S-01 the Densha look is on: thick ink, razor cel, Pip in headphones, every chapter volume really has components
        public IEnumerator DenshaLook_IsOn()
        {
            yield return new WaitForSeconds(0.3f);
            var sp = StylePack.Current;
            Assert.IsNotNull(sp, "World 1 has a style pack");
            Assert.That(Shader.GetGlobalFloat("_DR_Ink"), Is.GreaterThan(0.9f));
            Assert.That(Shader.GetGlobalFloat("_DR_CelHard"), Is.GreaterThan(0.9f));
            Assert.That(sp.outfit, Is.EqualTo(StylePack.Outfit.DenshaCap));
            var pip = Object.FindFirstObjectByType<PlayerMotor3D>();
            Assert.IsNotNull(pip.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Outfit"), "headphones on");
            var vols = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(vols.Length, Is.GreaterThanOrEqualTo(5));
            foreach (var v in vols)
            {
                Assert.IsNotNull(v.sharedProfile, v.name + " profile");
                Assert.That(v.sharedProfile.components.Count(c => c != null), Is.GreaterThanOrEqualTo(4), v.sharedProfile.name + " lost its components (post never applied before M14)");
            }
        }

        [UnityTest] // W1S-02 train-track rails in chapters 1, 3 and 4; Pip can grind them
        public IEnumerator Rails_InChapters()
        {
            yield return null;
            var names = GrindRail.All.Select(r => r.name).ToList();
            Assert.That(names.Count(n => n.StartsWith("EdgeRail")), Is.GreaterThanOrEqualTo(6), "chase edge rails");
            Assert.That(names, Has.Member("C_RailLine")); Assert.That(names, Has.Member("SkyRail"));
            var m = Object.FindFirstObjectByType<PlayerMotor3D>();
            var gc = m.GetComponent<GrindController>();
            Assert.IsNotNull(gc);
            var rail = GrindRail.All.First(r => r.name == "C_RailLine");
            Vector3 p = rail.PointAt(1f, out var tan);
            m.Teleport(p + Vector3.up * 1.1f, Quaternion.LookRotation(new Vector3(tan.x, 0, tan.z)));
            float t = 0; while (!gc.Grinding && t < 2f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(gc.Grinding, "snapped onto the rail line");
        }
    }
}
