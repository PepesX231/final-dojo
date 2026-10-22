using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>Movement tuning (GDD §9.3). Prototype values — tune through play, not by guessing.</summary>
    [CreateAssetMenu(menuName = "DreamRush/Player Motor Config", fileName = "PlayerMotorConfig")]
    public class PlayerMotorConfig : ScriptableObject
    {
        [Header("Ground")]
        [Min(0)] public float walkSpeed = 5.5f;
        [Min(0)] public float sprintSpeed = 8.0f;
        [Min(0)] public float groundAcceleration = 28f;
        [Min(0)] public float groundDeceleration = 35f;
        [Tooltip("Extra deceleration when input points against current velocity (brake/turnaround).")]
        [Min(0)] public float turnaroundDeceleration = 55f;
        [Min(0)] public float rotationSpeedDegrees = 720f;

        [Header("Air")]
        [Min(0)] public float airAcceleration = 13f;
        [Tooltip("Horizontal slow-down in the air when there is no input.")]
        [Min(0)] public float airDeceleration = 4f;
        [Min(0)] public float airRotationSpeedDegrees = 420f;

        [Header("Jump")]
        [Min(0.1f)] public float jumpHeight = 2.1f;
        [Min(0.1f)] public float gravity = 24f;
        [Min(1f)] public float fallGravityMultiplier = 1.35f;
        [Tooltip("Gravity multiplier while rising after jump was released (variable jump height).")]
        [Min(1f)] public float jumpCutGravityMultiplier = 2.6f;
        [Min(0)] public float coyoteTime = 0.12f;
        [Min(0)] public float jumpBuffer = 0.14f;
        [Min(1f)] public float maxFallSpeed = 24f;

        [Header("Grounding")]
        [Min(0)] public float groundSnapDistance = 0.2f;
        [Range(0, 89)] public float maxWalkableSlope = 45f;
        [Tooltip("Downward velocity applied while grounded so the controller keeps contact.")]
        [Min(0)] public float groundStickVelocity = 2f;

        public float JumpVelocity => JumpMath.InitialVelocity(jumpHeight, gravity);

        public static PlayerMotorConfig CreateDefault()
        {
            var c = CreateInstance<PlayerMotorConfig>();
            c.name = "PlayerMotorConfig (runtime default)";
            return c;
        }
    }
}
