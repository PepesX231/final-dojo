using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GARAGE: a giant inflatable unicorn on Mom's parking spot
    /// Two kids must squeeze it from BOTH sides at once to let the air out. Squeeze alone and the air rushes to
    /// the head, which balloons up and BOINGS you across the garage.
    public class UnicornHazard : CoopPuzzle
    {
        public Transform body, head;
        public float squeezeTime = 4f;
        float air = 1f, soloT, boingCd, wobble;
        Vector3 bodyScale, headScale;

        protected override void Start()
        {
            questEn = "Squash the air out of the giant pool unicorn - BOTH sit on it at once"; questTh = "ไล่ลมห่วงยางยูนิคอร์นยักษ์ - ขึ้นนั่งทับพร้อมกันทั้งสองคน";
            startEn = "A GIANT pool unicorn from the party is blocking Mom's parking spot! Squash it flat before she parks!"; startTh = "ห่วงยางยูนิคอร์นยักษ์จากปาร์ตี้ขวางที่จอดรถแม่! ช่วยกันนั่งทับให้แฟบก่อนแม่มาจอด!";
            doneEn = "Pfffffft... unicorn flattened!"; doneTh = "ฟู่ววว... ยูนิคอร์นแฟบแล้ว!";
            momEn = "Why is there a UNICORN in my parking spot?!"; momTh = "ทำไมมียูนิคอร์นอยู่ในที่จอดรถแม่?!";
            if (body) bodyScale = body.localScale; if (head) headScale = head.localScale;
            base.Start();
        }

        public override string StationPrompt(CoopStation s, PlayerController p)
        {
            if (OtherManned(s, p)) return T("SQUASH TOGETHER! (hold E)", "นั่งทับพร้อมกัน! (กด E ค้าง)");
            return NetGame.PlayerCount < 2 && !AllowSolo ? T("Too bouncy for one kid - needs a friend on the other side", "เด้งเกินไปสำหรับคนเดียว - ต้องมีเพื่อนอีกฝั่ง") : T("Hold E: sit on the unicorn (a friend must sit on the other side!)", "กด E ค้าง: นั่งทับยูนิคอร์น (ให้เพื่อนนั่งอีกฝั่งพร้อมกัน!)");
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime; boingCd -= dt; wobble += dt;
            if (Urgent && Sim)
            {
                if (AllManned)
                {
                    soloT = 0f;
                    air = Mathf.Max(0f, air - dt / squeezeTime);
                    if (Random.value < dt * 5f) MamaAudio.Play("squeak", transform.position, .5f, Random.Range(.45f, .7f));   // pffft
                    if (air <= 0f) { Fix(who[0]); MamaRound.Award(who[1], this, "Squashed the pool unicorn (team)", 40); }
                }
                else if (Count == 1)
                {
                    soloT += dt;
                    if (soloT > .9f && boingCd <= 0f)
                    {
                        boingCd = 2.5f; soloT = 0f;
                        for (int i = 0; i < who.Length; i++) if (who[i] && Time.time - seen[i] < .2f) Boing(who[i], head ? head.position : transform.position, "BOING!", "ดึ๋ง!", 5.5f);
                        air = Mathf.Min(1f, air + .15f);
                    }
                }
                else soloT = 0f;
            }
            // hidden until it "arrives" (it never appears in a solo game)
            if (body && body.gameObject.activeSelf != Active) { body.gameObject.SetActive(Active); if (head) head.gameObject.SetActive(Active); foreach (var st in stations) if (st) st.gameObject.SetActive(Active && !Fixed); var c = GetComponent<Collider>(); if (c) c.enabled = Active && !Fixed; }
            if (Fixed) { foreach (var st in stations) if (st && st.gameObject.activeSelf) st.gameObject.SetActive(false); var c = GetComponent<Collider>(); if (c && c.enabled) c.enabled = false; }
            // visuals: shrink as the air goes; the head swells while someone squeezes alone
            float k = Fixed ? .12f : Mathf.Lerp(.25f, 1f, air);
            float headPuff = Urgent ? 1f + Mathf.Clamp01(soloT / .9f) * .6f : 1f;
            float wob = 1f + Mathf.Sin(wobble * 7f) * .03f * (AllManned ? 3f : 1f);
            if (body) body.localScale = Vector3.Lerp(body.localScale, new Vector3(bodyScale.x * k * wob, bodyScale.y * k, bodyScale.z * k / wob), dt * 8f);
            if (head) head.localScale = Vector3.Lerp(head.localScale, headScale * Mathf.Max(.2f, k) * headPuff, dt * 10f);
        }

        public override void WriteState(System.IO.BinaryWriter w) { base.WriteState(w); w.Write(air); w.Write(soloT); }
        public override void ReadState(System.IO.BinaryReader r) { base.ReadState(r); air = r.ReadSingle(); soloT = r.ReadSingle(); }
    }
}
