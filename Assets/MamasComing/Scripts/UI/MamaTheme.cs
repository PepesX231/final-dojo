using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// "Craft paper & wood" look shared by every screen: Mali (Thai + Latin) for text, Luckiest Guy for
    /// English headings, and the hand-made sprite set in Resources/MamaUI (generated for this game).
    public static class MamaTheme
    {
        // palette (matches the paper/wood sprites)
        public static readonly Color Cream = new Color(0.95f, 0.89f, 0.77f);
        public static readonly Color WoodInk = new Color(0.23f, 0.14f, 0.09f);
        public static readonly Color Brick = new Color(0.80f, 0.33f, 0.29f);
        public static readonly Color Teal = new Color(0.20f, 0.62f, 0.59f);
        public static readonly Color Mustard = new Color(0.90f, 0.71f, 0.20f);
        public static readonly Color Olive = new Color(0.60f, 0.63f, 0.23f);
        public static readonly Color Pink = new Color(1f, 0.47f, 0.62f);
        public static readonly Color Sage = new Color(0.56f, 0.65f, 0.55f);

        static Font body, bold, display;
        public static Font Body => body ? body : body = Load("Mali-Medium");
        public static Font Bold => bold ? bold : bold = Load("Mali-Bold");
        public static Font Display => display ? display : display = Load("LuckiestGuy-Regular");
        static Font Load(string n) { var f = Resources.Load<Font>("MamaUI/Fonts/" + n); return f ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public static Sprite Sprite(string n)
        {
            if (sprites.TryGetValue(n, out var s) && s) return s;
            s = Resources.Load<Sprite>("MamaUI/" + n);
            sprites[n] = s; return s;
        }
        public static Sprite PaperS => Sprite("ui_paper");
        public static Sprite TornS => Sprite("ui_paper_torn");
        public static Sprite PaintS => Sprite("ui_paint");
        public static Sprite PlankS => Sprite("ui_plank");
        public static Sprite PlankDarkS => Sprite("ui_plank_dark");
        public static Sprite BoardS => Sprite("ui_board");
        public static Sprite SlotS => Sprite("ui_slot");
        public static Sprite KnobS => Sprite("ui_knob");
        public static Sprite TapeS => Sprite("ui_tape");
        public static Sprite HexS => Sprite("ui_hex");
        public static Sprite BurstS => Sprite("ui_burst");
        public static Sprite BubbleS => Sprite("ui_bubble");
        public static Sprite StrokeS => Sprite("ui_stroke");
        public static Sprite CoinS => Sprite("ui_coin");
        public static Sprite GearS => Sprite("ui_gear");
        public static bool Ready => PaperS != null;

        static bool IsTheme(Sprite s) => s && s.name.StartsWith("ui_");

        // ------------------------------------------------------------------ fonts

        public static bool HasThai(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (c >= '฀' && c <= '๿') return true;
            return false;
        }

        public static Text MakeHeading(Text t) { if (t && !t.GetComponent<MamaThemeHeading>()) t.gameObject.AddComponent<MamaThemeHeading>(); Apply(t); return t; }

        /// Picks the themed font for a Text (called every frame by the settings translation pass).
        public static void Apply(Text t)
        {
            if (!t) return;
            var w = t.GetComponent<MamaThemeWeight>();
            if (!w)
            {
                w = t.gameObject.AddComponent<MamaThemeWeight>();
                w.hideFlags = HideFlags.HideInInspector;
                w.bold = t.fontStyle == FontStyle.Bold || t.fontStyle == FontStyle.BoldAndItalic || t.GetComponent<MamaThemeHeading>();
                t.fontStyle = t.fontStyle == FontStyle.Italic || t.fontStyle == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal;
            }
            else if (t.fontStyle == FontStyle.Bold) { w.bold = true; t.fontStyle = FontStyle.Normal; }
            Font want = t.GetComponent<MamaThemeHeading>() && !HasThai(t.text) ? Display : (w.bold ? Bold : Body);
            if (t.font != want) t.font = want;
        }

        // ------------------------------------------------------------------ skins
        public static Image Skin(Image im, Sprite s, Color? tint = null, float border = 26f)
        {
            if (!im || !s) return im;
            im.sprite = s;
            bool sliced = s.border.sqrMagnitude > 0f;
            im.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (sliced)
            {
                float b = Mathf.Max(s.border.x, s.border.y);
                var r = im.rectTransform.rect;
                float fit = Mathf.Min(border, Mathf.Max(8f, Mathf.Min(r.width, r.height) * 0.42f));
                im.pixelsPerUnitMultiplier = b / Mathf.Max(1f, fit);
            }
            if (tint.HasValue) im.color = tint.Value;
            foreach (var o in im.GetComponents<Outline>()) Object.Destroy(o);
            return im;
        }

        public static RectTransform Tape(RectTransform parent, Vector2 anchor, Vector2 offset, float angle, float width = 128f)
        {
            if (!TapeS || !parent) return null;
            var go = new GameObject("Tape", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(width, width * 0.32f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            var im = go.GetComponent<Image>(); im.sprite = TapeS; im.raycastTarget = false;
            return rt;
        }

        // ------------------------------------------------------------------ generic pass over runtime-built HUD canvases
        static readonly HashSet<int> done = new HashSet<int>();
        static readonly string[] keepPlain = { "Fill", "Bar", "Flash", "AlarmFlash", "Needle", "Zone", "Stream", "Pipe", "Spout", "SpoutTip", "Wheel", "Spoke", "Hub", "Dial", "Face", "Knob", "OffWedge", "RingBack", "Ring", "Accent", "Soak", "Hold" };
        static readonly string[] tapeOn = { "Call", "Toast", "ObjBanner", "ContextCard", "HouseNotes", "OptionalIdea", "Paper" };

        public static void RestyleAll(Sprite round, Sprite circle)
        {
            if (!Ready) return;
            foreach (var im in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!im || !done.Add(im.GetInstanceID())) continue;
                if (im.GetComponentInParent<MamaThemeSkip>(true) || IsTheme(im.sprite)) continue;
                if (im.type == Image.Type.Filled) continue;
                bool plain = im.sprite == null || im.sprite.texture == Texture2D.whiteTexture;
                bool rnd = round && im.sprite == round, circ = circle && im.sprite == circle;
                if (!plain && !rnd && !circ) continue;
                string n = im.gameObject.name;
                if (System.Array.IndexOf(keepPlain, n) >= 0) continue;
                var p = im.transform.parent;
                if (p && (p.name == "Valve" || p.name == "Gas" || p.name == "Dial" || p.name == "Wheel" || p.name == "Track")) continue;
                var r = im.rectTransform.rect;
                if (r.width < 34f || r.height < 30f || r.width >= 1800f) continue;
                if (im.color.a < 0.4f) continue;
                float lum = im.color.r * 0.3f + im.color.g * 0.59f + im.color.b * 0.11f;
                if (circ) { Skin(im, CoinS); continue; }
                if (lum < 0.33f) { var a = im.color.a; Skin(im, PlankDarkS, new Color(1, 1, 1, Mathf.Max(0.92f, a)), 22f); continue; }
                Skin(im, PaintS, null, r.height < 70f ? 16f : 26f);
                if (System.Array.IndexOf(tapeOn, n) >= 0 && !im.transform.Find("Tape"))
                    Tape(im.rectTransform, new Vector2(0.5f, 1f), new Vector2(Random.Range(-40f, 40f), -2f), Random.Range(-7f, 7f), Mathf.Clamp(r.width * 0.22f, 90f, 150f));
            }
        }
    }
}
