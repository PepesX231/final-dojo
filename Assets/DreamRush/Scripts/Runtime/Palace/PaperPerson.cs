using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 2.5D crowd: a paper-cutout sticker person (a camera-facing card with a white outline). Turns its card to
    /// the camera on Y only, flips like a paper doll when its walking direction crosses the camera's right axis,
    /// bobs and wobbles while walking (PathMover drives position), idles with a slow sway when standing.
    /// </summary>
    public class PaperPerson : MonoBehaviour
    {
        public Transform card;
        public float height = 1.7f;
        Vector3 _last; float _face = 1f, _flip = 1f, _t, _speed;
        static Transform _cam;

        void Start() { _last = transform.position; _t = Random.value * 10f; }

        void LateUpdate()
        {
            if (card == null) return;
            if (_cam == null) { var c = Camera.main; if (c == null) return; _cam = c.transform; }
            float dt = Mathf.Max(1e-4f, Time.deltaTime);
            Vector3 v = (transform.position - _last) / dt; _last = transform.position; v.y = 0f;
            _speed = Mathf.Lerp(_speed, v.magnitude, dt * 6f);
            Vector3 toCam = _cam.position - transform.position; toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-4f) return;
            var look = Quaternion.LookRotation(-toCam.normalized);                    // card faces the camera
            if (v.sqrMagnitude > 0.04f) _face = Vector3.Dot(v, _cam.right) >= 0f ? 1f : -1f;
            _flip = Mathf.MoveTowards(_flip, _face, dt * 7f);                           // paper-doll flip
            _t += dt * (_speed > 0.2f ? _speed * 4.2f : 1.3f);
            float bob = _speed > 0.2f ? Mathf.Abs(Mathf.Sin(_t)) * 0.07f : Mathf.Sin(_t) * 0.01f;
            float tilt = _speed > 0.2f ? Mathf.Sin(_t) * 5f : Mathf.Sin(_t * 0.5f) * 2f;
            card.SetPositionAndRotation(transform.position + Vector3.up * (height * 0.5f + bob), look * Quaternion.Euler(0, 0, tilt));
            var s = card.localScale; card.localScale = new Vector3(Mathf.Abs(s.x) * (Mathf.Abs(_flip) < 0.05f ? 0.05f * Mathf.Sign(_flip + 1e-3f) : _flip), s.y, s.z);
        }
    }
}
