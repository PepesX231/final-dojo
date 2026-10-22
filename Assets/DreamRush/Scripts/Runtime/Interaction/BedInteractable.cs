using System.Collections;
using DreamRush.Flow;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Interaction
{
    /// <summary>Bed → GO TO SLEEP (GDD §7.5 Zone A, §22.1). Owns the poses used by the sleep sequence.</summary>
    public class BedInteractable : Interactable
    {
        [Tooltip("Where the apple lies down (position + facing of the body's front when lying).")]
        public Transform sleepPose;
        [Tooltip("Camera pose for the pillow close-up.")]
        public Transform pillowCamera;
        [Tooltip("Where the apple stands after waking up.")]
        public Transform wakePose;

        void Reset() => prompt = "GO TO SLEEP";

        public override bool CanInteract(InteractionContext context) =>
            base.CanInteract(context) && GameFlowManager.Get().State == FlowState.Hub;

        protected override void OnInteract(InteractionContext context) => GameFlowManager.Get().BeginSleep(this, context.Motor);
    }
}
