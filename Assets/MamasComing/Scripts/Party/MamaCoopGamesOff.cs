using UnityEngine;
using UnityEngine.SceneManagement;

namespace MamasComing
{
    /// Owner: the co-op unicorn ("pump") and dance-off minigames were not fun and had nothing to do with
    /// fixing the house -> they are taken out of every round (host and friends alike, so the item lists match).
    static class MamaCoopGamesOff
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded -= Strip; SceneManager.sceneLoaded += Strip; }

        static void Strip(Scene scene, LoadSceneMode mode)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Coop games") { go.SetActive(false); Object.DestroyImmediate(go); }
        }
    }
}
