using System;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>A punchable proxy (trigger collider) that forwards hits to its owner — the winding key, the stuck hour hand.</summary>
    public class BossWeakPoint : MonoBehaviour, IHittable
    {
        public Func<HitInfo, bool> onHit;
        public bool TakeHit(HitInfo hit) => isActiveAndEnabled && onHit != null && onHit(hit);

        public static BossWeakPoint Create(string name, Transform parent, Vector3 localPos, float radius, Func<HitInfo, bool> onHit)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = localPos;
            var c = go.AddComponent<SphereCollider>(); c.radius = radius; c.isTrigger = true;
            var w = go.AddComponent<BossWeakPoint>(); w.onHit = onHit;
            return w;
        }
    }
}
