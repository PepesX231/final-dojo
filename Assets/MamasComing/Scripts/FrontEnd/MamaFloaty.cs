using UnityEngine;

namespace MamasComing
{
    /// Small decor motion for menu / results stages: bob, sway and spin (unscaled time, no physics).
    public class MamaFloaty : MonoBehaviour
    {
        public float bob = 0.12f, bobSpeed = 0.7f, sway = 3f, spin;
        [Tooltip("Zero-g tumble: degrees per second around the local x / y / z axes.")]
        public Vector3 tumble;
        [Tooltip("Slow drift: metres it wanders around its start point.")]
        public Vector3 drift;
        public float phase;
        Vector3 basePos; Quaternion baseRot;
        void Awake() { basePos = transform.localPosition; baseRot = transform.localRotation; if (phase == 0f) phase = Random.value * 10f; }
        void Update()
        {
            float t = Time.unscaledTime * bobSpeed + phase;
            float u = Time.unscaledTime;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * Mathf.PI * 2f) * bob
                + new Vector3(Mathf.Sin(u * .21f + phase) * drift.x, Mathf.Sin(u * .17f + phase * 1.3f) * drift.y, Mathf.Cos(u * .19f + phase * .7f) * drift.z);
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 3.1f) * sway, u * spin, Mathf.Cos(t * 2.3f) * sway) * Quaternion.Euler(tumble * u);
        }
    }
}
