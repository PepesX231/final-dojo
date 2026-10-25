using UnityEngine;
using UnityEngine.UI;

namespace MamasComing
{
    /// Owner: no "2P" signs floating around the house. Only when you press E on a job that needs two kids
    /// and nobody is on the other spot, a warning pops up for a moment: this needs 2 players - call a friend.
    public class MamaCoopBanner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!FindFirstObjectByType<MamaCoopBanner>()) { var go = new GameObject("Coop warning"); DontDestroyOnLoad(go); go.AddComponent<MamaCoopBanner>(); } }

        RectTransform banner; Text text; float left, pop; string msg;
        static string T(string en, string th) => Loc.Thai ? th : en;

        void Build()
        {
            var canvas = MamaFlowUI.Canvas("Coop warning", transform, 45);
            banner = MamaFlowUI.Panel(canvas.transform, "Warning", new Vector2(0, 150), new Vector2(820, 96), MamaTheme.PaintS, MamaTheme.Pink);
            banner.GetComponent<Image>().raycastTarget = false;
            text = MamaFlowUI.Text(banner, "Text", Vector2.zero, new Vector2(780, 84), "", 30, Color.white, TextAnchor.MiddleCenter);
            text.font = MamaTheme.Bold; var o = text.gameObject.AddComponent<Outline>(); o.effectColor = new Color(.45f, .12f, .25f, .85f); o.effectDistance = new Vector2(2, -2);
            banner.gameObject.SetActive(false);
        }

        /// Show the warning from anywhere (e.g. a co-op job that was tried alone).
        public static void Warn(string en, string th) { var b = FindFirstObjectByType<MamaCoopBanner>(); if (b) b.Show(T(en, th)); }
        void Show(string m) { msg = m; left = 2.6f; pop = 0f; MamaAudio.Play("bad", null, .45f, 1.2f); }

        void LateUpdate()
        {
            if (!banner) Build();
            var gm = GameManager.I;
            bool play = gm && (gm.state == GameManager.State.Playing || gm.state == GameManager.State.MomHome) && !MamaSettings.Open && !(MiniGame.I && MiniGame.I.Active);
            if (play && gm.player && gm.player.controlsEnabled && MamaInput.InteractDown)
            {
                string w = Alone(gm.player);
                if (w != null) Show(w);
            }
            if (!play) left = 0f;
            left -= Time.unscaledDeltaTime; pop = Mathf.Min(1f, pop + Time.unscaledDeltaTime * 7f);
            bool on = left > 0f;
            if (banner.gameObject.activeSelf != on) banner.gameObject.SetActive(on);
            if (!on) return;
            text.text = msg;
            float e = 1f + Mathf.Sin(pop * Mathf.PI) * .12f;
            banner.localScale = Vector3.one * Mathf.Lerp(.6f, 1f, pop) * e;
            var cg = banner.GetComponent<CanvasGroup>(); if (!cg) cg = banner.gameObject.AddComponent<CanvasGroup>(); cg.alpha = Mathf.Clamp01(left / .35f);
        }

        /// The warning for a co-op job you just pressed E on alone (null = fine / not co-op).
        static string Alone(PlayerController me)
        {
            var t = me.Target;
            if (t is CoopStation cs && cs.puzzle && cs.puzzle.Urgent && !cs.puzzle.FriendOnOther(cs, me))
                return T("This needs 2 players!\nCall a friend to the other spot", "ต้องใช้ 2 คน!\nเรียกเพื่อนมาช่วยอีกจุดนึง");
            if (t is HeaterValve hv && hv.heater && hv.heater.Urgent && !hv.FriendOnOther(me))
                return T("This needs 2 players!\nA friend must turn the OTHER valve at the same time", "ต้องใช้ 2 คน!\nให้เพื่อนหมุนอีกวาล์วพร้อมกัน");
            if (t is SofaHazard sh && sh.Urgent)
                return T("Too heavy alone - call a friend to lift it with you!", "หนักมาก! ยกคนเดียวช้า - เรียกเพื่อนมาช่วยยก");
            return null;
        }
    }
}
