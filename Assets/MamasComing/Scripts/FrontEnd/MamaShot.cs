using UnityEngine;

namespace MamasComing
{
    /// A camera position for a front-end screen. Move / rotate this object (or use the context menu
    /// "Copy pose from Scene view") to reframe the title or lobby shot.
    public class MamaShot : MonoBehaviour
    {
        [Range(15f, 90f)] public float fov = 44f;
        [Tooltip("Slow sway so the shot feels alive (degrees).")]
        public float sway = 0.6f;

#if UNITY_EDITOR
        [ContextMenu("Copy pose from Scene view")]
        void CopyFromSceneView()
        {
            var sv = UnityEditor.SceneView.lastActiveSceneView; if (!sv) return;
            UnityEditor.Undo.RecordObject(transform, "Copy shot pose");
            transform.SetPositionAndRotation(sv.camera.transform.position, sv.camera.transform.rotation);
        }
        [ContextMenu("Look through this shot in Scene view")]
        void LookThrough()
        {
            var sv = UnityEditor.SceneView.lastActiveSceneView; if (!sv) return;
            sv.AlignViewToObject(transform); sv.cameraSettings.fieldOfView = fov; sv.Repaint();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.7f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, fov, 3f, 0.2f, 16f / 9f);
        }
#endif
    }
}
