using UnityEngine;

namespace MamasComing
{
    public static partial class NetGame
    {
        /// Sends a shove/bump to the player that owns the victim avatar (each client moves its own kid).
        public static System.Action<PlayerController, Vector3, float, float, bool, Vector3> SendHit;
        /// Relays a one-shot body animation of OUR kid to every other screen.
        public static System.Action<PlayerController, byte, float> SendAnim;
        /// Relays a comic word + sound at a spot to every other screen.
        public static System.Action<Vector3, string, string, Color, string, float, float> SendPop;

        /// How far apart (on the victim's own screen) a hit may still land: lag tolerance.
        public const float HitReach = 2.4f;

        /// Knock a player: applied locally if they are ours, otherwise relayed to their owner.
        public static void Hit(PlayerController victim, Vector3 dir, float force, float stun, bool dropHeld, Vector3 from)
        {
            if (!victim) return;
            if (victim.isLocal || !Online || SendHit == null) { ApplyHit(victim, dir, force, stun, dropHeld, from); return; }
            SendHit(victim, dir, force, stun, dropHeld, from);
        }

        /// Runs on the victim's own machine: their screen decides whether they had already dodged.
        public static void ApplyHit(PlayerController victim, Vector3 dir, float force, float stun, bool dropHeld, Vector3 from)
        {
            if (!victim) return;
            if (force < 0f) { KidCarry.Command(victim, force, dir, from); return; }   // grab / throw / drop a carried friend
            var rough = victim.GetComponent<KidRoughhouse>();
            if (rough && rough.Deflects(from)) return;                                // blocked, countered or still getting up
            Vector3 d = victim.transform.position - from; d.y = 0f;
            if (Debug.isDebugBuild) Debug.Log("[Fx] hit on " + victim.name + " dist=" + d.magnitude.ToString("0.00"));
            if (d.magnitude > HitReach) return;   // dodged on my screen: no hit
            victim.Bonk(dir, force, stun, dropHeld);
        }

        /// Comic word + sound everyone sees/hears at the same place.
        public static void Pop(Vector3 at, string en, string th, Color col, string sound, float vol = 0.9f, float pitch = 1f)
        {
            ShowPop(at, en, th, col, sound, vol, pitch);
            if (Online && SendPop != null) SendPop(at, en, th, col, sound, vol, pitch);
        }

        public static float LastPopTime = -1f;
        public static void ShowPop(Vector3 at, string en, string th, Color col, string sound, float vol, float pitch)
        {
            LastPopTime = Time.time; if (Debug.isDebugBuild) Debug.Log("[Fx] pop '" + en + "' sound=" + sound + " at " + at);
            if (!string.IsNullOrEmpty(sound)) MamaAudio.Play(sound, at, vol, pitch);
            string w = Loc.Thai && !string.IsNullOrEmpty(th) ? th : en;
            if (!string.IsNullOrEmpty(w)) KidRoughhouse.Pop(at, w, col);
        }
    }
}
