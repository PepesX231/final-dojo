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
        /// Sets the game's product name (the built .exe / window title) and saves ProjectSettings.
        public static void SetProductName(string name)
        {
            UnityEditor.PlayerSettings.productName = name;
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("[SceneCheck] productName=" + UnityEditor.PlayerSettings.productName);
        }
    }
}
