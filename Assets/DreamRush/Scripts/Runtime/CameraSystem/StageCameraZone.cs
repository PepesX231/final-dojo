using UnityEngine;

namespace DreamRush.CameraSystem
{
    /// <summary>Box volume that overrides stage camera framing while the player is inside (per section).</summary>
    public class StageCameraZone : MonoBehaviour
    {
        public Vector3 size = new Vector3(10, 10, 10);
        public int priority;
        public float yaw = 0f, pitch = 30f, distance = 13.9f, fieldOfView = 45f;

        public bool Contains(Vector3 p)
        {
            Vector3 l = transform.InverseTransformPoint(p);
            Vector3 h = size * 0.5f;
            return Mathf.Abs(l.x) <= h.x && Mathf.Abs(l.y) <= h.y && Mathf.Abs(l.z) <= h.z;
        }

        void OnEnable() => StageCameraController.RegisterZone(this);
        void OnDisable() => StageCameraController.UnregisterZone(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.67f, 0.63f, 0.87f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
