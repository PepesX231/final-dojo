using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Something lying on the floor that hides water underneath (rug, towel, bath mat, chair)
    /// and optionally soaks it up until saturated (then it drips and gets heavy).
    /// </summary>
    public class CoverObject : MonoBehaviour
    {
        public static readonly List<CoverObject> All = new List<CoverObject>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        [Tooltip("Half size of the covered footprint, local X / Z.")]
        public Vector2 halfSize = new Vector2(0.5f, 0.5f);
        [Tooltip("Liters per second this soaks up (0 = only hides).")]
        public float absorbRate;
        public float capacityLiters = 4f;
        public float soaked;
        public bool hidesWater = true;
        public Renderer[] tintRenderers;

        Pickup pickup;
        Color[] baseColors;
        MaterialPropertyBlock mpb;

        public bool Saturated => absorbRate > 0f && soaked >= capacityLiters;
        public float Wetness01 => capacityLiters > 0f ? Mathf.Clamp01(soaked / capacityLiters) : 0f;
        public bool OnFloor => (pickup == null || (!pickup.IsHeld && !pickup.Stashed)) && transform.position.y < 0.25f;

        void Awake()
        {
            pickup = GetComponent<Pickup>();
            mpb = new MaterialPropertyBlock();
            if (tintRenderers == null || tintRenderers.Length == 0) tintRenderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[tintRenderers.Length];
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                var m = tintRenderers[i] ? tintRenderers[i].sharedMaterial : null;
                baseColors[i] = m && m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            }
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Absorb(float liters)
        {
            soaked = Mathf.Min(capacityLiters, soaked + liters);
        }

        /// <summary>Wring it out (returns liters squeezed out).</summary>
        public float Wring()
        {
            float l = soaked; soaked = 0f; return l;
        }

        void Update()
        {
            if (absorbRate <= 0f || tintRenderers == null) return;
            float w = Wetness01;
            for (int i = 0; i < tintRenderers.Length; i++)
            {
                if (!tintRenderers[i] || tintRenderers[i] is ParticleSystemRenderer) continue;
                mpb.SetColor("_BaseColor", Color.Lerp(baseColors[i], baseColors[i] * 0.55f, w));
                mpb.SetFloat("_Smoothness", Mathf.Lerp(0.15f, 0.55f, w));
                tintRenderers[i].SetPropertyBlock(mpb);
            }
            // A soaked towel/rug that is being carried drips.
            if (pickup && pickup.IsHeld && soaked > 0.3f && FloodSim.I)
            {
                float drip = Mathf.Min(soaked, 0.12f * Time.deltaTime);
                soaked -= drip;
                FloodSim.I.AddLiters(transform.position, drip, 0.2f);
            }
        }

        public bool Covers(Vector3 p, float radius)
        {
            if (!hidesWater || !OnFloor) return false;
            Vector3 l = Quaternion.Inverse(transform.rotation) * (p - transform.position);
            return Mathf.Abs(l.x) <= halfSize.x + radius && Mathf.Abs(l.z) <= halfSize.y + radius;
        }

        public static CoverObject CoverAt(Vector3 p, float radius)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] && All[i].Covers(p, radius)) return All[i];
            return null;
        }
    }
}
