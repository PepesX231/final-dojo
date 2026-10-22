using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M16 city life: moves along a polyline (cars, sky traffic, monorail cars, pedestrians). Closed loops wrap around;
    /// open paths teleport back to the start (the far end hides in fog). Optional walk bob/sway for pedestrians.
    /// Presentation only: no colliders, never blocks Pip.
    /// </summary>
    public class PathMover : MonoBehaviour
    {
        public List<Vector3> points = new List<Vector3>();
        public bool closed = true;
        public float speed = 8f, offset;
        public float bob, sway;               // walkers: vertical bob amplitude (m) + side sway (deg)
        public float yawOffset;
        public bool yieldToPlayer;            // street cars brake for Pip instead of driving through him
        Transform _model; float[] _cum; float _len, _d, _phase, _k = 1f;

        void Start()
        {
            _model = transform.childCount > 0 ? transform.GetChild(0) : null;
            Rebuild(); _d = offset; _phase = Random.value * 6f;
        }

        public void Rebuild()
        {
            int n = points.Count; if (n < 2) { enabled = false; return; }
            int segs = closed ? n : n - 1;
            _cum = new float[segs + 1];
            for (int i = 0; i < segs; i++) _cum[i + 1] = _cum[i] + Vector3.Distance(points[i], points[(i + 1) % n]);
            _len = _cum[segs];
        }

        public Vector3 Eval(float d, out Vector3 dir)
        {
            int n = points.Count;
            d = closed ? Mathf.Repeat(d, _len) : Mathf.Clamp(d, 0, _len);
            int lo = 0, hi = _cum.Length - 1;
            while (hi - lo > 1) { int m = (lo + hi) / 2; if (_cum[m] <= d) lo = m; else hi = m; }
            Vector3 a = points[lo], b = points[(lo + 1) % n];
            float seg = Mathf.Max(1e-4f, _cum[lo + 1] - _cum[lo]);
            dir = (b - a) / seg;
            return Vector3.Lerp(a, b, (d - _cum[lo]) / seg);
        }

        void Update()
        {
            if (_cum == null) return;
            if (yieldToPlayer)
            {
                float want = 1f; var p0 = Player.PlayerMotor3D.Instance;
                if (p0 != null)
                {
                    Vector3 to = p0.transform.position - transform.position; to.y = 0f;
                    float ahead = Vector3.Dot(to, transform.forward), side = Vector3.Dot(to, transform.right);
                    if (ahead > -1f && ahead < 18f && Mathf.Abs(side) < 2.4f) want = Mathf.Clamp01((ahead - 10f) / 8f);   // stop well short: the follow camera sits ~6 m behind Pip
                }
                _k = Mathf.MoveTowards(_k, want, Time.deltaTime * (want < _k ? 3f : 0.8f));
            }
            _d += speed * _k * Time.deltaTime;
            if (!closed && _d > _len) _d -= _len;
            Vector3 p = Eval(_d, out var dir);
            transform.position = p;
            dir.y = 0f;
            if (dir.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0, yawOffset, 0);
            if (_model != null && (bob > 0f || sway > 0f))
            {
                float t = Time.time * speed * 2.6f + _phase;
                _model.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(t)) * bob, 0);
                _model.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t) * sway);
            }
        }
    }
}
