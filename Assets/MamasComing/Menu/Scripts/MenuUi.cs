using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MamasComing.Menu
{
    /// Small code-built uGUI kit shared by the main menu and the lobby (same approach as MamaUI: no prefabs to break).
    public static class MenuUi
    {
        public static readonly Color Ink = new Color(.07f, .06f, .07f);
        public static readonly Color Paper = new Color(1f, .97f, .88f);
        public static readonly Color Pink = new Color(1f, .27f, .55f);
        public static readonly Color Teal = new Color(.36f, .89f, .42f);   // toxic green
        public static readonly Color Lemon = new Color(.93f, .96f, .2f);   // acid yellow
        public static readonly Color Grape = new Color(.5f, .36f, .92f);
        public static readonly Color Shade = new Color(.12f, .05f, .12f, .55f);

        static Font font;
        public static Font Font => font ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Sprite round;
        /// Rounded rectangle sprite (9-sliced) generated once.
        public static Sprite Round
        {
            get
            {
                if (round) return round;
                const int S = 64, R = 22;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Max(R - x - .5f, x + .5f - (S - R))), dy = Mathf.Max(0, Mathf.Max(R - y - .5f, y + .5f - (S - R)));
                        float a = Mathf.Clamp01(R - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                        px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                round = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
                return round;
            }
        }

        static Texture2D halftone;
        /// Tileable comic halftone dot (white dot on transparent) for screen overlays.
        public static Texture2D Halftone
        {
            get
            {
                if (halftone) return halftone;
                const int S = 32;
                halftone = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
                var px = new Color32[S * S];
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(S / 2f, S / 2f));
                        px[y * S + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(7f - d) * 255));
                    }
                halftone.SetPixels32(px); halftone.Apply();
                return halftone;
            }
        }

        public static RectTransform Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = .5f;
            if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return (RectTransform)go.transform;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(.5f, .5f);
            return rt;
        }

        public static Image Panel(RectTransform rt, Color c, Color? outline = null, float outlineSize = 5f, bool raycast = false)
        {
            var im = rt.gameObject.AddComponent<Image>(); im.color = c; im.sprite = Round; im.type = Image.Type.Sliced; im.raycastTarget = raycast;
            if (outline.HasValue) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = outline.Value; o.effectDistance = new Vector2(outlineSize, -outlineSize); }
            return im;
        }

        public static Text Label(Transform parent, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleCenter, bool bold = true, float pad = 0)
        {
            var rt = Fill("Text", parent); rt.offsetMin = new Vector2(pad, 0); rt.offsetMax = new Vector2(-pad, 0);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            return t;
        }

        /// Chunky outlined comic text (title / big numbers).
        public static Text Punchy(Text t, Color edge, float size = 4f)
        {
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = edge; o.effectDistance = new Vector2(size, -size);
            var sh = t.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .35f); sh.effectDistance = new Vector2(size * 1.5f, -size * 2f);
            return t;
        }

        public static PunchButton Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, Action onClick, int fontSize = 40, float tilt = 0f)
        {
            var rt = Rect("Btn " + label, parent, anchor, pos, size);
            rt.localRotation = Quaternion.Euler(0, 0, tilt);
            var drop = Rect("Drop", rt, new Vector2(.5f, .5f), new Vector2(6, -9), size); Panel(drop, Shade);
            var faceRt = Rect("Face", rt, new Vector2(.5f, .5f), Vector2.zero, size);
            var img = Panel(faceRt, fill, Ink, 5f, true);
            var txt = Punchy(Label(faceRt, label, fontSize, Color.white), Ink, 3f);
            var b = rt.gameObject.AddComponent<PunchButton>();
            b.Init(img, txt, fill, onClick);
            return b;
        }
    }

    /// Bouncy hover / squash-on-press button: the menu should feel like it slaps.
    public class PunchButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Image face; public Text text; public Color fill; Action click;
        bool hover, down, interactable = true; float scale = 1f, vel, wiggle;
        public bool Interactable { get => interactable; set { interactable = value; Refresh(); } }

        public void Init(Image f, Text t, Color c, Action onClick) { face = f; text = t; fill = c; click = onClick; Refresh(); }
        public void SetLabel(string s) { if (text) text.text = s; }
        public void SetColor(Color c) { fill = c; Refresh(); }
        void Refresh() { if (face) face.color = interactable ? (hover ? Color.Lerp(fill, Color.white, .18f) : fill) : Color.Lerp(fill, Color.gray, .65f); }

        public void OnPointerEnter(PointerEventData e) { hover = true; wiggle = 1f; Refresh(); if (interactable) MenuSfx.Play(MenuSfx.Kind.Hover); }
        public void OnPointerExit(PointerEventData e) { hover = false; down = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { down = true; }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable) { MenuSfx.Play(MenuSfx.Kind.Nope); wiggle = 1.5f; return; }
            scale = 1.25f; MenuSfx.Play(MenuSfx.Kind.Click); click?.Invoke();
        }
        /// Keyboard / code path (Enter key etc.).
        public void Press() { if (interactable) OnPointerClick(null); }

        void Update()
        {
            float goal = !interactable ? 1f : down ? .9f : hover ? 1.08f : 1f;
            // spring toward the goal scale so clicks overshoot and wobble
            float dt = Time.unscaledDeltaTime;
            vel += (goal - scale) * 320f * dt; vel *= Mathf.Exp(-14f * dt);
            scale += vel * dt;
            wiggle = Mathf.MoveTowards(wiggle, 0f, dt * 3f);
            if (!face) return;
            var rt = face.rectTransform;
            rt.localScale = new Vector3(scale, scale, 1f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 30f) * 4f * wiggle);
        }
    }

    /// Tiny synthesized UI sounds so the menu is not silent (no audio assets needed).
    public static class MenuSfx
    {
        public enum Kind { Hover, Click, Nope, Pop, Go }
        static AudioSource src; static AudioClip[] clips;

        public static void Play(Kind k)
        {
            if (!src)
            {
                var go = new GameObject("MenuSfx"); UnityEngine.Object.DontDestroyOnLoad(go);
                src = go.AddComponent<AudioSource>(); src.playOnAwake = false; src.volume = .35f;
                clips = new[] { Tone(900, 1200, .05f, .25f), Tone(520, 980, .09f, .6f), Tone(260, 180, .14f, .5f), Tone(700, 1500, .12f, .5f), Tone(400, 1600, .35f, .6f) };
            }
            src.PlayOneShot(clips[(int)k]);
        }

        static AudioClip Tone(float f0, float f1, float len, float vol)
        {
            int rate = 44100, n = Mathf.CeilToInt(rate * len); var d = new float[n]; float ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n; ph += Mathf.Lerp(f0, f1, t) / rate * Mathf.PI * 2f;
                float env = Mathf.Min(1f, t * 40f) * (1f - t) * (1f - t);
                d[i] = (Mathf.Sin(ph) * .7f + Mathf.Sign(Mathf.Sin(ph)) * .12f) * env * vol;
            }
            var c = AudioClip.Create("menu tone", n, 1, rate, false); c.SetData(d, 0); return c;
        }
    }
}
