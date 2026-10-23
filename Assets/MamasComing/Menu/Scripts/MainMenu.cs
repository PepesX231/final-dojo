using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MamasComing.Menu
{
    /// Title screen: graffiti logo over a stage of dancing fruit kids. CREATE opens a room (pick level + room size),
    /// JOIN takes a room code or LAN address, SOLO opens an offline lobby. All roads lead to the 3D lobby.
    public class MainMenu : MonoBehaviour
    {
        public GameObject[] fruitPrefabs;
        public Material baseMaterial, inkMaterial;
        public Camera cam;

        enum Page { Main, Create, Join, Busy }
        Page page;
        RectTransform root, mainPage, createPage, joinPage, busyPage, titleRt;
        readonly List<(RectTransform rt, float phase, float tilt)> letters = new List<(RectTransform, float, float)>();
        readonly List<AvatarLook> dancers = new List<AvatarLook>();
        Text statusText, busyText, playersText, noteText;
        InputField codeField;
        PunchButton[] levelCards = new PunchButton[2];
        PunchButton onlineBtn, lanBtn;
        bool useOnline = true; int pendingMode; float busySince;
        Transform stage;

        void Start()
        {
            Loc.Load();
            AvatarLook.SetBaseMaterial(baseMaterial); AvatarLook.InkMaterial = inkMaterial;
            if (!cam) cam = Camera.main;
            // back from a lobby / level: close whatever room we were in
            if (NetSession.I && NetSession.I.State != NetSession.Mode.Offline) { NetSession.I.enabled = true; NetSession.I.Leave(); }
            MenuFlow.MaxPlayers = Mathf.Clamp(MenuFlow.MaxPlayers, MenuFlow.MinPlayers, MenuFlow.MaxAllowed);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            BuildStage();
            BuildUi();
            Show(Page.Main);
            if (!string.IsNullOrEmpty(MenuFlow.StatusNote)) { noteText.text = MenuFlow.StatusNote; MenuFlow.StatusNote = ""; }
        }

        // ================================================================== 3D backdrop
        void BuildStage()
        {
            stage = new GameObject("Menu stage").transform;
            // graffiti wall + floor
            AvatarLook.Prim(PrimitiveType.Cube, stage, new Vector3(0, 3f, 3.2f), new Vector3(18f, 8f, .4f), new Color(.42f, .78f, 1f));   // bright sky-blue mural wall
            AvatarLook.Prim(PrimitiveType.Cube, stage, new Vector3(0, -.25f, 0), new Vector3(18f, .5f, 9f), new Color(1f, .86f, .62f));
            Color[] spray = { MenuUi.Lemon, MenuUi.Teal, MenuUi.Pink, new Color(.98f, .96f, .9f), MenuUi.Grape };
            var rng = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                float x = (float)rng.NextDouble() * 16f - 8f, y = (float)rng.NextDouble() * 6f + .4f, s = .4f + (float)rng.NextDouble() * 1.8f;
                var blob = AvatarLook.Prim(i % 3 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere, stage, new Vector3(x, y, 2.95f), new Vector3(s * 1.6f, s, .05f), spray[i % spray.Length], false, true);
                blob.transform.localRotation = Quaternion.Euler(0, 0, (float)rng.NextDouble() * 80f - 40f);
            }
            // round stage the kids dance on
            AvatarLook.Prim(PrimitiveType.Cylinder, stage, new Vector3(1.4f, .1f, 0), new Vector3(7f, .2f, 4.2f), MenuUi.Lemon);
            AvatarLook.Prim(PrimitiveType.Cylinder, stage, new Vector3(1.4f, .03f, 0), new Vector3(7.6f, .06f, 4.8f), MenuUi.Ink);
            int n = KidLook.Count;
            for (int i = 0; i < n; i++)
            {
                var spot = new GameObject("Dancer " + i).transform; spot.SetParent(stage, false);
                float t = n == 1 ? .5f : i / (float)(n - 1);
                spot.localPosition = new Vector3(Mathf.Lerp(-1.2f, 4.0f, t), .2f, Mathf.Sin(t * Mathf.PI) * -.8f + .2f);
                spot.localRotation = Quaternion.Euler(0, 180f + Mathf.Lerp(18f, -18f, t), 0);
                var look = AvatarLook.Create(spot, fruitPrefabs, baseMaterial, new Cosmetic { fruit = (byte)i, hat = (byte)(i + 1), face = (byte)(i % MenuFlow.FaceEn.Length), color = (byte)(i * 3 % MenuFlow.Colors.Length) });
                dancers.Add(look);
            }
            if (cam) { cam.transform.position = new Vector3(-.6f, 1.7f, -6.4f); cam.transform.LookAt(new Vector3(-.2f, 1.1f, 0)); }
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < dancers.Count; i++)
            {
                var d = dancers[i]; if (!d) continue;
                float hop = Mathf.Max(0f, Mathf.Sin(t * 5.2f + i * 1.3f));
                d.transform.localPosition = Vector3.up * hop * .28f;
                d.Tick(hop > .2f ? 2.5f : 0f, i == Mathf.FloorToInt(t / 2.2f) % dancers.Count, hop > .3f);
            }
            if (cam) cam.transform.position = new Vector3(-.6f + Mathf.Sin(t * .25f) * .5f, 1.7f + Mathf.Sin(t * .4f) * .08f, -6.4f);
            for (int i = 0; i < letters.Count; i++)
            {
                var (rt, ph, tilt) = letters[i];
                float b = Mathf.Max(0f, Mathf.Sin(t * 3f - ph));
                rt.localScale = Vector3.one * (1f + b * .12f);
                rt.localRotation = Quaternion.Euler(0, 0, tilt + Mathf.Sin(t * 2f + ph) * 2.5f);
            }
            if (page == Page.Busy) WatchConnection();
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && page != Page.Main) Back();
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && page == Page.Join) DoJoin();
        }

        // ================================================================== UI
        void BuildUi()
        {
            root = MenuUi.Canvas("Menu UI", 30);
            var halftone = MenuUi.Fill("Halftone", root);
            var ht = halftone.gameObject.AddComponent<RawImage>(); ht.texture = MenuUi.Halftone; ht.color = new Color(1f, 1f, 1f, .16f); ht.raycastTarget = false;
            ht.uvRect = new Rect(0, 0, 1920f / 24f, 1080f / 24f);
            var band = MenuUi.Rect("Ink band", root, new Vector2(0, .5f), new Vector2(-40, 0), new Vector2(1040, 1500), new Vector2(0, .5f));
            band.localRotation = Quaternion.Euler(0, 0, -6f);
            MenuUi.Panel(band, new Color(.45f, .3f, .9f, .82f));   // bold grape band behind the buttons

            BuildTitle();
            mainPage = MenuUi.Rect("Main", root, new Vector2(0, .5f), new Vector2(110, -150), new Vector2(720, 560), new Vector2(0, .5f));
            MenuUi.Button(mainPage, MenuFlow.Txt("CREATE ROOM", "สร้างห้อง"), new Vector2(0, 1), new Vector2(0, 0), new Vector2(560, 112), MenuUi.Lemon, () => Show(Page.Create), 54, 2.5f).text.color = MenuUi.Ink;
            MenuUi.Button(mainPage, MenuFlow.Txt("JOIN ROOM", "เข้าห้อง"), new Vector2(0, 1), new Vector2(40, -136), new Vector2(520, 104), MenuUi.Teal, () => Show(Page.Join), 50, -2f);
            MenuUi.Button(mainPage, MenuFlow.Txt("SOLO WARM-UP", "ซ้อมคนเดียว"), new Vector2(0, 1), new Vector2(10, -262), new Vector2(440, 84), MenuUi.Pink, () => { MenuFlow.MaxPlayers = MenuFlow.MinPlayers; SceneManager.LoadScene(MenuFlow.LobbyScene); }, 38, 1.5f);
            MenuUi.Button(mainPage, MenuFlow.Txt("QUIT", "ออกเกม"), new Vector2(0, 1), new Vector2(30, -370), new Vector2(240, 70), new Color(.4f, .38f, .36f), Quit, 32, -2f);
            var note = MenuUi.Rect("Note", mainPage, new Vector2(0, 0), new Vector2(0, -10), new Vector2(720, 60));
            noteText = MenuUi.Label(note, "", 26, MenuUi.Pink, TextAnchor.MiddleLeft);

            BuildCreate();
            BuildJoin();

            busyPage = MenuUi.Rect("Busy", root, new Vector2(0, .5f), new Vector2(110, -150), new Vector2(720, 400), new Vector2(0, .5f));
            var bb = MenuUi.Rect("box", busyPage, new Vector2(0, 1), Vector2.zero, new Vector2(700, 220));
            MenuUi.Panel(bb, MenuUi.Paper, MenuUi.Ink, 6f);
            busyText = MenuUi.Label(bb, "", 36, MenuUi.Ink, TextAnchor.MiddleCenter, true, 30);
            MenuUi.Button(busyPage, MenuFlow.Txt("CANCEL", "ยกเลิก"), new Vector2(0, 1), new Vector2(0, -250), new Vector2(260, 76), new Color(.4f, .38f, .36f), Back, 32);

            // language toggle
            MenuUi.Button(root, "TH / EN", new Vector2(1, 1), new Vector2(-30, -26), new Vector2(190, 66), MenuUi.Grape, () => { Loc.SetThai(!Loc.Thai); SceneManager.LoadScene(MenuFlow.MenuScene); }, 30, 2f);
        }

        void BuildTitle()
        {
            titleRt = MenuUi.Rect("Title", root, new Vector2(0, 1), new Vector2(90, -40), new Vector2(900, 360));
            Word("MAMA'S", new Vector2(10, -20), 150, MenuUi.Lemon, 0);
            Word("COMING!", new Vector2(70, -170), 160, MenuUi.Pink, 6);
            var tag = MenuUi.Rect("Tagline", titleRt, new Vector2(0, 1), new Vector2(120, -340), new Vector2(620, 54));
            tag.localRotation = Quaternion.Euler(0, 0, -3f);
            MenuUi.Panel(tag, MenuUi.Teal, MenuUi.Ink, 4f);
            MenuUi.Label(tag, MenuFlow.Txt("FRUIT KIDS. ONE WRECKED HOUSE. NO TIME.", "เด็กผลไม้ บ้านพัง เวลาไม่พอ!"), 26, MenuUi.Ink);
        }

        void Word(string w, Vector2 pos, int size, Color c, int seed)
        {
            float x = pos.x; var rng = new System.Random(seed + 11);
            for (int i = 0; i < w.Length; i++)
            {
                float width = w[i] == '\'' || w[i] == '!' ? size * .32f : size * .66f;
                var rt = MenuUi.Rect("L" + i, titleRt, new Vector2(0, 1), new Vector2(x + width / 2f, pos.y - size / 2f), new Vector2(width + 20, size + 20), new Vector2(.5f, .5f));
                float tilt = (float)rng.NextDouble() * 14f - 7f;
                rt.localRotation = Quaternion.Euler(0, 0, tilt);
                // fat ink back plate then the colour letter: graffiti sticker read at a glance
                var back = MenuUi.Label(rt, w[i].ToString(), size, MenuUi.Ink);
                back.rectTransform.anchoredPosition = new Vector2(7, -9);
                var o = back.gameObject.AddComponent<Outline>(); o.effectColor = MenuUi.Ink; o.effectDistance = new Vector2(9, -9);
                var front = MenuUi.Label(rt, w[i].ToString(), size, c);
                var o2 = front.gameObject.AddComponent<Outline>(); o2.effectColor = MenuUi.Ink; o2.effectDistance = new Vector2(5, -5);
                var hl = front.gameObject.AddComponent<Shadow>(); hl.effectColor = new Color(1f, 1f, 1f, .55f); hl.effectDistance = new Vector2(-3, 3);
                letters.Add((rt, i * .45f + seed, tilt));
                x += width;
            }
        }

        void BuildCreate()
        {
            createPage = MenuUi.Rect("Create", root, new Vector2(0, .5f), new Vector2(100, -170), new Vector2(860, 620), new Vector2(0, .5f));
            var head = MenuUi.Rect("head", createPage, new Vector2(0, 1), new Vector2(0, 0), new Vector2(700, 70));
            MenuUi.Punchy(MenuUi.Label(head, MenuFlow.Txt("PICK A LEVEL", "เลือกด่าน"), 52, MenuUi.Lemon, TextAnchor.MiddleLeft), MenuUi.Ink, 4f);
            for (int i = 0; i < 2; i++)
            {
                int lv = i;
                var card = MenuUi.Button(createPage, MenuFlow.LevelName(i), new Vector2(0, 1), new Vector2(i * 390, -90), new Vector2(370, 200), MenuFlow.Levels[i].color, () => { MenuFlow.Level = lv; RefreshCreate(); }, 40, i == 0 ? -2f : 2f);
                card.text.alignment = TextAnchor.UpperCenter; card.text.rectTransform.offsetMax = new Vector2(0, -22);
                var blurb = MenuUi.Label(card.face.transform, MenuFlow.LevelBlurb(i), 22, Color.white, TextAnchor.LowerCenter, false, 18);
                blurb.rectTransform.offsetMin = new Vector2(18, 18);
                levelCards[i] = card;
            }
            var ph = MenuUi.Rect("players", createPage, new Vector2(0, 1), new Vector2(0, -320), new Vector2(760, 80));
            MenuUi.Panel(ph, MenuUi.Paper, MenuUi.Ink, 5f);
            playersText = MenuUi.Label(ph, "", 38, MenuUi.Ink);
            MenuUi.Button(ph, "-", new Vector2(0, .5f), new Vector2(10, 0), new Vector2(80, 64), MenuUi.Teal, () => { MenuFlow.MaxPlayers = Mathf.Max(MenuFlow.MinPlayers, MenuFlow.MaxPlayers - 1); RefreshCreate(); }, 44);
            MenuUi.Button(ph, "+", new Vector2(1, .5f), new Vector2(-10, 0), new Vector2(80, 64), MenuUi.Teal, () => { MenuFlow.MaxPlayers = Mathf.Min(MenuFlow.MaxAllowed, MenuFlow.MaxPlayers + 1); RefreshCreate(); }, 44);
            onlineBtn = MenuUi.Button(createPage, MenuFlow.Txt("ONLINE (CODE)", "ออนไลน์ (รหัส)"), new Vector2(0, 1), new Vector2(0, -420), new Vector2(370, 70), MenuUi.Grape, () => { useOnline = true; RefreshCreate(); }, 30);
            lanBtn = MenuUi.Button(createPage, MenuFlow.Txt("SAME WI-FI (LAN)", "วงแลนเดียวกัน"), new Vector2(0, 1), new Vector2(390, -420), new Vector2(370, 70), MenuUi.Grape, () => { useOnline = false; RefreshCreate(); }, 30);
            MenuUi.Button(createPage, MenuFlow.Txt("OPEN ROOM!", "เปิดห้องเลย!"), new Vector2(0, 1), new Vector2(170, -520), new Vector2(520, 100), MenuUi.Pink, DoCreate, 50, -1.5f);
            MenuUi.Button(createPage, MenuFlow.Txt("BACK", "กลับ"), new Vector2(0, 1), new Vector2(-10, -536), new Vector2(160, 70), new Color(.4f, .38f, .36f), Back, 30);
            RefreshCreate();
        }

        void RefreshCreate()
        {
            for (int i = 0; i < levelCards.Length; i++)
            {
                bool on = MenuFlow.Level == i;
                levelCards[i].SetColor(on ? MenuFlow.Levels[i].color : Color.Lerp(MenuFlow.Levels[i].color, new Color(.25f, .23f, .22f), .7f));
                levelCards[i].transform.localScale = Vector3.one * (on ? 1.04f : .94f);
                levelCards[i].SetLabel((on ? "> " : "") + MenuFlow.LevelName(i) + (on ? " <" : ""));
            }
            playersText.text = MenuFlow.Txt("ROOM SIZE: ", "จำนวนผู้เล่น: ") + MenuFlow.MaxPlayers + MenuFlow.Txt(" PLAYERS", " คน");
            onlineBtn.SetColor(useOnline ? MenuUi.Grape : new Color(.35f, .32f, .38f));
            lanBtn.SetColor(!useOnline ? MenuUi.Grape : new Color(.35f, .32f, .38f));
        }

        void BuildJoin()
        {
            joinPage = MenuUi.Rect("Join", root, new Vector2(0, .5f), new Vector2(110, -150), new Vector2(760, 520), new Vector2(0, .5f));
            var head = MenuUi.Rect("head", joinPage, new Vector2(0, 1), new Vector2(0, 0), new Vector2(700, 70));
            MenuUi.Punchy(MenuUi.Label(head, MenuFlow.Txt("GOT A CODE?", "มีรหัสห้องไหม?"), 52, MenuUi.Teal, TextAnchor.MiddleLeft), MenuUi.Ink, 4f);
            var box = MenuUi.Rect("Code box", joinPage, new Vector2(0, 1), new Vector2(0, -100), new Vector2(640, 110));
            MenuUi.Panel(box, MenuUi.Paper, MenuUi.Ink, 6f, true);
            var txtRt = MenuUi.Fill("Text", box); txtRt.offsetMin = new Vector2(24, 0); txtRt.offsetMax = new Vector2(-24, 0);
            var txt = txtRt.gameObject.AddComponent<Text>(); txt.font = MenuUi.Font; txt.fontSize = 56; txt.color = MenuUi.Ink; txt.alignment = TextAnchor.MiddleCenter; txt.fontStyle = FontStyle.Bold; txt.supportRichText = false;
            var phRt = MenuUi.Fill("Placeholder", box);
            var ph = phRt.gameObject.AddComponent<Text>(); ph.font = MenuUi.Font; ph.fontSize = 34; ph.color = new Color(.45f, .4f, .4f, .7f); ph.alignment = TextAnchor.MiddleCenter;
            ph.text = MenuFlow.Txt("ROOM CODE or 192.168.x.x", "รหัสห้อง หรือ 192.168.x.x");
            codeField = box.gameObject.AddComponent<InputField>(); codeField.textComponent = txt; codeField.placeholder = ph; codeField.characterLimit = 40;
            codeField.text = PlayerPrefs.GetString("mc_last_code", "");
            MenuUi.Button(joinPage, MenuFlow.Txt("JUMP IN!", "เข้าเลย!"), new Vector2(0, 1), new Vector2(120, -250), new Vector2(460, 100), MenuUi.Teal, DoJoin, 50, 1.5f);
            MenuUi.Button(joinPage, MenuFlow.Txt("BACK", "กลับ"), new Vector2(0, 1), new Vector2(-10, -266), new Vector2(160, 70), new Color(.4f, .38f, .36f), Back, 30);
            var st = MenuUi.Rect("status", joinPage, new Vector2(0, 1), new Vector2(0, -380), new Vector2(740, 80));
            statusText = MenuUi.Label(st, MenuFlow.Txt("Ask the host for the code on their lobby screen.", "ขอรหัสจากเพื่อนที่สร้างห้อง (โชว์อยู่บนจอล็อบบี้)"), 26, Color.white, TextAnchor.UpperLeft, false);
        }

        void Show(Page p)
        {
            page = p;
            mainPage.gameObject.SetActive(p == Page.Main);
            createPage.gameObject.SetActive(p == Page.Create);
            joinPage.gameObject.SetActive(p == Page.Join);
            busyPage.gameObject.SetActive(p == Page.Busy);
            titleRt.gameObject.SetActive(p == Page.Main || p == Page.Busy);
            if (p == Page.Join && codeField) codeField.ActivateInputField();
        }

        void Back()
        {
            if (page == Page.Busy && NetSession.I && NetSession.I.State != NetSession.Mode.Offline) NetSession.I.Leave();
            Show(Page.Main);
        }

        // ================================================================== networking
        void DoCreate()
        {
            NetSession.Ensure();
            pendingMode = 1; busySince = Time.unscaledTime;
            busyText.text = MenuFlow.Txt("Opening the room...", "กำลังเปิดห้อง...");
            Show(Page.Busy);
            if (useOnline) NetSession.I.HostOnline(); else NetSession.I.HostLan();
        }

        void DoJoin()
        {
            string code = codeField ? codeField.text.Trim() : "";
            if (code.Length == 0) { statusText.text = MenuFlow.Txt("Type the room code first!", "พิมพ์รหัสห้องก่อน!"); MenuSfx.Play(MenuSfx.Kind.Nope); return; }
            PlayerPrefs.SetString("mc_last_code", code);
            NetSession.Ensure();
            pendingMode = 2; busySince = Time.unscaledTime;
            busyText.text = MenuFlow.Txt("Knocking on the door...", "กำลังเคาะประตูห้อง...");
            Show(Page.Busy);
            NetSession.I.Join(code);
        }

        void WatchConnection()
        {
            var ns = NetSession.I; if (!ns) return;
            if (!string.IsNullOrEmpty(ns.Status)) busyText.text = ns.Status;
            var nm = NetworkManager.Singleton;
            if (pendingMode == 1 && ns.State == NetSession.Mode.Host) { SceneManager.LoadScene(MenuFlow.LobbyScene); pendingMode = 0; return; }
            if (pendingMode == 2 && ns.State == NetSession.Mode.Client && nm && nm.IsConnectedClient) { SceneManager.LoadScene(MenuFlow.LobbyScene); pendingMode = 0; return; }
            bool failed = ns.State == NetSession.Mode.Offline && Time.unscaledTime - busySince > .5f;
            bool slow = Time.unscaledTime - busySince > 15f;
            if (failed || slow)
            {
                if (slow) ns.Leave();
                string why = string.IsNullOrEmpty(ns.Status) || ns.Status.Contains("solo") || ns.Status.Contains("คนเดียว") ? MenuFlow.Txt("Could not connect. Check the code and try again.", "เชื่อมต่อไม่ได้ ลองเช็กรหัสแล้วลองใหม่") : ns.Status;
                bool joining = pendingMode == 2;
                pendingMode = 0; MenuSfx.Play(MenuSfx.Kind.Nope);
                if (statusText) statusText.text = why;
                noteText.text = why;
                Show(joining ? Page.Join : Page.Main);
            }
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
