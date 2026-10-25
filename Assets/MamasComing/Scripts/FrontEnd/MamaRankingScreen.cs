using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MamasComing
{
    /// Results stage: everyone stands on the podium (1st centre, 2nd left, 3rd right), can still walk around,
    /// and the left card lists score + completed jobs. Votes Lobby / Retry like before.
    [DefaultExecutionOrder(-100)]
    public class MamaRankingScreen : MonoBehaviour
    {
        public PlayerController player;
        [Tooltip("Decor fruits cheering in the background; any fruit a player is using is hidden.")]
        public Transform crowd;
        public static PlayerController LocalPlayer { get; private set; }
        /// Top of each podium pouf (rank 0 centre, 1 left, 2 right). The stage builder uses the same numbers.
        public static readonly Vector3[] Podium = { new Vector3(0f, 0.72f, 0f), new Vector3(-2.05f, 0.42f, 0.3f), new Vector3(2.05f, 0.26f, 0.4f) };

        class Bar { public int slot; public RectTransform row, fill; public Text score; public Image ring; public float shown, target, pop; }
        readonly List<Bar> bars = new List<Bar>();
        readonly List<MamaJobLog> replay = new List<MamaJobLog>();
        Text tickerText; float maxScore = 1, replayT; int replayIndex; bool replayDone;
        static Color SlotColor(int slot) => slot == 0 ? new Color(.95f, .55f, .3f) : slot == 1 ? new Color(.3f, .68f, .64f) : new Color(.62f, .5f, .85f);

        void BuildReplay()
        {
            replay.Clear();
            if (MamaRound.Data.log != null && MamaRound.Data.log.Count > 0) replay.AddRange(MamaRound.Data.log);
            else
            {
                // older data without a log: interleave each player's jobs with an even share of their score
                int most = 0; foreach (var r in ranked) most = Mathf.Max(most, r.completed.Count);
                for (int k = 0; k < most; k++) foreach (var r in ranked) if (k < r.completed.Count) replay.Add(new MamaJobLog { slot = r.slot, task = r.completed[k], points = r.completed.Count > 0 ? r.score / r.completed.Count + (k == 0 ? r.score % r.completed.Count : 0) : 0 });
            }
            replayT = 1.0f; replayIndex = 0; replayDone = replay.Count == 0;
            if (replayDone) FinishReplay();
        }

        void FinishReplay()
        {
            replayDone = true;
            foreach (var b in bars) { var r = ranked.Find(x => x.slot == b.slot); if (r != null) b.target = r.score; }
            if (tickerText) tickerText.text = ranked.Count > 0 ? MamaFlowUI.T("MVP: P", "ตัวท็อป: P") + (ranked[0].slot + 1) + "  •  " + ranked[0].score + MamaFlowUI.T(" pts", " คะแนน") : "";
            if (ranked.Count > 0) { var top = bars.Find(b => b.slot == ranked[0].slot); if (top != null) top.pop = 1f; }
            MamaAudio.Play("good", null, .7f);
        }

        void TickReplay(float dt)
        {
            foreach (var b in bars)
            {
                b.shown = Mathf.MoveTowards(b.shown, b.target, Mathf.Max(20f, maxScore) * dt * 1.6f);
                b.fill.sizeDelta = new Vector2(Mathf.Max(30f, 380f * b.shown / Mathf.Max(1f, maxScore)), 30); b.fill.gameObject.SetActive(b.shown > .5f);
                b.score.text = Mathf.RoundToInt(b.shown).ToString();
                b.pop = Mathf.MoveTowards(b.pop, 0f, dt * 2.5f);
                b.row.localScale = Vector3.one * (1f + Mathf.Sin(b.pop * Mathf.PI) * .05f);
            }
            if (replayDone) return;
            replayT -= dt; if (replayT > 0f) return;
            if (replayIndex >= replay.Count) { FinishReplay(); return; }
            var e = replay[replayIndex++];
            var bar = bars.Find(x => x.slot == e.slot);
            if (bar != null) { bar.target += e.points; bar.pop = 1f; }
            var who = ranked.Find(x => x.slot == e.slot);
            string fruit = who != null ? (Loc.Thai ? KidLook.NamesTh[who.fruit] : KidLook.Names[who.fruit]) : "";
            if (tickerText) tickerText.text = "P" + (e.slot + 1) + " " + fruit + "  •  " + Loc.T(e.task) + "   +" + e.points;
            MamaAudio.Play("pop", null, .45f, 1f + Mathf.Min(.6f, replayIndex * .03f));
            replayT = Mathf.Clamp(5f / Mathf.Max(1, replay.Count), .28f, .75f);
        }

        Text detail, detailTitle; Transform crown; RectTransform youBadge, title; List<MamaScore> ranked;
        readonly List<(int slot, Image bg, Image ring)> rows = new List<(int, Image, Image)>();
        float shownAt;

        static readonly Color Ink = new Color(.42f, .25f, .16f), CardCream = new Color(1f, .97f, .91f), Gold = new Color(1f, .78f, .25f), YouGreen = new Color(.36f, .7f, .36f);

        public static Vector3 SpawnFor(int slot)
        {
            var r = MamaRound.Ranked(); int rank = r.FindIndex(x => x.slot == slot); if (rank < 0) rank = slot;
            var p = Podium[Mathf.Clamp(rank, 0, Podium.Length - 1)]; return p + Vector3.up * .05f;
        }
        void Awake() { LocalPlayer=player; if(player) { player.transform.localScale = Vector3.one * 1.9f; var vis = player.fallPivot ? player.fallPivot.Find("Visual") : null; if (vis) vis.localPosition = Vector3.zero; /* the scene copy had the model lifted 0.33 - the kid hovered above the podium */ player.isLocal=true; player.WakeUp(); KidLook.Apply(player,KidLook.Selected); } }
        void OnDestroy() { if(LocalPlayer==player) LocalPlayer=null; }

        // ------------------------------------------------------------------ UI helpers
        static Sprite Round => MamaTheme.Sprite("ui_round");
        static Sprite RoundSoft => MamaTheme.Sprite("ui_round_soft");
        static Sprite Circle => MamaTheme.Sprite("ui_circle");
        static Image Rounded(RectTransform rt, Color c, Sprite s = null, float corner = 1f)
        {
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = s ? s : Round; im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = corner; im.color = c; im.raycastTarget = false; return im;
        }
        static RectTransform Card(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color c, float corner = 1f, bool shadow = true)
        {
            if (shadow) { var sh = MamaUIKit.Rect(name + " shadow", parent, anchor, MamaUIKit.CC, pos + new Vector2(6, -12), size + new Vector2(24, 24)); Rounded(sh, new Color(.4f, .16f, .04f, .22f), RoundSoft, corner); }
            var rt = MamaUIKit.Rect(name, parent, anchor, MamaUIKit.CC, pos, size); Rounded(rt, c, null, corner); return rt;
        }
        static Text Label(Transform parent, string name, Vector2 pos, Vector2 size, string s, int font, Color c, TextAnchor a = TextAnchor.MiddleLeft, Font f = null)
        {
            var t = MamaFlowUI.Text(parent, name, pos, size, s, font, c, a); if (f) t.font = f; t.raycastTarget = false; return t;
        }
        Button PillButton(Transform parent, string name, Vector2 pos, Vector2 size, string s, Color c, UnityEngine.Events.UnityAction click)
        {
            var rt = Card(parent, name, MamaUIKit.CC, pos, size, c, 3f, false);
            var im = rt.GetComponent<Image>(); im.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = im; b.onClick.AddListener(click);
            var cols = b.colors; cols.highlightedColor = new Color(1.1f, 1.1f, 1.1f); cols.pressedColor = new Color(.8f, .8f, .8f); b.colors = cols;
            Label(rt, "Label", new Vector2(0, 2), size - new Vector2(20, 8), s, 30, Color.white, TextAnchor.MiddleCenter, MamaTheme.Bold);
            return b;
        }

        void Start()
        {
            Loc.Load(); MamaAudio.Ensure(); MamaSettings.Ensure(); NetSession.Ensure();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            ranked=MamaRound.Ranked();
            if (crowd) foreach (var pk in crowd.GetComponentsInChildren<MamaPosedKid>(true)) pk.gameObject.SetActive(!ranked.Exists(r => r.fruit == pk.kid));
            BuildUI(); CreateCrown(); MamaAudio.Play("win",null,.8f); shownAt = Time.unscaledTime;
        }

        void BuildUI()
        {
            var c=MamaFlowUI.Canvas("Results • party podium",transform,150);
            int mySlot = NetSession.I && NetGame.Online ? NetSession.I.MySlot : 0;

            // ---------------------------------------------------------- left: ranking card
            var card = Card(c.transform, "Ranking card", new Vector2(0, .5f), new Vector2(380, 0), new Vector2(640, 960), CardCream, 2.6f);
            Label(card, "Heading", new Vector2(-40, 410), new Vector2(480, 100), MamaFlowUI.T("RANKING", "อันดับทีม"), 64, Ink, TextAnchor.MiddleLeft, MamaTheme.Display);
            var crownChip = Card(card, "Cleared chip", MamaUIKit.CC, new Vector2(215, 414), new Vector2(150, 56), Gold, 4f, false);
            Label(crownChip, "Text", Vector2.zero, new Vector2(140, 50), (MamaRound.Data == null || MamaRound.Data.grade <= 1 ? MamaFlowUI.T("CLEARED!", "รอดแล้ว!") : MamaRound.Data.grade == 2 ? MamaFlowUI.T("PHEW...", "เฉียดฉิว") : MamaRound.Data.grade == 3 ? MamaFlowUI.T("CAUGHT!", "โดนจับ!") : MamaFlowUI.T("GAME OVER", "เกมโอเวอร์")), 24, Ink, TextAnchor.MiddleCenter, MamaTheme.Bold);

            // ---- quest ticker + one bar per player; the bars grow job by job (replay of the round)
            var tick = Card(card, "Quest ticker", MamaUIKit.CC, new Vector2(0, 318), new Vector2(580, 70), new Color(.25f, .2f, .24f), 4f, false);
            tickerText = Label(tick, "Text", new Vector2(10, 0), new Vector2(540, 60), MamaFlowUI.T("Counting the jobs...", "กำลังนับงานที่ทำ..."), 24, new Color(1f, .93f, .78f), TextAnchor.MiddleLeft, MamaTheme.Bold);
            float y = 222;
            maxScore = 1; foreach (var r in ranked) maxScore = Mathf.Max(maxScore, r.score);
            for (int i = 0; i < ranked.Count; i++)
            {
                var row = ranked[i];
                var ring = MamaUIKit.Rect("P" + (row.slot + 1) + " select ring", card, MamaUIKit.CC, MamaUIKit.CC, new Vector2(0, y), new Vector2(592, 112));
                var ringIm = Rounded(ring, new Color(0, 0, 0, 0), null, 2.6f);
                var rt = Card(card, "P" + (row.slot + 1), MamaUIKit.CC, new Vector2(0, y), new Vector2(580, 100), Color.white, 3f, false);
                var bg = rt.GetComponent<Image>(); bg.raycastTarget = true; int slot = row.slot;
                var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = bg; b.onClick.AddListener(() => Select(slot));
                var face = MamaUIKit.Rect("Portrait", rt, MamaUIKit.CC, MamaUIKit.CC, new Vector2(-240, 0), new Vector2(76, 76));
                var faceBg = face.gameObject.AddComponent<Image>(); faceBg.sprite = Circle; faceBg.color = SlotColor(row.slot) * .35f + new Color(.65f, .65f, .65f, .65f); faceBg.raycastTarget = false;
                var pic = MamaUIKit.Rect("Face", face, MamaUIKit.CC, MamaUIKit.CC, Vector2.zero, new Vector2(76, 76));
                var portrait = Resources.Load<Sprite>("MamaUI/Portraits/fruit_" + row.fruit);
                var pim = pic.gameObject.AddComponent<Image>(); pim.sprite = portrait; pim.preserveAspect = true; pim.raycastTarget = false; pim.enabled = portrait;
                string fruit = Loc.Thai ? KidLook.NamesTh[row.fruit] : KidLook.Names[row.fruit];
                Label(rt, "Player", new Vector2(-40, 26), new Vector2(300, 36), "P" + (row.slot + 1) + "  " + fruit, 26, Ink, TextAnchor.MiddleLeft, MamaTheme.Bold);
                if (row.slot == mySlot)
                {
                    var you = Card(rt, "You tag", MamaUIKit.CC, new Vector2(150, 28), new Vector2(66, 30), YouGreen, 6f, false);
                    Label(you, "Text", Vector2.zero, new Vector2(62, 28), MamaFlowUI.T("YOU", "คุณ"), 17, Color.white, TextAnchor.MiddleCenter, MamaTheme.Bold);
                }
                // bar
                var track = Card(rt, "Bar track", MamaUIKit.CC, new Vector2(10, -18), new Vector2(380, 30), new Color(.92f, .88f, .82f), 6f, false);
                var fill = MamaUIKit.Rect("Fill", track, new Vector2(0, .5f), MamaUIKit.CC, Vector2.zero, new Vector2(0, 30)); fill.pivot = new Vector2(0, .5f);
                var fillIm = Rounded(fill, SlotColor(row.slot), null, 6f);
                var score = Label(rt, "Score", new Vector2(250, -4), new Vector2(80, 60), "0", 34, Ink, TextAnchor.MiddleRight, MamaTheme.Display);
                bars.Add(new Bar { slot = row.slot, row = rt, fill = fill, score = score, ring = ringIm });
                rows.Add((row.slot, bg, ringIm));
                y -= 118;
            }
            BuildReplay();

            // jobs of the selected player
            var jobs = Card(card, "Jobs", MamaUIKit.CC, new Vector2(0, -232), new Vector2(580, 190), new Color(.96f, .92f, .84f), 3f, false);
            detailTitle = Label(jobs, "Jobs title", new Vector2(0, 70), new Vector2(540, 40), MamaFlowUI.T("Tap a player to see their jobs", "กดที่ชื่อเพื่อดูงานที่ช่วยทำ"), 22, new Color(.2f, .55f, .52f), TextAnchor.MiddleLeft, MamaTheme.Bold);
            var viewport=MamaUIKit.Rect("Tasks viewport",jobs,MamaUIKit.CC,MamaUIKit.CC,new Vector2(0,-18),new Vector2(540,124));
            viewport.gameObject.AddComponent<Image>().color=new Color(1,1,1,.01f); viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content=MamaUIKit.Rect("Completed tasks",viewport,MamaUIKit.TC,MamaUIKit.TC,Vector2.zero,new Vector2(530,150));
            detail=content.gameObject.AddComponent<Text>(); detail.font=MamaTheme.Body; detail.fontSize=22; detail.color=Ink; detail.alignment=TextAnchor.UpperLeft; detail.horizontalOverflow=HorizontalWrapMode.Wrap; detail.verticalOverflow=VerticalWrapMode.Overflow; detail.lineSpacing=1.05f;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.scrollSensitivity=35; scroll.movementType=ScrollRect.MovementType.Clamped;

            // vote: two rounded pills + who voted what
            Label(card, "Vote hint", new Vector2(0, -364), new Vector2(580, 30), MamaFlowUI.T("Vote together • majority wins", "โหวตด้วยกัน • เสียงข้างมากชนะ"), 20, new Color(Ink.r, Ink.g, Ink.b, .75f), TextAnchor.MiddleCenter);
            PillButton(card, "Lobby", new Vector2(-148, -412), new Vector2(276, 66), MamaFlowUI.T("LOBBY", "กลับล็อบบี้"), MamaTheme.Teal, () => NetSession.I.Vote(1));
            PillButton(card, "Retry", new Vector2(148, -412), new Vector2(276, 66), MamaFlowUI.T("PLAY AGAIN", "เล่นอีกครั้ง"), new Color(.93f, .42f, .36f), () => NetSession.I.Vote(2));
            var votes = Label(card, "Votes", new Vector2(0, -460), new Vector2(580, 28), "", 18, new Color(Ink.r, Ink.g, Ink.b, .7f), TextAnchor.MiddleCenter);
            votes.gameObject.AddComponent<MamaVoteStatus>().label = votes;

            // ---------------------------------------------------------- right: celebration title + hint + YOU marker
            title = MamaUIKit.Rect("Celebration", c.transform, MamaUIKit.TC, MamaUIKit.CC, new Vector2(330, -110), new Vector2(1100, 130), -2f);
            var tt = MamaUIKit.Txt(title, OutcomeTitle(), null, MamaTheme.Display, 86, new Color(1f, .97f, .88f), TextAnchor.MiddleCenter, 5f, OutcomeColor());
            tt.resizeTextForBestFit = true; tt.resizeTextMinSize = 40; tt.resizeTextMaxSize = 86;
            // the round in one line - always shown, win or lose: what we fixed vs what Mom caught
            var rd = MamaRound.Data;
            if (rd != null)
            {
                var sum = Label(c.transform, "Outcome summary", new Vector2(330, 338), new Vector2(1100, 50),
                    MamaFlowUI.T("Fixed " + rd.fixedN + "   •   Mom found " + rd.foundN + "   •   Hidden " + rd.hiddenN, "แก้สำเร็จ " + rd.fixedN + "   •   แม่เจอ " + rd.foundN + "   •   ซ่อนเนียน " + rd.hiddenN),
                    32, new Color(1f, .97f, .9f), TextAnchor.MiddleCenter, MamaTheme.Bold);
                var so = sum.gameObject.AddComponent<Outline>(); so.effectColor = new Color(.4f, .18f, .1f, .85f); so.effectDistance = new Vector2(2, -2);
                if (rd.foundTop != null && rd.foundTop.Count > 0)
                {
                    var caught = Label(c.transform, "Outcome caught", new Vector2(330, 294), new Vector2(1100, 40),
                        MamaFlowUI.T("Mom saw: ", "แม่เห็น: ") + string.Join(",  ", rd.foundTop.ConvertAll(x => x.Length > 26 ? x.Substring(0, 24).TrimEnd() + "..." : x).GetRange(0, Mathf.Min(3, rd.foundTop.Count))) + (rd.foundN > 3 ? MamaFlowUI.T("  +" + (rd.foundN - 3) + " more", "  +อีก " + (rd.foundN - 3) + " อย่าง") : ""),
                        24, new Color(1f, .9f, .8f), TextAnchor.MiddleCenter, MamaTheme.Bold);
                    caught.resizeTextForBestFit = true; caught.resizeTextMinSize = 18; caught.resizeTextMaxSize = 26;
                    var co = caught.gameObject.AddComponent<Outline>(); co.effectColor = new Color(.4f, .18f, .1f, .85f); co.effectDistance = new Vector2(2, -2);
                }
            }
            var hint = Label(c.transform, "Move hint", new Vector2(330, -470), new Vector2(1000, 44), MamaFlowUI.T("WASD move  •  SPACE jump  •  celebrate together!", "WASD เดิน  •  SPACE กระโดด  •  ฉลองกับเพื่อนได้เลย!"), 26, new Color(1f, .97f, .9f, .9f), TextAnchor.MiddleCenter, MamaTheme.Bold);
            var hs = hint.gameObject.AddComponent<Outline>(); hs.effectColor = new Color(.55f, .25f, .1f, .8f); hs.effectDistance = new Vector2(2, -2);
            youBadge = Card(c.transform, "You marker", MamaUIKit.BL, Vector2.zero, new Vector2(84, 44), YouGreen, 4f, false);
            youBadge.pivot = new Vector2(.5f, 0);
            Label(youBadge, "Text", Vector2.zero, new Vector2(80, 40), MamaFlowUI.T("YOU", "คุณ"), 24, Color.white, TextAnchor.MiddleCenter, MamaTheme.Bold);
            var arrow = MamaUIKit.Rect("Arrow", youBadge, MamaUIKit.BC, MamaUIKit.CC, new Vector2(0, -6), new Vector2(18, 18), 45f); Rounded(arrow, YouGreen, null, 6f);

            if(ranked.Count>0) Select(ranked[0].slot);
        }

        static string OutcomeTitle()
        {
            var d = MamaRound.Data; int g = d == null ? 0 : d.grade;
            switch (g)
            {
                case 0: return MamaFlowUI.T("PERFECT ALIBI!", "เนียนสุดๆ! แม่ไม่รู้เลย");
                case 1: return MamaFlowUI.T("ALMOST CLEAN!", "เกือบเนียนแล้ว!");
                case 2: return MamaFlowUI.T("MOM IS SUSPICIOUS...", "แม่เริ่มสงสัยแล้ว...");
                case 3: return MamaFlowUI.T("BUSTED!", "โดนแม่จับได้!");
                default: return MamaFlowUI.T("GAME OVER!", "เกมโอเวอร์!");
            }
        }
        static Color OutcomeColor()
        {
            var d = MamaRound.Data; int g = d == null ? 0 : d.grade;
            return g <= 1 ? new Color(.12f, .45f, .40f) : g == 2 ? new Color(.62f, .38f, .08f) : new Color(.62f, .14f, .12f);
        }

        void Select(int slot)
        {
            var row=ranked.Find(x=>x.slot==slot); if(row==null)return;
            foreach (var r in rows) { r.ring.color = r.slot == slot ? new Color(.2f, .62f, .59f, 1f) : new Color(0, 0, 0, 0); }
            string fruit = Loc.Thai ? KidLook.NamesTh[row.fruit] : KidLook.Names[row.fruit];
            detailTitle.text = MamaFlowUI.T("Jobs by P", "งานของ P") + (slot + 1) + " • " + fruit;
            detail.text=row.completed.Count>0?string.Join("\n",row.completed.ConvertAll(s=>"•  "+Loc.T(s))):MamaFlowUI.T("No completed jobs recorded this round.","ยังไม่มีงานที่ทำสำเร็จในรอบนี้");
            detail.rectTransform.sizeDelta=new Vector2(530,Mathf.Max(150,detail.preferredHeight+12)); detail.rectTransform.anchoredPosition=Vector2.zero;
        }

        void CreateCrown()
        {
            var go=new GameObject("Golden helper crown"); crown=go.transform;
            var mesh=new Mesh(); var verts=new List<Vector3>(); var tris=new List<int>();
            int n=20; for(int i=0;i<n;i++) { float a=i*Mathf.PI*2/n; float y=i%4==0?.36f:.12f; verts.Add(new Vector3(Mathf.Cos(a)*.32f,0,Mathf.Sin(a)*.32f)); verts.Add(new Vector3(Mathf.Cos(a)*.37f,y,Mathf.Sin(a)*.37f)); }
            for(int i=0;i<n;i++){int a=i*2,b=((i+1)%n)*2;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1,a+1,b,a,b+1,b,a+1});}
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(1f,.76f,.2f));mat.SetFloat("_Metallic",.75f);mat.SetFloat("_Smoothness",.72f);
            mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(.45f,.3f,.05f));
            go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        }

        // skinned meshes report loose bounds, so the real (vertex) height is measured now and then and cached
        readonly Dictionary<PlayerController, (float h, float at)> heights = new Dictionary<PlayerController, (float, float)>();
        float TopOf(PlayerController pc)
        {
            if (!heights.TryGetValue(pc, out var e) || Time.unscaledTime - e.at > 1.5f)
            {
                // rigid parts (head, stem, outline shells) give an honest top; skinned / baked bounds came out too high
                float top = float.MinValue;
                foreach (var r in pc.GetComponentsInChildren<MeshRenderer>()) if (r.enabled && r.gameObject.activeInHierarchy && !r.name.Contains("Blob")) top = Mathf.Max(top, r.bounds.max.y);
                if (top == float.MinValue) top = MamaPosedKid.TightBounds(pc.transform).max.y;
                e = (top - pc.transform.position.y, Time.unscaledTime); heights[pc] = e;
            }
            return pc.transform.position.y + e.h;
        }

        void LateUpdate()
        {
            foreach (var pc in PlayerController.AllPlayers) if (pc && !pc.GetComponent<MamaBlobShadow>()) pc.gameObject.AddComponent<MamaBlobShadow>();   // contact shadows: kids read as standing on the podium
            if(player && !(NetSession.I && NetSession.I.Loading))
            {
                player.controlsEnabled=true;
                Vector3 p=player.transform.position;
                if(p.x < -7f || p.x > 7f || p.z < -4.5f || p.z > 3.3f || p.y < -.5f) player.Motor.Teleport(SpawnFor(NetSession.I?NetSession.I.MySlot:0),Quaternion.Euler(0,180,0));
            }
            float t = Time.unscaledTime;
            TickReplay(Time.unscaledDeltaTime);
            if(crown && ranked!=null && ranked.Count>0)
            {
                var winner=NetSession.I && NetGame.Online?NetSession.I.AvatarAt(ranked[0].slot):player;
                if(winner){crown.position=new Vector3(winner.transform.position.x,TopOf(winner)-.1f+Mathf.Sin(t*2)*.03f,winner.transform.position.z); crown.Rotate(0,Time.unscaledDeltaTime*40,0,Space.World);}
            }
            // "YOU" marker over the local kid
            var cam = Camera.main;
            if (youBadge && player && cam)
            {
                var sp = cam.WorldToScreenPoint(new Vector3(player.transform.position.x, TopOf(player) + .85f, player.transform.position.z));
                var canvas = youBadge.GetComponentInParent<Canvas>().transform as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sp, null, out var local);
                youBadge.anchorMin = youBadge.anchorMax = new Vector2(.5f, .5f);
                youBadge.anchoredPosition = local + Vector2.up * Mathf.Sin(t * 3f) * 6f; youBadge.gameObject.SetActive(sp.z > 0);
            }
            // title pops in, then breathes
            if (title) { float k = Mathf.Clamp01((t - shownAt) * 2.2f); float e = 1f + Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * .25f; title.localScale = Vector3.one * Mathf.Lerp(.3f, 1f, k) * e * (1f + Mathf.Sin(t * 2f) * .015f); }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// Test hook (play mode): fake a three-player result and rebuild the screen.
        public static void LogTops()
        {
            var pc = LocalPlayer; if (!pc) return;
            foreach (var r in pc.GetComponentsInChildren<Renderer>())
                if (r.enabled && !r.forceRenderingOff) Debug.Log("[Ranking] r " + r.name + " " + r.GetType().Name + " maxY=" + r.bounds.max.y.ToString("F2") + " active=" + r.gameObject.activeInHierarchy + " shadowMode=" + r.shadowCastingMode);
            Debug.Log("[Ranking] pc y=" + pc.transform.position.y.ToString("F2") + " tight=" + MamaPosedKid.TightBounds(pc.transform).max.y.ToString("F2"));
        }
        public static void Preview()
        {
            var d = new MamaRoundData { ended = true, won = true };
            d.players.Add(new MamaScore { slot = 0, fruit = KidLook.Selected, score = 420, tasks = 6, completed = new List<string> { "Put out the oil fire", "Fixed the leaking pipe", "Mopped the hallway", "Hid the burnt pot", "Threw away rotten food", "Reset the breaker" } });
            d.players.Add(new MamaScore { slot = 1, fruit = (KidLook.Selected + 2) % KidLook.Count, score = 310, tasks = 4, completed = new List<string> { "Closed the tap", "Mopped the kitchen" } });
            d.players.Add(new MamaScore { slot = 2, fruit = (KidLook.Selected + 3) % KidLook.Count, score = 150, tasks = 2, completed = new List<string> { "Opened the window" } });
            foreach (var p in d.players) for (int i = 0; i < p.completed.Count; i++) d.log.Add(new MamaJobLog { slot = p.slot, task = p.completed[i], points = p.score / p.completed.Count });
            d.log.Sort((a, b) => (a.task.Length * 7 + a.slot * 3) % 11 - (b.task.Length * 7 + b.slot * 3) % 11);
            MamaRound.Data = d;
            UnityEngine.SceneManagement.SceneManager.LoadScene(MamaLaunch.RankingScene);
        }
#endif
    }
}
