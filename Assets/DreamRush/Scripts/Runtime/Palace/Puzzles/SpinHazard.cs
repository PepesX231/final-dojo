using System.Collections.Generic;
using DreamRush.Combat;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17: a giant rooftop fan / clock hand that sweeps around and knocks Pip away. A FREEZE BELL stops it.</summary>
    public class SpinHazard : MonoBehaviour
    {
        public static readonly List<SpinHazard> All = new List<SpinHazard>();
        public float degPerSec = 70f, armLength = 6f, armWidth = 1.2f;
        public Transform arm;
        public float FrozenUntil { get; set; } = -1f;
        public bool Frozen => Time.time < FrozenUntil;
        Transform _player; float _cool;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Start() { var p = FindFirstObjectByType<Player.PlayerMotor3D>(); _player = p != null ? p.transform : null; }

        void Update()
        {
            if (arm == null) return;
            if (!Frozen) arm.Rotate(0, degPerSec * Time.deltaTime, 0, Space.World);
            if (Frozen || _player == null || Time.time < _cool) return;
            Vector3 to = _player.position - arm.position; float dy = to.y; to.y = 0f;
            if (dy < -0.5f || dy > 2.2f || to.magnitude > armLength) return;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 dir = arm.forward * s;
                float along = Vector3.Dot(to, dir); if (along < 0f) continue;
                float side = Vector3.Cross(dir, to).magnitude;
                if (side < armWidth)
                {
                    _cool = Time.time + 1f;
                    var recv = _player.GetComponent<IPlayerDamageReceiver>();
                    if (recv != null) recv.TryHurt("a clock-hand fan", _player.position - Vector3.Cross(Vector3.up, dir) * Mathf.Sign(degPerSec), 11f);
                }
            }
        }
    }
}
