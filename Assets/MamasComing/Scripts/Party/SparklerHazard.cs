using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- DINING: sparklers left burning in the cake
    public class SparklerHazard : Hazard
    {
        public ParticleSystem sparks;
        public FireSource clothFire;
        protected override void VisualTick(float dt, bool running)
        {
            if (sparks) { var em = sparks.emission; em.rateOverTime = running ? 40f + Progress * 60f : 0f; }
        }
        protected override void OnFailed() { if (clothFire && NetGame.IsAuthority) clothFire.Ignite(0.35f); }
    }
}
