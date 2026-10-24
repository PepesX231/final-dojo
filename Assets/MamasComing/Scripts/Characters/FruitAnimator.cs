using UnityEngine;

namespace MamasComing
{
    /// Drives a fruit kid's baked walk loop (Blender object + shape-key animation) from the player's speed.
    public class FruitAnimator : MonoBehaviour
    {
        public Animator anim;
        public PlayerController player;
        public float walkRef = 3.2f;
        bool walking; Vector3 baseScale; float phase;

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
            float s = player.PlanarSpeed;
            bool w = s > 0.25f && !player.Stunned;
            if (w != walking) { walking = w; anim.CrossFadeInFixedTime(w ? "Walk" : "Idle", 0.12f); }
            anim.speed = w ? Mathf.Clamp(s / walkRef, 0.6f, 1.9f) : 1f;
            // idle: gentle breathing squash so the fruit never looks frozen
            float b = w ? 0f : Mathf.Sin(Time.time * 3.2f + phase) * 0.025f;
            transform.localScale = new Vector3(baseScale.x * (1f - b * 0.5f), baseScale.y * (1f + b), baseScale.z * (1f - b * 0.5f));
        }
    }
}
