using UnityEngine;
namespace MamasComing
{
    public static class MamaLaunch
    {
        public const string MenuScene = "MC_MainMenu", LobbyScene = "MC_Lobby", GameScene = "MamasKitchen", LoadingScene = "MC_Loading", RankingScene = "MC_Ranking";
        public static bool FromMenu;
        public static bool CanStart => NetGame.IsAuthority && NetGame.Online && NetGame.PlayerCount >= 2 && !(NetSession.I && NetSession.I.Loading);
        public static bool InBuild(string scene) => Application.CanStreamedLevelBeLoaded(scene);
        public static void ToMenu() { NetSession.Ensure(); NetSession.I.LoadLocal(MenuScene); }
        public static void ToLobby() { NetSession.Ensure(); NetSession.I.LoadLocal(LobbyScene); }
        public static void ToGame()
        {
            if (!CanStart) return;
            MamaRound.Begin();
            NetSession.I.LoadLevelForAll(GameScene);
        }
    }
}
