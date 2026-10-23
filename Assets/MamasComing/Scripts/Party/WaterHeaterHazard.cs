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
            if (valveA && valveB && valveA.TurnedRecently(window) && valveB.TurnedRecently(window) && Mathf.Abs(valveA.turnedAt - valveB.turnedAt) <= window)
                Fix(valveA.lastBy ?? valveB.lastBy);
        }
        protected override void VisualTick(float dt, bool running)
        {
            if (needle) needle.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(120f, -120f, Fixed ? 0.2f : Progress) + (running ? Mathf.Sin(Time.time * 30f) * Progress * 4f : 0f));
            if (steam) { var em = steam.emission; em.rateOverTime = (running && Progress > 0.6f) || burstLeft > 0 ? 20f + Progress * 40f : 0f; }
        }
        protected override void OnFailed() { burstLeft = 12f; MamaAudio.Play("slam", transform.position, 1f, 0.5f); MamaAudio.Play("steam", transform.position, 1f); if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.8f); }
    }
}
