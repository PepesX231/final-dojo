using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MamasComing
{
    /// Title screen: cinematic shot of the living room with the fruit kids posed in front of the sunset window,
    /// logo top-left, a wooden menu band (QUIT · OPTIONS · HOW TO PLAY · CO-OP · PLAY) and control hints.
    /// Everything it shows is a normal scene object under FrontEnd/TitleRig - edit it in the Scene / Inspector.
    public class MamaTitleScreen : MonoBehaviour
    {
        public MamaShot shot;
        public Canvas canvas;
        public MamaPosedKid[] kids;
        public MamaMenuItem[] items;
        [Tooltip("Item selected when the title opens (index into Items).")]
        public int startItem = -1;
        public Text status;
        public RectTransform logo;
        [Tooltip("Speech bubbles pop in one after another, this many seconds apart.")]
        public float bubbleInterval = 1.4f;
        public RectTransform[] bubbles;
        [Tooltip("3D canvases (logo banner, menu plank) shown with this screen.")]
        public Canvas[] worldCanvases;
        Vector3 logoScale; Vector3[] bubbleScale;

        public MamaShot CurrentShot => items != null && sel >= 0 && sel < items.Length && items[sel] && items[sel].focus ? items[sel].focus : shot;
        int sel; bool visible; float shownAt, stickCooldown;

        void Awake()
        {
            if (logo) logoScale = logo.localScale;
            bubbleScale = new Vector3[bubbles != null ? bubbles.Length : 0];
            for (int i = 0; i < bubbleScale.Length; i++) if (bubbles[i]) bubbleScale[i] = bubbles[i].localScale;
            foreach (var it in items)
            {
                if (!it) continue;
                it.Hovered = h => Select(System.Array.IndexOf(items, h));
                it.Clicked = c => { Select(System.Array.IndexOf(items, c)); Activate(c); };
            }
        }

        public void SetVisible(bool v)
        {
            visible = v;
            if (canvas) canvas.gameObject.SetActive(v);
            if (worldCanvases != null) foreach (var w in worldCanvases) if (w) w.gameObject.SetActive(v);
            if (kids != null) foreach (var k in kids) if (k) k.gameObject.SetActive(v);
            if (v)
            {
                shownAt = Time.unscaledTime;
                sel = startItem >= 0 && startItem < items.Length ? startItem : items.Length - 1;
                Refresh();
            }
        }

        void Select(int i)
        {
            if (i < 0 || i >= items.Length || i == sel) return;
            sel = i; MamaAudio.Play("pop", null, 0.35f, 1.4f); Refresh();
        }

        void Refresh() { for (int i = 0; i < items.Length; i++) if (items[i]) items[i].SetSelected(i == sel); }

        void Update()
        {
            if (!visible) return;
            float age = Time.unscaledTime - shownAt;
            if (logo) logo.localScale = logoScale * (1f + Mathf.Max(0f, 0.35f - age) * 1.4f + Mathf.Sin(Time.unscaledTime * 1.7f) * 0.008f);
            if (bubbles != null)
                for (int i = 0; i < bubbles.Length; i++) if (bubbles[i])
                {
                    float t = age - 0.8f - i * bubbleInterval;
                    float s = t < 0f ? 0f : Mathf.Min(1f, t * 6f) * (1f + Mathf.Sin(Mathf.Min(1f, t * 3f) * Mathf.PI) * 0.15f);
                    bubbles[i].localScale = bubbleScale[i] * s;
                }
            foreach (var it in items) if (it && it.action == MamaMenuItem.Action.Play) it.interactable = NetGame.IsAuthority;
            if (status) status.text = StatusLine();

            if (MamaInput.EscDown) { MamaSettings.Toggle(); return; }
            if (MamaFrontEnd.OverlayOpen || age < 0.3f) return;
            var k = Keyboard.current; var g = Gamepad.current;
            int dir = 0;
            if (k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)) dir = -1;
            if (k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame)) dir = 1;
            if (g != null)
            {
                if (g.dpad.left.wasPressedThisFrame || g.dpad.up.wasPressedThisFrame) dir = -1; if (g.dpad.right.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame) dir = 1;
                float x = g.leftStick.ReadValue().x - g.leftStick.ReadValue().y; stickCooldown -= Time.unscaledDeltaTime;
                if (Mathf.Abs(x) > 0.6f && stickCooldown <= 0f) { dir = x > 0 ? 1 : -1; stickCooldown = 0.25f; }
                if (Mathf.Abs(x) < 0.3f) stickCooldown = 0f;
            }
            if (dir != 0) Select(Mathf.Clamp(sel + dir, 0, items.Length - 1));
            bool ok = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
            if (ok && items[sel]) Activate(items[sel]);
        }

        string StatusLine()
        {
            bool th = Loc.Thai;
            if (NetGame.Online)
            {
                string code = NetSession.I ? NetSession.I.JoinCode : "";
                string who = NetGame.IsHost ? (th ? "คุณเป็นเจ้าของห้อง" : "You are the host") : (th ? "รอเจ้าของห้องกดเริ่ม" : "Waiting for the host to start");
                return (th ? "เล่นกับเพื่อน " : "CO-OP ") + NetGame.PlayerCount + "/3" + (string.IsNullOrEmpty(code) ? "" : "  ·  " + code) + "  ·  " + who;
            }
            return th ? "เล่นคนเดียว · 1-3 คน" : "SOLO · 1-3 PLAYERS";
        }

        void Activate(MamaMenuItem it)
        {
            if (!it || !it.interactable) return;
            MamaAudio.Play("click", null, 0.7f);
            var fe = MamaFrontEnd.I;
            switch (it.action)
            {
                case MamaMenuItem.Action.Play: if (NetGame.Online && NetSession.I && !NetGame.IsHost) break; MamaLaunch.ToGame(); break;
                case MamaMenuItem.Action.Coop: MamaLaunch.ToLobby(); break;
                case MamaMenuItem.Action.HowTo: if (MamaHowToPlay.I) MamaHowToPlay.I.Show(false); break;
                case MamaMenuItem.Action.Options: MamaSettings.Toggle(); break;
                case MamaMenuItem.Action.Quit:
                    Debug.Log("[Title] QUIT");
                    Application.Quit();   // ignored in the Editor
                    break;
            }
        }
    }
}
