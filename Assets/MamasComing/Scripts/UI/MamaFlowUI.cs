using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace MamasComing
{
    public static class MamaFlowUI
    {
        public static string T(string en, string th) => Loc.Thai ? th : en;
        public static Canvas Canvas(string name, Transform parent, int order)
        {
            if (!EventSystem.current) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvas = MamaUIKit.Canvas(name, parent, order);
            canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }
        public static RectTransform Panel(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, Vector2? anchor = null)
        {
            var rt = MamaUIKit.Rect(name, parent, anchor ?? MamaUIKit.CC, MamaUIKit.CC, pos, size);
            MamaUIKit.Img(rt, sprite, color); return rt;
        }
        public static Text Text(Transform parent, string name, Vector2 pos, Vector2 size, string text, int font = 26, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = MamaUIKit.Label(name, parent, MamaUIKit.CC, MamaUIKit.CC, pos, size, text, null, MamaTheme.Body, font, color ?? MamaTheme.WoodInk, align);
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(16, font - 4); t.resizeTextMaxSize = font; return t;
        }
        public static Button Button(Transform parent, string name, Vector2 pos, Vector2 size, string label, Color color, UnityEngine.Events.UnityAction click)
        {
            var rt = Panel(parent,name,pos,size,MamaTheme.PaintS,color);
            rt.GetComponent<Image>().raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>(); b.onClick.AddListener(click);
            var colors = b.colors; colors.highlightedColor = new Color(1f,.91f,.72f); colors.pressedColor = new Color(.7f,.8f,.8f); b.colors=colors;
            Text(rt,"Label",Vector2.zero,size-new Vector2(24,12),label,27,MamaTheme.Cream,TextAnchor.MiddleCenter); return b;
        }
        public static RectTransform VotePanel(Transform parent, Vector2 pos)
        {
            var rt=Panel(parent,"Vote together",pos,new Vector2(790,145),MamaTheme.PlankDarkS,Color.white);
            Text(rt,"VoteHint",new Vector2(0,42),new Vector2(730,32),T("VOTE TOGETHER • majority wins • you can change your vote", "โหวตด้วยกัน • ใช้เสียงข้างมาก • เปลี่ยนคำตอบได้"),21,MamaTheme.Cream,TextAnchor.MiddleCenter);
            Button(rt,"Lobby",new Vector2(-190,-13),new Vector2(345,64),T("LOBBY", "กลับล็อบบี้"),MamaTheme.Teal,()=>NetSession.I.Vote(1));
            Button(rt,"Retry",new Vector2(190,-13),new Vector2(345,64),T("RETRY", "เล่นอีกครั้ง"),MamaTheme.Brick,()=>NetSession.I.Vote(2));
            var status=Text(rt,"Votes",new Vector2(0,-53),new Vector2(740,26),"",19,MamaTheme.Cream,TextAnchor.MiddleCenter);
            rt.gameObject.AddComponent<MamaVoteStatus>().label=status; return rt;
        }
    }
    public class MamaVoteStatus : MonoBehaviour
    {
        public Text label;
        void Update()
        {
            string line="";
            for(int s=0;s<3;s++)
            {
                if (NetGame.Online && NetSession.I && !NetSession.I.SlotTaken(s) || !NetGame.Online && s>0) continue;
                int v=MamaRound.Data.votes[s];
                line += (line.Length>0 ? "    •    " : "")+"P"+(s+1)+": "+ (v==1 ? MamaFlowUI.T("Lobby","ล็อบบี้") : v==2 ? MamaFlowUI.T("Retry","เล่นอีกครั้ง") : MamaFlowUI.T("choosing...","กำลังเลือก..."));
            }
            if(label) label.text=line;
        }
    }
}
