using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Mop bucket. Fill it from a tap or scoop out the tub. Tip it over and it floods the floor.
    /// Throw it (RMB) to splash water forward: puts out ordinary fires... and makes grease fires EXPLODE.
    /// </summary>
    public class Bucket : MessyProp
    {
        public float capacity = 10f;
        public float liters = 0f;
        public Transform waterVisual;
        public float waterMinY = 0.04f, waterMaxY = 0.5f;

        void Update()
        {
            if (waterVisual)
            {
                float k = liters / capacity;
                waterVisual.gameObject.SetActive(liters > 0.2f);
                var lp = waterVisual.localPosition; lp.y = Mathf.Lerp(waterMinY, waterMaxY, k); waterVisual.localPosition = lp;
            }
            bool tipped = Vector3.Dot(transform.up, Vector3.up) < 0.35f;
            if (tipped && !IsHeld && liters > 0.2f && GameManager.I && GameManager.I.SimRunning) Spill(1f);
        }

        public void Spill(float frac)
        {
            float l = liters * frac;
            if (l <= 0.1f) return;
            liters -= l;
            MamaAudio.Play("splash", transform.position, 1f, 0.8f);
            if (FloodSim.I) FloodSim.I.AddLiters(transform.position + transform.up * 0.3f, l, 0.5f);
            if (GameManager.I) GameManager.I.Toast("SPLOOSH! " + Mathf.RoundToInt(l) + " liters all over the floor!");
        }

        public void OnThrown(PlayerController p)
        {
            if (liters < 0.5f) return;
            Vector3 at = p.transform.position + p.transform.forward * 1.4f;
            FireSource hit = null;
            foreach (var f in FireSource.All)
            {
                if (!f || !f.burning) continue;
                Vector3 d = f.transform.position - at; d.y = 0f;
                if (d.magnitude < 1.4f) { hit = f; break; }
            }
            if (hit) hit.Douse(liters, p);
            if (FloodSim.I) FloodSim.I.AddLiters(at, liters * 0.85f, 0.7f);
            liters = 0f;
        }

        public override float SpeedMultiplier => Mathf.Lerp(0.95f, 0.7f, liters / capacity);

        public override string GetPrompt(PlayerController p)
        {
            if (p.Held is Mop m && m.load > 0.05f) return "Wring the mop into the bucket";
            return base.GetPrompt(p) + (liters > 0.2f ? " (" + Mathf.CeilToInt(liters) + " L)" : "");
        }

        public override void Interact(PlayerController p)
        {
            if (p.Held is Mop m && m.load > 0.05f)
            {
                float room = capacity - liters;
                float put = Mathf.Min(room, m.load);
                liters += put; m.load -= put;
                if (GameManager.I) GameManager.I.Toast(room <= put ? "Bucket is FULL - go empty it in the toilet!" : "Squeeeze! Mop wrung into the bucket.");
                return;
            }
            base.Interact(p);
        }

        public override bool IsProblem => base.IsProblem && liters > 0.5f;
    }
}
