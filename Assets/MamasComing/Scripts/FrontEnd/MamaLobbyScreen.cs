using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MamasComing
{
    /// Co-op lobby: the kids hang out on the sofa (one seat per player), name tags over their heads,
    /// an icon rail on the left, room info top-right, fruit picker on the right and START / LEAVE bottom-right.
    /// All of it lives under FrontEnd/LobbyRig in the scene.
    public class MamaLobbyScreen : MonoBehaviour
    {
        public MamaShot shot;
        public Canvas canvas;
        [Tooltip("Seat 0 = P1 (host). Move them on the sofa in the Scene view.")]
        public MamaPosedKid[] seats;
        [Tooltip("Shown on empty seats instead of a kid (optional).")]
        public GameObject[] emptyMarkers;
        public Text[] tagNames, tagFruits;
        public MamaMenuItem[] buttons;
        public Text roomCode, playerCount, fruitName, fruitHint, statusText, startLabel;
        public InputField codeField;
        public Image[] countPips;
        [Tooltip("Pose for each fruit when it sits in a seat (0 Apple, 1 Banana, 2 Orange, 3 Watermelon, 4 Wax apple).")]
        public MamaPosedKid.Pose[] seatPoseByFruit = { MamaPosedKid.Pose.Wave, MamaPosedKid.Pose.SitHandsBehindHead, MamaPosedKid.Pose.SitBounce, MamaPosedKid.Pose.SitSlouch, MamaPosedKid.Pose.Sit };
        public Button copyButton;
        public Text copyLabel;
        float copiedT;

        bool visible; int shownSlots = -1; public int ShownSlots => shownSlots;

        void Awake()
        {
            if (copyButton) copyButton.onClick.AddListener(CopyCode);
            foreach (var b in buttons) if (b) { b.Hovered = h => Hover(h); b.Clicked = c => Activate(c); }
        }

        void Hover(MamaMenuItem h) { foreach (var b in buttons) if (b) b.SetSelected(b == h); }

        public void SetVisible(bool v)
        {
            visible = v;
            if (canvas) canvas.gameObject.SetActive(v);
            if (seats != null) foreach (var s in seats) if (s) s.gameObject.SetActive(v);
            if (emptyMarkers != null) foreach (var e in emptyMarkers) if (e) e.SetActive(false);
            if (v) { NetSession.Ensure(); shownSlots = -1; foreach (var seat in seats) if (seat) seat.builtKid = -1; Sync(); }
        }

        void Update()
        {
            if (!visible) return;
            Sync();
            if (MamaInput.EscDown) { if (MamaSettings.Open) MamaSettings.Toggle(); else if (!MamaHowToPlay.Open) Back(); return; }
            if (MamaFrontEnd.OverlayOpen) return;
            var k = Keyboard.current; var g = Gamepad.current;
            bool typing = codeField && codeField.isFocused;
            if (!typing && k != null)
            {
                if (k.qKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) Cycle(-1);
                if (k.eKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) Cycle(1);

            }
            if (g != null)
            {
                if (g.leftShoulder.wasPressedThisFrame) Cycle(-1);
                if (g.rightShoulder.wasPressedThisFrame) Cycle(1);
                if (g.startButton.wasPressedThisFrame && MamaFrontEnd.I) MamaFrontEnd.I.StartGame();
                if (g.buttonEast.wasPressedThisFrame) Back();
            }
        }

        /// Who sits where: P1 is the host; offline only the local kid sits down.
        void Sync()
        {
            bool th = Loc.Thai;
            int count = NetGame.PlayerCount;
            for (int i = 0; i < (seats != null ? seats.Length : 0); i++)
            {
                var seat = seats[i]; if (!seat) continue;
                int kid = -1; bool me = false;
                if (NetSession.I && NetGame.Online) { if (NetSession.I.SlotTaken(i)) { me = i == NetSession.I.MySlot; kid = me ? KidLook.Selected : NetSession.I.KidOfSlot(i, i % KidLook.Count); } }
                else if (i == 0) { kid = KidLook.Selected; me = true; }
                bool occupied = kid >= 0;
                if (occupied && (seat.builtKid != kid || !seat.Model))
                {
                    seat.kid = kid; seat.fitToSeat = true; seat.normalizeHeight = 0.95f;
                    if (seatPoseByFruit != null && kid < seatPoseByFruit.Length) seat.pose = seatPoseByFruit[kid];
                    seat.Rebuild();
                }
                if (seat.Model) seat.Model.gameObject.SetActive(occupied);
                if (emptyMarkers != null && i < emptyMarkers.Length && emptyMarkers[i]) emptyMarkers[i].SetActive(!occupied);
                if (tagNames != null && i < tagNames.Length && tagNames[i])
                    tagNames[i].text = occupied ? "P" + (i + 1) + (me ? (th ? " · คุณ" : " · YOU") : "") : (th ? "ที่ว่าง" : "OPEN SEAT");
                if (tagFruits != null && i < tagFruits.Length && tagFruits[i])
                    tagFruits[i].text = occupied ? (th ? KidLook.NamesTh[kid] : Pretty(KidLook.Names[kid])) : (th ? "ชวนเพื่อนด้วยรหัสห้อง" : "invite a friend");
            }
            if (countPips != null) for (int i = 0; i < countPips.Length; i++) if (countPips[i]) countPips[i].color = i < count ? MamaTheme.Pink : new Color(1, 1, 1, 0.25f);
            if (playerCount) playerCount.text = count + "/3";
            if (roomCode)
            {
                string code = NetSession.I ? NetSession.I.JoinCode : "";
                roomCode.text = !NetGame.Online ? (th ? "ยังไม่มีห้อง" : "NO ROOM YET") : string.IsNullOrEmpty(code) ? (th ? "LAN · พอร์ต 7777" : "LAN · PORT 7777") : code;
            }
            string codeNow = NetSession.I ? NetSession.I.JoinCode : "";
            if (copyButton) copyButton.gameObject.SetActive(NetGame.Online && !string.IsNullOrEmpty(codeNow));
            if (copiedT > 0f) copiedT -= Time.unscaledDeltaTime;
            if (copyLabel) copyLabel.text = copiedT > 0f ? (th ? "คัดลอกแล้ว!" : "COPIED!") : (th ? "คัดลอก" : "COPY");
            int sel = KidLook.Selected;
            if (fruitName) fruitName.text = th ? KidLook.NamesTh[sel] : Pretty(KidLook.Names[sel]);
            if (fruitHint) fruitHint.text = th ? "Q / E เปลี่ยนผลไม้" : "Q / E  change fruit";
            if (statusText)
            {
                string st = NetSession.I ? NetSession.I.Status : "";
                if (string.IsNullOrEmpty(st)) st = th ? "สร้างห้องและชวนเพื่อน ต้องมีอย่างน้อย 2 คน" : "Host a room and invite a friend. At least 2 players required.";
                statusText.text = NetGame.IsClientOnly ? (th ? "รอเจ้าของห้องกดเริ่ม..." : "Waiting for the host to start...") : st;
            }
            if (startLabel) { startLabel.resizeTextForBestFit = true; startLabel.resizeTextMinSize = 22; startLabel.resizeTextMaxSize = 64; }
            if (startLabel) startLabel.text = count < 2 ? (th ? "รอเพื่อน 2 คนขึ้นไป" : "NEED 2 PLAYERS") : NetGame.IsAuthority ? (th ? "เริ่มเลย!" : "START!") : (th ? "รอเจ้าของห้อง" : "WAITING");
            foreach (var b in buttons) if (b)
            {
                if (b.action == MamaMenuItem.Action.Start) b.interactable = MamaLaunch.CanStart;
                if (b.action == MamaMenuItem.Action.Leave) b.interactable = NetGame.Online;
                if (b.action == MamaMenuItem.Action.HostOnline || b.action == MamaMenuItem.Action.HostLan || b.action == MamaMenuItem.Action.Join) b.interactable = !NetGame.Online;
            }
        }

        /// Copies the room code so it can be pasted to friends (Discord, LINE...).
        public void CopyCode()
        {
            string code = NetSession.I ? NetSession.I.JoinCode : "";
            if (string.IsNullOrEmpty(code)) return;
            GUIUtility.systemCopyBuffer = code; copiedT = 1.6f;
            MamaAudio.Play("good", null, 0.6f, 1.3f);
        }

        static string Pretty(string n) => n == "WaxApple" ? "WAX APPLE" : n.ToUpperInvariant();

        void Cycle(int d)
        {
            int id = (KidLook.Selected + d + KidLook.Count) % KidLook.Count;
            KidLook.Selected = id;
            MamaAudio.Play("pop", null, 0.6f, 1.2f);
            int slot = NetGame.Online && NetSession.I ? NetSession.I.MySlot : 0;
            if (seats != null && slot < seats.Length && seats[slot]) seats[slot].builtKid = -1;
            if (NetSession.I) NetSession.I.PublishPick();
        }

        void Back() => MamaLaunch.ToMenu();

        void Activate(MamaMenuItem b)
        {
            if (!b || !b.interactable) return;
            MamaAudio.Play("click", null, 0.7f);
            switch (b.action)
            {
                case MamaMenuItem.Action.Back: Back(); break;
                case MamaMenuItem.Action.Start: if (MamaFrontEnd.I) MamaFrontEnd.I.StartGame(); break;
                case MamaMenuItem.Action.HowTo: if (MamaHowToPlay.I) MamaHowToPlay.I.Show(false); break;
                case MamaMenuItem.Action.Options: MamaSettings.Toggle(); break;
                case MamaMenuItem.Action.PrevKid: Cycle(-1); break;
                case MamaMenuItem.Action.NextKid: Cycle(1); break;
                case MamaMenuItem.Action.HostOnline: NetSession.Ensure(); NetSession.I.HostOnline(); break;
                case MamaMenuItem.Action.HostLan: NetSession.Ensure(); NetSession.I.HostLan(); break;
                case MamaMenuItem.Action.Join: NetSession.Ensure(); NetSession.I.Join(codeField ? codeField.text : ""); break;
                case MamaMenuItem.Action.Leave: if (NetSession.I) NetSession.I.Leave(); break;
            }
        }
    }
}
