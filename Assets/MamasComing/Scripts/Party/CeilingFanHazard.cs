using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- LIVING ROOM: cake stuck on the spinning ceiling fan
    public class CeilingFanHazard : Hazard
    {
        public Transform blades;
        public Transform cakeBlob;
        public Material frostingMat;
        public Rect area = new Rect(1f, 0.5f, 9f, 5f);
        public Transform tvSplat;
        float spin, splatT;
        protected override void Tick(float dt)
        {
            splatT -= dt;
            if (splatT <= 0f)
            {
                splatT = 3.2f;
                if (SplatMess.Count(SplatKind.Frosting) < 12)
                {
                    var p = new Vector3(Random.Range(area.xMin, area.xMax), 0, Random.Range(area.yMin, area.yMax));
                    SplatMess.Spawn(SplatKind.Frosting, p, Random.Range(0.25f, 0.45f), frostingMat);
                    MamaAudio.Play("slap", p, 0.45f, 1.2f);
                }
            }
        }
        protected override void VisualTick(float dt, bool running)
        {
            spin = Mathf.MoveTowards(spin, running ? 540f : 0f, dt * 300f);
            if (blades) blades.Rotate(0f, spin * dt, 0f, Space.Self);
            if (cakeBlob) cakeBlob.gameObject.SetActive(!Failed);
        }
        protected override void OnFailed() { if (tvSplat) tvSplat.gameObject.SetActive(true); MamaAudio.Play("slap", transform.position, 1f, 0.8f); }
        public override bool IsProblem => Active && (!Fixed || Failed);
    }
}
