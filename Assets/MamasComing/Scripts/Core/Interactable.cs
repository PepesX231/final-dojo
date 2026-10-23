using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public string displayName = "Thing";
        public float interactRadius = 1.5f;
        public Transform interactAnchor;

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        public virtual Vector3 InteractPoint => interactAnchor ? interactAnchor.position : transform.position;
        public virtual bool CanInteract(PlayerController p) => true;
        public abstract string GetPrompt(PlayerController p);
        public abstract void Interact(PlayerController p);

        /// <summary>Seconds E must be held to complete (0 = instant). Turning valves, unclogging, etc.</summary>
        public virtual float HoldTime(PlayerController p) => 0f;
        /// <summary>Optional skill check that must be won before Interact() fires.</summary>
        public virtual MiniGame.Kind MiniGameKind(PlayerController p) => MiniGame.Kind.None;
        public virtual string MiniGameTitle => displayName;

        /// <summary>Called every frame while the hold is in progress.</summary>
        public virtual void HoldTick(PlayerController p, float progress01) { }
    }
}
