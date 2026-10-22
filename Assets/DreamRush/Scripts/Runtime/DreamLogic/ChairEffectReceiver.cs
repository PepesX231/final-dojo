using DreamRush.Boss;
using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>
    /// Translates environmental effects into chair statuses. Never wins the fight by itself:
    /// every effect produces a state the player must still exploit by walking up and sitting (§18.6).
    /// </summary>
    [RequireComponent(typeof(BossChairController))]
    public class ChairEffectReceiver : MonoBehaviour, IEffectReceiver
    {
        public DreamProperty properties = DreamProperty.SitAble | DreamProperty.Movable | DreamProperty.Rootable |
                                          DreamProperty.Freezeable | DreamProperty.Pullable | DreamProperty.SocialResponsive |
                                          DreamProperty.BossEntity | DreamProperty.Distractible;
        BossChairController _chair;

        public DreamProperty Properties => properties;
        public Transform Transform => transform;

        void Awake() => _chair = GetComponent<BossChairController>();
        void OnEnable() => EffectReceiverRegistry.Register(this);
        void OnDisable() => EffectReceiverRegistry.Unregister(this);

        public bool CanReceive(EffectContext c)
        {
            var need = EffectContext.RequiredProperty(c.Action);
            if (need != DreamProperty.None && (properties & need) == 0) return false;
            var s = _chair.State;
            return s != ChairState.Dormant && s != ChairState.Reveal && s != ChairState.Sitting && s != ChairState.Defeated;
        }

        public void ApplyEffect(EffectContext c)
        {
            switch (c.Action)
            {
                case EffectAction.Root: _chair.ApplyStatus(ChairState.Rooted, c.Duration, "Rooted"); break;
                case EffectAction.Freeze: _chair.ApplyStatus(ChairState.Frozen, c.Duration, "Frozen"); break;
                case EffectAction.Pull: _chair.BeginPulled(c.Source.transform.position, c.Duration); break;
                case EffectAction.ChangeBehavior: _chair.SetSocialCue(c.Duration); break;
            }
        }
    }
}
