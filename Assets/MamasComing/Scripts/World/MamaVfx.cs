using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing
{
    /// Cartoon VFX library: dust puffs, water splashes + ripples, comic hit bursts, fix sparkles, electric sparks,
    /// steam, fire bursts and confetti, drawn with the hand-made sprites in Resources/MamaVfx.
    /// One world-space particle system per look, re-used for every burst (cheap). Most effects fire automatically
    /// from the sound that already plays at that spot (MamaAudio -> ForSound), so every peer sees them.
    public class MamaVfx : MonoBehaviour
    {
        static MamaVfx host;
        public static float SizeK = 1.9f;
        readonly Dictionary<string, ParticleSystem> sys = new Dictionary<string, ParticleSystem>();
        readonly Dictionary<FireSource, ParticleSystem> glows = new Dictionary<FireSource, ParticleSystem>();
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        bool upgraded; float scanT;

        static MamaVfx H
        {
            get
            {
                if (host) return host;
                var go = new GameObject("MamaVfx"); host = go.AddComponent<MamaVfx>(); return host;
            }
        }

        Material Mat(string n)
        {
            if (mats.TryGetValue(n, out var m) && m) return m;
            m = Resources.Load<Material>("MamaVfx/M_" + n); mats[n] = m; return m;
        }

        // ------------------------------------------------------------------ systems
        ParticleSystem Sys(string key)
        {
            if (sys.TryGetValue(key, out var ps) && ps) return ps;
            var go = new GameObject("vfx " + key); go.transform.SetParent(transform, false);
            ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            var main = ps.main; main.loop = true; main.playOnAwake = true; main.maxParticles = 600; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0; main.startLifetime = 1; main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
            Color w = Color.white;
            switch (key)
            {
                case "puff":   Setup(ps, r, "vfx_puff", grav: -.05f, drag: 3f, grow: Curve(.5f, 1.25f, 1.4f), fade: Fade(1, .12f, .55f), spin: 60); break;
                case "smoke":  Setup(ps, r, "vfx_smoke", grav: -.08f, drag: 1.5f, grow: Curve(.6f, 1.4f, 2f), fade: Fade(.7f, .2f, .5f), spin: 30); break;
                case "drops":  Setup(ps, r, "vfx_drop", grav: 1.6f, drag: 0f, grow: Curve(1f, 1f, .5f), fade: Fade(1, .05f, .8f)); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .06f; r.lengthScale = 1.2f; break;
                case "ripple": Setup(ps, r, "vfx_ring", grav: 0, drag: 0, grow: Curve(.2f, .9f, 1.3f), fade: Fade(.85f, .02f, .2f)); r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; r.sortingFudge = 2; break;
                case "bang":   Setup(ps, r, "vfx_bang", grav: 0, drag: 0, grow: Pop(), fade: Fade(1, .01f, .75f), spin: 0); break;
                case "stars":  Setup(ps, r, "vfx_star", grav: .9f, drag: 1.2f, grow: Curve(1.1f, 1f, .2f), fade: Fade(1, .02f, .7f), spin: 220); break;
                case "glow":   Setup(ps, r, "vfx_soft", grav: 0, drag: 0, grow: Curve(.6f, 1f, 1.3f), fade: Fade(.9f, .05f, .3f)); break;
                case "sparks": Setup(ps, r, "vfx_spark", grav: 2.2f, drag: .6f, grow: Curve(1f, 1f, .3f), fade: Fade(1, .02f, .6f)); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .09f; r.lengthScale = 1.5f; break;
                case "flame":  Setup(ps, r, "vfx_flame", grav: -.6f, drag: 1.2f, grow: Curve(.7f, 1f, .2f), fade: Fade(.95f, .05f, .5f)); break;
                case "chips":  Setup(ps, r, "vfx_chip", grav: .5f, drag: 1.6f, grow: Curve(1f, 1f, .8f), fade: Fade(1, .02f, .85f), spin: 400); break;
            }
            ps.Play(); sys[key] = ps; return ps;
        }

        void Setup(ParticleSystem ps, ParticleSystemRenderer r, string mat, float grav, float drag, ParticleSystem.MinMaxCurve grow, Gradient fade, float spin = 0)
        {
            r.sharedMaterial = Mat(mat);
            var main = ps.main; main.gravityModifier = grav;
            if (drag > 0) { var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = drag; lv.multiplyDragByParticleSize = false; lv.multiplyDragByParticleVelocity = true; }
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = grow;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = fade;
            if (spin != 0) { var ro = ps.rotationOverLifetime; ro.enabled = true; ro.z = new ParticleSystem.MinMaxCurve(-spin * Mathf.Deg2Rad, spin * Mathf.Deg2Rad); }
        }
        static ParticleSystem.MinMaxCurve Curve(float a, float b, float c) => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, a), new Keyframe(.35f, b), new Keyframe(1, c)));
        static ParticleSystem.MinMaxCurve Pop() => new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, .2f), new Keyframe(.12f, 1.15f), new Keyframe(.3f, .95f), new Keyframe(1, .6f)));
        static Gradient Fade(float a, float inT, float outT)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(a, inT), new GradientAlphaKey(a, outT), new GradientAlphaKey(0, 1) });
            return g;
        }

        void Emit(string key, Vector3 pos, Vector3 vel, float size, float life, Color c, float rot = -1f)
        {
            var ps = Sys(key);
            // the game camera sits high above the house: everything is drawn chunkier and lingers a bit longer than 'real size'
            var ep = new ParticleSystem.EmitParams { position = pos, velocity = vel * 1.25f, startSize = size * SizeK, startLifetime = life * 1.35f, startColor = c, rotation = rot < 0 ? Random.value * 360f : rot, applyShapeToPosition = false };
            ps.Emit(ep, 1);
        }
        static Vector3 Ring(float speed, float up) { float a = Random.value * Mathf.PI * 2; return new Vector3(Mathf.Cos(a) * speed, up, Mathf.Sin(a) * speed); }

        // ------------------------------------------------------------------ effects
        public static void Puff(Vector3 p, float s = 1f, int n = 7, Color? tint = null)
        {
            var c = tint ?? new Color(1f, .95f, .9f, .85f);
            for (int i = 0; i < n; i++) H.Emit("puff", p + Ring(.12f * s, 0) + Vector3.up * .05f, Ring(Random.Range(.8f, 1.6f) * s, Random.Range(.2f, .6f)), Random.Range(.22f, .38f) * s, Random.Range(.45f, .7f), c);
        }
        public static void Splash(Vector3 p, float s = 1f)
        {
            var h = H; var blue = new Color(.45f, .72f, 1f, 1f);
            for (int i = 0; i < 16; i++) h.Emit("drops", p + Vector3.up * .05f, Ring(Random.Range(.6f, 1.8f) * s, Random.Range(1.6f, 3.2f) * s), Random.Range(.13f, .2f) * s, Random.Range(.5f, .8f), blue, 0);
            float y = FloorY(p);
            h.Emit("ripple", new Vector3(p.x, y + .02f, p.z), Vector3.zero, 1.1f * s, .7f, new Color(.85f, .96f, 1f, .9f), 0);
            h.Emit("ripple", new Vector3(p.x, y + .02f, p.z), Vector3.zero, .7f * s, .55f, new Color(.85f, .96f, 1f, .7f), 0);
            Puff(p, .6f * s, 3, new Color(.85f, .95f, 1f, .55f));
        }
        public static void Wet(Vector3 p, float s = 1f)
        {
            var h = H; var c = new Color(.7f, .88f, 1f, .9f);
            for (int i = 0; i < 6; i++) h.Emit("drops", p + Vector3.up * .05f, Ring(Random.Range(.4f, 1f) * s, Random.Range(.8f, 1.6f)), Random.Range(.05f, .09f) * s, .45f, c, 0);
        }
        public static void Impact(Vector3 p, float s = 1f)
        {
            var h = H;
            h.Emit("bang", p, Vector3.zero, .75f * s, .28f, new Color(1f, .93f, .55f, 1f), Random.Range(0, 360));
            h.Emit("bang", p, Vector3.zero, .45f * s, .24f, new Color(1f, 1f, 1f, 1f), Random.Range(0, 360));
            for (int i = 0; i < 5; i++) h.Emit("stars", p, Ring(Random.Range(1.6f, 2.6f) * s, Random.Range(1.2f, 2.4f)), Random.Range(.16f, .26f) * s, Random.Range(.5f, .75f), new Color(1f, .86f, .3f, 1f));
            Puff(p - Vector3.up * .4f, .7f * s, 4);
        }
        /// A problem got fixed: a golden ring, a flash and twinkling stars floating up.
        public static void Fixed(Vector3 p)
        {
            var h = H;
            h.Emit("glow", p + Vector3.up * .5f, Vector3.zero, 1.6f, .45f, new Color(1f, .9f, .55f, .8f));
            float y = FloorY(p);
            h.Emit("ripple", new Vector3(p.x, y + .03f, p.z), Vector3.zero, 2.2f, .8f, new Color(1f, .85f, .35f, 1f), 0);
            for (int i = 0; i < 14; i++) h.Emit("stars", p + Ring(Random.Range(.1f, .5f), Random.Range(.2f, 1f)), Ring(Random.Range(.3f, 1f), Random.Range(1.2f, 2.4f)), Random.Range(.14f, .28f), Random.Range(.8f, 1.3f), Color.Lerp(new Color(1f, .85f, .3f), Color.white, Random.value));
            Confetti(p + Vector3.up * .6f, 16);
        }
        public static void Confetti(Vector3 p, int n = 20)
        {
            Color[] cols = { new Color(1f, .45f, .55f), new Color(1f, .8f, .3f), new Color(.35f, .8f, .75f), new Color(.6f, .5f, 1f), new Color(.55f, .9f, .45f) };
            for (int i = 0; i < n; i++) H.Emit("chips", p, Ring(Random.Range(.6f, 2f), Random.Range(2f, 3.6f)), Random.Range(.08f, .13f), Random.Range(1.1f, 1.7f), cols[Random.Range(0, cols.Length)]);
        }
        public static void Zap(Vector3 p)
        {
            var h = H;
            h.Emit("glow", p, Vector3.zero, 1.4f, .25f, new Color(.6f, .9f, 1f, 1f));
            h.Emit("bang", p, Vector3.zero, .8f, .18f, new Color(.45f, .85f, 1f, 1f));
            h.Emit("bang", p, Vector3.zero, .45f, .16f, new Color(1f, 1f, .8f, 1f));
            for (int i = 0; i < 22; i++) h.Emit("sparks", p, Random.onUnitSphere * Random.Range(2.5f, 5f) + Vector3.up, Random.Range(.1f, .17f), Random.Range(.25f, .5f), Random.value < .5f ? new Color(.6f, .95f, 1f) : new Color(1f, .95f, .5f));
        }
        public static void FireBurst(Vector3 p)
        {
            var h = H;
            h.Emit("glow", p + Vector3.up * .3f, Vector3.zero, 1.8f, .5f, new Color(1f, .55f, .2f, 1f));
            for (int i = 0; i < 12; i++) h.Emit("flame", p + Ring(.15f, 0), Ring(Random.Range(.3f, .9f), Random.Range(1.5f, 2.6f)), Random.Range(.35f, .6f), Random.Range(.35f, .6f), new Color(1f, Random.Range(.55f, .8f), .25f, 1f), 0);
            for (int i = 0; i < 14; i++) h.Emit("sparks", p + Vector3.up * .2f, Ring(Random.Range(.4f, 1.4f), Random.Range(2f, 4f)), Random.Range(.07f, .12f), Random.Range(.6f, 1.1f), new Color(1f, .7f, .3f));
        }
        public static void Steam(Vector3 p, float s = 1f)
        {
            for (int i = 0; i < 6; i++) H.Emit("smoke", p + Ring(.1f, 0), Ring(Random.Range(.1f, .3f), Random.Range(.5f, 1f)), Random.Range(.3f, .5f) * s, Random.Range(.9f, 1.4f), new Color(.95f, .97f, 1f, .55f));
        }

        /// Sound -> matching burst at the same spot (called by MamaAudio for every positioned sound).
        public static void ForSound(string name, Vector3 p, float vol)
        {
            switch (name)
            {
                case "splash": Splash(p, Mathf.Clamp(vol * 1.1f, .6f, 1.3f)); break;
                case "slap": Impact(p + Vector3.up * .9f, .9f); break;
                case "thud": if (vol >= .5f) Puff(p, .9f, 6); break;
                case "slam": Puff(p, .8f, 5); break;
                case "place": Puff(p, .45f, 4); break;
                case "zap": Zap(p + Vector3.up * .2f); break;
                case "flare": FireBurst(p); break;
                case "steam": Steam(p + Vector3.up * .4f, 1.4f); break;
                case "squish": case "wring": case "thwop": Wet(p, .8f); break;
                case "crack": Puff(p, .4f, 4, new Color(.95f, .9f, .85f, .8f)); break;
                case "step": if (vol >= .45f) Puff(p, .28f, 2); break;      // sprinting kicks up a little dust
                case "pop": Puff(p, .35f, 3); break;
            }
        }

        static float FloorY(Vector3 p) => Physics.Raycast(p + Vector3.up * .5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y : p.y;

        // ------------------------------------------------------------------ fire & smoke look (per scene)
        public static void Prepare() { var h = H; h.Upgrade(); }
        void Upgrade()
        {
            if (upgraded) return; upgraded = true;
            var flame = Mat("vfx_flame"); var spark = Mat("vfx_spark"); var smoke = Mat("vfx_smoke");
            foreach (var f in FireSource.All)
            {
                if (!f) continue;
                if (f.flames && flame)
                {
                    var r = f.flames.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = flame;
                    var ro = f.flames.rotationOverLifetime; ro.enabled = true; ro.z = new ParticleSystem.MinMaxCurve(-.6f, .6f);
                    var sz = f.flames.sizeOverLifetime; sz.enabled = true; sz.size = Curve(.7f, 1f, .25f);
                }
                if (f.embers && spark)
                {
                    var r = f.embers.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = spark;
                    r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = .08f; r.lengthScale = 1.3f;
                    var main = f.embers.main; main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .75f, .3f), new Color(1f, .45f, .15f));
                }
            }
            if (SmokeSim.I && SmokeSim.I.smokeFx && smoke) SmokeSim.I.smokeFx.GetComponent<ParticleSystemRenderer>().sharedMaterial = smoke;
        }

        void LateUpdate()
        {
            // a warm, breathing glow under every burning fire (sells the heat, lights up the smoke)
            scanT -= Time.deltaTime; if (scanT > 0f) return; scanT = .1f;
            if (!upgraded && FireSource.All.Count > 0) Upgrade();
            foreach (var f in FireSource.All)
            {
                if (!f) continue;
                if (!glows.TryGetValue(f, out var g) || !g)
                {
                    if (!f.burning) continue;
                    var go = new GameObject("Fire glow"); go.transform.SetParent(f.transform, false); go.transform.localPosition = Vector3.up * .35f;
                    g = go.AddComponent<ParticleSystem>(); g.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Mat("vfx_soft"); r.shadowCastingMode = ShadowCastingMode.Off;
                    var main = g.main; main.loop = true; main.startLifetime = .9f; main.startSpeed = 0; main.maxParticles = 12; main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.startColor = new Color(1f, .5f, .18f, .32f);
                    var em = g.emission; em.rateOverTime = 6; var sh = g.shape; sh.enabled = false;
                    var col = g.colorOverLifetime; col.enabled = true; col.color = Fade(.8f, .3f, .6f);
                    g.Play(); glows[f] = g;
                }
                var m = g.main; m.startSize = new ParticleSystem.MinMaxCurve(.7f + f.intensity * 1.6f, .9f + f.intensity * 2f);
                var e = g.emission; e.enabled = f.burning;
            }
        }
    }
}
