using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// Bounce / launch cushion (Ch1 catapult pages, Ch3 Toy Galaxy). When Pip lands on it, it squashes and fires
    /// a ballistic arc that lands exactly on <see cref="target"/> (readable — the arc is shown by guide sparkles
    /// built along it). With no target it is a plain bounce (straight up, <see cref="bounceHeight"/>).
    /// A wind-up (<see cref="windup"/>) makes catapults readable: the page shakes, then snaps.
    /// </summary>
    public class LaunchPad : MonoBehaviour
    {
        public Transform target;
        public float apexHeight = 6f;
        public float bounceHeight = 5f;
        public float radius = 1.3f;
        public float windup = 0f;
        public Transform visual;
        public float cooldown = 0.6f;
        public int Launches { get; private set; }

        PlayerMotor3D _player;
        float _cool, _charge, _squash;
        bool _charging;

        void Start() => _player = FindFirstObjectByType<PlayerMotor3D>();

        /// <summary>Velocity that reaches <paramref name="to"/> from <paramref name="from"/> peaking <paramref name="apex"/> above the higher end.</summary>
        public static Vector3 ArcVelocity(Vector3 from, Vector3 to, float apex, float gravity, float fallGravity)
        {
            float top = Mathf.Max(from.y, to.y) + apex;
            float vy = Mathf.Sqrt(2f * gravity * (top - from.y));
            float tUp = vy / gravity;
            float tDown = Mathf.Sqrt(2f * Mathf.Max(0.01f, top - to.y) / fallGravity);
            Vector3 flat = to - from; flat.y = 0f;
            return flat / (tUp + tDown) + Vector3.up * vy;
        }

        public float RiseGravity => _player != null && _player.Config != null ? _player.Config.gravity : 24f;
        public float FallGravity => _player != null && _player.Config != null ? _player.Config.gravity * _player.Config.fallGravityMultiplier : 32f;

        void Update()
        {
            float dt = Time.deltaTime;
            _cool -= dt;
            _squash = Mathf.MoveTowards(_squash, 0f, dt * 4f);
            if (visual != null)
            {
                float s = _charging ? 1f - 0.25f * (_charge / Mathf.Max(0.01f, windup)) : 1f - 0.35f * Mathf.Sin(_squash * Mathf.PI);
                visual.localScale = new Vector3(2f - s, s, 2f - s);
                visual.localPosition = _charging ? new Vector3(Mathf.Sin(Time.time * 70f) * 0.04f, 0, 0) : Vector3.zero;
            }
            if (_player == null || _cool > 0f) return;
            Vector3 d = _player.transform.position - transform.position;
            bool on = _player.IsGrounded && new Vector2(d.x, d.z).magnitude < radius && d.y > -0.2f && d.y < 1.0f;
            if (!on) { _charging = false; _charge = 0f; return; }
            if (windup > 0f)
            {
                if (!_charging) { _charging = true; _charge = 0f; Audio.AudioManager.Play(Audio.Sfx.CardDeal, transform.position, 0.5f); }
                _charge += dt;
                if (_charge < windup) return;
            }
            Fire();
        }

        public void Fire()
        {
            _charging = false; _charge = 0f; _cool = cooldown; _squash = 1f; Launches++;
            Palace.WonderDirector.Novelty("launch pad");
            Vector3 from = _player.transform.position;
            Vector3 v = target != null ? ArcVelocity(from, target.position, apexHeight, RiseGravity, FallGravity) : Vector3.up * Mathf.Sqrt(2f * RiseGravity * bounceHeight);
            if (target != null) _player.LaunchBallistic(v); else _player.Launch(v);
            VFX.Play(VFXKind.DustPuff, transform.position + Vector3.up * 0.2f, 1.4f);
            VFX.Play(VFXKind.LandingRing, transform.position + Vector3.up * 0.05f, 1.2f, VFX.Lavender);
            Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 1f);
            CameraShake.Add(0.12f);
        }
    }
}
