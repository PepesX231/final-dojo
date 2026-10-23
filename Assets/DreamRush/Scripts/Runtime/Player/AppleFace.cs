using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Eyes for the apple model (GDD §20.2 face expressions). The FBX face is geometry inside the Body mesh with
    /// its own material slots (eye white / pupil / shine) and ink lid lines, so "eyes closed" = those slots draw as
    /// skin and only the ink lid lines remain. Blinks, closes while sleeping, squeezes shut when hurt.
    /// </summary>
    public class AppleFace : MonoBehaviour
    {
        public AppleProceduralAnimator animator;
        public float blinkMin = 2.4f, blinkMax = 5.0f, blinkTime = 0.12f;

        Renderer _body;
        Material[] _open, _closed;
        float _nextBlink, _blinkUntil, _squeezeUntil;
        bool _isClosed;

        public bool EyesClosed => _isClosed;
        public bool HasFace => _closed != null;

        /// <summary>Restore the neutral material set before cloning a live avatar during a blink.</summary>
        public void RestoreOpenEyes()
        {
            if (_body == null || _open == null) return;
            _body.sharedMaterials = _open;
            _isClosed = false;
        }

        void Awake()
        {
            if (animator == null) animator = GetComponent<AppleProceduralAnimator>();
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (r.name == "Body") { _body = r; break; }
            if (_body == null) return;
            _open = _body.sharedMaterials;
            _closed = (Material[])_open.Clone();
            Material lid = null;
            bool any = false;
            for (int i = 0; i < _open.Length; i++)
            {
                var m = _open[i];
                if (m == null) continue;
                if (m.name.StartsWith("M_Apple_EyeWhite") || m.name.StartsWith("M_Apple_Pupil"))
                {
                    lid ??= Look.DreamLook.Toon(new Color(0.56f, 0.1f, 0.12f) * 1.4f, null, 0.3f, name: "M_Apple_Eyelid");
                    _closed[i] = lid; any = true;
                }
            }
            if (!any) _closed = null;
            _nextBlink = Time.time + Random.Range(blinkMin, blinkMax);
        }

        /// <summary>Squeeze the eyes shut for a moment (hit, pinky toe kick).</summary>
        public void Squeeze(float seconds) => _squeezeUntil = Mathf.Max(_squeezeUntil, Time.time + seconds);

        void LateUpdate()
        {
            if (_closed == null) return;
            float now = Time.time;
            if (now >= _nextBlink)
            {
                _blinkUntil = now + blinkTime;
                // occasional double blink reads as "alive"
                _nextBlink = now + (Random.value < 0.2f ? 0.25f : Random.Range(blinkMin, blinkMax));
            }
            bool sleeping = animator != null && animator.sleeping;
            SetClosed(sleeping || now < _blinkUntil || now < _squeezeUntil);
        }

        void SetClosed(bool closed)
        {
            if (closed == _isClosed) return;
            _isClosed = closed;
            _body.sharedMaterials = closed ? _closed : _open;
        }
    }
}
