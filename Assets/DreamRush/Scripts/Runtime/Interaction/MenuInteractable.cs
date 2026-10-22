using System.Collections;
using DreamRush.Flow;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Interaction
{
    public enum MenuKind { Settings, DreamDiary, BedUpgrade }
    /// <summary>Physical object that opens a menu with a camera focus move (lamp → settings, bookshelf → diary).</summary>
    public class MenuInteractable : Interactable
    {
        public MenuKind kind;

        protected override void OnInteract(InteractionContext context)
        {
            var cam = Object.FindFirstObjectByType<CameraSystem.ThirdPersonCameraController>();
            if (cam != null && focusPose != null) cam.SetOverride(focusPose);
            ModalPanel panel = kind switch
            {
                MenuKind.Settings => ModalPanel.Create<SettingsPanel>("SETTINGS", new Vector2(1100, 960)),
                MenuKind.BedUpgrade => ModalPanel.Create<BedUpgradePanel>("UPGRADE BED", new Vector2(1180, 820)),
                _ => ModalPanel.Create<DreamDiaryPanel>("DREAM DIARY", new Vector2(1100, 900))
            };
            panel.Closed += () => { if (cam != null) cam.ClearOverride(); };
            StartCoroutine(Click());
            UIRoot.Get().Push(panel);
        }

        IEnumerator Click()
        {
            // tiny physical response so the object feels touched
            var t = transform;
            Vector3 s = t.localScale;
            t.localScale = s * 1.06f;
            yield return new WaitForSecondsRealtime(0.08f);
            t.localScale = s;
        }
    }
}
