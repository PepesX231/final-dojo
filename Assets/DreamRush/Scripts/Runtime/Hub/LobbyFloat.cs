using UnityEngine;

namespace DreamRush.Hub
{
    /// <summary>Dream-sky drift for the lobby diorama props: gentle bob, slow spin, optional orbit around a centre.</summary>
    public class LobbyFloat : MonoBehaviour
    {
        public float bobHeight = 0.25f, bobSpeed = 0.6f, spin = 6f;
        public Vector3 orbitCenter; public float orbitDegPerSec;
        float _phase; Vector3 _start;

        void Awake() { _start = transform.position; _phase = (transform.position.x * 0.37f + transform.position.z * 0.21f) % 6.28f; }

        void Update()
        {
            float t = Time.time;
            Vector3 p = _start;
            if (orbitDegPerSec != 0f)
            {
                Vector3 off = _start - orbitCenter;
                p = orbitCenter + Quaternion.Euler(0, orbitDegPerSec * t, 0) * off;
            }
            p.y += Mathf.Sin(t * bobSpeed + _phase) * bobHeight;
            transform.position = p;
            if (spin != 0f) transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
        }
    }
}
