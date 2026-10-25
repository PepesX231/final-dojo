using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    public partial class MamaUI
    {
        Text notesKey;
        void BuildPolishedLayout(RectTransform h)
        {
            MovePanel(h, "ObjBanner", new Vector2(28, -26), new Vector2(440, 96));
            objMain.fontSize = 26;
            objMain.rectTransform.offsetMin = new Vector2(18, 34);
            objMain.rectTransform.offsetMax = new Vector2(-16, -8);
            goalSub.fontSize = 18;
            goalSub.rectTransform.anchoredPosition = new Vector2(18, 7);
            goalSub.rectTransform.sizeDelta = new Vector2(-32, 26);
            MovePanel(h, "RoomName", new Vector2(32, -112), new Vector2(400, 30));
            roomLabel.fontSize = 21;
            MovePanel(h, "Timer", new Vector2(-28, -26), new Vector2(178, 78));
            timerDisc.sprite = roundSprite; timerDisc.type = Image.Type.Sliced;
            h.Find("Timer/Inner").GetComponent<Image>().color = new Color(0.16f, 0.37f, 0.34f);
            h.Find("Timer/Inner").GetComponent<Image>().sprite = roundSprite;
            h.Find("Timer/Inner").GetComponent<Image>().type = Image.Type.Sliced;
            MovePanel(h, "Timer/Inner", Vector2.zero, new Vector2(168, 68));
            MovePanel(h, "Timer/Inner/Time", new Vector2(0, -8), new Vector2(166, 46));
            MovePanel(h, "Timer/Inner/Sub", new Vector2(0, 23), new Vector2(166, 22));
            timerSub.fontSize = 17;
            MovePanel(h, "Problems", new Vector2(-28, -128), new Vector2(300, 248));
            h.Find("Problems").gameObject.SetActive(false);
            MovePanel(h, "ContextCard", new Vector2(28, 26), new Vector2(500, 236));
            MovePanel(h, "ContextCard/Name", new Vector2(18, -8), new Vector2(-36, 34));
            MovePanel(h, "ContextCard/Meaning", new Vector2(18, -47), new Vector2(-36, 140));
            MovePanel(h, "ContextCard/Keys", new Vector2(18, 8), new Vector2(-36, 28));
            contextTitle.fontSize = 27; contextBody.fontSize = 24; contextKeys.fontSize = 20;
            contextGO.SetActive(false);
            MovePanel(h, "Prompt", new Vector2(0, 32), new Vector2(720, 52));
            promptText.fontSize = 24;
            MovePanel(h, "Hold", new Vector2(0, 91), new Vector2(300, 10));
            MovePanel(h, "Toast", new Vector2(0, -30), new Vector2(670, 76));
            MovePanel(h, "Call", new Vector2(0, -30), new Vector2(670, 76));
            var speech = (RectTransform)momGO.transform;
            speech.anchorMin = speech.anchorMax = speech.pivot = TC;
            speech.anchoredPosition = new Vector2(0, -30); speech.sizeDelta = new Vector2(670, 92);
            speech.Find("Tag").gameObject.SetActive(false);
            callText.fontSize = 24; toastText.fontSize = 24; momText.fontSize = 24;
            foreach (var text in new[] { callText, toastText, momText })
            {
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 21; text.resizeTextMaxSize = 24;
                text.verticalOverflow = VerticalWrapMode.Truncate;
            }
            notesKey = Txt(Rect("NotesKey", h, BR, BR, BR, new Vector2(-28, 28), new Vector2(420, 32)), "", 19, Color.white, TextAnchor.MiddleRight, 0, true);
            Shadowed(notesKey);
            foreach (var outline in h.GetComponentsInChildren<Outline>(true))
            {
                if (outline.GetComponent<Text>()) continue;
                outline.effectDistance = new Vector2(1, -1);
                outline.effectColor = new Color(0.18f, 0.24f, 0.23f, 0.5f);
            }
            var card = titleGO.transform.Find("Card") as RectTransform;
            card.anchorMin = card.anchorMax = new Vector2(0.25f, 0.5f);
            card.anchoredPosition = Vector2.zero; card.sizeDelta = new Vector2(720, 650);
            MovePanel(card, "Band", new Vector2(0, -32), new Vector2(658, 100));
            card.Find("Band").localRotation = Quaternion.identity;
            card.Find("Band").GetComponent<Image>().color = new Color(0.72f, 0.29f, 0.26f);
            card.Find("Band").GetComponentInChildren<Text>().fontSize = 68;
            MovePanel(card, "Sub", new Vector2(0, -164), new Vector2(636, 140));
            card.Find("Sub").GetComponentInChildren<Text>().fontSize = 30;
            MovePanel(card, "Controls", new Vector2(0, -332), new Vector2(636, 178));
            card.Find("Controls").GetComponentInChildren<Text>().fontSize = 24;
            MovePanel(card, "Go", new Vector2(0, 38), new Vector2(636, 70));
            card.Find("Go").GetComponentInChildren<Text>().fontSize = 30;
            foreach (var outline in card.GetComponentsInChildren<Outline>(true))
                if (!outline.GetComponent<Text>()) outline.effectDistance = new Vector2(1, -1);
            RefreshPolishedCopy();
        }

        void RefreshPolishedCopy()
        {
            if (!notesKey) return;
            notesKey.text = MamaReadability.Text("TAB Notes · H Hint · F1 Guide · F9 Admin (host)", "TAB ภาพรวม · H คำใบ้ · F1 วิธีเล่น · F9 แอดมิน (เจ้าของห้อง)");
            goalSub.text = MamaReadability.Text("Stop the cause → put away → Mom inspects", "ปิดต้นเหตุ → เก็บของ → รอแม่ตรวจ");
            var card = titleGO.transform.Find("Card");
            card.Find("Sub").GetComponentInChildren<Text>().text = MamaReadability.Text(
                "The secret party is over. The house is a disaster.\nMom is on her way — leave no evidence!",
                "ปาร์ตี้ลับจบแล้ว แต่บ้านยังเละ\nแม่กำลังกลับมา... อย่าให้เห็นหลักฐาน!");
            card.Find("Controls").GetComponentInChildren<Text>().text = MamaReadability.Text(
                "YOUR GOAL: stop disasters, then clean or conceal.\nChoose what to fix. Friends can help.\n\nWASD move   ·   E use / pick up   ·   Q put down\nTAB house notes   ·   ESC settings / co-op",
                "เป้าหมาย: หยุดหายนะ แล้วเก็บกวาดหรือซ่อน\nเลือกทำเองได้ ชวนเพื่อนมาช่วยกัน\n\nWASD เดิน   ·   E ใช้ / หยิบ   ·   Q วาง\nTAB ภาพรวม   ·   ESC ตั้งค่า / เล่นกับเพื่อน");
        }
    }
}
