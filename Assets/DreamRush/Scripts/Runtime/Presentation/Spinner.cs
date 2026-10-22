using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>Decoration: spins around its local up axis and bobs gently (presentation only).</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 30f;
        public float bob = 0.2f;
        public float bobSpeed = 1.1f;
        Vector3 _home; float _phase;
        void Awake() { _home = transform.localPosition; _phase = Random.value * 6f; }
        void Update()
        {
            transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
            if (bob != 0f) transform.localPosition = _home + Vector3.up * Mathf.Sin(Time.time * bobSpeed + _phase) * bob;
        }
    }
}
