using System.Collections;
using UnityEngine;

namespace MamasComing.WitchTest
{
    public class WitchProp : MonoBehaviour
    {
        public string id, label;
        public Rigidbody body;
        public BoxCollider shape;
        public WitchHome home;
        public bool Placed { get; private set; }
        public bool Held { get; private set; }
        public bool Settling { get; private set; }
        public Vector3 initialPosition;
        public Quaternion initialRotation;
        public float Radius => Mathf.Max(shape.size.x, shape.size.z) * .5f;
        public Vector3 Center => transform.TransformPoint(shape.center);
        public Vector3 HalfSize => Vector3.Scale(shape.size, transform.lossyScale) * .48f;
        Quaternion levelRotation;

        void Awake() { initialPosition = transform.position; initialRotation = transform.rotation; levelRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0); }

        public void Lift()
        {
            StopAllCoroutines(); Settling = false; Placed = false; Held = true;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.isKinematic = true;
        }

        public void Turn(float angle) { levelRotation = Quaternion.AngleAxis(angle, Vector3.up) * levelRotation; }

        /// <summary>Collision-constrained movement to a desired pivot. A held prop never teleports through a wall.</summary>
        public void MoveHeld(Vector3 target, Transform player, float dt)
        {
            Quaternion rotation = Quaternion.Slerp(body.rotation, levelRotation, 1f - Mathf.Exp(-10f * dt));
            Vector3 delta = target - body.position;
            float distance = delta.magnitude;
            if (distance > .0001f)
            {
                float allowed = distance;
                foreach (var hit in Physics.BoxCastAll(Center, HalfSize, delta / distance, rotation, distance + .004f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform.IsChildOf(player)) continue;
                    allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .004f));
                }
                // Use a capped follow speed; even a sudden camera turn sweeps along the whole path.
                float step = Mathf.Min(allowed, distance * (1f - Mathf.Exp(-12f * dt)), 1.5f * dt);
                body.MovePosition(body.position + delta / distance * step);
            }
            body.MoveRotation(rotation);
        }

        public bool Release(bool allowPlacement)
        {
            Held = false;
            if (allowPlacement && home && home.Accepts(this, transform.position)) { StartCoroutine(Settle()); return true; }
            body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            return false;
        }

        IEnumerator Settle()
        {
            Settling = true;
            Vector3 from = body.position; Quaternion rotation = body.rotation;
            body.isKinematic = true;
            for (float t = 0; t < .32f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / .32f);
                body.position = Vector3.Lerp(from, home.transform.position, k);
                body.rotation = Quaternion.Slerp(rotation, home.transform.rotation, k);
                yield return null;
            }
            body.position = home.transform.position; body.rotation = home.transform.rotation;
            Placed = true; Settling = false;
        }

        public void ResetProp()
        {
            StopAllCoroutines(); Held = Placed = Settling = false;
            body.isKinematic = true; body.position = initialPosition; body.rotation = initialRotation;
            levelRotation = Quaternion.Euler(0, initialRotation.eulerAngles.y, 0);
            body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        }
    }
}
