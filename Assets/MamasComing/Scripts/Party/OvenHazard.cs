using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- KITCHEN: party pizza burning in the oven
    public class OvenHazard : Hazard
    {
        public FireSource ovenFire;
        public ParticleSystem ovenSmoke;
        public Light ovenGlow;
        protected override void Tick(float dt) { if (SmokeSim.I) SmokeSim.I.Add(transform.position + Vector3.up * 0.6f, (0.1f + Progress * 0.9f) * dt); }
        protected override void VisualTick(float dt, bool running)
        {
            if (ovenSmoke) { var em = ovenSmoke.emission; em.rateOverTime = running ? 4f + Progress * 26f : 0f; }
            if (ovenGlow) { ovenGlow.enabled = running; ovenGlow.intensity = 0.6f + Progress * 1.6f; }
        }
        protected override void OnFailed() { if (ovenFire && NetGame.IsAuthority) ovenFire.Ignite(0.4f); }
    }
}
