using DreamRush.Boss;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// Wind-up toy car that rolls a lane during the chase (pitch: "hitting obstacles loses distance").
    /// Readable and fair: slow, squeaks as it turns, low enough to jump over. A hit knocks the apple back and
    /// costs one Sleep moon — and the chair catches its breath (BossEncounter fatigue pushback).
    /// </summary>
    public class RollingToyHazard : MonoBehaviour
    {
        public Vector3 from, to;          // world positions of the lane ends
        public float speed = 3.2f;
        public float hitRadius = 0.75f;
        public float jumpClearance = 0.55f; // feet above this height (relative to the toy) = safe
        public bool AutoStep = true;

        public int Hits { get; private set; }
        public bool Active { get; private set; }

        PlayerMotor3D _player;
        BossEncounter _encounter;
        SleepStability _stability;
        float _u, _dir = 1f, _cooldown;
        Transform _visual;

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            _encounter = FindFirstObjectByType<BossEncounter>();
            _stability = FindFirstObjectByType<SleepStability>();
            _visual = transform.childCount > 0 ? transform.GetChild(0) : null;
            transform.position = from;
        }

        void Update() { if (AutoStep) Step(Time.deltaTime); }

        /// <summary>Tests can force activity without a boss encounter.</summary>
        public bool ForceActive;

        public void Step(float dt)
        {
            Active = ForceActive || (_encounter != null && _encounter.CurrentPhase == BossEncounter.Phase.Fight);
            if (!Active || dt <= 0f) return;
            float len = Mathf.Max(0.1f, Vector3.Distance(from, to));
            _u += _dir * speed * dt / len;
            if (_u >= 1f || _u <= 0f)
            {
                _u = Mathf.Clamp01(_u); _dir = -_dir;
                Audio.AudioManager.Play(Audio.Sfx.ChairYelp, transform.position, 0.25f); // little squeak at the turn
            }
            Vector3 heading = (to - from).normalized * _dir;
            transform.position = Vector3.Lerp(from, to, _u);
            if (heading.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(heading);
            if (_visual != null) _visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.03f, 0);

            _cooldown -= dt;
            if (_player == null || _cooldown > 0f) return;
            Vector3 d = _player.transform.position - transform.position;
            float height = d.y; d.y = 0f;
            if (d.magnitude > hitRadius || height > jumpClearance) return;

            // bonk
            _cooldown = 1.5f;
            Hits++;
            Vector3 away = d.sqrMagnitude > 0.001f ? d.normalized : -transform.forward;
            _player.Launch(away * 6f + Vector3.up * 5f);
            if (_stability != null) _stability.Damage("bumped by a toy car");
            var fx = Presentation.FXLibrary.Instance;
            if (fx != null) Presentation.WorldFX.Spawn(fx.ouch, _player.transform.position + Vector3.up * 1.6f, 0.9f, 0.8f, _player.transform);
            Audio.AudioManager.Play(Audio.Sfx.Kick, transform.position, 0.7f);
        }
    }
}
