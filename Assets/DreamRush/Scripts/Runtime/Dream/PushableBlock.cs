using DreamRush.Core;
using UnityEngine;
using UnityEngine.AI;

namespace DreamRush.Dream
{
    public interface IPushable
    {
        void Push(Vector3 direction, float dt, Transform pusher);
    }

    /// <summary>
    /// Toy block the player shoves by walking into it (GDD §14.2 movable block, §16.7 Obstacle method).
    /// Moves along the dominant axis only (predictable), never into walls; carves the NavMesh so the chair's
    /// escape routes change.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PushableBlock : MonoBehaviour, IPushable
    {
        public float speed = 2.6f;
        public float LastPushedTime { get; private set; } = -999f;
        public bool EverPushed { get; private set; }

        BoxCollider _box;
        Rigidbody _rb;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _rb = GetComponent<Rigidbody>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }

        public void Push(Vector3 direction, float dt, Transform pusher)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) return;
            Vector3 axis = Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
                ? new Vector3(Mathf.Sign(direction.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(direction.z));
            float step = speed * dt;
            Vector3 half = Vector3.Scale(_box.size * 0.5f, transform.lossyScale) * 0.95f;
            half.y *= 0.8f;
            Vector3 center = transform.TransformPoint(_box.center) + Vector3.up * 0.05f;
            var hits = Physics.BoxCastAll(center, half, axis, transform.rotation, step + 0.02f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == _box) continue;
                if (pusher != null && h.collider.transform.IsChildOf(pusher)) continue;
                if (h.distance <= 0f) continue;
                if (h.normal.y > 0.7f) continue; // floor
                return; // blocked
            }
            transform.position += axis * step; // kinematic body follows; immediate so stepped simulations see it
            Physics.SyncTransforms();
            LastPushedTime = Time.time;
            EverPushed = true;
        }
    }
}
