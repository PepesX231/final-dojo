using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.Boss
{
    /// <summary>
    /// World 1 boss skills (docs/LEVEL1_DESIGN.md "The boss"): make THE CHAIR as hard as possible to reach while
    /// staying fair — every skill has a readable telegraph (anticipation ≥ 0.6 s, red ground marks) and a counter.
    ///   P1 (fatigue &lt; 33%): Shockwave Stomp — hop + slam, an expanding ring you jump over.
    ///   P2 (&lt; 66%):       + Cushion Volley — 3 cushions land on red circles (1 s warning).
    ///                       + Musical Chairs — 4 decoys shuffle with the real chair; the real one has the gingham cushion.
    ///   P3 (≥ 66%):       everything faster, volley of 5.
    /// Phase only goes up (hysteresis). Skills run only while the chair is fleeing / hesitating, never during an
    /// opening (exhausted, stumble, rooted...) so the sit window is never stolen. Added only by the World 1 builder;
    /// the P0 Dream_Chair prototype (and its tests) do not use it.
    /// </summary>
    public class ChairSkills : MonoBehaviour
    {
        public BossChairController chair;
        public BossEncounter encounter;
        public float stompCooldown = 7f, volleyCooldown = 9f, chairsCooldown = 18f;
        public float firstSkillDelay = 4f;
        public float ringSpeed = 8.5f, ringMaxRadius = 15f, ringBand = 0.75f, ringJumpClear = 0.55f;
        public bool AutoRun = true;

        public int Phase { get; private set; } = 1;
        public string LastSkill { get; private set; }
        public int SkillsUsed { get; private set; }
        public int PlayerHits { get; private set; }
        public bool Busy { get; private set; }

        PlayerMotor3D _player;
        Dream.SleepStability _stability;
        float _tStomp, _tVolley, _tChairs, _fightTime;
        float _hurtCooldown;
        readonly List<GameObject> _decoys = new List<GameObject>();
        static Material _telegraphMat, _ringMat, _cushionMat;

        void Start()
        {
            if (chair == null) chair = GetComponent<BossChairController>();
            if (encounter == null) encounter = FindFirstObjectByType<BossEncounter>();
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _stability = FindFirstObjectByType<Dream.SleepStability>();
            _tStomp = firstSkillDelay; _tVolley = firstSkillDelay + 5f; _tChairs = firstSkillDelay + 9f;
        }

        void Update()
        {
            _hurtCooldown -= Time.deltaTime;          // ticks even when skills are driven manually
            if (!AutoRun || chair == null || _player == null) return;
            if (encounter != null && encounter.CurrentPhase != BossEncounter.Phase.Fight) { CleanupDecoys(); return; }
            float dt = Time.deltaTime;
            _fightTime += dt;
            float f = chair.Fatigue != null ? chair.Fatigue.Normalized : 0f;
            int want = f < 0.33f ? 1 : f < 0.66f ? 2 : 3;
            if (want > Phase) { Phase = want; UI.UIRoot.Get().Toast(Phase == 2 ? "THE CHAIR IS GETTING CRANKY!" : "THE CHAIR GOES ALL OUT!", 2f); CameraShake.Add(0.4f); }
            float speedUp = Phase >= 3 ? 1.5f : 1f;
            _tStomp -= dt * speedUp; _tVolley -= dt * speedUp; _tChairs -= dt * speedUp;
            if (Busy || !CanAct()) return;

            float dist = Flat(_player.transform.position - chair.transform.position).magnitude;
            if (Phase >= 2 && _tChairs <= 0f && dist < 14f) { _tChairs = chairsCooldown; StartCoroutine(MusicalChairs()); return; }
            if (Phase >= 2 && _tVolley <= 0f && dist > 3f) { _tVolley = volleyCooldown; StartCoroutine(Volley(Phase >= 3 ? 5 : 3)); return; }
            if (_tStomp <= 0f && dist < 11f && dist > 1.8f) { _tStomp = stompCooldown; StartCoroutine(Stomp()); }
        }

        bool CanAct()
        {
            var s = chair.State;
            return s == ChairState.Flee || s == ChairState.Panic || s == ChairState.Hesitate || s == ChairState.Alert;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // ================================================================== Shockwave Stomp
        public IEnumerator Stomp()
        {
            Busy = true; LastSkill = "Stomp"; SkillsUsed++;
            chair.SkillBusyUntil = Time.time + 1.4f;
            Vector3 c = chair.transform.position;
            var tele = Disc("StompTelegraph", c, 2.2f, TelegraphMat, new Color(1f, 0.25f, 0.3f, 0.9f));
            Audio.AudioManager.Play(Audio.Sfx.ChairGasp, c, 0.6f);
            var vis = chair.transform.Find("Visual");
            float wind = 0.65f;
            for (float t = 0; t < wind; t += Time.deltaTime)
            {
                float k = t / wind;
                if (vis != null) vis.localPosition = new Vector3(0, Mathf.Sin(k * Mathf.PI) * 0.9f, 0);           // hop
                if (vis != null) vis.localScale = new Vector3(1f + 0.15f * k, 1f - 0.12f * k, 1f + 0.15f * k);     // arch
                tele.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.2f, k) * 2f;
                yield return null;
            }
            if (vis != null) { vis.localPosition = Vector3.zero; vis.localScale = Vector3.one; }
            Destroy(tele);
            // SLAM
            VFX.Play(VFXKind.ImpactRing, c, 1.4f);
            VFX.Play(VFXKind.Poof, c + Vector3.up * 0.3f, 1.4f);
            CameraShake.Add(0.55f);
            Audio.AudioManager.Play(Audio.Sfx.Land, c, 1f);
            var ring = Disc("Shockwave", c + Vector3.up * 0.05f, 1f, RingMat, new Color(1f, 0.35f, 0.4f, 1f));
            float r = 0.5f;
            while (r < ringMaxRadius)
            {
                r += ringSpeed * Time.deltaTime;
                ring.transform.localScale = new Vector3(r * 2f, 1f, r * 2f);
                var rend = ring.GetComponentInChildren<Renderer>();
                var col = rend.material.color; col.a = Mathf.Clamp01(1.2f - r / ringMaxRadius); rend.material.color = col;
                Vector3 d = _player.transform.position - c;
                float h = d.y; float pd = Flat(d).magnitude;
                if (Mathf.Abs(pd - r) < ringBand && h < ringJumpClear) Hurt(Flat(d).normalized, "shockwave");
                yield return null;
            }
            Destroy(ring);
            Busy = false;
        }

        // ================================================================== Cushion Volley
        public IEnumerator Volley(int count)
        {
            Busy = true; LastSkill = "Volley"; SkillsUsed++;
            chair.SkillBusyUntil = Time.time + 0.8f;
            Audio.AudioManager.Play(Audio.Sfx.ChairYelp, chair.transform.position, 0.7f);
            var targets = new List<Vector3>();
            Vector3 lead = _player.Velocity; lead.y = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = _player.transform.position + lead * 0.8f + (i == 0 ? Vector3.zero : Quaternion.Euler(0, i * 360f / count, 0) * Vector3.forward * Random.Range(1.8f, 3.2f));
                if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
                targets.Add(p);
            }
            var marks = new List<GameObject>();
            foreach (var p in targets) marks.Add(Disc("VolleyMark", p + Vector3.up * 0.04f, 3.2f, TelegraphMat, new Color(1f, 0.25f, 0.3f, 0.85f)));
            var balls = new List<Transform>();
            foreach (var p in targets)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(b.GetComponent<Collider>());
                b.name = "VolleyCushion";
                b.transform.localScale = new Vector3(1.0f, 0.55f, 1.0f);
                b.GetComponent<Renderer>().sharedMaterial = CushionMat;
                balls.Add(b.transform);
            }
            Vector3 from = chair.transform.position + Vector3.up * 1.6f;
            float flight = 1.0f;
            for (float t = 0; t < flight; t += Time.deltaTime)
            {
                float k = t / flight;
                for (int i = 0; i < balls.Count; i++)
                {
                    Vector3 pos = Vector3.Lerp(from, targets[i], k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 6f;
                    balls[i].position = pos;
                    balls[i].Rotate(0, 0, 400f * Time.deltaTime);
                    marks[i].transform.localScale = Vector3.one * 3.2f * (0.6f + 0.4f * Mathf.PingPong(t * 6f, 1f));
                }
                yield return null;
            }
            for (int i = 0; i < balls.Count; i++)
            {
                VFX.Play(VFXKind.DustPuff, targets[i], 1.6f);
                VFX.Play(VFXKind.LandingRing, targets[i], 1.4f);
                Vector3 d = _player.transform.position - targets[i];
                if (Flat(d).magnitude < 1.6f && d.y < 1.0f) Hurt(Flat(d).sqrMagnitude > 0.01f ? Flat(d).normalized : Vector3.forward, "cushion volley");
                Destroy(marks[i]);
                Destroy(balls[i].gameObject, 0.15f);
            }
            Audio.AudioManager.Play(Audio.Sfx.Land, targets[0], 0.8f);
            CameraShake.Add(0.25f);
            Busy = false;
        }

        // ================================================================== Musical Chairs
        public IEnumerator MusicalChairs()
        {
            Busy = true; LastSkill = "MusicalChairs"; SkillsUsed++;
            Vector3 center = chair.transform.position;
            const int n = 5; const float radius = 3.2f, shuffle = 4f;
            chair.SkillBusyUntil = Time.time + shuffle + 0.6f;
            UI.UIRoot.Get().Toast("MUSICAL CHAIRS! — ตัวจริงมีเบาะลายสก๊อต", 2.2f);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, center, 0.8f);
            var vis = chair.transform.Find("Visual");
            int realSlot = Random.Range(0, n);
            for (int i = 0; i < n - 1; i++)
            {
                var d = BuildDecoy(vis);
                if (d != null) _decoys.Add(d);
            }
            VFX.Play(VFXKind.Poof, center + Vector3.up * 0.6f, 2.4f);
            float ang = 0f;
            for (float t = 0; t < shuffle; t += Time.deltaTime)
            {
                float spin = Mathf.Lerp(40f, 160f, Mathf.Sin(t / shuffle * Mathf.PI));
                ang += spin * Time.deltaTime;
                int di = 0;
                for (int s = 0; s < n; s++)
                {
                    Vector3 p = center + Quaternion.Euler(0, ang + s * 360f / n, 0) * Vector3.forward * radius;
                    if (NavMesh.SamplePosition(p, out var hit, 1.5f, NavMesh.AllAreas)) p = hit.position;
                    Vector3 hop = Vector3.up * Mathf.Abs(Mathf.Sin(t * 9f + s)) * 0.25f;
                    if (s == realSlot) { chair.transform.position = p; chair.transform.rotation = Quaternion.LookRotation(Flat(p - center).normalized + Vector3.forward * 0.001f); }
                    else if (di < _decoys.Count && _decoys[di] != null)
                    {
                        _decoys[di].transform.position = p + hop;
                        _decoys[di].transform.rotation = Quaternion.LookRotation(Flat(p - center).normalized + Vector3.forward * 0.001f);
                        di++;
                    }
                }
                DecoyBumps();
                yield return null;
            }
            // decoys scatter and poof after a while
            var dirs = new List<Vector3>();
            foreach (var d in _decoys) dirs.Add(d != null ? Flat(d.transform.position - center).normalized : Vector3.forward);
            for (float t = 0; t < 2.5f; t += Time.deltaTime)
            {
                for (int i = 0; i < _decoys.Count; i++) if (_decoys[i] != null) _decoys[i].transform.position += dirs[i] * 4f * Time.deltaTime;
                DecoyBumps();
                yield return null;
            }
            CleanupDecoys();
            Busy = false;
        }

        void DecoyBumps()
        {
            for (int i = 0; i < _decoys.Count; i++)
            {
                var d = _decoys[i];
                if (d == null) continue;
                Vector3 to = _player.transform.position - d.transform.position;
                if (Flat(to).magnitude < 0.95f && to.y < 1.2f)
                {
                    // wrong chair! it poofs and bonks you away (no moon lost — it's a trick, not an attack)
                    VFX.Play(VFXKind.Poof, d.transform.position + Vector3.up * 0.6f, 1.6f);
                    Audio.AudioManager.Play(Audio.Sfx.CardFail, d.transform.position, 0.8f);
                    _player.Launch(Flat(to).normalized * 6f + Vector3.up * 4.5f);
                    UI.UIRoot.Get().Toast("ตัวปลอม! (POOF)", 1.2f);
                    Destroy(d); _decoys[i] = null;
                }
            }
        }

        GameObject BuildDecoy(Transform vis)
        {
            if (vis == null) return null;
            var root = new GameObject("DecoyChair");
            foreach (var mr in vis.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var go = new GameObject(mr.name);
                go.transform.SetParent(root.transform, false);
                // copy the pose relative to the chair root
                go.transform.localPosition = chair.transform.InverseTransformPoint(mr.transform.position);
                go.transform.localRotation = Quaternion.Inverse(chair.transform.rotation) * mr.transform.rotation;
                go.transform.localScale = mr.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = go.AddComponent<MeshRenderer>();
                var mats = mr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && (mats[i].name.Contains("Cushion") || mats[i].name.Contains("Gingham"))) mats[i] = CushionMat;   // decoys: plain seat
                r.sharedMaterials = mats;
            }
            return root;
        }

        void CleanupDecoys()
        {
            foreach (var d in _decoys) if (d != null) { VFX.Play(VFXKind.Poof, d.transform.position + Vector3.up * 0.6f, 1.2f); Destroy(d); }
            _decoys.Clear();
        }

        void OnDisable() => CleanupDecoys();

        // ================================================================== shared
        public void Hurt(Vector3 away, string reason)
        {
            if (_hurtCooldown > 0f) return;
            _hurtCooldown = 1.3f;
            PlayerHits++;
            _player.Launch(away * 7f + Vector3.up * 6f);
            if (_stability != null) _stability.Damage(reason);
            var fx = FXLibrary.Instance;
            if (fx != null) WorldFX.Spawn(fx.ouch, _player.transform.position + Vector3.up * 1.6f, 1f, 0.8f, _player.transform);
            VFX.Play(VFXKind.StarBurst, _player.transform.position + Vector3.up, 1f, VFX.Danger);
            Audio.AudioManager.Play(Audio.Sfx.Kick, _player.transform.position, 0.9f);
            CameraShake.Add(0.4f);
            _player.GetComponentInChildren<AppleFace>()?.Squeeze(0.7f);
        }

        internal static GameObject Disc(string name, Vector3 at, float size, Material m, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = name;
            var holder = new GameObject(name);
            go.transform.SetParent(holder.transform, false);
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            holder.transform.position = at + Vector3.up * 0.03f;
            holder.transform.localScale = Vector3.one * size;
            var r = go.GetComponent<Renderer>();
            r.material = m; r.material.color = c;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return holder;
        }

        internal static Material TelegraphMat
        {
            get
            {
                if (_telegraphMat == null)
                {
                    var lib = VFXLibrary.Instance;
                    _telegraphMat = lib != null && lib.telegraph != null ? new Material(lib.telegraph) : Core.RuntimeMaterials.CreateUnlitTransparent(new Color(1, 0.2f, 0.3f, 0.6f), null, "M_Telegraph");
                }
                return _telegraphMat;
            }
        }

        static Material RingMat
        {
            get
            {
                if (_ringMat == null)
                {
                    var lib = VFXLibrary.Instance;
                    _ringMat = lib != null && lib.ring != null ? new Material(lib.ring) : Core.RuntimeMaterials.CreateUnlitTransparent(new Color(1, 0.3f, 0.35f, 0.8f), null, "M_Shock");
                }
                return _ringMat;
            }
        }

        internal static Material CushionMat => _cushionMat != null ? _cushionMat : (_cushionMat = Look.DreamLook.Toon(new Color(0.66f, 0.55f, 0.95f), null, 0.25f, name: "M_DecoyCushion"));
    }
}
