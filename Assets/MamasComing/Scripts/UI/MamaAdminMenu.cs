using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing
{
    /// F9 = host / solo admin menu during a round (works in builds too): jump to Mom's inspection, to the real
    /// result, to the defeat screen (with the reason), or straight to the score ranking. Clients can't open it.
    public class MamaAdminMenu : MonoBehaviour
    {
        static MamaAdminMenu inst;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (inst) return; var go = new GameObject("Admin menu (F9)"); DontDestroyOnLoad(go); inst = go.AddComponent<MamaAdminMenu>(); }

        public static bool Open => inst && inst.panel;
        GameObject panel; bool hadControls; CursorLockMode prevLock; bool prevVis;

        static bool CanUse()
        {
            var gm = GameManager.I;
            return gm && NetGame.IsAuthority && (gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome || gm.state == GameManager.State.Intro);
        }

        void Update()
        {
            var k = Keyboard.current; if (k == null) return;
            if (panel && !CanUse()) { Close(); return; }
            if (k.f9Key.wasPressedThisFrame)
            {
                if (panel) Close();
                else if (CanUse()) Show();
                else if (GameManager.I && NetGame.IsClientOnly) GameManager.I.Toast(MamaFlowUI.T("Only the host can open the admin menu (F9).", "เฉพาะเจ้าของห้องเท่านั้นที่เปิดเมนูแอดมิน (F9) ได้"));
                return;
            }
            if (!panel) return;
            if (k.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) Do(1);
            else if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) Do(2);
            else if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) Do(3);
            else if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) Do(4);
        }

        void Show()
        {
            var gm = GameManager.I;
            var c = MamaFlowUI.Canvas("Admin menu", transform, 400); panel = c.gameObject;
            var dim = MamaUIKit.Stretch("Dim", c.transform); MamaUIKit.Img(dim, null, new Color(0, 0, 0, .45f));
            var box = MamaFlowUI.Panel(c.transform, "Box", Vector2.zero, new Vector2(760, 640), MamaTheme.PaperS, Color.white);
            MamaFlowUI.Text(box, "Title", new Vector2(0, 262), new Vector2(680, 60), MamaFlowUI.T("ADMIN MENU  (host)", "เมนูแอดมิน  (เจ้าของห้อง)"), 40, MamaTheme.Brick, TextAnchor.MiddleCenter);
            MamaFlowUI.Text(box, "Sub", new Vector2(0, 214), new Vector2(680, 34), MamaFlowUI.T("Pick where to jump  •  1-4 or click  •  F9 / Esc closes", "เลือกฉากที่จะข้ามไป  •  กด 1-4 หรือคลิก  •  F9 / Esc ปิด"), 22, MamaTheme.WoodInk, TextAnchor.MiddleCenter);
            Btn(box, 0, "1   " + MamaFlowUI.T("Mom comes home NOW (watch her inspect)", "แม่กลับบ้านเดี๋ยวนี้ (ดูแม่ตรวจ)"), MamaTheme.Teal, 1);
            Btn(box, 1, "2   " + MamaFlowUI.T("Skip to the real result (instant inspection)", "ข้ามไปผลจริงทันที (แม่ตรวจในพริบตา)"), MamaUI.Orange, 2);
            Btn(box, 2, "3   " + MamaFlowUI.T("Show the defeat screen + why we lost", "ดูหน้าแพ้ + เหตุผลที่แพ้"), MamaTheme.Brick, 3);
            Btn(box, 3, "4   " + MamaFlowUI.T("Go straight to the score ranking", "วาปไปหน้าสรุปคะแนนเลย"), MamaTheme.Mustard, 4);
            hadControls = gm.player && gm.player.controlsEnabled; if (gm.player) gm.player.controlsEnabled = false;
            prevLock = Cursor.lockState; prevVis = Cursor.visible; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            MamaAudio.Play("pop", null, .6f, 1.2f);
        }

        void Btn(RectTransform box, int row, string label, Color col, int action)
        {
            var b = MamaFlowUI.Button(box, "Option " + action, new Vector2(0, 120 - row * 104), new Vector2(660, 86), label, col, () => Do(action));
            var t = b.GetComponentInChildren<UnityEngine.UI.Text>(); if (t) t.alignment = TextAnchor.MiddleLeft;
        }

        void Close(bool restore = true)
        {
            if (panel) Destroy(panel); panel = null;
            var gm = GameManager.I;
            if (restore && gm && gm.player && hadControls && gm.state != GameManager.State.Ending) gm.player.controlsEnabled = true;
            if (restore && gm && gm.state != GameManager.State.Ending) { Cursor.lockState = prevLock; Cursor.visible = prevVis; }
        }

        void Do(int action)
        {
            var gm = GameManager.I; if (!gm || !CanUse()) { Close(); return; }
            Close(action == 1);
            MamaAudio.Play("click", null, .7f);
            switch (action)
            {
                case 1: gm.PresentMom(); break;
                case 2: gm.SkipToResults(); break;
                case 3: gm.AdminLose(); break;
                case 4: gm.PresentResult(true); break;
            }
        }
    }
}
