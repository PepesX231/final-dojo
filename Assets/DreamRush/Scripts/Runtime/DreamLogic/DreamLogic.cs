using System;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.DreamLogic
{
    /// <summary>Reusable object properties (GDD §18.2). Effects check these — never object identity.</summary>
    [Flags]
    public enum DreamProperty
    {
        None = 0,
        SitAble = 1 << 0,
        Movable = 1 << 1,
        Rootable = 1 << 2,
        Freezeable = 1 << 3,
        Pullable = 1 << 4,
        SocialResponsive = 1 << 5,
        BossEntity = 1 << 6,
        Distractible = 1 << 7,
    }

    /// <summary>Shared environmental and combat effect actions.</summary>
    public enum EffectAction { Root, Freeze, Pull, ChangeBehavior, SpawnDecoy, CreatePlatform }

    public struct EffectContext
    {
        public EffectAction Action;
        public PlayerMotor3D Source;
        public float Duration;
        public Vector3 Point;

        public static DreamProperty RequiredProperty(EffectAction a) => a switch
        {
            EffectAction.Root => DreamProperty.Rootable,
            EffectAction.Freeze => DreamProperty.Freezeable,
            EffectAction.Pull => DreamProperty.Pullable,
            EffectAction.ChangeBehavior => DreamProperty.SocialResponsive,
            EffectAction.SpawnDecoy => DreamProperty.Distractible,
            _ => DreamProperty.None,
        };
    }

    /// <summary>GDD §18.5.</summary>
    public interface IEffectReceiver
    {
        DreamProperty Properties { get; }
        Transform Transform { get; }
        bool CanReceive(EffectContext context);
        void ApplyEffect(EffectContext context);
    }

    /// <summary>Win condition contract (GDD §18.5) — the chair's is "sat on for 2 s".</summary>
    public interface IBossWinCondition
    {
        bool IsSatisfied(BossContext context);
    }

    public struct BossContext
    {
        public float SitSeconds;
        public float RequiredSitSeconds;
        public bool SitStateValid;
    }

    /// <summary>The chair's rule: sitting must be continuous and long enough (GDD §15.9).</summary>
    public sealed class SitForSecondsWinCondition : IBossWinCondition
    {
        public bool IsSatisfied(BossContext c) => c.SitStateValid && c.SitSeconds >= c.RequiredSitSeconds;
    }
}
