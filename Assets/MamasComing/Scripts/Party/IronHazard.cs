using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- BEDROOM: iron left on Mom's dress
    public class IronHazard : Hazard
    {
        public FireSource boardFire;
        public ParticleSystem steam;
        public Renderer dress;
        public Transform ironLight;
        MaterialPropertyBlock mpb;

        protected override void Tick(float dt)
        {
            if (SmokeSim.I) SmokeSim.I.Add(transform.position + Vector3.up * 0.9f, 0.06f * Progress * dt * 10f);
        }
        protected override void VisualTick(float dt, bool running)
        {
            if (steam) { var em = steam.emission; em.rateOverTime = running ? 6f + Progress * 30f : 0f; }
            if (ironLight) ironLight.gameObject.SetActive(running);
            if (dress)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                float b = Failed ? 1f : (Urgent ? Progress * 0.6f : 0f);
                mpb.SetColor("_BaseColor", Color.Lerp(new Color(0.75f, 0.45f, 0.95f), new Color(0.15f, 0.1f, 0.08f), b));
                dress.SetPropertyBlock(mpb);
            }
        }
        protected override void OnFailed() { if (boardFire && NetGame.IsAuthority) boardFire.Ignite(0.3f); }
    }
}
