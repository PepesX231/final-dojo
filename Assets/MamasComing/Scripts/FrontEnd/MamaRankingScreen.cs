using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MamasComing
{
    [DefaultExecutionOrder(-100)]
    public class MamaRankingScreen : MonoBehaviour
    {
        public PlayerController player;
        public static PlayerController LocalPlayer { get; private set; }
        Text detail; Transform crown; int selectedSlot; List<MamaScore> ranked;
        public static Vector3 SpawnFor(int slot) => new Vector3(.1f + slot * 2.05f, .35f, .2f);
        void Awake() { LocalPlayer=player; if(player) { player.transform.localScale = Vector3.one * 1.9f; player.isLocal=true; player.WakeUp(); KidLook.Apply(player,KidLook.Selected); } }
        void OnDestroy() { if(LocalPlayer==player) LocalPlayer=null; }
        void Start()
        {
            Loc.Load(); MamaAudio.Ensure(); MamaSettings.Ensure(); NetSession.Ensure();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            ranked=MamaRound.Ranked();
            var c=MamaFlowUI.Canvas("Family scrapbook • results",transform,150);
            var board=MamaFlowUI.Panel(c.transform,"Scoreboard",new Vector2(-622,20),new Vector2(570,850),MamaTheme.PaperS,Color.white);
            MamaTheme.Tape(board,new Vector2(.5f,1),new Vector2(0,5),-4,195);
            MamaFlowUI.Text(board,"Title",new Vector2(0,345),new Vector2(490,95),MamaFlowUI.T("HOUSE HEROES", "ทีมกู้บ้าน"),49,MamaTheme.Teal,TextAnchor.MiddleCenter);
            MamaFlowUI.Text(board,"Subtitle",new Vector2(0,278),new Vector2(490,50),MamaFlowUI.T("EVERY LITTLE JOB HELPED THE TEAM", "ทุกงานเล็ก ๆ ช่วยให้ทีมรอดไปด้วยกัน"),19,null,TextAnchor.MiddleCenter);
            for(int i=0;i<ranked.Count;i++)
            {
                var row=ranked[i]; float y=168-i*126;
                var rt=MamaFlowUI.Panel(board,"P"+(row.slot+1),new Vector2(0,y),new Vector2(510,112),MamaTheme.PaintS,i==0?MamaTheme.Mustard:new Color(.85f,.89f,.81f));
                rt.GetComponent<Image>().raycastTarget=true; int slot=row.slot;
                var b=rt.gameObject.AddComponent<Button>(); b.targetGraphic=rt.GetComponent<Image>(); b.onClick.AddListener(()=>Select(slot));
                MamaFlowUI.Text(rt,"Place",new Vector2(-210,0),new Vector2(65,75),(i+1).ToString("00"),37,null,TextAnchor.MiddleCenter);
                string fruit=Loc.Thai?KidLook.NamesTh[row.fruit]:KidLook.Names[row.fruit];
                MamaFlowUI.Text(rt,"Player",new Vector2(-32,25),new Vector2(265,45),"P"+(row.slot+1)+"  "+fruit,27);
                MamaFlowUI.Text(rt,"Tasks",new Vector2(-32,-25),new Vector2(265,40),row.tasks+MamaFlowUI.T(" jobs completed", " งานที่ทำสำเร็จ"),21);
                MamaFlowUI.Text(rt,"Score",new Vector2(186,8),new Vector2(105,80),row.score.ToString(),38,null,TextAnchor.MiddleCenter);
            }
            MamaFlowUI.Text(board,"Details label",new Vector2(0,-214),new Vector2(485,38),MamaFlowUI.T("SELECT A PLAYER TO SEE THEIR WORK", "กดชื่อผู้เล่นเพื่อดูงานที่ช่วยทำ"),20,MamaTheme.Teal,TextAnchor.MiddleCenter);
            var viewport=MamaUIKit.Rect("Tasks viewport",board,MamaUIKit.CC,MamaUIKit.CC,new Vector2(0,-318),new Vector2(485,158));
            viewport.gameObject.AddComponent<Image>().color=new Color(1,1,1,.15f); viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content=MamaUIKit.Rect("Completed tasks",viewport,MamaUIKit.TC,MamaUIKit.TC,Vector2.zero,new Vector2(470,158));
            detail=content.gameObject.AddComponent<Text>(); detail.font=MamaTheme.Body; detail.fontSize=22; detail.color=MamaTheme.WoodInk; detail.alignment=TextAnchor.UpperLeft; detail.horizontalOverflow=HorizontalWrapMode.Wrap; detail.verticalOverflow=VerticalWrapMode.Overflow;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=35; scroll.movementType=ScrollRect.MovementType.Clamped;
            MamaFlowUI.Text(c.transform,"Celebration",new Vector2(320,395),new Vector2(1070,125),MamaFlowUI.T("THE CLEAN-UP CREW", "แก๊งเก็บบ้าน... รอดแล้ว!"),63,MamaTheme.Cream,TextAnchor.MiddleCenter);
            MamaFlowUI.Text(c.transform,"Move hint",new Vector2(320,-265),new Vector2(1070,75),MamaFlowUI.T("WASD move  •  SPACE jump  •  celebrate together!", "WASD เดิน  •  SPACE กระโดด  •  ฉลองกับเพื่อนได้เลย!"),27,MamaTheme.Cream,TextAnchor.MiddleCenter);
            MamaFlowUI.VotePanel(c.transform,new Vector2(340,-395));
            if(ranked.Count>0) Select(ranked[0].slot);
            CreateCrown(); MamaAudio.Play("win",null,.8f);
        }
        void Select(int slot)
        {
            selectedSlot=slot; var row=ranked.Find(x=>x.slot==slot); if(row==null)return;
            detail.text="P"+(slot+1)+"\n"+(row.completed.Count>0?string.Join("\n",row.completed.ConvertAll(s=>"• "+Loc.T(s))):MamaFlowUI.T("No completed jobs recorded this round.","ยังไม่มีงานที่ทำสำเร็จในรอบนี้"));
            detail.rectTransform.sizeDelta=new Vector2(470,Mathf.Max(158,detail.preferredHeight+12)); detail.rectTransform.anchoredPosition=Vector2.zero;
        }
        void CreateCrown()
        {
            var go=new GameObject("Golden helper crown"); crown=go.transform;
            var mesh=new Mesh(); var verts=new List<Vector3>(); var tris=new List<int>();
            int n=20; for(int i=0;i<n;i++) { float a=i*Mathf.PI*2/n; float y=i%4==0?.36f:.12f; verts.Add(new Vector3(Mathf.Cos(a)*.32f,0,Mathf.Sin(a)*.32f)); verts.Add(new Vector3(Mathf.Cos(a)*.37f,y,Mathf.Sin(a)*.37f)); }
            for(int i=0;i<n;i++){int a=i*2,b=((i+1)%n)*2;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1,a+1,b,a,b+1,b,a+1});}
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=MamaTheme.Mustard;mat.SetFloat("_Metallic",.65f);mat.SetFloat("_Smoothness",.65f);go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }
        void LateUpdate()
        {
            if(player && !(NetSession.I && NetSession.I.Loading))
            {
                player.controlsEnabled=true;
                Vector3 p=player.transform.position;
                if(p.x < -1.2f || p.x > 5.8f || p.z < -1.6f || p.z > 3.5f || p.y < -.5f) player.Motor.Teleport(SpawnFor(NetSession.I?NetSession.I.MySlot:0),Quaternion.Euler(0,180,0));
            }
            if(crown && ranked!=null && ranked.Count>0)
            {
                var winner=NetSession.I?NetSession.I.AvatarAt(ranked[0].slot):player;
                if(winner){float top=winner.transform.position.y;foreach(var renderer in winner.GetComponentsInChildren<Renderer>())if(!(renderer is ParticleSystemRenderer))top=Mathf.Max(top,renderer.bounds.max.y);crown.position=new Vector3(winner.transform.position.x,top+.2f+Mathf.Sin(Time.unscaledTime*2)*.06f,winner.transform.position.z); crown.Rotate(0,Time.unscaledDeltaTime*28,0,Space.World);}
            }
        }
    }
}
