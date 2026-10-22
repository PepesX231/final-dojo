using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Interaction
{
    /// <summary>Who is interacting (GDD §18.5 IInteractable).</summary>
    public struct InteractionContext
    {
        public GameObject Player;
        public PlayerMotor3D Motor;
        public InteractionSystem System;
    }

    public interface IInteractable
    {
        /// <summary>Short verb phrase shown as "[E] PROMPT".</summary>
        string Prompt { get; }
        Transform Anchor { get; }
        bool CanInteract(InteractionContext context);
        void Interact(InteractionContext context);
    }

    /// <summary>Pure scoring so selection rules are testable: nearer + more in front of the player wins.</summary>
    public static class InteractionScoring
    {
        /// <returns>Score (higher is better) or float.NegativeInfinity when out of range / behind.</returns>
        public static float Score(Vector3 playerPos, Vector3 playerForward, Vector3 targetPos, float range, float maxAngle)
        {
            Vector3 to = targetPos - playerPos;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > range) return float.NegativeInfinity;
            Vector3 fwd = playerForward; fwd.y = 0f;
            float angle = dist < 0.35f ? 0f : Vector3.Angle(fwd.sqrMagnitude > 1e-6f ? fwd : Vector3.forward, to);
            if (angle > maxAngle) return float.NegativeInfinity;
            return -(dist / range) * 1.0f - (angle / 180f) * 0.8f;
        }
    }

    /// <summary>
    /// Base component for world objects with an interaction. Subclasses override <see cref="OnInteract"/>.
    /// The optional <see cref="focusPose"/> is where the menu camera moves (GDD §8.3).
    /// </summary>
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        [SerializeField] protected string prompt = "INTERACT";
        [Tooltip("Point used for range/angle checks. Defaults to this transform.")]
        [SerializeField] protected Transform anchor;
        [Tooltip("Optional camera pose used when this object opens a menu.")]
        public Transform focusPose;
        public bool interactable = true;

        public virtual string Prompt => prompt;
        public Transform Anchor => anchor != null ? anchor : transform;
        public virtual bool CanInteract(InteractionContext context) => interactable && isActiveAndEnabled;
        public void Interact(InteractionContext context) => OnInteract(context);
        protected abstract void OnInteract(InteractionContext context);

        public void SetPrompt(string p) => prompt = p;

        protected virtual void OnEnable() => InteractionSystem.Register(this);
        protected virtual void OnDisable() => InteractionSystem.Unregister(this);
    }
}
