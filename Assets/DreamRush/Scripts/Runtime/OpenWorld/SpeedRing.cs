using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>Golden ring: fly through it to get flung along its axis (chained rings = sky routes).</summary>
    public class SpeedRing : MonoBehaviour
    {
        public float radius = 1.8f, boost = 24f, boostTime = 0.3f, lift = 6f;
        PlayerMotor3D _player; float _cool; Vector3 _lastSide;
        void Start() { _player = FindFirstObjectByType<PlayerMotor3D>(); }
        void Update()
        {
            transform.Rotate(transform.forward, 40f * Time.deltaTime, Space.World);
            _cool -= Time.deltaTime;
            if (_player == null) return;
            Vector3 p = _player.transform.position + Vector3.up * 0.6f - transform.position;
            float along = Vector3.Dot(p, transform.forward);
            Vector3 side = p - transform.forward * along;
            bool crossed = Mathf.Sign(along) != Mathf.Sign(Vector3.Dot(_lastSide, transform.forward)) && _lastSide != Vector3.zero;
            _lastSide = p;
            if (_cool > 0f || !crossed || side.magnitude > radius) return;
            _cool = 0.8f;
            Vector3 dir = transform.forward * Mathf.Sign(Vector3.Dot(_player.Velocity, transform.forward) + 0.001f);
            _player.SetVelocity(_player.PlanarVelocity + Vector3.up * lift);   // a little hop out of the ring...
            _player.Dash(dir, boost, boostTime);                                 // ...then the burst along its axis
            VFX.Play(VFXKind.PaperBurst, transform.position, 1.2f);
            VFX.Play(VFXKind.LandingRing, transform.position, 1f, VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 0.8f);
            CameraShake.Add(0.15f);
            Palace.WonderDirector.Novelty("speed ring");
        }
    }
}
