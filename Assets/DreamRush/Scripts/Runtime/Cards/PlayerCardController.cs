using System;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>
    /// The apple's equipped dream card (GDD §17.7: one active card in the first playable). Q / right shoulder
    /// uses it; cooldown only starts when the effect actually applied.
    /// </summary>
    public class PlayerCardController : MonoBehaviour
    {
        public CardDefinition Card { get; private set; }
        public float CooldownRemaining { get; private set; }
        public float CooldownNormalized => Card == null || Card.cooldown <= 0 ? 0 : Mathf.Clamp01(CooldownRemaining / Card.cooldown);
        public bool Ready => Card != null && CooldownRemaining <= 0f;
        public int Uses { get; private set; }

        public event Action<CardDefinition> CardChanged;
        public event Action<CardUseResult> Used;

        PlayerMotor3D _motor;
        PlayerInputReader _input;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor3D>();
            _input = GetComponent<PlayerInputReader>();
        }

        void Start()
        {
            if (Card != null) return;
            var lib = CardLibrary.Load();
            if (lib == null) return;
            var run = GameFlowManager.Exists ? GameFlowManager.Get().CurrentRun : null;
            CardDefinition chosen = run != null && !string.IsNullOrEmpty(run.ChosenCardId) ? lib.Find(run.ChosenCardId) : null;
            if (chosen == null && run != null && SceneContext.Current != null && SceneContext.Current.kind == SceneKind.Dream)
            {
                // dream scene played directly (no bedroom): deal one card from a seeded draw so cards are always testable
                var three = CardDrawManager.DrawThree(lib.starterPool, run.Seed);
                chosen = three.Count > 0 ? three[0] : null;
                run.ChosenCardId = chosen != null ? chosen.cardId : null;
                if (chosen != null && UIRoot.Exists) UIRoot.Get().Toast($"Dream card: {chosen.displayName}  [Q]", 3f);
            }
            if (chosen != null) Equip(chosen);
        }

        public void Equip(CardDefinition card)
        {
            Card = card;
            CooldownRemaining = 0f;
            CardChanged?.Invoke(card);
        }

        void Update()
        {
            if (CooldownRemaining > 0f) CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Time.deltaTime);
            if (_input != null && !_input.GameplayBlocked && _input.UseCardPressed) TryUse();
        }

        public CardUseResult TryUse()
        {
            if (Card == null) return new CardUseResult { Message = "No dream card" };
            if (CooldownRemaining > 0f)
            {
                var wait = new CardUseResult { Message = $"{Card.displayName} recharging ({CooldownRemaining:0.0}s)" };
                if (UIRoot.Exists) UIRoot.Get().Toast(wait.Message, 1.2f);
                return wait;
            }
            var res = CardEffectResolver.Use(Card, _motor);
            if (res.Used) { CooldownRemaining = Card.cooldown; Uses++; CardPresentation.Play(Card, res, _motor); }
            else Audio.AudioManager.Play(Audio.Sfx.CardFail, transform.position);
            if (UIRoot.Exists && !string.IsNullOrEmpty(res.Message)) UIRoot.Get().Toast(res.Message, 1.8f);
            Used?.Invoke(res);
            return res;
        }

        /// <summary>Tests: skip the remaining cooldown.</summary>
        public void ResetCooldown() => CooldownRemaining = 0f;
    }
}
