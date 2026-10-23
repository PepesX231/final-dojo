using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    public enum FireType { Grease, Ordinary }

    /// <summary>
    /// A fire with intensity 0..1. Grows over time, spreads to nearby flammables, pumps smoke.
    /// Grease fires (the curry pot) FLARE UP if you throw water on them; smother them (lid / wet towel)
    /// or use the extinguisher. Ordinary fires (towel, curtain) can be doused with water.
    /// </summary>
    public class FireSource : MonoBehaviour, IMess
    {
        public static readonly List<FireSource> All = new List<FireSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public string label = "Fire";
        public FireType type = FireType.Ordinary;
        [Range(0, 1)] public float intensity;
        public bool burning;
        public float growth = 0.012f;
        public float extraGrowth;
        public float spreadThreshold = 0.55f;
        public float spreadDelay = 14f;
        public List<FireSource> spreadTo = new List<FireSource>();
        public float smokeRate = 0.9f;
        public float radius = 0.4f;

        [Header("Visuals")]
        public ParticleSystem flames;
        public ParticleSystem embers;
        public Light glow;
        public float maxFlameRate = 60f;
        public float maxFlameSize = 0.9f;
        public float baseLight = 4f;
        public GameObject scorch;
        public Renderer[] charRenderers;

        float spreadT, flickerT;
        bool everBurned;
        MaterialPropertyBlock mpb;

        public bool EverBurned => everBurned;

        void Awake() { mpb = new MaterialPropertyBlock(); }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); MessRegistry.Add(this); }
        void OnDisable() { All.Remove(this); MessRegistry.Remove(this); }

        public void Ignite(float startIntensity)
        {
            if (!burning) { if (GameManager.I) GameManager.I.OnFireStarted(this); MamaAudio.Play("flare", transform.position, 0.9f, 0.8f); }
            burning = true;
            everBurned = true;
            intensity = Mathf.Max(intensity, startIntensity);
        }

        /// <summary>Extinguisher hit. amount ~ per-second power * dt.</summary>
        public void Extinguish(float amount)
        {
            if (!burning) return;
            intensity -= amount;
            if (intensity <= 0f) PutOut("The fire is OUT!");
        }

        /// <summary>Lid / wet towel / blanket. Works only if the fire isn't too big.</summary>
        public bool Smother(float maxIntensity)
        {
            if (!burning) return false;
            if (intensity > maxIntensity) return false;
            PutOut("Smothered! No air, no fire.");
            return true;
        }

        /// <summary>Water thrown on it.</summary>
        public void Douse(float liters, PlayerController p)
        {
            if (!burning) return;
            if (type == FireType.Grease)
            {
                intensity = Mathf.Min(1f, intensity + 0.45f);
                foreach (var s in spreadTo) if (s && !s.burning) s.Ignite(0.35f);
                if (flames) flames.Emit(80);
                MamaAudio.Play("flare", transform.position, 1f, 0.7f); MamaAudio.Play("splash", transform.position, 0.8f);
                if (SmokeSim.I) SmokeSim.I.Add(transform.position, 3f);
                if (p) p.KnockBack(transform.position, 5f);
                if (GameManager.I) GameManager.I.OnFlareUp(this);
                return;
            }
            intensity -= liters * 0.12f;
            if (intensity <= 0f) PutOut("*HSSSSS* Doused it!");
        }

        void PutOut(string msg)
        {
            burning = false;
            intensity = 0f;
            MamaAudio.Play("whoosh", transform.position, 0.9f, 0.6f); MamaAudio.Play("good", null, 0.6f);
            if (SmokeSim.I) SmokeSim.I.Add(transform.position, 0.8f);
            if (GameManager.I) { GameManager.I.Toast(msg); GameManager.I.OnFireOut(this); }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool sim = GameManager.I == null || GameManager.I.SimRunning;
            if (burning && sim)
            {
                intensity = Mathf.Min(1f, intensity + (growth + extraGrowth) * dt);
                if (SmokeSim.I) SmokeSim.I.Add(transform.position, (0.15f + intensity) * smokeRate * dt);
                if (intensity >= spreadThreshold)
                {
                    spreadT += dt;
                    if (spreadT >= spreadDelay)
                    {
                        foreach (var s in spreadTo)
                            if (s && !s.burning) { s.Ignite(0.15f); if (GameManager.I) GameManager.I.Toast("The fire SPREAD to the " + s.label.ToLower() + "!"); }
                        spreadT = -999f;
                    }
                }
            }

            float k = burning ? intensity : 0f;
            if (flames)
            {
                var em = flames.emission; em.rateOverTime = maxFlameRate * k;
                var main = flames.main; main.startSizeMultiplier = Mathf.Lerp(0.15f, maxFlameSize, k);
            }
            if (embers) { var em = embers.emission; em.rateOverTime = 14f * k; }
            if (glow)
            {
                flickerT += dt * 14f;
                glow.enabled = k > 0.01f;
                glow.intensity = baseLight * k * (0.75f + 0.25f * Mathf.PerlinNoise(flickerT, 0.3f));
            }
            if (scorch) scorch.SetActive(everBurned);
            if (charRenderers != null && everBurned)
                foreach (var r in charRenderers) if (r) { mpb.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.09f)); r.SetPropertyBlock(mpb); }
        }

        public float SpreadIn => burning && intensity >= spreadThreshold && spreadT >= 0f && HasUnburnt ? Mathf.Max(0f, spreadDelay - spreadT) : -1f;
        bool HasUnburnt { get { foreach (var s in spreadTo) if (s && !s.burning && !s.everBurned) return true; return false; } }

        // IMess
        public MessKind Kind => MessKind.Hazard;
        public bool IsProblem => burning || everBurned;
        public bool IsHidden => false;
        public int Severity => burning ? 5 : 2;
        public string MomLine => burning ? "FIRE!!! THE " + label.ToUpper() + " IS ON FIRE!!!" : "Why is the " + label.ToLower() + " all BLACK and burnt?!";
        public string Label => label;
        public Vector3 Position => transform.position;
    }
}
