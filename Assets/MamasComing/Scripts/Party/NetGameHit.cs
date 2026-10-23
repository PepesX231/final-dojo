using UnityEngine;

namespace MamasComing
{
    public static partial class NetGame
    {
        /// Sends a shove/bump to the player that owns the victim avatar (each client moves its own kid).
        public static System.Action<PlayerController, Vector3, float, float, bool> SendHit;

        /// Knock a player: applied locally if they are ours, otherwise relayed to their owner.
        public static void Hit(PlayerController victim, Vector3 dir, float force, float stun, bool dropHeld)
        {
            if (!victim) return;
            if (victim.isLocal || !Online || SendHit == null) { victim.Bonk(dir, force, stun, dropHeld); return; }
            SendHit(victim, dir, force, stun, dropHeld);
        }
    }
}
