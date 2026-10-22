using System.Collections;
using UnityEngine.SceneManagement;

namespace DreamRush.Tests
{
    /// <summary>Gives a PlayMode test a fresh empty scene so geometry from previously loaded scenes can't interfere.</summary>
    public static class TestScenes
    {
        public static IEnumerator Isolate()
        {
            var fresh = SceneManager.CreateScene("TestScene_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(fresh);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != fresh && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
