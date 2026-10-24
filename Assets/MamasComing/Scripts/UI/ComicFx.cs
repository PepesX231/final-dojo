using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Bright comic-book VFX: POW starbursts with halftone, speed lines, shock rings, ink-outlined dust puffs,
    /// dizzy stars and water splashes. Pooled camera-facing sprites (Art/ComicFx, MamasComing/ComicSprite), no
    /// particle systems, safe to call from anywhere: ComicFx.Pow(pos, colour).
    public class ComicFx : MonoBehaviour
    {
        enum Kind { Burst, BurstB, Puff, Star, Speed, Splash, Ring }
        static readonly string[] Tex = { "FX_ComicBurst", "FX_ComicBurstB", "FX_InkPuff", "FX_Star", "FX_SpeedLines", "FX_Splash", "FX_Ring" };

        class Sprite
        {
            public Transform t; public MeshRenderer r; public Kind kind;
            public Vector3 pos, vel; public float age, life, size, grow, spin, rot, gravity, pop;
            public Color col; public Transform follow; public Vector3 orbit; public bool alive;
        }

        static ComicFx I;
        readonly List<Sprite> live = new List<Sprite>(), pool = new List<Sprite>();
        Material[] mats; Mesh quad; MaterialPropertyBlock mpb;

        static ComicFx Get()
        {
            if (I) return I;
            var go = new GameObject("ComicFx");
            I = go.AddComponent<ComicFx>();
            return I;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; }

        void Awake()
        {
            mats = new Material[Tex.Length];
            for (int i = 0; i < Tex.Length; i++) mats[i] = Resources.Load<Material>("ComicFx/" + Tex[i]);
            mpb = new MaterialPropertyBlock();
            quad = new Mesh { name = "ComicQuad" };
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f) };
            quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.RecalculateBounds();
        }

        // ------------------------------------------------------------ public API
        /// Punch / bonk impact: starburst + speed lines + shock ring + popping stars.
        public static void Pow(Vector3 at, Color tint, float size = 1f)
        {
            var f = Get(); if (!f.Ready) return;
            var cam = Camera.main; Vector3 back = cam ? cam.transform.forward * .06f : Vector3.zero;
            f.Spawn(Kind.Speed, at + back * 2f, Vector3.zero, .34f, 1.9f * size, 1.25f, 0f, Color.white).pop = .05f;
            f.Spawn(Random.value < .5f ? Kind.Burst : Kind.BurstB, at + back, Vector3.zero, .55f, .95f * size, .25f, Random.Range(-40f, 40f), Color.Lerp(Color.white, tint, .25f)).pop = .09f;
            f.Spawn(Kind.Ring, at + back * 1.5f, Vector3.zero, .3f, .5f * size, 3.2f, 0f, Color.Lerp(Color.white, tint, .6f));
            for (int i = 0; i < 4; i++)
            {
                Vector3 v = (Random.insideUnitSphere + Vector3.up * .9f).normalized * Random.Range(2.2f, 3.4f) * size;
                var s = f.Spawn(Kind.Star, at, v, Random.Range(.45f, .65f), Random.Range(.14f, .22f) * size, -.4f, Random.Range(-400f, 400f), StarColor(i));
                s.gravity = 9f;
            }
        }

        /// Dust / landing puffs ringing out along the floor.
        public static void Puff(Vector3 at, float size = 1f, int count = 5)
        {
            var f = Get(); if (!f.Ready) return;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Random.value * .5f) / count * Mathf.PI * 2f;
                Vector3 v = new Vector3(Mathf.Cos(a), .25f, Mathf.Sin(a)) * Random.Range(1.2f, 1.9f) * size;
                var s = f.Spawn(Kind.Puff, at + Vector3.up * .08f, v, Random.Range(.35f, .5f), Random.Range(.2f, .3f) * size, -.6f, Random.Range(-90f, 90f), new Color(1f, .98f, .92f));
                s.gravity = -1.2f; s.pop = .06f;
            }
        }

        /// Cartoon stars circling a stunned kid's head.
        public static void Dizzy(Transform head, float seconds = 1.2f, float radius = .32f, float height = .62f)
        {
            var f = Get(); if (!f.Ready || !head) return;
            for (int i = 0; i < 3; i++)
            {
                var s = f.Spawn(Kind.Star, head.position, Vector3.zero, seconds, .16f, 0f, 220f, StarColor(i));
                s.follow = head; s.orbit = new Vector3(radius, height, i * 120f); s.pop = .1f;
            }
        }

        /// Water slosh: splash crown + droplets.
        public static void Splash(Vector3 at, float size = 1f)
        {
            var f = Get(); if (!f.Ready) return;
            f.Spawn(Kind.Splash, at + Vector3.up * .25f * size, Vector3.up * .4f, .45f, .6f * size, .5f, 0f, new Color(.55f, .85f, 1f)).pop = .08f;
            for (int i = 0; i < 4; i++)
            {
                var s = f.Spawn(Kind.Puff, at, (Random.insideUnitSphere + Vector3.up * 1.6f).normalized * 2.4f * size, .5f, .1f * size, -.2f, 0f, new Color(.45f, .8f, 1f));
                s.gravity = 9f;
            }
        }

        static Color StarColor(int i) => i % 3 == 0 ? new Color(1f, .9f, .25f) : i % 3 == 1 ? new Color(.45f, .9f, 1f) : new Color(1f, .5f, .75f);

        // ------------------------------------------------------------ runtime
        bool Ready => mats != null && mats[0];

        Sprite Spawn(Kind k, Vector3 at, Vector3 vel, float life, float size, float grow, float spin, Color col)
        {
            Sprite s;
            if (pool.Count > 0) { s = pool[pool.Count - 1]; pool.RemoveAt(pool.Count - 1); }
            else
            {
                var go = new GameObject("fx"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                s = new Sprite { t = go.transform, r = go.AddComponent<MeshRenderer>() };
                s.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; s.r.receiveShadows = false;
            }
            s.kind = k; s.r.sharedMaterial = mats[(int)k] ? mats[(int)k] : mats[0];
            s.pos = at; s.vel = vel; s.age = 0; s.life = life; s.size = size; s.grow = grow; s.spin = spin;
            s.rot = Random.Range(0f, 360f); s.gravity = 0; s.pop = 0; s.col = col; s.follow = null; s.alive = true;
            s.t.gameObject.SetActive(true);
            live.Add(s);
            return s;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var s = live[i];
                s.age += dt;
                float u = s.age / s.life;
                if (u >= 1f) { s.t.gameObject.SetActive(false); live.RemoveAt(i); pool.Add(s); continue; }
                s.vel += Vector3.down * s.gravity * dt;
                s.vel *= 1f - Mathf.Min(1f, dt * 2.2f);
                s.pos += s.vel * dt;
                if (s.follow)
                {
                    float a = (s.orbit.z + s.age * 300f) * Mathf.Deg2Rad;
                    s.pos = s.follow.position + new Vector3(Mathf.Cos(a) * s.orbit.x, s.orbit.y, Mathf.Sin(a) * s.orbit.x);
                }
                s.rot += s.spin * dt;
                // comic timing: snap in with an overshoot, hold, then shrink away
                float pop = s.pop > 0 && s.age < s.pop ? Mathf.Lerp(.2f, 1.3f, s.age / s.pop) : s.pop > 0 ? Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((s.age - s.pop) * 8f)) : 1f;
                float scale = s.size * pop * (1f + s.grow * u) * (u > .7f && s.grow <= 0 ? Mathf.Lerp(1f, .3f, (u - .7f) / .3f) : 1f);
                s.t.position = s.pos;
                if (cam) s.t.rotation = Quaternion.LookRotation(s.t.position - cam.transform.position, cam.transform.up) * Quaternion.Euler(0, 0, s.rot);
                s.t.localScale = Vector3.one * Mathf.Max(.001f, scale);
                var c = s.col; c.a *= u > .6f ? 1f - (u - .6f) / .4f : 1f;
                mpb.SetColor("_BaseColor", c);
                s.r.SetPropertyBlock(mpb);
            }
        }
    }
}
