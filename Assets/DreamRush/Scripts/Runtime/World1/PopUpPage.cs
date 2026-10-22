using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>
    /// Chapter 1 mechanic — pop-up book pages that spring up into steps when Pip comes near.
    ///   Stairs:   stay up (teach).
    ///   Timed:    fold back down <see cref="holdSeconds"/> after you first step on them, with a 0.8 s shake
    ///             warning (develop: "hurry!").
    /// Rises with an overshoot (paper "boing"), carries the player while moving (MovingPlatform).
    /// </summary>
    public class PopUpPage : MovingPlatform
    {
        public enum Mode { Stairs, Timed }
        public Mode mode = Mode.Stairs;
        public float riseHeight = 1.5f;
        public float triggerRadius = 6f;
        public float riseTime = 0.45f;
        public float holdSeconds = 3f;
        public float delay;            // stagger in a staircase
        public Transform visual;       // optional child shaken on warning

        public bool IsUp => _state == 2;
        int _state;                    // 0 down, 1 rising, 2 up, 3 warning, 4 sinking
        float _stateT, _downY, _upY;
        bool _stoodOn;
        float _near;

        protected override void Awake()
        {
            _upY = transform.position.y;
            _downY = _upY - riseHeight;
            transform.position = new Vector3(transform.position.x, _downY, transform.position.z);
            base.Awake();
            AutoMove = false;
        }

        protected override void Update()
        {
            float dt = Time.deltaTime;
            _stateT += dt;
            Vector3 pos = transform.position;
            switch (_state)
            {
                case 0:
                    // a short grace after sinking, then rise when Pip is near (staircase: staggered by delay)
                    if (_stateT > 0.6f && _player != null && Flat(_player.transform.position - pos).magnitude < triggerRadius) _near += dt;
                    else _near = 0f;
                    if (_near > delay) { _near = 0f; Go(1); }
                    break;
                case 1:
                {
                    float k = UI.UIStyle.OutBack(_stateT / riseTime, 2.2f);
                    pos.y = Mathf.LerpUnclamped(_downY, _upY, k);
                    if (_stateT >= riseTime) { pos.y = _upY; Go(2); Audio.AudioManager.Play(Audio.Sfx.Land, pos, 0.25f); }
                    break;
                }
                case 2:
                    if (mode == Mode.Timed)
                    {
                        if (PlayerOnTop) _stoodOn = true;
                        if (_stoodOn && _stateT > holdSeconds) Go(3);
                        else if (!_stoodOn) _stateT = 0f;
                    }
                    break;
                case 3:
                    if (visual != null) visual.localPosition = new Vector3(Mathf.Sin(_stateT * 60f) * 0.05f, 0, 0);
                    if (_stateT > 0.8f) { if (visual != null) visual.localPosition = Vector3.zero; Go(4); }
                    break;
                case 4:
                {
                    float k = UI.UIStyle.InOutCubic(_stateT / 0.5f);
                    pos.y = Mathf.Lerp(_upY, _downY, k);
                    if (_stateT >= 0.5f) { pos.y = _downY; _stoodOn = false; Go(0); }
                    break;
                }
            }
            transform.position = pos;
            Carry();
        }

        void Go(int s) { _state = s; _stateT = 0f; }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}
