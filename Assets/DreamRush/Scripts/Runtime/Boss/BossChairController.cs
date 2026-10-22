using System;
using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.Boss
{
    public enum ChairState
    {
        Dormant, Idle, Alert, Flee, Panic, Reposition, Hesitate, Stumble, Exhausted,
        Rooted, Frozen, OfferSeat, Sitting, Defeated, Reveal, Pulled,
        Perched   // World 1 P3: on top of the Throne Tower (ThroneClimb), off the NavMesh
    }

    /// <summary>
    /// THE CHAIR (GDD §15): fair flee AI with readable openings. See docs/CHAIR_BOSS_DESIGN.md.
    /// Steppable (<see cref="Step"/>) for deterministic bot tests; <see cref="AutoStep"/> drives it in play.
    /// Card effects (M6) enter through <see cref="ApplyStatus"/>; victory requires the seat interaction.
    /// </summary>
    public class BossChairController : MonoBehaviour
    {
        public ChairAIConfig config;
        public PlayerMotor3D player;
        [Tooltip("Arena bounds (world) used for candidates and anti-softlock.")]
        public Bounds arenaBounds = new Bounds(new Vector3(0, 0.5f, 81.5f), new Vector3(32, 6, 25));
        public int seed = 12345;
        public bool AutoStep = true;

        public ChairState State { get; private set; } = ChairState.Dormant;
        public float StateTime { get; private set; }
        public string OpeningCause { get; private set; }
        public ChairFatigue Fatigue { get; private set; }
        public ChairLocomotion Locomotion { get; private set; }
        public EscapeDestinationSelector Selector { get; private set; }
        public float SitProgress => State == ChairState.Sitting ? Mathf.Clamp01(StateTime / config.sitDuration) : 0f;
        public float SitSeconds => State == ChairState.Sitting ? StateTime : 0f;
        public int SoftlockRecoveries { get; private set; }
        public int OpeningsCount { get; private set; }
        public Transform SeatPoint { get; private set; }

        public event Action<ChairState, ChairState> StateChanged;
        public event Action<string> Defeated;
        /// <summary>Raised when the chair "poofs" back into the arena (visible feedback hook).</summary>
        public event Action<Vector3, Vector3> Repositioned;

        float _stateDuration, _replanTimer, _playerCloseTime, _playerFarTime, _progressTimer, _lastRemaining;
        float _peakY;
        bool _airborne;
        Vector3 _lastMoveDir;
        bool _started;
        float _time;
        readonly List<Vector3> _tmp = new List<Vector3>(16);
        readonly DreamLogic.IBossWinCondition _winCondition = new DreamLogic.SitForSecondsWinCondition();

        static readonly HashSet<ChairState> SitCompatible = new HashSet<ChairState>
            { ChairState.Exhausted, ChairState.Stumble, ChairState.Rooted, ChairState.Frozen, ChairState.OfferSeat };

        public bool IsSitCompatible => SitCompatible.Contains(State);

        void Awake()
        {
            if (config == null) config = ChairAIConfig.CreateDefault();
            Fatigue = new ChairFatigue(config.exhaustionThreshold);
            Locomotion = new ChairLocomotion(transform);
            SeatPoint = transform.Find("SeatPoint");
            if (SeatPoint == null)
            {
                SeatPoint = new GameObject("SeatPoint").transform;
                SeatPoint.SetParent(transform, false);
                SeatPoint.localPosition = new Vector3(0, 0.72f, 0.05f);
            }
        }

        /// <summary>Build candidates from the baked NavMesh. Call after the NavMesh exists.</summary>
        public void Initialize(int runSeed)
        {
            seed = runSeed;
            var candidates = EscapeDestinationSelector.BuildCandidates(arenaBounds, config.candidateSpacing);
            Selector = new EscapeDestinationSelector(candidates, EscapeDestinationSelector.NavMeshPath, config, seed);
            if (player == null) player = FindFirstObjectByType<PlayerMotor3D>();
            if (player != null)
            {
                player.Landed -= OnPlayerLanded; player.Landed += OnPlayerLanded;
                player.LeftGround -= OnPlayerLeftGround; player.LeftGround += OnPlayerLeftGround;
                player.Jumped -= OnPlayerLeftGround; player.Jumped += OnPlayerLeftGround;
            }
            if (NavMesh.SamplePosition(transform.position, out var hit, 2f, NavMesh.AllAreas)) transform.position = hit.position;
            _started = true;
        }

        /// <summary>Test/bot helper: fresh run state with a new seed at a given position.</summary>
        public void ResetForSeed(int newSeed, Vector3 position)
        {
            Fatigue = new ChairFatigue(config.exhaustionThreshold);
            Locomotion.Stop(hard: true);
            transform.position = position;
            Selector = new EscapeDestinationSelector(new List<EscapeDestinationSelector.Candidate>(Selector.Candidates),
                EscapeDestinationSelector.NavMeshPath, config, newSeed);
            seed = newSeed;
            OpeningsCount = 0; SoftlockRecoveries = 0; OpeningCause = null;
            _time = 0f; _playerCloseTime = 0f; _progressTimer = 0f; _airborne = false;
            SetState(ChairState.Flee);
        }

        void OnDestroy()
        {
            if (player != null) { player.Landed -= OnPlayerLanded; player.LeftGround -= OnPlayerLeftGround; player.Jumped -= OnPlayerLeftGround; }
        }

        void Update()
        {
            if (AutoStep && _started) Step(Time.deltaTime);
        }

        // ================================================================== public API
        public void Activate() { if (State == ChairState.Dormant || State == ChairState.Reveal) SetState(ChairState.Flee); }
        public void SetDormant() => SetState(ChairState.Dormant);

        // ------------------------------------------------------------ card hooks (M6, via ChairEffectReceiver)
        Transform _threatOverride;
        float _threatUntil, _socialUntil, _pullDuration;

        /// <summary>Decoy: for a while the chair treats <paramref name="decoy"/> as the threat to flee from.</summary>
        public void SetThreatOverride(Transform decoy, float seconds)
        {
            _threatOverride = decoy;
            _threatUntil = _time + seconds;
            _replanTimer = 0f;
        }

        /// <summary>Social cue (Be a Grandpa): while active, the chair offers its seat when the player is near.</summary>
        public void SetSocialCue(float seconds) => _socialUntil = _time + seconds;
        public bool SocialCueActive => _time < _socialUntil;

        /// <summary>Fishing rod: reeled toward <paramref name="toward"/>, then a recovery window (Pulled opening).</summary>
        const float PullSpeed = 8f;
        public bool BeginPulled(Vector3 toward, float seconds)
        {
            if (State == ChairState.Sitting || State == ChairState.Defeated || State == ChairState.Dormant || State == ChairState.Reveal) return false;
            Vector3 dir = toward - transform.position; dir.y = 0f;
            Vector3 target = toward - dir.normalized * 1.4f;
            Locomotion.TargetSpeed = PullSpeed;
            Locomotion.SetPathTo(target); // reeled along the floor, around furniture
            // the line keeps reeling until the chair arrives (bounded so it can never lock the fight)
            _pullDuration = Mathf.Clamp(Locomotion.RemainingDistance / PullSpeed + 0.6f, seconds, 4f);
            SetState(ChairState.Pulled);
            return true;
        }

        Vector3 ThreatPosition => _threatOverride != null && _time < _threatUntil ? _threatOverride.position : player.transform.position;
        public void BeginReveal() => SetState(ChairState.Reveal);

        /// <summary>Card / effect entry point (M6). Duration in seconds. Returns false if not applicable now.</summary>
        public bool ApplyStatus(ChairState status, float duration, string cause)
        {
            if (State == ChairState.Sitting || State == ChairState.Defeated || State == ChairState.Dormant) return false;
            if (status != ChairState.Rooted && status != ChairState.Frozen && status != ChairState.OfferSeat && status != ChairState.Stumble) return false;
            OpenWith(status, duration, cause);
            return true;
        }

        /// <summary>Seat validation (GDD §15.9). Reason is filled when not allowed.</summary>
        public bool CanSit(PlayerMotor3D p, out string reason)
        {
            reason = null;
            if (p == null) { reason = "no player"; return false; }
            if (!IsSitCompatible) { reason = "chair won't hold still"; return false; }
            if (!p.IsGrounded) { reason = "player airborne"; return false; }
            Vector3 d = p.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > config.sitRange) { reason = "too far"; return false; }
            if (Mathf.Abs(p.transform.position.y - transform.position.y) > 1.2f) { reason = "seat not reachable"; return false; }
            return true;
        }

        public bool BeginSitting(PlayerMotor3D p)
        {
            if (!CanSit(p, out _)) return false;
            Locomotion.Stop(hard: true);
            SetState(ChairState.Sitting);
            return true;
        }

        /// <summary>Player hopped off before 2 s — the chair bolts.</summary>
        public void CancelSitting()
        {
            if (State != ChairState.Sitting) return;
            Fatigue.RecoverTo(0.35f);
            SetState(ChairState.Panic);
        }

        // ================================================================== simulation
        public void Step(float dt)
        {
            if (dt <= 0f || player == null) return;
            _time += dt;
            StateTime += dt;
            if (DevHold) return; // art review: keep the current pose/state, time still flows for the animator
            Vector3 pp = ThreatPosition;
            Vector3 toPlayer = pp - transform.position; toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 toReal = player.transform.position - transform.position; toReal.y = 0f;

            // Be a Grandpa: a polite chair offers its seat instead of fleeing (property SocialResponsive)
            if (SocialCueActive && toReal.magnitude < config.awarenessRadius + 1f &&
                (State == ChairState.Idle || State == ChairState.Alert || State == ChairState.Flee || State == ChairState.Panic || State == ChairState.Hesitate))
                OpenWith(ChairState.OfferSeat, _socialUntil - _time, "Grandpa");

            switch (State)
            {
                case ChairState.Dormant:
                case ChairState.Defeated:
                case ChairState.Reveal:
                    Locomotion.Step(dt, config.acceleration * 2f, config.turnRate * 2f);
                    return;

                case ChairState.Idle:
                    Fatigue.Rest(dt, config.fatigueRestRate);
                    if (dist < config.awarenessRadius) SetState(ChairState.Alert);
                    break;

                case ChairState.Alert:
                    Face(toPlayer, dt);
                    if (dist < config.escapeTriggerRadius || StateTime >= config.alertDuration) SetState(ChairState.Flee);
                    else if (dist > config.awarenessRadius) { _playerFarTime += dt; if (_playerFarTime > config.forgetAfterSeconds) SetState(ChairState.Idle); }
                    break;

                case ChairState.Flee:
                case ChairState.Panic:
                    if (SkillBusy) { Locomotion.Stop(false); Face(toPlayer, dt); break; }
                    UpdateFlee(dt, dist);
                    break;

                case ChairState.Hesitate:
                    Fatigue.Rest(dt, config.fatigueRestRate);
                    Face(toPlayer, dt); // looks back at the player — readable opening to close distance
                    if (dist < config.escapeTriggerRadius || StateTime >= _stateDuration) SetState(ChairState.Flee);
                    break;

                case ChairState.Stumble:
                case ChairState.Exhausted:
                case ChairState.Rooted:
                case ChairState.Frozen:
                case ChairState.OfferSeat:
                    if (State != ChairState.Exhausted) Fatigue.Rest(dt, config.fatigueRestRate * 0.5f);
                    if (State == ChairState.OfferSeat) Face(toPlayer, dt);
                    if (StateTime >= _stateDuration)
                    {
                        if (State == ChairState.Exhausted) Fatigue.RecoverTo(config.fatigueAfterExhaustion);
                        SetState(dist < config.escapeTriggerRadius ? ChairState.Panic : ChairState.Flee);
                    }
                    break;

                case ChairState.Perched:
                    Face(toReal, dt);   // smug: watches you climb
                    break;

                case ChairState.Pulled:
                    Locomotion.TargetSpeed = PullSpeed;
                    if (StateTime >= _pullDuration || !Locomotion.HasPath)
                        OpenWith(ChairState.Stumble, 2.2f, "Pulled");
                    break;

                case ChairState.Sitting:
                    if (player.CommandSource != null && player.CommandSource.ReadCommand().JumpPressed) { CancelSitting(); break; }
                    if (_winCondition.IsSatisfied(new DreamLogic.BossContext { SitSeconds = StateTime, RequiredSitSeconds = config.sitDuration, SitStateValid = true }))
                    {
                        SetState(ChairState.Defeated);
                        Defeated?.Invoke(OpeningCause ?? "Exhaustion");
                        return;
                    }
                    break;
            }

            if (IsPerched)
            {
                // on the tower top: anything that would make it run (cancelled sit, card expiry, reel) → back on the throne
                if (State == ChairState.Flee || State == ChairState.Panic || State == ChairState.Hesitate || State == ChairState.Alert ||
                    State == ChairState.Idle || State == ChairState.Reposition || State == ChairState.Pulled)
                { Locomotion.Stop(hard: true); SetState(ChairState.Perched); }
                return;
            }
            Locomotion.Step(dt, config.acceleration, config.turnRate);
            CheckSoftlock(dt);
        }

        void UpdateFlee(float dt, float dist)
        {
            bool panic = State == ChairState.Panic;
            Locomotion.TargetSpeed = panic ? config.panicSpeed : config.normalSpeed;
            if (Locomotion.Speed > 0.5f) Fatigue.Run(dt, panic ? config.fatiguePanicRate : config.fatigueRunRate);
            else Fatigue.Rest(dt, config.fatigueRestRate);

            if (Fatigue.IsExhausted) { OpenWith(ChairState.Exhausted, config.exhaustionDuration, "Exhaustion"); return; }

            // panic when the player stays very close
            if (dist < config.panicRadius) _playerCloseTime += dt; else _playerCloseTime = Mathf.Max(0f, _playerCloseTime - dt);
            if (!panic && _playerCloseTime > config.panicAfterSeconds) { SetState(ChairState.Panic); return; }
            if (panic && dist > config.awarenessRadius) { SetState(ChairState.Flee); return; }

            _replanTimer -= dt;
            bool arrived = !Locomotion.HasPath;
            if (arrived && dist > config.awarenessRadius * 0.9f && !panic)
            {
                // safe for now: pause and look back (hesitation opening, §15.6)
                _stateDuration = Mathf.Lerp(config.hesitationMin, config.hesitationMax, Selector.NextFloat());
                SetState(ChairState.Hesitate);
                return;
            }
            if (arrived || (_replanTimer <= 0f && dist < config.awarenessRadius))
                Replan(dist);
        }

        void Replan(float dist)
        {
            _replanTimer = State == ChairState.Panic ? config.repositionInterval * 0.6f : config.repositionInterval;
            Vector3 threat = ThreatPosition;
            var res = Selector.Choose(transform.position, threat, _time);
            if (!res.Found) res = Selector.ChooseRelaxed(transform.position, threat, _time);
            if (!res.Found && dist < config.escapeTriggerRadius)
            {
                // Cornered. Only a route the player deliberately blocked with a pushed block counts as the
                // Obstacle method (§16.7); against plain walls the chair panics and squeezes past the player.
                if (BlockedByPushedBlock()) { OpenWith(ChairState.Stumble, config.stumbleDuration, "Obstacle"); return; }
                res = Selector.ChooseSqueeze(transform.position, threat, _time);
                if (res.Found && State != ChairState.Panic) SetState(ChairState.Panic);
            }
            if (!res.Found) return;
            Vector3 newDir = Selector.LastPath.Count > 1 ? Selector.LastPath[1] - transform.position : res.Destination - transform.position;
            newDir.y = 0f;
            Vector3 vel = Locomotion.Velocity; vel.y = 0f;
            bool sharp = vel.magnitude > 2f && newDir.sqrMagnitude > 0.01f && Vector3.Angle(vel, newDir) > config.sharpTurnAngle;
            Selector.Remember(res.Destination, _time);
            Locomotion.SetPath(Selector.LastPath);
            if (sharp && State == ChairState.Panic && Selector.NextFloat() < config.stumbleChanceOnSharpTurn)
                OpenWith(ChairState.Stumble, config.stumbleDuration, "Exhaustion");
        }

        Dream.PushableBlock[] _blocks;

        bool BlockedByPushedBlock()
        {
            _blocks ??= FindObjectsByType<Dream.PushableBlock>(FindObjectsSortMode.None);
            foreach (var b in _blocks)
                if (b != null && b.EverPushed && Vector3.Distance(b.transform.position, transform.position) < 4f) return true;
            return false;
        }

        void OpenWith(ChairState s, float duration, string cause)
        {
            Locomotion.Stop(hard: s == ChairState.Frozen || s == ChairState.Rooted);
            OpeningCause = cause;
            _stateDuration = duration;
            OpeningsCount++;
            SetState(s);
        }

        void Face(Vector3 dir, float dt)
        {
            if (dir.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir.normalized), config.turnRate * dt);
        }

        /// <summary>World 1 skills (ChairSkills) hold the chair in place while a telegraphed attack plays (Time.time).</summary>
        public float SkillBusyUntil;
        public bool SkillBusy => Time.time < SkillBusyUntil;

        // ------------------------------------------------------------ Throne Climb (World 1 P3, ThroneClimb)
        /// <summary>True while the chair holds the top of the Throne Tower: no NavMesh locomotion, no softlock repositioning.</summary>
        public bool IsPerched { get; private set; }
        public void BeginPerch() { Locomotion.Stop(hard: true); IsPerched = true; SetState(ChairState.Perched); }
        public void EndPerch() { IsPerched = false; if (State == ChairState.Perched) SetState(ChairState.Flee); }
        /// <summary>The player made it to the top: the chair is spent (sit window until the player leaves the top).</summary>
        public void PerchExhausted(string cause) { if (IsPerched && State == ChairState.Perched) OpenWith(ChairState.Exhausted, 9999f, cause); }
        public void PerchRecover() { if (IsPerched && State == ChairState.Exhausted) SetState(ChairState.Perched); }

        /// <summary>Dev/art review only (DevCommands): freeze the AI in its current state.</summary>
        public bool DevHold;
        public void SetStateForDev(ChairState s) => SetState(s);

        void SetState(ChairState s)
        {
            var prev = State;
            State = s;
            StateTime = 0f;
            _playerFarTime = 0f;
            if (s == ChairState.Flee || s == ChairState.Panic) _replanTimer = 0f;
            if (s == ChairState.Idle || s == ChairState.Alert || s == ChairState.Hesitate) Locomotion.Stop(false);
            if (prev != s) StateChanged?.Invoke(prev, s);
        }

        // ================================================================== shortcut detection (§16.6)
        void OnPlayerLeftGround()
        {
            _airborne = true;
            _peakY = player.transform.position.y;
        }

        void OnPlayerLanded(float impact)
        {
            if (!_airborne) return;
            _airborne = false;
            float fall = _peakY - player.transform.position.y;
            if (State != ChairState.Flee && State != ChairState.Panic && State != ChairState.Hesitate) return;
            Vector3 to = player.transform.position - transform.position; to.y = 0f;
            Vector3 moveDir = Locomotion.Velocity.sqrMagnitude > 0.5f ? Locomotion.Velocity.normalized : transform.forward;
            if (fall >= config.shortcutMinFall && to.magnitude <= config.shortcutLandingRadius && Vector3.Dot(moveDir, to.normalized) > 0.2f)
                OpenWith(ChairState.Stumble, config.stumbleDuration, "Shortcut");
        }

        void LateUpdate() => TrackPlayerAir();

        /// <summary>Tracks the highest point of the player's current fall (also catches drops without a take-off event).</summary>
        public void TrackPlayerAir()
        {
            if (player == null) return;
            if (!player.IsGrounded && !_airborne) { _airborne = true; _peakY = player.transform.position.y; }
            if (_airborne) _peakY = Mathf.Max(_peakY, player.transform.position.y);
        }

        // ================================================================== anti-softlock (§15.8)
        void CheckSoftlock(float dt)
        {
            bool bad = transform.position.y < config.minY
                       || !arenaBounds.Contains(new Vector3(transform.position.x, arenaBounds.center.y, transform.position.z))
                       || !NavMesh.SamplePosition(transform.position, out _, 0.6f, NavMesh.AllAreas);

            if (Locomotion.HasPath && Locomotion.TargetSpeed > 0.1f && (State == ChairState.Flee || State == ChairState.Panic))
            {
                float rem = Locomotion.RemainingDistance;
                if (rem < _lastRemaining - 0.05f) _progressTimer = 0f; else _progressTimer += dt;
                _lastRemaining = rem;
                if (_progressTimer > config.stuckSeconds) bad = true;
            }
            else _progressTimer = 0f;

            if (bad) SafeReposition();
        }

        public void SafeReposition()
        {
            Vector3 from = transform.position;
            Vector3 best = arenaBounds.center;
            float bestD = -1f;
            Vector3 pp = player != null ? player.transform.position : arenaBounds.center;
            foreach (var c in Selector.Candidates)
            {
                float d = (c.Position - pp).sqrMagnitude;
                if (d > bestD && c.Openness > 1.5f) { bestD = d; best = c.Position; }
            }
            Locomotion.Stop(hard: true);
            transform.position = best;
            _progressTimer = 0f;
            SoftlockRecoveries++;
            Repositioned?.Invoke(from, best);
            SetState(ChairState.Alert);
        }
    }
}
