using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing.WitchTest
{
    [DefaultExecutionOrder(90)]
    public class TinyWitchCamera : MonoBehaviour
    {
        public Transform target;
        public float yaw = 0, pitch = 24, distance = .48f;
        public bool inputEnabled;
        public Vector3 shoulder = new Vector3(.035f, .09f, 0);
        public float Sensitivity = .11f;

        void LateUpdate()
        {
            if (!target) return;
            if (inputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
                var pad = Gamepad.current;
                if (pad != null) delta += pad.rightStick.ReadValue() * (Time.unscaledDeltaTime * 1200f);
                yaw += delta.x * Sensitivity; pitch = Mathf.Clamp(pitch - delta.y * Sensitivity, -22, 68);
            }
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 pivot = target.position + Quaternion.Euler(0, yaw, 0) * shoulder;
            // Aiming upward must not swing the camera under the worktop into the apple.
            var boom = Quaternion.Euler(Mathf.Clamp(pitch, 12, 55), yaw, 0);
            Vector3 wanted = pivot - boom * Vector3.forward * distance;
            float d = distance;
            foreach (var hit in Physics.SphereCastAll(pivot, .018f, (wanted - pivot).normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(target) || hit.collider.GetComponentInParent<WitchProp>()) continue;
                d = Mathf.Min(d, Mathf.Max(.12f, hit.distance - .02f));
            }
            transform.SetPositionAndRotation(pivot - boom * Vector3.forward * d, rotation);
        }
    }
}
