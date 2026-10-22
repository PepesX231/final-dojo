using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Screen transition v3 (docs/RESEARCH_V2.md §1 rule 4 — no fades): three jagged diagonal slabs (red, plum,
    /// cream) sweep across in a staggered wipe, a big Kanit caption slams in on a starburst, then the slabs keep
    /// travelling off the other side to reveal the next scene. Unscaled time.
    /// </summary>
    public class TransitionOverlay : MonoBehaviour
    {
        const float Angle = -16f;
        RectTransform[] _layers;
        RectTransform _captionHolder, _burst;
        Text _caption;
        public bool IsCovered { get; private set; }

        public static TransitionOverlay Get()
        {
            var root = UIRoot.Get();
            var existing = root.Overlay.GetComponentInChildren<TransitionOverlay>(true);
            if (existing != null) return existing;
            var rt = UIFactory.Stretch(UIFactory.Rect("TransitionOverlay", root.Overlay));
            var o = rt.gameObject.AddComponent<TransitionOverlay>();
            o.Build();
            return o;
        }

        void Build()
        {
            var colors = new[] { UIStyle.Red, UIStyle.Cream, UIStyle.Plum };   // teal · cream · plum (M20: no red)
            _layers = new RectTransform[colors.Length];
            var pivot = UIFactory.Rect("Rotated", transform);
            pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 0.5f);
            pivot.localRotation = Quaternion.Euler(0, 0, Angle);
            for (int i = 0; i < colors.Length; i++)
            {
                var layer = UIFactory.Rect("Layer_" + i, pivot);
                layer.anchorMin = layer.anchorMax = new Vector2(0.5f, 0.5f);
                layer.sizeDelta = new Vector2(3400, 2600);
                // body + a jagged leading edge (UI3_Wipe) on the right side
                var body = UIStyle.Image("Body", layer, null, colors[i]); body.sprite = UIFactory.RoundedSmall;
                body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
                body.rectTransform.offsetMin = Vector2.zero; body.rectTransform.offsetMax = new Vector2(-260, 0);
                if (UIStyle.Wipe != null)
                {
                    var edge = UIStyle.Image("Edge", layer, UIStyle.Wipe, colors[i], false);
                    edge.rectTransform.anchorMin = new Vector2(1, 0); edge.rectTransform.anchorMax = new Vector2(1, 1);
                    edge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    edge.rectTransform.sizeDelta = new Vector2(300, 0);
                    edge.rectTransform.anchoredPosition = new Vector2(-260 + 150 - 12, 0);
                    edge.rectTransform.localScale = new Vector3(-1, 1, 1);   // white side joins the body, teeth lead
                }
                if (i == 2 && UIStyle.Halftone != null)
                {
                    var ht = UIStyle.Pattern("Halftone", body.rectTransform, UIStyle.Halftone, new Color(1, 1, 1, 0.06f), 1.4f);
                    UIFactory.Stretch(ht.rectTransform);
                }
                _layers[i] = layer;
            }
            _captionHolder = UIFactory.Rect("Caption", transform);
            _captionHolder.anchorMin = _captionHolder.anchorMax = new Vector2(0.5f, 0.5f);
            _captionHolder.sizeDelta = new Vector2(1400, 400);
            _captionHolder.localRotation = Quaternion.Euler(0, 0, 6f);
            var burst = UIStyle.Image("Burst", _captionHolder, UIStyle.Burst, UIStyle.Red, false);
            _burst = burst.rectTransform; _burst.sizeDelta = new Vector2(900, 420); burst.enabled = false;   // M20: no starburst behind captions
            _caption = UIStyle.Title("Text", _captionHolder, "", 110, Color.white, TextAnchor.MiddleCenter, redDrop: false);
            UIStyle.HardShadow(_caption, UIStyle.Plum, new Vector2(10, -10));
            UIFactory.Stretch(_caption.rectTransform);
            SetProgress(0f, true);
            gameObject.SetActive(false);
        }

        /// <param name="t">0 = open, 1 = covered. Closing comes in from the left; opening leaves to the right.</param>
        void SetProgress(float t, bool closing)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                float delay = i * 0.12f;
                float k = Mathf.Clamp01((t - delay) / (1f - 0.24f));
                float x;
                if (closing) x = Mathf.LerpUnclamped(-3600f, 0f, UIStyle.OutCubic(k));
                else x = Mathf.LerpUnclamped(3600f, 0f, UIStyle.InOutCubic(k));   // leave toward the right
                _layers[i].anchoredPosition = new Vector2(x + i * 12f, 0);
            }
            float ck = Mathf.Clamp01((t - 0.7f) / 0.3f);
            bool show = !string.IsNullOrEmpty(_caption.text) && ck > 0f;
            _captionHolder.gameObject.SetActive(show);
            if (show)
            {
                float s = closing ? UIStyle.OutBack(ck, 2.6f) : ck;
                _captionHolder.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, s) * (closing ? 1f : s);
                _burst.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 20f);
                var c = _caption.color; c.a = ck; _caption.color = c;
            }
        }

        public IEnumerator Close(float duration = 0.9f, string caption = "")
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _caption.text = (caption ?? "").ToUpperInvariant();
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                SetProgress(t / duration, closing: true);
                yield return null;
            }
            SetProgress(1f, closing: true);
            IsCovered = true;
        }

        public IEnumerator Open(float duration = 0.9f)
        {
            IsCovered = false;
            for (float t = duration; t > 0; t -= Time.unscaledDeltaTime)
            {
                SetProgress(t / duration, closing: false);
                yield return null;
            }
            SetProgress(0f, closing: false);
            gameObject.SetActive(false);
        }

        public void CoverInstant(string caption = "")
        {
            gameObject.SetActive(true);
            _caption.text = (caption ?? "").ToUpperInvariant();
            SetProgress(1f, true);
            IsCovered = true;
        }
    }
}
