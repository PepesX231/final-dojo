using System.Collections;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>Ch2 cardboard bridge plank: shakes 0.45 s after Pip steps on it, drops, then returns.</summary>
    public class CrumblePlatform : MovingPlatform
    {
        public float shakeTime = 0.45f, fallTime = 1.2f, returnAfter = 3.5f;
        public Transform visual;
        int _state; float _st; Vector3 _home;
        Collider[] _cols;

        protected override void Awake() { base.Awake(); _home = transform.position; AutoMove = false; _cols = GetComponentsInChildren<Collider>(); }

        protected override void Update()
        {
            _st += Time.deltaTime;
            switch (_state)
            {
                case 0: if (IsPlayerOnTop()) { _state = 1; _st = 0; Audio.AudioManager.Play(Audio.Sfx.ChairStumble, transform.position, 0.35f); } break;
                case 1:
                    if (visual != null) visual.localPosition = new Vector3(Mathf.Sin(_st * 70f) * 0.04f, 0, Mathf.Cos(_st * 55f) * 0.03f);
                    if (_st > shakeTime) { _state = 2; _st = 0; if (visual != null) visual.localPosition = Vector3.zero; }
                    break;
                case 2:
                    transform.position = _home + Vector3.down * (_st * _st * 9f);
                    transform.rotation = Quaternion.Euler(_st * 40f, 0, _st * 25f);
                    if (_st > 0.25f) foreach (var c in _cols) c.enabled = false;
                    if (_st > fallTime) { _state = 3; _st = 0; foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false; }
                    break;
                case 3:
                    if (_st > returnAfter)
                    {
                        transform.SetPositionAndRotation(_home, Quaternion.identity);
                        foreach (var c in _cols) c.enabled = true;
                        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true;
                        VFX.Play(VFXKind.Poof, _home + Vector3.up * 0.3f, 1f);
                        _state = 0; _st = 0;
                    }
                    break;
            }
            Carry();
        }
    }
}
