using DreamRush.Player;
using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>DECOY APPLE (spawn card): a cardboard apple stand-in that distractible things treat as the player.</summary>
    public class DecoyApple : MonoBehaviour
    {
        public float life = 6f;
        float _t;

        void Update()
        {
            _t += Time.deltaTime;
            transform.localScale = Vector3.one * (_t < 0.2f ? _t / 0.2f : (life - _t < 0.3f ? Mathf.Max(0, (life - _t) / 0.3f) : 1f));
            transform.rotation = Quaternion.Euler(0, Mathf.Sin(_t * 3f) * 12f, Mathf.Sin(_t * 5f) * 4f);
            if (_t >= life) Destroy(gameObject);
        }
    }
}
