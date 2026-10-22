using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// "The apple child falls gently into a strange living room" (GDD §13.3): the first fall uses feather-light
    /// gravity, then the normal config is restored on landing.
    /// </summary>
    public class DreamArrival : MonoBehaviour
    {
        public PlayerMotor3D player;
        public float floatGravity = 5f;
        public float floatMaxFall = 3.5f;

        PlayerMotorConfig _original, _floaty;
        bool _active;

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMotor3D>();
            if (player == null || player.Config == null) return;
            _original = player.Config;
            _floaty = Instantiate(_original);
            _floaty.gravity = floatGravity;
            _floaty.maxFallSpeed = floatMaxFall;
            player.Config = _floaty;
            player.Landed += OnLanded;
            _active = true;
        }

        void OnLanded(float _) => Restore();

        void Restore()
        {
            if (!_active) return;
            _active = false;
            player.Landed -= OnLanded;
            player.Config = _original;
            Destroy(_floaty);
        }

        void OnDestroy() { if (_active && player != null) Restore(); }
    }
}
