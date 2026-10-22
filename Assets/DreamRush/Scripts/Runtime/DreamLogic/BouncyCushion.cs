using DreamRush.Player;
using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>BOUNCY CUSHION (platform card): landing on it launches the apple high — reach the balcony, drop in front of the chair.</summary>
    public class BouncyCushion : MonoBehaviour
    {
        public float life = 12f;
        public float launchSpeed = 13.5f;
        public float radius = 0.9f;
        float _t, _squash;
        Vector3 _baseScale;

        void Awake() => _baseScale = transform.localScale;

        void Update()
        {
            _t += Time.deltaTime;
            var m = FindFirstObjectByType<PlayerMotor3D>();
            if (m != null)
            {
                Vector3 d = m.transform.position - transform.position;
                float top = transform.position.y + _baseScale.y * 0.5f;
                bool above = Mathf.Abs(d.x) < radius && Mathf.Abs(d.z) < radius && m.transform.position.y >= transform.position.y - 0.5f && m.transform.position.y < top + 0.35f;
                if (above && m.VerticalVelocity <= 0.1f)
                {
                    m.Launch(new Vector3(m.PlanarVelocity.x, launchSpeed, m.PlanarVelocity.z));
                    _squash = 1f;
                }
            }
            _squash = Mathf.MoveTowards(_squash, 0f, Time.deltaTime * 5f);
            float pop = _t < 0.2f ? _t / 0.2f : (life - _t < 0.4f ? Mathf.Max(0f, (life - _t) / 0.4f) : 1f);
            transform.localScale = new Vector3(_baseScale.x * (1f + 0.2f * _squash), _baseScale.y * (1f - 0.45f * _squash), _baseScale.z * (1f + 0.2f * _squash)) * pop;
            if (_t >= life) Destroy(gameObject);
        }
    }
}
