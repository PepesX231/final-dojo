using System;
using System.Collections.Generic;
using DreamRush.Combat;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// Shared body for every Midnight Palace enemy (M14, user: "the monsters all feel the same"): HP + hit flash,
    /// knock-back, hit-stun, juggles (a launch sends it flying; it floats while you keep hitting it), stun, ground-following
    /// movement on whatever is below, leash, lock-on point, death VFX and one static <see cref="AnyDied"/> event that
    /// ambush waves count. Subclasses only write <see cref="Think"/> (their personality) and optional guard rules.
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour, IHittable
    {
        public static readonly List<EnemyBase> All = new List<EnemyBase>();
        public static event Action<EnemyBase> AnyDied;
        public static int Defeated { get; private set; }
        public static void ResetDefeated() => Defeated = 0;
        /// <summary>For enemies that keep their own class (TickMinion): report a death to the shared counter.</summary>
        public static void ReportDeath(EnemyBase e) { Defeated++; AnyDied?.Invoke(e); }

        public string displayName = "SHADOW";
        public float maxHp = 60f, notice = 16f, leash = 32f;
        public float height = 1.8f, radius = 0.6f;
        public float hp;
        public bool Dead { get; private set; }
        public bool Airborne { get; private set; }
        public bool Stunned => Time.time < _stunUntil;
        public float Hp01 => Mathf.Clamp01(hp / Mathf.Max(1f, maxHp));
        public float LastHurtTime { get; private set; } = -99f;

        protected Transform player; protected PlayerMotor3D motor; protected IPlayerDamageReceiver recv;
        protected Vector3 home; protected Transform model; protected float t;
        Renderer[] _rends; MaterialPropertyBlock _mpb;
        float _vy, _stunUntil, _flashUntil; Vector3 _knock;

        protected virtual void Awake()
        {
            hp = maxHp;
            model = transform.childCount > 0 ? transform.GetChild(0) : transform;
            _rends = GetComponentsInChildren<Renderer>(); _mpb = new MaterialPropertyBlock();
            if (GetComponent<LockOnTarget>() == null) { var lt = gameObject.AddComponent<LockOnTarget>(); lt.offset = new Vector3(0, height * 0.55f, 0); }
            if (GetComponent<Collider>() == null)
            {
                var c = gameObject.AddComponent<CapsuleCollider>(); c.center = new Vector3(0, height * 0.5f, 0); c.radius = radius; c.height = height; c.isTrigger = true;
            }
        }

        protected virtual void OnEnable() { All.Add(this); home = transform.position; }
        protected virtual void OnDisable() { All.Remove(this); Tokens.Remove(this); }

        protected virtual void Start()
        {
            motor = FindFirstObjectByType<PlayerMotor3D>();
            if (motor != null) { player = motor.transform; recv = motor.GetComponent<IPlayerDamageReceiver>(); }
        }

        /// <summary>Ambush entrance: falls from the sky onto <paramref name="target"/> (can't be hit until it lands).</summary>
        public virtual void DropIn(Vector3 target) { _dropTarget = target; _dropping = true; }
        Vector3 _dropTarget; bool _dropping;
        public bool Dropping => _dropping;

        void Update()
        {
            if (Dead || player == null) return;
            float dt = Time.deltaTime; t += dt;
            if (_dropping)
            {
                transform.position = Vector3.MoveTowards(transform.position, _dropTarget, 24f * dt);
                if ((transform.position - _dropTarget).sqrMagnitude < 0.01f) { _dropping = false; home = transform.position; VFX.Play(VFXKind.DustPuff, transform.position, 1.2f + height * 0.3f); CameraShake.Add(0.12f + height * 0.04f); OnLanded(); }
                return;
            }
            if (_flashUntil > 0f && Time.time > _flashUntil) { _flashUntil = 0f; ClearTint(); }
            if (_knock.sqrMagnitude > 0.01f) { MoveGround(_knock * dt, ignoreLeash: true); _knock = Vector3.MoveTowards(_knock, Vector3.zero, 30f * dt); _vel = Vector3.zero; }
            if (Airborne) { UpdateAir(dt); return; }
            if (Stunned) { StunPose(dt); return; }
            Vector3 to = player.position - transform.position; to.y = 0f;
            Think(dt, to, to.magnitude);
        }

        /// <summary>The enemy's personality (only called when grounded, alive and not stunned).</summary>
        protected abstract void Think(float dt, Vector3 toPlayer, float dist);

        /// <summary>Return false to block the hit (guard). Default: everything lands.</summary>
        protected virtual bool AcceptHit(ref HitInfo hit) => true;
        protected virtual void OnHurt(HitInfo hit) { }
        protected virtual void OnStunPose(float dt) { model.localRotation = Quaternion.Euler(0, Time.time * 300f, Mathf.Sin(Time.time * 9f) * 10f); }
        protected virtual float GravityScale => 1f;
        /// <summary>Called when a DropIn lands (wake up and fight).</summary>
        protected virtual void OnLanded() { }

        void StunPose(float dt)
        {
            OnStunPose(dt);
            if (Mathf.Repeat(Time.time, 0.3f) < dt) VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * (height + 0.3f), 0.3f, VFX.Gold);
            if (!Stunned) model.localRotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ air (juggles)
        public void Launch(float vy)
        {
            if (Dead) return;
            Airborne = true; _vy = Mathf.Max(_vy, vy);
            model.localRotation = Quaternion.identity;
        }

        void UpdateAir(float dt)
        {
            // floaty while being juggled: gravity is low for 0.45 s after each hit
            float g = (Time.time - LastHurtTime < 0.45f ? 7f : 26f) * GravityScale;
            _vy -= g * dt;
            Vector3 p = transform.position + Vector3.up * _vy * dt;
            model.localRotation = Quaternion.Euler(t * 420f, 0, 0);
            if (_vy < 0f && Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out var hit, 1.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                p.y = hit.point.y; Airborne = false; _vy = 0f;
                model.localRotation = Quaternion.identity;
                VFX.Play(VFXKind.DustPuff, p, 1.1f); CameraShake.Add(0.08f);
                Stun(0.6f);
            }
            if (p.y < home.y - 40f) { Die(); return; }
            transform.position = p;
        }

        // ------------------------------------------------------------------ helpers for subclasses
        protected bool MoveGround(Vector3 delta, bool ignoreLeash = false)
        {
            Vector3 p = transform.position + delta;
            if (!ignoreLeash && Vector3.Distance(p, home) > leash) return false;
            if (GroundBelow(p, out float gy)) p.y = gy; else return false;   // never off a ledge
            transform.position = p; return true;
        }

        bool GroundBelow(Vector3 p, out float y)
        {
            y = 0f; float best = float.MaxValue; bool found = false;
            foreach (var h in Physics.RaycastAll(p + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (player != null && h.transform.IsChildOf(player)) continue;   // never climb onto Pip
                if (h.distance < best) { best = h.distance; y = h.point.y; found = true; }
            }
            return found;
        }

        /// <summary>Solid world geometry ahead (ignores Pip, triggers and other enemies) — charge crashes.</summary>
        protected bool Blocked(Vector3 origin, Vector3 dir, float dist)
        {
            foreach (var h in Physics.RaycastAll(origin, dir, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (player != null && h.transform.IsChildOf(player)) continue;
                if (h.transform.IsChildOf(transform)) continue;
                return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ M18 movement: steering, crowd, gait
        /// <summary>Current steered ground velocity (m/s).</summary>
        public Vector3 Velocity => _vel;
        Vector3 _vel; float _gaitPhase, _strafeSign = 1f, _strafeFlipAt;
        /// <summary>
        /// Accelerating movement instead of teleport-speed: velocity eases toward <paramref name="desired"/> (direction ×
        /// speed) at <paramref name="accel"/> m/s², plus a separation push away from nearby enemies so packs fan out
        /// instead of stacking on one spot. Returns false if the ground/leash stopped it.
        /// </summary>
        protected bool Steer(Vector3 desired, float accel, float dt, bool ignoreLeash = false)
        {
            desired.y = 0f;
            Vector3 sep = Vector3.zero;
            foreach (var e in All)
            {
                if (e == this || e.Dead) continue;
                Vector3 d = transform.position - e.transform.position; d.y = 0f;
                float r = (radius + e.radius) * 1.7f, m = d.magnitude;
                if (m < r && m > 0.001f) sep += d / m * (1f - m / r);
            }
            desired += sep * 5f;
            _vel = Vector3.MoveTowards(_vel, desired, accel * dt);
            if (_vel.sqrMagnitude < 0.0004f) return true;
            bool ok = MoveGround(_vel * dt, ignoreLeash);
            if (!ok) _vel *= 0.3f;
            return ok;
        }

        /// <summary>Brake to a stop (for wind-ups: the body keeps a little momentum, then plants).</summary>
        protected void Brake(float decel, float dt) { _vel = Vector3.MoveTowards(_vel, Vector3.zero, decel * dt); if (_vel.sqrMagnitude > 0.0004f) MoveGround(_vel * dt); }

        /// <summary>Circle the player at <paramref name="range"/>, switching direction now and then (waiting for an attack slot).</summary>
        protected Vector3 OrbitDir(Vector3 toPlayer, float range)
        {
            if (Time.time > _strafeFlipAt) { _strafeFlipAt = Time.time + UnityEngine.Random.Range(1.6f, 3.4f); if (UnityEngine.Random.value < 0.45f) _strafeSign = -_strafeSign; }
            Vector3 n = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, n) * _strafeSign;
            float radial = Mathf.Clamp((toPlayer.magnitude - range) * 0.8f, -1f, 1f);
            return (side + n * radial).normalized;
        }

        /// <summary>
        /// Procedural gait on the model from the real velocity: steps scale with speed, the body leans into acceleration
        /// and the direction of travel (relative to facing), with a little hop + side-to-side waddle. <paramref name="hop"/>
        /// is the step height, <paramref name="stride"/> metres per step.
        /// </summary>
        protected void Gait(float dt, float hop = 0.12f, float stride = 0.9f, float sway = 8f, float lean = 10f)
        {
            float sp = _vel.magnitude;
            _gaitPhase += sp / Mathf.Max(0.1f, stride) * Mathf.PI * dt;
            float k = Mathf.Clamp01(sp / 3f);
            Vector3 local = transform.InverseTransformDirection(_vel);
            Vector3 acc = (_vel - _lastVel) / Mathf.Max(dt, 1e-4f); _lastVel = _vel;
            _accS = Vector3.Lerp(_accS, transform.InverseTransformDirection(acc), 1f - Mathf.Exp(-8f * dt));
            float step = Mathf.Abs(Mathf.Sin(_gaitPhase));
            model.localPosition = new Vector3(0, step * hop * k, 0);
            float pitch = Mathf.Clamp(local.z * lean / 4f + _accS.z * 1.2f, -lean * 1.5f, lean * 1.5f);
            float roll = Mathf.Sin(_gaitPhase) * sway * k - Mathf.Clamp(local.x * lean / 4f, -lean, lean);
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(pitch, 0, roll), 1f - Mathf.Exp(-14f * dt));
        }
        Vector3 _lastVel, _accS;

        // ------------------------------------------------------------------ M18 attack tokens (packs take turns, Arkham-style)
        static readonly HashSet<EnemyBase> Tokens = new HashSet<EnemyBase>();
        public static int MaxAttackers = 2;
        public bool HasToken => Tokens.Contains(this);
        protected bool RequestToken()
        {
            Tokens.RemoveWhere(e => e == null || e.Dead || !e.isActiveAndEnabled);
            if (Tokens.Contains(this)) return true;
            if (Tokens.Count >= MaxAttackers) return false;
            Tokens.Add(this); return true;
        }
        protected void ReleaseToken() => Tokens.Remove(this);

        protected void Face(Vector3 dir, float degPerSec, float dt)
        {
            dir.y = 0; if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), degPerSec * dt);
        }

        protected bool PlayerAirborneAbove(float clearance) => player.position.y - transform.position.y > clearance;

        /// <summary>Hurt the player if within <paramref name="reach"/> (flat) and below <paramref name="maxHeight"/> above our feet.</summary>
        protected bool HitPlayer(string reason, float reach, float maxHeight, float knock = 6f)
        {
            Vector3 d = player.position - transform.position;
            if (new Vector2(d.x, d.z).magnitude > reach || d.y > maxHeight || d.y < -1.5f) return false;
            return recv != null && recv.TryHurt(reason, transform.position, knock);
        }

        public void Stun(float seconds) { _stunUntil = Mathf.Max(_stunUntil, Time.time + seconds); }

        protected void Tint(Color emission)
        {
            _mpb.SetColor("_Emission", emission);
            foreach (var r in _rends) if (r != null && r.name != "InkOutline") r.SetPropertyBlock(_mpb);
        }
        protected void ClearTint() { foreach (var r in _rends) if (r != null) r.SetPropertyBlock(null); }

        // ------------------------------------------------------------------ IHittable
        public bool TakeHit(HitInfo hit)
        {
            if (Dead || _dropping) return false;
            if (!AcceptHit(ref hit))
            {
                VFX.Play(VFXKind.ImpactRing, hit.point, 0.5f, VFX.Cream);
                Audio.AudioManager.Play(Audio.Sfx.Land, hit.point, 0.5f);
                return false;
            }
            hp -= hit.damage;
            LastHurtTime = Time.time;
            Tint(Color.white * 1.6f); _flashUntil = Time.time + 0.07f;
            Vector3 dir = hit.direction; dir.y = 0;
            _knock = dir.normalized * (hit.heavy ? 9f : 4f);
            if (hit.launch > 0f) Launch(hit.launch);
            else if (Airborne) _vy = Mathf.Max(_vy, 2.2f);           // air juggle: each hit holds it up
            OnHurt(hit);
            if (hp <= 0f) Die();
            return true;
        }

        protected virtual void Die()
        {
            if (Dead) return;
            Dead = true; Tokens.Remove(this);
            VFX.Play(VFXKind.Poof, transform.position + Vector3.up * height * 0.5f, 1.4f + height * 0.3f, VFX.Lavender);
            VFX.Play(VFXKind.Confetti, transform.position + Vector3.up, 0.8f);
            Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 0.8f);
            ReportDeath(this);
            if (PalaceDirector.Current != null) LostMinute.Burst(transform.position, Mathf.Clamp(Mathf.RoundToInt(maxHp / 55f), 1, 4));   // shadows drop the minutes they stole
            WonderDirector.Novelty(displayName.ToLowerInvariant() + " down");
            Destroy(gameObject, 0.05f);
        }
    }
}
