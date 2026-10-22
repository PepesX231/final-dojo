using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>Hidden fatigue meter (GDD §15.7). Pure logic.</summary>
    public sealed class ChairFatigue
    {
        public float Value { get; private set; }
        public float Threshold { get; }

        public ChairFatigue(float threshold) { Threshold = threshold; }

        public bool IsExhausted => Value >= Threshold;
        public float Normalized => Threshold <= 0 ? 0 : Mathf.Clamp01(Value / Threshold);

        public void Run(float dt, float rate) => Value = Mathf.Min(Threshold, Value + rate * dt);
        public void Rest(float dt, float rate) => Value = Mathf.Max(0f, Value - rate * dt);

        /// <summary>After an exhaustion the chair recovers to a fraction (so the next opening needs another chase).</summary>
        public void RecoverTo(float fraction) => Value = Threshold * Mathf.Clamp01(fraction);

        /// <summary>Pushback when the player stumbles (pitch: hitting obstacles loses ground).</summary>
        public void Refresh(float fraction) => Value = Mathf.Max(0f, Value - Threshold * fraction);
    }
}
