using UnityEngine;

namespace MamasComing
{
    /// Plays the sculpted fruit kids' baked clips (Art/Sculpted, built by sculpt/kids.py) from the player's state:
    /// Idle, Walk, Run, Jump, Punch, Hit, plus a cheeky Taunt when left standing. Replaces FruitAnimator on those models.
    public class SculptKidMotion : MonoBehaviour
    {
        public Animator anim;
        public float walkRef = 3.2f, runRef = 5.5f, runAbove = 4.4f;
        public float tauntAfter = 7f;

        PlayerController player; DreamRush.Player.PlayerMotor3D motor;
        string cur; float lockUntil, still; bool wasStunned, wasPunching;

        void Awake() { if (!anim) anim = GetComponentInChildren<Animator>(); }
        void OnEnable() { cur = null; lockUntil = 0; }
        void OnDestroy() => Unhook();

        void Unhook()
        {
            if (motor) { motor.Jumped -= OnJump; motor.Landed -= OnLand; }
            motor = null;
        }

        void OnJump() { if (anim) { Cross("Jump", .04f); lockUntil = Time.time + .25f; } }
        void OnLand(float speed) { if (cur == "Jump") lockUntil = 0; }

        /// Celebrate (round won, chore done).
        public void Cheer(float seconds = 1.6f) { Cross("Cheer", .1f); lockUntil = Time.time + seconds; }

        void Cross(string state, float fade)
        {
            if (cur == state) return;
            cur = state; anim.speed = 1f;
            anim.CrossFadeInFixedTime(state, fade);
        }

        void Update()
        {
            var p = GetComponentInParent<PlayerController>();
            if (p != player) { Unhook(); player = p; }
            if (!anim || !player) return;
            if (!motor && player.Motor) { motor = player.Motor; motor.Jumped += OnJump; motor.Landed += OnLand; }

            bool stunned = player.Stunned;
            bool punching = player.actions && player.actions.Current == KidActionAnimator.Clip.Punch;
            if (stunned && !wasStunned) { Cross("Hit", .05f); lockUntil = Time.time + .9f; }
            if (punching && !wasPunching) { cur = null; Cross("Punch", .03f); lockUntil = Time.time + .45f; }
            wasStunned = stunned; wasPunching = punching;
            if (Time.time < lockUntil) return;

            float s = player.PlanarSpeed;
            bool grounded = !motor || motor.IsGrounded;
            still = s < .25f && grounded && !stunned && !player.Held ? still + Time.deltaTime : 0f;
            if (!grounded) { if (cur != "Jump") Cross("Jump", .12f); return; }
            if (s > runAbove || (player.Sprinting && s > .5f)) { Cross("Run", .12f); anim.speed = Mathf.Clamp(s / runRef, .7f, 1.6f); }
            else if (s > .25f) { Cross("Walk", .12f); anim.speed = Mathf.Clamp(s / walkRef, .6f, 1.9f); }
            else if (still > tauntAfter) { Cross("Taunt", .2f); lockUntil = Time.time + 2f; still = 0f; }
            else Cross("Idle", .18f);
        }
    }
}
