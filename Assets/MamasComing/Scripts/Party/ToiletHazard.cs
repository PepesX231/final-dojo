using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- BATHROOM: clogged toilet overflowing (needs the plunger)
    public class ToiletHazard : Hazard
    {
        public Transform overflowPoint;
        public float rate = 0.4f;
        public GameObject overflowFx;
        public override MiniGame.Kind MiniGameKind(PlayerController p) => HasTool(p) ? MiniGame.Kind.Plunger : MiniGame.Kind.None;
        public override string GetPrompt(PlayerController p) => HasTool(p) ? Quest : T("Need the PLUNGER (garage)", "ต้องใช้ที่ปั๊มส้วม (โรงรถ)");
        public override void Interact(PlayerController p)
        {
            if (!HasTool(p)) { if (GameManager.I) GameManager.I.KidSay(T("I need the plunger!", "ต้องใช้ที่ปั๊ม!")); return; }
            Fix(p);
        }
        protected static bool HasTool(PlayerController p) => p && p.Held is ToolItem t && t.kind == ToolItem.Kind.Plunger;
        protected override void Tick(float dt)
        {
            bool water = GameManager.I == null || !GameManager.I.mainValve || GameManager.I.mainValve.open;
            if (water && FloodSim.I && overflowPoint) FloodSim.I.AddLiters(overflowPoint.position, rate * (Failed ? 2f : 1f) * dt, 0.25f);
        }
        protected override float ProgressMul => GameManager.I && GameManager.I.mainValve && !GameManager.I.mainValve.open ? 0.15f : 1f;
        protected override void VisualTick(float dt, bool running) { if (overflowFx) overflowFx.SetActive(running); }
    }
}
