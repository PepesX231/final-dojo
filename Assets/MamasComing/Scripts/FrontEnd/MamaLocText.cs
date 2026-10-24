using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// Bilingual label authored in the scene: edit the English and Thai text right in the Inspector.
    [ExecuteAlways, RequireComponent(typeof(Text))]
    public class MamaLocText : MonoBehaviour
    {
        [TextArea] public string en;
        [TextArea] public string th;
        [Tooltip("Font for English. Change this (not Text.font) to restyle the label.")]
        public Font latinFont;
        [Tooltip("Font for Thai (Luckiest Guy has no Thai letters). Empty = Mali Bold.")]
        public Font thaiFont;
        [Tooltip("Font size used for Thai (0 = same as English).")]
        public int thaiSize;
        [SerializeField, HideInInspector] int latinSize;
        Text text;

        void OnEnable() { text = GetComponent<Text>(); if (!latinFont && text) latinFont = text.font; if (latinSize <= 0 && text) latinSize = text.fontSize; Refresh(); }
        void Update() { Refresh(); }

        public void Refresh()
        {
            if (!text) text = GetComponent<Text>();
            if (!text) return;
            bool thai = Loc.Thai && !string.IsNullOrEmpty(th);
            string want = thai ? th : en;
            if (!string.IsNullOrEmpty(want) && text.text != want) text.text = want;
            var f = thai ? (thaiFont ? thaiFont : MamaTheme.Bold) : latinFont;
            if (f && text.font != f) text.font = f;
            int size = thai && thaiSize > 0 ? thaiSize : latinSize;
            if (size > 0 && text.fontSize != size) { text.fontSize = size; if (text.resizeTextForBestFit) text.resizeTextMaxSize = size; }
        }
    }
}
