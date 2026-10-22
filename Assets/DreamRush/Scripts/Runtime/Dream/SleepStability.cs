using System;
using DreamRush.Core;
using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// Dream "health" (GDD §22.3, §21.3): falls and hazards cost stability; at zero the dream ends (mom wakes you).
    /// Max = 3 + Cloud Mattress level. Comfort Blanket gives one free hit per run (applied in M7 upgrades).
    /// </summary>
    public class SleepStability : MonoBehaviour
    {
        public static SleepStability Current { get; private set; }

        public int baseMax = 3;
        public float invulnerableSeconds = 1.2f;

        public int Max { get; private set; }
        public int Value { get; private set; }
        public int Shields { get; set; }
        public bool Depleted => Value <= 0;

        public event Action<int, int> Changed;   // value, max
        public event Action<string> Damaged;      // reason
        public event Action DepletedEvent;

        float _invulnerableUntil;

        void Awake()
        {
            Current = this;
            var save = SaveManager.Instance ?? SaveManager.GetOrCreate();
            Max = baseMax + Progression.BedUpgrades.BonusStability(save.Data.progression);
            Value = Max;
            Shields = Progression.BedUpgrades.StartShields(save.Data.progression);
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Start()
        {
            var level = DreamLevelManager.Current;
            if (level != null) level.Fell += OnFell;
            Changed?.Invoke(Value, Max);
        }

        void OnFell() => Damage("fell into the dream void", ignoreInvulnerability: true);

        /// <summary>Restore moons (rest at a café, warm milk). Returns how many were restored.</summary>
        public int Heal(int amount = 99)
        {
            int before = Value; Value = Mathf.Min(Max, Value + amount);
            if (Value != before) Changed?.Invoke(Value, Max);
            return Value - before;
        }

        /// <summary>Permanent (this run) extra moon — the Watchmaker's "Extra Moon".</summary>
        public void AddMax(int n = 1) { Max += n; Value += n; Changed?.Invoke(Value, Max); }

        public bool Damage(string reason, bool ignoreInvulnerability = false)
        {
            if (Depleted) return false;
            if (!ignoreInvulnerability && Time.time < _invulnerableUntil) return false;
            _invulnerableUntil = Time.time + invulnerableSeconds;
            if (Shields > 0) { Shields--; Damaged?.Invoke(reason + " (blanket absorbed it)"); return true; }
            Value = Mathf.Max(0, Value - 1);
            Audio.AudioManager.Play(Audio.Sfx.Hurt, transform.position);
            var face = FindFirstObjectByType<Player.AppleFace>();
            if (face != null) face.Squeeze(0.6f);
            Presentation.CameraShake.Add(0.3f);
            Changed?.Invoke(Value, Max);
            Damaged?.Invoke(reason);
            if (Value == 0) DepletedEvent?.Invoke();
            return true;
        }
    }
}
