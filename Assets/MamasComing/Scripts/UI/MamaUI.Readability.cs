using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    public partial class MamaUI
    {
        Text contextTitle, contextBody, contextKeys, overviewList, helpTitle, helpBody, roomLabel, goalSub;
        GameObject contextGO, overviewGO, helpGO;
        bool? readabilityThai;

        void MovePanel(Transform parent, string path, Vector2 pos, Vector2 size)
        {
            var t = parent.Find(path) as RectTransform;
            if (!t) return;
            t.anchoredPosition = pos; t.sizeDelta = size; t.localRotation = Quaternion.identity;
        }

        void BuildReadability(RectTransform h)
        {
            // Keep the handmade palette, but reserve the middle of the screen for the house.
            h.Find("ObjBadge").gameObject.SetActive(false);
            h.Find("ObjCount").gameObject.SetActive(false);
            h.Find("MessMeter").gameObject.SetActive(false);
            MovePanel(h, "ObjBanner", new Vector2(28, -28), new Vector2(540, 112));
            objMain.fontSize = 30; objMain.alignment = TextAnchor.UpperLeft;
            objMain.rectTransform.offsetMin = new Vector2(22, 40); objMain.rectTransform.offsetMax = new Vector2(-20, -12);
            goalSub = Txt(Rect("GoalSub", (RectTransform)h.Find("ObjBanner"), BL, BR, BL, new Vector2(22, 14), new Vector2(-38, 30)), "", 21, ReadableInk, TextAnchor.MiddleLeft, 0, false);
            goalSub.text = MamaReadability.Text("Party's over. Fix it together before Mom's inspection!", "ปาร์ตี้จบแล้ว ช่วยกันแก้ก่อนแม่มาตรวจ!");
            roomLabel = Txt(Rect("RoomName", h, TL, TL, TL, new Vector2(32, -151), new Vector2(520, 36)), "", 22, Color.white, TextAnchor.MiddleLeft, 0, true);
            Shadowed(roomLabel);
            MovePanel(h, "Timer", new Vector2(-30, -26), new Vector2(160, 160));
            MovePanel(h, "Timer/Inner", Vector2.zero, new Vector2(138, 138));
            MovePanel(h, "Problems", new Vector2(-28, -205), new Vector2(300, 248));
            var problems = h.Find("Problems");
            foreach (var row in new[] { fireRow, floodRow, smokeRow })
            {
                row.title.fontSize = 22; row.status.fontSize = 20;
                var r = (RectTransform)row.go.transform; r.sizeDelta = new Vector2(-28, 62);
                MovePanel(r, "Icon", new Vector2(4, -6), new Vector2(38, 38));
                row.icon.GetComponentInChildren<Text>().fontSize = 22;
                MovePanel(r, "Title", new Vector2(52, -1), new Vector2(-56, 26));
                MovePanel(r, "Status", new Vector2(52, -28), new Vector2(-56, 26));
                MovePanel(r, "Bar", new Vector2(52, -57), new Vector2(-56, 5));
                row.status.alignment = TextAnchor.MiddleLeft;
            }
            ((RectTransform)fireRow.go.transform).anchoredPosition = new Vector2(0, -44);
            ((RectTransform)floodRow.go.transform).anchoredPosition = new Vector2(0, -110);
            ((RectTransform)smokeRow.go.transform).anchoredPosition = new Vector2(0, -176);
            // Resource bars are secondary; show the physical item and useful words instead of a letter badge.
            h.Find("HeldBadge").gameObject.SetActive(false);
            h.Find("HeldLabel").gameObject.SetActive(false);
            MovePanel(h, "Prompt", new Vector2(0, 46), new Vector2(790, 64));
            promptText.fontSize = 25;
            MovePanel(h, "Hold", new Vector2(0, 120), new Vector2(360, 16));
            MovePanel(h, "Toast", new Vector2(0, -210), new Vector2(820, 66));
            toastText.fontSize = 26;
            MovePanel(h, "Call", new Vector2(0, -100), new Vector2(820, 86));
            callText.fontSize = 27;
            MovePanel(h, "MomSay", new Vector2(0, 164), new Vector2(960, 92));
            momText.fontSize = 28;

            var card = Rect("ContextCard", h, BL, BL, BL, new Vector2(28, 26), new Vector2(490, 190));
            Img(card, Paper, roundSprite, true, Edge, 2);
            Img(Rect("Accent", card, TL, BL, BL, Vector2.zero, new Vector2(5, 0)), Teal, null, false);
            contextTitle = Txt(Rect("Name", card, TL, TR, TL, new Vector2(22, -12), new Vector2(-42, 40)), "", 27, ReadableInk, TextAnchor.MiddleLeft, 0, true);
            contextBody = Txt(Rect("Meaning", card, TL, TR, TL, new Vector2(22, -55), new Vector2(-42, 76)), "", 22, ReadableInk, TextAnchor.UpperLeft, 0, false);
            contextKeys = Txt(Rect("Keys", card, BL, BR, BL, new Vector2(22, 12), new Vector2(-42, 37)), "", 20, ReadableInk, TextAnchor.MiddleLeft, 0, true);
            contextGO = card.gameObject;

            var overview = Rect("HouseNotes", h, TR, TR, TR, new Vector2(-350, -205), new Vector2(540, 465));
            Img(overview, Paper, roundSprite, true, Edge, 2);
            overviewList = Txt(Rect("List", overview, Vector2.zero, Vector2.one, CC, Vector2.zero, new Vector2(-40, -32)), "", 25, ReadableInk, TextAnchor.UpperLeft, 0, false);
            overviewGO = overview.gameObject; overviewGO.SetActive(false);

            var help = Rect("OptionalIdea", h, BL, BL, BL, new Vector2(28, 26), new Vector2(490, 240));
            Img(help, Paper, roundSprite, true, Edge, 2);
            helpTitle = Txt(Rect("Title", help, TL, TR, TL, new Vector2(22, -10), new Vector2(-44, 40)), "", 28, ReadableInk, TextAnchor.MiddleLeft, 0, true);
            helpBody = Txt(Rect("Body", help, TL, TR, TL, new Vector2(22, -57), new Vector2(-44, 168)), "", 22, ReadableInk, TextAnchor.UpperLeft, 0, false);
            helpGO = help.gameObject; helpGO.SetActive(false);

            // A short invitation, not a controls manual or a prescribed solution.
            var titleCard = titleGO.transform.Find("Card");
            titleCard.localRotation = Quaternion.identity;
            var sub = titleCard.Find("Sub").GetComponentInChildren<Text>();
            sub.text = MamaReadability.Text("Mom was away for the day, so you threw a SECRET PARTY... and everyone fell asleep.\nNow the house is flooding, burning and wrecked - and Mom is coming home EARLY.", "แม่ไม่อยู่บ้านทั้งวัน เลยแอบจัดปาร์ตี้กับเพื่อน... แล้วหลับกันหมด\nตื่นมาบ้านน้ำท่วม ไฟไหม้ ของพังเละ - และแม่กำลังจะกลับเร็วกว่าที่คิด!");
            sub.fontSize = 38;
            var controls = titleCard.Find("Controls").GetComponentInChildren<Text>();
            controls.text = MamaReadability.Text("WORK TOGETHER: every room has a problem, and ignored problems cause NEW ones.\nStop the disasters first, then clean up or hide the evidence.\n\nWASD move  •  E use / pick up  •  Q put down  •  RMB throw\nHold H for an idea  •  TAB house notes  •  ESC settings / co-op", "ช่วยกันนะ: ทุกห้องมีปัญหา และปัญหาที่ปล่อยไว้จะลามเป็นปัญหาใหม่\nหยุดหายนะก่อน แล้วค่อยเก็บกวาดหรือซ่อนหลักฐาน\n\nWASD เดิน  •  E ใช้ / หยิบ  •  Q วาง  •  คลิกขวา ขว้าง\nกด H ค้าง คำใบ้  •  TAB ภาพรวม  •  ESC ตั้งค่า / เล่นกับเพื่อน");
            controls.fontSize = 30; controls.color = ReadableInk;
            RefreshReadabilityCopy();
            BuildPolishedLayout(h);
        }

        void RefreshReadabilityCopy()
        {
            if (!goalSub || readabilityThai == Loc.Thai) return;
            readabilityThai = Loc.Thai;
            fireRow.title.text = MamaReadability.Text("HEAT / FIRE", "ความร้อน / ไฟ");
            floodRow.title.text = MamaReadability.Text("WATER", "น้ำบนพื้น");
            smokeRow.title.text = MamaReadability.Text("SMOKE", "ควัน");
            goalSub.text = MamaReadability.Text("Party's over. Fix it together before Mom's inspection!", "ปาร์ตี้จบแล้ว ช่วยกันแก้ก่อนแม่มาตรวจ!");
            var card = titleGO.transform.Find("Card");
            card.Find("Sub").GetComponentInChildren<Text>().text = MamaReadability.Text("Mom was away for the day, so you threw a SECRET PARTY... and everyone fell asleep.\nNow the house is flooding, burning and wrecked - and Mom is coming home EARLY.", "แม่ไม่อยู่บ้านทั้งวัน เลยแอบจัดปาร์ตี้กับเพื่อน... แล้วหลับกันหมด\nตื่นมาบ้านน้ำท่วม ไฟไหม้ ของพังเละ - และแม่กำลังจะกลับเร็วกว่าที่คิด!");
            card.Find("Controls").GetComponentInChildren<Text>().text = MamaReadability.Text("WORK TOGETHER: every room has a problem, and ignored problems cause NEW ones.\nStop the disasters first, then clean up or hide the evidence.\n\nWASD move  •  E use / pick up  •  Q put down  •  RMB throw\nHold H for an idea  •  TAB house notes  •  ESC settings / co-op", "ช่วยกันนะ: ทุกห้องมีปัญหา และปัญหาที่ปล่อยไว้จะลามเป็นปัญหาใหม่\nหยุดหายนะก่อน แล้วค่อยเก็บกวาดหรือซ่อนหลักฐาน\n\nWASD เดิน  •  E ใช้ / หยิบ  •  Q วาง  •  คลิกขวา ขว้าง\nกด H ค้าง คำใบ้  •  TAB ภาพรวม  •  ESC ตั้งค่า / เล่นกับเพื่อน");
        }

        static Color ReadableInk => new Color(0.17f, 0.21f, 0.23f);

        public void Confirm(string message)
        {
            toastQ.Clear();
            toastText.text = message; toastT = 2.2f; toastGO.SetActive(true);
        }

        public void SetContext(string title, string body, string keys, string room)
        {
            if (!contextGO) return;
            contextTitle.text = title; contextBody.text = body; contextKeys.text = keys;
            roomLabel.text = room;
        }

        public void SetReadabilityPanels(bool overview, bool help, string title, string body)
        {
            if (!overviewGO) return;
            overviewGO.SetActive(overview && !help && !QuestDirector.I);
            var problemPanel = hud.transform.Find("Problems");
            if (problemPanel) problemPanel.gameObject.SetActive(overview && !help);
            helpGO.SetActive(help);
            helpTitle.text = title; helpBody.text = body;
            bool mini = MiniGame.I && MiniGame.I.Active;
            contextGO.SetActive(!mini && !help);
        }

        public void SetContextVisible(bool visible)
        {
            if (contextGO) contextGO.SetActive(visible);
        }
    }
}
