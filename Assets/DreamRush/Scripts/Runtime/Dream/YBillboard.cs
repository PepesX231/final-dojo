using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// Standing paper cutout that turns only around the vertical axis toward the camera (GDD §5.8 — posters and
    /// floor marks must NOT use this; full billboarding is reserved for effects).
    /// </summary>
    public class YBillboard : MonoBehaviour
    {
        public float maxAngle = 60f;
        Quaternion _rest;

        void Awake() => _rest = transform.rotation;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 to = cam.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 1e-4f) return;
            var face = Quaternion.LookRotation(-to.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(_rest, face, maxAngle);
        }
    }
}
