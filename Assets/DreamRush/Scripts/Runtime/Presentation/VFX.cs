using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Presentation
{
    public enum VFXKind { DustPuff, LandingRing, StarBurst, ImpactRing, Confetti, CardBurst, Zzz, Poof, Embers, HitSpark, Flare, FlareStreak, Rays }

    /// <summary>
    /// Real particle VFX built in code (docs/RESEARCH_V2.md §3): Anticipation → Impact (first 25% of life) →
    /// Dissipation, non-linear curves, big simple shapes, limited palette, and an HD-2D style point-light flash on
    /// impacts. Pooled per kind. Presentation only — never affects gameplay.
    /// </summary>
    public static class VFX
    {
        static readonly Dictionary<VFXKind, Stack<ParticleSystem>> _pool = new Dictionary<VFXKind, Stack<ParticleSystem>>();
        static Transform _root;
        public static int Spawned { get; private set; }

        public static readonly Color Cream = new Color(0.98f, 0.94f, 0.86f);
        public static readonly Color Gold = new Color(1f, 0.84f, 0.45f);
        public static readonly Color Lavender = new Color(0.72f, 0.66f, 0.95f);
        public static readonly Color Danger = new Color(0.96f, 0.3f, 0.32f);

        public static ParticleSystem Play(VFXKind kind, Vector3 position, float scale = 1f, Color? tint = null)
        {
            var lib = VFXLibrary.Instance;
            if (lib == null) return null;
            if (_root == null) { _root = new GameObject("[VFX]").transform; Object.DontDestroyOnLoad(_root.gameObject); }
            if (!_pool.TryGetValue(kind, out var stack)) _pool[kind] = stack = new Stack<ParticleSystem>();
            ParticleSystem ps = null;
            while (stack.Count > 0 && ps == null) ps = stack.Pop();
            if (ps == null) ps = Build(kind, lib);
            if (ps == null) return null;
            ps.transform.SetParent(_root, false);
            if (kind == VFXKind.LandingRing || kind == VFXKind.ImpactRing) position.y += 0.06f;
            ps.transform.position = position;
            ps.transform.localScale = Vector3.one * scale;
            if (tint.HasValue)
            {
                var main = ps.main;
                main.startColor = tint.Value;
            }
            ps.gameObject.SetActive(true);
            ps.Clear(true);
            ps.Play(true);
            Spawned++;
            var ret = ps.GetComponent<VFXReturn>();
            ret.Arm(kind, stack);
            if (kind == VFXKind.StarBurst || kind == VFXKind.ImpactRing || kind == VFXKind.CardBurst || kind == VFXKind.Flare)
                LightFlash(position + Vector3.up * 0.6f, tint ?? Gold, 2.6f * scale, 5f * scale, 0.28f);
            return ps;
        }

        /// <summary>HD-2D trick: every impact briefly lights the world around it.</summary>
        public static void LightFlash(Vector3 at, Color color, float intensity, float range, float seconds)
        {
            var go = new GameObject("VFX_LightFlash");
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            go.transform.position = at;
            go.AddComponent<LightFlashFade>().Init(intensity, seconds);
        }

        static ParticleSystem Build(VFXKind kind, VFXLibrary lib)
        {
            var go = new GameObject("VFX_" + kind);
            go.SetActive(false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            go.AddComponent<VFXReturn>();
            var main = ps.main;
            main.playOnAwake = false; main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape;
            var col = ps.colorOverLifetime; col.enabled = true;
            var size = ps.sizeOverLifetime; size.enabled = true;

            switch (kind)
            {
                case VFXKind.DustPuff:
                case VFXKind.Poof:
                {
                    bool poof = kind == VFXKind.Poof;
                    r.sharedMaterial = lib.puff;
                    main.duration = 0.6f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, poof ? 0.7f : 0.5f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(poof ? 2.5f : 1.2f, poof ? 4.5f : 2.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(poof ? 0.6f : 0.25f, poof ? 1.1f : 0.5f);
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.startColor = Cream; main.gravityModifier = -0.15f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(poof ? 14 : 7)) });
                    shape.shapeType = poof ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Circle; shape.radius = poof ? 0.4f : 0.25f;
                    if (!poof) shape.rotation = new Vector3(90, 0, 0);
                    var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.2f; lim.dampen = 0.25f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.5f));
                    col.color = Fade(Cream, 0.05f, 0.6f);
                    break;
                }
                case VFXKind.LandingRing:
                case VFXKind.ImpactRing:
                {
                    bool impact = kind == VFXKind.ImpactRing;
                    r.sharedMaterial = lib.ring;
                    r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    main.duration = 0.5f; main.startLifetime = impact ? 0.55f : 0.32f; main.startSpeed = 0f;
                    main.startSize = impact ? 1f : 0.8f; main.startColor = impact ? Danger : Cream;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    shape.enabled = false;
                    // impact within the first 25%: fast expand, then slow out
                    var curve = new AnimationCurve(new Keyframe(0f, 0.2f, 0f, 9f), new Keyframe(0.25f, 0.8f), new Keyframe(1f, 1f, 0.2f, 0f));
                    size.size = new ParticleSystem.MinMaxCurve(impact ? 9f : 2.6f, curve);
                    col.color = Fade(impact ? Danger : Cream, 0.02f, 0.4f);
                    break;
                }
                case VFXKind.StarBurst:
                case VFXKind.CardBurst:
                {
                    r.sharedMaterial = lib.sparkleAdd;
                    bool card = kind == VFXKind.CardBurst;
                    main.duration = 0.8f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, card ? 9f : 6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.2f, card ? 0.6f : 0.45f);
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.startColor = new ParticleSystem.MinMaxGradient(Gold, new Color(1f, 0.62f, 0.25f));
                    main.gravityModifier = 0.4f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(card ? 24 : 14)) });
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.2f;
                    var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.5f; lim.dampen = 0.18f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.15f, 1.2f), new Keyframe(1, 0f)));
                    var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    col.color = Fade(Color.white, 0.0f, 0.5f);
                    break;
                }
                case VFXKind.Confetti:
                {
                    r.sharedMaterial = lib.confetti;
                    main.duration = 1f; main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 11f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
                    main.startRotation3D = true;
                    main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2); main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                    var grad = new Gradient();
                    grad.SetKeys(new[] { new GradientColorKey(new Color(0.91f, 0.36f, 0.4f), 0f), new GradientColorKey(Gold, 0.33f),
                                         new GradientColorKey(new Color(0.56f, 0.84f, 0.93f), 0.66f), new GradientColorKey(Lavender, 1f) },
                                 new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                    main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
                    main.gravityModifier = 0.55f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 90) });
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.4f; shape.rotation = new Vector3(-90, 0, 0);
                    var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 1.2f; lim.dampen = 0.08f;
                    var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
                    rot.x = new ParticleSystem.MinMaxCurve(-8f, 8f); rot.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.8f; noise.frequency = 0.6f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Constant(0, 1, 1));
                    col.color = Fade(Color.white, 0.0f, 0.85f);
                    break;
                }
                case VFXKind.Embers:
                {
                    // the reference's floating embers: tiny hot sparks that drift up and wander, long tail of life
                    r.sharedMaterial = lib.glowAdd;
                    main.duration = 0.5f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.9f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 3.2f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(1f, 0.55f, 0.15f));
                    main.gravityModifier = -0.22f;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 22) });
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.45f;
                    var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 0.6f; lim.dampen = 0.12f;
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.9f; noise.frequency = 1.1f; noise.scrollSpeed = 0.6f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.1f, 1.2f), new Keyframe(0.7f, 0.8f), new Keyframe(1, 0f)));
                    var eg = new Gradient();
                    eg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0.4f), new GradientColorKey(new Color(1f, 0.35f, 0.1f), 1f) },
                               new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
                    col.color = new ParticleSystem.MinMaxGradient(eg);
                    break;
                }
                case VFXKind.HitSpark:
                {
                    // anime impact: a star of thin hot streaks that shoot out and die in ~0.12 s
                    r.sharedMaterial = lib.streakAdd != null ? lib.streakAdd : lib.glowAdd;
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.2f; r.velocityScale = 0.06f;
                    main.duration = 0.2f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.17f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 22f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
                    main.startColor = Color.white;
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.05f;
                    var lim = ps.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 1f; lim.dampen = 0.35f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0.2f)));
                    col.color = Fade(Color.white, 0f, 0.4f);
                    break;
                }
                case VFXKind.Flare:
                case VFXKind.FlareStreak:
                {
                    // backlit-sun flare: a big soft bloom + an anamorphic horizontal streak (HD-2D lens look)
                    bool streak = kind == VFXKind.FlareStreak;
                    r.sharedMaterial = streak && lib.streakAdd != null ? lib.streakAdd : lib.glowAdd;
                    main.duration = 0.5f; main.startLifetime = streak ? 0.32f : 0.42f; main.startSpeed = 0f;
                    main.startSize3D = streak;
                    if (streak) { main.startSizeX = 9f; main.startSizeY = 0.32f; main.startSizeZ = 1f; }
                    else main.startSize = 5f;
                    main.startColor = new Color(1f, 0.86f, 0.55f);
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                    shape.enabled = false;
                    size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f, 0f, 8f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0.6f)));
                    col.color = Fade(Color.white, 0.04f, 0.25f);
                    break;
                }
                case VFXKind.Rays:
                {
                    // god rays: long beams through the centre that turn slowly while they fade
                    r.sharedMaterial = lib.beamAdd != null ? lib.beamAdd : lib.glowAdd;
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 16f; r.velocityScale = 0f;
                    main.duration = 0.5f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
                    main.startSpeed = 0.02f; main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f, 0.8f), new Color(1f, 0.7f, 0.9f, 0.6f));
                    emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                    shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.01f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.2f, 0f, 6f), new Keyframe(0.2f, 1f), new Keyframe(1, 0.7f)));
                    col.color = Fade(Color.white, 0.05f, 0.35f);
                    break;
                }
                case VFXKind.Zzz:
                {
                    r.sharedMaterial = lib.z;
                    main.duration = 2f; main.loop = true; main.startLifetime = 2.2f;
                    main.startSpeed = 0.5f; main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
                    main.startColor = Lavender;
                    emission.rateOverTime = 0.9f;
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 20f; shape.radius = 0.05f; shape.rotation = new Vector3(-80, 0, 0);
                    var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.8f;
                    size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.4f, 1, 1.4f));
                    col.color = Fade(Lavender, 0.2f, 0.6f);
                    break;
                }
            }
            return ps;
        }

        static ParticleSystem.MinMaxGradient Fade(Color c, float fadeIn, float holdUntil)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(fadeIn > 0 ? 0f : 1f, 0f), new GradientAlphaKey(1f, Mathf.Max(0.01f, fadeIn)),
                              new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }
    }

    /// <summary>Returns a finished one-shot particle system to its pool.</summary>
    public class VFXReturn : MonoBehaviour
    {
        ParticleSystem _ps;
        Stack<ParticleSystem> _stack;
        bool _armed;
        public void Arm(VFXKind kind, Stack<ParticleSystem> stack) { _ps = GetComponent<ParticleSystem>(); _stack = stack; _armed = !_ps.main.loop; }
        void Update()
        {
            if (!_armed || _ps == null || _ps.IsAlive(true)) return;
            _armed = false;
            gameObject.SetActive(false);
            _stack?.Push(_ps);
        }
    }

    public class LightFlashFade : MonoBehaviour
    {
        Light _l; float _i, _t, _life;
        public void Init(float intensity, float seconds) { _l = GetComponent<Light>(); _i = intensity; _life = seconds; }
        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / _life;
            if (_l != null) _l.intensity = _i * (k < 0.15f ? k / 0.15f : Mathf.Pow(1f - (k - 0.15f) / 0.85f, 2f));
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
