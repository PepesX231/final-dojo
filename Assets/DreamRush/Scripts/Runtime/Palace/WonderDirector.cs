using System.Collections;
using System.Collections.Generic;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// The open-world rule from the brief: "every 10 seconds the player meets something new, beautiful and inviting".
    /// Everything novel reports to <see cref="Novelty"/> (district cards, rails, rings, pads, shards, fights, set pieces).
    /// Authored <see cref="WonderEvent"/>s fire when Pip comes near; if nothing happened for <see cref="maxQuiet"/> seconds
    /// the director stages an ambient set piece in front of the camera (paper-plane flock, sky whale, fireworks,
    /// shooting stars, calling-card rain, a passing sky tram, a Cyberpunk time-skip glitch, the moon's wink…).
    /// <see cref="MaxGap"/> is measured for the playtest bot (test: ≤ 10 s).
    /// </summary>
    public class WonderDirector : MonoBehaviour
    {
        public static WonderDirector Current { get; private set; }
        public float maxQuiet = 7.5f;
        [Header("Templates (disabled scene objects)")]
        public GameObject planeTemplate, whaleTemplate, tramTemplate, cardTemplate, minionTemplate;
        public Transform moon;
        public List<WonderEvent> events = new List<WonderEvent>();

        public float LastNovelty { get; private set; }
        public float MaxGap { get; private set; }
        public int Count { get; private set; }
        public string LastName { get; private set; }
        public bool Measure = true;
        readonly List<string> _log = new List<string>();
        Transform _player; Camera _cam;
        string _lastAmbient;

        static readonly string[] Ambient = { "PaperPlanes", "SkyWhale", "Fireworks", "ShootingStars", "CallingCards", "TramPass", "TimeSkip", "MoonWink" };

        void Awake() { Current = this; LastNovelty = Time.time; }
        void OnDestroy() { if (Current == this) Current = null; }

        void Start()
        {
            var m = FindFirstObjectByType<Player.PlayerMotor3D>();
            _player = m != null ? m.transform : null;
            _cam = Camera.main;
            foreach (var t in new[] { planeTemplate, whaleTemplate, tramTemplate, cardTemplate, minionTemplate }) if (t != null) t.SetActive(false);
        }

        public static void Novelty(string what)
        {
            var d = Current; if (d == null) return;
            float gap = Time.time - d.LastNovelty;
            if (d.Measure && gap > d.MaxGap) d.MaxGap = gap;
            d.LastNovelty = Time.time; d.Count++; d.LastName = what;
            if (d._log.Count > 60) d._log.RemoveAt(0);
            d._log.Add($"{Time.time:F1}s {what}");
        }

        public string Log => string.Join(" | ", _log);
        public void ResetMeasure() { MaxGap = 0f; LastNovelty = Time.time; Count = 0; _log.Clear(); }

        void Update()
        {
            if (_player == null) return;
            if (_cam == null) _cam = Camera.main;
            foreach (var e in events)
                if (e != null && !e.Fired && Vector3.Distance(e.transform.position, _player.position) < e.radius) { e.Fired = true; Play(e.kind, e.transform); }
            if (Time.time - LastNovelty > maxQuiet)
            {
                string k; int guard = 0;
                do { k = Ambient[Random.Range(0, Ambient.Length)]; } while (k == _lastAmbient && guard++ < 5);
                _lastAmbient = k;
                Play(k, null);
            }
        }

        /// <summary>Stage a set piece (at <paramref name="at"/> or in front of the camera).</summary>
        public void Play(string kind, Transform at)
        {
            Novelty(kind);
            Vector3 fwd = _cam != null ? Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 origin = at != null ? at.position : _player.position + fwd * 30f;
            switch (kind)
            {
                case "PaperPlanes": StartCoroutine(Planes(_player.position + fwd * 6f + Vector3.up * 5f, right)); break;
                case "SkyWhale": StartCoroutine(Whale(_player.position + fwd * 70f + Vector3.up * 38f, right)); break;
                case "Fireworks": StartCoroutine(Fireworks(_player.position + fwd * 45f + Vector3.up * 30f, right)); break;
                case "ShootingStars": StartCoroutine(Stars(_player.position + fwd * 80f + Vector3.up * 55f, right)); break;
                case "CallingCards": StartCoroutine(Cards(_player.position + fwd * 5f + Vector3.up * 9f)); break;
                case "TramPass": StartCoroutine(Tram(_player.position + fwd * 26f + Vector3.up * 16f, right)); break;
                case "TimeSkip": StartCoroutine(TimeSkip()); break;
                case "MoonWink": StartCoroutine(MoonWink()); break;
                case "Ambush": if (at != null) StartCoroutine(Ambush(at)); break;
                case "NeonWave": if (at != null) StartCoroutine(NeonWave(at)); break;
                default: break;
            }
        }

        // ================================================================== set pieces
        GameObject Spawn(GameObject template, Vector3 p, Quaternion r)
        {
            if (template == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetPositionAndRotation(p, r); return g; }
            var go = Instantiate(template, p, r); go.SetActive(true); return go;
        }

        IEnumerator Planes(Vector3 c, Vector3 right)
        {
            var list = new List<(Transform t, Vector3 off, float ph)>();
            for (int i = 0; i < 12; i++)
            {
                var off = new Vector3(Random.Range(-4f, 4f), Random.Range(-1.5f, 2.5f), Random.Range(-4f, 4f));
                var g = Spawn(planeTemplate, c - right * 40f + off, Quaternion.LookRotation(right));
                g.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f);
                list.Add((g.transform, off, Random.Range(0f, 6f)));
            }
            Audio.AudioManager.Play(Audio.Sfx.Whoosh, c, 0.8f);
            for (float t = 0; t < 5.5f; t += Time.deltaTime)
            {
                foreach (var (tr, off, ph) in list)
                {
                    Vector3 p = c - right * 40f + right * (t * 15f) + off + Vector3.up * Mathf.Sin(t * 2.4f + ph) * 1.2f;
                    tr.rotation = Quaternion.LookRotation(p - tr.position + right * 0.01f) * Quaternion.Euler(0, 0, Mathf.Sin(t * 3f + ph) * 25f);
                    tr.position = p;
                }
                yield return null;
            }
            foreach (var (tr, _, _) in list) Destroy(tr.gameObject);
        }

        IEnumerator Whale(Vector3 c, Vector3 right)
        {
            var g = Spawn(whaleTemplate, c - right * 120f, Quaternion.LookRotation(-right));   // model faces -Y(Blender) = +Z: its nose leads along +Z
            g.transform.rotation = Quaternion.LookRotation(right) * Quaternion.Euler(0, 180, 0);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, c, 1f);
            for (float t = 0; t < 14f; t += Time.deltaTime)
            {
                g.transform.position = c - right * 120f + right * (t * 17f) + Vector3.up * Mathf.Sin(t * 0.7f) * 3f;
                if (Mathf.Repeat(t, 1.2f) < Time.deltaTime) VFX.Play(VFXKind.Zzz, g.transform.position + Vector3.up * 6f, 3f);
                yield return null;
            }
            Destroy(g);
        }

        IEnumerator Fireworks(Vector3 c, Vector3 right)
        {
            Color[] cols = { new Color(1f, 0.25f, 0.6f), new Color(0.2f, 0.95f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.6f, 0.4f, 1f) };
            for (int i = 0; i < 7; i++)
            {
                Vector3 p = c + right * Random.Range(-25f, 25f) + Vector3.up * Random.Range(-6f, 10f);
                var col = cols[i % cols.Length];
                VFX.Play(VFXKind.StarBurst, p, 4f, col);
                VFX.Play(VFXKind.Confetti, p, 3f);
                VFX.Play(VFXKind.LandingRing, p, 6f, col);
                LightFlash(p, col);
                Audio.AudioManager.Play(Audio.Sfx.Land, p, 0.6f);
                yield return new WaitForSeconds(Random.Range(0.25f, 0.55f));
            }
        }

        void LightFlash(Vector3 p, Color c)
        {
            var l = new GameObject("FireworkLight").AddComponent<Light>();
            l.type = LightType.Point; l.range = 60f; l.intensity = 8f; l.color = c; l.transform.position = p;
            StartCoroutine(FadeLight(l, 0.6f));
        }

        IEnumerator FadeLight(Light l, float s)
        {
            float i0 = l.intensity;
            for (float t = 0; t < s; t += Time.deltaTime) { l.intensity = Mathf.Lerp(i0, 0, t / s); yield return null; }
            Destroy(l.gameObject);
        }

        IEnumerator Stars(Vector3 c, Vector3 right)
        {
            var lib = VFXLibrary.Instance;
            for (int i = 0; i < 6; i++)
            {
                var g = new GameObject("ShootingStar");
                var tr = g.AddComponent<TrailRenderer>();
                tr.time = 0.6f; tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.9f), new Keyframe(1, 0));
                tr.sharedMaterial = lib != null ? lib.glowAdd : null;
                var grad = new Gradient(); grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.6f, 0.9f, 1f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
                tr.colorGradient = grad;
                Vector3 a = c + right * Random.Range(-60f, 20f) + Vector3.up * Random.Range(0f, 20f);
                Vector3 dir = (right + Vector3.down * 0.45f).normalized;
                g.transform.position = a;
                StartCoroutine(Fly(g.transform, dir * 70f, 1.2f));
                yield return new WaitForSeconds(Random.Range(0.15f, 0.5f));
            }
        }

        IEnumerator Fly(Transform t, Vector3 v, float s)
        {
            for (float k = 0; k < s; k += Time.deltaTime) { if (t == null) yield break; t.position += v * Time.deltaTime; yield return null; }
            if (t != null) Destroy(t.gameObject, 0.7f);
        }

        IEnumerator Cards(Vector3 c)
        {
            var list = new List<(Transform t, Vector3 v, Vector3 spin)>();
            for (int i = 0; i < 22; i++)
            {
                var g = Spawn(cardTemplate, c + new Vector3(Random.Range(-7f, 7f), Random.Range(0f, 4f), Random.Range(-7f, 7f)), Random.rotation);
                list.Add((g.transform, new Vector3(Random.Range(-0.6f, 0.6f), -Random.Range(1.2f, 2.2f), Random.Range(-0.6f, 0.6f)), Random.insideUnitSphere * 220f));
            }
            UI.UIRoot.Get().Toast("CALLING CARD: \"WE WILL STEAL YOUR SLEEP\" — COUNT MIDNIGHT", 2.5f);
            for (float t = 0; t < 6f; t += Time.deltaTime)
            {
                foreach (var (tr, v, sp) in list) { tr.position += (v + new Vector3(Mathf.Sin(t * 3f + tr.GetInstanceID()) * 0.8f, 0, 0)) * Time.deltaTime; tr.Rotate(sp * Time.deltaTime); }
                yield return null;
            }
            foreach (var (tr, _, _) in list) Destroy(tr.gameObject);
        }

        IEnumerator Tram(Vector3 c, Vector3 right)
        {
            var g = Spawn(tramTemplate, c - right * 90f, Quaternion.LookRotation(right) * Quaternion.Euler(0, 180, 0));
            Audio.AudioManager.Play(Audio.Sfx.ChairYelp, c, 0.7f);
            for (float t = 0; t < 8f; t += Time.deltaTime)
            {
                g.transform.position = c - right * 90f + right * (t * 24f);
                if (Mathf.Repeat(t, 0.15f) < Time.deltaTime) VFX.Play(VFXKind.StarBurst, g.transform.position + Vector3.up * 3.9f, 0.4f, VFX.Gold);
                yield return null;
            }
            Destroy(g);
        }

        IEnumerator TimeSkip()
        {
            var sp = Look.StylePack.Current;
            UI.UIRoot.Get().Toast("— TIME SKIP —  นาฬิกาทั้งเมืองหมุนเร็วผิดปกติ", 2f);
            Audio.AudioManager.Play(Audio.Sfx.Clank, Vector3.zero, 0.8f);
            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                if (sp != null) sp.Glitch = Mathf.Sin(t / 1.4f * Mathf.PI) * (0.6f + Mathf.PingPong(t * 8f, 0.4f));
                ClockSpin.Speed = 1f + 40f * Mathf.Sin(t / 1.4f * Mathf.PI);
                yield return null;
            }
            if (sp != null) sp.Glitch = 0f;
            ClockSpin.Speed = 1f;
        }

        IEnumerator MoonWink()
        {
            if (moon == null) yield break;
            Vector3 s0 = moon.localScale;
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, moon.position, 1f);
            for (float t = 0; t < 1.6f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / 1.6f * Mathf.PI);
                moon.localScale = new Vector3(s0.x * (1 + k * 0.25f), s0.y * (1 - k * 0.9f * Mathf.Clamp01(Mathf.Sin(t * 6f))), s0.z);
                yield return null;
            }
            moon.localScale = s0;
        }

        IEnumerator NeonWave(Transform at)
        {
            var signs = at.GetComponentsInChildren<Renderer>();
            foreach (var r in signs) r.enabled = false;
            Audio.AudioManager.Play(Audio.Sfx.UiOpen, at.position, 0.8f);
            foreach (var r in signs) { r.enabled = true; VFX.Play(VFXKind.StarBurst, r.bounds.center, 0.6f, VFX.Lavender); yield return new WaitForSeconds(0.12f); }
        }

        IEnumerator Ambush(Transform at)
        {
            UI.BossIntroCard.Show("AMBUSH!", "— TICK-TOCK SHADOWS —", chapterCard: true);
            CameraShake.Add(0.5f);
            int n = at.GetComponent<WonderEvent>() != null ? at.GetComponent<WonderEvent>().count : 3;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = at.position + Quaternion.Euler(0, i * 360f / n, 0) * Vector3.forward * 5f;
                var g = Spawn(minionTemplate, p + Vector3.up * 8f, Quaternion.identity);
                var mn = g.GetComponent<TickMinion>(); if (mn != null) mn.DropIn(p);
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}
