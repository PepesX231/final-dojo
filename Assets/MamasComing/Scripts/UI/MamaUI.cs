using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// <summary>Builds the whole "handmade paper" HUD at runtime.</summary>
    public partial class MamaUI : MonoBehaviour
    {
        public Sprite roundSprite;
        public Sprite circleSprite;

        public static readonly Color Paper = new Color(0.99f, 0.93f, 0.80f);
        public static readonly Color PaperDark = new Color(0.93f, 0.84f, 0.68f);
        public static readonly Color Edge = new Color(0.58f, 0.36f, 0.20f);
        public static readonly Color Ink = new Color(0.17f, 0.21f, 0.23f);
        public static readonly Color Pink = new Color(1f, 0.56f, 0.69f);
        public static readonly Color Teal = new Color(0.25f, 0.74f, 0.74f);
        public static readonly Color Orange = new Color(1f, 0.6f, 0.2f);
        public static readonly Color Red = new Color(0.9f, 0.27f, 0.27f);
        public static readonly Color Water = new Color(0.3f, 0.6f, 0.95f);
        public static readonly Color SmokeCol = new Color(0.45f, 0.43f, 0.42f);

        public class Row { public GameObject go; public Text title, status; public Image fill, icon; }

        Font font;
        RectTransform root;
        GameObject hud, promptGO, toastGO, callGO, momGO, titleGO, endGO, holdGO;
        Text objMain, objCount, objList, timerText, timerSub, heldText, heldIcon, promptText, toastText, callText, momText, endTitle, endBody, endStats;
        Image timerDisc, meterFill, heldSoak, endBand, holdFill, alarmFlash;
        public Row fireRow, floodRow, smokeRow;
        readonly Queue<string> toastQ = new Queue<string>();
        readonly Queue<string> momQ = new Queue<string>();
        float toastT, momT, callT;
        public bool ImportantMessage => callT>0f || momT>0f;

        static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0), CC = new Vector2(0.5f, 0.5f);

        public void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var cgo = new GameObject("MamaCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var sc = cgo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            root = (RectTransform)cgo.transform;

            var flash = Rect("AlarmFlash", root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
            alarmFlash = Img(flash, new Color(1f, 0.1f, 0.1f, 0f), null, false);

            hud = Rect("HUD", root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero).gameObject;
            var H = (RectTransform)hud.transform;

            // Objective (top-left)
            var badge = Rect("ObjBadge", H, TL, TL, TL, new Vector2(28, -22), new Vector2(150, 150));
            Img(badge, Paper, circleSprite, false, Edge, 4);
            var ring = Rect("Ring", badge, CC, CC, CC, Vector2.zero, new Vector2(116, 116));
            Img(ring, Pink, circleSprite, false);
            Txt(ring, "!", 86, Color.white, TextAnchor.MiddleCenter, 0, true);
            var banner = Rect("ObjBanner", H, TL, TL, TL, new Vector2(176, -36), new Vector2(680, 92));
            banner.localRotation = Quaternion.Euler(0, 0, 1.2f);
            Img(banner, Paper, roundSprite, true, Edge, 3);
            objMain = Txt(banner, "", 34, Ink, TextAnchor.MiddleLeft, 26, true);
            var tag = Rect("ObjCount", H, TL, TL, TL, new Vector2(186, -134), new Vector2(150, 62));
            tag.localRotation = Quaternion.Euler(0, 0, -1.5f);
            Img(tag, PaperDark, roundSprite, true, Edge, 3);
            objCount = Txt(tag, "0/0", 34, Ink, TextAnchor.MiddleCenter, 0, true);
            objList = Txt(Rect("ObjList", H, TL, TL, TL, new Vector2(190, -206), new Vector2(680, 260)), "", 26, Color.white, TextAnchor.UpperLeft, 6, true);
            Shadowed(objList);

            // Timer (top-right)
            var tbadge = Rect("Timer", H, TR, TR, TR, new Vector2(-28, -22), new Vector2(190, 190));
            timerDisc = Img(tbadge, Paper, circleSprite, false, Edge, 4);
            var tinner = Rect("Inner", tbadge, CC, CC, CC, Vector2.zero, new Vector2(160, 160));
            Img(tinner, Teal, circleSprite, false);
            timerText = Txt(Rect("Time", tinner, CC, CC, CC, new Vector2(0, -12), new Vector2(160, 80)), "5:00", 54, Color.white, TextAnchor.MiddleCenter, 0, true);
            timerSub = Txt(Rect("Sub", tinner, CC, CC, CC, new Vector2(0, 32), new Vector2(160, 40)), "MOM IN", 22, Color.white, TextAnchor.MiddleCenter, 0, true);
            Shadowed(timerText);

            // Problem panel (right, under timer)
            var panel = Rect("Problems", H, TR, TR, TR, new Vector2(-24, -228), new Vector2(430, 300));
            panel.localRotation = Quaternion.Euler(0, 0, -0.8f);
            Img(panel, Paper, roundSprite, true, Edge, 3);
            Txt(Rect("Head", panel, TL, TR, TC, new Vector2(0, -8), new Vector2(0, 40)), "DISASTER REPORT", 24, Edge, TextAnchor.MiddleCenter, 0, true);
            fireRow = MakeRow(panel, -54, "FIRE", Red);
            floodRow = MakeRow(panel, -134, "FLOOD", Water);
            smokeRow = MakeRow(panel, -214, "SMOKE", SmokeCol);

            // Held item (bottom-right)
            var hb = Rect("HeldBadge", H, BR, BR, BR, new Vector2(-40, 70), new Vector2(170, 170));
            Img(hb, Paper, circleSprite, false, Edge, 4);
            var hInner = Rect("Inner", hb, CC, CC, CC, Vector2.zero, new Vector2(138, 138));
            Img(hInner, PaperDark, circleSprite, false);
            heldIcon = Txt(hInner, "~", 70, Ink, TextAnchor.MiddleCenter, 0, true);
            var soakRT = Rect("Soak", hb, BC, BC, BC, new Vector2(0, 14), new Vector2(110, 12));
            Img(soakRT, new Color(0, 0, 0, 0.15f), roundSprite, true);
            var soakFill = Rect("Fill", soakRT, Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
            heldSoak = Img(soakFill, Water, roundSprite, false);
            heldSoak.type = Image.Type.Filled; heldSoak.fillMethod = Image.FillMethod.Horizontal;
            var hl = Rect("HeldLabel", H, BR, BR, BR, new Vector2(-15, 18), new Vector2(260, 62));
            hl.localRotation = Quaternion.Euler(0, 0, -1.2f);
            Img(hl, Paper, roundSprite, true, Edge, 3);
            heldText = Txt(hl, "Empty hands", 28, Ink, TextAnchor.MiddleCenter, 4, true);

            // Mess meter (bottom-left)
            var mm = Rect("MessMeter", H, BL, BL, BL, new Vector2(28, 24), new Vector2(430, 92));
            Img(mm, Paper, roundSprite, true, Edge, 3);
            Txt(Rect("Label", mm, TL, TL, TL, new Vector2(18, -6), new Vector2(400, 36)), "HOW BUSTED ARE YOU", 22, Ink, TextAnchor.MiddleLeft, 0, true);
            var bar = Rect("Bar", mm, BL, BL, BL, new Vector2(18, 16), new Vector2(394, 28));
            Img(bar, new Color(0.45f, 0.3f, 0.2f, 0.25f), roundSprite, true);
            var fill = Rect("Fill", bar, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-6, -6));
            meterFill = Img(fill, Pink, roundSprite, false);
            meterFill.type = Image.Type.Filled; meterFill.fillMethod = Image.FillMethod.Horizontal; meterFill.fillAmount = 0.5f;

            // Hold progress (above prompt)
            var hg = Rect("Hold", H, BC, BC, BC, new Vector2(0, 228), new Vector2(420, 26));
            Img(hg, new Color(0.25f, 0.13f, 0.07f, 0.8f), roundSprite, true);
            var hf = Rect("Fill", hg, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-8, -8));
            holdFill = Img(hf, Teal, roundSprite, false);
            holdFill.type = Image.Type.Filled; holdFill.fillMethod = Image.FillMethod.Horizontal;
            holdGO = hg.gameObject;

            // Prompt (bottom-center)
            var pr = Rect("Prompt", H, BC, BC, BC, new Vector2(0, 150), new Vector2(900, 70));
            Img(pr, new Color(0.25f, 0.13f, 0.07f, 0.82f), roundSprite, true);
            promptText = Txt(pr, "", 29, Color.white, TextAnchor.MiddleCenter, 8, true);
            promptGO = pr.gameObject;

            // Toast (upper-center)
            var to = Rect("Toast", H, TC, TC, TC, new Vector2(0, -170), new Vector2(1000, 68));
            Img(to, Paper, roundSprite, true, Edge, 3);
            toastText = Txt(to, "", 29, Ink, TextAnchor.MiddleCenter, 12, true);
            toastGO = to.gameObject;

            // Call / big event
            var ca = Rect("Call", H, TC, TC, TC, new Vector2(0, -36), new Vector2(1060, 110));
            ca.localRotation = Quaternion.Euler(0, 0, -0.8f);
            Img(ca, Pink, roundSprite, true, Edge, 4);
            callText = Txt(ca, "", 34, Color.white, TextAnchor.MiddleCenter, 18, true);
            Shadowed(callText);
            callGO = ca.gameObject;

            // Mom speech
            var mo = Rect("MomSay", H, BC, BC, BC, new Vector2(0, 280), new Vector2(1180, 104));
            Img(mo, new Color(1f, 0.86f, 0.9f), roundSprite, true, Red, 4);
            var momTag = Rect("Tag", mo, TL, TL, BL, new Vector2(24, -14), new Vector2(120, 44));
            Img(momTag, Red, roundSprite, true);
            Txt(momTag, "MOM", 28, Color.white, TextAnchor.MiddleCenter, 0, true);
            momText = Txt(mo, "", 34, new Color(0.45f, 0.1f, 0.15f), TextAnchor.MiddleCenter, 18, true);
            momGO = mo.gameObject;

            BuildGuides(H);
            BuildMini2(H);
            BuildBubble(H);

            promptGO.SetActive(false); toastGO.SetActive(false); callGO.SetActive(false); momGO.SetActive(false); holdGO.SetActive(false);
            BuildTitle();
            BuildEnd();
            BuildReadability(H);
        }

        // ------------------------------------------------------------------ mini game panel
        GameObject miniGO; Text miniTitle, miniHint; RectTransform miniTrack, miniZone, miniNeedle; Image miniFill, miniFlash; Text[] miniPips;

        void BuildMini(RectTransform H)
        {
            var p = Rect("MiniGame", H, BC, BC, BC, new Vector2(0, 330), new Vector2(620, 170));
            p.localRotation = Quaternion.Euler(0, 0, 0.8f);
            Img(p, Paper, roundSprite, true, Edge, 4);
            miniFlash = Img(Rect("Flash", p, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-10, -10)), new Color(1, 1, 1, 0), roundSprite, true);
            miniTitle = Txt(Rect("Title", p, TL, TR, TC, new Vector2(0, -8), new Vector2(0, 44)), "", 32, Ink, TextAnchor.MiddleCenter, 0, true);
            miniTrack = Rect("Track", p, CC, CC, CC, new Vector2(0, -6), new Vector2(520, 36));
            Img(miniTrack, new Color(0.45f, 0.3f, 0.2f, 0.3f), roundSprite, true);
            miniZone = Rect("Zone", miniTrack, new Vector2(0, 0), new Vector2(0, 1), CC, Vector2.zero, new Vector2(100, -4));
            Img(miniZone, new Color(0.35f, 0.85f, 0.4f, 0.9f), roundSprite, true);
            var fill = Rect("Fill", miniTrack, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-6, -6));
            miniFill = Img(fill, Water, roundSprite, false);
            miniFill.type = Image.Type.Filled; miniFill.fillMethod = Image.FillMethod.Horizontal;
            miniNeedle = Rect("Needle", miniTrack, new Vector2(0, 0), new Vector2(0, 1), CC, Vector2.zero, new Vector2(10, 22));
            Img(miniNeedle, Red, roundSprite, true, Edge, 2);
            miniPips = new Text[3];
            for (int i = 0; i < 3; i++) miniPips[i] = Txt(Rect("Pip" + i, p, BC, BC, BC, new Vector2(-40 + i * 40, 38), new Vector2(40, 40)), "o", 34, Edge, TextAnchor.MiddleCenter, 0, true);
            miniHint = Txt(Rect("Hint", p, BL, BR, BC, new Vector2(0, 8), new Vector2(0, 30)), "", 22, Edge, TextAnchor.MiddleCenter, 0, true);
            miniGO = p.gameObject; miniGO.SetActive(false);
        }

        public void SetMiniGame(bool on, string title, MiniGame.Kind kind, float needle, float zone, float zoneW, float fill, int hits, int need, float flash, bool good)
        {
            if (miniGO == null) return;
            if (miniGO.activeSelf != on) miniGO.SetActive(on);
            if (!on) return;
            miniTitle.text = title;
            float w = miniTrack.rect.width;
            bool timing = kind == MiniGame.Kind.Timing;
            miniZone.gameObject.SetActive(timing); miniNeedle.gameObject.SetActive(timing); miniFill.gameObject.SetActive(!timing);
            foreach (var pp in miniPips) pp.gameObject.SetActive(timing);
            if (timing)
            {
                miniZone.anchoredPosition = new Vector2(zone * w, 0); miniZone.sizeDelta = new Vector2(zoneW * w, -4);
                miniNeedle.anchoredPosition = new Vector2(needle * w, 0);
                for (int i = 0; i < miniPips.Length; i++) { miniPips[i].text = i < hits ? "●" : "○"; miniPips[i].color = i < hits ? new Color(0.25f, 0.7f, 0.3f) : Edge; }
                miniHint.text = "[E] when it's in the green!";
            }
            else
            {
                miniFill.fillAmount = fill;
                miniHint.text = "Tap [E] fast!";
                miniTrack.localScale = Vector3.one * (1f + flash * 0.6f);
            }
            miniFlash.color = flash > 0f ? (good ? new Color(0.4f, 1f, 0.5f, 0.35f) : new Color(1f, 0.3f, 0.3f, 0.45f)) : new Color(1, 1, 1, 0);
        }

        // ------------------------------------------------------------------ mini game v2 (real tap wheel / gas knob)
        GameObject m2GO, valveGO, gasGO; Text m2Title, m2Hint, keyA, keyD; Image m2Flash, ring, stream, wedge; RectTransform wheel, pointer, keyART, keyDRT; Text[] gPips;

        void BuildMini2(RectTransform H)
        {
            var p = Rect("MiniGame2", H, BC, BC, BC, new Vector2(0, 250), new Vector2(560, 380));
            p.localRotation = Quaternion.Euler(0, 0, 0.6f);
            Img(p, Paper, roundSprite, true, Edge, 5);
            m2Flash = Img(Rect("Flash", p, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-12, -12)), new Color(1, 1, 1, 0), roundSprite, true);
            m2Title = Txt(Rect("Title", p, TL, TR, TC, new Vector2(0, -10), new Vector2(0, 46)), "", 32, Ink, TextAnchor.MiddleCenter, 0, true);
            m2Hint = Txt(Rect("Hint", p, BL, BR, BC, new Vector2(0, 10), new Vector2(0, 32)), "", 24, Edge, TextAnchor.MiddleCenter, 0, true);

            // ---- tap: spout + water stream + red wheel
            var v = Rect("Valve", p, CC, CC, CC, new Vector2(0, -6), new Vector2(520, 280));
            valveGO = v.gameObject;
            var chrome = new Color(0.78f, 0.82f, 0.86f);
            Img(Rect("Pipe", v, CC, CC, CC, new Vector2(0, 100), new Vector2(40, 90)), chrome, roundSprite, true, Edge, 2);
            Img(Rect("Spout", v, CC, CC, CC, new Vector2(70, 62), new Vector2(150, 34)), chrome, roundSprite, true, Edge, 2);
            Img(Rect("SpoutTip", v, CC, CC, CC, new Vector2(132, 40), new Vector2(30, 36)), chrome, roundSprite, true, Edge, 2);
            var st = Rect("Stream", v, CC, CC, TC, new Vector2(132, 22), new Vector2(18, 130));
            stream = Img(st, new Color(0.35f, 0.65f, 1f, 0.85f), roundSprite, true);
            var ringRT = Rect("Ring", v, CC, CC, CC, new Vector2(-30, -10), new Vector2(200, 200));
            ring = Img(ringRT, new Color(0.35f, 0.8f, 0.4f, 0.9f), circleSprite, false);
            ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = 2; ring.fillClockwise = true;
            Img(Rect("RingBack", ringRT, CC, CC, CC, Vector2.zero, new Vector2(176, 176)), Paper, circleSprite, false);
            wheel = Rect("Wheel", ringRT, CC, CC, CC, Vector2.zero, new Vector2(160, 160));
            Img(wheel, Red, circleSprite, false, Edge, 3);
            Img(Rect("Inner", wheel, CC, CC, CC, Vector2.zero, new Vector2(116, 116)), Paper, circleSprite, false);
            for (int i = 0; i < 4; i++) { var sp = Rect("Spoke", wheel, CC, CC, CC, Vector2.zero, new Vector2(18, 140)); sp.localRotation = Quaternion.Euler(0, 0, i * 45f); Img(sp, Red, roundSprite, true); }
            Img(Rect("Hub", wheel, CC, CC, CC, Vector2.zero, new Vector2(40, 40)), chrome, circleSprite, false, Edge, 2);
            keyART = Rect("KeyA", v, CC, CC, CC, new Vector2(-200, -10), new Vector2(70, 70));
            Img(keyART, PaperDark, roundSprite, true, Edge, 3); keyA = Txt(keyART, "A", 40, Ink, TextAnchor.MiddleCenter, 0, true);
            keyDRT = Rect("KeyD", v, CC, CC, CC, new Vector2(215, -10), new Vector2(70, 70));
            Img(keyDRT, PaperDark, roundSprite, true, Edge, 3); keyD = Txt(keyDRT, "D", 40, Ink, TextAnchor.MiddleCenter, 0, true);

            // ---- gas knob dial
            var gk = Rect("Gas", p, CC, CC, CC, new Vector2(0, -6), new Vector2(520, 280));
            gasGO = gk.gameObject;
            var dial = Rect("Dial", gk, CC, CC, CC, new Vector2(0, -6), new Vector2(230, 230));
            Img(dial, new Color(0.2f, 0.22f, 0.25f), circleSprite, false, Edge, 4);
            var wRT = Rect("OffWedge", dial, CC, CC, CC, Vector2.zero, new Vector2(214, 214));
            wedge = Img(wRT, new Color(0.35f, 0.85f, 0.4f, 0.95f), circleSprite, false);
            wedge.type = Image.Type.Filled; wedge.fillMethod = Image.FillMethod.Radial360; wedge.fillOrigin = 2; wedge.fillClockwise = true;
            Img(Rect("Face", dial, CC, CC, CC, Vector2.zero, new Vector2(150, 150)), new Color(0.93f, 0.93f, 0.9f), circleSprite, false);
            Txt(Rect("OFF", dial, CC, CC, CC, new Vector2(0, 140), new Vector2(90, 34)), "OFF", 28, new Color(0.25f, 0.7f, 0.3f), TextAnchor.MiddleCenter, 0, true);
            Txt(Rect("LO", dial, CC, CC, CC, new Vector2(-150, -40), new Vector2(60, 30)), "LO", 24, Orange, TextAnchor.MiddleCenter, 0, true);
            Txt(Rect("HI", dial, CC, CC, CC, new Vector2(150, -40), new Vector2(60, 30)), "HI", 24, Red, TextAnchor.MiddleCenter, 0, true);
            pointer = Rect("Pointer", dial, CC, CC, BC, Vector2.zero, new Vector2(16, 98));
            Img(pointer, Red, roundSprite, true, Edge, 2);
            Img(Rect("Knob", dial, CC, CC, CC, Vector2.zero, new Vector2(70, 70)), new Color(0.15f, 0.15f, 0.17f), circleSprite, false, Edge, 2);
            gPips = new Text[3];
            for (int i = 0; i < 3; i++) gPips[i] = Txt(Rect("Pip" + i, gk, CC, CC, CC, new Vector2(200, 60 - i * 50), new Vector2(50, 50)), "○", 40, Edge, TextAnchor.MiddleCenter, 0, true);
            m2GO = p.gameObject; m2GO.SetActive(false);
        }

        public void SetMini2(bool on, string title, MiniGame.Kind kind, float a, float b, float c, float d, int e, float flash, bool good)
        {
            if (m2GO == null) return;
            if (m2GO.activeSelf != on) m2GO.SetActive(on);
            if (!on) return;
            m2Title.text = title;
            bool valve = kind == MiniGame.Kind.Mash;
            valveGO.SetActive(valve); gasGO.SetActive(!valve);
            if (valve)
            {
                float prog = Mathf.Clamp01(a / Mathf.Max(1f, b));
                wheel.localRotation = Quaternion.Euler(0, 0, -a);
                ring.fillAmount = prog;
                stream.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(18f, 4f, prog), Mathf.Lerp(130f, 20f, prog));
                stream.color = new Color(0.35f, 0.65f, 1f, Mathf.Lerp(0.85f, 0.3f, prog));
                bool wantD = e == 1;
                keyART.localScale = Vector3.one * (wantD ? 0.85f : 1.15f + Mathf.Sin(Time.time * 14f) * 0.05f);
                keyDRT.localScale = Vector3.one * (wantD ? 1.15f + Mathf.Sin(Time.time * 14f) * 0.05f : 0.85f);
                keyA.color = wantD ? Edge : Red; keyD.color = wantD ? Red : Edge;
                m2Hint.text = "[A] [D] [A] [D]...  don't let it slip back!";
            }
            else
            {
                pointer.localRotation = Quaternion.Euler(0, 0, -a);
                wedge.fillAmount = b * 2f / 360f;
                wedge.rectTransform.localRotation = Quaternion.Euler(0, 0, b);
                int hits = Mathf.RoundToInt(c);
                for (int i = 0; i < gPips.Length; i++) { gPips[i].text = i < hits ? "●" : "○"; gPips[i].color = i < hits ? new Color(0.25f, 0.7f, 0.3f) : Edge; }
                m2Hint.text = "[E] right when it points at OFF!";
            }
            m2Flash.color = flash > 0f ? (good ? new Color(0.4f, 1f, 0.5f, 0.3f) : new Color(1f, 0.3f, 0.3f, 0.45f)) : new Color(1, 1, 1, 0);
            m2GO.transform.localScale = Vector3.one * (1f + (flash > 0f && !good ? Mathf.Sin(Time.time * 60f) * 0.015f : 0f));
        }

        // ------------------------------------------------------------------ kid speech bubble
        RectTransform bubble; Text bubbleText; float bubbleT; Vector2 bubblePos;

        void BuildBubble(RectTransform H)
        {
            bubble = Rect("KidBubble", H, CC, CC, BC, Vector2.zero, new Vector2(330, 64));
            Img(bubble, Color.white, roundSprite, true, Edge, 3);
            var tail = Rect("Tail", bubble, BC, BC, TC, new Vector2(0, 4), new Vector2(30, 24));
            Txt(tail, "▼", 26, Color.white, TextAnchor.UpperCenter, 0, true);
            bubbleText = Txt(bubble, "", 26, Ink, TextAnchor.MiddleCenter, 12, true);
            bubble.gameObject.SetActive(false);
        }

        public void KidSay(string s, float seconds = 2.8f)
        {
            if (bubble == null || string.IsNullOrEmpty(s)) return;
            bubbleText.text = s;
            bubble.sizeDelta = new Vector2(Mathf.Clamp(s.Length * 15f + 40f, 140f, 460f), 64f);
            bubbleT = seconds;
            bubble.localScale = Vector3.one * 0.6f;
            bubble.gameObject.SetActive(true);
        }

        public void TickBubble(Transform head)
        {
            if (bubble == null) return;
            if (bubbleT > 0f) bubbleT -= Time.deltaTime;
            bool on = bubbleT > 0f && head && Camera.main;
            if (bubble.gameObject.activeSelf != on) bubble.gameObject.SetActive(on);
            if (!on) return;
            Vector3 sp = Camera.main.WorldToScreenPoint(head.position + Vector3.up * 1.45f);
            if (sp.z < 0f) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bubble.parent, sp, null, out Vector2 local);
            bubblePos = Vector2.Lerp(bubblePos == Vector2.zero ? local : bubblePos, local, 1f - Mathf.Exp(-20f * Time.deltaTime));
            bubble.anchoredPosition = bubblePos;
            bubble.localScale = Vector3.Lerp(bubble.localScale, Vector3.one, Time.deltaTime * 14f);
        }

        // ------------------------------------------------------------------ guide markers
        class Guide { public RectTransform root, arrow, labelRT; public Image disc; public Text glyph, label; public Vector2 pos; public bool init; }
        readonly Guide[] guides = new Guide[6];

        void BuildGuides(RectTransform H)
        {
            for (int i = 0; i < guides.Length; i++)
            {
                var g = new Guide();
                float size = i == 0 ? 64f : (i == 1 ? 54f : 40f);
                g.root = Rect("Guide" + i, H, CC, CC, CC, Vector2.zero, new Vector2(size, size));
                var arrowRT = Rect("Arrow", g.root, CC, CC, CC, Vector2.zero, new Vector2(size * 1.9f, size * 1.9f));
                var at = Txt(arrowRT, "▼", Mathf.RoundToInt(size * 0.55f), Edge, TextAnchor.LowerCenter, 0, true);
                g.arrow = arrowRT;
                g.disc = Img(g.root, Paper, circleSprite, false, Edge, 3);
                g.glyph = Txt(g.root, "E", Mathf.RoundToInt(size * 0.55f), Ink, TextAnchor.MiddleCenter, 0, true);
                g.labelRT = Rect("Label", g.root, TC, TC, TC, new Vector2(0, size * 0.5f + 34f), new Vector2(240, 34));
                Img(g.labelRT, new Color(0.25f, 0.13f, 0.07f, 0.75f), roundSprite, true);
                g.label = Txt(g.labelRT, "", i <= 1 ? 22 : 17, Color.white, TextAnchor.MiddleCenter, 6, true);
                g.root.gameObject.SetActive(false);
                guides[i] = g;
            }
        }

        /// <summary>Small floating marker over a world position; clamps to the screen edge with an arrow when off-screen.</summary>
        public void SetGuide(int i, bool on, Vector3 world, string glyph, string label, Color col)
        {
            if (i < 0 || i >= guides.Length || guides[i] == null) return;
            var g = guides[i];
            var cam = Camera.main;
            if (!on || !cam) { if (g.root.gameObject.activeSelf) g.root.gameObject.SetActive(false); g.init = false; return; }
            if (!g.root.gameObject.activeSelf) g.root.gameObject.SetActive(true);
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
            float scale = root.lossyScale.x;
            float left = 90f * scale, right = Screen.width - 350f * scale;
            float bottom = 310f * scale, top = Screen.height - 250f * scale;
            bool off = behind || sp.x < left || sp.x > right || sp.y < bottom || sp.y > top;
            Vector2 center = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            Vector2 p2 = new Vector2(sp.x, sp.y);
            if (off)
            {
                Vector2 d = p2 - center; if (d.sqrMagnitude < 1f) d = Vector2.down;
                float sx = (right - left) * 0.5f / Mathf.Max(1e-3f, Mathf.Abs(d.x)), sy = (top - bottom) * 0.5f / Mathf.Max(1e-3f, Mathf.Abs(d.y));
                p2 = center + d * Mathf.Min(sx, sy);
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f;
                g.arrow.localRotation = Quaternion.Euler(0, 0, ang);
            }
            else
            {
                p2.y += 28f + Mathf.Sin(Time.time * 4f + i) * 6f;
                g.arrow.localRotation = Quaternion.identity;
            }
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)g.root.parent, p2, null, out Vector2 local);
            g.pos = g.init ? Vector2.Lerp(g.pos, local, 1f - Mathf.Exp(-18f * Time.deltaTime)) : local; g.init = true;
            g.root.anchoredPosition = g.pos;
            g.disc.color = Color.Lerp(Paper, col, 0.35f);
            g.glyph.text = glyph; g.glyph.color = col;
            g.label.text = label;
            g.labelRT.gameObject.SetActive(!string.IsNullOrEmpty(label));
            g.labelRT.anchoredPosition = new Vector2(off ? (p2.x < center.x ? 115f : -115f) : 0f, off ? -45f : 48f);
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.05f;
            g.root.localScale = Vector3.one * (i >= 2 ? 1f + Mathf.Sin(Time.time * 6f + i) * 0.03f : pulse);
            var cg = g.root.GetComponent<CanvasGroup>(); if (!cg) cg = g.root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = i == 0 ? 1f : (i == 1 ? 0.85f : 0.95f);
        }

        Row MakeRow(RectTransform panel, float y, string title, Color col)
        {
            var r = new Row();
            var rt = Rect(title, panel, TL, TR, TC, new Vector2(0, y), new Vector2(-28, 74));
            r.go = rt.gameObject;
            var ic = Rect("Icon", rt, TL, TL, TL, new Vector2(4, -4), new Vector2(60, 60));
            r.icon = Img(ic, col, circleSprite, false);
            Txt(ic, title.Substring(0, 1), 30, Color.white, TextAnchor.MiddleCenter, 0, true);
            r.title = Txt(Rect("Title", rt, TL, TR, TL, new Vector2(76, -2), new Vector2(-80, 32)), title, 24, Ink, TextAnchor.MiddleLeft, 0, true);
            r.status = Txt(Rect("Status", rt, TL, TR, TL, new Vector2(76, -2), new Vector2(-80, 32)), "", 20, Edge, TextAnchor.MiddleRight, 0, true);
            var bar = Rect("Bar", rt, TL, TR, TL, new Vector2(76, -40), new Vector2(-80, 20));
            Img(bar, new Color(0.45f, 0.3f, 0.2f, 0.22f), roundSprite, true);
            var f = Rect("Fill", bar, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-4, -4));
            r.fill = Img(f, col, roundSprite, false);
            r.fill.type = Image.Type.Filled; r.fill.fillMethod = Image.FillMethod.Horizontal; r.fill.fillAmount = 0f;
            return r;
        }

        public void SetRow(Row r, float v, string status, bool alert)
        {
            r.fill.fillAmount = Mathf.Lerp(r.fill.fillAmount, Mathf.Clamp01(v), Time.deltaTime * 5f);
            r.status.text = status;
            r.status.color = alert ? Color.Lerp(Red, Ink, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f)) : Edge;
            r.go.transform.localScale = Vector3.one * (alert ? 1f + 0.03f * Mathf.Sin(Time.time * 10f) : 1f);
        }

        void BuildTitle()
        {
            var full = Rect("Title", root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
            Img(full, new Color(0.2f, 0.1f, 0.05f, 0.55f), null, false);
            var card = Rect("Card", full, CC, CC, CC, Vector2.zero, new Vector2(1260, 840));
            card.localRotation = Quaternion.Euler(0, 0, -1f);
            Img(card, Paper, roundSprite, true, Edge, 6);
            var band = Rect("Band", card, TC, TC, TC, new Vector2(0, -40), new Vector2(980, 170));
            band.localRotation = Quaternion.Euler(0, 0, 2f);
            Img(band, Pink, roundSprite, true, Edge, 4);
            Shadowed(Txt(band, "MAMA'S COMING!", 104, Color.white, TextAnchor.MiddleCenter, 0, true));
            Txt(Rect("Sub", card, TC, TC, TC, new Vector2(0, -240), new Vector2(1120, 140)),
                "You dozed off on the sofa... the BATHTUB is overflowing, the CURRY is about to catch fire\nand smoke is creeping through the house. Fix it - or fake it - before Mom gets home!",
                32, Ink, TextAnchor.MiddleCenter, 0, true);
            Txt(Rect("Controls", card, TC, TC, TC, new Vector2(0, -430), new Vector2(1160, 250)),
                "WASD move     SHIFT run (wet floor = you WILL slip)     SPACE jump     MOUSE look\n" +
                "E interact / pick up     HOLD E valves, drains, knobs     Q drop     RMB throw\n" +
                "Hold LMB  use item: mop / squeegee / extinguisher / scrub     SCROLL zoom     ESC mouse",
                25, Edge, TextAnchor.MiddleCenter, 0, false);
            var go = Rect("Go", card, BC, BC, BC, new Vector2(0, 44), new Vector2(560, 86));
            Img(go, Teal, roundSprite, true, Edge, 3);
            Shadowed(Txt(go, "Press any key to wake up", 36, Color.white, TextAnchor.MiddleCenter, 0, true));
            titleGO = full.gameObject;
        }

        void BuildEnd()
        {
            var full = Rect("End", root, Vector2.zero, Vector2.one, CC, Vector2.zero, Vector2.zero);
            Img(full, new Color(0.2f, 0.1f, 0.05f, 0.6f), null, false);
            var card = Rect("Card", full, CC, CC, CC, Vector2.zero, new Vector2(1200, 860));
            card.localRotation = Quaternion.Euler(0, 0, 1f);
            Img(card, Paper, roundSprite, true, Edge, 6);
            var band = Rect("Band", card, TC, TC, TC, new Vector2(0, -40), new Vector2(960, 150));
            band.localRotation = Quaternion.Euler(0, 0, -2f);
            endBand = Img(band, Red, roundSprite, true, Edge, 4);
            endTitle = Txt(band, "BUSTED!", 84, Color.white, TextAnchor.MiddleCenter, 0, true);
            Shadowed(endTitle);
            endBody = Txt(Rect("Body", card, TC, TC, TC, new Vector2(0, -210), new Vector2(1060, 120)), "", 34, Ink, TextAnchor.MiddleCenter, 0, true);
            endStats = Txt(Rect("Stats", card, TC, TC, TC, new Vector2(0, -340), new Vector2(1060, 400)), "", 26, Edge, TextAnchor.UpperCenter, 0, true);
            var go = Rect("Again", card, BC, BC, BC, new Vector2(0, 40), new Vector2(520, 80));
            Img(go, Teal, roundSprite, true, Edge, 3);
            Shadowed(Txt(go, "Press R to try again", 36, Color.white, TextAnchor.MiddleCenter, 0, true));
            endGO = full.gameObject;
            endGO.SetActive(false);
        }

        // ------------------------------------------------------------------ API
        public void ShowTitle(bool on) { titleGO.SetActive(on); hud.SetActive(!on); }

        public void SetObjectives(string main, string count, IList<string> rest)
        {
            objMain.text = MamaReadability.Text("Leave no evidence for Mom", "จัดการบ้านก่อนแม่จับได้");
            objCount.text = count;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < rest.Count && i < 7; i++) sb.Append("o  ").Append(rest[i]).Append('\n');
            objList.text = sb.ToString();
            if (overviewList) overviewList.text = MamaReadability.Text("THINGS MOM COULD NOTICE\n", "สิ่งที่แม่อาจสังเกตเห็น\n") + "• " + main + "\n" + sb;
            objList.transform.parent.gameObject.SetActive(false);
        }

        public void SetTimer(float secondsLeft, bool momHome)
        {
            if (momHome)
            {
                timerSub.text = "MOM IS"; timerText.text = "HOME"; timerText.fontSize = 32;
                timerDisc.color = Color.Lerp(Paper, Red, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
                return;
            }
            int s = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft));
            timerText.fontSize = 40;
            timerText.text = (s / 60) + ":" + (s % 60).ToString("00");
            timerSub.text = "MOM IN";
            if (secondsLeft < 60f) timerDisc.color = Color.Lerp(Paper, Red, 0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
            else if (secondsLeft < 120f) timerDisc.color = Color.Lerp(Paper, Orange, 0.6f);
            else timerDisc.color = Paper;
        }

        public void SetHeld(string name, float meter)
        {
            heldText.text = string.IsNullOrEmpty(name) ? "Empty hands" : name;
            heldIcon.text = string.IsNullOrEmpty(name) ? "~" : name.Substring(0, 1).ToUpper();
            heldSoak.transform.parent.gameObject.SetActive(meter >= 0f);
            if (meter >= 0f) heldSoak.fillAmount = Mathf.Clamp01(meter);
        }

        public void SetPrompt(string s)
        {
            bool on = !string.IsNullOrEmpty(s);
            if (promptGO.activeSelf != on) promptGO.SetActive(on);
            if (on) promptText.text = s;
        }

        public void SetHold(float p)
        {
            bool on = p > 0.001f;
            if (holdGO.activeSelf != on) holdGO.SetActive(on);
            if (on) holdFill.fillAmount = p;
        }

        public void SetAlarm(bool beeping)
        {
            float a = beeping ? 0.018f + 0.012f * Mathf.Sin(Time.time * 3f) : 0f;
            alarmFlash.color = new Color(1f, 0.1f, 0.1f, a);
        }

        public void SetMess(float v)
        {
            meterFill.fillAmount = Mathf.Lerp(meterFill.fillAmount, v, Time.deltaTime * 4f);
            meterFill.color = Color.Lerp(Teal, Red, meterFill.fillAmount);
        }

        public void Toast(string s)
        {
            if (string.IsNullOrEmpty(s) || toastQ.Contains(s) || (toastGO.activeSelf && toastText.text == s)) return;
            if (toastQ.Count < 4) toastQ.Enqueue(s);
        }

        public void Call(string s, Color? col = null)
        {
            callText.text = s;
            callGO.GetComponent<Image>().color = col ?? Pink;
            callGO.SetActive(true);
            callT = 4.5f;
        }

        public void MomSay(string s) { if (momQ.Count < 8) momQ.Enqueue(s); }

        public void ShowEnding(string title, string body, string stats, Color accent)
        {
            hud.SetActive(false);
            alarmFlash.color = Color.clear;
            endTitle.text = title; endBody.text = body; endStats.text = stats; endBand.color = accent;
            endGO.SetActive(true);
        }

        void Update()
        {
            RefreshReadabilityCopy();
            RefreshPolishedCopy();
            if (toastGO == null) return;
            float dt = Time.deltaTime;
            if (toastT > 0f && callT <= 0f && momT <= 0f) toastT -= dt;
            if (toastT <= 0f)
            {
                if (toastQ.Count > 0) { toastText.text = toastQ.Dequeue(); toastGO.SetActive(true); toastT = 2.6f; }
                else if (toastGO.activeSelf) toastGO.SetActive(false);
            }
            if (callT > 0f)
            {
                if (momT <= 0f) callT -= dt;
                callGO.transform.localScale = Vector3.one;
                if (callT <= 0f) callGO.SetActive(false);
            }
            if (momT > 0f) momT -= dt;
            if (momT <= 0f)
            {
                if (momQ.Count > 0) { momText.text = momQ.Dequeue(); momGO.SetActive(true); momT = 2.6f; }
                else if (momGO.activeSelf) momGO.SetActive(false);
            }
            // One notification lane: speech > emergency > routine confirmation.
            bool speaking = momT > 0f && momGO.activeSelf;
            callGO.SetActive(callT > 0f && !speaking);
            toastGO.SetActive(toastT > 0f && callT <= 0f && !speaking);
        }

        // ------------------------------------------------------------------ helpers
        RectTransform Rect(string name, RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        Image Img(RectTransform rt, Color c, Sprite s, bool sliced, Color? outline = null, float outlineSize = 0f)
        {
            var im = rt.gameObject.AddComponent<Image>();
            im.color = c;
            im.raycastTarget = false;
            if (s) { im.sprite = s; im.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            if (outline.HasValue)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = outline.Value;
                o.effectDistance = new Vector2(outlineSize, -outlineSize);
            }
            return im;
        }

        Text Txt(RectTransform parent, string s, int size, Color c, TextAnchor a, float pad, bool bold)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, 0); rt.offsetMax = new Vector2(-pad, 0);
            var t = go.AddComponent<Text>();
            t.font = font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Text Shadowed(Text t)
        {
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.35f, 0.15f, 0.08f, 0.6f);
            sh.effectDistance = new Vector2(2, -3);
            return t;
        }
    }
}
