using System;
using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Interaction
{
    /// <summary>
    /// Finds the best interactable near the player each frame and triggers it on Interact (E / west button).
    /// Uses a registry instead of physics so props need no trigger colliders.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class InteractionSystem : MonoBehaviour
    {
        public float range = 1.9f;
        public float maxAngle = 75f;

        static readonly List<IInteractable> Registry = new List<IInteractable>();
        public static void Register(IInteractable i) { if (!Registry.Contains(i)) Registry.Add(i); }
        public static void Unregister(IInteractable i) => Registry.Remove(i);
        public static IReadOnlyList<IInteractable> All => Registry;

        public IInteractable Current { get; private set; }
        public event Action<IInteractable> CurrentChanged;
        public event Action<IInteractable> Interacted;
        /// <summary>When false nothing is selected (menus, cutscenes, dreams without interactions).</summary>
        public bool Active { get; set; } = true;

        PlayerMotor3D _motor;
        PlayerInputReader _input;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor3D>();
            _input = GetComponent<PlayerInputReader>();
        }

        void Update()
        {
            var best = Active ? FindBest() : null;
            if (!ReferenceEquals(best, Current))
            {
                Current = best;
                CurrentChanged?.Invoke(Current);
            }
            if (Current != null && _input != null && !_input.GameplayBlocked && _input.InteractPressed)
                TryInteract();
        }

        public IInteractable FindBest()
        {
            var ctx = Context();
            IInteractable best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = Registry.Count - 1; i >= 0; i--)
            {
                var it = Registry[i];
                if (it == null || (it is UnityEngine.Object o && o == null)) { Registry.RemoveAt(i); continue; }
                if (!it.CanInteract(ctx)) continue;
                float s = InteractionScoring.Score(transform.position, transform.forward, it.Anchor.position, range, maxAngle);
                if (s > bestScore) { bestScore = s; best = it; }
            }
            return best;
        }

        public bool TryInteract()
        {
            if (Current == null) return false;
            var ctx = Context();
            if (!Current.CanInteract(ctx)) return false;
            var target = Current;
            target.Interact(ctx);
            Interacted?.Invoke(target);
            return true;
        }

        public InteractionContext Context() => new InteractionContext { Player = gameObject, Motor = _motor, System = this };
    }
}
