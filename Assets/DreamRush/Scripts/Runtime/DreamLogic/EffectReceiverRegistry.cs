using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>All live effect receivers (for card targeting) — registered by their components.</summary>
    public static class EffectReceiverRegistry
    {
        static readonly List<IEffectReceiver> Receivers = new List<IEffectReceiver>();
        public static IReadOnlyList<IEffectReceiver> All => Receivers;
        public static void Register(IEffectReceiver r) { if (!Receivers.Contains(r)) Receivers.Add(r); }
        public static void Unregister(IEffectReceiver r) => Receivers.Remove(r);

        /// <summary>Best target in front of <paramref name="from"/> within range/cone that has the property.</summary>
        public static IEffectReceiver FindTarget(Vector3 from, Vector3 forward, float range, float coneDegrees, DreamProperty required)
        {
            IEffectReceiver best = null;
            float bestScore = float.MaxValue;
            forward.y = 0f;
            for (int i = Receivers.Count - 1; i >= 0; i--)
            {
                var r = Receivers[i];
                if (r == null || (r is Object o && o == null)) { Receivers.RemoveAt(i); continue; }
                if (required != DreamProperty.None && (r.Properties & required) == 0) continue;
                Vector3 to = r.Transform.position - from; to.y = 0f;
                float d = to.magnitude;
                if (d > range) continue;
                float ang = d < 0.5f ? 0f : Vector3.Angle(forward, to);
                if (ang > coneDegrees * 0.5f) continue;
                float score = d + ang * 0.05f;
                if (score < bestScore) { bestScore = score; best = r; }
            }
            return best;
        }
    }
}
