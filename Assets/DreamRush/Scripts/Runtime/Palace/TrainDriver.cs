using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 monorail you can RIDE: one driver moves every car along the loop (keeps the couplings tight), stops at
    /// stations for <see cref="dwell"/> seconds (hop onto the roof), and each car carries Pip (MovingPlatform, no auto
    /// move). Fast travel around the city + a moving vantage point.
    /// </summary>
    public class TrainDriver : MonoBehaviour
    {
        public List<Vector3> points = new List<Vector3>();
        public List<Transform> cars = new List<Transform>();
        public float spacing = 12.6f, speed = 15f, dwell = 7f, yawOffset = 180f;
        public List<float> stops = new List<float>();     // distances along the loop where the HEAD car stops
        public float Head { get; private set; }
        public bool AtStation { get; private set; }
        float[] _cum; float _len, _wait, _k = 1f; int _next;

        void Start()
        {
            int n = points.Count; _cum = new float[n + 1];
            for (int i = 0; i < n; i++) _cum[i + 1] = _cum[i] + Vector3.Distance(points[i], points[(i + 1) % n]);
            _len = _cum[n];
            stops.Sort();
            Place();
        }

        public Vector3 Eval(float d, out Vector3 dir)
        {
            d = Mathf.Repeat(d, _len); int n = points.Count;
            int lo = 0, hi = n; while (hi - lo > 1) { int m = (lo + hi) / 2; if (_cum[m] <= d) lo = m; else hi = m; }
            Vector3 a = points[lo], b = points[(lo + 1) % n]; float seg = Mathf.Max(1e-4f, _cum[lo + 1] - _cum[lo]);
            dir = (b - a) / seg; return Vector3.Lerp(a, b, (d - _cum[lo]) / seg);
        }

        void Update()
        {
            if (_cum == null || _len <= 0f) return;
            float dt = Time.deltaTime;
            if (_wait > 0f) { _wait -= dt; AtStation = true; if (_wait <= 0f) { AtStation = false; Audio.AudioManager.Play(Audio.Sfx.UiOpen, cars.Count > 0 ? cars[0].position : transform.position, 0.6f); } return; }
            float stopAt = stops.Count > 0 ? stops[_next % stops.Count] : -1f;
            float toStop = stopAt < 0 ? 999f : Mathf.Repeat(stopAt - Head, _len);
            float want = toStop < 30f ? Mathf.Clamp(toStop / 30f, 0.12f, 1f) : 1f;      // brake into the station
            _k = Mathf.MoveTowards(_k, want, dt * 0.8f);
            float step = speed * _k * dt;
            if (stopAt >= 0 && step >= toStop) { Head = stopAt; _wait = dwell; _next++; _k = 0.15f; Audio.AudioManager.Play(Audio.Sfx.Door, cars.Count > 0 ? cars[0].position : transform.position, 0.6f); }
            else Head = Mathf.Repeat(Head + step, _len);
            Place();
        }

        void Place()
        {
            for (int i = 0; i < cars.Count; i++)
            {
                if (cars[i] == null) continue;
                Vector3 p = Eval(Head - i * spacing, out var dir); dir.y = 0f;
                cars[i].SetPositionAndRotation(p, dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir) * Quaternion.Euler(0, yawOffset, 0) : cars[i].rotation);
            }
        }
    }
}
