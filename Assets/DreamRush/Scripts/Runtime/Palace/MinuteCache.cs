using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// A little stash of LOST MINUTES placed by the level builder (rooftops, alleys, behind billboards). Spawns its
    /// pickups at runtime in a small ring; each one has its own save key so it is collected once per run.
    /// </summary>
    public class MinuteCache : MonoBehaviour
    {
        public string key = "cache";
        public int count = 3;
        public float radius = 1.1f;

        void Start()
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / Mathf.Max(1, count);
                Vector3 p = transform.position + (count > 1 ? new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius : Vector3.zero);
                LostMinute.Spawn(p, 1, $"{key}:{i}", transform);
            }
        }
    }
}
