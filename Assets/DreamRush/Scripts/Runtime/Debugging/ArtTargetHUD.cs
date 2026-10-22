using UnityEngine;

namespace DreamRush.Debugging
{
    /// <summary>Art Target only: fills the real HUD with representative content so UI is judged in context.</summary>
    public class ArtTargetHUD : MonoBehaviour
    {
        public string cardId = "CameraFlash";

        System.Collections.IEnumerator Start()
        {
            yield return null; yield return null;
            var hud = UI.DreamHUD.Current;
            if (hud != null)
            {
                hud.SetObjective("SIT ON THE CHAIR");
                hud.ShowBoss("THE CHAIR");
            }
            var cards = FindFirstObjectByType<Cards.PlayerCardController>();
            var lib = Cards.CardLibrary.Load();
            if (cards != null && lib != null) cards.Equip(lib.Find(cardId));
        }
    }
}
