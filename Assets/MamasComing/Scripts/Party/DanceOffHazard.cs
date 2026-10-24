using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GAME ROOM: the dance game froze on "DANCE OR IT NEVER STOPS"
    /// Two kids stand on the two pads and press E TOGETHER on every beat. Six good beats in a row turn it off.
    /// Mistime it (or stomp alone) and the speakers blast everybody back.
    public class DanceOffHazard : CoopPuzzle
    {
        public float bpm = 70f;
        public float window = .22f;
        public int needed = 6;
        public TextMesh screenText;
        public Light beatLight;
        public Renderer[] padGlow;
        int streak; float[] pressT; float lastJudged = -9f, flash;

        protected override void Start()
        {
            holdMode = false;
            questEn = "The dance game is stuck LOUD - dance on both pads together (E on the beat)"; questTh = "เกมเต้นค้างเสียงดังลั่น - ขึ้นแป้นทั้งสองแล้วกด E พร้อมกันตามจังหวะ";
            startEn = "The dance game glitched: DANCE OR IT NEVER STOPS!"; startTh = "เกมเต้นรวน: เต้นไม่งั้นไม่หยุด!";
            doneEn = "PERFECT combo! The game finally shut up."; doneTh = "คอมโบเพอร์เฟกต์! เกมเงียบแล้ว";
            momEn = "Who's been DANCING on my carpet?!"; momTh = "ใครมาเต้นบนพรมแม่?!";
            base.Start();
            pressT = new float[stations.Length]; for (int i = 0; i < pressT.Length; i++) pressT[i] = -9f;
        }

        float Beat => 60f / bpm;
        float Phase => Time.time % Beat;                       // 0 = on the beat
        float OffBeat => Mathf.Min(Phase, Beat - Phase);       // seconds away from the nearest beat

        public override string StationPrompt(CoopStation s, PlayerController p)
        {
            if (NetGame.PlayerCount < 2 && !AllowSolo) return T("Needs a friend on the other pad", "ต้องมีเพื่อนอยู่อีกแป้น");
            return T("E on the BEAT - together! (" + streak + "/" + needed + ")", "กด E ตามจังหวะ พร้อมกัน! (" + streak + "/" + needed + ")");
        }

        public override void Pressed(CoopStation s, PlayerController p)
        {
            base.Pressed(s, p);
            if (!Urgent || s.index >= pressT.Length) return;
            pressT[s.index] = Time.time;
            MamaAudio.Play("click", s.transform.position, .6f, 1.3f);
        }

        protected override void Update()
        {
            base.Update();
            float dt = Time.deltaTime; flash = Mathf.MoveTowards(flash, 0f, dt * 3f);
            bool on = Urgent;
            if (on && Sim && pressT != null && pressT.Length >= 2)
            {
                // judge once per beat, right after its window closes
                float beatStart = Time.time - Phase;
                if (Phase > window && Phase < window + .1f && lastJudged < beatStart)
                {
                    lastJudged = beatStart;
                    bool a = Mathf.Abs(pressT[0] - beatStart) < window || Mathf.Abs(pressT[0] - (beatStart + Beat)) < window;
                    bool b = Mathf.Abs(pressT[1] - beatStart) < window || Mathf.Abs(pressT[1] - (beatStart + Beat)) < window;
                    bool twoPeople = who[0] && who[1] && who[0] != who[1];
                    if (a && b && twoPeople)
                    {
                        streak++; flash = 1f;
                        NetGame.Pop(transform.position + Vector3.up * 2.2f, streak >= needed ? "PERFECT!" : "NICE x" + streak, streak >= needed ? "เพอร์เฟกต์!" : "เยี่ยม x" + streak, new Color(.5f, 1f, .7f), "good", .7f, 1f + streak * .08f);
                        if (streak >= needed) { Fix(who[0]); MamaRound.Award(who[1], this, "Danced the game quiet (team)", 40); }
                    }
                    else if (a || b)
                    {
                        // only one stomped (or out of sync): the speakers blast everyone on a pad
                        streak = Mathf.Max(0, streak - 2);
                        for (int i = 0; i < who.Length; i++) if (who[i] && Time.time - seen[i] < Beat * 2f) Boing(who[i], transform.position, "OUT OF SYNC!", "ไม่พร้อมกัน!", 4f);
                    }
                    pressT[0] = pressT[1] = -9f;
                }
            }
            // visuals: beat light + pad glow pulse on every beat, a big screen counter
            float pulse = on ? Mathf.Clamp01(1f - Phase / (Beat * .35f)) : 0f;
            if (beatLight) { beatLight.enabled = on; beatLight.intensity = 1f + pulse * 5f + flash * 4f; beatLight.color = Color.Lerp(new Color(1f, .3f, .8f), new Color(.4f, 1f, .6f), flash); }
            if (padGlow != null) foreach (var r in padGlow) if (r) { var c = Color.Lerp(new Color(.25f, .2f, .35f), new Color(1f, .45f, .85f), on ? .3f + pulse * .7f : 0f); r.material.SetColor("_BaseColor", c); r.material.SetColor("_EmissionColor", c * (on ? .6f + pulse * 1.5f : 0f)); }
            if (screenText) screenText.text = Fixed ? "GOOD NIGHT ♪" : on ? (pulse > .5f ? "> STOMP! <" : "DANCE " + streak + "/" + needed) : "";
        }

        public override void WriteState(System.IO.BinaryWriter w) { base.WriteState(w); w.Write((byte)streak); }
        public override void ReadState(System.IO.BinaryReader r) { base.ReadState(r); streak = r.ReadByte(); }
    }
}
