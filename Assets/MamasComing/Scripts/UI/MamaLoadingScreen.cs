using UnityEngine;
using UnityEngine.UI;
namespace MamasComing
{
    /// Persistent loading curtain; Next changes only the card, never the readiness barrier.
    public class MamaLoadingScreen : MonoBehaviour
    {
        static MamaLoadingScreen instance;
        public static float Progress;
        Text tipTitle, tipBody, status, counter; RawImage photo; Image progress;
        int index; float nextTip;
        static readonly string[] Keys={"fire","water","pot","fridge","power","team"};
        static readonly string[] TitlesEn={"OIL FIRE? NO WATER!","STOP THE LEAK FIRST","A CLEAN POT LEAVES NO CLUE","SOMETHING SMELLS...","POWER OUT? CHECK THE FLOOR","MAKE ROOM FOR YOUR FRIENDS"};
        static readonly string[] TitlesTh={"ไฟน้ำมัน ห้ามสาดน้ำ!","หยุดน้ำไหลก่อนถูพื้น","ล้างหม้อให้สะอาด ไม่เหลือหลักฐาน","กลิ่นอะไรในตู้เย็น?","ไฟดับ ตรวจพื้นก่อน","แบ่งทางให้เพื่อนด้วย"};
        static readonly string[] BodyEn={"Switch off the stove. Use a lid or aim the fire extinguisher at the base of the flames. Water makes an oil fire flare up.","Close the tap or repair the leaking pipe, then mop up the water. Walk slowly while mopping to clear the wet floor safely.","Carry the burnt pot to the kitchen sink and hold left click to scrub. A cabinet can hide it, but Mom may open the door!","Open the fridge, take out the rotten food and carry it to a trash bin. Closing the door alone does not remove spoiled food.","Dry the water near electrical sockets, then restore power at the garage breaker. Fixing the cause stops it tripping again.","Walking into a friend blocks your path. Sprinting into them can knock both of you down. Split up the jobs and keep the corridors clear."};
        static readonly string[] BodyTh={"ปิดเตาก่อน ใช้ฝาหม้อครอบหรือพ่นถังดับเพลิงไปที่โคนไฟ อย่าสาดน้ำ เพราะไฟน้ำมันจะปะทุแรงขึ้น","ปิดก๊อกหรือซ่อมท่อที่รั่วก่อน แล้วค่อยถูน้ำ เดินช้า ๆ ระหว่างถู จะได้เก็บน้ำโดยไม่ลื่นล้ม","ถือหม้อไหม้ไปที่อ่างล้างในครัว แล้วกดคลิกซ้ายค้างเพื่อขัด ตู้ช่วยซ่อนได้ แต่แม่อาจเปิดเจอ!","เปิดตู้เย็น หยิบอาหารเน่าออกมา แล้วถือไปทิ้งถังขยะ แค่ปิดประตูตู้เย็นยังไม่ทำให้อาหารเน่าหายไป","ถูน้ำใกล้ปลั๊กให้แห้ง แล้วไปเปิดเบรกเกอร์ที่โรงรถ แก้ต้นเหตุด้วย ไฟจะได้ไม่ตัดซ้ำ","เดินชนเพื่อนจะติดกัน แต่ถ้าวิ่งชนอาจล้มกลิ้งทั้งคู่ แบ่งหน้าที่และเว้นทางเดินให้เพื่อนถือของผ่านด้วย"};
        public static void Open()
        {
            if(instance) return; Progress=0;
            var go=new GameObject("Mama Loading Curtain"); DontDestroyOnLoad(go); instance=go.AddComponent<MamaLoadingScreen>();
        }
        public static void Close() { if(instance) Destroy(instance.gameObject); instance=null; }
        // chalkboard look: a polaroid of the problem, a sticky-note tip and a little chalk kid mopping in the corner
        RectTransform polaroidRt, noteRt; RawImage kid; Text dots; float tipPop = 1f, openedAt; GameObject returnLink;
        const float StuckSeconds = 30f;

