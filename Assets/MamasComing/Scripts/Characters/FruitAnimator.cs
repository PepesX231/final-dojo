using UnityEngine;

namespace MamasComing
{
    /// Drives a fruit kid's baked walk loop (Blender object + shape-key animation) from the player's speed.
    public class FruitAnimator : MonoBehaviour
    {
        public Animator anim;
        public PlayerController player;
        public float walkRef = 3.2f;
        bool walking; Vector3 baseScale; float phase, speed, clipSeconds = 1.6f;
        Vector3 lastPosition;

        void Awake()
        {
            if (!anim) anim = GetComponentInChildren<Animator>();
            if (!player) player = GetComponentInParent<PlayerController>();
            baseScale = transform.localScale; phase = Random.value * 6f;
            if (anim && anim.runtimeAnimatorController)
                foreach (var clip in anim.runtimeAnimatorController.animationClips) if (clip.name.Contains("Walk")) { clipSeconds = clip.length; break; }
            lastPosition = player ? player.transform.position : transform.position;
        }

        void OnEnable() { walking = false; if (anim) anim.Play("Idle", 0, 0f); }

        void Update()
        {
            if (!anim || !player) return;
            float dt = Time.deltaTime; if (dt <= 0f) return;
            Vector3 delta = player.transform.position - lastPosition; delta.y = 0; lastPosition = player.transform.position;
            float actual = delta.magnitude < 2f ? delta.magnitude / dt : 0f;
            speed = Mathf.Lerp(speed, actual, 1f - Mathf.Exp(-12f * dt));
            float s = speed;
            bool grounded = !player.Motor || player.Motor.IsGrounded;
            bool w = s > (walking ? .12f : .3f) && !player.Stunned && grounded;
            if (w != walking) { walking = w; anim.CrossFadeInFixedTime(w ? "Walk" : "Idle", .18f); }
            float stride = player.Body ? player.Body.gait.stride : 1.5f;
            // Match the authored cycle to actual distance, including remote movement
            // and walking into walls. Apple uses its own procedural animator.
            float playback = Mathf.Clamp(s * clipSeconds / Mathf.Max(.8f, stride), .25f, 3.5f);
            anim.speed = Mathf.Lerp(anim.speed, w ? playback : 1f, 1f - Mathf.Exp(-10f * dt));
            // idle: gentle breathing squash so the fruit never looks frozen
            float b = w ? 0f : Mathf.Sin(Time.time * 3.2f + phase) * 0.025f;
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(baseScale.x * (1f - b * 0.5f), baseScale.y * (1f + b), baseScale.z * (1f - b * 0.5f)), 1f - Mathf.Exp(-12f * dt));
        }
    }
}
