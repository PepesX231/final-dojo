using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// THE ALARM — a giant nightmare alarm clock that hunts Pip across Dreamland (docs/OPENWORLD_DESIGN.md).
    /// Roam → Notice (0.8 s wind-up) → Chase (9.5 m/s, faster than sprint: dash, glide and terrain to escape) →
    /// Slam within 7 m (telegraphed shockwave, jump it) → loses track when far / out of sight 4 s / you reach a
    /// pillow-fire safe zone. A ground pound next to it stuns it for 3 s. Contact: −1 Sleep moon + big knockback.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NightmareAlarm : MonoBehaviour
    {
        public enum State { Roam, Notice, Chase, Slam, Search, Stunned }
        public List<Transform> roamPoints = new List<Transform>();
        public Transform visual;
        public Light eyeLight;
        public float sightRange = 34f, sightAngle = 110f, loseRange = 48f, loseSightSeconds = 4f;
        public float roamSpeed = 3.2f, chaseSpeed = 9.5f;
        public float slamRange = 7f, slamCooldown = 4f, hitRadius = 2.6f;

        public State Current { get; private set; } = State.Roam;
        public float StateTime { get; private set; }
        public int Hits { get; private set; }
        public static NightmareAlarm Active { get; private set; }
        public bool IsHunting => Current == State.Notice || Current == State.Chase || Current == State.Slam;

        NavMeshAgent _agent;
        PlayerMotor3D _player;
        Dream.SleepStability _stab;
        float _lostTime, _slamCd, _hitCd, _tick;
        int _roam;
        static Material _ringMat;

        void Awake() { _agent = GetComponent<NavMeshAgent>(); Active = this; }
        void OnDestroy() { if (Active == this) Active = null; PlayerAbilities.AnyPound -= OnPound; }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _stab = FindFirstObjectByType<Dream.SleepStability>();
            _agent.speed = roamSpeed; _agent.acceleration = 22f; _agent.angularSpeed = 300f;
            PlayerAbilities.AnyPound += OnPound;
            NextRoam();
        }

        void OnPound(Vector3 at, float radius)
        {
            if (Vector3.Distance(at, transform.position) < radius + 2f && Current != State.Stunned) Stun(3f);
        }

        public void Stun(float seconds)
        {
            Go(State.Stunned);
            _stunFor = seconds;
            var fx = FXLibrary.Instance;
            if (fx != null) WorldFX.Spawn(fx.dizzy, transform.position + Vector3.up * 6f, 2.4f, seconds, transform);
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 4f, 2f, VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.ChairStumble, transform.position, 1f);
            UIRoot.Get().Toast("THE ALARM มึน! รีบหนี!", 1.5f);
        }
        float _stunFor;

        void Go(State s)
        {
            if (Current == s) { StateTime = 0f; return; }
            var prev = Current;
            Current = s; StateTime = 0f;
            if (_agent.isOnNavMesh) _agent.isStopped = s == State.Notice || s == State.Slam || s == State.Stunned;
            _agent.speed = s == State.Chase ? chaseSpeed : roamSpeed;
            if (s == State.Notice)
            {
                Audio.AudioManager.Play(Audio.Sfx.ChairGasp, transform.position, 1f);
                var fx = FXLibrary.Instance;
                if (fx != null) WorldFX.Spawn(fx.exclaim, transform.position + Vector3.up * 6.5f, 2.2f, 0.9f, transform);
                UIRoot.Get().Toast("⚠ THE ALARM เห็นคุณแล้ว — หนี!!", 1.6f);
                Audio.AudioManager.PlayMusic(Audio.Music.Boss, 0.4f);
            }
            if ((prev == State.Chase || prev == State.Slam) && (s == State.Search || s == State.Roam))
            {
                UIRoot.Get().Toast("หนีพ้นแล้ว… (THE ALARM lost you)", 1.6f);
                Audio.AudioManager.PlayMusic(Audio.Music.Dream, 1.2f);
            }
        }

        bool CanSee()
        {
            Vector3 eye = transform.position + Vector3.up * 3.3f;
            Vector3 p = _player.transform.position + Vector3.up * 0.6f;
            Vector3 d = p - eye;
            if (d.magnitude > sightRange) return false;
            if (Vector3.Angle(transform.forward, new Vector3(d.x, 0, d.z)) > sightAngle * 0.5f && d.magnitude > 9f) return false;
            if (SafeZone.Contains(_player.transform.position)) return false;
            return !Physics.Raycast(eye, d.normalized, d.magnitude - 0.8f, ~0, QueryTriggerInteraction.Ignore) || d.magnitude < 6f;
        }

        void Update()
        {
            if (_player == null) return;
            float dt = Time.deltaTime;
            StateTime += dt; _slamCd -= dt; _hitCd -= dt;
            Vector3 to = _player.transform.position - transform.position; to.y = 0;
            float dist = to.magnitude;
            bool safe = SafeZone.Contains(_player.transform.position);
            switch (Current)
            {
                case State.Roam:
                    if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 2f) NextRoam();
                    if (CanSee()) Go(State.Notice);
                    break;
                case State.Notice:
                    Face(to, 400f);
                    if (StateTime > 0.8f) Go(State.Chase);
                    break;
                case State.Chase:
                    if (_agent.isOnNavMesh) _agent.SetDestination(_player.transform.position);
                    bool see = CanSee();
                    _lostTime = see ? 0f : _lostTime + dt;
                    if (safe || dist > loseRange || _lostTime > loseSightSeconds) { Go(State.Search); break; }
                    if (dist < slamRange && _slamCd <= 0f && _player.IsGrounded) StartCoroutine(Slam());
                    break;
                case State.Search:
                    if (StateTime > 3f) { NextRoam(); Go(State.Roam); }
                    else if (!safe && CanSee() && StateTime > 1f) Go(State.Notice);
                    break;
                case State.Stunned:
                    if (StateTime > _stunFor) Go(State.Search);
                    break;
            }
            // contact damage (the body), never inside a safe zone
            if (!safe && dist < hitRadius && Mathf.Abs(_player.transform.position.y - transform.position.y) < 4.5f && Current != State.Stunned) Hurt(to.normalized, "caught by THE ALARM");
            Animate(dt);
        }

        IEnumerator Slam()
        {
            Go(State.Slam);
            _slamCd = slamCooldown;
            Vector3 c = transform.position;
            var tele = Disc(c, 3f, new Color(1f, 0.2f, 0.3f, 0.85f));
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                if (visual != null) { visual.localScale = new Vector3(1.1f, 0.85f, 1.1f); visual.localPosition = Vector3.zero; }
                tele.transform.localScale = Vector3.one * Mathf.Lerp(3f, 9f, t / 0.6f);
                yield return null;
            }
            for (float t = 0; t < 0.35f; t += Time.deltaTime)
            {
                if (visual != null) { visual.localScale = Vector3.one; visual.localPosition = Vector3.up * Mathf.Sin(t / 0.35f * Mathf.PI) * 3f; }
                yield return null;
            }
            if (visual != null) visual.localPosition = Vector3.zero;
            Destroy(tele);
            VFX.Play(VFXKind.ImpactRing, c, 2.2f, VFX.Danger);
            VFX.Play(VFXKind.Poof, c + Vector3.up, 3f);
            CameraShake.Add(0.8f);
            Audio.AudioManager.Play(Audio.Sfx.Land, c, 1f);
            var ring = Disc(c + Vector3.up * 0.05f, 1f, new Color(1f, 0.35f, 0.45f, 1f), ring: true);
            float r = 1f;
            while (r < 16f)
            {
                r += 13f * Time.deltaTime;
                ring.transform.localScale = new Vector3(r * 2f, 1f, r * 2f);
                Vector3 d = _player.transform.position - c;
                float h = d.y; d.y = 0;
                if (Mathf.Abs(d.magnitude - r) < 0.9f && h < 0.6f && _player.IsGrounded) Hurt(d.normalized, "alarm shockwave");
                yield return null;
            }
            Destroy(ring);
            if (Current == State.Slam) Go(State.Chase);
        }

        void Hurt(Vector3 away, string reason)
        {
            if (_hitCd > 0f) return;
            _hitCd = 1.6f; Hits++;
            _player.Launch(away * 11f + Vector3.up * 9f);
            if (_stab != null) _stab.Damage(reason);
            VFX.Play(VFXKind.StarBurst, _player.transform.position + Vector3.up, 1.4f, VFX.Danger);
            Audio.AudioManager.Play(Audio.Sfx.Kick, _player.transform.position, 1f);
            CameraShake.Add(0.6f);
            UIRoot.Get().ScreenFlash(new Color(1f, 0.2f, 0.3f, 0.45f), 0.3f);
            _player.GetComponentInChildren<AppleFace>()?.Squeeze(0.8f);
        }

        void NextRoam()
        {
            if (roamPoints.Count == 0 || !_agent.isOnNavMesh) return;
            _roam = (_roam + 1 + Random.Range(0, roamPoints.Count - 1)) % roamPoints.Count;
            _agent.isStopped = false;
            _agent.SetDestination(roamPoints[_roam].position);
        }

        void Face(Vector3 dir, float speed)
        {
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
        }

        void Animate(float dt)
        {
            float t = Time.time;
            bool hunt = IsHunting;
            if (eyeLight != null) { eyeLight.color = hunt ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.85f, 0.35f); eyeLight.intensity = hunt ? 9f + Mathf.Sin(t * 30f) * 3f : 4f; }
            if (visual == null || Current == State.Slam) return;
            float sp = _agent.velocity.magnitude;
            float ring = hunt ? Mathf.Sin(t * 60f) * 3f : 0f;              // bells rattle while hunting
            visual.localRotation = Quaternion.Euler(-sp * 1.2f, 0, Mathf.Sin(t * Mathf.Max(2f, sp)) * 5f + ring);
            visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t * Mathf.Max(2f, sp * 0.9f))) * (sp > 1f ? 0.5f : 0.08f), 0);
            if (Current == State.Stunned) visual.localRotation = Quaternion.Euler(0, t * 200f, Mathf.Sin(t * 8f) * 12f);
            _tick -= dt;
            if (_tick <= 0f && (_player.transform.position - transform.position).sqrMagnitude < 50f * 50f)
            {
                _tick = hunt ? 0.25f : 1f;
                Audio.AudioManager.Play(hunt ? Audio.Sfx.ChairStep : Audio.Sfx.UiClick, transform.position, hunt ? 0.9f : 0.5f);
            }
        }

        GameObject Disc(Vector3 at, float size, Color c, bool ring = false)
        {
            var holder = new GameObject("AlarmTelegraph");
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(holder.transform, false);
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            holder.transform.position = at + Vector3.up * 0.06f;
            holder.transform.localScale = Vector3.one * size;
            var lib = VFXLibrary.Instance;
            var m = lib != null ? (ring ? lib.ring : lib.telegraph) : null;
            var r = q.GetComponent<Renderer>();
            if (m != null) { r.material = m; r.material.color = c; }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return holder;
        }
    }
}
