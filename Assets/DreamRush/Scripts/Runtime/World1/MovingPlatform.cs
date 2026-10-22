using DreamRush.Player;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// Kinematic platform that carries the player (the CharacterController does not do this by itself).
    /// Motion: ping-pong between the start and <see cref="offset"/>, plus optional spin around local up.
    /// Runs before the player motor so the carry delta is applied in the same frame.
    /// Subclasses (PopUpPage, CrumblePlatform) drive <see cref="ExternalTarget"/> instead of the ping-pong.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 offset = Vector3.zero;
        public float period = 4f;
        public float phase;
        public float spinDegPerSec;
        [Header("Orbit (Toy Galaxy islands)")]
        public Vector3 orbitCenter;
        public float orbitRadius;
        public float orbitDegPerSec;
        public float orbitPhaseDeg;
        public float carryRadius = 0f;           // 0 = use collider bounds
        public bool AutoMove = true;

        protected Vector3 _start;
        protected float _t;
        protected PlayerMotor3D _player;
        Collider _col;
        Vector3 _lastPos;
        Quaternion _lastRot;
        /// <summary>When set, the platform moves toward this world position instead of ping-ponging.</summary>
        public Vector3? ExternalTarget;

        public bool PlayerOnTop { get; private set; }

        protected virtual void Awake()
        {
            _start = transform.position;
            _col = GetComponentInChildren<Collider>();
            _lastPos = transform.position; _lastRot = transform.rotation;
        }

        protected virtual void Start() { _player = FindFirstObjectByType<PlayerMotor3D>(); }

        protected virtual void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _t += dt;
            if (AutoMove)
            {
                if (orbitRadius > 0f)
                {
                    float a = (orbitPhaseDeg + orbitDegPerSec * _t) * Mathf.Deg2Rad;
                    transform.position = orbitCenter + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * orbitRadius + Vector3.up * Mathf.Sin(_t * 1.3f + orbitPhaseDeg) * 0.25f;
                }
                else if (offset.sqrMagnitude > 1e-6f)
                {
                    float k = 0.5f - 0.5f * Mathf.Cos((_t / Mathf.Max(0.1f, period) + phase) * Mathf.PI * 2f);
                    transform.position = _start + offset * k;
                }
                if (spinDegPerSec != 0f) transform.Rotate(0f, spinDegPerSec * dt, 0f, Space.World);
            }
            Carry();
        }

        protected void Carry()
        {
            Vector3 dp = transform.position - _lastPos;
            Quaternion dr = transform.rotation * Quaternion.Inverse(_lastRot);
            PlayerOnTop = IsPlayerOnTop();
            if (PlayerOnTop && _player != null && _player.Controller != null && _player.Controller.enabled)
            {
                Vector3 p = _player.transform.position;
                Vector3 rotated = _lastPos + dr * (p - _lastPos);
                Vector3 delta = (rotated - p) + dp;
                if (delta.sqrMagnitude > 1e-8f) _player.Controller.Move(delta);
                float yaw = dr.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(0, yaw)) > 0.001f) _player.transform.Rotate(0, yaw, 0, Space.World);
            }
            _lastPos = transform.position; _lastRot = transform.rotation;
        }

        protected bool IsPlayerOnTop()
        {
            if (_player == null || !_player.IsGrounded || _col == null) return false;
            var b = _col.bounds;
            Vector3 p = _player.transform.position;
            if (carryRadius > 0f)
            {
                Vector3 d = p - transform.position; d.y = 0;
                return d.magnitude < carryRadius && p.y > b.max.y - 0.35f && p.y < b.max.y + 0.6f;
            }
            return p.x > b.min.x - 0.3f && p.x < b.max.x + 0.3f && p.z > b.min.z - 0.3f && p.z < b.max.z + 0.3f &&
                   p.y > b.max.y - 0.35f && p.y < b.max.y + 0.6f;
        }
    }
}
