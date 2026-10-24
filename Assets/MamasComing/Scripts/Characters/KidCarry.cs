using UnityEngine;

namespace MamasComing
{
    /// A knocked-down kid can be picked up by a friend, carried over their head and thrown (right click).
    /// Runs on the CARRIED kid's own machine (each player moves their own kid): it follows the carrier's hands,
    /// can wriggle free by mashing punch / jump, and flies when thrown. Commands arrive through NetGame.Hit
    /// with negative "force" codes so they travel on the existing reliable hit channel.
    [RequireComponent(typeof(PlayerController))]
    public class KidCarry : MonoBehaviour
    {
        public const float GrabCode = -1f, ThrowCode = -2f, DropCode = -3f;
        public const float MaxCarry = 5f;           // seconds before you slip out on your own
        public const int MashToEscape = 7;

        PlayerController me;
        public PlayerController Carrier { get; private set; }
        float carriedT; int mashes;

        void Awake() { me = GetComponent<PlayerController>(); }

        public bool Carried => Carrier;

        /// Victim side: apply a carry command sent by the friend at `from`.
        public static void Command(PlayerController victim, float code, Vector3 dir, Vector3 from)
        {
            if (!victim) return;
            var c = victim.GetComponent<KidCarry>(); if (!c) c = victim.gameObject.AddComponent<KidCarry>();
            if (code == GrabCode) c.Begin(Nearest(victim, from));
            else if (code == ThrowCode) c.Throw(dir);
            else c.Release(Vector3.zero);
        }

        static PlayerController Nearest(PlayerController victim, Vector3 from)
        {
            PlayerController best = null; float bd = 3f;
            foreach (var p in PlayerController.AllPlayers) { if (!p || p == victim) continue; Vector3 d = p.transform.position - from; d.y = 0; if (d.magnitude < bd) { bd = d.magnitude; best = p; } }
            return best;
        }

        void Begin(PlayerController carrier)
        {
            if (!carrier || !me.KnockedDown && !me.Stunned) return;   // only a curled-up (stunned) fruit can be lifted
            Carrier = carrier; carriedT = 0f; mashes = 0;
            if (me.Motor) me.Motor.InputEnabled = false;
            NetGame.Pop(transform.position + Vector3.up * 1.2f, "HUP!", "ฮึบ!", new Color(.6f, .95f, .6f), "whoosh", .6f, 1.3f);
        }

        public void Throw(Vector3 velocity)
        {
            if (!Carrier) return;
            var c = Carrier; Release(Vector3.zero);
            velocity.y = 0f;
            me.Bonk(velocity, Mathf.Clamp(velocity.magnitude, 3.5f, 7f), 1.3f, true);
            NetGame.Pop(transform.position + Vector3.up * 1.4f, "WHEEE!", "ปลิ้ววว!", new Color(1f, .75f, .3f), "whoosh", .8f, 1.1f);
        }

        void Release(Vector3 push)
        {
            Carrier = null; carriedT = 0f;
            if (me.Motor) me.Motor.InputEnabled = true;
        }

        void LateUpdate()
        {
            if (!Carrier) return;
            if (!Carrier.isActiveAndEnabled) { Release(Vector3.zero); return; }
            carriedT += Time.deltaTime;
            me.KeepStunned(PlayerController.RecoverTime + .3f);   // stay a curled-up fruit while carried
            // above the carrier's head, a little in front
            float top = 1.25f * Carrier.transform.lossyScale.y;
            Vector3 hold = Carrier.transform.position + Vector3.up * top + Carrier.transform.forward * .15f;
            if (me.Motor) me.Motor.Teleport(hold, Carrier.transform.rotation); else transform.position = hold;
            if (!me.isLocal) return;
            // wriggle free
            if (MamaInput.ShoveDown || MamaInput.ClickDown || UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                mashes++;
                if (mashes % 2 == 0) NetGame.Pop(transform.position + Vector3.up * .8f, "wiggle!", "ดิ้น!", Color.white, "squeak", .4f, 1.4f);
            }
            if (mashes >= MashToEscape || carriedT > MaxCarry)
            {
                var c = Carrier; Release(Vector3.zero);
                me.Bonk(c ? -c.transform.forward : -transform.forward, 3.5f, .6f, false);
                NetGame.Pop(transform.position + Vector3.up * 1.2f, "FREE!", "หลุดแล้ว!", new Color(.5f, .9f, 1f), "pop", .7f, 1.2f);
            }
        }
    }
}
