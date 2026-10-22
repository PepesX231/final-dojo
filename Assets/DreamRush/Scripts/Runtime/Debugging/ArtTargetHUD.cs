using UnityEngine;

namespace DreamRush.Debugging
{
    /// <summary>Art Target only: fills the real HUD with representative content so UI is judged in context.</summary>
    public class ArtTargetHUD : MonoBehaviour
    {

        System.Collections.IEnumerator Start()
        {
            yield return null; yield return null;
            var hud = UI.DreamHUD.Current;
            if (hud != null)
            {
                hud.SetObjective("SIT ON THE CHAIR");
                hud.ShowBoss("THE CHAIR");
            }
        }
    }
}
