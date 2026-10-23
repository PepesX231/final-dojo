using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// Runs the party-disaster round: starts problems on a schedule, tracks the neighbours' patience,
    /// shows the quest board (most urgent first) and the room warning icons.
    [DefaultExecutionOrder(200)]
    public class QuestDirector : MonoBehaviour
    {
        public static QuestDirector I { get; private set; }
        public float Elapsed { get; private set; }
        public float Neighbor;              // 0..100: at 100 the neighbour calls Mom -> she comes home a minute earlier
        int neighborCalls;
        public int FixedCount { get; private set; }

        /// Fewer players = problems escalate a bit slower.
        public float Pace => NetGame.PlayerCount <= 1 ? 0.72f : NetGame.PlayerCount == 2 ? 0.9f : 1f;

        GameManager gm;
        static string T(string en, string th) => Loc.Thai ? th : en;

        void Awake() { I = this; gm = GetComponent<GameManager>(); }
        void OnDestroy() { if (I == this) I = null; }

        void Start() { BuildHud(); }

        void Update()
        {
            if (!gm) return;
            if (gm.state != GameManager.State.Playing) return;
            float dt = Time.deltaTime;
            if (NetGame.IsAuthority)
            {
                Elapsed += dt;
                foreach (var h in Hazard.All.ToArray())
                    if (h && !h.Active && !h.Fixed && h.startAt >= 0f && Elapsed >= h.startAt * (1f / Mathf.Max(0.6f, Pace)) * 0.85f) h.Activate();

                // neighbours: loud music, smoke alarm and visible flames annoy them
                float add = 0f;
                foreach (var h in Hazard.All) if (h is SpeakerHazard sp && sp.Urgent && sp.Powered) add += 1.1f;
                if (gm.alarm && gm.alarm.Beeping) add += 0.9f;
                foreach (var f in FireSource.All) if (f && f.burning && f.intensity > 0.5f) add += 0.35f;
                Neighbor = Mathf.Clamp(Neighbor + (add > 0f ? add * Pace : -0.4f) * dt, 0f, 100f);
                if (Neighbor >= 100f && neighborCalls < 2)
                {
                    neighborCalls++; Neighbor = 35f;
                    gm.CutTime(60f);
                    gm.ui.Call(T("The neighbour called Mom about the NOISE! She's coming home 1 minute EARLIER!", "ข้างบ้านโทรฟ้องแม่เรื่องเสียงดัง! แม่จะกลับเร็วขึ้น 1 นาที!"), MamaUI.Red);
                    MamaAudio.Play("ring", null, 0.8f);
                    if (gm.cam) gm.cam.Shake(0.5f);
                }
            }
        }

        // ------------------------------------------------------------------ events
        public void Alert(Hazard h, string en, string th)
        {
            if (!gm) return;
            gm.ui.Call(T(en, th ?? en), MamaUI.Orange);
            MamaAudio.Play("bad", null, 0.35f, 1.3f);
            NetGame.Broadcast(NetGame.Msg.Call, en, th, 1);
        }
        public void Alarm(Hazard h, string en, string th)
        {
            if (!gm) return;
            gm.ui.Call(T(en, th ?? en), MamaUI.Red);
            if (gm.cam) gm.cam.Shake(0.5f);
            MamaAudio.Play("bad", null, 0.6f, 0.8f);
            NetGame.Broadcast(NetGame.Msg.Call, en, th, 2);
        }
        public void Cheer(string en, string th)
        {
            if (gm) gm.Toast(T(en, th));
            NetGame.Broadcast(NetGame.Msg.Toast, en, th, 0);
        }
        public void OnFixed(Hazard h, PlayerController p)
        {
            FixedCount++;
            MamaAudio.Play("good", null, 0.75f);
            string who = p ? NetGame.NameOf(p) : null;
            string en = (who != null ? who + ": " : "") + (string.IsNullOrEmpty(h.doneEn) ? "Fixed!" : h.doneEn);
            string th = (who != null ? who + ": " : "") + (string.IsNullOrEmpty(h.doneTh) ? "เรียบร้อย!" : h.doneTh);
            Cheer(en, th);
        }

        // ------------------------------------------------------------------ quest board data
        public struct Entry { public string text; public int room; public float urgency; public Color col; public bool coop; public float progress; }
        readonly List<Entry> entries = new List<Entry>();
        float[] roomWater; float waterScanT;

        public List<Entry> Entries()
        {
            entries.Clear();
            void Add(string en, string th, Vector3 at, float u, Color c, float prog = -1f, bool coop = false)
                => entries.Add(new Entry { text = T(en, th), room = GameManager.RoomOf(at), urgency = u, col = c, progress = prog, coop = coop });

            foreach (var h in Hazard.All)
            {
                if (!h || !h.Urgent) continue;
                if (h is BreakerPanel) { if (!PowerGrid.On) Add("RESTORE THE POWER", "กู้ไฟให้กลับมา", h.InteractPoint, 0.75f, MamaUI.Orange, -1f); continue; }
                Color c = h.Progress > 0.7f ? MamaUI.Red : h.Progress > 0.4f ? MamaUI.Orange : MamaUI.Teal;
                entries.Add(new Entry { text = h.Quest, room = GameManager.RoomOf(h.InteractPoint), urgency = 0.3f + h.Progress * 0.7f, col = c, progress = h.Progress, coop = h.coop });
            }
            // fish / hamster
            int fishOut = 0; float worst = 1f; Vector3 fp = Vector3.zero;
            foreach (var f in FindObjectsByType<Fish>(FindObjectsSortMode.None)) if (f && f.Out && !f.Saved && !f.Dead) { fishOut++; if (f.Breath < worst) { worst = f.Breath; fp = f.transform.position; } }
            if (fishOut > 0) Add("SAVE THE FISH! (" + fishOut + ") - bucket of water", "ช่วยปลา! (" + fishOut + ") - ใส่ถังน้ำ", fp, 0.5f + (1f - worst) * 0.5f, MamaUI.Red, 1f - worst);
            foreach (var hm in FindObjectsByType<Hamster>(FindObjectsSortMode.None)) if (hm && !hm.Settled) Add("Catch the hamster (corner it together!)", "จับหนูแฮมสเตอร์ (ช่วยกันต้อน!)", hm.transform.position, 0.45f, MamaUI.Orange, -1f, true);

            // classic problems
            FireSource big = null; foreach (var f in FireSource.All) if (f && f.burning && (big == null || f.intensity > big.intensity)) big = f;
            if (big) Add("PUT OUT THE FIRE: " + big.label, "ดับไฟ: " + Loc.T(big.label), big.transform.position, 1.2f, MamaUI.Red, big.intensity);
            if (gm.stove && gm.stove.on) Add("Turn off the gas stove", "ปิดเตาแก๊ส", gm.stove.transform.position, 0.85f, MamaUI.Orange, gm.stove.IgnitesIn >= 0 ? 1f - gm.stove.IgnitesIn / gm.stove.igniteAfter : -1f);
            if (gm.tap && gm.tap.Flowing) Add("Stop the bathtub tap", "ปิดก๊อกอ่างอาบน้ำ", gm.tap.transform.position, gm.tap.Overflowing ? 0.9f : 0.5f, MamaUI.Water);
            if (gm.alarm && gm.alarm.Beeping) Add("Silence the smoke alarm", "ทำให้สัญญาณควันเงียบ", gm.alarm.transform.position, 0.6f, MamaUI.Red);
            if (SmokeSim.I && SmokeSim.I.IsProblem) Add("Air out the smoke (windows + fan)", "ไล่ควัน (เปิดหน้าต่าง + พัดลม)", gm.stove ? gm.stove.transform.position : Vector3.zero, 0.55f, MamaUI.SmokeCol);
            var drain = FindFirstObjectByType<Drain>();
            if (drain && drain.clogged && FloodSim.I && FloodSim.I.WetArea > 3f) Add("Unclog the bathroom drain", "ทะลวงท่อห้องน้ำ", drain.transform.position, 0.4f, MamaUI.Water);
            if (FloodSim.I)
            {
                waterScanT -= Time.deltaTime;
                if (roomWater == null || waterScanT <= 0f) { waterScanT = 0.5f; roomWater = new float[GameManager.RoomRects.Length]; for (int r = 0; r < roomWater.Length; r++) roomWater[r] = FloodSim.I.VisibleLitersInRect(GameManager.RoomRects[r]); }
                for (int r = 0; r < roomWater.Length; r++) if (roomWater[r] > 6f)
                        entries.Add(new Entry { text = T("Mop up the water", "ถูน้ำที่ท่วม") + " (" + Mathf.RoundToInt(roomWater[r]) + " L)", room = r, urgency = Mathf.Clamp(roomWater[r] / 120f, 0.2f, 0.7f), col = MamaUI.Water, progress = -1f });
            }
            if (gm.pot && MessRegistry.Visible(gm.pot)) Add("Scrub or hide the burnt pot", "ขัดหรือซ่อนหม้อไหม้", gm.pot.transform.position, 0.25f, MamaUI.Ink);
            int frost = SplatMess.Count(SplatKind.Frosting), conf = SplatMess.Count(SplatKind.Confetti), foam = SplatMess.Count(SplatKind.Foam), soda = SplatMess.Count(SplatKind.Soda);
            if (frost > 0) AddSplat(SplatKind.Frosting, "Wipe the cake frosting (" + frost + ")", "เช็ดครีมเค้ก (" + frost + ")");
            if (foam > 0) AddSplat(SplatKind.Foam, "Mop the soap foam (" + foam + ")", "ถูฟองสบู่ (" + foam + ")");
            if (conf > 0) AddSplat(SplatKind.Confetti, "Sweep up the confetti (" + conf + ")", "กวาดกระดาษสี (" + conf + ")");
            if (soda > 0) AddSplat(SplatKind.Soda, "Mop the sticky soda (" + soda + ")", "ถูน้ำอัดลมเหนียว ๆ (" + soda + ")");
            // clutter grouped by room
            var perRoom = new Dictionary<int, int>();
            foreach (var m in MessRegistry.All)
            {
                if (!(m is MessyProp mp) || m is Bucket || m is Hamster || m is ToolItem || !MessRegistry.Visible(m)) continue;
                int r = GameManager.RoomOf(mp.transform.position); perRoom[r] = perRoom.TryGetValue(r, out int n) ? n + 1 : 1;
            }
            foreach (var kv in perRoom) entries.Add(new Entry { text = T("Tidy up party mess", "เก็บของเลอะจากปาร์ตี้") + " (" + kv.Value + ")", room = kv.Key, urgency = 0.15f, col = MamaUI.Teal, progress = -1f });

            entries.Sort((a, b) => b.urgency.CompareTo(a.urgency));
            return entries;

            void AddSplat(SplatKind k, string en, string th)
            {
                Vector3 p = Vector3.zero; foreach (var s in SplatMess.AllSplats) if (s && s.kind == k) { p = s.transform.position; break; }
                Add(en, th, p, 0.2f, MamaUI.Teal);
            }
        }

        // ------------------------------------------------------------------ HUD: quest board + neighbour meter
        RectTransform board; Text boardTitle, neighLbl; Image neighFill;
        readonly List<(GameObject go, Text t, Text room, Image bar, Image bg, Text coop)> rows = new List<(GameObject, Text, Text, Image, Image, Text)>();
        Font font; Sprite white;
        float hudT;

        void BuildHud()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            var cgo = new GameObject("QuestBoard", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cgo.transform.SetParent(transform, false);
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 60;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            board = R("Board", cgo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -190), new Vector2(470, 470));
            boardTitle = Tx(R("Title", board, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(6, 0), new Vector2(0, 34)), "", 24, MamaUI.Paper, TextAnchor.MiddleLeft);
            boardTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            for (int i = 0; i < 8; i++)
            {
                var row = R("Row" + i, board, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, -38 - i * 50), new Vector2(0, 44));
                var bg = row.gameObject.AddComponent<Image>(); bg.sprite = white; bg.color = new Color(0.99f, 0.93f, 0.8f, 0.92f);
                var bar = R("Bar", row, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), Vector2.zero, new Vector2(0, 5)).gameObject.AddComponent<Image>();
                bar.sprite = white; bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal;
                var t = Tx(R("T", row, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(12, 2), new Vector2(-150, -6)), "", 19, MamaUI.Ink, TextAnchor.MiddleLeft);
                var room = Tx(R("Room", row, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-10, 2), new Vector2(140, -6)), "", 16, MamaUI.Edge, TextAnchor.MiddleRight);
                var coop = Tx(R("Coop", row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(4, 8), new Vector2(46, 22)), "2P", 15, Color.white, TextAnchor.MiddleCenter);
                var cbg = coop.gameObject.AddComponent<Outline>(); cbg.effectColor = MamaUI.Pink; cbg.effectDistance = new Vector2(2, -2);
                rows.Add((row.gameObject, t, room, bar, bg, coop));
            }
            // neighbour meter (top centre, under the gear)
            var nm = R("Neighbors", cgo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -98), new Vector2(360, 34));
            var nbg = nm.gameObject.AddComponent<Image>(); nbg.sprite = white; nbg.color = new Color(0.99f, 0.93f, 0.8f, 0.9f);
            neighFill = R("Fill", nm, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, new Vector2(-8, -8)).gameObject.AddComponent<Image>();
            neighFill.sprite = white; neighFill.type = Image.Type.Filled; neighFill.fillMethod = Image.FillMethod.Horizontal;
            neighLbl = Tx(R("L", nm, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), "", 18, MamaUI.Ink, TextAnchor.MiddleCenter);
        }

        void LateUpdate()
        {
            if (!gm || board == null) return;
            bool play = gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome;
            board.gameObject.SetActive(play && !MamaSettings.Open);
            neighFill.transform.parent.gameObject.SetActive(play && gm.state == GameManager.State.Playing && !MamaSettings.Open);
            if (!play) return;

            hudT -= Time.unscaledDeltaTime;
            var list = Entries();
            if (hudT <= 0f)
            {
                hudT = 0.2f;
                boardTitle.text = gm.state == GameManager.State.MomHome ? T("MOM IS HOME - act natural!", "แม่กลับมาแล้ว - ทำตัวปกติ!") : T("PROBLEMS  ", "ปัญหาที่ต้องแก้  ") + list.Count + (PowerGrid.On ? "" : T("     POWER OUT!", "     ไฟดับ!"));
                for (int i = 0; i < rows.Count; i++)
                {
                    bool on = i < list.Count && gm.state == GameManager.State.Playing;
                    rows[i].go.SetActive(on);
                    if (!on) continue;
                    var e = list[i];
                    rows[i].t.text = e.text;
                    rows[i].room.text = e.room >= 0 ? T(GameManager.RoomNames[e.room], Loc.T(GameManager.RoomNames[e.room])) : "";
                    rows[i].bar.fillAmount = e.progress >= 0 ? e.progress : 0f;
                    rows[i].bar.color = e.col;
                    rows[i].bg.color = Color.Lerp(new Color(0.99f, 0.93f, 0.8f, 0.92f), e.col, e.urgency > 0.85f ? 0.25f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f) : 0.08f);
                    rows[i].coop.gameObject.SetActive(e.coop);
                }
            }
            neighFill.fillAmount = Neighbor / 100f;
            neighFill.color = Color.Lerp(MamaUI.Teal, MamaUI.Red, Neighbor / 100f);
            neighLbl.text = T("NEIGHBOURS ", "ข้างบ้าน ") + (Neighbor > 70f ? T("ABOUT TO CALL MOM!", "กำลังจะโทรฟ้องแม่!") : Neighbor > 35f ? T("annoyed", "เริ่มรำคาญ") : T("calm", "ยังเฉย ๆ"));

            // room warning icons (slots 0,1): the two most urgent rooms you are NOT in
            int me = GameManager.RoomOf(gm.player.transform.position); int slot = 0; var used = new HashSet<int>();
            if (gm.state == GameManager.State.Playing && !(MiniGame.I && MiniGame.I.Active))
                foreach (var e in list)
                {
                    if (slot >= 2) break;
                    if (e.room < 0 || e.room == me || used.Contains(e.room) || e.urgency < 0.3f) { used.Add(e.room); continue; }
                    used.Add(e.room);
                    gm.ui.SetGuide(slot++, true, GameManager.RoomCenter(e.room), "!", T(GameManager.RoomNames[e.room], Loc.T(GameManager.RoomNames[e.room])), e.col);
                }
            for (; slot < 2; slot++) gm.ui.SetGuide(slot, false, Vector3.zero, null, null, Color.white);
        }

        RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 piv, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(n, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(p, false);
            r.anchorMin = amin; r.anchorMax = amax; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        Text Tx(RectTransform r, string s, int size, Color c, TextAnchor a)
        {
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = size;
            return t;
        }
    }
}
