using UnityEngine;

namespace MamasComing
{
    /// Drives a fruit kid's baked walk loop (Blender object + shape-key animation) from the player's speed.
    public class FruitAnimator : MonoBehaviour
    {
        public Animator anim;
        public PlayerController player;
        public float walkRef = 3.2f;
        bool walking; Vector3 baseScale; float phase, smoothSpeed;

        void Awake()
        {
            if (!anim) anim = GetComponentInChildren<Animator>();
            if (!player) player = GetComponentInParent<PlayerController>();
            baseScale = transform.localScale; phase = Random.value * 6f;
        }

        void OnEnable() { walking = false; if (anim) anim.Play("Idle", 0, 0f); }
        void Start() { if (!GetComponent<ReadableFruitGait>()) gameObject.AddComponent<ReadableFruitGait>(); }

        void Update()
        {
            if (!anim || !player) return;
            smoothSpeed = Mathf.Lerp(smoothSpeed, player.PlanarSpeed, 1f - Mathf.Exp(-12f * Time.deltaTime));
            float s = smoothSpeed;
            bool w = s > 0.25f && !player.Stunned;
            if (w != walking) { walking = w; anim.CrossFadeInFixedTime(w ? "Walk" : "Idle", 0.12f); }
            anim.speed = w ? Mathf.Clamp(s / walkRef, 0.6f, 1.9f) : 1f;
            // idle: gentle breathing squash so the fruit never looks frozen
            float b = w ? 0f : Mathf.Sin(Time.time * 3.2f + phase) * 0.025f;
            bool airborne = player.Motor && !player.Motor.IsGrounded && !player.Stunned;
            Vector3 goal = new Vector3(baseScale.x * (1f - b * 0.5f) * (airborne ? .97f : 1f), baseScale.y * (1f + b) * (airborne ? 1.06f : 1f), baseScale.z * (1f - b * 0.5f) * (airborne ? .97f : 1f));
            transform.localScale = Vector3.Lerp(transform.localScale, goal, 1f - Mathf.Exp(-14f * Time.deltaTime));
        }
    }
}
