using UnityEditor.SceneManagement;
using UnityEngine;
namespace MamasComing.Editor
{
    public static class MamaSceneCheck
    {
        public static void Report()
        {
            var s = EditorSceneManager.GetActiveScene();
            Debug.Log("[SceneCheck] " + s.path + " dirty=" + s.isDirty + " frontEnd=" + (GameObject.Find("FrontEnd") != null));
        }
        public static void CloseGuide() { if (MamasComing.MamaHowToPlay.I) MamasComing.MamaHowToPlay.I.Close(); Debug.Log("[SceneCheck] guide closed, state=" + (MamasComing.GameManager.I ? MamasComing.GameManager.I.state.ToString() : "-")); }
        public static void Open(string path) { EditorSceneManager.OpenScene(path, OpenSceneMode.Single); Report(); }
        public static void ReloadKitchenFromDisk()
        {
            EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity", OpenSceneMode.Single);
            Report();
        }
    }
}
