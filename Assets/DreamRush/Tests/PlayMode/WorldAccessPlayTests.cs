using System.Collections;
using System.IO;
using System.Linq;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Palace;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace DreamRush.Tests
{
    public class WorldAccessPlayTests
    {
        string path; SaveManager previous;
        [SetUp] public void Setup() {
            previous=SaveManager.Instance;path=Path.Combine(Path.GetTempPath(),"dreamrush_gate_"+System.Guid.NewGuid().ToString("N")+".json");
            var save=new SaveManager(path);save.Load();SaveManager.SetInstance(save);
            if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();
        }
        [TearDown] public void Cleanup() {
            if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();SaveManager.SetInstance(previous);
            foreach(var p in new[]{path,path+".tmp",path+".bak"})if(File.Exists(p))File.Delete(p);
        }
        [UnityTest] public IEnumerator GalaxyConfirm_RejectsFreshSaveAndExplainsPrerequisite() {
            SceneManager.LoadScene(GameFlowManager.GalaxyScene);yield return null;yield return null;
            var galaxy=GalaxySelect.Current;galaxy.Select(galaxy.planets.FindIndex(p=>p.sceneName=="Dream_MidnightPalace"),true);
            Assert.IsFalse(galaxy.Confirm());Assert.IsFalse(galaxy.Done);Assert.IsNull(galaxy.ChosenScene);
            Assert.IsTrue(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("THE DREAM BOOK")),"Prerequisite is visible in Galaxy UI");
            Assert.That(SaveManager.Instance.Data.progression.FindLevelRecord("Dream_World1"),Is.Null);
        }
        [UnityTest] public IEnumerator GalaxyConfirm_AllowsWorldTwoAfterPersistedWorldOneWin() {
            SaveManager.Instance.Data.progression.RecordClear("Dream_World1",240,3);SaveManager.Instance.Save();
            var reloaded=new SaveManager(path);reloaded.Load();SaveManager.SetInstance(reloaded);
            SceneManager.LoadScene(GameFlowManager.GalaxyScene);yield return null;yield return null;
            var galaxy=GalaxySelect.Current;galaxy.Select(galaxy.planets.FindIndex(p=>p.sceneName=="Dream_MidnightPalace"),true);
            Assert.IsTrue(galaxy.Confirm());Assert.That(galaxy.ChosenScene,Is.EqualTo("Dream_MidnightPalace"));
        }
        [UnityTest] public IEnumerator DecorativeCulling_TracksPlayerAndCamera_AndRestoresOnDisable() {
            SceneManager.LoadScene("Test_MovementGym");yield return null;yield return null;
            var player=Object.FindFirstObjectByType<DreamRush.Player.PlayerMotor3D>();
            var near=new GameObject("near-decor");var far=new GameObject("far-decor");
            var host=new GameObject("decor-culler");var c=host.AddComponent<PalaceDressingVisibility>();var centre=player.transform.position;
            c.rooms.Add(new PalaceDressingVisibility.Entry{dressing=near,area=new Bounds(centre,Vector3.one*10)});
            c.rooms.Add(new PalaceDressingVisibility.Entry{dressing=far,area=new Bounds(centre+Vector3.right*1000,Vector3.one*10)});
            yield return null;c.Refresh();Assert.IsTrue(near.activeSelf);Assert.IsFalse(far.activeSelf);
            var camera=Camera.main;var old=camera.transform.position;camera.transform.position=centre+Vector3.right*1000;
            c.Refresh();Assert.IsTrue(far.activeSelf);camera.transform.position=old;
            c.enabled=false;Assert.IsTrue(far.activeSelf);Object.Destroy(host);Object.Destroy(near);Object.Destroy(far);
        }
        [UnityTest] public IEnumerator ObjectiveMarker_PreservesAuthoredWidth_AndHidesWhenCollected() {
            SceneManager.LoadScene("Test_MovementGym");yield return null;yield return null;
            var h=new GameObject("test-hour").AddComponent<StolenHour>();h.hiddenUntilUnlocked=true;
            var go=new GameObject("test-beacon");go.SetActive(false);go.transform.localScale=new Vector3(.65f,2f,.45f);
            var b=go.AddComponent<HourBeacon>();b.hour=h;go.SetActive(true);yield return new WaitForSeconds(.12f);
            Assert.That(go.transform.localScale.x,Is.InRange(.59f,.71f));Assert.That(go.transform.localScale.z,Is.InRange(.41f,.49f));Assert.That(go.transform.localScale.y,Is.EqualTo(2f));
            h.Collect();yield return null;Assert.IsFalse(go.activeSelf);Object.Destroy(go);Object.Destroy(h.gameObject);
        }
    }
}