        /// A text-only button with a hand-drawn underline that wiggles on hover (no box).
        static GameObject LinkButton(Transform parent, string name, Vector2 pos, Vector2 size, string label, int font, Color color, UnityEngine.Events.UnityAction click)
        {
            var rt = MamaUIKit.Rect(name, parent, MamaUIKit.CC, MamaUIKit.CC, pos, size);
            var hit = rt.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = hit; b.transition = Selectable.Transition.None; b.onClick.AddListener(click);
            var t = MamaFlowUI.Text(rt, "Label", new Vector2(0, 6), size, label, font, color, TextAnchor.MiddleCenter); t.font = MamaTheme.Bold; t.raycastTarget = false;
            var line = MamaUIKit.Img(MamaUIKit.Rect("Underline", rt, MamaUIKit.BC, MamaUIKit.BC, new Vector2(0, 6), new Vector2(size.x * .78f, 4), -1f), MamaTheme.StrokeS, color, 0f);
            rt.gameObject.AddComponent<MamaLinkHover>().Init(t, line.rectTransform);
            return rt.gameObject;
        }
        const int KidFrames = 16, KidCols = 4; const float KidFps = 12f;
        static readonly Color Chalk = new Color(.95f, .94f, .89f), ChalkDim = new Color(.95f, .94f, .89f, .55f);
        static readonly Color Sticky = new Color(1f, .86f, .48f), Tag = new Color(1f, .78f, .55f), PenBlue = new Color(.27f, .5f, .86f);

        static RectTransform Art(Transform parent, string name, string sprite, Vector2 pos, Vector2 size)
        {
            var rt = MamaUIKit.Rect(name, parent, MamaUIKit.CC, MamaUIKit.CC, pos, size);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Resources.Load<Sprite>("MamaUI/Art/" + sprite); im.raycastTarget = false;
            if (!im.sprite) im.color = new Color(1, 1, 1, .9f);
            return rt;
        }

        static RectTransform Shadowed(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float rot)
        {
            var shadow = MamaFlowUI.Panel(parent, name + " shadow", pos + new Vector2(10, -14), size, MamaTheme.PaperS, new Color(0, 0, 0, .38f));
            shadow.localRotation = Quaternion.Euler(0, 0, rot);
            var rt = MamaFlowUI.Panel(parent, name, pos, size, MamaTheme.PaperS, color);
            rt.localRotation = Quaternion.Euler(0, 0, rot); return rt;
        }

