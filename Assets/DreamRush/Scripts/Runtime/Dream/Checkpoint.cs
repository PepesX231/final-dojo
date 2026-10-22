using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>Respawn point; reached when the player comes within <see cref="radius"/>. Higher order = later in the level.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public int order;
        public float radius = 2.5f;
        public bool isStart;

        void OnEnable() => DreamLevelManager.Register(this);
        void OnDisable() => DreamLevelManager.Unregister(this);

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.47f, 0.72f, 0.55f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
