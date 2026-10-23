using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- LAUNDRY: soap foam explosion from the washer
    public class WasherHazard : Hazard
    {
        public Transform foamOrigin;
        public Material foamMat;
        public Transform drum;
        public Transform socket;
        float spawnT;
        protected override void Tick(float dt)
        {
            spawnT -= dt;
            if (spawnT <= 0f && foamOrigin)
            {
                spawnT = Failed ? 1.0f : 2.2f;
                if (SplatMess.Count(SplatKind.Foam) < 18)
                {
                    Vector2 r = Random.insideUnitCircle * (0.6f + Progress * 1.6f);
                    var s = SplatMess.Spawn(SplatKind.Foam, foamOrigin.position + new Vector3(r.x, 0, Mathf.Abs(r.y) + 0.2f), 0.7f + Progress * 0.5f, foamMat);
                    s.transform.localScale = new Vector3(s.transform.localScale.x, 0.05f, s.transform.localScale.z);
                }
            }
            if (FloodSim.I && foamOrigin) FloodSim.I.AddLiters(foamOrigin.position + Vector3.forward * 0.5f, 0.12f * dt, 0.3f);
        }
        protected override void VisualTick(float dt, bool running) { if (drum && running) drum.Rotate(0f, 0f, 400f * dt, Space.Self); }
        protected override void OnFailed()
        {
            if (NetGame.IsAuthority) PowerGrid.Trip("Soapy water got into the laundry socket! *BZZT*", "น้ำสบู่เข้าปลั๊กห้องซักผ้า! *ซี่ดด*");
        }
    }
}