        void Awake()
        {
            var c = MamaFlowUI.Canvas("Loading", transform, 1000);
            var bg = MamaUIKit.Stretch("Chalkboard", c.transform);
            var board = bg.gameObject.AddComponent<RawImage>(); board.raycastTarget = true;
            board.texture = Resources.Load<Texture2D>("MamaUI/Loading/chalkboard"); if (!board.texture) board.color = new Color(.13f, .19f, .17f);

            // chalk title, top-left
            var logo = MamaUIKit.Label("Chalk title", c.transform, MamaUIKit.TL, MamaUIKit.TL, new Vector2(70, -48), new Vector2(700, 80), "MAMA'S COMING!", null, MamaTheme.Display, 54, Chalk, TextAnchor.MiddleLeft, 0f, 3f);
            logo.color = new Color(1f, .93f, .78f, .92f);

            // the polaroid with the problem photo (paper art rendered by ArtSource/UI/render_ui_art.js; photo sits under the frame)
            polaroidRt = MamaUIKit.Rect("Polaroid", c.transform, MamaUIKit.CC, MamaUIKit.CC, new Vector2(-235, 45), new Vector2(940, 710), -1.5f);
            var pic = MamaUIKit.Rect("Problem photo", polaroidRt, MamaUIKit.CC, MamaUIKit.CC, new Vector2(0, 50), new Vector2(816, 462));
            photo = pic.gameObject.AddComponent<RawImage>(); photo.raycastTarget = false;
            Art(polaroidRt, "Frame", "ui_polaroid", Vector2.zero, new Vector2(940, 710));
            tipTitle = MamaFlowUI.Text(polaroidRt, "Caption", new Vector2(-20, -248), new Vector2(760, 90), "", 40, PenBlue, TextAnchor.MiddleLeft);
            tipTitle.font = MamaTheme.Bold;
            // orange pinked paper tag over the top edge
            var tag = Art(polaroidRt, "Next job tag", "ui_tag", new Vector2(0, 320), new Vector2(370, 126));
            tag.localRotation = Quaternion.Euler(0, 0, 1.2f);
            MamaFlowUI.Text(tag, "Tag text", new Vector2(0, 4), new Vector2(300, 70), MamaFlowUI.T("Next job", "ภารกิจต่อไป"), 38, new Color(.55f, .28f, .12f), TextAnchor.MiddleCenter).font = MamaTheme.Bold;

            // sticky note with the tip (curled corner, crease)
            noteRt = MamaUIKit.Rect("Tip note", c.transform, MamaUIKit.CC, MamaUIKit.CC, new Vector2(470, 70), new Vector2(530, 620), 1.8f);
            Art(noteRt, "Paper", "ui_sticky", Vector2.zero, new Vector2(530, 620));
            var tape = Art(noteRt, "Tape", "ui_tape2", new Vector2(0, 292), new Vector2(190, 58)); tape.localRotation = Quaternion.Euler(0, 0, -4f);
            MamaFlowUI.Text(noteRt, "Tip label", new Vector2(-20, 205), new Vector2(380, 70), MamaFlowUI.T("Tips:", "เคล็ดลับ:"), 40, new Color(.36f, .22f, .12f)).font = MamaTheme.Bold;
            tipBody = MamaFlowUI.Text(noteRt, "Tip", new Vector2(0, 5), new Vector2(400, 320), "", 30, new Color(.3f, .2f, .12f), TextAnchor.UpperLeft);
            // handwritten link instead of a boxed button
            LinkButton(noteRt, "Next tip", new Vector2(10, -222), new Vector2(330, 64), MamaFlowUI.T("Next tip  →", "เคล็ดลับถัดไป  →"), 32, new Color(.2f, .42f, .78f), Next);
            counter = MamaFlowUI.Text(noteRt, "Counter", new Vector2(160, 207), new Vector2(110, 40), "", 22, new Color(.45f, .3f, .15f, .8f), TextAnchor.MiddleRight);

            // little chalk kid mopping (2D frame animation) + chalk status, bottom-left
            var kidRt = MamaUIKit.Rect("Chalk kid (loading)", c.transform, MamaUIKit.BL, MamaUIKit.BL, new Vector2(40, 10), new Vector2(330, 330));
            kid = kidRt.gameObject.AddComponent<RawImage>(); kid.raycastTarget = false;
            kid.texture = Resources.Load<Texture2D>("MamaUI/Loading/chalk_kid_sheet"); if (!kid.texture) kid.enabled = false;
            dots = MamaUIKit.Label("Chalk loading", c.transform, MamaUIKit.BL, MamaUIKit.BL, new Vector2(370, 92), new Vector2(620, 70), "", null, MamaTheme.Bold, 40, Chalk, TextAnchor.MiddleLeft);
            status = MamaUIKit.Label("Status", c.transform, MamaUIKit.BL, MamaUIKit.BL, new Vector2(372, 48), new Vector2(900, 46), "", null, MamaTheme.Body, 24, ChalkDim, TextAnchor.MiddleLeft);
            // chalk "progress": a hand-drawn line that grows (no percentage)
            var track = MamaUIKit.Rect("Chalk line", c.transform, MamaUIKit.BL, MamaUIKit.BL, new Vector2(372, 30), new Vector2(560, 6));
            MamaUIKit.Img(track, null, new Color(1, 1, 1, .12f));
            progress = MamaUIKit.Img(MamaUIKit.Stretch("Drawn", track), null, new Color(1f, .93f, .78f, .8f));

            // the way back only appears when loading is really stuck (different builds, a friend never arrives)
            returnLink = LinkButton(c.transform, "Return", new Vector2(700, -480), new Vector2(380, 56), MamaFlowUI.T("←  Back to lobby", "←  กลับล็อบบี้"), 30, Chalk, () => { if (NetSession.I) NetSession.I.AbortLoading(); });
            returnLink.SetActive(false); openedAt = Time.unscaledTime;
            index = Random.Range(0, Keys.Length); ShowTip(); tipPop = 0f;
        }
        public void Next() { index=(index+1)%Keys.Length; ShowTip(); }
        void ShowTip()
        {
            tipTitle.text=Loc.Thai ? TitlesTh[index]:TitlesEn[index]; tipBody.text=Loc.Thai?BodyTh[index]:BodyEn[index];
            photo.texture=Resources.Load<Texture2D>("MamaTips/"+Keys[index]); photo.color=photo.texture?Color.white:MamaTheme.Teal;
            counter.text=(index+1)+" / "+Keys.Length; nextTip=Time.unscaledTime+12; tipPop=0f;
        }
        void Update()
        {
            if(Time.unscaledTime>nextTip) Next();
            float t=Time.unscaledTime;
            // chalk kid: 16-frame loop from the sheet (top row first)
            if(kid&&kid.enabled){int f=(int)(t*KidFps)%KidFrames;int rows=KidFrames/KidCols;kid.uvRect=new Rect((f%KidCols)/(float)KidCols,1f-(f/KidCols+1)/(float)rows,1f/KidCols,1f/rows);}
            // cards drop in / swap with a small bounce, then sway gently
            tipPop=Mathf.Min(1f,tipPop+Time.unscaledDeltaTime*2.6f);
            float e=1f+Mathf.Sin(tipPop*Mathf.PI*2.2f)*(1f-tipPop)*.12f;float k=Mathf.SmoothStep(.85f,1f,tipPop)*e;
            if(noteRt){noteRt.localScale=Vector3.one*k;noteRt.localRotation=Quaternion.Euler(0,0,1.8f+Mathf.Sin(t*1.1f)*.6f);}
            if(polaroidRt)polaroidRt.localRotation=Quaternion.Euler(0,0,-1.5f+Mathf.Sin(t*.8f+1)*.4f);

            progress.rectTransform.anchorMax=new Vector2(Mathf.Max(.03f,Progress),1);
            var net=NetSession.I;
            if(returnLink&&!returnLink.activeSelf&&(Time.unscaledTime-openedAt>StuckSeconds||(NetSession.I&&NetSession.I.HasFlowError))) returnLink.SetActive(true);
            if(dots) dots.text=MamaFlowUI.T("Tidying up","กำลังเก็บกวาด")+new string('.',1+(int)(t*2.5f)%3);
            status.text=net? net.LoadStatus + (NetGame.Online?"   •   "+MamaFlowUI.T("Players ready ","ผู้เล่นพร้อม ")+net.ReadyCount+"/"+NetGame.PlayerCount:"") : "";
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
    }

    /// Hover feedback for text links: grows a little and the underline wiggles.
    public class MamaLinkHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        Text label; RectTransform line; bool over; float k;
        public void Init(Text t, RectTransform l) { label = t; line = l; }
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => over = true;
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => over = false;
        void Update()
        {
            k = Mathf.MoveTowards(k, over ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            transform.localScale = Vector3.one * (1f + k * .08f);
            if (line) line.localRotation = Quaternion.Euler(0, 0, -1f + Mathf.Sin(Time.unscaledTime * 9f) * 2f * k);
        }
    }
}
