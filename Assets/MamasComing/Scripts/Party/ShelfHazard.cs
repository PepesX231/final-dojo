using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GAME ROOM: the bookshelf tipped over (co-op)
    /// One kid HOLDS the shelf up against the wall (hold E on spot 1), the other SCREWS it to the wall
    /// (tap E on spot 2). Screws only bite while somebody else is holding the shelf.
    public class ShelfHazard : CoopPuzzle
    {
        public Transform shelfPivot;          // bottom edge of the shelf; tilts forward when fallen
        public float fallenAngle = 62f, heldAngle = 6f;
        public int needed = 8;
        int screws; float tilt; float wobble;

        protected override void Start()
        {
            questEn = "The bookshelf fell over - one holds it up, one screws it to the wall"; questTh = "ชั้นหนังสือล้ม - คนนึงยันชั้นไว้ อีกคนขันน็อตยึดผนัง";
            startEn = "CRASH! The bookshelf in the GAME ROOM fell over! (needs 2 kids)"; startTh = "โครม! ชั้นหนังสือในห้องเกมล้ม! (ต้องช่วยกัน 2 คน)";
            doneEn = "Shelf screwed back to the wall!"; doneTh = "ยึดชั้นหนังสือกลับเข้าผนังแล้ว!";
            momEn = "Why are my books all over the floor?!"; momTh = "ทำไมหนังสือแม่กระจายเต็มพื้น?!";
            base.Start();
            if (shelfPivot) tilt = 0f;
        }

        public override bool StationHolds(CoopStation s) => s.index == 0;   // spot 1 holds, spot 2 taps

        bool Supported(PlayerController notThis) => IsManned(0) && who[0] != notThis;

        public override string StationPrompt(CoopStation s, PlayerController p)
        {
            if (s.index == 0) return T("Hold E: hold the shelf up (a friend screws it in)", "กด E ค้าง ยันชั้นไว้ (ให้เพื่อนขันน็อต)");
            return Supported(p) ? T("Tap E: screw it in! (" + screws + "/" + needed + ")", "กด E รัวๆ ขันน็อต! (" + screws + "/" + needed + ")")
                                : T("A friend must hold the shelf up first (spot 1)", "ต้องให้เพื่อนยันชั้นไว้ก่อน (จุดที่ 1)");
        }

        public override string StationLabel(CoopStation s, out Color c)
        {
            if (s.index == 0)
            {
                bool on = IsManned(0);
                c = on ? new Color(.45f, 1f, .55f) : new Color(1f, .5f, .8f);
                return on ? T("OK! HOLDING", "OK! ยันอยู่") : T("HOLD SHELF\n(hold E)", "ยันชั้นตรงนี้\n(กด E ค้าง)");
            }
            bool ready = IsManned(0);
            c = ready ? new Color(1f, .9f, .4f) : new Color(1f, .5f, .8f);
            return ready ? T("SCREW IT! " + screws + "/" + needed + "\n(tap E)", "ขันน็อต! " + screws + "/" + needed + "\n(กด E รัวๆ)")
                         : T("SCREWS " + screws + "/" + needed + "\nwait for a holder", "ขันน็อต " + screws + "/" + needed + "\nรอเพื่อนยันชั้น");
        }

        public override void Pressed(CoopStation s, PlayerController p)
        {
            base.Pressed(s, p);
            if (s.index != 1 || !Urgent || !Sim) return;
            if (!Supported(p))
            {
                wobble = 1f;
                NetGame.Pop(s.transform.position + Vector3.up * 1.6f, "Someone hold it up!", "ต้องมีคนยันชั้นก่อน!", new Color(1f, .6f, .85f), "bad", .6f, 1f);
                return;
            }
            screws++;
            NetGame.Pop(s.transform.position + Vector3.up * 1.6f, "SCREW " + screws + "/" + needed, "ขัน " + screws + "/" + needed, new Color(.6f, 1f, .7f), "click", .7f, 1f + screws * .05f);
            if (screws >= needed) { var holder = who[0]; Fix(p); MamaRound.Award(holder, this, "Held the bookshelf up (team)", 40); }
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime; wobble = Mathf.MoveTowards(wobble, 0f, dt * 2f);
            float target = Fixed ? 0f : Active ? (IsManned(0) ? heldAngle * (1f - screws / (float)Mathf.Max(1, needed)) : fallenAngle) : 0f;
            tilt = Mathf.MoveTowards(tilt, target, dt * 90f);
            if (shelfPivot) shelfPivot.localRotation = Quaternion.Euler(0f, 0f, -(tilt + Mathf.Sin(Time.time * 40f) * wobble * 3f));
        }

        protected override void OnFixed(PlayerController p) { MamaAudio.Play("good", transform.position, .8f); }
        public override void WriteState(System.IO.BinaryWriter w) { base.WriteState(w); w.Write((byte)screws); }
        public override void ReadState(System.IO.BinaryReader r) { base.ReadState(r); screws = r.ReadByte(); }
    }
}
