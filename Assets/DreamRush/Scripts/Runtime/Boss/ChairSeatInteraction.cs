using DreamRush.Interaction;
using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>"[E] SIT" — only offered when the seat validation passes (GDD §15.9).</summary>
    public class ChairSeatInteraction : Interactable
    {
        public BossChairController chair;
        public BossEncounter encounter;

        void Reset() => prompt = "SIT";

        void Awake()
        {
            if (chair == null) chair = GetComponent<BossChairController>();
            prompt = "SIT ON THE CHAIR";
        }

        public override bool CanInteract(InteractionContext context) =>
            base.CanInteract(context) && chair != null && chair.CanSit(context.Motor, out _);

        protected override void OnInteract(InteractionContext context)
        {
            if (encounter != null) encounter.TrySit(context.Motor);
        }
    }
}
