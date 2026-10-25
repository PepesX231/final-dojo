using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Dev: drive co-op jobs with a fake second kid (play mode).
    public static class MamaCoopTest
    {
        static PlayerController friend;
        static PlayerController Me => GameManager.I ? GameManager.I.player : null;
        public static void Setup()
        {
            CoopPuzzle.AllowSolo = true;
            foreach (var h in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)) if (h is ShelfHazard || h is WifiHazard || h is WaterHeaterHazard) { h.Activate(); Debug.Log("[CoopTest] activated " + h.name); }
            if (!friend && Me)
            {
                var go = Object.Instantiate(Me.gameObject, Me.transform.position + Vector3.right * 2f, Quaternion.identity); go.name = "Fake friend";
                friend = go.GetComponent<PlayerController>(); friend.isLocal = false; friend.controlsEnabled = false;
                if (friend.Motor) friend.Motor.enabled = false; var cc = go.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
                foreach (var cam in go.GetComponentsInChildren<Camera>()) Object.Destroy(cam.gameObject);
                foreach (var al in go.GetComponentsInChildren<AudioListener>()) Object.Destroy(al);
            }
        }
        static Interactable Find(string n) => Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == n);
        /// "target,me|friend,seconds" - hold E on it for a while
        public static void Hold(string arg)
        {
            var a = arg.Split(','); var t = Find(a[0]); var p = a[1] == "me" ? Me : friend; float until = (float)EditorApplication.timeSinceStartup + float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
            if (!t || !p) { Debug.Log("[CoopTest] hold: missing " + a[0]); return; }
            EditorApplication.CallbackFunction f = null;
            f = () => { if (!Application.isPlaying || EditorApplication.timeSinceStartup > until) { EditorApplication.update -= f; return; } t.HoldTick(p, .5f); };
            EditorApplication.update += f;
            if (a[1] == "friend") friend.transform.position = t.InteractPoint + Vector3.back * .6f;
        }
        public static void Press(string arg) { var a = arg.Split(','); var t = Find(a[0]); var p = a[1] == "me" ? Me : friend; if (t && p) { t.Interact(p); Debug.Log("[CoopTest] pressed " + t.name + " by " + a[1]); } }
        public static void State()
        {
            foreach (var h in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None)) if (h is ShelfHazard || h is WifiHazard || h is WaterHeaterHazard)
                Debug.Log("[CoopTest] " + h.name + " active=" + h.Active + " fixed=" + h.Fixed + " prog=" + h.Progress.ToString("F2") + (h is WaterHeaterHazard w ? " turn=" + w.Turn.ToString("F2") : ""));
        }
        public static void Hud()
        {
            var t = System.Type.GetType("MamasComing.MamaCoopBanner, Assembly-CSharp");
            Debug.Log("[CoopTest] hud type=" + t);
            if (t == null) return;
            var h = Object.FindFirstObjectByType(t, FindObjectsInactive.Include) as Component;
            Debug.Log("[CoopTest] hud obj=" + (h ? h.name + " enabled=" + ((Behaviour)h).isActiveAndEnabled : "none"));
            if (h) foreach (Transform c in h.transform) Debug.Log("[CoopTest] child " + c.name + " " + c.gameObject.activeInHierarchy);
        }
        public static void Warn() { MamasComing.MamaCoopBanner.Warn("This needs 2 players!\nCall a friend to the other spot", "ต้องใช้ 2 คน!\nเรียกเพื่อนมาช่วยอีกจุดนึง"); }
        public static void Code()
        {
            var w = Object.FindFirstObjectByType<WifiHazard>(); if (!w) return;
            var f = typeof(WifiHazard).GetField("code", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var c = (int[])f.GetValue(w); Debug.Log("[CoopTest] code " + string.Join(",", c));
        }
    }
}
