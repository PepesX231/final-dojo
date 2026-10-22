using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>Named level section (A–E, GDD §13.1) with the objective shown in the HUD while inside.</summary>
    public class DreamSection : MonoBehaviour
    {
        public string sectionId = "A";
        public string displayName = "Arrival";
        [TextArea] public string objective = "";
        public int order;
        public Vector3 size = new Vector3(10, 10, 10);

        public bool Contains(Vector3 p)
        {
            Vector3 l = transform.InverseTransformPoint(p);
            Vector3 h = size * 0.5f;
            return Mathf.Abs(l.x) <= h.x && Mathf.Abs(l.y) <= h.y && Mathf.Abs(l.z) <= h.z;
        }

        void OnEnable() => DreamLevelManager.Register(this);
        void OnDisable() => DreamLevelManager.Unregister(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.44f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
