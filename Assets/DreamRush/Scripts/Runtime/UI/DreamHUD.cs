using DreamRush.Core;
using DreamRush.Dream;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Dream HUD v3 (GDD §23.3 + docs/RESEARCH_V2.md §1 fixed zones):
    /// objective + sleep moons top-left, boss name + "tired" meter top-centre,
    /// sit meter bottom-centre. Persona slabs, Chakra Petch / Kanit, hard shadows.
    /// </summary>
    public class DreamHUD : MonoBehaviour
    {
        public static DreamHUD Current { get; private set; }

        public RectTransform Root { get; private set; }
        Text _section, _objective, _bossName, _sitText;
        RectTransform _bossTag, _sitBar, _sitFill, _tiredFill, _objPanel, _stabRt;
        readonly System.Collections.Generic.List<Image> _moons = new System.Collections.Generic.List<Image>();
        readonly System.Collections.Generic.List<float> _moonPop = new System.Collections.Generic.List<float>();
        Dream.SleepStability _stability;
        DreamLevelManager _level;
        Boss.BossChairController _chair;
        string _objectiveOverride;
        float _objPulse;

        void Awake() => Current = this;

        void Start()
        {
            var ui = UIRoot.Get();
            Root = UIFactory.Stretch(UIFactory.Rect("DreamHUD", ui.Hud));
            BuildObjective();
            BuildStability();
            BuildBoss();

            _level = DreamLevelManager.Current;
            if (_level != null)
            {
                _level.SectionChanged += OnSection;
                if (_level.CurrentSection != null) OnSection(_level.CurrentSection);
            }
        }

        void BuildObjective()
        {
            var holder = UIFactory.Rect("Objective", Root);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 1); holder.pivot = new Vector2(0, 1);
            holder.anchoredPosition = new Vector2(28, -26); holder.sizeDelta = new Vector2(640, 130);
            _objPanel = holder;

            var panel = UIStyle.Image("Panel", holder, UIStyle.Cut, new Color(UIStyle.Plum.r, UIStyle.Plum.g, UIStyle.Plum.b, 0.88f));
            panel.rectTransform.anchorMin = Vector2.zero; panel.rectTransform.anchorMax = Vector2.one;
            panel.rectTransform.offsetMin = new Vector2(0, 0); panel.rectTransform.offsetMax = new Vector2(0, -26);
            UIStyle.HardShadow(panel, new Color(0, 0, 0, 0.5f), new Vector2(6, -6));
            var stripe = UIStyle.Image("Edge", panel.rectTransform, null, UIStyle.Red); stripe.sprite = UIFactory.RoundedSmall;
            stripe.rectTransform.anchorMin = new Vector2(0, 0); stripe.rectTransform.anchorMax = new Vector2(0, 1);
            stripe.rectTransform.sizeDelta = new Vector2(10, -20); stripe.rectTransform.anchoredPosition = new Vector2(14, -2);

            var tag = UIStyle.Image("SectionSlab", holder, UIStyle.Slab, UIStyle.Red);
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0, 1); tag.rectTransform.pivot = new Vector2(0, 1);
            tag.rectTransform.sizeDelta = new Vector2(360, 46); tag.rectTransform.anchoredPosition = new Vector2(-6, 6);
            tag.rectTransform.localRotation = Quaternion.Euler(0, 0, 2f);
            UIStyle.HardShadow(tag, UIStyle.Plum, new Vector2(5, -5));
            _section = UIStyle.Hud("Section", tag.rectTransform, "", 22, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_section.rectTransform); _section.rectTransform.offsetMin = new Vector2(34, 0); _section.rectTransform.offsetMax = new Vector2(-20, 0);

            _objective = UIStyle.Body("ObjectiveText", panel.rectTransform, "", 27, UIStyle.Cream, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_objective.rectTransform);
            _objective.rectTransform.offsetMin = new Vector2(40, 8); _objective.rectTransform.offsetMax = new Vector2(-18, -18);
            UIEnter.Add(holder, new Vector2(-700, 0), 0.2f, 0.4f);
        }

        void BuildStability()
        {
            var rt = UIFactory.Rect("SleepStability", Root); _stabRt = rt;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(34, -168); rt.sizeDelta = new Vector2(380, 66);
            var lbl = UIStyle.Hud("Label", rt, "SLEEP", 24, UIStyle.Cream, TextAnchor.MiddleLeft);
            UIStyle.HardShadow(lbl, UIStyle.Plum, new Vector2(3, -3));
            lbl.rectTransform.anchorMin = new Vector2(0, 0); lbl.rectTransform.anchorMax = new Vector2(0.3f, 1);
            lbl.rectTransform.offsetMin = new Vector2(4, 0); lbl.rectTransform.offsetMax = Vector2.zero;
            _stability = Dream.SleepStability.Current;
            int max = _stability != null ? _stability.Max : 3;
            for (int i = 0; i < max; i++)
            {
                var m = UIStyle.Image("Moon_" + i, rt, UIStyle.MoonFull, Color.white, false);
                m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0, 0.5f);
                m.rectTransform.sizeDelta = new Vector2(58, 58);
                m.rectTransform.anchoredPosition = new Vector2(124 + i * 56, 0);
                m.rectTransform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? -8f : 6f));
                _moons.Add(m); _moonPop.Add(0f);
            }
            if (_stability != null) { _stability.Changed += OnStability; OnStability(_stability.Value, _stability.Max); }
            UIEnter.Add(rt, new Vector2(-500, 0), 0.3f);
            FitObjective();
        }

        int _lastStability = -1;
        void OnStability(int value, int max)
        {
            for (int i = 0; i < _moons.Count; i++)
            {
                _moons[i].sprite = i < value ? UIStyle.MoonFull : UIStyle.MoonEmpty;
                if (_lastStability >= 0 && i < _lastStability && i >= value) _moonPop[i] = 1f;   // just lost this moon
            }
            _lastStability = value;
        }

        void BuildBoss()
        {
            var holder = UIFactory.Rect("BossTag", Root);
            _bossTag = holder;
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 1); holder.pivot = new Vector2(0.5f, 1);
            holder.anchoredPosition = new Vector2(0, -20); holder.sizeDelta = new Vector2(640, 120);
            var slab = UIStyle.Image("Slab", holder, UIStyle.Slab, UIStyle.Plum);
            slab.rectTransform.anchorMin = new Vector2(0, 0.38f); slab.rectTransform.anchorMax = Vector2.one;
            slab.rectTransform.offsetMin = slab.rectTransform.offsetMax = Vector2.zero;
            UIStyle.HardShadow(slab, UIStyle.Red, new Vector2(8, -8));
            _bossName = UIStyle.Title("Name", slab.rectTransform, "", 50, UIStyle.Cream, TextAnchor.MiddleCenter, redDrop: false);
            UIFactory.Stretch(_bossName.rectTransform); _bossName.rectTransform.offsetMin = new Vector2(40, 0); _bossName.rectTransform.offsetMax = new Vector2(-40, 2);
            // "TIRED" meter (Astro Bot boss bar spirit, but it fills as the boss tires out — that is the win condition)
            var bar = UIStyle.Image("TiredBar", holder, UIStyle.Slab, UIStyle.Plum);
            bar.rectTransform.anchorMin = new Vector2(0.08f, 0.04f); bar.rectTransform.anchorMax = new Vector2(0.92f, 0.32f);
            bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
            var fillHolder = UIFactory.Rect("FillMask", bar.rectTransform); UIFactory.Stretch(fillHolder, 5);
            fillHolder.gameObject.AddComponent<RectMask2D>();
            var fill = UIStyle.Image("Fill", fillHolder, UIStyle.Slab, UIStyle.Lavender);
            _tiredFill = fill.rectTransform;
            _tiredFill.anchorMin = Vector2.zero; _tiredFill.anchorMax = new Vector2(0, 1); _tiredFill.offsetMin = _tiredFill.offsetMax = Vector2.zero;
            var lbl = UIStyle.Hud("Label", bar.rectTransform, "TIRED", 18, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Stretch(lbl.rectTransform); lbl.rectTransform.offsetMin = new Vector2(34, 0);
            UIStyle.HardShadow(lbl, UIStyle.Plum, new Vector2(2, -2));
            _bossTag.gameObject.SetActive(false);

            var sit = UIFactory.Rect("SitBar", Root);
            _sitBar = sit;
            sit.anchorMin = sit.anchorMax = new Vector2(0.5f, 0); sit.pivot = new Vector2(0.5f, 0);
            sit.anchoredPosition = new Vector2(0, 170); sit.sizeDelta = new Vector2(640, 84);
            var sb = UIStyle.Image("Bg", sit, UIStyle.Slab, UIStyle.Plum); UIFactory.Stretch(sb.rectTransform);
            UIStyle.HardShadow(sb, new Color(0, 0, 0, 0.5f), new Vector2(7, -7));
            var sm = UIFactory.Rect("FillMask", sit); UIFactory.Stretch(sm, 7); sm.gameObject.AddComponent<RectMask2D>();
            var sf = UIStyle.Image("Fill", sm, UIStyle.Slab, UIStyle.Red);
            _sitFill = sf.rectTransform;
            _sitFill.anchorMin = Vector2.zero; _sitFill.anchorMax = new Vector2(0, 1); _sitFill.offsetMin = _sitFill.offsetMax = Vector2.zero;
            _sitText = UIStyle.Title("Text", sit, "", 38, Color.white, TextAnchor.MiddleCenter, redDrop: false);
            UIFactory.Stretch(_sitText.rectTransform);
            _sitBar.gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_bossTag != null && _bossTag.gameObject.activeSelf)
            {
                if (_chair == null) _chair = FindFirstObjectByType<Boss.BossChairController>();
                float k = _chair != null && _chair.Fatigue != null ? _chair.Fatigue.Normalized : 0f;
                _tiredFill.anchorMax = new Vector2(Mathf.Lerp(_tiredFill.anchorMax.x, k, 1f - Mathf.Exp(-8f * dt)), 1);
            }
            for (int i = 0; i < _moons.Count; i++)
            {
                if (_moonPop[i] <= 0f) { _moons[i].rectTransform.localScale = Vector3.one; continue; }
                _moonPop[i] = Mathf.MoveTowards(_moonPop[i], 0f, dt * 2f);
                float p = _moonPop[i];
                _moons[i].rectTransform.localScale = Vector3.one * (1f + 0.6f * Mathf.Sin(p * Mathf.PI));
                _moons[i].rectTransform.anchoredPosition = new Vector2(124 + i * 56, Mathf.Sin(p * 40f) * 8f * p);
            }
            if (_objPanel != null && _objPulse > 0f)
            {
                _objPulse = Mathf.MoveTowards(_objPulse, 0f, dt * 2.5f);
                _objPanel.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(_objPulse * Mathf.PI));
            }
        }

        public void ShowBoss(string name)
        {
            if (_bossTag == null) return;
            _bossName.text = name;
            if (!_bossTag.gameObject.activeSelf)
            {
                _bossTag.gameObject.SetActive(true);
                UIEnter.Add(_bossTag, new Vector2(0, 220), 0f, 0.45f);
            }
        }

        /// <summary>"SITTING... 1.2 / 2.0 SEC" (GDD §23.4). Negative seconds hides the bar.</summary>
        public void SetSitProgress(float seconds, float total)
        {
            if (_sitBar == null) return;
            bool show = seconds >= 0f;
            _sitBar.gameObject.SetActive(show);
            if (!show) return;
            float k = Mathf.Clamp01(seconds / total);
            _sitFill.anchorMax = new Vector2(k, 1);
            _sitText.text = $"SITTING...  {Mathf.Min(seconds, total):0.0} / {total:0.0} SEC";
            _sitBar.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 18f) * k);
        }

        void OnDestroy()
        {
            if (_stability != null) _stability.Changed -= OnStability;
            if (_level != null) _level.SectionChanged -= OnSection;
            if (Root != null) Destroy(Root.gameObject);
            if (Current == this) Current = null;
        }

        void OnSection(DreamSection s)
        {
            _section.text = $"{s.sectionId}  ◆  {s.displayName.ToUpperInvariant()}";
            _objective.text = _objectiveOverride ?? s.objective;
            FitObjective();
            _objPulse = 1f;
        }

        /// <summary>Boss phases replace the section objective (e.g. "SIT ON THE CHAIR"). Null restores it.</summary>
        public void SetObjective(string text)
        {
            _objectiveOverride = text;
            if (_objective != null) _objective.text = text ?? (_level != null && _level.CurrentSection != null ? _level.CurrentSection.objective : "");
            FitObjective();
            _objPulse = 1f;
        }

        /// <summary>M18: a multi-line checklist (level 2) grows the panel and pushes the SLEEP moons down instead of spilling over them.</summary>
        void FitObjective()
        {
            if (_objective == null || _objPanel == null) return;
            int lines = 1; foreach (char ch in _objective.text ?? "") if (ch == '\n') lines++;
            bool list = lines > 2;
            _objective.fontSize = list ? 19 : 27;
            _objective.alignment = list ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            _objective.rectTransform.offsetMax = new Vector2(-18, list ? -30 : -18);
            float h = list ? 66f + lines * 25f : 130f;
            _objPanel.sizeDelta = new Vector2(list ? 720f : 640f, h);
            if (_stabRt != null) _stabRt.anchoredPosition = new Vector2(34, -(h + 38f));
        }

        public string ObjectiveText => _objective != null ? _objective.text : null;
    }
}
