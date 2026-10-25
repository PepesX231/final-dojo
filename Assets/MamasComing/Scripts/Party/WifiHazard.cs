using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- WiFi died: the password is on the router (game room),
    // but it has to be entered on the TV (living room). One kid reads, the other types - shout it to each other!
    /// Spot 1 (router): hold E to read the 4-colour code (only while you hold it).
    /// Spot 2 (TV): the TV flips through colours - tap E when it shows the next colour of the code.
    /// The TV only takes input while a DIFFERENT kid is reading the router.
    public class WifiHazard : CoopPuzzle
    {
        public float step = 1.05f;
        public TextMesh tvScreen;
        int[] code = { 0, 1, 2, 3 }; int entered, cursor, prevCursor; float cursorT, switchedAt;
        static readonly string[] En = { "RED", "BLUE", "GREEN", "YELLOW" }, Th = { "แดง", "ฟ้า", "เขียว", "เหลือง" }, Hex = { "#FF4D5E", "#4DA6FF", "#5BE37A", "#FFD84D" };
        static string Word(int i) => "<color=" + Hex[i] + ">" + (Loc.Thai ? Th[i] : En[i]) + "</color>";

        protected override void Start()
        {
            questEn = "WiFi is down - read the code at the ROUTER (game room), enter it on the TV (living room)";
            questTh = "เน็ตหลุด - อ่านรหัสที่เราเตอร์ (ห้องเกม) แล้วกดใส่ที่ทีวี (ห้องนั่งเล่น)";
            startEn = "The WiFi died! Mom's video call will fail! (needs 2 kids)"; startTh = "เน็ตบ้านหลุด! แม่วิดีโอคอลมาไม่ติดแน่! (ต้องช่วยกัน 2 คน)";
            doneEn = "WiFi is back!"; doneTh = "เน็ตกลับมาแล้ว!";
            momEn = "Why did the WiFi cut out on my call?!"; momTh = "ทำไมเน็ตหลุดตอนแม่โทรมา?!";
            base.Start();
        }

        protected override void OnActivated()
        {
            if (!NetGame.IsAuthority) return;
            for (int i = 0; i < code.Length; i++) code[i] = Random.Range(0, 4);
            if (code[0] == code[1] && code[1] == code[2]) code[2] = (code[2] + 1) % 4;   // keep it readable
            entered = 0;
        }

        public override bool StationHolds(CoopStation s) => s.index == 0;   // read = hold, TV = tap
        bool Reader(PlayerController notThis) => IsManned(0) && who[0] != notThis;
        string CodeText() { string s = ""; for (int i = 0; i < code.Length; i++) s += (i > 0 ? "  " : "") + (i < entered ? "<color=#888888>OK</color>" : Word(code[i])); return s; }

        public override string StationPrompt(CoopStation s, PlayerController p)
        {
            if (s.index == 0) return T("Hold E: read the WiFi code - tell your friend at the TV!", "กด E ค้าง อ่านรหัสเน็ต - บอกเพื่อนที่อยู่หน้าทีวี!");
            return Reader(p) ? T("Tap E when the TV shows the next colour! (" + entered + "/4)", "กด E ตอนทีวีขึ้นสีถัดไปของรหัส! (" + entered + "/4)")
                             : T("A friend must read the code at the router (game room)", "ต้องให้เพื่อนไปอ่านรหัสที่เราเตอร์ (ห้องเกม)");
        }

        public override string StationLabel(CoopStation s, out Color c)
        {
            if (s.index == 0)
            {
                bool on = IsManned(0);
                c = on ? Color.white : new Color(1f, .5f, .8f);
                return on ? T("WIFI CODE:\n", "รหัสเน็ต:\n") + CodeText() : T("ROUTER\n(hold E to read)", "เราเตอร์\n(กด E ค้างเพื่ออ่าน)");
            }
            c = Color.white;
            string now = T("TV: ", "ทีวี: ") + Word(cursor) + "   " + entered + "/4";
            return IsManned(0) ? now + T("\n(tap E on the right colour)", "\n(กด E ตอนสีถูก)") : T("TV\nwait for the router reader", "ทีวี\nรอเพื่อนอ่านรหัสที่เราเตอร์");
        }

        public override void Pressed(CoopStation s, PlayerController p)
        {
            base.Pressed(s, p);
            if (s.index != 1 || !Urgent || !Sim) return;
            Vector3 at = s.transform.position + Vector3.up * 1.7f;
            if (!Reader(p)) { NetGame.Pop(at, "Nobody is reading the router!", "ยังไม่มีใครอ่านรหัสที่เราเตอร์!", new Color(1f, .6f, .85f), "bad", .6f, 1f); return; }
            int want = code[entered];
            // friends see the TV a moment late: the colour that just went by still counts
            bool ok = cursor == want || (prevCursor == want && Time.time - switchedAt < .3f);
            if (ok)
            {
                entered++;
                NetGame.Pop(at, En[want] + "! " + entered + "/4", Th[want] + "! " + entered + "/4", new Color(.6f, 1f, .7f), "good", .6f, 1f + entered * .08f);
                if (entered >= code.Length) { var reader = who[0]; Fix(p); MamaRound.Award(reader, this, "Read the WiFi code (team)", 40); }
            }
            else
            {
                entered = 0;
                NetGame.Pop(at, "WRONG CODE! Start again", "รหัสผิด! เริ่มใหม่", new Color(1f, .5f, .5f), "bad", .7f, .9f);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (Urgent && Sim)
            {
                cursorT += Time.deltaTime;
                if (cursorT >= step) { cursorT = 0f; prevCursor = cursor; cursor = (cursor + 1) % 4; switchedAt = Time.time; }
            }
            if (tvScreen) tvScreen.text = Fixed ? "WiFi OK" : Urgent ? (Loc.Thai ? Th[cursor] : En[cursor]) : "";
            if (tvScreen) { Color col; ColorUtility.TryParseHtmlString(Hex[cursor], out col); tvScreen.color = Fixed ? new Color(.6f, 1f, .7f) : col; }
        }

        public override void WriteState(System.IO.BinaryWriter w)
        {
            base.WriteState(w); w.Write((byte)entered); w.Write((byte)cursor);
            for (int i = 0; i < 4; i++) w.Write((byte)code[i]);
        }
        public override void ReadState(System.IO.BinaryReader r)
        {
            base.ReadState(r); entered = r.ReadByte(); int c = r.ReadByte();
            if (c != cursor) { prevCursor = cursor; cursor = c; switchedAt = Time.time; }
            for (int i = 0; i < 4; i++) code[i] = r.ReadByte() % 4;
        }
    }
}
