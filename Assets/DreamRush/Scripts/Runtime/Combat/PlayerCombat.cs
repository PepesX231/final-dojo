using System;
using DreamRush.CameraSystem;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.Combat
{
    /// <summary>
    /// Pip's level-2 combat kit (docs/LEVEL2_DESIGN.md §3), Elden-Ring grammar on top of the platformer motor:
    ///   LOCK-ON   R / middle mouse / R3 (also F / RMB / L1)  — camera + body face the target, legs strafe
    ///   LEAF BLADE  LMB / J / RB: 3-hit combo (light). Keep holding after the first swing → charged LEAP SLAM (heavy)
    ///   DODGE ROLL  tap Shift / tap L2 (or Ctrl / RT): i-frames 0.02–0.34 s; no direction = backstep
    ///   DREAM DODGE a roll that starts ≤0.18 s before a hit → slow-mo, stamina refund (rewarding, readable)
    ///   STAMINA  attacks and rolls cost it; regen after a short pause (no infinite roll spam)
    /// M14 move set (user: "Pip only goes hit-hit-hit"):
    ///   AIR SPIN     attack in the air: 360° spinning slash that keeps you aloft (×2 per jump) — juggles, cuckoos
    ///   RISING SLASH jump right after a landed light hit: launcher — the enemy flies up with you
    ///   DASH THRUST  attack during / just after a roll: a piercing lunge (great vs. Gear Rollers and guards' backs)
    ///   COUNTER      attack within 0.8 s after a DREAM DODGE: a critical ×2 hit
    ///   DREAM BURST  V / gamepad Y when the DREAM meter is full (hits + perfect dodges fill it): star blast, launches all
    /// M18: no sword — Pip fights with big cartoon mitts (FistRig drives the arms after the procedural animator).
    /// Jump, double jump and ground pound still work, so the boss can be fought like a platformer too.
    /// Damage to Pip goes through <see cref="TryHurt"/> (moons = SleepStability).
    /// </summary>
    [DefaultExecutionOrder(25)]
    public class PlayerCombat : MonoBehaviour, IPlayerDamageReceiver
    {
        public static PlayerCombat Current { get; private set; }
        public enum Act { Idle, Attack, Charge, Heavy, Roll, Hurt, AirSpin, Rising, Thrust, Burst }

        [Header("Stamina")]
        public float maxStamina = 100f, staminaRegen = 62f, regenDelay = 0.45f;
        public float rollCost = 24f, lightCost = 11f, heavyCost = 28f;
        [Header("Roll")]
        public float rollSpeed = 11.5f, rollTime = 0.42f, iframeStart = 0.02f, iframeEnd = 0.34f, perfectWindow = 0.18f;
        [Header("Fists")]
        public float[] comboDamage = { 24f, 24f, 36f };
        public float[] comboPosture = { 10f, 10f, 18f };
        public float lightRange = 1.95f, heavyRadius = 2.7f, heavyDamage = 70f, heavyPosture = 42f;
        public float lockRange = 45f;
        [Header("Move set (M14)")]
        public float airSpinDamage = 22f, thrustDamage = 34f, risingDamage = 20f, risingLaunch = 11f, burstDamage = 80f, burstRadius = 6.5f;
        public float dreamPerHit = 7f, dreamPerPerfect = 25f;
        [Header("Upgrades (Watchmaker)")]
        public float damageMul = 1f, meterGainMul = 1f;

        public Act State { get; private set; } = Act.Idle;
        public int ComboStep { get; private set; }
        public float Stamina { get; private set; }
        public bool Locked => Target != null;
        public LockOnTarget Target { get; private set; }
        public bool Invulnerable { get; private set; }
        public int HitsLanded { get; private set; }
        public int PerfectDodges { get; private set; }
        public float Charge01 { get; private set; }
        public float Meter { get; private set; }               // 0..100, full = DREAM BURST ready
        public bool BurstReady => Meter >= 100f;
        public int AirSpins { get; private set; }
        public int Counters { get; private set; }
        public int Bursts { get; private set; }
        /// <summary>The striking fist (follows the punching mitt; kept so FX and tests have one anchor).</summary>
        public Transform FistPivot => _bladePivot;
        public float ActTime => _t;
        // M18 skill-bar state (the HUD lights a skill when it can be used right now)
        public bool CounterOpen => Time.time < _counterUntil;
        public float CounterLeft01 => Mathf.Clamp01((_counterUntil - Time.time) / 0.8f);
        public bool DashPunchOpen => (State == Act.Roll && _rollT > 0.16f) || Time.time - _rollEndAt < 0.25f;
        public int AirSpinsLeft => 2 - _airSpinsThisJump;
        public bool Airborne => _m != null && !_m.IsGrounded;
        public bool CanHeavy => Stamina > 0.5f;
        public bool RisingOpen => State == Act.Attack && _hitDone;
        /// <summary>Raised whenever a named move starts ("combo","charge","heavy","air","rising","thrust","burst").</summary>
        public event Action<string> MoveUsed;
        public void AddDream(float v) { Meter = Mathf.Clamp(Meter + v, 0f, 100f); }
        public event Action<HitInfo> HitLanded;
        public event Action PerfectDodge;

        PlayerMotor3D _m;
        IPlayerCommandSource _src;
        ThirdPersonCameraController _cam;
        Transform _bladePivot;
        FistRig _fists;
        RectTransform _reticle;
        float _t, _lastSpend = -9f, _pressAt = -9f, _rollT;
        bool _queued, _hitDone, _attackHeld;
        Vector3 _rollDir;
        float _slowUntil, _rollEndAt = -9f, _counterUntil = -9f;
        int _airSpinsThisJump; float _groundedAt = -9f;
        readonly System.Collections.Generic.HashSet<IHittable> _pierced = new System.Collections.Generic.HashSet<IHittable>();

        void Awake()
        {
            Current = this;
            _m = GetComponent<PlayerMotor3D>();
            _src = GetComponent<IPlayerCommandSource>();
            Stamina = maxStamina;
            _bladePivot = new GameObject("FistPivot").transform; _bladePivot.SetParent(transform, false); _bladePivot.localPosition = new Vector3(0.42f, 0.62f, 0.3f);
            _fists = GetComponent<FistRig>() ?? gameObject.AddComponent<FistRig>();
        }

        void Start()
        {
            _cam = FindFirstObjectByType<ThirdPersonCameraController>();
            if (UI.UIRoot.Exists && GetComponent<UI.CombatHUD>() == null) gameObject.AddComponent<UI.CombatHUD>().combat = this;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (_reticle != null) Destroy(_reticle.gameObject);
            if (Time.timeScale < 1f && _slowUntil > 0f) Time.timeScale = 1f;
        }

        // ================================================================== input & state
        void Update()
        {
            float dt = Time.deltaTime;
            var src = _m.CommandSource ?? _src;
            var cmd = src != null && _m.InputEnabled ? src.ReadCommand() : PlayerCommand.None;
            _attackHeld = cmd.AttackHeld;
            if (cmd.AttackPressed) _pressAt = Time.time;

            // stamina
            if (Time.time - _lastSpend > regenDelay && State != Act.Roll) Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * dt);

            // lock-on
            if (cmd.LockPressed) ToggleLock();
            if (Target != null && (!Target.isActiveAndEnabled || !Target.lockable || Vector3.Distance(Target.Point, transform.position) > lockRange * 1.3f)) Target = null;

            _t += dt;
            if (_m.IsGrounded) _groundedAt = Time.time;
            switch (State)
            {
                case Act.Idle:
                    if (cmd.SkillPressed && BurstReady) { StartBurst(); break; }
                    if (cmd.RollPressed || cmd.DashPressed) { TryRoll(); break; }
                    if (cmd.AttackPressed)
                    {
                        if (!_m.IsGrounded) { StartAirSpin(); break; }
                        if (Time.time - _rollEndAt < 0.25f) { StartThrust(); break; }
                        StartLight(0);
                    }
                    break;
                case Act.Attack: if (cmd.SkillPressed && BurstReady) { StartBurst(); break; } UpdateLight(cmd); break;
                case Act.Charge: UpdateCharge(cmd); break;
                case Act.Heavy: UpdateHeavy(); break;
                case Act.Roll:
                    if (cmd.AttackPressed && _rollT > 0.16f) { Invulnerable = false; StartThrust(); break; }
                    UpdateRoll(); break;
                case Act.Hurt: if (_t > 0.42f) State = Act.Idle; break;
                case Act.AirSpin: UpdateAirSpin(cmd); break;
                case Act.Rising: UpdateRising(cmd); break;
                case Act.Thrust: UpdateThrust(); break;
                case Act.Burst: UpdateBurst(); break;
            }
            if (_m.IsGrounded) _airSpinsThisJump = 0;

            // motor shaping
            _m.MoveSpeedScale = State == Act.Attack ? 0.25f : State == Act.Charge ? 0.2f : State == Act.Heavy || State == Act.Burst ? 0f : State == Act.Hurt ? 0.3f : State == Act.AirSpin ? 0.6f : 1f;
            if (Target != null && State != Act.Roll)
            {
                Vector3 to = Target.Point - transform.position; to.y = 0;
                _m.FaceDirection = to;
            }
            else if (State == Act.Attack || State == Act.Charge || State == Act.Heavy || State == Act.Thrust || State == Act.Rising) _m.FaceDirection = transform.forward;
            else _m.FaceDirection = Vector3.zero;

            if (_slowUntil > 0f && Time.unscaledTime > _slowUntil) { Time.timeScale = 1f; _slowUntil = 0f; }
            if (_glitchOff > 0f && Time.unscaledTime > _glitchOff) { _glitchOff = -1f; var sp = Look.StylePack.Current; if (sp != null) sp.Glitch = 0f; }
        }

        void LateUpdate()
        {
            AnimateFists();
            UpdateLockCamera();
            UpdateReticle();
        }

        bool Spend(float cost)
        {
            if (Stamina <= 0.5f) return false;          // can always act once above zero (ER style: go negative-ish)
            Stamina = Mathf.Max(0f, Stamina - cost);
            _lastSpend = Time.time;
            return true;
        }

        public void ToggleLock()
        {
            if (Target != null) { Target = null; Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 0.4f); return; }
            Transform view = _cam != null ? _cam.transform : transform;
            LockOnTarget best = null; float bestScore = float.MaxValue;
            foreach (var t in LockOnTarget.All)
            {
                if (t == null || !t.lockable) continue;
                Vector3 to = t.Point - view.position; float d = to.magnitude;
                if (d > lockRange) continue;
                float ang = Vector3.Angle(view.forward, to);
                float score = ang * 2f + d;
                if (score < bestScore) { bestScore = score; best = t; }
            }
            Target = best;
            if (best != null) Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 0.45f);
        }

        // ================================================================== light combo
        void StartLight(int step)
        {
            if (!Spend(lightCost)) return;
            State = Act.Attack; ComboStep = step; _t = 0f; _queued = false; _hitDone = false;
            if (Target == null && _m.MoveIntent.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(_m.MoveIntent.normalized);
        }

        const float LightWind = 0.1f, LightActive = 0.1f, LightRecover = 0.24f;

        void UpdateLight(PlayerCommand cmd)
        {
            if (cmd.AttackPressed && _t > 0.05f) _queued = true;
            if (!_hitDone && _t >= LightWind)
            {
                _hitDone = true;
                if (_m.IsGrounded) _m.SetVelocity(transform.forward * (ComboStep == 2 ? 6.5f : 4.5f));
                PunchFX(ComboStep == 0 ? "light0" : ComboStep == 1 ? "light1" : "light2");
                Swing(transform.position + Vector3.up * 0.8f, lightRange, 110f, comboDamage[ComboStep], comboPosture[ComboStep], false);
            }
            float end = LightWind + LightActive + (ComboStep == 2 ? LightRecover * 1.6f : LightRecover);
            // hold after the first swing → charge a leap slam
            if (ComboStep == 0 && _attackHeld && Time.time - _pressAt > 0.32f && _t > LightWind + LightActive) { State = Act.Charge; _t = 0f; Charge01 = 0f; return; }
            if ((cmd.RollPressed || cmd.DashPressed) && _t > LightWind + LightActive) { TryRoll(); return; }   // recovery cancel
            if (cmd.JumpPressed && _hitDone && Time.time - _groundedAt < 0.12f && _t > LightWind) { StartRising(); return; }      // launcher (the motor may already have jumped this frame)
            if (_t >= LightWind + LightActive && _queued && ComboStep < 2) { StartLight(ComboStep + 1); return; }
            if (_t >= end) { State = Act.Idle; ComboStep = 0; }
        }

        // ================================================================== charged leap slam
        void UpdateCharge(PlayerCommand cmd)
        {
            Charge01 = Mathf.Clamp01(_t / 0.9f);
            if (Mathf.Repeat(_t, 0.12f) < Time.deltaTime) VFX.Play(VFXKind.Poof, _bladePivot.position + Vector3.up * 0.3f, 0.3f + Charge01 * 0.3f, VFX.Gold);
            if (Mathf.Repeat(_t, 0.22f) < Time.deltaTime) VFX.Play(VFXKind.Embers, _bladePivot.position + Vector3.up * 0.2f, 0.5f + Charge01 * 0.5f);
            if (cmd.RollPressed || cmd.DashPressed) { TryRoll(); return; }
            if (!_attackHeld || _t > 1.4f)
            {
                if (!Spend(heavyCost)) { State = Act.Idle; return; }
                State = Act.Heavy; _t = 0f; _hitDone = false;
                _m.SetVelocity(transform.forward * (4f + Charge01 * 4f) + Vector3.up * 7.5f);
                Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 1f);
            }
        }

        void UpdateHeavy()
        {
            if (!_hitDone && _t > 0.18f && (_m.IsGrounded || _t > 0.9f))
            {
                _hitDone = true;
                float k = 1f + Charge01 * 0.6f;
                PunchFX("heavy");
                Swing(transform.position + transform.forward * 0.9f + Vector3.up * 0.6f, heavyRadius, 360f, heavyDamage * k, heavyPosture * k, true);
                VFX.Play(VFXKind.ImpactRing, transform.position + transform.forward * 0.9f, 1.3f + Charge01 * 0.6f, VFX.Gold);
                VFX.Play(VFXKind.DustPuff, transform.position, 1.6f);
                CameraShake.Add(0.5f);
                Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f);
                _t = 0.9f;
            }
            if (_hitDone && _t > 1.25f) State = Act.Idle;
        }

        // ================================================================== dodge roll
        void TryRoll()
        {
            if (!_m.IsGrounded || !Spend(rollCost)) return;
            Vector3 dir = _m.MoveIntent.sqrMagnitude > 0.01f ? _m.MoveIntent.normalized : -transform.forward;   // no input = backstep
            _rollDir = dir;
            State = Act.Roll; _rollT = 0f; _t = 0f;
            _m.Dash(dir, rollSpeed, rollTime);
            Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 0.6f);
            VFX.Play(VFXKind.DustPuff, transform.position, 0.9f);
        }

        void UpdateRoll()
        {
            _rollT += Time.deltaTime;
            Invulnerable = _rollT >= iframeStart && _rollT <= iframeEnd;
            if (_rollT >= rollTime + 0.08f) { State = Act.Idle; Invulnerable = false; _rollEndAt = Time.time; }
        }

        /// <summary>Dev/test hook: start a move by name (light, light2, air, rising, thrust, burst, charge).</summary>
        public string DevDo(string move)
        {
            Stamina = maxStamina;
            switch (move)
            {
                case "light": StartLight(0); break;
                case "light2": StartLight(2); break;
                case "air": _m.SetVelocity(Vector3.up * 8f); _airSpinsThisJump = 0; State = Act.Idle; StartAirSpin(); break;
                case "rising": StartRising(); break;
                case "thrust": StartThrust(); break;
                case "burst": Meter = 100f; StartBurst(); break;
                case "charge": State = Act.Charge; _t = 0.5f; Charge01 = 0.5f; break;
                default: return "unknown move " + move;
            }
            return "move " + move + " → " + State;
        }

        // ================================================================== M14 moves
        void StartAirSpin()
        {
            if (_airSpinsThisJump >= 2 || !Spend(lightCost)) return;
            _airSpinsThisJump++; AirSpins++;
            State = Act.AirSpin; _t = 0f; _hitDone = false;
            Vector3 v = _m.PlanarVelocity; _m.SetVelocity(v * 0.7f + Vector3.up * Mathf.Max(3.5f, _m.VerticalVelocity));
        }

        void UpdateAirSpin(PlayerCommand cmd)
        {
            if (!_hitDone && _t > 0.07f)
            {
                _hitDone = true;
                PunchFX("air");
                Swing(transform.position + Vector3.up * 0.6f, 2.5f, 360f, airSpinDamage, 8f, false, "AirSpin");
                VFX.Play(VFXKind.LandingRing, transform.position + Vector3.up * 0.6f, 1.1f, VFX.Gold);
            }
            if (_t > 0.3f) State = Act.Idle;
        }

        void StartRising()
        {
            if (!Spend(lightCost)) return;
            State = Act.Rising; _t = 0f; _hitDone = false;
            _m.SetVelocity(transform.forward * 2f + Vector3.up * 10.5f);
            Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 1f);
        }

        void UpdateRising(PlayerCommand cmd)
        {
            if (!_hitDone && _t > 0.04f)
            {
                _hitDone = true;
                PunchFX("rising");
                Swing(transform.position + Vector3.up * 0.9f, 2.6f, 150f, risingDamage, 12f, false, "Rising", launch: risingLaunch);
            }
            if (_t > 0.15f && cmd.AttackPressed) { State = Act.Idle; StartAirSpin(); return; }   // rising → air spin juggle
            if (_t > 0.45f) State = Act.Idle;
        }

        void StartThrust()
        {
            if (!Spend(lightCost * 1.4f)) { State = Act.Idle; return; }
            State = Act.Thrust; _t = 0f; _pierced.Clear();
            Vector3 dir = Target != null ? Target.Point - transform.position : (_m.MoveIntent.sqrMagnitude > 0.01f ? _m.MoveIntent : transform.forward);
            dir.y = 0; if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir.normalized);
            _m.Dash(transform.forward, 15f, 0.2f);
            PunchFX("thrust");
            VFX.Play(VFXKind.DustPuff, transform.position, 1f);
        }

        void UpdateThrust()
        {
            if (_t > 0.03f && _t < 0.28f)
                Swing(transform.position + transform.forward * 1.1f + Vector3.up * 0.7f, 1.6f, 120f, thrustDamage, 22f, false, "Thrust", pierce: true);
            if (_t > 0.45f) State = Act.Idle;
        }

        void StartBurst()
        {
            Meter = 0f; Bursts++;
            State = Act.Burst; _t = 0f; _hitDone = false; Invulnerable = true;
            _m.SetVelocity(Vector3.up * 4f);
            UI.UIRoot.Get().Toast("DREAM BURST!", 1f);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, transform.position, 1f);
            Time.timeScale = 0.35f; _slowUntil = Time.unscaledTime + 0.35f;
        }

        void UpdateBurst()
        {
            if (!_hitDone && _t > 0.12f)
            {
                _hitDone = true;
                PunchFX("burst");
                Swing(transform.position + Vector3.up * 0.8f, burstRadius, 360f, burstDamage, 60f, true, "Burst", launch: 9f);
                VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up, 3.2f, VFX.Gold);
                VFX.Play(VFXKind.ImpactRing, transform.position, 3f, VFX.Lavender);
                VFX.Play(VFXKind.PaperBurst, transform.position + Vector3.up, 1.6f);
                CameraShake.Add(0.7f);
                var sp = Look.StylePack.Current; if (sp != null) { sp.Glitch = 1f; _glitchOff = Time.unscaledTime + 0.25f; }
            }
            if (_t > 0.55f) { State = Act.Idle; Invulnerable = false; }
        }
        float _glitchOff = -1f;

        // ================================================================== hits
        readonly Collider[] _hits = new Collider[32];

        void Swing(Vector3 center, float range, float arcDeg, float damage, float posture, bool heavy, string src = null, float launch = 0f, bool pierce = false)
        {
            bool counter = Time.time < _counterUntil;
            if (counter) { damage *= 2f; posture *= 1.5f; }
            damage *= damageMul;
            int n = Physics.OverlapSphereNonAlloc(center, range, _hits, ~0, QueryTriggerInteraction.Collide);
            var done = new System.Collections.Generic.HashSet<IHittable>();
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i].GetComponentInParent<IHittable>();
                if (h == null || done.Contains(h) || (pierce && _pierced.Contains(h))) continue;
                Vector3 p = _hits[i].ClosestPoint(center);
                Vector3 to = p - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, to) > arcDeg * 0.5f) continue;
                done.Add(h); if (pierce) _pierced.Add(h);
                var info = new HitInfo { damage = damage, posture = posture, point = p, direction = transform.forward, heavy = heavy, critical = counter, launch = launch, source = src ?? (heavy ? "LeapSlam" : "Fist" + ComboStep) };
                if (h.TakeHit(info))
                {
                    HitsLanded++;
                    if (src != "Burst") AddDream(dreamPerHit * meterGainMul);
                    if (counter) { Counters++; _counterUntil = -9f; UI.UIRoot.Get().Toast("COUNTER!", 0.7f); VFX.Play(VFXKind.PaperBurst, p, 0.9f); }
                    HitLanded?.Invoke(info);
                    VFX.Play(VFXKind.StarBurst, p, heavy ? 1.3f : 0.8f, heavy ? VFX.Gold : VFX.Cream);
                    VFX.Play(VFXKind.HitSpark, p, heavy || counter ? 1.5f : 1f, counter ? new Color(1f, 0.6f, 0.85f) : (Color?)null);
                    if (heavy || counter || ComboStep == 2) VFX.Play(VFXKind.Embers, p, 0.8f);
                    if (counter) Presentation.ComicPop.Show(p, 1.9f, "POW");
                    else if (ComboStep == 2 && State == Act.Attack) Presentation.ComicPop.Show(p, 1.5f, "POP");
                    CameraShake.Add(heavy ? 0.35f : 0.14f);
                    HitStop(heavy ? 0.09f : 0.045f);
                    Audio.AudioManager.Play(heavy || counter ? Audio.Sfx.PunchHeavy : Audio.Sfx.Punch, p, heavy ? 1f : 0.85f);
                    if (heavy || counter) Audio.AudioManager.Play(Audio.Sfx.HitHeavy, p, 0.45f);
                }
            }
        }

        // ================================================================== M15 skill VFX (crescent slashes, embers, flare)
        public static readonly Color SlashGold = new Color(1f, 0.88f, 0.58f), SlashHot = new Color(1f, 0.62f, 0.25f), SlashCounter = new Color(1f, 0.45f, 0.78f);

        /// <summary>M18 punch VFX: a shock ring around the fist, speed streaks, dust/ember on the big ones.</summary>
        void PunchFX(string move)
        {
            MoveUsed?.Invoke(move.StartsWith("light") ? "combo" : move);
            Vector3 c = transform.position + Vector3.up * 0.85f; Vector3 f = transform.forward, rt = transform.right;
            Quaternion look = Quaternion.LookRotation(f), ring = Quaternion.LookRotation(Vector3.up, -f);
            bool counter = Time.time < _counterUntil;
            Color col = counter ? SlashCounter : SlashGold;
            // M20 comet punches (user reference): the big moves launch a flaming mitt
            if (_fists != null && (move == "light2" || move == "thrust" || move == "burst"))
                FlyingFist.Launch(_fists.MittR, _fists.HandR + f * 0.3f, move == "light2" ? (f + Vector3.up * 0.35f).normalized : f, move == "thrust" ? 30f : 24f, 0.3f, move == "burst" ? 2.4f : 1.8f);
            else if (_fists != null && move.StartsWith("light")) VFX.Play(VFXKind.Flare, (move == "light1" ? _fists.HandL : _fists.HandR) + f * 0.4f, 0.35f, new Color(1f, 0.7f, 0.25f));
            switch (move)
            {
                case "light0": Ring(c + f * 1.1f + rt * 0.2f, ring, 0.55f, col, 0.35f); Audio.AudioManager.Play(Audio.Sfx.Whoosh, c, 0.3f); break;
                case "light1": Ring(c + f * 1.1f - rt * 0.2f, ring, 0.55f, col, 0.35f); Audio.AudioManager.Play(Audio.Sfx.Whoosh, c, 0.32f); break;
                case "light2":
                    SlashArc.Spawn(c + f * 0.7f, Quaternion.LookRotation(Vector3.up, -f) * Quaternion.Euler(0, 0, 0), 1.2f, 70f, counter ? col : SlashHot, 0.7f, 0.22f);   // uppercut swoosh
                    Ring(c + f * 1f + Vector3.up * 0.5f, Quaternion.LookRotation(f), 0.8f, counter ? col : SlashHot, 0.5f);
                    Audio.AudioManager.Play(Audio.Sfx.SwingHeavy, c, 0.8f);
                    break;
                case "heavy":
                    SlashArc.Spawn(transform.position + f * 0.9f + Vector3.up * 0.15f, look, heavyRadius, 360f, SlashHot, 1.1f, 0.4f);    // ground shockwave
                    Ring(transform.position + f * 0.9f + Vector3.up * 0.2f, look, heavyRadius * 0.6f, SlashGold, 0.6f);
                    VFX.Play(VFXKind.Poof, transform.position + f * 0.9f, 1.4f);
                    VFX.Play(VFXKind.Embers, transform.position + f * 0.9f + Vector3.up * 0.4f, 1.4f);
                    VFX.Play(VFXKind.Flare, transform.position + f * 0.9f + Vector3.up * 0.8f, 0.6f);
                    Presentation.ComicPop.Show(transform.position + f * 1.2f + Vector3.up, 2f, "BAM");
                    Audio.AudioManager.Play(Audio.Sfx.SwingHeavy, c, 1f);
                    break;
                case "air":
                    SlashArc.Spawn(transform.position + Vector3.up * 0.6f, look * Quaternion.Euler(0, 0, 8f), 1.9f, 360f, col, 0.6f, 0.3f);
                    Audio.AudioManager.Play(Audio.Sfx.Swing, c, 0.8f);
                    break;
                case "rising":
                    Ring(c + f * 0.6f + Vector3.up * 0.4f, look, 0.9f, col, 0.5f);
                    SlashArc.Spawn(c + f * 0.6f + Vector3.down * 0.6f, Quaternion.LookRotation(Vector3.up, -f), 1.8f, 50f, col, 0.6f, 0.3f);
                    VFX.Play(VFXKind.Embers, c, 0.7f);
                    Audio.AudioManager.Play(Audio.Sfx.SwingHeavy, c, 0.8f);
                    break;
                case "thrust":   // rocket punch: speed lines behind, a cone of rings ahead
                    SlashArc.Spawn(c - f * 1.2f, look, 3.2f, 20f, col, 0.35f, 0.24f);
                    for (int i = 0; i < 3; i++) Ring(c + f * (0.9f + i * 0.7f), ring, 0.35f + i * 0.18f, col, 0.25f + i * 0.04f);
                    VFX.Play(VFXKind.HitSpark, c + f * 1.6f, 0.9f);
                    Audio.AudioManager.Play(Audio.Sfx.Whoosh, c, 0.9f);
                    break;
                case "burst":
                    SlashArc.Spawn(c, look, burstRadius * 0.8f, 360f, SlashGold, 1.4f, 0.5f);
                    SlashArc.Spawn(c, look * Quaternion.Euler(0, 0, 70f), burstRadius * 0.55f, 360f, SlashCounter, 0.9f, 0.45f);
                    Ring(c + f * 0.8f, ring, 1.4f, SlashGold, 0.6f);
                    VFX.Play(VFXKind.Flare, c, 1.2f);
                    VFX.Play(VFXKind.FlareStreak, c, 1.2f);
                    VFX.Play(VFXKind.Rays, c, 1.2f);
                    VFX.Play(VFXKind.Embers, c, 2.2f);
                    Presentation.ComicPop.Show(c + f, 2.6f, "WHAM");
                    Audio.AudioManager.Play(Audio.Sfx.Burst, c, 1f);
                    break;
            }
        }

        /// <summary>A full thin crescent ring (the "air pops" around a punch).</summary>
        static void Ring(Vector3 at, Quaternion rot, float r, Color col, float width) => SlashArc.Spawn(at, rot, r, 360f, col, width, 0.18f);

        void HitStop(float seconds)
        {
            if (_slowUntil > 0f) return;
            Time.timeScale = 0.05f; _slowUntil = Time.unscaledTime + seconds;
        }

        // ================================================================== being hit
        public bool TryHurt(string reason, Vector3 from, float knockback = 7f)
        {
            if (Invulnerable)
            {
                if (_rollT <= perfectWindow + iframeStart)
                {
                    PerfectDodges++;
                    PerfectDodge?.Invoke();
                    AddDream(dreamPerPerfect); _counterUntil = Time.time + 0.8f;
                    Stamina = Mathf.Min(maxStamina, Stamina + rollCost);
                    Time.timeScale = 0.3f; _slowUntil = Time.unscaledTime + 0.55f;
                    VFX.Play(VFXKind.LandingRing, transform.position, 1.6f, VFX.Lavender);
                    VFX.Play(VFXKind.PaperBurst, transform.position + Vector3.up, 0.8f);
                    UI.UIRoot.Get().Toast("DREAM DODGE!", 0.9f);
                    Audio.AudioManager.Play(Audio.Sfx.SleepChime, transform.position, 0.8f);
                }
                return false;
            }
            var st = Dream.SleepStability.Current;
            bool landed = st == null || st.Damage(reason);
            if (!landed) return false;
            Vector3 away = transform.position - from; away.y = 0;
            if (away.sqrMagnitude < 0.01f) away = -transform.forward;
            _m.Launch(away.normalized * knockback + Vector3.up * 5.5f);
            State = Act.Hurt; _t = 0f; ComboStep = 0;
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up, 1f, VFX.Danger);
            var fx = FXLibrary.Instance;
            if (fx != null) WorldFX.Spawn(fx.ouch, transform.position + Vector3.up * 1.6f, 1f, 0.8f, transform);
            return true;
        }

        // ================================================================== camera + reticle
        float _baseDistance = -1f;
        void UpdateLockCamera()
        {
            if (_cam == null) return;
            if (_baseDistance < 0f) _baseDistance = _cam.distance;
            float k = 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime);
            if (Target == null) { _cam.distance = Mathf.Lerp(_cam.distance, _baseDistance, k * 0.5f); return; }
            // frame Pip AND a 6 m boss: pull back, and look UP at him when close (Elden Ring lock camera)
            Vector3 flat = Target.Point - transform.position; float dist = new Vector2(flat.x, flat.z).magnitude;
            Vector3 to = Target.Point - _cam.transform.position;
            float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float height = Target.Point.y - transform.position.y;
            float pitch = Mathf.Clamp(Mathf.Lerp(-6f, 14f, Mathf.InverseLerp(2f, 18f, dist)) - Mathf.Max(0f, height - 2f) * 1.2f, -12f, 30f);
            _cam.Yaw = Mathf.LerpAngle(_cam.Yaw, yaw, k);
            _cam.Pitch = Mathf.Lerp(_cam.Pitch, pitch, k * 0.5f);
            _cam.distance = Mathf.Lerp(_cam.distance, _baseDistance + 1.8f, k * 0.5f);
        }

        void UpdateReticle()
        {
            if (_reticle == null && UI.UIRoot.Exists)
            {
                var img = UI.UIFactory.Panel("LockReticle", UI.UIRoot.Get().Overlay, Color.white, UI.UIStyle.Diamond);
                img.raycastTarget = false;
                _reticle = img.rectTransform; _reticle.sizeDelta = new Vector2(38, 38);
                _reticle.anchorMin = _reticle.anchorMax = Vector2.zero;
            }
            if (_reticle == null) return;
            var cam = Camera.main;
            bool show = Target != null && cam != null;
            Vector3 sp = show ? cam.WorldToScreenPoint(Target.Point) : Vector3.zero;
            show &= sp.z > 0f;
            _reticle.gameObject.SetActive(show);
            if (!show) return;
            var canvas = _reticle.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            _reticle.anchoredPosition = new Vector2(sp.x, sp.y) / scale;
            _reticle.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 90f);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.08f;
            _reticle.localScale = Vector3.one * pulse;
            _reticle.GetComponent<Image>().color = UI.UIStyle.Red;
        }

        // ================================================================== fists (M18: replaces the leaf blade)
        void AnimateFists()
        {
            if (_bladePivot != null && _fists != null)
            {
                bool left = State == Act.Attack && ComboStep == 1;
                _bladePivot.SetPositionAndRotation(left ? _fists.HandL : _fists.HandR, transform.rotation);
            }
            // roll: spin the apple forward
            var vis = transform.Find("Visual");
            var model = vis != null && vis.childCount > 0 ? vis.GetChild(0) : null;
            if (model != null)
            {
                if (State == Act.Roll)
                {
                    float a = Mathf.Clamp01(_rollT / rollTime) * 360f;
                    Vector3 pivot = new Vector3(0, 0.6f, 0);
                    Quaternion r = Quaternion.AngleAxis(a, Vector3.right);
                    model.localRotation = r; model.localPosition = pivot - r * pivot;
                }
                else if (_wasRolling) { model.localRotation = Quaternion.identity; model.localPosition = Vector3.zero; }
            }
            _wasRolling = State == Act.Roll;
        }
        bool _wasRolling;
    }
}
