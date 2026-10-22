using System.Collections;
using System.Collections.Generic;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>
    /// Undertale-style bullet patterns for COUNT MIDNIGHT's "STOP THE CLOCK" (docs/LEVEL2_DESIGN.md §4.3), laid out on the
    /// arena floor so a 3D platformer can read them: every bullet flies at chest height (0.7 m) — a jump clears the
    /// low gears/numerals, the tall clock-hand pillars must be walked/rolled around. Every pattern leaves a gap.
    /// Pooled; hits go through <see cref="IPlayerDamageReceiver"/> (so dodge-roll i-frames work on bullets too).
    /// </summary>
    public class MidnightBullets : MonoBehaviour
    {
        public enum Kind { Numeral, Gear, Pillar, Golden }
        class Bullet { public Transform t; public Vector3 vel; public float r, life, born, spin; public Kind kind; public bool alive, reflected; public Renderer rend; public CountMidnight boss; }

        public float bulletHeight = 0.7f, jumpClear = 0.75f;
        public Transform player;
        readonly List<Bullet> _pool = new List<Bullet>();
        Material _numMat, _gearMat, _pillarMat, _markMat, _goldMat;
        public int GoldenReflected { get; private set; }
        IPlayerDamageReceiver _receiver;
        public int Alive { get; private set; }
        public int Spawned { get; private set; }
        public int PlayerHits { get; private set; }

        void Awake()
        {
            _numMat = Look.DreamLook.Toon(new Color(1f, 0.92f, 0.55f), null, 0.6f, name: "M_Bullet_Numeral"); _numMat.SetColor("_Emission", new Color(1.6f, 1.2f, 0.4f));
            _gearMat = Look.DreamLook.Toon(new Color(0.4f, 0.95f, 1f), null, 0.6f, name: "M_Bullet_Gear"); _gearMat.SetColor("_Emission", new Color(0.2f, 1.2f, 1.6f));
            _pillarMat = Look.DreamLook.Toon(new Color(1f, 0.25f, 0.5f), null, 0.5f, name: "M_Bullet_Pillar"); _pillarMat.SetColor("_Emission", new Color(1.5f, 0.15f, 0.5f));
            _goldMat = Look.DreamLook.Toon(new Color(1f, 0.8f, 0.2f), null, 0.8f, name: "M_Bullet_Golden"); _goldMat.SetColor("_Emission", new Color(2.2f, 1.4f, 0.2f));
            var lib = VFXLibrary.Instance;
            _markMat = lib != null && lib.telegraph != null ? new Material(lib.telegraph) : Core.RuntimeMaterials.CreateUnlitTransparent(new Color(1, 0.2f, 0.3f, 0.6f), null, "M_BulletMark");
        }

        void Start()
        {
            if (player == null) { var m = FindFirstObjectByType<Player.PlayerMotor3D>(); if (m != null) player = m.transform; }
            if (player != null) _receiver = player.GetComponent<IPlayerDamageReceiver>();
        }

        Bullet Get(Kind k)
        {
            Bullet b = null;
            foreach (var x in _pool) if (!x.alive && x.kind == k) { b = x; break; }
            if (b == null)
            {
                var go = GameObject.CreatePrimitive(k == Kind.Pillar ? PrimitiveType.Cube : k == Kind.Gear ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
                if (k == Kind.Golden) { var sc = go.GetComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 1.2f; }
                else Destroy(go.GetComponent<Collider>());
                go.name = "Bullet_" + k; go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>(); r.sharedMaterial = k == Kind.Pillar ? _pillarMat : k == Kind.Gear ? _gearMat : k == Kind.Golden ? _goldMat : _numMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                b = new Bullet { t = go.transform, kind = k, rend = r };
                if (k == Kind.Golden) { var bb = b; var wp = go.AddComponent<BossWeakPoint>(); wp.onHit = h => Reflect(bb, h); Look.DreamLook.AddOutline(go, 2f); }
                _pool.Add(b);
            }
            b.alive = true; b.reflected = false; b.t.gameObject.SetActive(true); b.born = Time.time;
            Spawned++;
            return b;
        }

        public void Fire(Kind k, Vector3 pos, Vector3 vel, float life = 6f)
        {
            var b = Get(k);
            b.t.position = new Vector3(pos.x, k == Kind.Pillar ? pos.y + 1.6f : pos.y + bulletHeight, pos.z);
            b.vel = vel; b.life = life; b.spin = k == Kind.Gear ? 540f : 160f;
            b.r = k == Kind.Pillar ? 0.55f : k == Kind.Gear ? 0.42f : 0.36f;
            b.t.localScale = k == Kind.Pillar ? new Vector3(0.5f, 3.2f, 0.5f) : k == Kind.Gear ? new Vector3(0.85f, 0.08f, 0.85f) : Vector3.one * 0.62f;
            b.t.rotation = k == Kind.Gear ? Quaternion.identity : Quaternion.LookRotation(vel.sqrMagnitude > 0.01f ? vel : Vector3.forward);
        }

        /// <summary>M19: a golden numeral — punch it (PlayerCombat hit) and it flies back at the Count.</summary>
        public void FireGolden(Vector3 pos, Vector3 vel, CountMidnight boss)
        {
            var b = Get(Kind.Golden);
            b.t.position = new Vector3(pos.x, pos.y + bulletHeight + 0.3f, pos.z);
            b.vel = vel; b.life = 7f; b.spin = 200f; b.r = 0.55f; b.boss = boss;
            b.t.localScale = Vector3.one * 1.05f; b.t.rotation = Quaternion.identity;
        }

        bool Reflect(Bullet b, HitInfo h)
        {
            if (!b.alive || b.reflected || b.boss == null) return false;
            b.reflected = true; b.born = Time.time; b.life = 4f;
            Vector3 target = b.boss.transform.position + Vector3.up * 3.2f;
            b.vel = (target - b.t.position).normalized * 22f;
            VFX.Play(VFXKind.HitSpark, b.t.position, 1.4f, VFX.Gold);
            Presentation.ComicPop.Show(b.t.position, 1.4f, "POP");
            Audio.AudioManager.Play(Audio.Sfx.HitHeavy, b.t.position, 0.9f);
            return true;
        }

        public void ClearInRadius(Vector3 c, float radius)
        {
            foreach (var b in _pool)
                if (b.alive && b.kind != Kind.Golden && new Vector2(b.t.position.x - c.x, b.t.position.z - c.z).magnitude < radius)
                { b.alive = false; b.t.gameObject.SetActive(false); VFX.Play(VFXKind.Poof, b.t.position, 0.4f, VFX.Lavender); }
        }

        public void ClearAll()
        {
            foreach (var b in _pool) if (b.alive) { b.alive = false; b.t.gameObject.SetActive(false); }
            Alive = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime; int alive = 0;
            Vector3 pp = player != null ? player.position : new Vector3(9999, 0, 0);
            foreach (var b in _pool)
            {
                if (!b.alive) continue;
                if (Time.time - b.born > b.life) { b.alive = false; b.t.gameObject.SetActive(false); continue; }
                alive++;
                b.t.position += b.vel * dt;
                b.t.Rotate(0, b.spin * dt, 0, Space.World);
                if (b.reflected)
                {
                    if (b.boss != null && (b.boss.transform.position + Vector3.up * 3.2f - b.t.position).magnitude < 2.4f)
                    { GoldenReflected++; b.boss.OnReflectedHit(b.t.position); b.alive = false; b.t.gameObject.SetActive(false); }
                    continue;
                }
                // hit test against Pip's capsule (feet → 1.2 m)
                Vector3 d = pp - b.t.position; float flat = new Vector2(d.x, d.z).magnitude;
                bool vertical = b.kind == Kind.Pillar ? true : (pp.y - (b.t.position.y - bulletHeight)) < jumpClear;   // jumped over a low one?
                if (vertical && flat < b.r + 0.38f && _receiver != null)
                {
                    if (_receiver.TryHurt(b.kind == Kind.Pillar ? "a clock hand" : b.kind == Kind.Gear ? "a midnight gear" : "a flying numeral", b.t.position, 5f)) PlayerHits++;
                    if (b.kind != Kind.Pillar) { b.alive = false; b.t.gameObject.SetActive(false); VFX.Play(VFXKind.Poof, b.t.position, 0.5f); }
                }
            }
            Alive = alive;
        }

        // ================================================================== patterns (each returns when its last bullet is out)
        /// <summary>Rings of numerals expanding from <paramref name="c"/>; each ring has a 3-bullet gap that rotates ring to ring.</summary>
        public IEnumerator ClockRing(Vector3 c, int rings = 4, int count = 24, float speed = 6.5f, float interval = 0.85f)
        {
            float gapAt = Random.Range(0, count);
            for (int r = 0; r < rings; r++)
            {
                for (int i = 0; i < count; i++)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(i * 360f / count, gapAt * 360f / count)) < 360f / count * 1.6f) continue;
                    float a = (i / (float)count) * Mathf.PI * 2f + r * 0.13f;
                    Vector3 dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    Fire(Kind.Numeral, c + dir * 1.5f, dir * speed);
                }
                Audio.AudioManager.Play(Audio.Sfx.ChairStep, c, 0.9f);
                gapAt += count / 4f + Random.Range(-2f, 2f);
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>Two-armed gear spiral turning around the boss for <paramref name="seconds"/>.</summary>
        public IEnumerator GearSpiral(Vector3 c, float seconds = 3.6f, float rate = 0.09f, float turnDeg = 22f, float speed = 7.5f, int arms = 2)
        {
            float a = Random.Range(0f, 360f);
            for (float t = 0; t < seconds; t += rate)
            {
                for (int k = 0; k < arms; k++)
                {
                    float ang = (a + k * 360f / arms) * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(ang), 0, Mathf.Cos(ang));
                    Fire(Kind.Gear, c + dir * 1.8f, dir * speed);
                }
                a += turnDeg;
                yield return new WaitForSeconds(rate);
            }
        }

        /// <summary>Clock hands fall from the sky onto red circles (0.9 s warning); one always aims at Pip.</summary>
        public IEnumerator HandRain(Vector3 c, float radius, int count = 14, float warn = 0.9f, float spacing = 0.18f)
        {
            var marks = new List<(GameObject g, Vector3 p)>();
            for (int i = 0; i < count; i++)
            {
                Vector3 p = i == 0 && player != null ? new Vector3(player.position.x, c.y, player.position.z)
                    : c + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(2f, radius);
                var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(g.GetComponent<Collider>()); g.name = "HandMark";
                g.transform.position = p + Vector3.up * 0.04f; g.transform.localScale = new Vector3(2.4f, 0.01f, 2.4f);
                g.GetComponent<Renderer>().sharedMaterial = _markMat;
                marks.Add((g, p));
                StartCoroutine(Drop(g, p, warn));
                yield return new WaitForSeconds(spacing);
            }
            yield return new WaitForSeconds(warn + 0.3f);
        }

        IEnumerator Drop(GameObject mark, Vector3 p, float warn)
        {
            for (float t = 0; t < warn; t += Time.deltaTime)
            {
                mark.transform.localScale = new Vector3(2.4f, 0.01f, 2.4f) * (0.7f + 0.3f * Mathf.PingPong(t * 6f, 1f));
                yield return null;
            }
            Destroy(mark);
            var b = Get(Kind.Pillar);
            b.t.position = p + Vector3.up * 1.6f; b.t.localScale = new Vector3(0.5f, 3.2f, 0.5f); b.vel = Vector3.zero; b.life = 0.5f; b.r = 1.1f; b.spin = 0f;
            b.t.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            VFX.Play(VFXKind.ImpactRing, p, 0.9f, VFX.Danger);
            VFX.Play(VFXKind.DustPuff, p, 1.2f);
            CameraShake.Add(0.08f);
        }

        /// <summary>Undertale wall: a line of pillars sweeps across the arena with a moving gap you must stay in.</summary>
        public IEnumerator TickWall(Vector3 c, float radius, int walls = 3, float speed = 6f, float gapWidth = 3.2f)
        {
            for (int w = 0; w < walls; w++)
            {
                float yaw = Random.Range(0f, 360f);
                Quaternion q = Quaternion.Euler(0, yaw, 0);
                Vector3 fwd = q * Vector3.forward, right = q * Vector3.right;
                float gap = Random.Range(-radius * 0.5f, radius * 0.5f);
                for (float x = -radius; x <= radius; x += 0.9f)
                {
                    if (Mathf.Abs(x - gap) < gapWidth * 0.5f) continue;
                    Fire(Kind.Pillar, c - fwd * radius + right * x, fwd * speed, radius * 2f / speed + 0.2f);
                }
                Audio.AudioManager.Play(Audio.Sfx.ChairGasp, c, 0.6f);
                yield return new WaitForSeconds(radius * 2f / speed * 0.55f);
            }
        }
    }
}
