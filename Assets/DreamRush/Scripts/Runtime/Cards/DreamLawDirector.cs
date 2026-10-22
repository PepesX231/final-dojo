using System;
using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>
    /// "การ์ดข้อห้าม" from the pitch, implemented as GDD Dream Laws: during the chase a random rule card
    /// slams onto the screen (with a 2 s warning) and forbids one action for a short time, forcing a new plan.
    /// Rules act on the motor's rule modifiers only — no special cases elsewhere.
    /// </summary>
    public class DreamLawDirector : MonoBehaviour
    {
        public PlayerMotor3D player;
        public float firstLawAfter = 35f;
        public float interval = 40f;
        public float warning = 2f;
        public float lawDuration = 15f;
        public int seed = 1;
        public bool running;

        public CardDefinition ActiveLaw { get; private set; }
        public float ActiveRemaining { get; private set; }
        public int LawsPlayed { get; private set; }
        public event Action<CardDefinition, float> LawChanged; // law (null = none), duration

        List<CardDefinition> _laws;
        System.Random _rng;
        float _clock, _next;
        CardDefinition _pending, _last;
        bool _warned;

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMotor3D>();
            var lib = CardLibrary.Load();
            _laws = lib != null ? lib.dreamLaws : new List<CardDefinition>();
            _rng = new System.Random(seed);
            _next = firstLawAfter;
        }

        public void Begin(int runSeed)
        {
            seed = runSeed;
            _rng = new System.Random(seed);
            _clock = 0f;
            _next = firstLawAfter;
            running = true;
        }

        public void Stop()
        {
            running = false;
            if (ActiveLaw != null) EndLaw();
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (!running || _laws == null || _laws.Count == 0 || player == null) return;
            _clock += dt;
            if (ActiveLaw != null)
            {
                ActiveRemaining -= dt;
                if (ActiveRemaining <= 0f) EndLaw();
                return;
            }
            if (!_warned && _clock >= _next - warning)
            {
                _pending = PickLaw();
                _warned = true;
                if (UIRoot.Exists) UIRoot.Get().Toast("A DREAM LAW is being written...", warning);
                Audio.AudioManager.Play(Audio.Sfx.LawWarn, Vector3.zero);
            }
            if (_clock >= _next && _pending != null) StartLaw(_pending);
        }

        CardDefinition PickLaw()
        {
            CardDefinition pick;
            int guard = 0;
            do { pick = _laws[_rng.Next(_laws.Count)]; } while (pick == _last && _laws.Count > 1 && guard++ < 10);
            return pick;
        }

        void StartLaw(CardDefinition law)
        {
            ActiveLaw = law;
            ActiveRemaining = law.duration > 0 ? law.duration : lawDuration;
            _last = law;
            LawsPlayed++;
            Apply(law.lawRule, true);
            if (UIRoot.Exists) UIRoot.Get().Toast($"DREAM LAW:  {law.displayName}\n{law.description}", 3f);
            LawChanged?.Invoke(law, ActiveRemaining);
        }

        void EndLaw()
        {
            Apply(ActiveLaw.lawRule, false);
            ActiveLaw = null;
            _pending = null;
            _warned = false;
            _next = _clock + interval;
            if (UIRoot.Exists) UIRoot.Get().Toast("The Dream Law fades.", 1.5f);
            LawChanged?.Invoke(null, 0f);
        }

        void Apply(DreamLawRule rule, bool on)
        {
            switch (rule)
            {
                case DreamLawRule.NoJump: player.AllowJump = !on; break;
                case DreamLawRule.NoSprint: player.AllowSprint = !on; break;
                case DreamLawRule.Slippery: player.GroundGripScale = on ? 0.18f : 1f; break;
            }
        }

        void OnDestroy()
        {
            if (ActiveLaw != null && player != null) Apply(ActiveLaw.lawRule, false);
        }
    }
}
