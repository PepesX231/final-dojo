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
        void Awake()
        {
            var c=MamaFlowUI.Canvas("Loading",transform,1000);
            MamaUIKit.Img(MamaUIKit.Stretch("Background",c.transform),null,new Color(.085f,.15f,.16f),raycast:true);
            MamaFlowUI.Text(c.transform,"Eyebrow",new Vector2(0,475),new Vector2(1550,50),"MAMA'S COMING!",32,MamaTheme.Mustard,TextAnchor.MiddleCenter);
            MamaFlowUI.Text(c.transform,"Heading",new Vector2(0,390),new Vector2(1550,80),MamaFlowUI.T("ONE LAST CLEAN-UP BEFORE MOM GETS HOME", "เตรียมช่วยกันเก็บบ้าน ก่อนแม่กลับมา"),42,MamaTheme.Cream,TextAnchor.MiddleCenter);
            var frame=MamaFlowUI.Panel(c.transform,"Polaroid",new Vector2(-310,10),new Vector2(920,640),MamaTheme.PaperS,Color.white);
            frame.localRotation=Quaternion.Euler(0,0,-2);
            MamaTheme.Tape(frame,new Vector2(.5f,1),Vector2.zero,5,200);
            var pic=MamaUIKit.Rect("Problem photo",frame,MamaUIKit.CC,MamaUIKit.CC,new Vector2(0,35),new Vector2(850,475));
            photo=pic.gameObject.AddComponent<RawImage>(); photo.raycastTarget=false;
            tipTitle=MamaFlowUI.Text(frame,"Caption",new Vector2(0,-245),new Vector2(850,80),"",30,MamaTheme.Teal,TextAnchor.MiddleCenter);
            var note=MamaFlowUI.Panel(c.transform,"Tip note",new Vector2(500,25),new Vector2(620,610),MamaTheme.PaperS,new Color(1f,.91f,.58f));
            note.localRotation=Quaternion.Euler(0,0,1.5f); MamaTheme.Tape(note,new Vector2(.5f,1),Vector2.zero,-7,160);
            MamaFlowUI.Text(note,"Tip label",new Vector2(0,225),new Vector2(510,60),MamaFlowUI.T("A NOTE FOR THE CREW", "โน้ตฝากถึงทีมเก็บบ้าน"),32);
            tipBody=MamaFlowUI.Text(note,"Tip",new Vector2(0,20),new Vector2(510,300),"",29);
            MamaFlowUI.Button(note,"Next tip",new Vector2(0,-205),new Vector2(370,75),MamaFlowUI.T("NEXT TIP  >", "คำแนะนำถัดไป  >"),MamaTheme.Teal,Next);
            counter=MamaFlowUI.Text(note,"Counter",new Vector2(0,-270),new Vector2(500,35),"",20,null,TextAnchor.MiddleCenter);
            var track=MamaFlowUI.Panel(c.transform,"Progress track",new Vector2(0,-380),new Vector2(1480,16),null,new Color(.25f,.34f,.33f));
            progress=MamaUIKit.Img(MamaUIKit.Stretch("Progress",track),null,MamaTheme.Mustard);
            status=MamaFlowUI.Text(c.transform,"Status",new Vector2(0,-435),new Vector2(1480,65),"",24,MamaTheme.Cream,TextAnchor.MiddleCenter);
            MamaFlowUI.Button(c.transform,"Return",new Vector2(730,-502),new Vector2(330,48),MamaFlowUI.T("RETURN TO LOBBY", "กลับล็อบบี้"),MamaTheme.Brick,()=>{ if(NetSession.I) NetSession.I.AbortLoading(); });
            index=Random.Range(0,Keys.Length); ShowTip();
        }
        public void Next() { index=(index+1)%Keys.Length; ShowTip(); }
        void ShowTip()
        {
            tipTitle.text=Loc.Thai ? TitlesTh[index]:TitlesEn[index]; tipBody.text=Loc.Thai?BodyTh[index]:BodyEn[index];
            photo.texture=Resources.Load<Texture2D>("MamaTips/"+Keys[index]); photo.color=photo.texture?Color.white:MamaTheme.Teal;
            counter.text=(index+1)+" / "+Keys.Length; nextTip=Time.unscaledTime+12;
        }
        void Update()
        {
            if(Time.unscaledTime>nextTip) Next();
            progress.rectTransform.anchorMax=new Vector2(Mathf.Max(.025f,Progress),1);
            var net=NetSession.I;
            status.text=net? net.LoadStatus + "  •  " + Mathf.RoundToInt(Progress*100)+"%" + (NetGame.Online?"  •  "+MamaFlowUI.T("Players ready ","ผู้เล่นพร้อม ")+net.ReadyCount+"/"+NetGame.PlayerCount:"") : "";
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
    }
}
