using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>Slow breathing glow of a giant crystal's light (FF crystal spirit).</summary>
    public class CrystalPulse : MonoBehaviour
    {
        public Light l;
        public float baseIntensity = 3.5f, amount = 1.2f, speed = 1.1f;
        float _phase;
        void Start() { _phase = Random.value * 6f; if (l != null) baseIntensity = l.intensity; }
        void Update() { if (l != null) l.intensity = baseIntensity + Mathf.Sin(Time.time * speed + _phase) * amount; }
    }
}
