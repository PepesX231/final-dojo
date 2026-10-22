using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// UI v3 — Persona × Final Fantasy (docs/RESEARCH_V2.md §1). Three colours per screen (Midnight Plum, Cream,
    /// Apple Red) + Dream Lavender as the single "selected" accent. Kanit Black Italic for titles/big numbers,
    /// Chakra Petch for HUD/labels, Mali only for diegetic storybook text. Hard shadows, skewed slabs, chamfered
    /// FF windows. Sprites from ArtSource/Illustrations/Scripts/make_ui_kit_v3.py.
    /// </summary>
    public static class UIStyle
    {
        public static readonly Color Plum = new Color(0.07f, 0.05f, 0.11f);
        public static readonly Color PlumSoft = new Color(0.14f, 0.10f, 0.21f);
        public static readonly Color Obsidian = new Color(0.04f, 0.03f, 0.06f);
        public static readonly Color Cream = new Color(0.98f, 0.96f, 0.91f);
        // Persona 5 / Chrono-Punk rebel palette: high-contrast Crimson Red, Obsidian Black, Rebel Yellow & Electric Cyan
        public static readonly Color Red = new Color(0.86f, 0.08f, 0.18f);
        public static readonly Color RedDark = new Color(0.42f, 0.03f, 0.09f);
        public static readonly Color RebelYellow = new Color(1.0f, 0.84f, 0.04f);
        public static readonly Color Cyan = new Color(0.06f, 0.84f, 0.96f);
        public static readonly Color Lavender = new Color(0.72f, 0.62f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.24f);

        static Font _title, _hud, _body, _story;
        public static Font TitleFont => _title ??= Resources.Load<Font>("DreamRush/Fonts/Kanit-BlackItalic") ?? UIFactory.FontBold;
        public static Font TitleFontAlt => Resources.Load<Font>("DreamRush/Fonts/Kanit-ExtraBoldItalic") ?? TitleFont;
        public static Font HudFont => _hud ??= Resources.Load<Font>("DreamRush/Fonts/ChakraPetch-Bold") ?? UIFactory.FontBold;
        public static Font BodyFont => _body ??= Resources.Load<Font>("DreamRush/Fonts/ChakraPetch-SemiBold") ?? HudFont;
        public static Font StoryFont => _story ??= Resources.Load<Font>("DreamRush/Fonts/Mali-SemiBold") ?? BodyFont;

        static Sprite _slab, _cut, _window, _burst, _half, _stripes, _brush, _diamond, _pointer, _moonF, _moonE, _key, _wipe, _card;
        public static Sprite Slab => _slab ??= Kit("UI3_Slab", new Vector4(34, 8, 34, 8));
        public static Sprite Slash => Slab;
        public static Sprite Cut => _cut ??= Kit("UI3_Cut", new Vector4(22, 22, 22, 22));
        public static Sprite Window => _window ??= Kit("UI3_Window", new Vector4(30, 30, 30, 30));
        public static Sprite Burst => _burst ??= Kit("UI3_Burst", Vector4.zero);
        public static Sprite Halftone => _half ??= Kit("UI3_Halftone", Vector4.zero, TextureWrapMode.Repeat);
        public static Sprite Stripes => _stripes ??= Kit("UI3_Stripes", Vector4.zero, TextureWrapMode.Repeat);
        public static Sprite Brush => _brush ??= Kit("UI3_Brush", Vector4.zero);
        public static Sprite Diamond => _diamond ??= Kit("UI3_Diamond", Vector4.zero);
        public static Sprite Pointer => _pointer ??= Kit("UI3_Pointer", Vector4.zero);
        public static Sprite MoonFull => _moonF ??= Kit("UI3_MoonFull", Vector4.zero);
        public static Sprite MoonEmpty => _moonE ??= Kit("UI3_MoonEmpty", Vector4.zero);
        public static Sprite KeyCap => _key ??= Kit("UI3_Key", new Vector4(16, 16, 16, 16));
        public static Sprite Wipe => _wipe ??= Kit("UI3_Wipe", Vector4.zero);
        public static Sprite CardFrame => _card ??= Kit("UI3_Card", new Vector4(40, 40, 40, 40));

        static Sprite _chronoGear, _swirlRing;
        /// <summary>Procedural Chrono-Punk / Persona 5 mechanical gear + inner clockwork ring sprite.</summary>
        public static Sprite ChronoGear => _chronoGear ??= MakeChronoGear();
        /// <summary>Procedural Persona-style concentric graphic swirl/ring sprite.</summary>
        public static Sprite SwirlRing => _swirlRing ??= MakeSwirlRing();

        static Sprite MakeChronoGear()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            float c = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    // 12 outer gear teeth + inner notched bezel + 6 spokes
                    float teeth = Mathf.Cos(ang * 12f);
                    float outerR = 0.84f + (teeth > 0.15f ? 0.11f : 0f);
                    float a = 0f;
                    // Outer gear ring
                    if (r <= outerR && r >= 0.64f)
                        a = Mathf.Clamp01((outerR - r) * 45f) * Mathf.Clamp01((r - 0.64f) * 45f);
                    // Inner clock tick ring
                    float ticks = Mathf.Cos(ang * 24f);
                    if (r <= 0.57f && r >= 0.49f && ticks > 0.2f)
                        a = Mathf.Max(a, 0.9f);
                    // Inner solid ring + 6 spokes
                    if (r <= 0.45f && r >= 0.37f)
                        a = Mathf.Max(a, 1f);
                    float spoke = Mathf.Abs(Mathf.Sin(ang * 3f));
                    if (r <= 0.65f && r >= 0.18f && spoke < 0.14f)
                        a = Mathf.Max(a, 0.88f);
                    // Center hub
                    if (r <= 0.20f && r >= 0.08f)
                        a = Mathf.Max(a, 1f);
                    byte ba = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
                    px[y * N + x] = new Color32(255, 255, 255, ba);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSwirlRing()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            float c = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - c) / c, dy = (y - c) / c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    float wave = Mathf.Sin(r * 28f - ang * 2f);
                    float mask = Mathf.Clamp01(1f - r) * Mathf.Clamp01((r - 0.12f) * 6f);
                    float a = mask * Mathf.SmoothStep(0.15f, 0.65f, wave);
                    byte ba = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
                    px[y * N + x] = new Color32(255, 255, 255, ba);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        public static RectTransform BuildRansomWord(string name, Transform parent, string word, float tileSize, Color accentBg, bool hot = true) =>
            BuildRansomWord(name, parent, word, tileSize, Obsidian, Cream, accentBg);

        /// <summary>Builds a Persona 5 / Chrono-Punk ransom-note word made of tilted alternating cut-out letter tiles.</summary>
        public static RectTransform BuildRansomWord(string name, Transform parent, string word, float tileSize, Color darkBg, Color lightBg, Color accentBg)
        {
            var holder = UIFactory.Rect(name, parent);
            float x = 0f;
            for (int i = 0; i < word.Length; i++)
            {
                char ch = word[i];
                if (ch == ' ') { x += tileSize * 0.42f; continue; }
                bool inv = (i % 3 == 1);
                bool acc = (i == 0);
                Color bg = acc ? accentBg : (inv ? lightBg : darkBg);
                Color fg = acc ? Color.white : (inv ? darkBg : lightBg);
                float w = (ch == 'I' || ch == '1') ? tileSize * 0.68f : (ch == 'M' || ch == 'W' ? tileSize * 1.08f : tileSize * 0.88f);
                float h = tileSize * (0.94f + (i % 2) * 0.10f);
                var tile = Image($"Ch_{i}", holder, Cut ?? Slab, bg);
                var trt = tile.rectTransform;
                trt.anchorMin = trt.anchorMax = new Vector2(0, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.sizeDelta = new Vector2(w, h);
                float yJit = ((i % 2 == 0) ? 1f : -1f) * (tileSize * 0.06f);
                float rot = ((i * 7) % 5 - 2) * 3.2f;
                trt.anchoredPosition = new Vector2(x + w * 0.5f, yJit);
                trt.localRotation = Quaternion.Euler(0, 0, rot);
                HardShadow(tile, new Color(0, 0, 0, 0.78f), new Vector2(4, -4));
                var lbl = Title("L", trt, ch.ToString(), Mathf.RoundToInt(tileSize * 0.68f), fg, TextAnchor.MiddleCenter, redDrop: false);
                Object.Destroy(lbl.GetComponent<Outline>());
                UIFactory.Stretch(lbl.rectTransform);
                x += w + tileSize * 0.05f;
            }
            holder.sizeDelta = new Vector2(x, tileSize * 1.15f);
            return holder;
        }

        static Sprite Kit(string name, Vector4 border, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var tex = Resources.Load<Texture2D>("DreamRush/UI3/" + name);
            if (tex == null) return null;
            tex.wrapMode = wrap;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        // ---------------------------------------------------------------- builders
        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool sliced = true)
        {
            var img = UIFactory.Panel(name, parent, color, sprite);
            img.type = sliced && sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Tiled pattern fill (halftone / stripes) — the Persona "texture" layer behind panels.</summary>
        public static Image Pattern(string name, Transform parent, Sprite tile, Color color, float pixelsPerUnit = 1.2f)
        {
            var img = Image(name, parent, tile, color, false);
            img.type = UnityEngine.UI.Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = pixelsPerUnit;
            return img;
        }

        /// <summary>Hard offset shadow (no blur) — layered under titles and slabs.</summary>
        public static Shadow HardShadow(Graphic g, Color color, Vector2 offset)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = color; s.effectDistance = offset; s.useGraphicAlpha = true;
            return s;
        }

        /// <summary>Kanit Black Italic title with a plum outline and a red hard drop (P5 attitude, readable on anything).</summary>
        public static Text Title(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool redDrop = true)
        {
            var t = UIFactory.Label(name, parent, text, size, color, align);
            t.font = TitleFont; t.fontStyle = FontStyle.Normal;
            t.lineSpacing = 0.85f;
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = Plum; o.effectDistance = new Vector2(3, -3);
            if (redDrop) HardShadow(t, Red, new Vector2(7, -7));
            return t;
        }

        public static Text Hud(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = UIFactory.Label(name, parent, text, size, color, align);
            t.font = HudFont; t.fontStyle = FontStyle.Normal;
            return t;
        }

        public static Text Body(string name, Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var t = UIFactory.Label(name, parent, text, size, color, align);
            t.font = BodyFont; t.fontStyle = FontStyle.Normal; t.lineSpacing = 1.05f;
            return t;
        }

        /// <summary>A skewed slab tag: coloured slab + hard plum shadow + label.</summary>
        public static RectTransform Tag(string name, Transform parent, string text, int size, Color slab, Color textColor, Vector2 sizeDelta)
        {
            var img = Image(name, parent, Slab, slab);
            img.rectTransform.sizeDelta = sizeDelta;
            HardShadow(img, Plum, new Vector2(6, -6));
            var t = Hud("Label", img.rectTransform, text, size, textColor, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(30, 0); t.rectTransform.offsetMax = new Vector2(-30, 0);
            return img.rectTransform;
        }

        /// <summary>Angular key cap glyph ("Q", "E", "R"...) — drawn for this game, used in prompts.</summary>
        public static RectTransform Key(Transform parent, string key, float size = 46f)
        {
            var img = Image("Key_" + key, parent, KeyCap, Color.white);
            img.rectTransform.sizeDelta = new Vector2(Mathf.Max(size, 22 * key.Length + 24), size);
            var t = Hud("K", img.rectTransform, key, Mathf.RoundToInt(size * 0.52f), Plum, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform); t.rectTransform.offsetMin = new Vector2(0, 4);
            return img.rectTransform;
        }

        // ---------------------------------------------------------------- easing (never linear — §1 rule 7)
        public static float OutBack(float t, float s = 1.70158f) { t = Mathf.Clamp01(t) - 1f; return t * t * ((s + 1f) * t + s) + 1f; }
        public static float OutCubic(float t) { t = 1f - Mathf.Clamp01(t); return 1f - t * t * t; }
        public static float InOutCubic(float t) { t = Mathf.Clamp01(t); return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; }
    }

    /// <summary>Entrance animation: slides in from an offset with overshoot after a delay (unscaled time).</summary>
    public class UIEnter : MonoBehaviour
    {
        public Vector2 from = new Vector2(-600, 0);
        public float delay, duration = 0.38f;
        public float fromAngle;
        RectTransform _rt; Vector2 _to; Quaternion _rotTo; float _t0; CanvasGroup _cg;
        bool _done;

        public static UIEnter Add(Component c, Vector2 from, float delay, float duration = 0.38f, float fromAngle = 0f)
        {
            Application.runInBackground = true;
            var e = c.gameObject.AddComponent<UIEnter>();
            e.from = from; e.delay = delay; e.duration = duration; e.fromAngle = fromAngle;
            e.Tick();
            return e;
        }

        void OnEnable()
        {
            _rt = (RectTransform)transform;
            if (!_done) { _to = _rt.anchoredPosition; _rotTo = _rt.localRotation; }
            _t0 = Time.unscaledTime;
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            Tick();
        }

        void Update() => Tick();

        public void Tick()
        {
            if (_rt == null) return;
            float t = duration > 0.001f ? (Time.unscaledTime - _t0 - delay) / duration : 1f;
            float k = UIStyle.OutBack(t, 1.4f);
            _rt.anchoredPosition = Vector2.LerpUnclamped(_to + from, _to, k);
            _rt.localRotation = _rotTo * Quaternion.Euler(0, 0, Mathf.LerpUnclamped(fromAngle, 0f, k));
            _cg.alpha = Mathf.Clamp01(0.25f + Mathf.Max(0f, t) * 4f);
            if (t >= 1f) { _rt.anchoredPosition = _to; _rt.localRotation = _rotTo; _cg.alpha = 1f; _done = true; enabled = false; }
        }
    }

    /// <summary>
    /// Persona-style selectable: unselected = plum slab + cream text; selected/hovered = red slab ×1.12, shifted
    /// right, white text, black hard shadow, a pointer glyph, squash on press.
    /// </summary>
    public class SlabButtonFX : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Image slab;
        public Text label;
        public RectTransform pointer;
        public Color normalSlab = UIStyle.Plum, hotSlab = UIStyle.Red, normalText = UIStyle.Cream, hotText = Color.white;
        public float hotScale = 1.12f, hotShift = 18f;
        bool _selected, _hover;
        float _k, _press;
        Vector2 _labelBase; bool _labelSet;
        Selectable _sel;

        public bool Hot => _selected || _hover;

        void Awake() { _sel = GetComponent<Selectable>(); }
        void OnEnable() { _k = Hot ? 1f : 0f; Apply(); }
        void Start() { if (label != null && !_labelSet) { _labelBase = label.rectTransform.anchoredPosition; _labelSet = true; } }
        public void OnSelect(BaseEventData e) { _selected = true; Audio.AudioManager.Play(Audio.Sfx.UiHover, Vector3.zero, 0.6f); }
        public void OnDeselect(BaseEventData e) => _selected = false;
        public void OnPointerEnter(PointerEventData e) { _hover = true; if (_sel != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject); }
        public void OnPointerExit(PointerEventData e) => _hover = false;
        public void OnPointerDown(PointerEventData e) { _press = 1f; }
        public void OnPointerUp(PointerEventData e) { }
        public void Pulse() => _press = 1f;

        void Update()
        {
            if (_sel != null && !_sel.interactable) { _selected = _hover = false; }
            _k = Mathf.MoveTowards(_k, Hot ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            _press = Mathf.MoveTowards(_press, 0f, Time.unscaledDeltaTime * 6f);
            Apply();
        }

        void Apply()
        {
            float e = UIStyle.OutBack(_k, 2.2f);
            var rt = (RectTransform)transform;
            float squash = 1f - 0.08f * Mathf.Sin(_press * Mathf.PI);
            rt.localScale = new Vector3(Mathf.LerpUnclamped(1f, hotScale, e) * (2f - squash), Mathf.LerpUnclamped(1f, hotScale, e) * squash, 1f);
            // shift the visuals, never the root (layouts and entrance tweens own the root position)
            var shift = new Vector2(hotShift * e, 0);
            if (slab != null) slab.rectTransform.anchoredPosition = shift;
            if (label != null) label.rectTransform.anchoredPosition = shift + _labelBase;
            if (pointer != null) pointer.anchoredPosition = new Vector2(-14, 0) + shift;
            bool dis = _sel != null && !_sel.interactable;
            if (slab != null) slab.color = dis ? Color.Lerp(normalSlab, Color.gray, 0.5f) : Color.Lerp(normalSlab, hotSlab, _k);
            if (label != null) label.color = dis ? new Color(normalText.r, normalText.g, normalText.b, 0.45f) : Color.Lerp(normalText, hotText, _k);
            if (pointer != null)
            {
                pointer.gameObject.SetActive(_k > 0.02f);
                pointer.localScale = Vector3.one * UIStyle.OutBack(_k, 3f);
            }
        }
    }
}
