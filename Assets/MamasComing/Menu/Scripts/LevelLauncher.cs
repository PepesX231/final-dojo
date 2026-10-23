using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MamasComing.Menu
{
    /// Loads the level chosen in the lobby and skips its own title screen, so the round starts as soon as the scene is up.
    /// Mama's House is synced by NetSession (host starts, clients follow the host's world state);
    /// the witch level is single-player code, so every player gets their own copy of it.
    public class LevelLauncher : MonoBehaviour
    {
        static LevelLauncher I;
        int pending = -1;

        public static void Launch(int level)
        {
            if (!I) { var go = new GameObject("LevelLauncher"); DontDestroyOnLoad(go); I = go.AddComponent<LevelLauncher>(); SceneManager.sceneLoaded += I.OnLoaded; }
            level = Mathf.Clamp(level, 0, MenuFlow.Levels.Length - 1);
            MenuFlow.Level = level; I.pending = level;
            MenuFlow.PushFruitToGame(MenuFlow.Mine.fruit);
            if (NetSession.I) NetSession.I.enabled = true;   // the lobby pauses its per-frame sync while nobody has a kid yet
            SceneManager.LoadScene(MenuFlow.Levels[level].scene);
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            if (pending < 0 || s.name != MenuFlow.Levels[pending].scene) return;
            int level = pending; pending = -1;
            StartCoroutine(Begin(level));
        }

        IEnumerator Begin(int level)
        {
            yield return null; yield return null;   // let the level's Start() build its HUD first
            if (MenuFlow.Levels[level].networked)
            {
                var gm = GameManager.I;
                if (gm && gm.state == GameManager.State.Title && NetGame.IsAuthority)
                {
                    gm.StartRound();
                    NetGame.Broadcast(NetGame.Msg.Start, null, null, 0);
                }
                // clients: NetSession starts the round when the host's world state arrives
            }
            else
            {
                var witch = FindFirstObjectByType<WitchTest.WitchLevel>();
                if (witch && witch.phase == WitchTest.WitchLevel.Phase.Title) witch.Begin();
            }
        }
    }
}
