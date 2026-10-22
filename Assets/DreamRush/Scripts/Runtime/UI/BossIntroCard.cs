using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Boss intro splash (Astro Bot boss card × Persona cut-in): letterbox bars slam in, a red diagonal band
    /// carries the boss name in huge Kanit, the epithet types on a plum slab, then everything slides out.
    /// Purely presentational; never blocks gameplay longer than <see cref="Duration"/> (unscaled).
    /// </summary>
    public class BossIntroCard : MonoBehaviour
    {
        public const float Duration = 2.4f;
        public static BossIntroCard Current { get; private set; }
        RectTransform _top, _bottom, _band, _name, _epithet;
        Text _epithetText;
        string _epi;
        bool _chapter;
        float _t0;

        public static BossIntroCard Show(string bossName, string epithet, bool chapterCard = false)
        {
            if (Current != null) Destroy(Current.gameObject);
            var rt = UIFactory.Stretch(UIFactory.Rect("BossIntro", UIRoot.Get().Overlay));
            rt.SetAsLastSibling();
            var c = rt.gameObject.AddComponent<BossIntroCard>();
            Current = c;
            c._chapter = chapterCard;
            c.Build(bossName, epithet);
            return c;
        }

        void Build(string bossName, string epithet)
        {
            _t0 = Time.unscaledTime;
            _epi = epithet;
            _top = Bar("Top", 1f); _bottom = Bar("Bottom", 0f);

            var bandHolder = UIFactory.Rect("BandHolder", transform);
            bandHolder.anchorMin = bandHolder.anchorMax = new Vector2(0.5f, 0.5f);
            bandHolder.localRotation = Quaternion.Euler(0, 0, -7f);
            var band = UIStyle.Image("Band", bandHolder, null, _chapter ? UIStyle.Plum : UIStyle.Red); band.sprite = UIFactory.RoundedSmall;
            _band = band.rectTransform; _band.sizeDelta = new Vector2(3200, 300);
            if (UIStyle.Halftone != null) UIFactory.Stretch(UIStyle.Pattern("Halftone", _band, UIStyle.Halftone, new Color(0, 0, 0, 0.12f), 1.3f).rectTransform);
            var edge = UIStyle.Image("Edge", _band, null, UIStyle.Plum); edge.sprite = UIFactory.RoundedSmall;
            edge.rectTransform.anchorMin = new Vector2(0, 0); edge.rectTransform.anchorMax = new Vector2(1, 0); edge.rectTransform.sizeDelta = new Vector2(0, 16);

            var name = UIStyle.Title("Name", bandHolder, bossName.ToUpperInvariant(), _chapter ? 130 : 190, Color.white, TextAnchor.MiddleCenter, redDrop: _chapter);
            UIStyle.HardShadow(name, UIStyle.Plum, new Vector2(14, -14));
            name.GetComponent<Outline>().effectDistance = new Vector2(5, -5);
            _name = name.rectTransform; _name.sizeDelta = new Vector2(2000, 280);

            var epi = UIStyle.Tag("Epithet", bandHolder, "", 40, _chapter ? UIStyle.Red : UIStyle.Plum, UIStyle.Cream, new Vector2(Mathf.Max(520, epithet.Length * 26 + 120), 74));
            _epithet = epi; _epithet.anchoredPosition = new Vector2(260, -190);
            _epithetText = epi.GetComponentInChildren<Text>();
            _epithetText.font = UIStyle.TitleFontAlt;
            if (!_chapter)
            {
                Audio.AudioManager.Play(Audio.Sfx.ChairGasp, Vector3.zero);
                UIRoot.Get().ScreenFlash(new Color(1f, 0.9f, 0.9f, 0.5f), 0.2f);
            }
            else
            {
                _top.gameObject.SetActive(false); _bottom.gameObject.SetActive(false);   // chapters: no letterbox, quicker read
                _band.sizeDelta = new Vector2(3200, 220);
            }
        }

        RectTransform Bar(string n, float y)
        {
            var img = UIStyle.Image(n, transform, null, Color.black); img.sprite = null;
            var r = img.rectTransform;
            r.anchorMin = new Vector2(0, y); r.anchorMax = new Vector2(1, y); r.pivot = new Vector2(0.5f, y);
            r.sizeDelta = new Vector2(0, 150);
            return r;
        }

        void Update()
        {
            float t = Time.unscaledTime - _t0;
            float inK = UIStyle.OutCubic(t / 0.25f);
            float outK = UIStyle.InOutCubic((t - (Duration - 0.35f)) / 0.35f);
            _top.anchoredPosition = new Vector2(0, Mathf.Lerp(150, 0, inK) + 150 * outK);
            _bottom.anchoredPosition = new Vector2(0, Mathf.Lerp(-150, 0, inK) - 150 * outK);
            _band.anchoredPosition = new Vector2(Mathf.Lerp(-3300f, 0f, UIStyle.OutCubic((t - 0.1f) / 0.3f)) + 3300f * outK, 0);
            float nk = UIStyle.OutBack((t - 0.3f) / 0.3f, 2.4f);
            _name.localScale = Vector3.one * Mathf.LerpUnclamped(3f, 1f, nk) * (1f - outK);
            _name.anchoredPosition = new Vector2(Mathf.Sin(t * 1.3f) * 20f, 20);
            _name.gameObject.SetActive(t > 0.3f);
            int chars = Mathf.Clamp(Mathf.FloorToInt((t - 0.7f) * 32f), 0, _epi.Length);
            _epithetText.text = _epi.Substring(0, chars);
            _epithet.gameObject.SetActive(t > 0.65f);
            _epithet.anchoredPosition = new Vector2(260 + 3000f * outK, -190);
            if (t > Duration) Destroy(gameObject);
        }

        void OnDestroy() { if (Current == this) Current = null; }
    }
}
