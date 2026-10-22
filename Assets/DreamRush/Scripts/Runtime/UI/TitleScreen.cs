using System.Collections;
using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Hub;
using DreamRush.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DreamRush.UI
{
    /// <summary>
    /// THE LOBBY — Persona 5 × Chrono-Punk Rebel Pop edition.
    /// Pip stands in the left foreground of the Night Room diorama while a mechanical Chrono-Gear & concentric
    /// crimson/obsidian graphic swirl hub anchors the centre-left; the 6 menu commands radiate in an angled arc
    /// of black-and-white ransom-note stamp blocks around the gear (referencing @chrono.punk & Persona 5).
    /// Also surfaces the Cognitive Problem-Solving Dossier ("ชุดฝึกทักษะแก้ปัญหาในโลกความฝัน").
    /// Preserves all TitleScreen APIs and camera shot hooks for HubFlowPlayModeTests.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        public static bool ShownThisSession { get; internal set; }
        public static TitleScreen Current { get; private set; }
        /// <summary>Tests and dev tools can turn the lobby off.</summary>
        public static bool Enabled = true;

        static readonly Color P5Crimson = new Color(0.86f, 0.07f, 0.16f);
        static readonly Color P5CrimsonDark = new Color(0.48f, 0.03f, 0.09f);
        static readonly Color P5Obsidian = new Color(0.05f, 0.04f, 0.07f);
        static readonly Color P5Yellow = new Color(1.00f, 0.84f, 0.04f);
        static readonly Color P5Paper = new Color(0.98f, 0.96f, 0.92f);
        static readonly Color P5Steel = new Color(0.16f, 0.24f, 0.38f);

        struct Item
        {
            public string key, label, sub, help, tagCode;
            public System.Action act;
            public RectTransform root, body, ransomRoot;
            public Image outerCut, tape, jag, slashBar, numBg;
            public Text text, echoA, echoB, subText, numText;
            public Button button;
            public float hot, arcAngle;
            public Vector2 basePos;
        }

        readonly List<Item> _items = new List<Item>();
        LobbyTheme _theme;
        RectTransform _root, _menu, _logoRoot, _clock, _pointer, _bandTop, _bandBot, _stickers, _hint;
        RectTransform _gearHub, _gearOuter, _gearInner, _swirlA, _swirlB, _gearHand, _dossier;
        Text _time, _date, _mode, _help, _bandTopText, _bandBotText, _dossierText;
        RectTransform _secondHand;
        readonly List<(RectTransform rt, float baseRot, float phase)> _letters = new List<(RectTransform, float, float)>();
        readonly List<(RectTransform rt, float born, float life, float size)> _live = new List<(RectTransform, float, float, float)>();
        Transform _camPose;
        ThirdPersonCameraController _cam;
        PlayerMotor3D _player;
        PlayerUIBridge _bridge;
        int _selected = -1;
        float _t0, _nextSticker, _shotBlend, _handAngle;
        bool _leaving, _walking;
        Vector3 _camPos; Quaternion _camRot;

        static Sprite S(string n)
        {
            var tex = Resources.Load<Texture2D>("DreamRush/Lobby/" + n);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                n == "LB_Tape" ? new Vector4(40, 8, 40, 8) : Vector4.zero);
        }
        static Font Fnt(string n) => Resources.Load<Font>("DreamRush/Fonts/" + n) ?? UIStyle.TitleFont;
        static readonly string[] LetterFonts = { "Kanit-BlackItalic", "Bungee-Regular", "DelaGothicOne-Regular", "BowlbyOne-Regular", "RubikMonoOne-Regular" };
        static readonly string[] StickerNames = { "LB_Spark", "LB_Ring", "LB_Arrow" };
        readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        Sprite Sp(string n) { if (!_sprites.TryGetValue(n, out var s)) { s = S(n); _sprites[n] = s; } return s; }

        // ================================================================== API (unchanged)
        public static TitleScreen ShowIfFirstBoot()
        {
            if (!Enabled || Current != null) return null;
            ShownThisSession = true;
            var ui = UIRoot.Get();
            var rt = UIFactory.Stretch(UIFactory.Rect("Lobby", ui.Overlay));
            var t = rt.gameObject.AddComponent<TitleScreen>();
            Current = t;
            t.Build();
            return t;
        }

        /// <summary>Dev/automation: leave the lobby into free roam (as WALK AROUND).</summary>
        public static void Dismiss() { if (Current != null) Current.Walk(); }

        public int ItemCount => _items.Count;
        public string SelectedKey => _selected >= 0 ? _items[_selected].key : null;
        public LobbyTheme Theme => _theme;
        public bool Walking => _walking;
        public void SelectKey(string key) { for (int i = 0; i < _items.Count; i++) if (_items[i].key == key) { Select(i); if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_items[i].button.gameObject); } }
        public void ActivateKey(string key) { for (int i = 0; i < _items.Count; i++) if (_items[i].key == key) Activate(i); }

        // ================================================================== build
        void Build()
        {
            _root = (RectTransform)transform;
            _t0 = Time.unscaledTime;
            _theme = LobbyThemeDirector.Current != null ? LobbyThemeDirector.Current.Theme : LobbyTheme.Current;
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _cam = FindFirstObjectByType<ThirdPersonCameraController>();
            _bridge = _player != null ? _player.GetComponent<PlayerUIBridge>() : null;
            if (_bridge != null) _bridge.CutsceneLock = true;
            _camPose = new GameObject("LobbyCamPose").transform;
            if (_cam != null) { _camPos = _cam.transform.position; _camRot = _cam.transform.rotation; _cam.SetOverride(_camPose); _cam.ReleaseCursor(); }
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var spot = GameObject.Find("LobbyPipSpot");
            if (spot != null && _player != null) _player.Teleport(spot.transform.position, spot.transform.rotation);

            var data = Core.SaveManager.GetOrCreate().Data;
            var p = data.progression;
            int solvedMethods = data.diary != null && data.diary.discoveredVictories != null ? data.diary.discoveredVictories.Count : 0;

            _items.Add(new Item
            {
                key = "DREAM", tagCode = "TRIAL // 01", label = "ENTER DREAM",
                sub = "เข้าสู่โลกความฝัน · ฝึกทักษะแก้ปัญหา",
                help = "เข้าสู่โลกแห่งความฝัน สังเกตเส้นทาง ทดลอง และใช้สิ่งแวดล้อมเพื่อแก้ปัญหา",
                act = Dream
            });
            _items.Add(new Item
            {
                key = "WALK", tagCode = "ROOM // 02", label = "WALK AROUND",
                sub = "สำรวจห้องนอน & เบาะแส",
                help = "เดินสำรวจห้องของ Pip เพื่อค้นหาเบาะแส — กด TAB (หรือ START) เพื่อเปิดหน้าเมนู Persona",
                act = Walk
            });
            _items.Add(new Item
            {
                key = "BED", tagCode = "GEAR // 03", label = "UPGRADES",
                sub = "อัปเกรดเตียง & ทักษะความฝัน",
                help = $"ใช้ Dream Memory ({p.dreamMemory} แต้ม) อัปเกรดผ้าห่มกันกระแทกและฟูกเมฆเพิ่มความเสถียร",
                act = () => UIRoot.Get().Push(ModalPanel.Create<BedUpgradePanel>("UPGRADE BED", new Vector2(1180, 820)))
            });
            _items.Add(new Item
            {
                key = "DIARY", tagCode = "CASE // 04", label = "PROBLEM LOG",
                sub = $"สมุดบันทึกการแก้ปัญหา ({solvedMethods} วิธี)",
                help = "ตรวจสอบวิธีแก้ปัญหาที่ค้นพบ ประวัติการผ่านด่าน และวิเคราะห์จุดอ่อนของบอส",
                act = () => UIRoot.Get().Push(ModalPanel.Create<DreamDiaryPanel>("DREAM DIARY", new Vector2(1100, 900)))
            });
            _items.Add(new Item
            {
                key = "SETTINGS", tagCode = "SYS // 05", label = "SETTINGS",
                sub = "ตั้งค่าระบบ & การเข้าถึง",
                help = "ปรับแต่งเสียง มุมมองกล้อง ความไวเมาส์ และตัวช่วยการมองเห็น",
                act = () => UIRoot.Get().Push(ModalPanel.Create<SettingsPanel>("SETTINGS", new Vector2(1100, 960)))
            });
            _items.Add(new Item
            {
                key = "QUIT", tagCode = "EXIT // 06", label = "WAKE UP",
                sub = "ตื่นนอน (ออกจากเกม)",
                help = "บันทึกความก้าวหน้าและตื่นจากโลกความฝัน — แล้วพบกันใหม่คืนหน้า",
                act = Quit
            });

            BuildFX();
            BuildChronoHub();
            BuildBands();
            BuildLogo();
            BuildClock();
            BuildDossier();
            BuildMenu();
            BuildHelp();
            _stickers = UIFactory.Rect("Stickers", _root); UIFactory.Stretch(_stickers); _stickers.SetSiblingIndex(3);
            Select(0);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_items[0].button.gameObject);
            Audio.AudioManager.Play(Audio.Sfx.UiOpen, Vector3.zero);
            if (LobbyThemeDirector.Current != null) LobbyThemeDirector.Current.ThemeChanged += OnThemeChanged;
        }

        void OnThemeChanged(LobbyTheme t) { _theme = t; Recolor(); }

        Image Img(string name, Transform parent, string sprite, Color c)
        {
            var img = UIFactory.Panel(name, parent, c, sprite != null ? Sp(sprite) : null);
            img.raycastTarget = false;
            if (sprite == "LB_Tape") img.type = Image.Type.Sliced;
            return img;
        }

        Text Txt(string name, Transform parent, string s, string font, int size, Color c, TextAnchor a)
        {
            var t = UIFactory.Label(name, parent, s, size, c, a);
            t.font = Fnt(font); t.fontStyle = FontStyle.Normal; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, float rot = 0f)
        {
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            return rt;
        }

        Image _half1, _half2, _grain, _scan, _crimsonWing, _blackWing, _yellowSlash;

        void BuildFX()
        {
            // Right-side Persona 5 / Chrono-Punk graphic crimson backdrop wing + obsidian cut-out frame
            // Leaves the left/centre-left diorama window completely open so Pip is framed clearly in 3D!
            _crimsonWing = UIFactory.Panel("CrimsonWing", _root, new Color(P5Crimson.r, P5Crimson.g, P5Crimson.b, 0.88f));
            _crimsonWing.raycastTarget = false;
            Anchor(_crimsonWing.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 0f), new Vector2(820f, 1650f), 11f);
            UIStyle.HardShadow(_crimsonWing, P5Obsidian, new Vector2(-18f, -18f));

            _blackWing = UIFactory.Panel("BlackWing", _root, new Color(P5Obsidian.r, P5Obsidian.g, P5Obsidian.b, 0.92f));
            _blackWing.raycastTarget = false;
            Anchor(_blackWing.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(270f, -20f), new Vector2(760f, 1650f), 7f);

            _yellowSlash = UIFactory.Panel("YellowSlash", _root, P5Yellow);
            _yellowSlash.raycastTarget = false;
            Anchor(_yellowSlash.rectTransform, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-195f, 0f), new Vector2(18f, 1700f), 11f);

            _half1 = Img("HalftoneTL", _root, "LB_HalfCorner", P5Crimson);
            Anchor(_half1.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(680, 680), 0);
            _half1.rectTransform.localScale = new Vector3(-1, -1, 1); _half1.rectTransform.pivot = new Vector2(1, 0);

            _half2 = Img("HalftoneBR", _root, "LB_HalfCorner", P5Yellow);
            Anchor(_half2.rectTransform, new Vector2(1, 0), new Vector2(1, 0), Vector2.zero, new Vector2(860, 860), 0);

            _grain = Img("Grain", _root, "LB_Grain", Color.white); _grain.type = Image.Type.Tiled; UIFactory.Stretch(_grain.rectTransform);
            _scan = Img("Scan", _root, "LB_Scan", Color.white); _scan.type = Image.Type.Tiled; _scan.pixelsPerUnitMultiplier = 0.5f; UIFactory.Stretch(_scan.rectTransform);
        }

        /// <summary>Chrono-Punk mechanical gear pocketwatch & concentric crimson swirl hub anchored to the right radial menu arc.</summary>
        void BuildChronoHub()
        {
            _gearHub = Anchor(UIFactory.Rect("ChronoHub", _root), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-610f, 10f), new Vector2(360f, 360f));

            // Concentric Persona graphic swirl rings behind the gear
            var swA = UIStyle.Image("SwirlOuter", _gearHub, UIStyle.SwirlRing, new Color(P5Crimson.r, P5Crimson.g, P5Crimson.b, 0.36f), false);
            _swirlA = Anchor(swA.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 680f));

            var swB = UIStyle.Image("SwirlInner", _gearHub, UIStyle.SwirlRing, new Color(P5Yellow.r, P5Yellow.g, P5Yellow.b, 0.16f), false);
            _swirlB = Anchor(swB.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 460f));

            // Shadow gear + Metallic Steel/Obsidian Outer Gear + Crimson Inner Gear
            var gearShadow = UIStyle.Image("GearShadow", _gearHub, UIStyle.ChronoGear, new Color(0, 0, 0, 0.78f), false);
            Anchor(gearShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, -10f), new Vector2(256f, 256f));

            var gearOut = UIStyle.Image("GearOuter", _gearHub, UIStyle.ChronoGear, P5Steel, false);
            _gearOuter = Anchor(gearOut.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(246f, 246f));

            var gearIn = UIStyle.Image("GearInner", _gearHub, UIStyle.ChronoGear, P5Crimson, false);
            _gearInner = Anchor(gearIn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(148f, 148f));

            // Golden Clock Hand that aims directly at the selected menu item in the arc
            _gearHand = Anchor(UIFactory.Rect("GearHandPivot", _gearHub), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            var beam = UIStyle.Image("HandBeam", _gearHand, UIStyle.Slab, P5Yellow);
            Anchor(beam.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(175f, 10f));
            UIStyle.HardShadow(beam, P5Obsidian, new Vector2(4f, -4f));

            var centerCap = UIStyle.Image("CenterCap", _gearHub, UIStyle.Diamond ?? UIStyle.ChronoGear, P5Paper, false);
            Anchor(centerCap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
            UIEnter.Add(_gearHub, new Vector2(260f, -80f), 0.04f, 0.32f, -25f);
        }

        void BuildBands()
        {
            const string loop = "COGNITIVE TRIALS  ◆  OBSERVE · EXPERIMENT · SOLVE  ◆  DREAM RUSH  ◆  ฝึกทักษะการแก้ปัญหา  ◆  TAKE YOUR SLEEP  ◆  ";
            var top = Img("BandTop", _root, "LB_Band", P5Crimson);
            _bandTop = Anchor(top.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(240, -34), new Vector2(3200, 56), -3.5f);
            _bandTopText = Txt("Text", _bandTop, string.Concat(loop, loop, loop, loop), "ChakraPetch-Bold", 30, P5Paper, TextAnchor.MiddleLeft);
            Anchor(_bandTopText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6000, 56));

            var bot = Img("BandBottom", _root, "LB_Band", P5Obsidian);
            _bandBot = Anchor(bot.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(-280, 58), new Vector2(3200, 48), 2.5f);
            _bandBotText = Txt("Text", _bandBot, string.Concat(loop, loop, loop, loop), "Kanit-BlackItalic", 26, P5Yellow, TextAnchor.MiddleLeft);
            Anchor(_bandBotText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6000, 48));
            UIEnter.Add(_bandTop, new Vector2(1800, 0), 0.02f, 0.5f);
            UIEnter.Add(_bandBot, new Vector2(-1800, 0), 0.06f, 0.5f);
        }

        void BuildLogo()
        {
            _logoRoot = Anchor(UIFactory.Rect("Logo", _root), new Vector2(0, 0), new Vector2(0, 0), new Vector2(54, 118), new Vector2(860, 340), 3.5f);
            // High-contrast crimson/black slash plate behind the ransom logo
            var backSlash = UIStyle.Image("LogoSlash", _logoRoot, UIStyle.Slab, P5Crimson);
            Anchor(backSlash.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(-18, 52), new Vector2(690, 210), -4f);
            UIStyle.HardShadow(backSlash, P5Obsidian, new Vector2(12, -12));

            var r = new System.Random(20261020);
            Line("DREAM", 0, 0, 108, r);
            Line("RUSH!", 136, -138, 122, r);

            var tagSlab = UIStyle.Image("TagSlab", _logoRoot, UIStyle.Slab, P5Obsidian);
            Anchor(tagSlab.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(16, 20), new Vector2(680, 52), -1.5f);
            UIStyle.HardShadow(tagSlab, P5Yellow, new Vector2(6, -6));
            var tagline = Txt("Tag", tagSlab.rectTransform, "COGNITIVE DREAM TRIALS  ·  ผจญภัยแก้ปัญหาในความฝัน", "ChakraPetch-Bold", 25, P5Paper, TextAnchor.MiddleCenter);
            UIFactory.Stretch(tagline.rectTransform);

            _logoRoot.localScale = Vector3.one * 0.84f;
            UIEnter.Add(_logoRoot, new Vector2(-900, -200), 0.15f, 0.55f, -12f);
        }

        void Line(string word, float x0, float y, int size, System.Random r)
        {
            float x = x0;
            int i = _letters.Count;
            foreach (char ch in word)
            {
                var tile = Img("L_" + ch, _logoRoot, "LB_Tile", Color.white);
                float w = size * (ch == 'M' || ch == 'W' ? 1.12f : ch == '!' ? 0.55f : 0.9f) * (0.92f + (float)r.NextDouble() * 0.18f);
                float h = size * (1.05f + (float)r.NextDouble() * 0.18f);
                float rot = ((float)r.NextDouble() - 0.5f) * 16f;
                Anchor(tile.rectTransform, new Vector2(0, 0), new Vector2(0.5f, 0.5f), new Vector2(x + w / 2, 245 + y + ((float)r.NextDouble() - 0.5f) * 14f), new Vector2(w, h), rot);
                var t = Txt("Ch", tile.rectTransform, ch.ToString(), LetterFonts[(i + r.Next(3)) % LetterFonts.Length], Mathf.RoundToInt(size * 0.78f), Color.black, TextAnchor.MiddleCenter);
                UIFactory.Stretch(t.rectTransform);
                UIStyle.HardShadow(tile, P5Obsidian, new Vector2(8, -8));
                _letters.Add((tile.rectTransform, rot, (float)r.NextDouble() * 6.28f));
                x += w * 0.95f;
                i++;
            }
        }

        void BuildClock()
        {
            _clock = Anchor(UIFactory.Rect("Clock", _root), new Vector2(0, 1), new Vector2(0, 1), new Vector2(44, -76), new Vector2(540, 240), -4f);
            var border = UIStyle.Image("Border", _clock, UIStyle.Slab, P5Paper);
            Anchor(border.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-6, 6), new Vector2(512, 152));
            var card = Img("Card", _clock, "LB_Tape", P5Obsidian);
            Anchor(card.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(500, 140));
            UIStyle.HardShadow(card, P5Crimson, new Vector2(10, -10));

            var ring = Img("Ring", _clock, "LB_ClockRing", P5Crimson);
            Anchor(ring.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(76, -70), new Vector2(104, 104));
            _secondHand = Img("Hand", ring.rectTransform, null, P5Yellow).rectTransform;
            Anchor(_secondHand, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(6, 42));

            _time = Txt("Time", _clock, "00:00", "Bungee-Regular", 84, P5Paper, TextAnchor.MiddleLeft);
            Anchor(_time.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(144, -68), new Vector2(360, 110));
            var o = _time.gameObject.AddComponent<Outline>(); o.effectColor = P5Obsidian; o.effectDistance = new Vector2(3, -3);

            var modeTape = Img("Mode", _clock, "LB_Tape", P5Crimson);
            Anchor(modeTape.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(22, -128), new Vector2(410, 58), 2.5f);
            UIStyle.HardShadow(modeTape, P5Obsidian, new Vector2(6, -6));
            _mode = Txt("Text", modeTape.rectTransform, "", "Kanit-BlackItalic", 32, P5Paper, TextAnchor.MiddleCenter);
            UIFactory.Stretch(_mode.rectTransform);

            _date = Txt("Date", _clock, "", "ChakraPetch-Bold", 23, P5Yellow, TextAnchor.MiddleLeft);
            Anchor(_date.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -186), new Vector2(520, 32));
            UIStyle.HardShadow(_date, P5Obsidian, new Vector2(3, -3));
            UIEnter.Add(_clock, new Vector2(0, 220), 0.05f, 0.32f, 8f);
        }

        void BuildDossier()
        {
            var d = Core.SaveManager.GetOrCreate().Data;
            int runsWon = d.diary != null ? d.diary.runsWon : 0;
            int methods = d.diary != null && d.diary.discoveredVictories != null ? d.diary.discoveredVictories.Count : 0;
            int mem = d.progression != null ? d.progression.dreamMemory : 0;

            _dossier = Anchor(UIFactory.Rect("CognitiveDossier", _root), new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -326), new Vector2(500, 118), 1.5f);
            var plate = UIStyle.Image("Plate", _dossier, UIStyle.Cut ?? UIStyle.Slab, new Color(P5Obsidian.r, P5Obsidian.g, P5Obsidian.b, 0.92f));
            UIFactory.Stretch(plate.rectTransform);
            UIStyle.HardShadow(plate, P5Crimson, new Vector2(7, -7));

            var header = UIStyle.Image("Header", _dossier, UIStyle.Slab, P5Yellow);
            Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-8, 12), new Vector2(340, 36), -2f);
            var hTxt = Txt("HTxt", header.rectTransform, "PROBLEM-SOLVING DOSSIER", "ChakraPetch-Bold", 20, P5Obsidian, TextAnchor.MiddleCenter);
            UIFactory.Stretch(hTxt.rectTransform);

            _dossierText = Txt("Stats", _dossier,
                $"SOLVED DREAMS: {runsWon}   ◆   METHODS: {methods}   ◆   MEMORY: {mem}\nทักษะหลัก: สังเกตกฎ · ทดลอง · ประยุกต์ใช้สิ่งแวดล้อมแก้ปัญหา",
                "ChakraPetch-Bold", 21, P5Paper, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_dossierText.rectTransform);
            _dossierText.rectTransform.offsetMin = new Vector2(22, 8);
            _dossierText.rectTransform.offsetMax = new Vector2(-18, -14);
            UIEnter.Add(_dossier, new Vector2(-360, 0), 0.08f, 0.32f, -4f);
        }

        void BuildMenu()
        {
            _menu = Anchor(UIFactory.Rect("Menu", _root), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-34, 24), new Vector2(920, 820));
            _pointer = Img("Pointer", _menu, "LB_Apple", P5Yellow).rectTransform;
            _pointer.sizeDelta = new Vector2(78, 78);

            // Radial arc coordinates around the Chrono-Gear (inspired by @chrono.punk reference image 2)
            float[] xOffsets = { -110f, -45f, -15f, -25f, -70f, -140f };
            float[] yOffsets = { -68f, -192f, -316f, -440f, -564f, -688f };
            float[] arcTilts = { 8.5f, 5.0f, 1.5f, -2.0f, -5.5f, -9.0f };
            float[] handAngles = { 26f, 15f, 4f, -7f, -18f, -29f };

            for (int i = 0; i < _items.Count; i++)
            {
                int idx = i;
                var it = _items[i];
                it.arcAngle = handAngles[i];
                var rt = Anchor(UIFactory.Rect("Item_" + it.key, _menu), new Vector2(1, 1), new Vector2(1, 0.5f),
                    new Vector2(xOffsets[i], yOffsets[i]), new Vector2(780, 108), arcTilts[i]);
                it.basePos = rt.anchoredPosition;
                var enter = UIFactory.Stretch(UIFactory.Rect("Enter", rt));
                var body = Anchor(UIFactory.Rect("Body", enter), new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(780, 108));

                // Jagged crimson/yellow burst behind active item
                it.jag = Img("Jag", body, "LB_Jag", P5Crimson);
                Anchor(it.jag.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(28, -4), new Vector2(720, 180));

                // Diagonal yellow rebel slash streak when active
                it.slashBar = UIStyle.Image("SlashBar", body, UIStyle.Slab, P5Yellow);
                Anchor(it.slashBar.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(10, -34), new Vector2(640, 18), -2.5f);

                // Outer white ransom-note cut-out border + Inner obsidian stamp slab
                it.outerCut = UIStyle.Image("OuterCut", body, UIStyle.Cut ?? UIStyle.Slab, P5Paper);
                Anchor(it.outerCut.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(6, -4), new Vector2(560, 92), -1.2f);
                UIStyle.HardShadow(it.outerCut, new Color(0, 0, 0, 0.85f), new Vector2(10, -10));

                it.tape = Img("Tape", body, "LB_Tape", P5Obsidian);
                Anchor(it.tape.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(546, 80));

                // Left-side ransom-note index stamp ("01" .. "06")
                it.numBg = UIStyle.Image("NumStamp", body, UIStyle.Cut ?? UIStyle.Slab, P5Crimson);
                Anchor(it.numBg.rectTransform, new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-536, 10), new Vector2(76, 62), 6f);
                UIStyle.HardShadow(it.numBg, P5Obsidian, new Vector2(5, -5));
                it.numText = Txt("Num", it.numBg.rectTransform, $"0{i + 1}", "Bungee-Regular", 30, P5Paper, TextAnchor.MiddleCenter);
                UIFactory.Stretch(it.numText.rectTransform);

                // Main command typography + RGB split echoes
                it.echoA = Txt("EchoA", body, it.label, "Kanit-BlackItalic", 58, P5Crimson, TextAnchor.MiddleRight);
                it.echoB = Txt("EchoB", body, it.label, "Kanit-BlackItalic", 58, P5Yellow, TextAnchor.MiddleRight);
                it.text = Txt("Label", body, it.label, "Kanit-BlackItalic", 58, P5Paper, TextAnchor.MiddleRight);
                foreach (var t in new[] { it.echoA, it.echoB, it.text })
                    Anchor(t.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-34, 13), new Vector2(720, 96));

                // Sub-label in a sharp cut-out tag strip
                it.subText = Txt("Sub", body, it.sub, "ChakraPetch-Bold", 21, P5Yellow, TextAnchor.MiddleRight);
                Anchor(it.subText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-36, -21), new Vector2(580, 34));

                // Clickable hit area
                var hit = UIFactory.Panel("Hit", rt, new Color(0, 0, 0, 0), null);
                UIFactory.Stretch(hit.rectTransform); hit.raycastTarget = true;
                var b = hit.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = hit;
                b.onClick.AddListener(() => Activate(idx));
                var trig = hit.gameObject.AddComponent<EventTrigger>();
                AddTrigger(trig, EventTriggerType.Select, () => Select(idx));
                AddTrigger(trig, EventTriggerType.PointerEnter, () => { if (!_mouseMoved) return; if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject); Select(idx); });
                it.button = b; it.root = rt; it.body = body;
                UIEnter.Add(enter, new Vector2(900, 0), 0.18f + i * 0.05f, 0.4f, -8f);
                _items[i] = it;
            }
            for (int i = 0; i < _items.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = _items[(i + _items.Count - 1) % _items.Count].button;
                nav.selectOnDown = _items[(i + 1) % _items.Count].button;
                _items[i].button.navigation = nav;
            }
        }

        static void AddTrigger(EventTrigger trig, EventTriggerType type, System.Action a)
        {
            var e = new EventTrigger.Entry { eventID = type };
            e.callback.AddListener(_ => a());
            trig.triggers.Add(e);
        }

        RectTransform _helpRt;
        void BuildHelp()
        {
            var strip = Img("Help", _root, "LB_Tape", P5Obsidian);
            _helpRt = Anchor(strip.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 114), new Vector2(1080, 68), 1.8f);
            UIStyle.HardShadow(strip, P5Crimson, new Vector2(8, -8));
            _help = Txt("Text", _helpRt, "", "ChakraPetch-Bold", 25, P5Paper, TextAnchor.MiddleLeft);
            UIFactory.Stretch(_help.rectTransform); _help.rectTransform.offsetMin = new Vector2(42, 0); _help.rectTransform.offsetMax = new Vector2(-28, 0);
            UIEnter.Add(_helpRt, new Vector2(0, -260), 0.36f);
        }

        // ================================================================== colours (Persona 5 × Chrono-Punk with subtle time-of-day mood accent)
        void Recolor()
        {
            var t = _theme;
            _half1.color = new Color(P5Crimson.r, P5Crimson.g, P5Crimson.b, 0.38f);
            _half2.color = new Color(P5Yellow.r, P5Yellow.g, P5Yellow.b, 0.26f);
            _grain.color = new Color(1, 1, 1, t.night ? 0.10f : 0.06f);
            _scan.color = new Color(0, 0, 0, t.night ? 0.06f : 0.03f);
            _bandTop.GetComponent<Image>().color = P5Crimson; _bandTopText.color = P5Paper;
            _bandBot.GetComponent<Image>().color = P5Obsidian; _bandBotText.color = P5Yellow;

            // Ransom-note logo tiles: alternating Crimson, Paper White, Obsidian Black, and Rebel Yellow
            Color[] tiles = { P5Crimson, P5Paper, P5Obsidian, P5Yellow, P5Paper, P5Obsidian };
            for (int i = 0; i < _letters.Count; i++)
            {
                var img = _letters[i].rt.GetComponent<Image>();
                var c = tiles[(i * 2 + i / 3) % tiles.Length];
                img.color = c;
                var tx = _letters[i].rt.GetComponentInChildren<Text>();
                float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                tx.color = lum > 0.55f ? P5Obsidian : P5Paper;
            }
            _clock.Find("Card").GetComponent<Image>().color = P5Obsidian;
            _clock.Find("Ring").GetComponent<Image>().color = P5Crimson; _secondHand.GetComponent<Image>().color = P5Yellow;
            _time.color = P5Paper;
            _clock.Find("Mode").GetComponent<Image>().color = P5Crimson; _mode.color = P5Paper; _mode.text = t.name + " · " + t.thai;
            _helpRt.GetComponent<Image>().color = P5Obsidian; _help.color = P5Paper;
            _pointer.GetComponent<Image>().color = P5Yellow;
            _date.color = P5Yellow;
        }

        // ================================================================== behaviour
        void Select(int i)
        {
            if (i == _selected || i < 0 || i >= _items.Count) return;
            bool first = _selected < 0;
            _selected = i;
            if (_help != null) _help.text = $"[{_items[i].tagCode}]  ◆  {_items[i].help}";
            if (!first)
            {
                _shotBlend = 0f;
                Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero, 0.4f);
                SpawnSticker(_items[i].root, true);
            }
            if (_theme != null && first) Recolor();
        }

        void Activate(int i)
        {
            if (_leaving || _walking || UIRoot.Get().AnyPanelOpen) return;
            Select(i);
            Audio.AudioManager.Play(Audio.Sfx.UiClick, Vector3.zero, 0.8f);
            _items[i].act?.Invoke();
        }

        void Dream()
        {
            if (_leaving) return;
            StartCoroutine(DreamRoutine());
        }

        IEnumerator DreamRoutine()
        {
            _leaving = true;
            yield return Slash("TAKE YOUR DREAM!");
            yield return CloseUI();
            var bed = FindFirstObjectByType<Interaction.BedInteractable>();
            if (Flow.GameFlowManager.Exists && bed != null && _player != null) Flow.GameFlowManager.Get().BeginSleep(bed, _player);
            Destroy(gameObject);
        }

        /// <summary>P5-style full-screen slash card: two diagonal colour shards sweep in with a ransom caption.</summary>
        IEnumerator Slash(string caption)
        {
            var layer = UIFactory.Stretch(UIFactory.Rect("Slash", _root));
            var a = Img("ShardA", layer, null, P5Crimson); var b = Img("ShardB", layer, null, P5Obsidian);
            var c = Img("ShardC", layer, null, P5Yellow);
            Anchor(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3600, 420), -14f);
            Anchor(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3600, 140), -14f);
            Anchor(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3600, 32), -14f);
            var cap = Txt("Cap", layer, caption, "Kanit-BlackItalic", 132, P5Paper, TextAnchor.MiddleCenter);
            Anchor(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800, 200), -14f);
            var o = cap.gameObject.AddComponent<Outline>(); o.effectColor = P5Obsidian; o.effectDistance = new Vector2(5, -5);
            UIStyle.HardShadow(cap, P5Yellow, new Vector2(10, -10));
            Audio.AudioManager.Play(Audio.Sfx.Whoosh, Vector3.zero, 1f);
            for (float t = 0; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                float k = UIStyle.OutCubic(t / 0.28f);
                a.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-3600, 0, k), 0);
                b.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(3600, 0, UIStyle.OutCubic((t - 0.05f) / 0.28f)), -165);
                c.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-3600, 0, UIStyle.OutCubic((t - 0.08f) / 0.28f)), 180);
                cap.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, UIStyle.OutBack((t - 0.1f) / 0.3f));
                cap.color = new Color(P5Paper.r, P5Paper.g, P5Paper.b, Mathf.Clamp01((t - 0.1f) * 8f));
                yield return null;
            }
            Destroy(layer.gameObject, 0.9f);
        }

        bool _mouseMoved;
        float _panelFade; bool _closing; CanvasGroup _lobbyCg; bool _pipHidden; readonly System.Collections.Generic.List<Renderer> _hiddenR = new System.Collections.Generic.List<Renderer>();
        void ShowPip() { foreach (var r in _hiddenR) if (r != null) r.enabled = true; _hiddenR.Clear(); _pipHidden = false; }

        IEnumerator CloseUI()
        {
            _closing = true;
            ShowPip();
            if (_player != null) _player.FaceDirection = Vector3.zero;
            var cg = gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                if (cg == null || _menu == null || _logoRoot == null) yield break;
                float k = UIStyle.InOutCubic(t / 0.3f);
                _menu.anchoredPosition = new Vector2(-34 + k * 1200f, 24);
                _logoRoot.anchoredPosition = new Vector2(54 - k * 1100f, 118);
                cg.alpha = 1f - k;
                yield return null;
            }
            if (cg == null) yield break;
            cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;
            if (_cam != null) { _cam.ClearOverride(); if (_player != null) { _cam.Yaw = _player.transform.eulerAngles.y; _cam.Pitch = 15f; } }
            if (_bridge != null) _bridge.CutsceneLock = false;
        }

        void Walk()
        {
            if (_leaving || _walking) return;
            StartCoroutine(WalkRoutine());
        }

        IEnumerator WalkRoutine()
        {
            _walking = true;
            yield return CloseUI();
            BuildHint();
            if (_cam != null) _cam.lockCursor = true;
        }

        void BuildHint()
        {
            var tape = Img("Hint", UIRoot.Get().Overlay, "LB_Tape", P5Obsidian);
            _hint = Anchor(tape.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(350, 62), 3f);
            UIStyle.HardShadow(tape, P5Crimson, new Vector2(6, -6));
            var t = Txt("Text", _hint, "TAB  ◆  LOBBY", "Kanit-BlackItalic", 30, P5Yellow, TextAnchor.MiddleCenter);
            UIFactory.Stretch(t.rectTransform);
        }

        public void BackToLobby()
        {
            if (UIRoot.Exists) UIRoot.Get().CloseAll();
            if (_hint != null) Destroy(_hint.gameObject);
            Current = null; _walking = false; _leaving = true;
            if (_cam != null) _cam.lockCursor = false;
            Destroy(gameObject);
            ShowIfFirstBoot();
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ================================================================== per-frame
        void SpawnSticker(RectTransform near, bool burst)
        {
            if (_stickers == null || !burst) return;
            var name = StickerNames[Random.Range(0, StickerNames.Length)];
            var img = Img("Sticker", _stickers, name, Color.white);
            Color[] cols = { P5Crimson, P5Yellow, P5Paper };
            img.color = cols[Random.Range(0, cols.Length)];
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            if (near != null)
            {
                Vector3 w = near.TransformPoint(new Vector3(-near.rect.width * Random.Range(0.35f, 0.85f), Random.Range(-45f, 45f), 0));
                rt.anchoredPosition = _stickers.InverseTransformPoint(w);
            }
            float size = Random.Range(64f, 108f);
            rt.sizeDelta = Vector2.one * size;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            _live.Add((rt, Time.unscaledTime, 0.65f, size));
        }

        void Update()
        {
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            var ms = Mouse.current; if (!_mouseMoved && ms != null && ms.delta.ReadValue().sqrMagnitude > 4f) _mouseMoved = true;
            if (_walking)
            {
                if (Flow.GameFlowManager.Exists && Flow.GameFlowManager.Get().State != Flow.FlowState.Hub)
                {
                    if (_hint != null) Destroy(_hint.gameObject);
                    _leaving = true; Destroy(gameObject); return;
                }
                var kb = Keyboard.current; var gp = Gamepad.current;
                if ((kb != null && kb.tabKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame)) BackToLobby();
                return;
            }
            if (_theme == null) return;
            bool panelOpen = UIRoot.Exists && UIRoot.Get().AnyPanelOpen;
            _panelFade = Mathf.MoveTowards(_panelFade, panelOpen ? 1f : 0f, dt * 7f);
            if (!_closing)
            {
                if (_lobbyCg == null) { _lobbyCg = GetComponent<CanvasGroup>(); if (_lobbyCg == null) _lobbyCg = gameObject.AddComponent<CanvasGroup>(); }
                _lobbyCg.alpha = 1f - _panelFade; _lobbyCg.blocksRaycasts = !panelOpen; _lobbyCg.interactable = !panelOpen;
            }

            // Clock update
            var dtNow = System.DateTime.Now;
            _time.text = dtNow.ToString((dtNow.Second % 2 == 0) ? "HH:mm" : "HH mm", System.Globalization.CultureInfo.InvariantCulture);
            _date.text = dtNow.ToString("ddd dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant() + "   ·   " + _theme.thai;
            _secondHand.localRotation = Quaternion.Euler(0, 0, -(dtNow.Second + dtNow.Millisecond / 1000f) * 6f);

            // Chrono-Gear mechanical rotation & hand targeting
            if (_gearOuter != null) _gearOuter.localRotation = Quaternion.Euler(0, 0, -now * 14f);
            if (_gearInner != null) _gearInner.localRotation = Quaternion.Euler(0, 0, now * 26f);
            if (_swirlA != null) _swirlA.localRotation = Quaternion.Euler(0, 0, now * 6f);
            if (_swirlB != null) _swirlB.localRotation = Quaternion.Euler(0, 0, -now * 9f);
            if (_gearHand != null && _selected >= 0)
            {
                float targetAng = _items[_selected].arcAngle;
                _handAngle = Mathf.LerpAngle(_handAngle, targetAng, 1f - Mathf.Exp(-14f * dt));
                _gearHand.localRotation = Quaternion.Euler(0, 0, _handAngle);
            }

            // Scrolling bands
            float w = 1400f;
            _bandTopText.rectTransform.anchoredPosition = new Vector2(-Mathf.Repeat((now - _t0) * 90f, w), 0);
            _bandBotText.rectTransform.anchoredPosition = new Vector2(-w + Mathf.Repeat((now - _t0) * 70f, w), 0);

            // Ransom logo beat
            float beat = Mathf.Pow(1f - Mathf.Repeat((now - _t0) * 2f, 1f), 6f);
            for (int i = 0; i < _letters.Count; i++)
            {
                var (rt, br, ph) = _letters[i];
                rt.localRotation = Quaternion.Euler(0, 0, br + Mathf.Sin(now * 2.1f + ph) * 3.0f);
                float pop = (i % 4 == Mathf.FloorToInt((now - _t0) * 2f) % 4) ? beat * 0.10f : 0f;
                rt.localScale = Vector3.one * (1f + pop);
            }

            // Radial ransom-stamp menu animation
            bool panel = UIRoot.Get().AnyPanelOpen;
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                float target = i == _selected && !panel ? 1f : 0f;
                it.hot = Mathf.MoveTowards(it.hot, target, dt * 7f);
                float k = UIStyle.OutBack(it.hot, 2.1f);
                int size = Mathf.RoundToInt(Mathf.Lerp(54, 76, k));
                it.text.fontSize = it.echoA.fontSize = it.echoB.fontSize = size;

                // Unselected: Stark white text on obsidian stamp with white cut-out frame
                // Selected: Obsidian/White high-impact text over Crimson jag + Rebel Yellow slash
                it.text.color = Color.Lerp(P5Paper, P5Paper, it.hot);
                float jit = it.hot > 0.5f ? Mathf.Sin(now * 37f) * 2.0f : 0f;
                it.echoA.color = new Color(P5Obsidian.r, P5Obsidian.g, P5Obsidian.b, it.hot * 0.98f);
                it.echoB.color = new Color(P5Yellow.r, P5Yellow.g, P5Yellow.b, it.hot * 0.92f);
                it.echoA.rectTransform.anchoredPosition = new Vector2(-34 + 6 * it.hot + jit, 13 - 5 * it.hot);
                it.echoB.rectTransform.anchoredPosition = new Vector2(-34 - 5 * it.hot - jit, 13 + 4 * it.hot);

                float barW = Mathf.Max(460f, it.text.preferredWidth + 130f);
                it.outerCut.color = Color.Lerp(P5Paper, P5Yellow, it.hot);
                it.outerCut.rectTransform.sizeDelta = new Vector2(barW + 16f, Mathf.Lerp(92f, 106f, k));
                it.tape.color = Color.Lerp(P5Obsidian, P5Crimson, it.hot * 0.85f);
                it.tape.rectTransform.sizeDelta = new Vector2(barW, Mathf.Lerp(78f, 90f, k));

                it.numBg.color = Color.Lerp(P5Crimson, P5Obsidian, it.hot);
                it.numText.color = Color.Lerp(P5Paper, P5Yellow, it.hot);
                it.numBg.rectTransform.anchoredPosition = new Vector2(-barW - 14f, 8f);

                it.jag.color = new Color(P5Crimson.r, P5Crimson.g, P5Crimson.b, Mathf.Clamp01(it.hot * 1.35f));
                it.jag.rectTransform.localScale = new Vector3(Mathf.Lerp(0.3f, 1.04f, k), Mathf.Lerp(0.2f, 1.02f, k) * (1f + Mathf.Sin(now * 9f) * 0.03f * it.hot), 1);
                it.jag.rectTransform.sizeDelta = new Vector2(barW + 150f, 178f);

                it.slashBar.color = new Color(P5Yellow.r, P5Yellow.g, P5Yellow.b, it.hot);
                it.slashBar.rectTransform.sizeDelta = new Vector2((barW + 90f) * k, 16f);

                it.subText.color = Color.Lerp(new Color(P5Paper.r, P5Paper.g, P5Paper.b, 0.72f), P5Yellow, it.hot);
                it.body.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -3.2f, k));
                it.root.anchoredPosition = it.basePos + new Vector2(-44f * k, 0);
                it.body.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.06f, k);
                _items[i] = it;
            }
            if (_selected >= 0)
            {
                var sel = _items[_selected];
                float barW = Mathf.Max(460f, sel.text.preferredWidth + 130f);
                Vector3 wp = sel.root.TransformPoint(new Vector3(-barW - 76f, 8f + Mathf.Sin(now * 6f) * 6f, 0));
                _pointer.position = Vector3.Lerp(_pointer.position, wp, 1f - Mathf.Exp(-18f * dt));
                _pointer.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(now * 4f) * 12f);
            }
            if (!panel && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && _selected >= 0)
                EventSystem.current.SetSelectedGameObject(_items[_selected].button.gameObject);

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var (rt, born, life, size) = _live[i];
                if (rt == null) { _live.RemoveAt(i); continue; }
                float a = now - born;
                float s = a < 0.2f ? UIStyle.OutBack(a / 0.2f, 3f) : a > life - 0.2f ? Mathf.Clamp01((life - a) / 0.2f) : 1f;
                rt.localScale = Vector3.one * s;
                if (a > life) { Destroy(rt.gameObject); _live.RemoveAt(i); }
            }
            UpdateCamera(dt);
        }

        void UpdateCamera(float dt)
        {
            if (_camPose == null || _selected < 0) return;
            string key = _items[_selected].key;
            if (_player != null && _cam != null)
            {
                bool hide = key != "DREAM" && key != "WALK" && Vector3.Distance(_cam.transform.position, _player.transform.position) < 5f;
                var ds = Find("LobbyShot_DREAM");
                if (ds != null)
                {
                    Vector3 to = ds.transform.position - _player.transform.position; to.y = 0;
                    if (to.sqrMagnitude > 0.01f)
                        _player.FaceDirection = to.normalized;
                }
                if (hide != _pipHidden)
                {
                    if (hide) { _pipHidden = true; foreach (var r in _player.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; _hiddenR.Add(r); } }
                    else ShowPip();
                }
            }
            var shot = Find("LobbyShot_" + key);
            Vector3 pos; Quaternion rot;
            if (shot != null) { pos = shot.transform.position; rot = shot.transform.rotation; }
            else if (_player != null)
            {
                Vector3 focus = _player.transform.position + Vector3.up * 0.75f;
                rot = Quaternion.Euler(14f, _player.transform.eulerAngles.y + 160f + _selected * 25f, 0);
                pos = focus - rot * Vector3.forward * 4f;
            }
            else return;
            float t = Time.unscaledTime;
            pos += new Vector3(Mathf.PerlinNoise(t * 0.25f, 1.3f) - 0.5f, Mathf.PerlinNoise(t * 0.21f, 7.1f) - 0.5f, 0) * 0.22f;
            rot *= Quaternion.Euler((Mathf.PerlinNoise(t * 0.3f, 3.3f) - 0.5f) * 1.4f, (Mathf.PerlinNoise(t * 0.27f, 9.9f) - 0.5f) * 2.1f, 0);
            _shotBlend = Mathf.MoveTowards(_shotBlend, 1f, dt * 1.8f);
            float k = 1f - Mathf.Exp(-(2.8f + 6f * (1f - _shotBlend)) * dt);
            _camPos = Vector3.Lerp(_camPos, pos, k);
            _camRot = Quaternion.Slerp(_camRot, rot, k);
            _camPose.SetPositionAndRotation(_camPos, _camRot);
            var focusObj = Find("LobbyFocus_" + key);
            if (_player != null)
            {
                Vector3 look = (focusObj != null ? focusObj.transform.position : _camPos) - _player.transform.position; look.y = 0;
                if (look.sqrMagnitude > 0.01f)
                    _player.transform.rotation = Quaternion.RotateTowards(_player.transform.rotation, Quaternion.LookRotation(look), 240f * dt);
            }
        }

        readonly Dictionary<string, GameObject> _found = new Dictionary<string, GameObject>();
        GameObject Find(string n) { if (!_found.TryGetValue(n, out var g)) { g = GameObject.Find(n); _found[n] = g; } return g; }

        void OnDestroy()
        {
            ShowPip();
            if (_player != null) _player.FaceDirection = Vector3.zero;
            if (_hint != null && !_walking) Destroy(_hint.gameObject);
            if (LobbyThemeDirector.Current != null) LobbyThemeDirector.Current.ThemeChanged -= OnThemeChanged;
            if (_camPose != null) Destroy(_camPose.gameObject);
            if (Current == this) Current = null;
            if (_bridge != null && !_leaving) _bridge.CutsceneLock = false;
        }
    }
}
