using UnityEngine;
using UnityEngine.UI;

namespace MamasComing.WitchTest
{
    /// <summary>All panel/button/icon artwork comes from the generated PNG atlas. Text remains live and localized.</summary>
    public class WitchHUD : MonoBehaviour
    {
        public Sprite[] skin;
        public Texture2D spellPages;
        WitchLevel level;
        Font font;
        RectTransform root;
        GameObject gameplay, title, pause, ending, notes, prompt;
        Text timer, progress, interaction, heldLabel, notice, endTitle, endBody;
        Image reticle;
        static readonly Color Ink = new Color(.105f, .095f, .17f), Cream = new Color(1, .97f, .91f), Peach = new Color(1, .72f, .52f);
        static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BC = new Vector2(.5f, 0), CC = new Vector2(.5f, .5f);

        public void Build(WitchLevel owner)
        {
            level = owner;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma", "Arial Unicode MS" }, 32);
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("AppleWitchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 80;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            root = (RectTransform)go.transform;
            var hud = Rect("Gameplay", root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero); gameplay = hud.gameObject;

            var goal = Panel("Goal", hud, TL, new Vector2(28, -26), new Vector2(385, 100), 0);
            Label("GoalText", goal, "ก่อนแม่กลับ", 30, Ink, new Vector2(70, -13), new Vector2(285, 39));
            progress = Label("Progress", goal, "เก็บของให้เข้าที่  0 / 3", 21, Ink, new Vector2(70, -57), new Vector2(285, 30));
            var clock = Panel("Timer", hud, TR, new Vector2(-28, -26), new Vector2(190, 100), 1);
            Label("ClockLabel", clock, "แม่ถึงใน", 20, Cream, new Vector2(21, -12), new Vector2(150, 27));
            timer = Label("Time", clock, "03:30", 36, Cream, new Vector2(21, -40), new Vector2(150, 45));

            var cross = Rect("Aim", hud, CC, CC, CC, Vector2.zero, new Vector2(27, 27));
            reticle = Art(cross, 7); reticle.color = new Color(1, 1, 1, .85f);
            var action = Panel("Action", hud, BC, new Vector2(0, 30), new Vector2(650, 76), 1); prompt = action.gameObject;
            Art(Rect("Mouse", action, TL, TL, TL, new Vector2(20, -12), new Vector2(48, 48)), 4);
            interaction = Label("ActionText", action, "เล็งของรก แล้วคลิกเพื่อยก", 26, Cream, new Vector2(84, -17), new Vector2(540, 44));
            heldLabel = Label("Held", hud, "", 24, Cream, new Vector2(34, -155), new Vector2(460, 42));
            notice = Label("Notice", hud, "", 25, Cream, new Vector2(520, -48), new Vector2(800, 66));
            var shadow = notice.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(1, -2); shadow.effectColor = new Color(0, 0, 0, .6f);
            var helpHint = Rect("HelpHint", hud, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 40), new Vector2(350, 36));
            var hint = AddText(helpHint, "TAB / H ค้าง • เปิดตำรา", 21, Cream, TextAnchor.MiddleRight);
            hint.gameObject.AddComponent<Shadow>().effectColor = Ink;

            title = Menu("Welcome", "APPLE & THE WITCH", "ลูกแอปเปิลในครัวแม่มด", "จัดถ้วย หนังสือ และขวดกลับเข้าที่\nก่อนแม่กลับมาเห็นโต๊ะรก\n\nWASD เดิน • เมาส์เล็ง • คลิกยก / วาง\nล้อเมาส์ปรับระยะ • H ค้างเพื่อเปิดตำรา", "เริ่มเก็บกวาด", level.Begin);
            pause = Menu("Pause", "พักก่อน", "", "เล็งกลางจอ แล้วคลิกเพื่อยกของ\nล้อเมาส์ปรับใกล้–ไกล • คลิกอีกทีเพื่อวาง\nQ / E หมุนของ • คลิกขวาปล่อย\nWASD เดิน • SPACE กระโดด", "กลับไปเล่น", level.TogglePause);
            pause.SetActive(false);
            ending = Menu("Ending", "", "", "", "ลองอีกรอบ", level.Restart);
            endTitle = ending.transform.Find("Card/Heading").GetComponent<Text>();
            endBody = ending.transform.Find("Card/Body").GetComponent<Text>(); ending.SetActive(false);
            BuildNotes();
            gameplay.SetActive(false);
        }

