using DreamRush.Core;
using UnityEngine;

namespace DreamRush.Progression
{
    /// <summary>
    /// Shows the bed's upgrades (GDD §21.5): children named "Upgrade_Mattress_1..3", "Upgrade_Pillow",
    /// "Upgrade_Blanket" are toggled from the save. Refreshes after a purchase.
    /// </summary>
    public class BedVisuals : MonoBehaviour
    {
        void OnEnable() { UI.BedUpgradePanel.Purchased += Refresh; Refresh(); }
        void OnDisable() => UI.BedUpgradePanel.Purchased -= Refresh;

        public void Refresh()
        {
            var p = SaveManager.GetOrCreate().Data.progression;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Upgrade_")) continue;
                bool on = false;
                if (t.name.StartsWith("Upgrade_Mattress_") && int.TryParse(t.name.Substring("Upgrade_Mattress_".Length), out int lv)) on = p.mattressLevel >= lv;
                else if (t.name == "Upgrade_Blanket") on = p.blanketLevel > 0;
                t.gameObject.SetActive(on);
            }
        }
    }
}
