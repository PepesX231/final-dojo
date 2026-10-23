using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Sink / toilet / bathtub: somewhere to wring the mop, empty the bucket, wet a towel or fill a bucket.
    /// </summary>
    public class WaterDump : Interactable
    {
        public static readonly List<WaterDump> AllDumps = new List<WaterDump>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDumps() { AllDumps.Clear(); }

        [Tooltip("Can fill a bucket / wet a towel here (needs the main valve open).")]
        public bool hasTap;
        public MainValve mainValve;
        [Tooltip("Optional: scooping takes water from this tub instead of a tap.")]
        public WaterTap tub;
        public ParticleSystem splashFx;
        public float accepted;

        public bool HasWater => (hasTap && (!mainValve || mainValve.open)) || (tub && tub.tubLevel > 5f);

        protected override void OnEnable() { base.OnEnable(); if (!AllDumps.Contains(this)) AllDumps.Add(this); }
        protected override void OnDisable() { base.OnDisable(); AllDumps.Remove(this); }

        public static WaterDump Nearest(Vector3 p, float r)
        {
            WaterDump best = null; float bd = r;
            foreach (var d in AllDumps)
            {
                if (!d) continue;
                Vector3 v = d.InteractPoint - p; v.y = 0f;
                float m = v.magnitude;
                if (m < bd) { bd = m; best = d; }
            }
            return best;
        }

        public void Accept(float liters) { accepted += liters; }

        public void Splash(float dt)
        {
            if (splashFx && Random.value < dt * 12f) splashFx.Emit(2);
        }

        public override bool CanInteract(PlayerController p)
        {
            var h = p.Held;
            if (h is Mop m) return m.load > 0.05f;
            if (h is Bucket b) return b.liters > 0.2f || HasWater;
            if (h is Towel t) return t.Soaked > 0.1f || HasWater;
            return false;
        }

        public override string GetPrompt(PlayerController p)
        {
            var h = p.Held;
            if (h is Mop) return "Wring out the mop here";
            if (h is Bucket b)
            {
                if (b.liters > 0.2f) return "Empty the bucket (" + Mathf.CeilToInt(b.liters) + " L) here";
                return tub ? "Hold [E]  Scoop water OUT of the tub" : "Hold [E]  Fill the bucket";
            }
            if (h is Towel t) return t.Soaked > 0.1f ? "Wring out the towel (hold LMB)" : "Soak the towel (for smothering fires)";
            return displayName;
        }

        public override float HoldTime(PlayerController p) => p.Held is Bucket b && b.liters <= 0.2f ? 1.6f : 0f;

        public override void Interact(PlayerController p)
        {
            var h = p.Held;
            if (h is Mop m) { Accept(m.load); m.Wring(); Toast("Squeeeeze. Mop wrung out."); return; }
            if (h is Bucket b)
            {
                if (b.liters > 0.2f) { Accept(b.liters); b.liters = 0f; Toast("Glug glug glug... bucket emptied."); if (splashFx) splashFx.Emit(20); return; }
                float take = b.capacity;
                if (tub) { take = Mathf.Min(take, tub.tubLevel); tub.tubLevel -= take; Toast("Scooped " + Mathf.RoundToInt(take) + " L out of the tub!"); }
                else if (!HasWater) { Toast("Nothing comes out... the main valve is off."); return; }
                b.liters = take;
                return;
            }
            if (h is Towel t && t.Soaked <= 0.1f)
            {
                if (!HasWater) { Toast("No water!"); return; }
                var c = t.GetComponent<CoverObject>(); if (c) c.soaked = c.capacityLiters;
                Toast("Towel soaked through. Good for smothering flames!");
            }
        }

        void Toast(string s) { if (GameManager.I) GameManager.I.Toast(s); }
    }
}
