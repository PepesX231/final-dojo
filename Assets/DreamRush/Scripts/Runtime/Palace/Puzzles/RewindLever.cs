using DreamRush.Combat;
using DreamRush.Interaction;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17: the big clock-key lever that rewinds a <see cref="RewindBridge"/> (E or a punch).</summary>
    public class RewindLever : Interactable, IHittable
    {
        public RewindBridge bridge;
        public Transform handle;
        float _spin;
        protected override void OnEnable() { base.OnEnable(); prompt = "REWIND TIME"; }
        protected override void OnInteract(InteractionContext context) => Pull();
        public bool TakeHit(HitInfo hit) { Pull(); return true; }
        void Pull() { if (bridge != null) { bridge.Rewind(); _spin = 1f; } }
        void Update()
        {
            if (handle == null) return;
            _spin = Mathf.MoveTowards(_spin, 0f, Time.deltaTime);
            handle.Rotate(0, 0, -720f * _spin * Time.deltaTime, Space.Self);
        }
    }
}
