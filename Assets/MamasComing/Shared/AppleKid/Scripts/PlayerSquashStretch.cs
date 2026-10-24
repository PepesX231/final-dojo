using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Procedural game-feel layer on the visual root (GDD §20.3): jump stretch, landing squash,
    /// acceleration lean, brake lean, turn bank. Purely cosmetic — never feeds back into movement.
    /// Volume-preserving scale so the apple keeps its mass.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public class PlayerSquashStretch : MonoBehaviour
    {
        public PlayerMotor3D motor;

        [Header("Squash / Stretch")]
        public float jumpStretch = 1.18f;
        public float maxLandSquash = 0.78f;
        [Tooltip("Impact speed (m/s) that produces the maximum squash.")]
        public float landSpeedForMaxSquash = 14f;
        public float springStiffness = 260f;
        public float springDamping = 16f;

        [Header("Lean")]
        public float accelLeanDegrees = 9f;
        public float brakeLeanDegrees = 12f;
        public float bankDegrees = 10f;
        public float leanSmoothing = 12f;

        float _scaleY = 1f, _scaleVel;
        Vector3 _lastPlanar;
        float _leanPitch, _leanRoll;
        Quaternion _baseLocalRot;
        Vector3 _baseLocalScale;
        float _lastYaw;

        public float CurrentScaleY => _scaleY;

        void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor3D>();
            _baseLocalRot = transform.localRotation;
            _baseLocalScale = transform.localScale;
        }

        void OnEnable()
        {
            if (motor == null) return;
            motor.Jumped += OnJumped;
            motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (motor == null) return;
            motor.Jumped -= OnJumped;
            motor.Landed -= OnLanded;
        }

        void OnJumped() => _scaleY = jumpStretch;

        void OnLanded(float impact)
        {
            float t = Mathf.Clamp01(impact / landSpeedForMaxSquash);
            _scaleY = Mathf.Lerp(0.94f, maxLandSquash, t);
        }

        void LateUpdate()
        {
            if (motor == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Damped spring back to 1 (sub-stepped: a stiff spring integrated with one big step
            // goes unstable at low frame rates and pins the kid at the 0.6 / 1.4 clamps).
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.008f), 1, 20);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float force = -springStiffness * (_scaleY - 1f) - springDamping * _scaleVel;
                _scaleVel += force * h;
                _scaleY += _scaleVel * h;
            }
            _scaleY = Mathf.Clamp(_scaleY, 0.6f, 1.4f);

            // Lean from planar acceleration in local space.
            Vector3 planar = motor.PlanarVelocity;
            Vector3 accel = (planar - _lastPlanar) / dt;
            _lastPlanar = planar;
            Vector3 localAccel = motor.transform.InverseTransformDirection(accel);
            float fwdAccel = Mathf.Clamp(localAccel.z / 30f, -1f, 1f);
            float targetPitch = fwdAccel >= 0f ? fwdAccel * accelLeanDegrees : fwdAccel * brakeLeanDegrees;

            float yaw = motor.transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / dt;
            _lastYaw = yaw;
            float speed01 = Mathf.Clamp01(planar.magnitude / 8f);
            float targetRoll = Mathf.Clamp(-yawRate / 720f, -1f, 1f) * bankDegrees * speed01;

            if (!motor.IsGrounded) { targetPitch *= 0.3f; targetRoll *= 0.3f; }
            float k = 1f - Mathf.Exp(-leanSmoothing * dt);
            _leanPitch = Mathf.Lerp(_leanPitch, targetPitch, k);
            _leanRoll = Mathf.Lerp(_leanRoll, targetRoll, k);

            float xz = 1f / Mathf.Sqrt(Mathf.Max(0.2f, _scaleY));
            transform.localScale = Vector3.Scale(_baseLocalScale, new Vector3(xz, _scaleY, xz));
            transform.localRotation = _baseLocalRot * Quaternion.Euler(_leanPitch, 0f, _leanRoll);
        }
    }
}
