using UnityEngine;

namespace DreamRush.Core
{
    /// <summary>
    /// Physics queries that ignore a given hierarchy (usually the caller itself) without needing
    /// custom layers. Uses shared NonAlloc buffers — main thread only.
    /// </summary>
    public static class PhysicsUtil
    {
        static readonly RaycastHit[] Buffer = new RaycastHit[32];

        public static bool SphereCastIgnoring(Vector3 origin, float radius, Vector3 direction, float distance,
            Transform ignoreRoot, out RaycastHit closest, int layerMask = Physics.DefaultRaycastLayers)
        {
            int n = Physics.SphereCastNonAlloc(origin, radius, direction, Buffer, distance, layerMask,
                QueryTriggerInteraction.Ignore);
            return PickClosest(n, ignoreRoot, out closest);
        }

        public static bool RaycastIgnoring(Vector3 origin, Vector3 direction, float distance,
            Transform ignoreRoot, out RaycastHit closest, int layerMask = Physics.DefaultRaycastLayers)
        {
            int n = Physics.RaycastNonAlloc(origin, direction, Buffer, distance, layerMask,
                QueryTriggerInteraction.Ignore);
            return PickClosest(n, ignoreRoot, out closest);
        }

        static bool PickClosest(int n, Transform ignoreRoot, out RaycastHit closest)
        {
            closest = default;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = Buffer[i];
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                // distance 0 means the cast started inside the collider — not a useful hit for us
                if (h.distance <= 0f && h.point == Vector3.zero) continue;
                if (h.distance < best) { best = h.distance; closest = h; found = true; }
            }
            return found;
        }
    }
}
