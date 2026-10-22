using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.CameraSystem
{
    /// <summary>
    /// Curated dream-stage camera (GDD §10.2–10.3): fixed 3/4 perspective that follows the player with
    /// look-ahead and damping, limited yaw "peek" with mouse/stick, zone overrides per section, and boss
    /// framing that keeps a second target (the chair) in view.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class StageCameraController : MonoBehaviour
    {
        [Header("Targets")]
        public Transform target;
        public PlayerInputReader input;
        [Tooltip("Optional second subject to keep in frame (boss).")]
        public Transform secondaryTarget;

        [Header("Framing (GDD §10.2 starting values)")]
        public float fieldOfView = 45f;
        public float yaw = 0f;
        public float pitch = 30f;
        public float distance = 13.9f; // = back 12, up 7
        public float focusHeight = 0.8f;
        public float lookAhead = 2f;
        public float lookAheadTime = 0.35f;
        public float damping = 0.18f;

        [Header("Peek")]
        public float maxYawNudge = 25f;
        public float maxPitchNudge = 10f;
        public float nudgeReturnSpeed = 30f;
        public float mouseSensitivity = 0.08f;
        public float stickSensitivity = 70f;

        [Header("Boss framing")]
        public float secondaryWeight = 0.35f;
        public float maxExtraDistance = 5f;

        public bool InputEnabled { get; set; } = true;
        public bool lockCursor = true;
        public StageCameraZone ActiveZone { get; private set; }

        static readonly List<StageCameraZone> Zones = new List<StageCameraZone>();
        public static void RegisterZone(StageCameraZone z) { if (!Zones.Contains(z)) Zones.Add(z); }
        public static void UnregisterZone(StageCameraZone z) => Zones.Remove(z);

        Camera _cam;
        Vector3 _vel, _look, _lookVel;
        float _yawNudge, _pitchNudge, _lastNudge;
        float _curYaw, _curPitch, _curDist, _curFov;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _curYaw = yaw; _curPitch = pitch; _curDist = distance; _curFov = fieldOfView;
        }

        void Start() => Snap();

        public void Snap()
        {
            if (target == null) return;
            PickZone();
            ApplyZoneInstant();
            transform.SetPositionAndRotation(DesiredPosition(Focus()), Rotation());
            _vel = Vector3.zero;
        }

        Quaternion Rotation() => Quaternion.Euler(_curPitch + _pitchNudge, _curYaw + _yawNudge, 0f);

        Vector3 Focus()
        {
            Vector3 f = target.position + Vector3.up * focusHeight + _look;
            if (secondaryTarget != null && secondaryTarget.gameObject.activeInHierarchy)
                f = Vector3.Lerp(f, secondaryTarget.position + Vector3.up * focusHeight, secondaryWeight);
            return f;
        }

        Vector3 DesiredPosition(Vector3 focus)
        {
            float extra = 0f;
            if (secondaryTarget != null && secondaryTarget.gameObject.activeInHierarchy)
                extra = Mathf.Clamp(Vector3.Distance(target.position, secondaryTarget.position) * 0.45f - 2f, 0f, maxExtraDistance);
            return focus - Rotation() * Vector3.forward * (_curDist + extra);
        }

        void PickZone()
        {
            StageCameraZone best = null;
            int bestPriority = int.MinValue;
            foreach (var z in Zones)
                if (z != null && z.Contains(target.position) && z.priority > bestPriority) { best = z; bestPriority = z.priority; }
            ActiveZone = best;
        }

        void ApplyZoneInstant()
        {
            _curYaw = ActiveZone ? ActiveZone.yaw : yaw;
            _curPitch = ActiveZone ? ActiveZone.pitch : pitch;
            _curDist = ActiveZone ? ActiveZone.distance : distance;
            _curFov = ActiveZone ? ActiveZone.fieldOfView : fieldOfView;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            PickZone();
            float k = 1f - Mathf.Exp(-2.5f * dt); // smooth zone transitions (no sudden rotations, §10.3)
            _curYaw = Mathf.LerpAngle(_curYaw, ActiveZone ? ActiveZone.yaw : yaw, k);
            _curPitch = Mathf.Lerp(_curPitch, ActiveZone ? ActiveZone.pitch : pitch, k);
            _curDist = Mathf.Lerp(_curDist, ActiveZone ? ActiveZone.distance : distance, k);
            _curFov = Mathf.Lerp(_curFov, ActiveZone ? ActiveZone.fieldOfView : fieldOfView, k);
            if (_cam != null) _cam.fieldOfView = _curFov;

            if (InputEnabled && lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

            // peek nudge
            if (InputEnabled && input != null)
            {
                Vector2 d = Vector2.ClampMagnitude(input.LookMouseDelta, 80f) * mouseSensitivity + input.LookStick * (stickSensitivity * dt);
                if (d.sqrMagnitude > 1e-6f) _lastNudge = Time.time;
                _yawNudge = Mathf.Clamp(_yawNudge + d.x, -maxYawNudge, maxYawNudge);
                _pitchNudge = Mathf.Clamp(_pitchNudge - d.y, -maxPitchNudge, maxPitchNudge);
            }
            if (Time.time - _lastNudge > 0.8f)
            {
                _yawNudge = Mathf.MoveTowards(_yawNudge, 0f, nudgeReturnSpeed * dt);
                _pitchNudge = Mathf.MoveTowards(_pitchNudge, 0f, nudgeReturnSpeed * dt);
            }

            // look-ahead in movement direction
            var motor = target.GetComponent<PlayerMotor3D>();
            Vector3 desiredLook = motor != null ? Vector3.ClampMagnitude(motor.PlanarVelocity * lookAheadTime, lookAhead) : Vector3.zero;
            _look = Vector3.SmoothDamp(_look, desiredLook, ref _lookVel, 0.35f, Mathf.Infinity, dt);

            Vector3 desired = DesiredPosition(Focus());
            transform.position -= _shake; // smoothing runs on the un-shaken pose
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, damping, Mathf.Infinity, dt);
            transform.rotation = Rotation();
            _shake = Presentation.CameraShake.Offset(transform);
            transform.position += _shake;
        }

        Vector3 _shake;

        public void ReleaseCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