        void BuildNotes()
        {
            var panel = Panel("SpellNotes", root, CC, Vector2.zero, new Vector2(1020, 680), 0);
            Label("Heading", panel, "ตำราจัดของฉบับแอปเปิล", 36, Ink, new Vector2(100, -44), new Vector2(820, 56));
            var picture = Rect("IllustratedPage", panel, TL, TL, TL, new Vector2(100, -120), new Vector2(820, 410));
            var raw = picture.gameObject.AddComponent<RawImage>(); raw.texture = spellPages; raw.raycastTarget = false;
            Label("Instructions", panel, "คลิกเพื่อยก / วาง  •  ล้อเมาส์ใกล้–ไกล  •  ปล่อย TAB / H เพื่อปิด", 25, Ink, new Vector2(100, -560), new Vector2(820, 50));
            notes = panel.gameObject; notes.SetActive(false);
        }

        GameObject Menu(string name, string heading, string sub, string body, string button, UnityEngine.Events.UnityAction click)
        {
            var full = Rect(name, root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
            var card = Panel("Card", full, CC, Vector2.zero, new Vector2(780, 600), 1);
            Label("Heading", card, heading, 46, Cream, new Vector2(56, -46), new Vector2(668, 68));
            Label("Subtitle", card, sub, 27, Peach, new Vector2(56, -126), new Vector2(668, 46));
            var bodyText = Label("Body", card, body, 28, Cream, new Vector2(56, -205), new Vector2(668, 220));
            bodyText.alignment = TextAnchor.UpperLeft;
            var btnRect = Panel("StartButton", card, BC, new Vector2(0, 46), new Vector2(390, 78), 2);
            var b = btnRect.gameObject.AddComponent<Button>(); b.targetGraphic = btnRect.GetComponent<Image>(); b.onClick.AddListener(click);
            b.targetGraphic.raycastTarget = true;
            AddText(btnRect, button, 28, Ink, TextAnchor.MiddleCenter);
            return full.gameObject;
        }

        public void Refresh()
        {
            if (!level || !timer) return;
            int seconds = Mathf.CeilToInt(level.TimeLeft);
            timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            timer.color = seconds <= 30 ? Peach : Cream;
            progress.text = "เก็บของให้เข้าที่  " + level.TidyCount + " / " + level.props.Length;
            notice.text = level.CurrentFeedback;
            heldLabel.text = level.Held ? "กำลังยก • " + level.Held.label : "";
            if (level.Held)
                interaction.text = level.ValidPlacement ? "คลิก • วางลง" + level.Held.home.label : "ล้อเมาส์ • ใกล้–ไกล    คลิก • วาง";
            else interaction.text = level.Aimed ? "คลิก • ยก" + level.Aimed.label : "เล็งของรก แล้วคลิกเพื่อยก";
            reticle.color = level.ValidPlacement ? new Color(.65f, 1, .77f) : level.Aimed || level.Held ? Peach : new Color(1, 1, 1, .75f);
            prompt.SetActive(level.phase == WitchLevel.Phase.Playing && !level.Paused);
        }

        public void ShowTitle(bool on) { title.SetActive(on); gameplay.SetActive(!on); }
        public void ShowPause(bool on) { pause.SetActive(on); gameplay.SetActive(!on); if (on) notes.SetActive(false); }
        public void ShowNotes(bool on) { if (notes) notes.SetActive(on && !level.Paused && level.phase == WitchLevel.Phase.Playing); }
        public void ShowEnd(bool won)
        {
            ending.SetActive(true); gameplay.SetActive(false); notes.SetActive(false);
            endTitle.text = won ? "เรียบร้อยก่อนแม่กลับ!" : "แม่กลับมาแล้ว!";
            endBody.text = won ? "ถ้วย หนังสือ และขวดกลับเข้าที่\nคทาช่วยยกของ แต่คนที่จัดการได้คือเรา\n\nกด R เพื่อลองวิธีอื่น" : "ยังมีของรกบนโต๊ะ\nลองเลือกทางเดินและระยะยกใหม่\n\nกด R เพื่อลองอีกครั้ง";
        }
        public void ClearOverlays() { title.SetActive(false); pause.SetActive(false); ending.SetActive(false); notes.SetActive(false); gameplay.SetActive(true); }

        RectTransform Panel(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, int sprite)
        {
            var r = Rect(name, parent, anchor, anchor, anchor, position, size); Art(r, sprite).type = Image.Type.Simple; return r;
        }
        Image Art(RectTransform r, int sprite)
        {
            var image = r.gameObject.AddComponent<Image>(); image.sprite = skin[sprite]; image.raycastTarget = false; return image;
        }
        Text Label(string name, RectTransform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            return AddText(Rect(name, parent, TL, TL, TL, position, dimensions), text, size, color, TextAnchor.MiddleLeft);
        }
        Text AddText(RectTransform rect, string value, int size, Color color, TextAnchor alignment)
        {
            if (rect.GetComponent<Graphic>()) rect = Rect("Caption", rect, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = alignment; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
        }
        static RectTransform Rect(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(parent, false);
            r.anchorMin = min; r.anchorMax = max; r.pivot = pivot; r.anchoredPosition = position; r.sizeDelta = size; return r;
        }
    }
}
