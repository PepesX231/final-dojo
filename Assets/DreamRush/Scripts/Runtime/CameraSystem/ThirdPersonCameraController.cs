using DreamRush.Core;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.CameraSystem
{
    /// <summary>
    /// Free third-person orbit camera for the Bedroom Hub and the movement gym (GDD §7.3, §10.1).
    /// Smooth follow, mouse/right-stick orbit, pitch clamp, sphere-cast collision (fast in / eased out).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;
        public PlayerInputReader input;
        public float targetHeight = 0.8f;

        [Header("Framing (GDD §7.3 starting values)")]
        public float fieldOfView = 50f;
        public float distance = 5.5f;
        public float minDistance = 0.9f;
        public float pitchMin = -15f;
        public float pitchMax = 55f;
        public float defaultPitch = 18f;

        [Header("Input")]
        [Tooltip("Degrees per mouse pixel.")]
        public float mouseSensitivity = 0.12f;
        [Tooltip("Degrees per second at full stick.")]
        public float stickSensitivity = 160f;
        public bool invertY;
        public bool lockCursor = true;

        [Header("Smoothing")]
        public float followSmoothTime = 0.08f;
        [Tooltip("How quickly the camera returns outward after an obstruction clears (units/sec).")]
        public float collisionRecoverSpeed = 4f;
        public float collisionRadius = 0.25f;

        [Header("Assist")]
        [Tooltip("Gently rotates behind the player when moving without camera input. 0 = off.")]
        public float autoFollowYawSpeed = 0f;
        [Tooltip("Seconds without camera input before auto-follow starts.")]
        public float autoFollowDelay = 1.5f;

        [Header("Platformer framing (docs/RESEARCH_V2.md §5)")]
        [Tooltip("Ignore jump height: the pivot only rises when grounded higher or after rising this far.")]
        public float verticalDeadzone = 0f;
        [Tooltip("Extra distance while sprinting (shows more of where you run).")]
        public float sprintExtraDistance = 0f;
        [Tooltip("Optional subject to frame (boss). Hold Focus to turn toward it; within focusRadius the camera also widens.")]
        public Transform focusTarget;
        public float focusRadius = 22f;
        public float focusTurnSpeed = 240f;
        public float focusMaxExtraDistance = 3f;

        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public bool InputEnabled { get; set; } = true;
        public float CurrentDistance => _currentDistance;

        Camera _cam;
        Vector3 _pivot;
        Vector3 _pivotVel;
        float _groundY, _extraDistance;
        bool _groundInit;
        float _currentDistance;
        float _lastLookInputTime;
        int _ignoreLookFrames = 3;
        bool _wasLocked;

        [Tooltip("Mouse delta above this (pixels/frame) is treated as a cursor-warp spike and clamped.")]
        public float maxMouseDeltaPerFrame = 120f;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            Pitch = defaultPitch;
            if (target != null) Yaw = target.eulerAngles.y;
            _currentDistance = distance;
        }

        void Start() => SnapToTarget();

        public void SnapToTarget()
        {
            if (target == null) return;
            _pivot = target.position + Vector3.up * targetHeight;
            _pivotVel = Vector3.zero;
            _groundY = target.position.y;
            _groundInit = true;
            _extraDistance = 0f;
            _currentDistance = distance;
            Apply(0f, snap: true);
        }

        // ---------------------------------------------------------------- focus override (menus, cutscenes)
        Transform _override;
        float _overrideWeight;   // 0 = orbit camera, 1 = override pose
        Vector3 _orbitPos;
        Quaternion _orbitRot;
        public float overrideBlendSpeed = 5f;
        public bool HasOverride => _override != null;

        /// <summary>Smoothly moves the camera to <paramref name="pose"/> (GDD §8.3). Null clears.</summary>
        public void SetOverride(Transform pose) => _override = pose;
        public void ClearOverride() => _override = null;

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeScale > 0f) dt = Time.deltaTime;

            if (lockCursor && InputEnabled && _override == null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            // Locking/unlocking the cursor (or regaining focus) produces one huge delta; skip a few frames.
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (locked != _wasLocked || !Application.isFocused) _ignoreLookFrames = 3;
            _wasLocked = locked;

            if (InputEnabled && input != null)
            {
                Vector2 rawMouse = Vector2.ClampMagnitude(input.LookMouseDelta, maxMouseDeltaPerFrame);
                if (_ignoreLookFrames > 0) { _ignoreLookFrames--; rawMouse = Vector2.zero; }
                Vector2 mouse = rawMouse * mouseSensitivity;
                Vector2 stick = input.LookStick * (stickSensitivity * dt);
                Vector2 look = mouse + stick;
                if (look.sqrMagnitude > 1e-6f) _lastLookInputTime = Time.time;
                Yaw += look.x;
                Pitch += (invertY ? look.y : -look.y);
            }
            Pitch = Mathf.Clamp(Pitch, pitchMin, pitchMax);

            var tmotor = target.GetComponent<PlayerMotor3D>();
            // boss focus: hold to swing behind the apple toward the chair (Z-target spirit)
            bool focusing = focusTarget != null && focusTarget.gameObject.activeInHierarchy && input != null && input.FocusHeld && InputEnabled;
            if (focusing)
            {
                Vector3 to = focusTarget.position - target.position; to.y = 0f;
                if (to.sqrMagnitude > 0.5f)
                {
                    Yaw = Mathf.MoveTowardsAngle(Yaw, Quaternion.LookRotation(to).eulerAngles.y, focusTurnSpeed * dt);
                    Pitch = Mathf.MoveTowards(Pitch, Mathf.Max(defaultPitch, 22f), 60f * dt);
                    _lastLookInputTime = Time.time;
                }
            }
            if (autoFollowYawSpeed > 0f && Time.time - _lastLookInputTime > autoFollowDelay)
            {
                var motor = target.GetComponent<PlayerMotor3D>();
                if (motor != null && motor.PlanarVelocity.sqrMagnitude > 1f)
                {
                    float desiredYaw = Quaternion.LookRotation(motor.PlanarVelocity).eulerAngles.y;
                    Yaw = Mathf.MoveTowardsAngle(Yaw, desiredYaw, autoFollowYawSpeed * dt);
                }
            }

            Vector3 desiredPivot = target.position + Vector3.up * targetHeight;
            if (verticalDeadzone > 0f && tmotor != null)
            {
                // platformer boom: jumps don't bob the camera; landing higher (or falling) does
                if (!_groundInit || tmotor.IsGrounded) { _groundY = target.position.y; _groundInit = true; }
                float y = target.position.y;
                float pivotY = _groundY;
                if (y > _groundY + verticalDeadzone) pivotY = y - verticalDeadzone;
                if (y < _groundY) pivotY = y;
                desiredPivot.y = pivotY + targetHeight;
            }
            float wantExtra = 0f;
            if (tmotor != null && tmotor.IsSprinting) wantExtra += sprintExtraDistance;
            if (focusTarget != null && focusTarget.gameObject.activeInHierarchy)
            {
                float sep = Vector3.Distance(target.position, focusTarget.position);
                if (sep < focusRadius) wantExtra += Mathf.Clamp01(sep / focusRadius) * focusMaxExtraDistance;
            }
            _extraDistance = Mathf.MoveTowards(_extraDistance, wantExtra, 2.5f * dt);
            _pivot = Vector3.SmoothDamp(_pivot, desiredPivot, ref _pivotVel, followSmoothTime, Mathf.Infinity, dt);
            Apply(dt, snap: false);

            float targetW = _override != null ? 1f : 0f;
            _overrideWeight = Mathf.MoveTowards(_overrideWeight, targetW, overrideBlendSpeed * Time.unscaledDeltaTime);
            if (_overrideWeight > 0f)
            {
                _orbitPos = transform.position; _orbitRot = transform.rotation;
                if (_override != null) { _lastOverridePos = _override.position; _lastOverrideRot = _override.rotation; }
                float w = Mathf.SmoothStep(0f, 1f, _overrideWeight);
                transform.SetPositionAndRotation(Vector3.Lerp(_orbitPos, _lastOverridePos, w),
                    Quaternion.Slerp(_orbitRot, _lastOverrideRot, w));
            }
            transform.position += Presentation.CameraShake.Offset(transform); // pose is rebuilt from the pivot every frame
        }
        Vector3 _lastOverridePos;
        Quaternion _lastOverrideRot;

        void Apply(float dt, bool snap)
        {
            if (_cam != null) _cam.fieldOfView = fieldOfView;
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;

            // Collision: cast from pivot toward desired camera position.
            float want = distance + _extraDistance;
            float allowed = want;
            bool blocked = PhysicsUtil.SphereCastIgnoring(_pivot, collisionRadius, back, want, target, out RaycastHit hit);
            if (blocked) allowed = Mathf.Max(minDistance, hit.distance - 0.05f);

            if (snap || (blocked && allowed < _currentDistance)) _currentDistance = allowed; // obstruction: pull in instantly
            else _currentDistance = Mathf.MoveTowards(_currentDistance, allowed, collisionRecoverSpeed * dt);

            transform.SetPositionAndRotation(_pivot + back * _currentDistance, rot);
        }

        public void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDisable() => ReleaseCursor();
    }
}
