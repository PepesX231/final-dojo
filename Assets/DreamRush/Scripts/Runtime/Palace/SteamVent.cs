using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M16: street / rooftop steam plume (soft puffs drifting up and sideways) — the ref's "the city breathes".</summary>
    public class SteamVent : MonoBehaviour
    {
        public float rate = 6f, height = 6f, width = 1.2f;
        public Color color = new Color(0.7f, 0.72f, 0.8f, 0.16f);

        void Start()
        {
            var lib = Presentation.VFXLibrary.Instance; if (lib == null || lib.puff == null) return;
            var ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = lib.puff; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(height / 3.5f, height / 2.2f); main.startSize = new ParticleSystem.MinMaxCurve(width, width * 2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startColor = color; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60; main.prewarm = true;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = width * 0.3f; sh.rotation = new Vector3(-90, 0, 0);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 2.6f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.4f, 1.2f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.4f;
            ps.Play();
        }
    }
}
