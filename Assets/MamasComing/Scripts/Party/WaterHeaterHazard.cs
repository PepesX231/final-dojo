using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GARAGE: water heater pressure (two valves at once = co-op)
    public class WaterHeaterHazard : Hazard
    {
        public HeaterValve valveA, valveB;
        public Transform needle;
        public ParticleSystem steam;
        public SinkPipeHazard pipe;
        public Transform burstPoint;
        public float window = 3f;
        bool pipeBurst;
        float burstLeft;
        bool WaterOn => GameManager.I == null || !GameManager.I.mainValve || GameManager.I.mainValve.open;
        protected override float ProgressMul => WaterOn ? 1f : 0.05f;
        public override bool CanInteract(PlayerController p) => false;  // use the two valves
        protected override void Tick(float dt)
        {
            if (!pipeBurst && pipe && Progress >= 0.5f) { pipeBurst = true; pipe.Activate("BANG! The pressure burst the pipe under the KITCHEN SINK!", "ปัง! แรงดันน้ำทำท่อใต้ซิงก์ครัวแตก!"); }
            if (burstLeft > 0f && FloodSim.I && burstPoint) { burstLeft -= dt; FloodSim.I.AddLiters(burstPoint.position, 2.2f * dt, 0.4f); }
            // two DIFFERENT kids must hold both valves at the same time until the pressure is down
            bool two = valveA && valveB && valveA.Held && valveB.Held && valveA.heldBy != valveB.heldBy;
            if (two)
            {
                turn += dt / Mathf.Max(.5f, turnTime);
                if (turn >= 1f) { var a = valveA.heldBy; var b = valveB.heldBy; Fix(a); MamaRound.Award(b, this, "Turned the heater valves (team)", 40); }
            }
            else turn = Mathf.Max(0f, turn - dt * .4f);
        }
        [Tooltip("Seconds both valves must be held together.")]
        public float turnTime = 2.2f;
        float turn;
        public float Turn => turn;
        public bool Turning => valveA && valveB && valveA.Held && valveB.Held && valveA.heldBy != valveB.heldBy;
        public override void WriteState(System.IO.BinaryWriter w) { base.WriteState(w); w.Write((byte)Mathf.RoundToInt(Mathf.Clamp01(turn) * 255)); }
        public override void ReadState(System.IO.BinaryReader r) { base.ReadState(r); turn = r.ReadByte() / 255f; }
        protected override void VisualTick(float dt, bool running)
        {
            if (needle) needle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(120f, -120f, Fixed ? 0.2f : Progress) + (running ? Mathf.Sin(Time.time * 30f) * Progress * 4f : 0f));
            if (steam) { var em = steam.emission; em.rateOverTime = (running && Progress > 0.6f) || burstLeft > 0 ? 20f + Progress * 40f : 0f; }
        }
        protected override void OnFailed() { burstLeft = 12f; MamaAudio.Play("slam", transform.position, 1f, 0.5f); MamaAudio.Play("steam", transform.position, 1f); if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.8f); }
    }
}
