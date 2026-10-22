using DreamRush.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// M18 skill UI/UX (user: "if there are skills, make the UI/UX good"). A row of fist-move cards bottom-right, each
    /// with an icon, its input and a short name. Cards are context-aware, the way action games teach optional inputs:
    ///   • DIM   — can't be used right now (no stamina, no air spins left)
    ///   • HOT   — usable *this instant* and worth it (counter window after a DREAM DODGE, dash-punch window after a roll,
    ///             launcher after a landed hit, air spin while airborne): gold glow, bob and a shrinking timer ring
    ///   • PUNCH — the card kicks and flashes when that move fires, tying input → effect
    /// DREAM BURST sits on the end as a big diamond that fills with the DREAM meter and throbs when ready.
    /// </summary>
    public class SkillBar : MonoBehaviour
    {
        public PlayerCombat combat;
        public RectTransform Root { get; private set; }
        public int HotCount { get; private set; }
        class Card { public string id; public RectTransform rt, glow; public Image icon, bg, ring; public Text key; public float kick, hot; }
        Card[] _cards; Card _burst; Image _burstFill; Text _burstLabel;
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.28f), HotGold = new Color(1f, 0.82f, 0.3f);

        static readonly (string id, string icon, string key, string name)[] Defs =
        {
            ("combo",   "Combo",   "LMB",        "คอมโบหมัด"),
            ("charge",  "Slam",    "ค้าง LMB",   "ทุบพื้น"),
            ("air",     "Spin",    "อากาศ+LMB",  "หมัดหมุน"),
            ("rising",  "Rising",  "โดน→SPACE",  "อัปเปอร์คัต"),
            ("thrust",  "Dash",    "Shift→LMB",  "จรวดหมัด"),
            ("counter", "Counter", "หลบพอดี→LMB","สวนกลับ ×2"),
        };

        void Start()
        {
            if (!UIRoot.Exists || combat == null) { enabled = false; return; }
            var hud = UIRoot.Get().Hud;
            Root = UIFactory.Rect("SkillBar", hud);
            Root.anchorMin = Root.anchorMax = new Vector2(1, 0); Root.pivot = new Vector2(1, 0);
            Root.anchoredPosition = new Vector2(-34, 30); Root.sizeDelta = new Vector2(Defs.Length * 96 + 150, 150);
            _cards = new Card[Defs.Length];
            for (int i = 0; i < Defs.Length; i++) _cards[i] = MakeCard(Defs[i], new Vector2(i * 96, 0));
            // DREAM BURST diamond
            var b = UIFactory.Rect("Burst", Root); b.anchorMin = b.anchorMax = new Vector2(0, 0); b.pivot = new Vector2(0.5f, 0.5f);
            b.anchoredPosition = new Vector2(Defs.Length * 96 + 72, 70); b.sizeDelta = new Vector2(118, 118);
            var dBg = UIStyle.Image("Bg", b, UIStyle.Diamond, UIStyle.Plum, false); UIFactory.Stretch(dBg.rectTransform);
            _burstFill = UIStyle.Image("Fill", b, UIStyle.Diamond, new Color(1f, 0.35f, 0.75f), false); UIFactory.Stretch(_burstFill.rectTransform, 9);
            _burstFill.type = Image.Type.Filled; _burstFill.fillMethod = Image.FillMethod.Vertical; _burstFill.fillOrigin = 0;
            var ic = UIStyle.Image("Icon", b, Icon("Burst"), Color.white, false); UIFactory.Stretch(ic.rectTransform, 30); ic.preserveAspect = true;
            _burstLabel = UIStyle.Hud("Key", b, "V  BURST", 17, Color.white, TextAnchor.MiddleCenter);
            var kr = _burstLabel.rectTransform; kr.anchorMin = new Vector2(0, 0); kr.anchorMax = new Vector2(1, 0); kr.pivot = new Vector2(0.5f, 1f); kr.anchoredPosition = new Vector2(0, 2); kr.sizeDelta = new Vector2(40, 24);
            UIStyle.HardShadow(_burstLabel, UIStyle.Plum, new Vector2(2, -2));
            _burst = new Card { id = "burst", rt = b, icon = ic, bg = dBg };
            combat.MoveUsed += OnMove;
        }

        static Sprite Icon(string n)
        {
            var t = Resources.Load<Texture2D>("DreamRush/UI3/Skills/SK_" + n);
            return t == null ? null : Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        Card MakeCard((string id, string icon, string key, string name) d, Vector2 pos)
        {
            var c = new Card { id = d.id };
            c.rt = UIFactory.Rect("Skill_" + d.id, Root); c.rt.anchorMin = c.rt.anchorMax = new Vector2(0, 0); c.rt.pivot = new Vector2(0.5f, 0.5f);
            c.rt.anchoredPosition = pos + new Vector2(46, 78); c.rt.sizeDelta = new Vector2(88, 88);
            c.rt.localRotation = Quaternion.Euler(0, 0, (pos.x / 96f % 2f < 1f) ? 3f : -3f);   // paper-cut jitter, matches the UI kit
            var glow = UIStyle.Image("Glow", c.rt, UIStyle.Burst, new Color(HotGold.r, HotGold.g, HotGold.b, 0f), false); c.glow = glow.rectTransform;
            UIFactory.Stretch(c.glow, -26); glow.raycastTarget = false;
            c.bg = UIStyle.Image("Bg", c.rt, UIStyle.Cut, UIStyle.Plum); UIFactory.Stretch(c.bg.rectTransform);
            c.ring = UIFactory.Panel("Timer", c.rt, HotGold, UIFactory.RoundedSmall); UIFactory.Stretch(c.ring.rectTransform, 4);
            c.ring.type = Image.Type.Filled; c.ring.fillMethod = Image.FillMethod.Horizontal; c.ring.fillAmount = 0f;
            var inner = UIStyle.Image("Inner", c.rt, UIStyle.Cut, UIStyle.PlumSoft); UIFactory.Stretch(inner.rectTransform, 7);
            c.icon = UIStyle.Image("Icon", c.rt, Icon(d.icon), Color.white, false); UIFactory.Stretch(c.icon.rectTransform, 14); c.icon.preserveAspect = true;
            var name = UIStyle.Hud("Name", c.rt, d.name, 15, Color.white, TextAnchor.MiddleCenter);
            var nr = name.rectTransform; nr.anchorMin = new Vector2(0, 1); nr.anchorMax = new Vector2(1, 1); nr.pivot = new Vector2(0.5f, 0f); nr.anchoredPosition = new Vector2(0, 2); nr.sizeDelta = new Vector2(24, 22);
            UIStyle.HardShadow(name, UIStyle.Plum, new Vector2(2, -2));
            c.key = UIStyle.Hud("Key", c.rt, d.key, 13, UIStyle.Cream, TextAnchor.MiddleCenter);
            var kr = c.key.rectTransform; kr.anchorMin = new Vector2(0, 0); kr.anchorMax = new Vector2(1, 0); kr.pivot = new Vector2(0.5f, 1f); kr.anchoredPosition = new Vector2(0, -2); kr.sizeDelta = new Vector2(30, 20);
            UIStyle.HardShadow(c.key, UIStyle.Plum, new Vector2(2, -2));
            return c;
        }

        void OnMove(string id)
        {
            if (id == "burst") { _burst.kick = 1f; return; }
            if (id == "heavy") id = "charge";
            foreach (var c in _cards) if (c.id == id) c.kick = 1f;
            if (combat != null && combat.CounterOpen && id != "counter") foreach (var c in _cards) if (c.id == "counter") c.kick = 1f;
        }

        /// <summary>usable (false = dim), hot (usable now and worth it), timer (0..1 ring, -1 none).</summary>
        (bool ok, bool hot, float timer) StateOf(string id)
        {
            var c = combat; bool idle = c.State == PlayerCombat.Act.Idle;
            switch (id)
            {
                case "combo": return (c.CanHeavy && !c.Airborne, false, -1f);
                case "charge": return (c.CanHeavy && !c.Airborne, c.State == PlayerCombat.Act.Charge, c.State == PlayerCombat.Act.Charge ? c.Charge01 : -1f);
                case "air": return (c.AirSpinsLeft > 0 && c.CanHeavy, c.Airborne && c.AirSpinsLeft > 0, c.Airborne ? c.AirSpinsLeft / 2f : -1f);
                case "rising": return (true, c.RisingOpen, -1f);
                case "thrust": return (c.CanHeavy, c.DashPunchOpen, -1f);
                case "counter": return (c.CounterOpen, c.CounterOpen, c.CounterOpen ? c.CounterLeft01 : -1f);
            }
            return (idle, false, -1f);
        }

        void Update()
        {
            if (combat == null || Root == null) return;
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
            int hot = 0;
            for (int i = 0; i < _cards.Length; i++)
            {
                var c = _cards[i]; var (ok, isHot, timer) = StateOf(c.id);
                if (isHot) hot++;
                c.hot = Mathf.MoveTowards(c.hot, isHot ? 1f : 0f, dt * (isHot ? 14f : 4f));
                c.kick = Mathf.MoveTowards(c.kick, 0f, dt * 4f);
                float bob = c.hot * Mathf.Sin(now * 9f + i) * 4f;
                c.rt.anchoredPosition = new Vector2(i * 96 + 46, 78 + bob + c.hot * 10f);
                float s = 1f + c.hot * 0.12f + c.kick * c.kick * 0.3f;
                c.rt.localScale = Vector3.one * s;
                var g = c.glow.GetComponent<Image>(); g.color = new Color(HotGold.r, HotGold.g, HotGold.b, c.hot * (0.55f + 0.25f * Mathf.Sin(now * 12f)) + c.kick * 0.6f);
                c.glow.localRotation = Quaternion.Euler(0, 0, now * 40f);
                c.icon.color = Color.Lerp(ok ? Color.white : Dim, HotGold, Mathf.Max(c.hot * 0.6f, c.kick));
                c.bg.color = Color.Lerp(UIStyle.Plum, UIStyle.Red, c.kick * 0.8f);
                c.ring.fillAmount = timer < 0f ? Mathf.MoveTowards(c.ring.fillAmount, 0f, dt * 6f) : timer;
                c.key.color = ok ? UIStyle.Cream : new Color(1f, 1f, 1f, 0.4f);
            }
            HotCount = hot;
            // M19 layout: sit above the boss bar while a boss fight is on
            float y = BossBarUI.Current != null ? 150f : 30f;
            Root.anchoredPosition = new Vector2(-34, Mathf.Lerp(Root.anchoredPosition.y, y, 1f - Mathf.Exp(-8f * dt)));
            // burst diamond
            float m = combat.Meter / 100f; _burstFill.fillAmount = Mathf.MoveTowards(_burstFill.fillAmount, m, dt * 1.5f);
            bool ready = combat.BurstReady;
            float p = ready ? Mathf.PingPong(now * 3f, 1f) : 0f;
            _burst.kick = Mathf.MoveTowards(_burst.kick, 0f, dt * 3f);
            _burst.rt.localScale = Vector3.one * (1f + p * 0.1f + _burst.kick * 0.4f);
            _burst.rt.localRotation = Quaternion.Euler(0, 0, ready ? Mathf.Sin(now * 5f) * 6f : 0f);
            _burstFill.color = ready ? Color.Lerp(new Color(1f, 0.35f, 0.75f), Color.white, p * 0.5f) : new Color(1f, 0.35f, 0.75f, 0.85f);
            _burst.icon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.35f + m * 0.4f);
            _burstLabel.text = ready ? "V  BURST!" : $"{Mathf.FloorToInt(m * 100f)}%";
        }

        void OnDestroy()
        {
            if (combat != null) combat.MoveUsed -= OnMove;
            if (Root != null) Destroy(Root.gameObject);
        }
    }
}
