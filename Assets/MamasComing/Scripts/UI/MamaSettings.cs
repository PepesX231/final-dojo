using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MamasComing
{
    /// Gear button + settings panel (language, volumes) and the live translation pass over every UI Text.
    public class MamaSettings : MonoBehaviour
    {
        public static MamaSettings I;
        public static bool Open => I && I.panel && I.panel.activeSelf;
        public static float MasterVol = 1f, MusicVol = 0.8f, SfxVol = 0.8f;

        GameObject panel; RectTransform gear; Font baseFont, thaiFont;
        Text title, langLbl, musicLbl, sfxLbl, masterLbl, closeLbl, hint; Image btnEN, btnTH; Image[] bars = new Image[3];
        float closedAt = -10f; bool prevLock;
        readonly Dictionary<Text, string[]> seen = new Dictionary<Text, string[]>();
        Text[] all = new Text[0]; float rescan;
        static readonly Color Paper = new Color(1f, 0.96f, 0.86f), Edge = new Color(0.45f, 0.3f, 0.2f), Ink = new Color(0.25f, 0.15f, 0.1f), Pink = new Color(0.95f, 0.45f, 0.55f), Teal = new Color(0.25f, 0.72f, 0.7f);

        public static void Ensure() { if (!I) new GameObject("MamaSettings").AddComponent<MamaSettings>(); }

        /// Title "press any key" must ignore clicks that were meant for the gear / panel.
        public static bool BlockStart
        {
            get
            {
                if (!I) return false;
                if (Open || Time.unscaledTime - I.closedAt < 0.35f) return true;
                var m = Mouse.current;
                return m != null && I.gear && RectTransformUtility.RectangleContainsScreenPoint(I.gear, m.position.ReadValue(), null);
            }
        }

        void Awake()
        {
            I = this;
            Loc.Load();
            MasterVol = PlayerPrefs.GetFloat("mama_master", 1f); MusicVol = PlayerPrefs.GetFloat("mama_music", 0.8f); SfxVol = PlayerPrefs.GetFloat("mama_sfx", 0.8f);
            baseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            thaiFont = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Arial Unicode MS" }, 32);
            if (!FindFirstObjectByType<EventSystem>())
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                DontDestroyOnLoad(es);
            }
            Build();
            Loc.Changed += () => { foreach (var kv in seen) if (kv.Key) kv.Key.text = kv.Value[0]; RefreshLabels(); };
            ApplyAudio();
        }

        // ------------------------------------------------------------------ UI
        RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 piv, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(n, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(p, false);
            r.anchorMin = amin; r.anchorMax = amax; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        Image Im(RectTransform r, Color c, bool outline = false)
        {
            var i = r.gameObject.AddComponent<Image>(); i.color = c;
            if (outline) { var o = r.gameObject.AddComponent<Outline>(); o.effectColor = Edge; o.effectDistance = new Vector2(3, -3); }
            return i;
        }
        Text Tx(RectTransform r, string s, int size, Color c)
        {
            var t = r.gameObject.AddComponent<Text>(); t.font = baseFont; t.text = s; t.fontSize = size; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; return t;
        }
        Button Btn(Transform p, Vector2 pos, Vector2 size, Color c, string label, System.Action a, out Text lbl, out Image img)
        {
            var r = R("Btn", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            img = Im(r, c, true); var b = r.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { MamaAudio.Play("click", null, 0.7f); a(); });
            lbl = Tx(R("L", r, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), label, 30, Ink); return b;
        }

        void Build()
        {
            var cgo = new GameObject("SettingsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 200;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            var root = cgo.transform;

            gear = R("Gear", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(74, 74));
            Im(gear, Paper, true); var gb = gear.gameObject.AddComponent<Button>(); gb.onClick.AddListener(() => { MamaAudio.Play("click", null, 0.7f); Toggle(); });
            Tx(R("G", gear, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "⚙", 48, Edge).font = thaiFont ? thaiFont : baseFont;

            var dim = R("Settings", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Im(dim, new Color(0, 0, 0, 0.45f)); panel = dim.gameObject;
            var p = R("Panel", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 900));
            p.localRotation = Quaternion.Euler(0, 0, -0.6f); Im(p, Paper, true);
            var band = R("Band", p, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(-60, 86)); Im(band, Pink);
            title = Tx(R("T", band, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "", 46, Color.white);

            langLbl = Tx(R("LangL", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(600, 40)), "", 30, Ink);
            Btn(p, new Vector2(-130, 245), new Vector2(230, 64), Paper, "English", () => Loc.SetThai(false), out _, out btnEN);
            Btn(p, new Vector2(130, 245), new Vector2(230, 64), Paper, "ภาษาไทย", () => Loc.SetThai(true), out var thLbl, out btnTH);
            thLbl.font = thaiFont ? thaiFont : baseFont;

            masterLbl = VolRow(p, 160, 0); musicLbl = VolRow(p, 95, 1); sfxLbl = VolRow(p, 30, 2);

            // ---- co-op
            coopLbl = Tx(R("CoopL", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(680, 40)), "", 30, Ink);
            Btn(p, new Vector2(-180, -95), new Vector2(220, 58), Paper, "", () => { NetSession.Ensure(); NetSession.I.HostOnline(); }, out hostLbl, out _);
            Btn(p, new Vector2(60, -95), new Vector2(220, 58), Paper, "", () => { NetSession.Ensure(); NetSession.I.HostLan(); }, out lanLbl, out _);
            Btn(p, new Vector2(270, -95), new Vector2(150, 58), Paper, "", () => { if (NetSession.I) NetSession.I.Leave(); }, out leaveLbl, out _);
            var inRT = R("Code", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-70, -165), new Vector2(420, 58));
            Im(inRT, Color.white, true);
            var itx = Tx(R("Text", inRT, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, -8)), "", 28, Ink); itx.alignment = TextAnchor.MiddleLeft; itx.supportRichText = false;
            var iph = Tx(R("Ph", inRT, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20, -8)), "", 24, new Color(0.5f, 0.45f, 0.4f)); iph.alignment = TextAnchor.MiddleLeft; iph.fontStyle = FontStyle.Italic;
            codeField = inRT.gameObject.AddComponent<InputField>(); codeField.textComponent = itx; codeField.placeholder = iph; codeField.characterLimit = 40;
            placeholder = iph;
            Btn(p, new Vector2(230, -165), new Vector2(180, 58), Teal, "", () => { NetSession.Ensure(); NetSession.I.Join(codeField.text); }, out joinLbl, out _);
            joinLbl.color = Color.white;
            netStatus = Tx(R("NetStatus", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -225), new Vector2(700, 40)), "", 22, Edge);
            netStatus.horizontalOverflow = HorizontalWrapMode.Wrap;

            Btn(p, new Vector2(0, -320), new Vector2(260, 70), Teal, "", Toggle, out closeLbl, out _);
            closeLbl.color = Color.white;
            hint = Tx(R("Hint", p, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(600, 30)), "", 20, Edge);
            panel.SetActive(false);
            RefreshLabels();
        }

        Text VolRow(RectTransform p, float y, int idx)
        {
            var l = Tx(R("VolL", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-300, y), new Vector2(200, 40)), "", 28, Ink);
            l.alignment = TextAnchor.MiddleLeft;
            Btn(p, new Vector2(-50, y), new Vector2(56, 56), Paper, "-", () => Step(idx, -0.1f), out _, out _);
            var track = R("Track", p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-12, y), new Vector2(230, 26));
            Im(track, new Color(0.85f, 0.78f, 0.66f));
            var fill = R("Fill", track, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            bars[idx] = Im(fill, Teal); bars[idx].type = Image.Type.Filled; bars[idx].fillMethod = Image.FillMethod.Horizontal;
            bars[idx].sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            Btn(p, new Vector2(262, y), new Vector2(56, 56), Paper, "+", () => Step(idx, 0.1f), out _, out _);
            return l;
        }

        void Step(int idx, float d)
        {
            if (idx == 0) MasterVol = Mathf.Clamp01(Mathf.Round((MasterVol + d) * 10f) / 10f);
            if (idx == 1) MusicVol = Mathf.Clamp01(Mathf.Round((MusicVol + d) * 10f) / 10f);
            if (idx == 2) SfxVol = Mathf.Clamp01(Mathf.Round((SfxVol + d) * 10f) / 10f);
            PlayerPrefs.SetFloat("mama_master", MasterVol); PlayerPrefs.SetFloat("mama_music", MusicVol); PlayerPrefs.SetFloat("mama_sfx", SfxVol); PlayerPrefs.Save();
            ApplyAudio(); if (idx == 2) MamaAudio.Play("pop", null, 0.8f);
        }

        void ApplyAudio()
        {
            AudioListener.volume = MasterVol;
            bars[0].fillAmount = MasterVol; bars[1].fillAmount = MusicVol; bars[2].fillAmount = SfxVol;
        }

        Text coopLbl, hostLbl, lanLbl, leaveLbl, joinLbl, netStatus, placeholder; InputField codeField;

        void RefreshLabels()
        {
            bool th = Loc.Thai;
            coopLbl.text = th ? "เล่นกับเพื่อน (2-3 คน)" : "CO-OP  (2-3 players)";
            hostLbl.text = th ? "สร้างห้องออนไลน์" : "Host online";
            lanLbl.text = th ? "สร้างห้อง LAN" : "Host LAN";
            leaveLbl.text = th ? "ออก" : "Leave";
            joinLbl.text = th ? "เข้าร่วม" : "Join";
            placeholder.text = th ? "รหัสห้อง หรือ IP ของเพื่อน" : "Room code or friend's IP";
            foreach (var t in new[] { coopLbl, hostLbl, lanLbl, leaveLbl, joinLbl, netStatus, placeholder, codeField.textComponent }) t.font = th ? thaiFont : baseFont;
            title.text = th ? "ตั้งค่า" : "SETTINGS";
            langLbl.text = th ? "ภาษา" : "Language";
            masterLbl.text = th ? "เสียงรวม" : "Master";
            musicLbl.text = th ? "เพลง" : "Music";
            sfxLbl.text = th ? "เสียงเอฟเฟกต์" : "Sounds";
            closeLbl.text = th ? "กลับไปเล่น" : "Resume";
            hint.text = th ? "กด ESC เพื่อเปิด/ปิดหน้านี้" : "Press ESC to open / close";
            btnEN.color = th ? Paper : Color.Lerp(Paper, Teal, 0.55f);
            btnTH.color = th ? Color.Lerp(Paper, Teal, 0.55f) : Paper;
            foreach (var t in new[] { title, langLbl, masterLbl, musicLbl, sfxLbl, closeLbl, hint }) t.font = th ? thaiFont : baseFont;
        }

        public static void Toggle()
        {
            Ensure(); var s = I;
            bool open = !s.panel.activeSelf;
            if (open)
            {
                s.prevLock = Cursor.lockState == CursorLockMode.Locked;
                Time.timeScale = 0f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            }
            else
            {
                Time.timeScale = 1f; s.closedAt = Time.unscaledTime;
                if (s.prevLock) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            }
            s.panel.SetActive(open);
        }

        // ------------------------------------------------------------------ translation pass
        void LateUpdate()
        {
            if (gear) gear.gameObject.SetActive(!Open && Cursor.lockState != CursorLockMode.Locked);
            if (Open && netStatus) netStatus.text = NetSession.I ? NetSession.I.Status : (Loc.Thai ? "เล่นคนเดียว - เพื่อนกดเข้าร่วมด้วยรหัสห้องได้" : "Solo - host a room and give friends the code");
            rescan -= Time.unscaledDeltaTime;
            if (rescan <= 0f) { rescan = 1f; all = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None); }
            bool th = Loc.Thai;
            foreach (var t in all)
            {
                if (!t || t.transform.IsChildOf(transform)) continue;
                if (!seen.TryGetValue(t, out var st)) { st = new[] { t.text, null, null }; seen[t] = st; st[2] = t.font ? t.font.name : ""; st[1] = "\u0001"; }
                if (t.text != st[1]) { st[0] = t.text; st[1] = Loc.T(st[0]); if (t.text != st[1]) t.text = st[1]; }
                var want = th ? thaiFont : baseFont;
                if (want && t.font != want && t.text != "⚙") t.font = want;
            }
        }

        void OnDestroy() { if (I == this) { I = null; Time.timeScale = 1f; } }
    }
}
