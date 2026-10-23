using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing
{
    /// <summary>
    /// Hides a wall (keeps its shadow) when it is between the camera and the player.
    /// Also hides whatever is mounted on it (picture frames, door trims, windows, curtains).
    /// </summary>
    public class CutawayWall : MonoBehaviour
    {
        public static readonly List<CutawayWall> All = new List<CutawayWall>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public Vector3 normal = Vector3.forward;
        [Tooltip("x = half length along the wall, y = half height")]
        public Vector2 halfExtent = new Vector2(5f, 1.4f);
        public float decorDepth = 0.2f;

        readonly List<Renderer> rends = new List<Renderer>();
        bool hidden;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            rends.AddRange(GetComponentsInChildren<Renderer>(true));
            // grab mounted decor: static renderers sitting on this wall's plane, within its extent
            Vector3 o = transform.position, n = normal.normalized, along = Vector3.Cross(Vector3.up, n).normalized;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r || r is ParticleSystemRenderer || rends.Contains(r)) continue;
                if (r.GetComponentInParent<Pickup>() || r.GetComponentInParent<CutawayWall>()) continue;
                if (r.GetComponentInParent<PlayerController>() || r.GetComponentInParent<MomController>()) continue;
                Vector3 c = r.bounds.center, d = c - o;
                if (Mathf.Abs(Vector3.Dot(d, n)) > decorDepth) continue;
                if (Mathf.Abs(Vector3.Dot(d, along)) > halfExtent.x + 0.15f) continue;
                // heights relative to this wall's own floor (the house has two storeys)
                float baseY = o.y - halfExtent.y, h = halfExtent.y * 2f;
                if (c.y < baseY + 0.3f || c.y > baseY + h + 0.4f) continue;
                if (r.bounds.size.y > h + 0.7f) continue;
                rends.Add(r);
            }
        }

        public void Evaluate(Vector3 cam, Vector3 target)
        {
            Vector3 o = transform.position;
            float a = Vector3.Dot(cam - o, normal), b = Vector3.Dot(target - o, normal);
            bool hide = false;
            if (a * b < 0f)
            {
                float t = a / (a - b);
                Vector3 hit = cam + (target - cam) * t;
                Vector3 along = Vector3.Cross(Vector3.up, normal).normalized;
                hide = Mathf.Abs(Vector3.Dot(hit - o, along)) < halfExtent.x + 1.4f;
            }
            if (hide == hidden) return;
            hidden = hide;
            foreach (var r in rends)
                if (r) r.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }
}
