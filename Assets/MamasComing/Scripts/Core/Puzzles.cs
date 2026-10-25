using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MamasComing
{
    /// Small hands-on minigames for the big fixes (never just a bar):
    /// Wires (breaker), Simon (washer / DJ mixer), Plunger (toilet), Bolts (burst pipe), Tug (unplug the iron),
    /// Blow (sparklers), Glue (vase), Tape (fish tank).
    public class Puzzles : MonoBehaviour
    {
        public static Puzzles I { get; private set; }
        static string T(string en, string th) => Loc.Thai ? th : en;

        MiniGame.Kind kind;
        float limit, timeLeft, flash; bool flashGood;
        RectTransform card, area; Text title, hint; Image timerBar, flashImg;
        readonly List<GameObject> temp = new List<GameObject>();
        Font font; Sprite white, round, circle;

        // --- shared state
        int step, misses, lastKey; float a, b, c;
        readonly List<int> seq = new List<int>();
        readonly List<Image> imgs = new List<Image>();
        readonly List<Text> txts = new List<Text>();
        readonly List<RectTransform> rts = new List<RectTransform>();
        static readonly Color[] WireCols = { new Color(0.92f, 0.25f, 0.22f), new Color(0.25f, 0.5f, 0.95f), new Color(1f, 0.84f, 0.2f), new Color(0.3f, 0.8f, 0.35f) };
        static readonly string[] Arrows = { "←", "↑", "→", "↓" };
        static readonly string[] Shapes = { "▲", "■", "●", "♥" };

        public static void Ensure() { if (!I) new GameObject("Puzzles").AddComponent<Puzzles>(); }

        void Awake()
        {
            I = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tex = Texture2D.whiteTexture; white = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            var ui = FindFirstObjectByType<MamaUI>();
            round = ui ? ui.roundSprite : white; circle = ui ? ui.circleSprite : white;
            var cgo = new GameObject("PuzzleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            cgo.transform.SetParent(transform, false);
            var cv = cgo.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 120;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            card = R("Card", cgo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 220), new Vector2(680, 440));
            var bg = Img(card, MamaUI.Paper, round); var o = card.gameObject.AddComponent<Outline>(); o.effectColor = MamaUI.Edge; o.effectDistance = new Vector2(5, -5);
            card.localRotation = Quaternion.Euler(0, 0, -0.5f);
            flashImg = Img(R("Flash", card, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14, -14)), new Color(1, 1, 1, 0), round);
            title = Tx(R("Title", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(-30, 48)), "", 32, MamaUI.Ink);
            hint = Tx(R("Hint", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(-30, 34)), "", 22, MamaUI.Edge);
            // long hints / titles used to run off the card: wrap them and shrink to fit instead
            hint.horizontalOverflow = HorizontalWrapMode.Wrap; hint.verticalOverflow = VerticalWrapMode.Truncate; hint.resizeTextForBestFit = true; hint.resizeTextMinSize = 14; hint.resizeTextMaxSize = 22; hint.rectTransform.sizeDelta = new Vector2(-30, 52);
            title.horizontalOverflow = HorizontalWrapMode.Wrap; title.verticalOverflow = VerticalWrapMode.Truncate; title.resizeTextForBestFit = true; title.resizeTextMinSize = 18; title.resizeTextMaxSize = 32;
            var tb = R("Timer", card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(-60, 10));
            Img(tb, new Color(0.85f, 0.78f, 0.66f), white);
            timerBar = Img(R("Fill", tb, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero), MamaUI.Teal, white);
            timerBar.type = Image.Type.Filled; timerBar.fillMethod = Image.FillMethod.Horizontal;
            area = R("Area", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(620, 300));
            card.gameObject.SetActive(false);
        }

        // ================================================================== lifecycle
        public void Begin(MiniGame.Kind k, string label)
        {
            foreach (var g in temp) if (g) Destroy(g);
            temp.Clear(); imgs.Clear(); txts.Clear(); rts.Clear(); seq.Clear();
            kind = k; step = 0; misses = 0; lastKey = 0; a = b = c = 0f; flash = 0f;
            title.text = label;
            card.gameObject.SetActive(true);
            switch (k)
            {
                case MiniGame.Kind.Wires: BuildWires(); break;
                case MiniGame.Kind.Simon: BuildSimon(); break;
                case MiniGame.Kind.Plunger: BuildPlunger(); break;
                case MiniGame.Kind.Bolts: BuildBolts(); break;
                case MiniGame.Kind.Tug: BuildTug(); break;
                case MiniGame.Kind.Blow: BuildBlow(); break;
                case MiniGame.Kind.Glue: BuildGlue(); break;
                case MiniGame.Kind.Tape: BuildTape(); break;
            }
            timeLeft = limit;
        }

        public void End() { if (kind == MiniGame.Kind.Wires && card && card.gameObject.activeSelf) EndWires(); if (card) card.gameObject.SetActive(false); }

        /// 0 = running, 1 = won, -1 = failed.
        public int Tick(float dt, bool fresh)
        {
            if (flash > 0f) flash -= dt;
            flashImg.color = flash > 0f ? (flashGood ? new Color(0.4f, 1f, 0.5f, 0.25f) : new Color(1f, 0.3f, 0.3f, 0.4f)) : new Color(1, 1, 1, 0);
            card.anchoredPosition = new Vector2(flash > 0f && !flashGood ? Mathf.Sin(Time.time * 70f) * 8f : 0f, 220f);
            if (limit > 0f) { timeLeft -= dt; timerBar.fillAmount = Mathf.Clamp01(timeLeft / limit); timerBar.color = timeLeft < limit * 0.3f ? MamaUI.Red : MamaUI.Teal; if (timeLeft <= 0f) return -1; }
            else timerBar.fillAmount = 0f;
            if (!fresh) return 0;
            switch (kind)
            {
                case MiniGame.Kind.Wires: return TickWires(dt);
                case MiniGame.Kind.Simon: return TickSimon(dt);
                case MiniGame.Kind.Plunger: return TickPlunger(dt);
                case MiniGame.Kind.Bolts: return TickBolts(dt);
                case MiniGame.Kind.Tug: return TickTug(dt);
                case MiniGame.Kind.Blow: return TickBlow(dt);
                case MiniGame.Kind.Glue: return TickGlue(dt);
                case MiniGame.Kind.Tape: return TickTape(dt);
            }
            return 1;
        }

        void Good(string sfx = "click", float pitch = 1f) { flash = 0.15f; flashGood = true; MamaAudio.Play(sfx, null, 0.7f, pitch); }
        void Bad(string sfx = "bad") { flash = 0.3f; flashGood = false; MamaAudio.Play(sfx, null, 0.7f); if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.25f); }

        // ================================================================== input
        static bool Down(Key k1, Key k2) { var k = Keyboard.current; return k != null && (k[k1].wasPressedThisFrame || k[k2].wasPressedThisFrame); }
        static int ArrowDown()
        {
            var g = Gamepad.current;
            if (Down(Key.A, Key.LeftArrow) || (g != null && g.dpad.left.wasPressedThisFrame)) return 0;
            if (Down(Key.W, Key.UpArrow) || (g != null && g.dpad.up.wasPressedThisFrame)) return 1;
            if (Down(Key.D, Key.RightArrow) || (g != null && g.dpad.right.wasPressedThisFrame)) return 2;
            if (Down(Key.S, Key.DownArrow) || (g != null && g.dpad.down.wasPressedThisFrame)) return 3;
            return -1;
        }
        static bool E => MamaInput.InteractDown || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
        static bool EHeld => MamaInput.InteractHeld || (Keyboard.current != null && (Keyboard.current.eKey.isPressed || Keyboard.current.spaceKey.isPressed));

        // ================================================================== WIRES  (match colours left -> right)
        int[] rightCols; bool[] doneL, doneR; int dragFrom = -1; RectTransform dragLine; CursorLockMode prevLock; bool prevVis, prevCam;
        static readonly float[] WireY = { 105, 35, -35, -105 };
        void BuildWires()
        {
            limit = 18f; hint.text = T("Drag each wire with the MOUSE to the socket of the same colour  •  wrong = ZAP!", "ใช้เมาส์ลากสายไปเสียบช่องสีเดียวกัน  •  ผิด = ไฟช็อต!");
            rightCols = new[] { 0, 1, 2, 3 }; for (int i = 0; i < 4; i++) { int j = Random.Range(i, 4); (rightCols[i], rightCols[j]) = (rightCols[j], rightCols[i]); }
            doneL = new bool[4]; doneR = new bool[4]; dragFrom = -1;
            Img(Add("Panel", new Vector2(0, 0), new Vector2(560, 280)), new Color(0.22f, 0.24f, 0.27f), round);
            for (int i = 0; i < 4; i++)
            {
                imgs.Add(Img(Add("L" + i, new Vector2(-230, WireY[i]), new Vector2(60, 40)), WireCols[i], round));
                imgs.Add(Img(Add("R" + i, new Vector2(230, WireY[i]), new Vector2(60, 40)), WireCols[rightCols[i]], round));
            }
            dragLine = Add("Drag", Vector2.zero, new Vector2(0, 14)); Img(dragLine, Color.white, white); dragLine.gameObject.SetActive(false);
            prevLock = Cursor.lockState; prevVis = Cursor.visible; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var cam = GameManager.I ? GameManager.I.cam : null; prevCam = cam && cam.inputEnabled; if (cam) cam.inputEnabled = false;
        }
        void EndWires()
        {
            Cursor.lockState = prevLock; Cursor.visible = prevVis;
            var cam = GameManager.I ? GameManager.I.cam : null; if (cam) cam.inputEnabled = prevCam;
        }
        int HitSocket(Vector2 local, float x)
        {
            for (int i = 0; i < 4; i++) if (Mathf.Abs(local.x - x) < 50f && Mathf.Abs(local.y - WireY[i]) < 30f) return i;
            return -1;
        }
        int TickWires(float dt)
        {
            var m = Mouse.current; if (m == null) return 0;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, m.position.ReadValue(), null, out Vector2 local);
            for (int i = 0; i < 4; i++) if (!doneL[i]) imgs[i * 2].rectTransform.localScale = Vector3.one * (dragFrom == i ? 1.2f : 1f + Mathf.Sin(Time.time * 6f + i) * 0.04f);
            if (m.leftButton.wasPressedThisFrame) { int l = HitSocket(local, -230f); if (l >= 0 && !doneL[l]) { dragFrom = l; MamaAudio.Play("click", null, 0.5f, 1.3f); } }
            if (dragFrom >= 0)
            {
                SetLine(dragLine, new Vector2(-200, WireY[dragFrom]), local, WireCols[dragFrom]);
                if (m.leftButton.wasReleasedThisFrame)
                {
                    int r = HitSocket(local, 230f);
                    if (r >= 0 && !doneR[r] && rightCols[r] == dragFrom)
                    {
                        Line(new Vector2(-200, WireY[dragFrom]), new Vector2(200, WireY[r]), WireCols[dragFrom], 14f);
                        doneL[dragFrom] = doneR[r] = true; step++; Good("click", 1f + step * 0.15f);
                    }
                    else if (r >= 0) { Bad("zap"); timeLeft -= 2.5f; }
                    dragFrom = -1; dragLine.gameObject.SetActive(false);
                    if (step >= 4) { EndWires(); return 1; }
                }
            }
            return 0;
        }
        void SetLine(RectTransform r, Vector2 p0, Vector2 p1, Color c)
        {
            r.gameObject.SetActive(true); r.anchoredPosition = (p0 + p1) / 2f; r.sizeDelta = new Vector2((p1 - p0).magnitude, 14f);
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg); r.GetComponent<Image>().color = c;
        }

        // ================================================================== SIMON  (watch the lights, repeat)
        void BuildSimon()
        {
            limit = 0f; hint.text = T("Watch... then repeat with the arrow keys / WASD", "ดูให้ดี... แล้วกดตามด้วยลูกศร / WASD");
            for (int i = 0; i < 5; i++) seq.Add(Random.Range(0, 4));
            Vector2[] pos = { new Vector2(-120, 0), new Vector2(0, 80), new Vector2(120, 0), new Vector2(0, -80) };
            Color[] cols = { new Color(0.95f, 0.35f, 0.35f), new Color(0.35f, 0.6f, 1f), new Color(0.35f, 0.85f, 0.45f), new Color(1f, 0.82f, 0.25f) };
            for (int i = 0; i < 4; i++)
            {
                var im = Img(Add("Btn" + i, pos[i], new Vector2(100, 76)), cols[i] * 0.55f, round); imgs.Add(im);
                txts.Add(Tx(Add("A" + i, pos[i], new Vector2(100, 76)), Arrows[i], 44, Color.white));
            }
            txts.Add(Tx(Add("Status", new Vector2(220, 110), new Vector2(160, 40)), "", 24, MamaUI.Ink));
            a = -0.8f; b = 0; step = 0; c = 0; // a = show timer, b = 0 showing / 1 input, c = input index
        }
        int TickSimon(float dt)
        {
            Color[] cols = { new Color(0.95f, 0.35f, 0.35f), new Color(0.35f, 0.6f, 1f), new Color(0.35f, 0.85f, 0.45f), new Color(1f, 0.82f, 0.25f) };
            for (int i = 0; i < 4; i++) imgs[i].color = Color.Lerp(imgs[i].color, cols[i] * 0.55f, dt * 8f);
            if (b == 0)
            {
                txts[4].text = T("WATCH", "ดูให้ดี");
                a += dt;
                int idx = Mathf.FloorToInt(a / 0.55f);
                if (a >= 0 && idx < seq.Count && a - idx * 0.55f < 0.05f + dt) { imgs[seq[idx]].color = cols[seq[idx]] * 1.25f; MamaAudio.Play("pop", null, 0.6f, 0.8f + seq[idx] * 0.2f); }
                if (a >= 0 && idx < seq.Count) imgs[seq[idx]].color = Color.Lerp(imgs[seq[idx]].color, cols[seq[idx]] * 1.3f, 0.5f);
                if (idx >= seq.Count) { b = 1; c = 0; }
                return 0;
            }
            txts[4].text = T("YOUR TURN ", "ตาเรา ") + (int)c + "/" + seq.Count;
            int k = ArrowDown();
            if (k < 0) return 0;
            imgs[k].color = cols[k] * 1.3f;
            if (k == seq[(int)c]) { Good("pop", 0.8f + k * 0.2f); c++; if (c >= seq.Count) return 1; }
            else { Bad(); misses++; if (misses >= 3) return -1; b = 0; a = -0.8f; }
            return 0;
        }

        // ================================================================== PLUNGER  (pump when the ring hits the drain)
        void BuildPlunger()
        {
            limit = 24f; hint.text = T("Press E when the ring lands on the drain!  5 good pumps", "กด E ตอนวงแหวนตรงกับท่อ!  ปั๊มให้ได้ 5 ครั้ง");
            var bowl = Img(Add("Bowl", new Vector2(0, 0), new Vector2(260, 260)), new Color(0.93f, 0.95f, 0.97f), circle); imgs.Add(bowl);
            var target = Img(Add("Target", new Vector2(0, 0), new Vector2(90, 90)), new Color(0.35f, 0.25f, 0.2f), circle); imgs.Add(target);
            // the shrinking ring: a solid red disc with a bowl-coloured disc inside it (the old version was a clear
            // image with an outline - Unity culls fully transparent images, so the ring never showed up)
            var ring = Img(Add("Ring", Vector2.zero, new Vector2(250, 250)), MamaUI.Red, circle); rts.Add(ring.rectTransform);
            Img(R("Inner", ring.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), Vector2.zero, new Vector2(-16, -16)), new Color(0.93f, 0.95f, 0.97f), circle);
            target.rectTransform.SetAsLastSibling();
            txts.Add(Tx(Add("Count", new Vector2(220, 100), new Vector2(120, 50)), "0/5", 34, MamaUI.Ink));
            a = 250f; b = 120f;
        }
        int TickPlunger(float dt)
        {
            a -= b * dt; if (a < 30f) { a = 250f; Bad("gurgle"); }
            rts[0].sizeDelta = new Vector2(a, a);
            imgs[1].color = Mathf.Abs(a - 90f) < 22f ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.35f, 0.25f, 0.2f);   // drain lights up green = press now
            if (E)
            {
                if (Mathf.Abs(a - 90f) < 22f) { step++; b += 25f; a = 250f; Good("thwop", 0.9f + step * 0.1f); imgs[1].rectTransform.localScale = Vector3.one * (1f - step * 0.12f); }
                else { Bad("gurgle"); a = 250f; step = Mathf.Max(0, step - 1); }
            }
            txts[0].text = step + "/5";
            return step >= 5 ? 1 : 0;
        }

        // ================================================================== BOLTS  (tighten in star order with A/D)
        void BuildBolts()
        {
            limit = 20f; hint.text = T("Tighten the glowing bolt: A D A D...  (order matters!)", "ขันน็อตที่เรืองแสง: A D A D...  (ต้องตามลำดับ!)");
            Img(Add("Pipe", Vector2.zero, new Vector2(520, 70)), new Color(0.75f, 0.55f, 0.35f), white);
            Img(Add("Flange", Vector2.zero, new Vector2(220, 220)), new Color(0.7f, 0.72f, 0.75f), circle);
            Vector2[] p = { new Vector2(0, 80), new Vector2(0, -80), new Vector2(-80, 0), new Vector2(80, 0) };   // star order
            for (int i = 0; i < 4; i++)
            {
                var bolt = Img(Add("Bolt" + i, p[i], new Vector2(54, 54)), new Color(0.45f, 0.47f, 0.5f), round); imgs.Add(bolt); rts.Add(bolt.rectTransform);
                txts.Add(Tx(Add("N" + i, p[i], new Vector2(54, 54)), (i + 1).ToString(), 26, Color.white));
            }
            var wr = Img(Add("Wrench", new Vector2(240, -90), new Vector2(150, 34)), new Color(0.6f, 0.62f, 0.66f), round); rts.Add(wr.rectTransform);
            a = 0; // turns on current bolt
        }
        int TickBolts(float dt)
        {
            for (int i = 0; i < 4; i++) imgs[i].color = i < step ? new Color(0.3f, 0.8f, 0.4f) : (i == step ? Color.Lerp(new Color(0.45f, 0.47f, 0.5f), new Color(1f, 0.85f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.time * 9f)) : new Color(0.45f, 0.47f, 0.5f));
            int k = ArrowDown();
            int key = k == 0 ? -1 : k == 2 ? 1 : 0;
            if (key != 0)
            {
                if (key != lastKey) { a++; rts[step].localRotation = Quaternion.Euler(0, 0, -a * 45f); rts[4].localRotation = Quaternion.Euler(0, 0, -a * 30f); Good("ratchet", 0.9f + a * 0.05f); }
                else { a = Mathf.Max(0, a - 1); Bad("bad"); }
                lastKey = key;
                if (a >= 5) { step++; a = 0; lastKey = 0; MamaAudio.Play("good", null, 0.4f, 1.4f); if (step >= 4) return 1; }
            }
            return 0;
        }

        // ================================================================== TUG  (pull the plug: keep tension in the green)
        void BuildTug()
        {
            limit = 16f; hint.text = T("HOLD E to pull - keep the needle in the GREEN. Too hard = it whips back!", "กด E ค้างเพื่อดึง - คุมเข็มให้อยู่ในช่องเขียว ดึงแรงไป = สายดีดกลับ!");
            var meter = Add("Meter", new Vector2(-150, 0), new Vector2(60, 260)); Img(meter, new Color(0.85f, 0.78f, 0.66f), round);
            var green = R("Green", meter, new Vector2(0, 0.45f), new Vector2(1, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Img(green, new Color(0.35f, 0.82f, 0.45f), white);
            var red = R("Red", meter, new Vector2(0, 0.88f), new Vector2(1, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero); Img(red, MamaUI.Red, white);
            var needle = Img(R("Needle", meter, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 8)), MamaUI.Ink, white); rts.Add(needle.rectTransform);
            var prog = Add("Prog", new Vector2(110, 0), new Vector2(300, 40)); Img(prog, new Color(0.85f, 0.78f, 0.66f), round);
            var pf = Img(R("F", prog, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero), MamaUI.Teal, white); pf.type = Image.Type.Filled; pf.fillMethod = Image.FillMethod.Horizontal; imgs.Add(pf);
            var plug = Img(Add("Plug", new Vector2(110, 60), new Vector2(70, 50)), new Color(0.95f, 0.95f, 0.92f), round); rts.Add(plug.rectTransform);
            a = 0.1f; b = 0f; c = 0f; // a tension, b progress, c wobble vel
        }
        int TickTug(float dt)
        {
            c += (Random.Range(-1f, 1f) * 2.2f - c * 1.5f) * dt;
            a += ((EHeld ? 0.55f : -0.5f) + c) * dt;
            a = Mathf.Clamp01(a);
            rts[0].anchorMin = rts[0].anchorMax = new Vector2(0.5f, a); rts[0].anchoredPosition = Vector2.zero;
            if (a > 0.45f && a < 0.7f) { b += dt / 2.8f; if (Random.value < dt * 6f) MamaAudio.Play("creak", null, 0.2f, 1.6f); }
            if (a > 0.88f) { b = Mathf.Max(0f, b - 0.3f); a = 0.2f; Bad("whoosh"); }
            imgs[0].fillAmount = b;
            rts[1].anchoredPosition = new Vector2(110 + b * 120f, 60 + Mathf.Sin(Time.time * 30f) * a * 4f);
            return b >= 1f ? 1 : 0;
        }

        // ================================================================== BLOW  (mash E to blow out the sparklers)
        void BuildBlow()
        {
            limit = 12f; hint.text = T("MASH E / SPACE to blow them out before they light the tablecloth!", "กด E / SPACE รัว ๆ เป่าให้ดับ ก่อนไฟจะติดผ้าปูโต๊ะ!");
            Img(Add("Cake", new Vector2(0, -70), new Vector2(360, 110)), new Color(1f, 0.8f, 0.85f), round);
            for (int i = 0; i < 5; i++)
            {
                Img(Add("Stick" + i, new Vector2(-140 + i * 70, 10), new Vector2(8, 90)), new Color(0.6f, 0.6f, 0.65f), white);
                var fl = Img(Add("Spark" + i, new Vector2(-140 + i * 70, 70), new Vector2(46, 46)), new Color(1f, 0.85f, 0.3f), circle); imgs.Add(fl); rts.Add(fl.rectTransform);
            }
            a = 1f;
        }
        int TickBlow(float dt)
        {
            a = Mathf.Min(1f, a + dt * 0.28f);
            if (E) { a -= 0.075f; MamaAudio.Play("whoosh", null, 0.3f, 1.8f); }
            for (int i = 0; i < 5; i++)
            {
                float lit = Mathf.Clamp01(a * 5f - i);
                rts[i].localScale = Vector3.one * (lit * (0.8f + Random.value * 0.4f));
            }
            if (a <= 0f) { Good("good"); return 1; }
            return 0;
        }

        // ================================================================== GLUE  (pick the piece that fits)
        void BuildGlue()
        {
            limit = 0f; hint.text = T("Which piece fits the hole?  ← ↑ →  (4 pieces)", "ชิ้นไหนพอดีกับรู?  ← ↑ →  (4 ชิ้น)");
            Img(Add("Vase", new Vector2(-170, 10), new Vector2(170, 240)), new Color(0.3f, 0.75f, 0.75f), round);
            var hole = Tx(Add("Hole", new Vector2(-170, 20), new Vector2(120, 120)), "", 90, new Color(0.15f, 0.2f, 0.22f)); txts.Add(hole);
            for (int i = 0; i < 3; i++)
            {
                var opt = Img(Add("Opt" + i, new Vector2(40 + i * 110, 10), new Vector2(96, 96)), MamaUI.PaperDark, round); imgs.Add(opt);
                txts.Add(Tx(Add("S" + i, new Vector2(40 + i * 110, 14), new Vector2(96, 96)), "", 60, new Color(0.3f, 0.75f, 0.75f)));
                txts.Add(Tx(Add("K" + i, new Vector2(40 + i * 110, -62), new Vector2(96, 30)), Arrows[i == 0 ? 0 : i == 1 ? 1 : 2], 26, MamaUI.Edge));
            }
            txts.Add(Tx(Add("Round", new Vector2(150, 110), new Vector2(200, 40)), "", 24, MamaUI.Ink));
            NewGlueRound();
        }
        int glueAns;
        void NewGlueRound()
        {
            int shape = Random.Range(0, 4); txts[0].text = Shapes[shape];
            glueAns = Random.Range(0, 3);
            var others = new List<int> { 0, 1, 2, 3 }; others.Remove(shape);
            for (int i = 0; i < 3; i++) { int s = i == glueAns ? shape : others[Random.Range(0, others.Count)]; others.Remove(s); txts[1 + i * 2].text = Shapes[s]; }
            a = 5f;
        }
        int TickGlue(float dt)
        {
            txts[7].text = T("Piece ", "ชิ้นที่ ") + (step + 1) + "/4   " + Mathf.CeilToInt(a) + "s";
            a -= dt; if (a <= 0f) { Bad("crack"); misses++; if (misses >= 3) return -1; NewGlueRound(); return 0; }
            int k = ArrowDown(); int pick = k == 0 ? 0 : k == 1 ? 1 : k == 2 ? 2 : -1;
            if (pick < 0) return 0;
            if (pick == glueAns) { step++; Good("place", 1.2f); if (step >= 4) return 1; NewGlueRound(); }
            else { Bad("crack"); misses++; if (misses >= 3) return -1; NewGlueRound(); }
            return 0;
        }

        // ================================================================== TAPE  (follow the crack with quick arrow presses)
        void BuildTape()
        {
            limit = 16f; hint.text = T("Press the arrow shown before the water bursts through!", "กดลูกศรที่ขึ้นมาให้ทันก่อนน้ำพุ่ง!");
            Img(Add("Glass", Vector2.zero, new Vector2(520, 240)), new Color(0.6f, 0.85f, 1f, 0.5f), round);
            for (int i = 0; i < 6; i++)
            {
                var seg = Img(Add("Crack" + i, new Vector2(-200 + i * 80, (i % 2 == 0 ? 20 : -20)), new Vector2(84, 10)), new Color(0.2f, 0.25f, 0.3f), white);
                seg.rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -25 : 25); imgs.Add(seg);
                seq.Add(Random.Range(0, 4));
            }
            txts.Add(Tx(Add("Prompt", new Vector2(0, 85), new Vector2(120, 80)), "", 64, MamaUI.Red));
            a = 1.4f;
        }
        int TickTape(float dt)
        {
            for (int i = 0; i < 6; i++) imgs[i].color = i < step ? new Color(0.95f, 0.85f, 0.4f) : new Color(0.2f, 0.25f, 0.3f);
            txts[0].text = Arrows[seq[step]];
            txts[0].rectTransform.anchoredPosition = new Vector2(-200 + step * 80, 85);
            a -= dt;
            int k = ArrowDown();
            if (a <= 0f || (k >= 0 && k != seq[step])) { Bad("splash"); step = Mathf.Max(0, step - 2); a = 1.4f; return 0; }
            if (k == seq[step]) { step++; a = 1.3f; Good("rip", 0.9f + step * 0.08f); if (step >= 6) return 1; }
            return 0;
        }

        // ================================================================== UI helpers
        RectTransform Add(string n, Vector2 pos, Vector2 size)
        {
            var r = R(n, area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            temp.Add(r.gameObject); return r;
        }
        void Line(Vector2 p0, Vector2 p1, Color c, float w)
        {
            var r = Add("Line", (p0 + p1) / 2f, new Vector2((p1 - p0).magnitude, w));
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(p1.y - p0.y, p1.x - p0.x) * Mathf.Rad2Deg);
            Img(r, c, white); r.SetAsFirstSibling();
        }
        RectTransform R(string n, Transform p, Vector2 amin, Vector2 amax, Vector2 piv, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(n, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(p, false);
            r.anchorMin = amin; r.anchorMax = amax; r.pivot = piv; r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        Image Img(RectTransform r, Color c, Sprite s) { var i = r.gameObject.AddComponent<Image>(); i.sprite = s; i.color = c; i.raycastTarget = false; if (s == round) i.type = Image.Type.Sliced; return i; }
        Text Tx(RectTransform r, string s, int size, Color c)
        {
            var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
        }
    }
}
