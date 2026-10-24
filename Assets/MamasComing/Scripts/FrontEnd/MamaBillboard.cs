using UnityEngine;

namespace MamasComing
{
    /// Turns a world-space speech bubble / tag to face the camera (keeps it upright).
    [ExecuteAlways]
    public class MamaBillboard : MonoBehaviour
    {
        public float wobble = 2f;
        [Tooltip("Optional: stay above this kid's head (follows its pose).")]
        public MamaPosedKid follow;
        public Vector3 offset = new Vector3(0.08f, 0.12f, 0f);
        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            if (follow && follow.Model) transform.position = follow.HeadPoint + cam.transform.right * offset.x + Vector3.up * offset.y;
            transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up) * Quaternion.Euler(0, 0, Application.isPlaying ? Mathf.Sin(Time.unscaledTime * 2f + transform.GetInstanceID()) * wobble : 0f);
        }
    }
}
