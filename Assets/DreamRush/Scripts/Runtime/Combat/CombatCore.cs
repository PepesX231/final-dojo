using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>One hit from the player's leaf blade (level 2 combat, docs/LEVEL2_DESIGN.md §3).</summary>
    public struct HitInfo
    {
        public float damage, posture;
        public Vector3 point, direction;
        public bool heavy, critical;
        /// <summary>Upward launch speed for juggles (rising slash, dream burst). 0 = none.</summary>
        public float launch;
        public string source;
    }

    /// <summary>Anything the player can hit (boss parts, breakables).</summary>
    public interface IHittable
    {
        bool TakeHit(HitInfo hit);
    }

    /// <summary>Who can hurt the player: everything goes through here so dodge i-frames, perfect dodges and knock-back are consistent.</summary>
    public interface IPlayerDamageReceiver
    {
        /// <returns>true = the hit landed (false = dodged / invulnerable).</returns>
        bool TryHurt(string reason, Vector3 from, float knockback = 7f);
    }
}
