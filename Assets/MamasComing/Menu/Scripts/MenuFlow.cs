using UnityEngine;

namespace MamasComing.Menu
{
    /// What the player picked on the way from the main menu to a level: room settings + their dress-up look.
    public static class MenuFlow
    {
        public const string MenuScene = "MC_MainMenu";
        public const string LobbyScene = "MC_Lobby";

        public struct LevelInfo
        {
            public string scene, en, th, blurbEn, blurbTh;
            public Color color;
            public bool networked;   // world is synced by NetSession; otherwise each player runs their own copy
        }

        public static readonly LevelInfo[] Levels =
        {
            new LevelInfo { scene = "MamasKitchen", en = "MAMA'S HOUSE", th = "บ้านของแม่", color = new Color(1f, .45f, .55f), networked = true,
                blurbEn = "The party wrecked the house. Clean it before Mom walks in!", blurbTh = "ปาร์ตี้พังบ้านหมดแล้ว รีบเก็บก่อนแม่เดินเข้าบ้าน!" },
            new LevelInfo { scene = "AppleWitch_Test", en = "WITCH'S KITCHEN", th = "ครัวแม่มด", color = new Color(.55f, .42f, .95f), networked = false,
                blurbEn = "Tiny you, giant magic kitchen. Wand everything back home!", blurbTh = "ตัวจิ๋วในครัวเวทมนตร์ ใช้ไม้กายสิทธิ์เก็บของเข้าที่!" },
        };

        public static int Level;
        public static int MaxPlayers = 3;
        public static int MinPlayers => 2;
        /// Upper bound comes from the co-op session itself, so it grows automatically if NetSession allows more.
        public static int MaxAllowed => Mathf.Max(MinPlayers, NetSession.MaxPlayers);
        public static string StatusNote = "";

        public static string Txt(string en, string th) => Loc.Thai ? th : en;
        public static string LevelName(int i) => Loc.Thai ? Levels[i].th : Levels[i].en;
        public static string LevelBlurb(int i) => Loc.Thai ? Levels[i].blurbTh : Levels[i].blurbEn;

        // ------------------------------------------------------------ dress-up catalogue
        public static string[] FruitEn => KidLook.Names;
        public static string[] FruitTh => KidLook.NamesTh;
        public static readonly string[] HatEn = { "No hat", "Party cone", "Top hat", "Crown", "Chef hat", "Propeller", "Big bow" };
        public static readonly string[] HatTh = { "ไม่ใส่หมวก", "หมวกปาร์ตี้", "หมวกทรงสูง", "มงกุฎ", "หมวกเชฟ", "หมวกใบพัด", "โบว์ยักษ์" };
        public static readonly string[] FaceEn = { "Nothing", "Cool shades", "Star glasses", "Mustache" };
        public static readonly string[] FaceTh = { "ไม่มี", "แว่นดำเท่ๆ", "แว่นดาว", "หนวดคุณลุง" };
        public static readonly Color[] Colors =
        {
            new Color(1f, .36f, .52f), new Color(.2f, .78f, .74f), new Color(1f, .82f, .2f), new Color(.52f, .4f, .95f),
            new Color(.36f, .82f, .32f), new Color(1f, .55f, .2f), new Color(.25f, .55f, 1f), new Color(.96f, .96f, .96f),
        };
        public static readonly string[] ColorEn = { "Bubblegum", "Mint", "Lemon", "Grape", "Lime", "Tangerine", "Sky", "Cloud" };
        public static readonly string[] ColorTh = { "ชมพูหมากฝรั่ง", "มิ้นต์", "เลมอน", "องุ่น", "มะนาว", "ส้มจี๊ด", "ฟ้า", "ขาวเมฆ" };

        public static Cosmetic Mine
        {
            get => new Cosmetic
            {
                fruit = (byte)KidLook.Selected,
                hat = (byte)Mathf.Clamp(PlayerPrefs.GetInt("mc_look_hat", 1), 0, HatEn.Length - 1),
                face = (byte)Mathf.Clamp(PlayerPrefs.GetInt("mc_look_face", 0), 0, FaceEn.Length - 1),
                color = (byte)Mathf.Clamp(PlayerPrefs.GetInt("mc_look_color", 0), 0, Colors.Length - 1),
            };
            set
            {
                KidLook.Selected = value.fruit; PlayerPrefs.SetInt("mc_look_hat", value.hat);
                PlayerPrefs.SetInt("mc_look_face", value.face); PlayerPrefs.SetInt("mc_look_color", value.color);
                PlayerPrefs.Save();
            }
        }

        /// The in-level kid wears the fruit picked in the lobby (KidLook is shared with the co-op gameplay).
        public static void PushFruitToGame(int fruit) { KidLook.Selected = fruit; }
    }

    [System.Serializable]
    public struct Cosmetic
    {
        public byte fruit, hat, face, color;
        public Color Tint => MenuFlow.Colors[color % MenuFlow.Colors.Length];
        public bool Same(Cosmetic o) => fruit == o.fruit && hat == o.hat && face == o.face && color == o.color;
    }
}
