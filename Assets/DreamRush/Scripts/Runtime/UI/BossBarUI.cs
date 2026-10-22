using DreamRush.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Level-2 boss HUD (Elden Ring layout, chaos-pop skin): bottom-centre name plate + slanted HP bar with a white "chip"
    /// trail that catches up after a hit, a gold POSTURE bar growing from the centre (Sekiro read), and Pip's stamina
    /// slab bottom-left. Phase 2 tints the HP bar cyan ("MIDNIGHT").
    /// </summary>
    public class BossBarUI : MonoBehaviour
    {
        public static BossBarUI Current { get; private set; }
        CountMidnight _boss;
        RectTransform _hpFill, _chipFill, _postL, _postR, _stamFill, _root;
        Image _hpImg;
        Text _name, _status; Image[] _wind;
        float _chip = 1f, _lastHp = 1f, _hitAt;
        const float W = 1180f;

        public static BossBarUI Show(CountMidnight boss)
        {
            if (Current != null) Destroy(Current.gameObject);
            var rt = UIFactory.Stretch(UIFactory.Rect("BossBar", UIRoot.Get().Overlay));
            var b = rt.gameObject.AddComponent<BossBarUI>();
            b._boss = boss; Current = b; b.Build();
            return b;
        }

        public static void Hide() { if (Current != null) Destroy(Current.gameObject); }

        void OnDestroy() { if (Current == this) Current = null; }

        static RectTransform Bar(string name, Transform parent, Vector2 pos, Vector2 size, Color c, Vector2 pivot)
        {
            var img = UIStyle.Image(name, parent, UIStyle.Slab, c);
            var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0); rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
        }

        void Build()
        {
            _root = (RectTransform)transform;
            var back = Bar("HPBack", _root, new Vector2(0, 66), new Vector2(W + 24, 34), new Color(0.06f, 0.04f, 0.12f, 0.92f), new Vector2(0.5f, 0.5f));
            UIStyle.HardShadow(back.GetComponent<Image>(), Color.black, new Vector2(8, -8));
            _chipFill = Bar("Chip", _root, new Vector2(-W / 2, 66), new Vector2(W, 22), new Color(1f, 0.95f, 0.85f), new Vector2(0, 0.5f));
            _hpFill = Bar("HP", _root, new Vector2(-W / 2, 66), new Vector2(W, 22), new Color(0.92f, 0.12f, 0.28f), new Vector2(0, 0.5f));
            _hpImg = _hpFill.GetComponent<Image>();
            _postL = Bar("PostureL", _root, new Vector2(0, 98), new Vector2(0, 12), new Color(1f, 0.78f, 0.25f), new Vector2(1, 0.5f));
            _postR = Bar("PostureR", _root, new Vector2(0, 98), new Vector2(0, 12), new Color(1f, 0.78f, 0.25f), new Vector2(0, 0.5f));
            _name = UIStyle.Title("Name", _root, "COUNT MIDNIGHT", 46, Color.white, TextAnchor.LowerLeft);
            var nrt = _name.rectTransform; nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0); nrt.pivot = new Vector2(0, 0);
            nrt.anchoredPosition = new Vector2(-W / 2 + 6, 86); nrt.sizeDelta = new Vector2(900, 64);
            var ep = UIStyle.Hud("Epithet", _root, "THE TWELFTH HOUR", 22, new Color(0.75f, 0.9f, 1f), TextAnchor.LowerRight);
            var ert = ep.rectTransform; ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 0); ert.pivot = new Vector2(1, 0);
            ert.anchoredPosition = new Vector2(W / 2, 90); ert.sizeDelta = new Vector2(500, 34);
            // stamina (bottom-left)
            var sb = UIStyle.Image("StamBack", _root, UIStyle.Slab, new Color(0.06f, 0.04f, 0.12f, 0.9f));
            var sbr = sb.rectTransform; sbr.anchorMin = sbr.anchorMax = Vector2.zero; sbr.pivot = Vector2.zero; sbr.anchoredPosition = new Vector2(40, 150); sbr.sizeDelta = new Vector2(380, 26);
            var sf = UIStyle.Image("Stam", sbr, UIStyle.Slab, new Color(0.45f, 1f, 0.55f));
            _stamFill = sf.rectTransform; _stamFill.anchorMin = new Vector2(0, 0); _stamFill.anchorMax = new Vector2(0, 1); _stamFill.pivot = new Vector2(0, 0.5f);
            _stamFill.anchoredPosition = new Vector2(6, 0); _stamFill.sizeDelta = new Vector2(368, -8);
            var st = UIStyle.Hud("StamLabel", sbr, "STAMINA", 18, Color.white, TextAnchor.MiddleLeft);
            st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0, 1); st.rectTransform.pivot = new Vector2(0, 0);
            st.rectTransform.anchoredPosition = new Vector2(8, 2); st.rectTransform.sizeDelta = new Vector2(200, 26);
            // M19: the winding-key gauge (3 diamonds left of the bar) + one line telling you what works right now
            _wind = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var d = UIStyle.Image("Wind" + i, _root, UIStyle.Diamond, new Color(1f, 0.8f, 0.3f), false);
                var r = d.rectTransform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0); r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(-W / 2 - 40 - i * 34, 66); r.sizeDelta = new Vector2(34, 34);
                _wind[i] = d;
            }
            _status = UIStyle.Hud("Status", _root, "", 22, new Color(1f, 0.92f, 0.7f), TextAnchor.MiddleCenter);
            var srt = _status.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0); srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0, 30); srt.sizeDelta = new Vector2(W + 200, 32);
            UIStyle.HardShadow(_status, UIStyle.Plum, new Vector2(2, -2));
            UIEnter.Add(_root, new Vector2(0, -240), 0.05f, 0.45f);
        }

        void Update()
        {
            if (_boss == null) { Destroy(gameObject); return; }
            float hp = Mathf.Clamp01(_boss.HP / _boss.maxHP);
            _hpFill.sizeDelta = new Vector2(W * hp, 22);
            if (_chip < hp) _chip = hp;
            if (hp < _lastHp - 0.0001f) _hitAt = Time.unscaledTime;
            _lastHp = hp;
            if (Time.unscaledTime - _hitAt > 0.6f) _chip = Mathf.MoveTowards(_chip, hp, Time.unscaledDeltaTime * 0.35f);   // white chip waits, then drains
            _chipFill.sizeDelta = new Vector2(W * _chip, 22);
            _hpImg.color = _boss.Phase == 2 ? new Color(0.1f, 0.85f, 1f) : new Color(0.92f, 0.12f, 0.28f);
            float post = Mathf.Clamp01(_boss.Posture / _boss.maxPosture);
            bool broken = _boss.Current == CountMidnight.Mode.Staggered || _boss.Current == CountMidnight.Mode.Finisher;
            float pw = broken ? W * 0.5f * (0.85f + Mathf.PingPong(Time.unscaledTime * 4f, 0.15f)) : W * 0.5f * post;
            _postL.sizeDelta = _postR.sizeDelta = new Vector2(pw, 12);
            var pc = broken ? Color.white : Color.Lerp(new Color(1f, 0.78f, 0.25f), new Color(1f, 0.3f, 0.2f), post);
            _postL.GetComponent<Image>().color = _postR.GetComponent<Image>().color = pc;
            for (int i = 0; i < _wind.Length; i++)
            {
                float f = Mathf.Clamp01(_boss.Wind - i);
                _wind[i].color = Color.Lerp(new Color(0.25f, 0.2f, 0.35f, 0.8f), new Color(1f, 0.8f, 0.3f), f);
                _wind[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45f + (f > 0.01f && _boss.Wound ? Time.unscaledTime * 120f : 0f));
            }
            _status.text = _boss.StatusLine;
            _status.color = _boss.Current == CountMidnight.Mode.Frozen || _boss.HandStuck || !_boss.Wound ? Color.Lerp(Color.white, new Color(1f, 0.8f, 0.3f), Mathf.PingPong(Time.unscaledTime * 4f, 1f)) : new Color(1f, 0.92f, 0.7f, 0.85f);
            var pcmb = PlayerCombat.Current;
            if (pcmb != null && pcmb.GetComponent<CombatHUD>() != null && _stamFill.parent.gameObject.activeSelf) _stamFill.parent.gameObject.SetActive(false);   // CombatHUD already shows stamina
            if (pcmb != null) _stamFill.sizeDelta = new Vector2(368f * Mathf.Clamp01(pcmb.Stamina / pcmb.maxStamina), -8);
        }
    }
}
