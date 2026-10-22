using System;
using DreamRush.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// Builds storybook-style uGUI elements from code (GDD §23.1: cream paper, dark ink, rounded corners,
    /// limited accents). Graybox-quality visuals with final layout logic; art pass swaps sprites/fonts only.
    /// </summary>
    public static class UIFactory
    {
        static Font _font, _fontBold;
        static Sprite _rounded, _roundedSmall, _note, _button, _tape, _ribbon, _moonFull, _moonEmpty, _key;

        // Storybook kit (docs/VISUAL_RECONSTRUCTION.md §2 UI): Mali (OFL, Thai + Latin) + hand-inked paper 9-slices.
        public static Font Font => _font ??= Resources.Load<Font>("DreamRush/Fonts/ChakraPetch-SemiBold") ?? Resources.Load<Font>("DreamRush/Fonts/Mali-SemiBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Font FontBold => _fontBold ??= Resources.Load<Font>("DreamRush/Fonts/ChakraPetch-Bold") ?? Font;
        /// <summary>Main paper panel (ink frame). Tinted by Image.color — the texture is near-white.</summary>
        public static Sprite Rounded => _rounded ??= UIStyle.Cut ?? CreateRoundedSprite(64, 18);
        /// <summary>Small chip (tags, category ribbons, moons background).</summary>
        public static Sprite RoundedSmall => _roundedSmall ??= CreateRoundedSprite(32, 3);
        public static Sprite Note => _note ??= Rounded;
        public static Sprite ButtonSprite => _button ??= UIStyle.Slab ?? Rounded;
        public static Sprite Tape => _tape ??= KitSprite("UI_Tape", 0);
        public static Sprite Ribbon => _ribbon ??= KitSprite("UI_Ribbon", 0, new Vector4(84, 0, 84, 0));
        public static Sprite MoonFull => _moonFull ??= KitSprite("UI_MoonFull", 0);
        public static Sprite MoonEmpty => _moonEmpty ??= KitSprite("UI_MoonEmpty", 0);
        public static Sprite Key => _key ??= KitSprite("UI_Key", 18);

        // UI v3 (UIStyle): panels are Midnight Plum, text is Cream, accent is Apple Red, selection is Lavender.
        public static readonly Color Paper = UIStyle.Plum;
        public static readonly Color PaperShade = UIStyle.PlumSoft;
        public static readonly Color Ink = UIStyle.Cream;
        public static readonly Color Accent = UIStyle.Red;
        public static readonly Color Dream = UIStyle.Lavender;
        /// <summary>Hard drop shadow under panels.</summary>
        public static readonly Color PaperShadow = new Color(0.03f, 0.02f, 0.05f, 0.85f);

        static Sprite KitSprite(string name, float border, Vector4? borders = null)
        {
            var tex = Resources.Load<Texture2D>("DreamRush/UI/" + name);
            if (tex == null) return null;
            tex.wrapMode = TextureWrapMode.Clamp;
            var b = borders ?? new Vector4(border, border, border, border);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, b);
        }

        /// <summary>A strip of washi tape pinning a paper piece (decoration with a job: says "this is a note stuck on the page").</summary>
        public static Image AddTape(RectTransform onto, Vector2 anchor, float angle, float width = 120f)
        {
            if (Tape == null) return null;
            var t = Panel("Tape", onto, Color.white, Tape);
            t.type = Image.Type.Simple;
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = anchor;
            t.rectTransform.sizeDelta = new Vector2(width, width * 0.3f);
            t.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            t.raycastTarget = false;
            return t;
        }

        public static Sprite CreateRoundedSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "T_UI_Rounded" };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Text Label(string name, Transform parent, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            bool bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            t.font = bold ? FontBold : Font;
            if (bold) style = style == FontStyle.BoldAndItalic ? FontStyle.Italic : FontStyle.Normal; // real bold face, no synthetic smear
            t.text = text;
            t.lineSpacing = 0.95f;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, string text, UnityAction onClick, float width = 360, float height = 64)
        {
            // Persona slab: the root is an invisible hit box so the slab can scale/shift without moving the layout.
            var hit = Panel(name, parent, new Color(1, 1, 1, 0), null);
            var rt = hit.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            var le = hit.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = height;
            var slab = Panel("Slab", rt, UIStyle.Plum, ButtonSprite);
            Stretch(slab.rectTransform);
            slab.raycastTarget = false;
            var sh = slab.gameObject.AddComponent<Shadow>();
            sh.effectColor = PaperShadow; sh.effectDistance = new Vector2(6, -6);
            var btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() => Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero));
            var label = Label("Label", rt, text, Mathf.RoundToInt(Mathf.Min(30, height * 0.44f)), Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(28, 0); label.rectTransform.offsetMax = new Vector2(-28, 0);
            RectTransform pointer = null;
            if (UIStyle.Pointer != null)
            {
                var p = Panel("Pointer", rt, Color.white, UIStyle.Pointer);
                p.type = Image.Type.Simple; p.raycastTarget = false;
                pointer = p.rectTransform;
                pointer.anchorMin = pointer.anchorMax = new Vector2(0, 0.5f);
                pointer.sizeDelta = new Vector2(48, 36); pointer.anchoredPosition = new Vector2(-14, 0);
                var ps = p.gameObject.AddComponent<Shadow>(); ps.effectColor = UIStyle.Plum; ps.effectDistance = new Vector2(3, -3);
            }
            var fx = hit.gameObject.AddComponent<SlabButtonFX>();
            fx.slab = slab; fx.label = label; fx.pointer = pointer;
            btn.onClick.AddListener(fx.Pulse);
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static Slider Slider(string name, Transform parent, string label, float min, float max, float value,
            Action<float> onChanged, Func<float, string> format = null)
        {
            var row = Row(name, parent, 56);
            var lbl = Label("Label", row, label, 24, Ink, TextAnchor.MiddleLeft);
            Anchor(lbl.rectTransform, 0f, 0.42f);
            var valueText = Label("Value", row, "", 22, Ink, TextAnchor.MiddleRight);
            Anchor(valueText.rectTransform, 0.86f, 1f);

            var sRt = Rect("Slider", row);
            Anchor(sRt, 0.44f, 0.84f, 0.3f, 0.7f);
            var bg = Panel("Background", sRt, Color.Lerp(Paper, Ink, 0.25f), RoundedSmall);
            Stretch(bg.rectTransform);
            var fillArea = Rect("Fill Area", sRt); Stretch(fillArea);
            var fill = Panel("Fill", fillArea, Accent, RoundedSmall);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var handleArea = Rect("Handle Area", sRt); Stretch(handleArea);
            var handle = Panel("Handle", handleArea, Ink, RoundedSmall);
            handle.rectTransform.sizeDelta = new Vector2(26, 0);
            handle.rectTransform.anchorMin = new Vector2(0, -0.4f); handle.rectTransform.anchorMax = new Vector2(0, 1.4f);

            var s = sRt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.value = value;
            format ??= v => v.ToString("0.00");
            valueText.text = format(value);
            s.onValueChanged.AddListener(v => { valueText.text = format(v); onChanged?.Invoke(v); });
            return s;
        }

        public static Toggle Toggle(string name, Transform parent, string label, bool value, Action<bool> onChanged)
        {
            var row = Row(name, parent, 52);
            var lbl = Label("Label", row, label, 24, Ink, TextAnchor.MiddleLeft);
            Anchor(lbl.rectTransform, 0f, 0.8f);
            var box = Panel("Box", row, Color.Lerp(Paper, Ink, 0.2f), RoundedSmall);
            Anchor(box.rectTransform, 0.9f, 0.98f, 0.12f, 0.88f);
            var check = Panel("Check", box.rectTransform, Accent, RoundedSmall);
            Stretch(check.rectTransform, 6);
            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = box;
            t.graphic = check;
            t.isOn = value;
            t.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return t;
        }

        public static RectTransform Row(string name, Transform parent, float height)
        {
            var rt = Rect(name, parent);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            return rt;
        }

        public static void Anchor(RectTransform rt, float xMin, float xMax, float yMin = 0f, float yMax = 1f)
        {
            rt.anchorMin = new Vector2(xMin, yMin); rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static VerticalLayoutGroup VerticalLayout(GameObject go, float spacing, int pad, TextAnchor align = TextAnchor.UpperCenter)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }
    }
}
