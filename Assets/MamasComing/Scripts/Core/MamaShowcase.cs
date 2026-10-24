using UnityEngine;
using UnityEngine.InputSystem;
namespace MamasComing
{
    /// Opt-in presentation controls. F8 toggles them; only the authority changes the shared scene.
    public class MamaShowcase : MonoBehaviour
    {
        public static bool Enabled;
        void Update()
        {
            var k=Keyboard.current;if(k==null)return;
            if(k.f8Key.wasPressedThisFrame){Enabled=!Enabled;Debug.Log("[Showcase] "+Enabled+" | 1 Lobby, 2 Loading/Game, 3 Play, 4 Mom, 5 Ranking, 6 Failure");}
            if(!Enabled || !NetGame.IsAuthority || NetSession.I.Loading)return;
            if(k.digit1Key.wasPressedThisFrame)NetSession.I.LoadLevelForAll(MamaLaunch.LobbyScene);
            if(k.digit2Key.wasPressedThisFrame){MamaRound.Begin();NetSession.I.LoadLevelForAll(MamaLaunch.GameScene);}
            if(k.digit3Key.wasPressedThisFrame){if(GameManager.I)GameManager.I.StartRound();else{MamaRound.Begin();NetSession.I.LoadLevelForAll(MamaLaunch.GameScene);}}
            if(k.digit4Key.wasPressedThisFrame && GameManager.I)GameManager.I.PresentMom();
            if(k.digit5Key.wasPressedThisFrame)
            {
                if(GameManager.I)GameManager.I.PresentResult(true);
                else {MamaRound.Finish(true,"HOUSE HEROES!","","",Vector3.zero,"");NetSession.I.LoadLevelForAll(MamaLaunch.RankingScene);}
            }
            if(k.digit6Key.wasPressedThisFrame && GameManager.I)GameManager.I.PresentResult(false);
        }
        void OnGUI()
        {
            if(!Enabled)return;
            GUI.color=new Color(1,.9f,.55f);GUI.Box(new Rect(12,Screen.height-39,Mathf.Min(Screen.width-24,850),30),"PRESENTATION • F8 off   1 Lobby   2 Load   3 Play   4 Mom   5 Ranking   6 Failure");GUI.color=Color.white;
        }
    }
}
