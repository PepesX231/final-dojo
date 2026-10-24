using UnityEngine;
namespace MamasComing
{
    public static class MamaLaunch
    {
        public const string MenuScene = "MC_MainMenu", LobbyScene = "MC_MainMenu", GameScene = "MamasKitchen", LoadingScene = "MC_Loading", RankingScene = "MC_Ranking";
        public static bool FromMenu;
        public static bool CanStart => NetGame.IsAuthority && NetGame.Online && NetGame.PlayerCount >= 2 && !(NetSession.I && NetSession.I.Loading);
        public static bool InBuild(string scene) => Application.CanStreamedLevelBeLoaded(scene);
        /// The menu scene opens straight on the character-select sofa (set before loading it).
        public static bool WantLobby;
        // Menu and lobby live in the same floating-void scene: switching between them is a camera move, not a load.
        public static void ToMenu() { if (MamaFrontEnd.I && MamaFrontEnd.I.title) { MamaFrontEnd.I.Show(MamaFrontEnd.Screen.Title); return; } NetSession.Ensure(); NetSession.I.LoadLocal(MenuScene); }
        public static void ToLobby() { if (MamaFrontEnd.I && MamaFrontEnd.I.lobby) { MamaFrontEnd.I.Show(MamaFrontEnd.Screen.Lobby); return; } WantLobby = true; NetSession.Ensure(); NetSession.I.LoadLocal(LobbyScene); }
        public static void ToGame()
        {
            if (!CanStart) return;
            MamaRound.Begin();
            NetSession.I.LoadLevelForAll(GameScene);
        }
    }
}
