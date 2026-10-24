using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MamasComing
{
    /// Illustrated instructions use captures of the authored house and its actual props.
    public class MamaHowToPlay : MonoBehaviour
    {
        static MamaHowToPlay instance;
        public static MamaHowToPlay I => instance;
        static bool shownOnce;
        bool startOnClose = true;
        RectTransform helpButton;
        public static bool Open => instance && instance.panel && instance.panel.activeSelf;
        public static bool BlockStart => Open || instance && Time.unscaledTime - instance.closedAt < .3f;
        GameManager gm;
        GameObject panel;
        Font english, thai;
        Text heading, subtitle, keys, solo, startLabel, settingsLabel, helpLabel;
        readonly Text[] titles = new Text[3], bodies = new Text[3];
        bool? language;
        bool pausedHere, cursorLocked;
        float closedAt = -10f;
        float savedScale = 1f;
        static string T(string en, string th) => Loc.Thai ? th : en;

        void Awake()
        {
            instance = this; gm = GetComponent<GameManager>();
            english = MamaTheme.Body;
            thai = MamaTheme.Body;
            var canvas = new GameObject("How to play", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().sortingOrder = 180;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var help = Box("Help", canvas.transform, new Vector2(1, 1), new Vector2(-225, -28), new Vector2(180, 60), MamaUI.Paper);
            help.pivot = Vector2.one; helpButton = help;
            helpLabel = Label(help, "", 24, Vector2.zero, new Vector2(172, 54), TextAnchor.MiddleCenter);
            help.gameObject.AddComponent<Button>().onClick.AddListener(Show);
            var dim = Box("Instructions", canvas.transform, Vector2.one * .5f, Vector2.zero, new Vector2(1920, 1080), new Color(.06f, .1f, .12f, .78f));
            panel = dim.gameObject;
            var paper = Box("Paper", dim, Vector2.one * .5f, Vector2.zero, new Vector2(1660, 900), MamaUI.Paper);
            heading = Label(paper, "", 48, new Vector2(0, 385), new Vector2(1550, 70), TextAnchor.MiddleCenter);
            subtitle = Label(paper, "", 27, new Vector2(0, 323), new Vector2(1500, 50), TextAnchor.MiddleCenter);
            string[] images = { "stop", "pickup", "home" };
            for (int i = 0; i < 3; i++)
            {
                var card = Box("Step " + (i + 1), paper, Vector2.one * .5f, new Vector2((i - 1) * 520, 12), new Vector2(492, 550), new Color(1f, .98f, .91f));
                var picture = new GameObject("Game capture", typeof(RectTransform), typeof(RawImage));
                var pr = (RectTransform)picture.transform; pr.SetParent(card, false); pr.anchorMin = pr.anchorMax = Vector2.one * .5f;
                pr.anchoredPosition = new Vector2(0, 129); pr.sizeDelta = new Vector2(460, 259);
                var raw = picture.GetComponent<RawImage>(); raw.texture = Resources.Load<Texture2D>("HowToPlay/" + images[i]); raw.raycastTarget = false;
                titles[i] = Label(card, "", 30, new Vector2(0, -40), new Vector2(450, 70), TextAnchor.MiddleLeft);
                bodies[i] = Label(card, "", 27, new Vector2(0, -163), new Vector2(450, 170), TextAnchor.UpperLeft);
            }
            solo = Label(paper, "", 25, new Vector2(0, -307), new Vector2(1540, 48), TextAnchor.MiddleCenter);
            keys = Label(paper, "", 23, new Vector2(0, -358), new Vector2(1550, 43), TextAnchor.MiddleCenter);
            var settings = Box("Co-op and sound", paper, Vector2.one * .5f, new Vector2(-315, -414), new Vector2(530, 60), MamaUI.PaperDark);
            settingsLabel = Label(settings, "", 26, Vector2.zero, new Vector2(510, 55), TextAnchor.MiddleCenter);
            settings.gameObject.AddComponent<Button>().onClick.AddListener(MamaSettings.Toggle);
            var start = Box("Ready", paper, Vector2.one * .5f, new Vector2(315, -414), new Vector2(530, 60), MamaUI.Teal);
            startLabel = Label(start, "", 28, Vector2.zero, new Vector2(510, 55), TextAnchor.MiddleCenter);
            start.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            panel.SetActive(false); Refresh();
            Theme(paper, settings, start);
        }

        void Theme(RectTransform paper, RectTransform settings, RectTransform start)
        {
            if (!MamaTheme.Ready) return;
            MamaTheme.Skin(paper.GetComponent<Image>(), MamaTheme.PaperS, Color.white, 46f);
            MamaTheme.Tape(paper, new Vector2(0.04f, 1f), new Vector2(10, -34), 32f, 150f);
            MamaTheme.Tape(paper, new Vector2(0.96f, 1f), new Vector2(-10, -34), -28f, 150f);
            for (int i = 0; i < 3; i++)
            {
                var card = (RectTransform)paper.Find("Step " + (i + 1));
                MamaTheme.Skin(card.GetComponent<Image>(), MamaTheme.PaintS, new Color(1f, .98f, .93f), 24f);
                card.localRotation = Quaternion.Euler(0, 0, (i - 1) * -1.2f);
                MamaTheme.Tape(card, new Vector2(.5f, 1f), new Vector2(0, -4), (i - 1) * 6f, 140f);
            }
            MamaTheme.Skin(settings.GetComponent<Image>(), MamaTheme.PlankS, Color.white, 22f);
            MamaTheme.Skin(start.GetComponent<Image>(), MamaTheme.PaintS, MamaTheme.Teal, 22f);
            MamaTheme.Skin(helpButton.GetComponent<Image>(), MamaTheme.PaintS, MamaUI.Paper, 18f);
            settingsLabel.color = MamaTheme.Cream; startLabel.color = Color.white;
            MamaTheme.MakeHeading(heading); heading.fontSize = 52; heading.color = MamaTheme.Brick;
            foreach (var tl in titles) MamaTheme.MakeHeading(tl);
        }

        RectTransform Box(string name, Transform parent, Vector2 anchor, Vector2 at, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = at; rt.sizeDelta = size; go.GetComponent<Image>().color = color; return rt;
        }
        Text Label(Transform parent, string text, int size, Vector2 at, Vector2 area, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false); rt.anchorMin = rt.anchorMax = Vector2.one * .5f; rt.anchoredPosition = at; rt.sizeDelta = area;
            var label = go.GetComponent<Text>(); label.text = text; label.font = english; label.fontSize = size; label.color = MamaUI.Ink;
            label.alignment = alignment; label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }
        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool bot = Debug.isDebugBuild && (System.Array.IndexOf(args, "-mamabot") >= 0 || System.Array.IndexOf(args, "-mamaautostart") >= 0 || System.Array.IndexOf(args, "-mamaautostartsolo") >= 0);
            // from the menu: show the guide on the first round of this session only, then just start
            if (!bot && gm && gm.state == GameManager.State.Title && !MamaFrontEnd.I && !(MamaLaunch.FromMenu && shownOnce)) { shownOnce = true; Show(); }
        }
        public void Show() => Show(true);
        /// startRound: closing the guide on the title starts the round (PLAY); false = just look at it.
        public void Show(bool startRound)
        {
            if (Open) return;
            startOnClose = startRound;
            cursorLocked = Cursor.lockState == CursorLockMode.Locked;
            savedScale = Time.timeScale;
            pausedHere = !NetGame.Online;
            if (pausedHere) Time.timeScale = 0f;
            panel.SetActive(true); if (gm && gm.ui) gm.ui.ShowTitle(false); Refresh();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        public void Close()
        {
            if (!Open) return;
            panel.SetActive(false); closedAt = Time.unscaledTime;
            if (pausedHere) Time.timeScale = savedScale;
            pausedHere = false;
            if (!gm) { }
            else if (gm.state == GameManager.State.Title && !startOnClose) { }
            else if (gm.state == GameManager.State.Title)
            {
                if (NetGame.IsAuthority) { gm.StartRound(); NetGame.Broadcast(NetGame.Msg.Start, null, null, 0); }
                else gm.ui.ShowTitle(true);
            }
            else if (gm && (cursorLocked || gm.Live) && !MamaSettings.Open) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame && !MamaSettings.Open)
            { if (Open) Close(); else Show(); }
            if (language != Loc.Thai) Refresh();
            if (helpButton) helpButton.gameObject.SetActive(!MamaFrontEnd.Active);
            if (Open && NetGame.Online && pausedHere) { Time.timeScale = savedScale > 0f ? savedScale : 1f; pausedHere = false; }
        }
        void LateUpdate() { if (Open) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
        void Refresh()
        {
            language = Loc.Thai;
            helpLabel.text = T("F1  How to play", "F1  วิธีเล่น");
            heading.text = T("Mom is coming! Leave no evidence.", "แม่กำลังกลับ! เก็บบ้านก่อนแม่จับได้");
            subtitle.text = T("Stop the disaster → pick things up → return them or hide them. Picking up alone does not finish the job.", "หยุดต้นเหตุ → หยิบของ → เก็บเข้าที่หรือซ่อนให้พ้นตา แค่หยิบขึ้นมายังไม่จบงานนะ!");
            titles[0].text = T("1  Stop things getting worse", "1  หยุดต้นเหตุก่อน");
            titles[1].text = T("2  Pick up, then take it home", "2  หยิบแล้วนำไปเก็บ");
            titles[2].text = T("3  Return it — or hide it", "3  วางเข้าที่ หรือเลือกซ่อน");
            bodies[0].text = T("E turns off the stove / tap.\nOil fire? Use a lid or extinguisher.\nDo not throw water on burning oil!", "กด E ปิดเตา / ปิดก๊อกน้ำ\nไฟในหม้อ ใช้ฝาปิดหรือถังดับเพลิง\nห้ามสาดน้ำใส่ไฟน้ำมัน!");
            bodies[1].text = T("E picks up an item. Follow its room label and the HOME marker.\nExample: the teddy belongs in the bedroom, on the bed.", "กด E หยิบ แล้วเดินตามป้ายบอกห้อง\nกับหมุดปลายทาง “เก็บที่นี่”\nเช่น ตุ๊กตาหมีต้องคืนห้องนอน บนเตียง");
            bodies[2].text = T("Stand near the green outline and press Q to return it.\nOr carry it to a cupboard and press E to hide it. Mom may look inside!", "เข้าใกล้เงาสีเขียว แล้วกด Q วาง\nหรือถือไปที่ตู้ กด E เพื่อซ่อน\nระวังแม่เปิดตรวจเจอ!");
            solo.text = T("Solo: stove → tap → one room at a time. With friends: share the jobs, then make some trouble!", "คนเดียว: เริ่มปิดเตา → ปิดก๊อก → เก็บทีละห้อง   |   กับเพื่อน: แบ่งหน้าที่ แล้วค่อยแกล้งกัน!");
            keys.text = T("WASD move · Shift run · E use / pick up · Q place · RMB throw · LMB / G punch · TAB notes", "WASD เดิน · Shift วิ่ง · E ใช้ / หยิบ · Q วาง · คลิกขวา ขว้าง · คลิกซ้าย / G ต่อย · TAB ภาพรวม");
            settingsLabel.text = T("ESC  Co-op / sound settings", "ESC  ชวนเพื่อน / ปรับเสียง");
            startLabel.text = gm && gm.state == GameManager.State.Title && !startOnClose ? T("Got it!", "เข้าใจแล้ว!") : T("Got it — let's clean up!", "เข้าใจแล้ว ไปเก็บบ้านกัน!");
        }
        void OnDestroy() { if (pausedHere) Time.timeScale = savedScale; if (instance == this) instance = null; }
    }
}
