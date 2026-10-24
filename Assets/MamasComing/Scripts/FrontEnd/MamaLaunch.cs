using UnityEngine;
using UnityEngine.SceneManagement;

namespace MamasComing
{
    /// Scene flow: MC_MainMenu (title) -> MC_Lobby (co-op) -> MamasKitchen (gameplay).
    public static class MamaLaunch
    {
        public const string MenuScene = "MC_MainMenu", LobbyScene = "MC_Lobby", GameScene = "MamasKitchen";

        /// The gameplay scene was entered from the menu: skip its old "press any key" title and start.
        public static bool FromMenu;

        public static bool InBuild(string scene) => Application.CanStreamedLevelBeLoaded(scene);

        public static void ToMenu() { if (InBuild(MenuScene)) SceneManager.LoadScene(MenuScene); }
        public static void ToLobby() { if (InBuild(LobbyScene)) SceneManager.LoadScene(LobbyScene); }

        /// Host / solo: load the house for everyone (online clients follow the host).
        public static void ToGame()
        {
            if (!NetGame.IsAuthority) return;
            FromMenu = true;
            if (NetGame.Online && NetSession.I) NetSession.I.LoadLevelForAll(GameScene);
            else SceneManager.LoadScene(GameScene);
        }
    }
}
