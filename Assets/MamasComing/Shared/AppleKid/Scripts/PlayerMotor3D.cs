using System;
using DreamRush.Core;
using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Apple child movement (GDD §9). CharacterController based, camera-relative, with coyote time,
    /// jump buffer, variable jump height, ground snapping and wall-speed cancelling.
    /// Deterministic: <see cref="Step"/> can be called directly with a fixed dt (tests, replays).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-50)]
    public class PlayerMotor3D : MonoBehaviour
    {
        [SerializeField] PlayerMotorConfig config;
        [Tooltip("Camera used for camera-relative input. Falls back to Camera.main.")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("If true the motor steps itself in Update using its command source.")]
        [SerializeField] bool autoStep = true;

        public event Action Jumped;
        /// <summary>Argument = downward impact speed (m/s, positive).</summary>
        public event Action<float> Landed;
        public event Action LeftGround;

        public PlayerMotorConfig Config { get => config; set { config = value; SyncAssist(); } }
        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }
        public bool AutoStep { get => autoStep; set => autoStep = value; }
        public IPlayerCommandSource CommandSource { get; set; }
        /// <summary>When false the motor still simulates gravity but ignores commands (menus, cutscenes).</summary>
        public bool InputEnabled { get; set; } = true;

        // Movement modifiers used by environmental and combat effects.
        public bool AllowJump { get; set; } = true;
        public bool AllowSprint { get; set; } = true;
        /// <summary>Multiplies ground deceleration / turnaround (below 1 = slippery).</summary>
        public float GroundGripScale { get; set; } = 1f;

        public CharacterController Controller { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsJumpRising => _jumpRising;
        public bool IsSprinting { get; private set; }
        public Vector3 PlanarVelocity => _planar;
        public float VerticalVelocity => _vy;
        public Vector3 Velocity => _planar + Vector3.up * _vy;
        public Vector3 MoveIntent { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float AirTime { get; private set; }
        public float LastLandingSpeed { get; private set; }
        public JumpAssist Assist => _assist;

        Vector3 _planar;
        float _vy;
        bool _jumpRising;
        JumpAssist _assist;
        static readonly Vector3 Up = Vector3.up;

        /// <summary>The active player (last one enabled).</summary>
        public static PlayerMotor3D Instance { get; private set; }
        void OnEnable() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            if (config == null) config = PlayerMotorConfig.CreateDefault();
            _assist = new JumpAssist(config.coyoteTime, config.jumpBuffer);
            if (CommandSource == null) CommandSource = GetComponent<IPlayerCommandSource>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void SyncAssist()
        {
            if (_assist == null || config == null) return;
            _assist.CoyoteTime = config.coyoteTime;
            _assist.BufferTime = config.jumpBuffer;
        }

        void Update()
        {
            if (!autoStep) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var cmd = CommandSource != null ? CommandSource.ReadCommand() : PlayerCommand.None;
            Step(dt, cmd);
        }

        /// <summary>Advance the simulation by <paramref name="dt"/> seconds.</summary>
        public void Step(float dt, PlayerCommand cmd)
        {
            if (Controller == null || !Controller.enabled) return;
            if (!InputEnabled) cmd = PlayerCommand.None;
            if (!AllowJump) { cmd.JumpPressed = false; cmd.JumpHeld = false; }
            if (!AllowSprint) cmd.Sprint = false;
            _lastDt = dt;
            var c = config;
            SyncAssist();
            bool wasGrounded = IsGrounded;

            // 1. Intent ---------------------------------------------------------------
            Vector3 intent = cameraTransform != null
                ? MovementMath.CameraRelative(cmd.Move, cameraTransform.forward, cameraTransform.right, Up)
                : Vector3.ClampMagnitude(new Vector3(cmd.Move.x, 0f, cmd.Move.y), 1f);
            MoveIntent = intent;
            IsSprinting = cmd.Sprint && intent.sqrMagnitude > 0.01f;
            Vector3 targetPlanar = intent * ((IsSprinting ? c.sprintSpeed : c.walkSpeed) * MoveSpeedScale);

            // 2. Jump -----------------------------------------------------------------
            _assist.Tick(dt, IsGrounded && _vy <= 0.01f, cmd.JumpPressed);
            bool jumpedThisStep = false;
            if (_assist.TryConsumeJump())
            {
                _vy = c.JumpVelocity; // always from base gravity → buffered/coyote jumps are full height
                _jumpRising = true;
                jumpedThisStep = true;
                IsGrounded = false;
                Jumped?.Invoke();
            }

            // 3. Gravity --------------------------------------------------------------
            float vyStart = _vy;
            if (_dashTime > 0f)
            {
                // dash: straight line, gravity off (air) — the arc resumes when it ends
                _dashTime -= dt;
                _planar = _dashVel;
                if (!IsGrounded) _vy = Mathf.Max(_vy, 0f) * 0.2f;
                if (_dashTime <= 0f) { _planar = _dashVel.normalized * Mathf.Min(_dashVel.magnitude, DashCarrySpeed); if (!IsGrounded) _ballistic = true; }
            }
            else if (IsGrounded && !jumpedThisStep)
            {
                _vy = -c.groundStickVelocity;
            }
            else
            {
                if (_vy <= 0f) _jumpRising = false;
                float g = MovementMath.SelectGravity(c, _vy, _jumpRising, cmd.JumpHeld);
                _vy = Mathf.Max(_vy - g * dt, -c.maxFallSpeed);
                _vy += ExternalVerticalAccel * dt;
                if (FallSpeedOverride > 0f) _vy = Mathf.Max(_vy, -FallSpeedOverride);
            }

            // 4. Horizontal -----------------------------------------------------------
            if (_dashTime > 0f) { }
            else if (_ballistic && !IsGrounded)
            {
                // launch-pad arc: keep the planned velocity (lands on target); input only nudges it
                if (intent.sqrMagnitude > 0.01f) _planar += intent * (c.airAcceleration * 0.3f * dt);
            }
            else
            {
                float accel = MovementMath.SelectAcceleration(c, IsGrounded, _planar, targetPlanar);
                if (IsGrounded && GroundGripScale < 1f) accel *= GroundGripScale;
                _planar = Vector3.MoveTowards(_planar, targetPlanar, accel * dt);
            }

            // 5. Facing ---------------------------------------------------------------
            if (FaceDirection.sqrMagnitude > 1e-4f && _dashTime <= 0f)
            {   // lock-on strafing (PlayerCombat): body keeps facing the target while the legs move anywhere
                var targetRot = Quaternion.LookRotation(new Vector3(FaceDirection.x, 0f, FaceDirection.z).normalized, Up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, c.rotationSpeedDegrees * 1.5f * dt);
            }
            else if (intent.sqrMagnitude > 1e-4f)
            {
                var targetRot = Quaternion.LookRotation(intent.normalized, Up);
                float rs = IsGrounded ? c.rotationSpeedDegrees : c.airRotationSpeedDegrees;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rs * dt);
            }

            // 6. Move -----------------------------------------------------------------
            Vector3 horizontal = _planar;
            if (IsGrounded && !jumpedThisStep && horizontal.sqrMagnitude > 1e-6f)
            {
                // Follow the ground plane so walking downhill does not launch the apple.
                Vector3 along = Vector3.ProjectOnPlane(horizontal, GroundNormal);
                if (along.sqrMagnitude > 1e-6f) horizontal = along.normalized * horizontal.magnitude;
            }
            float vyBeforeMove = _vy;
            // Average of start/end velocity = exact integration under constant gravity, so jump height
            // does not depend on frame rate.
            float vyMove = 0.5f * (vyStart + _vy);
            CollisionFlags flags = Controller.Move((horizontal + Up * vyMove) * dt);

            if ((flags & CollisionFlags.Above) != 0 && _vy > 0f)
            {
                _vy = 0f; // bonk: start falling immediately
                _jumpRising = false;
            }

            // 7. Ground ---------------------------------------------------------------
            bool grounded = (flags & CollisionFlags.Below) != 0;
            bool probe = ProbeGround(c.groundSnapDistance, out RaycastHit groundHit);
            if (grounded && probe)
            {
                GroundNormal = groundHit.normal;
                if (Vector3.Angle(groundHit.normal, Up) > c.maxWalkableSlope) grounded = false;
            }
            else if (grounded)
            {
                GroundNormal = Up;
            }

            if (!grounded && wasGrounded && !jumpedThisStep && _vy <= 0f && probe &&
                Vector3.Angle(groundHit.normal, Up) <= c.maxWalkableSlope)
            {
                // Ground snap: stepped off a small ledge / slope crest — stick to the ground.
                Controller.Move(Vector3.down * (c.groundSnapDistance + Controller.skinWidth));
                grounded = Controller.isGrounded;
                if (grounded) GroundNormal = groundHit.normal;
            }

            // 8. Events ---------------------------------------------------------------
            if (grounded && !wasGrounded)
            {
                _ballistic = false;
                LastLandingSpeed = Mathf.Max(0f, -vyBeforeMove);
                _jumpRising = false;
                AirTime = 0f;
                IsGrounded = true;
                Landed?.Invoke(LastLandingSpeed);
            }
            else if (!grounded && wasGrounded && !jumpedThisStep)
            {
                IsGrounded = false;
                LeftGround?.Invoke();
            }
            IsGrounded = grounded;
            if (!grounded) AirTime += dt;
        }

        /// <summary>Sphere-cast from the bottom of the capsule.</summary>
        bool ProbeGround(float extraDistance, out RaycastHit hit)
        {
            var cc = Controller;
            float r = cc.radius * 0.9f;
            Vector3 bottomSphere = transform.position + cc.center + Up * (-cc.height * 0.5f + cc.radius);
            Vector3 origin = bottomSphere + Up * 0.05f;
            float dist = 0.05f + (cc.radius - r) + cc.skinWidth + extraDistance + 0.02f;
            return PhysicsUtil.SphereCastIgnoring(origin, r, Vector3.down, dist, transform, out hit);
        }

        float _lastDt = 1f / 60f;

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var pushable = hit.collider.GetComponent<Dream.IPushable>();
            if (pushable != null && IsGrounded && Mathf.Abs(hit.normal.y) < 0.5f)
            {
                pushable.Push(-hit.normal, _lastDt, transform);
                return; // keep walking speed while shoving
            }
            // Remove the part of horizontal velocity that pushes into walls / too-steep slopes,
            // so speed is not "stored" while blocked (MOV-09).
            Vector3 n = hit.normal;
            if (n.y < -0.1f) return; // ceiling — handled via CollisionFlags.Above
            if (Vector3.Angle(n, Up) <= config.maxWalkableSlope + 1f) return; // walkable floor
            Vector3 nPlanar = Vector3.ProjectOnPlane(n, Up);
            if (nPlanar.sqrMagnitude < 1e-6f) return;
            nPlanar.Normalize();
            float into = Vector3.Dot(_planar, nPlanar);
            if (into < 0f) _planar -= nPlanar * into;
        }

        // ------------------------------------------------------------------ external control
        /// <summary>Launch upward/outward (bouncy cushion, knockback). Not affected by jump-cut.</summary>
        public void Launch(Vector3 velocity)
        {
            _planar = Vector3.ProjectOnPlane(velocity, Up);
            _vy = velocity.y;
            _jumpRising = false;
            if (_vy > 0f) IsGrounded = false;
        }

        bool _ballistic;
        float _dashTime;
        Vector3 _dashVel;
        /// <summary>Planar speed kept after an air dash (momentum tech: sprint-jump-dash).</summary>
        public float DashCarrySpeed = 11f;
        /// <summary>&gt;0 clamps the fall speed (glide). Reset by the owner every frame.</summary>
        public float FallSpeedOverride { get; set; }
        /// <summary>Extra vertical acceleration while airborne (updrafts). Reset by the owner every frame.</summary>
        public float ExternalVerticalAccel { get; set; }
        public bool IsDashing => _dashTime > 0f;
        /// <summary>Non-zero = face this way instead of the move direction (lock-on). Owner sets it every frame.</summary>
        public Vector3 FaceDirection { get; set; }
        /// <summary>Scales the walk/run target speed (attacks slow you down). Owner sets it every frame.</summary>
        public float MoveSpeedScale { get; set; } = 1f;

        /// <summary>Straight-line burst along <paramref name="dir"/> (planar) for <paramref name="duration"/> s.</summary>
        public void Dash(Vector3 dir, float speed, float duration)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
            _dashVel = dir.normalized * speed;
            _dashTime = duration;
            _jumpRising = false;
            transform.rotation = Quaternion.LookRotation(_dashVel.normalized, Up);
        }

        /// <summary>Overwrite velocity (double jump, ground pound).</summary>
        public void SetVelocity(Vector3 v)
        {
            _planar = Vector3.ProjectOnPlane(v, Up);
            _vy = v.y;
            _jumpRising = false;
            _dashTime = 0f;
            if (_vy > 0f) IsGrounded = false;
        }
        public bool IsBallistic => _ballistic;

        /// <summary>Launch-pad flight: like <see cref="Launch"/> but air drag is off until landing so the arc is exact.</summary>
        public void LaunchBallistic(Vector3 velocity)
        {
            Launch(velocity);
            _ballistic = true;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool was = Controller.enabled;
            Controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            Controller.enabled = was;
            StopImmediately();
            _ballistic = false;
            _dashTime = 0f;
            IsGrounded = false;
            _assist?.Reset();
        }

        public void StopImmediately()
        {
            _planar = Vector3.zero;
            _vy = 0f;
            _jumpRising = false;
        }
    }
}
