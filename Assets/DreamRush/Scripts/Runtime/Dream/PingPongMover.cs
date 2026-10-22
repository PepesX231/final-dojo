using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>Moves back and forth between two local offsets (moving props).</summary>
    public class PingPongMover : MonoBehaviour
    {
        public Vector3 offset = new Vector3(4, 0, 0);
        public float period = 3f;
        public bool paused;
        Vector3 _start;
        float _t;

        void Awake() => _start = transform.localPosition;

        void Update()
        {
            if (paused) return;
            _t += Time.deltaTime;
            float k = 0.5f - 0.5f * Mathf.Cos(_t / period * Mathf.PI * 2f);
            transform.localPosition = _start + offset * k;
        }
    }
}
