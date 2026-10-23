using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- KITCHEN: burst pipe under the sink (needs the wrench)
    public class SinkPipeHazard : Hazard
    {
        public Transform leakPoint;
        public ParticleSystem spray;
        public float rate = 1.0f;
        bool WaterOn => GameManager.I == null || !GameManager.I.mainValve || GameManager.I.mainValve.open;
        public override MiniGame.Kind MiniGameKind(PlayerController p) => HasWrench(p) ? MiniGame.Kind.Bolts : MiniGame.Kind.None;
        public override string GetPrompt(PlayerController p) => HasWrench(p) ? Quest : T("Need a WRENCH (garage toolbox)", "ต้องใช้ประแจ (กล่องเครื่องมือโรงรถ)");
        public override void Interact(PlayerController p)
        {
            if (!HasWrench(p)) { if (GameManager.I) GameManager.I.KidSay(T("I need a wrench!", "ต้องใช้ประแจ!")); return; }
            Fix(p);
        }
        static bool HasWrench(PlayerController p) => p && p.Held is ToolItem t && t.kind == ToolItem.Kind.Wrench;
        protected override float ProgressMul => WaterOn ? 1f : 0.1f;
        protected override void Tick(float dt) { if (WaterOn && FloodSim.I && leakPoint) FloodSim.I.AddLiters(leakPoint.position, rate * (Failed ? 2f : 1f) * dt, 0.35f); }
        protected override void VisualTick(float dt, bool running) { if (spray) { var em = spray.emission; em.rateOverTime = running && WaterOn ? 140f : 0f; } }
        protected override void OnFailed() { if (NetGame.IsAuthority) PowerGrid.Trip("Water reached the kitchen outlets! *BZZT*", "น้ำไหลเข้าปลั๊กในครัว! *ซี่ดด*"); }
    }
}
