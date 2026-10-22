using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>On the player: finds rails, snaps on, rides, hops off. Runs after the motor.</summary>
    [DefaultExecutionOrder(40)]
    public class GrindController : MonoBehaviour
    {
        public GrindRail Rail { get; private set; }
        public bool Grinding => Rail != null;
        public float Speed { get; private set; }
        public int RailsRidden { get; private set; }
        PlayerMotor3D _m; IPlayerCommandSource _src;
        float _s, _dir, _cooldown;
        TrailRenderer _sparks;

        void Awake() { _m = GetComponent<PlayerMotor3D>(); _src = GetComponent<IPlayerCommandSource>(); }

        void Update()
        {
            float dt = Time.deltaTime;
            _cooldown -= dt;
            var src = _m.CommandSource ?? _src;
            var cmd = src != null && _m.InputEnabled ? src.ReadCommand() : PlayerCommand.None;
            if (Rail == null)
            {
                if (_cooldown > 0f || _m.VerticalVelocity > 2f) return;
                foreach (var r in GrindRail.All)
                {
                    float s = r.Closest(transform.position, out float d);
                    if (d > r.snapRadius) continue;
                    Vector3 p = r.PointAt(s, out var tan);
                    if (transform.position.y < p.y - 0.5f) continue;   // only from above / level
                    Attach(r, s, tan);
                    break;
                }
                return;
            }
            // riding
            Vector3 pos = Rail.PointAt(_s, out var t);
            Vector3 dir = t * _dir;
            Speed = Mathf.Clamp(Speed - dir.y * 18f * dt + 2.5f * dt, Rail.minSpeed, Rail.maxSpeed);   // downhill speeds you up
            _s += _dir * Speed * dt;
            pos = Rail.PointAt(_s, out t); dir = t * _dir;
            transform.position = pos + Vector3.up * 0.08f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z).normalized + Vector3.forward * 0.0001f);
            _m.SetVelocity(dir * Speed);
            if (Mathf.Repeat(Time.time, 0.06f) < dt) VFX.Play(VFXKind.StarBurst, transform.position, 0.25f, VFX.Gold);
            bool end = _s <= 0f || _s >= Rail.Length;
            if (cmd.JumpPressed || end)
            {
                Vector3 v = dir * Speed + Vector3.up * (cmd.JumpPressed ? _m.Config.JumpVelocity * 1.05f : 3f);
                Detach();
                _m.LaunchBallistic(v);
                if (cmd.JumpPressed) { Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 1f); VFX.Play(VFXKind.LandingRing, transform.position, 0.9f, VFX.Lavender); }
            }
        }

        void Attach(GrindRail r, float s, Vector3 tan)
        {
            Rail = r; _s = s;
            Vector3 v = _m.Velocity; v.y = 0;
            _dir = Vector3.Dot(v.sqrMagnitude > 0.1f ? v : transform.forward, tan) >= 0f ? 1f : -1f;
            Speed = Mathf.Max(r.minSpeed, v.magnitude);
            _m.AutoStep = false;
            RailsRidden++;
            Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 0.8f);
            GrindLoop(true);
            VFX.Play(VFXKind.ImpactRing, transform.position, 0.8f, VFX.Lavender);
            CameraShake.Add(0.12f);
            WonderDirector.Novelty("grind");
        }

        public void Detach()
        {
            Rail = null; _cooldown = 0.35f;
            _m.AutoStep = true;
            GrindLoop(false);
        }

        // M15: a looping metal hiss while riding (pitch follows speed)
        AudioSource _loop;
        void GrindLoop(bool on)
        {
            if (on && _loop == null)
            {
                var clip = Resources.Load<AudioClip>("DreamRush/Audio/SFX_Grind"); if (clip == null) return;
                _loop = gameObject.AddComponent<AudioSource>(); _loop.clip = clip; _loop.loop = true; _loop.spatialBlend = 0f; _loop.playOnAwake = false;
            }
            if (_loop == null) return;
            if (on) { _loop.volume = 0.32f; _loop.Play(); } else _loop.Stop();
        }

        void LateUpdate() { if (_loop != null && _loop.isPlaying && Rail != null) _loop.pitch = Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(Rail.minSpeed, Rail.maxSpeed, Speed)); }

        void OnDisable() { if (Rail != null) Detach(); }
    }
}
