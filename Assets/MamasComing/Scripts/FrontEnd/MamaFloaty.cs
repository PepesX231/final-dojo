using UnityEngine;

namespace MamasComing
{
    /// Small decor motion for menu / results stages: bob, sway and spin (unscaled time, no physics).
    public class MamaFloaty : MonoBehaviour
    {
        public float bob = 0.12f, bobSpeed = 0.7f, sway = 3f, spin;
        public float phase;
        Vector3 basePos; Quaternion baseRot;
        void Awake() { basePos = transform.localPosition; baseRot = transform.localRotation; if (phase == 0f) phase = Random.value * 10f; }
        void Update()
        {
            float t = Time.unscaledTime * bobSpeed + phase;
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * Mathf.PI * 2f) * bob;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 3.1f) * sway, Time.unscaledTime * spin, Mathf.Cos(t * 2.3f) * sway);
        }
    }
}
