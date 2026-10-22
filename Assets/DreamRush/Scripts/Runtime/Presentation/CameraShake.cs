using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// Small trauma-style camera shake shared by the camera controllers (GDD §20.6: readable, never nauseating).
    /// Cameras add <see cref="Offset"/> after computing their pose and remove it before their next smoothing step.
    /// </summary>
    public static class CameraShake
    {
        public static bool Reduced;
        static float _trauma, _lastTime;

        /// <summary>amount 0..1 (0.3 = landing thud, 0.6 = boss kick).</summary>
        public static void Add(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        public static Vector3 Offset(Transform cam)
        {
            float now = Time.unscaledTime;
            float dt = Mathf.Clamp(now - _lastTime, 0f, 0.1f);
            _lastTime = now;
            _trauma = Mathf.Max(0f, _trauma - dt * 1.8f);
            if (_trauma <= 0f || cam == null) return Vector3.zero;
            float a = _trauma * _trauma * (Reduced ? 0.12f : 0.35f);
            float t = now * 28f;
            float x = (Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f * a;
            float y = (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * a;
            return cam.right * x + cam.up * y;
        }

        public static void ResetForTests() { _trauma = 0f; }
    }
}
