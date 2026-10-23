using UnityEngine;

namespace DreamRush.Player
{
    public static class JumpMath
    {
        /// <summary>Initial vertical speed that reaches <paramref name="height"/> under constant gravity g: v = √(2gh).</summary>
        public static float InitialVelocity(float height, float gravity) => Mathf.Sqrt(2f * Mathf.Max(0f, gravity) * Mathf.Max(0f, height));

        /// <summary>Time to reach the apex for a jump of initial velocity v under gravity g.</summary>
        public static float TimeToApex(float v0, float gravity) => gravity <= 0f ? 0f : v0 / gravity;
    }

    public static class MovementMath
    {
        /// <summary>
        /// Converts 2D input into a world direction relative to the camera, projected on the plane defined by
        /// <paramref name="up"/>. Magnitude of the result equals the clamped input magnitude.
        /// </summary>
        public static Vector3 CameraRelative(Vector2 input, Vector3 cameraForward, Vector3 cameraRight, Vector3 up)
        {
            float mag = Mathf.Clamp01(input.magnitude);
            if (mag < 1e-4f) return Vector3.zero;

            Vector3 fwd = Vector3.ProjectOnPlane(cameraForward, up);
            if (fwd.sqrMagnitude < 1e-6f)
            {
                // Camera looking straight down/up: derive forward from its right vector.
                fwd = Vector3.ProjectOnPlane(Vector3.Cross(cameraRight, up), up);
            }
            fwd.Normalize();
            Vector3 right = Vector3.Cross(up, fwd).normalized;

            Vector3 dir = fwd * input.y + right * input.x;
            if (dir.sqrMagnitude < 1e-8f) return Vector3.zero;
            return dir.normalized * mag;
        }

        /// <summary>Chooses the horizontal acceleration to use this frame.</summary>
        public static float SelectAcceleration(PlayerMotorConfig c, bool grounded, Vector3 currentPlanar, Vector3 targetPlanar)
        {
            bool hasInput = targetPlanar.sqrMagnitude > 1e-4f;
            if (!grounded) return hasInput ? c.airAcceleration : c.airDeceleration;
            if (!hasInput) return c.groundDeceleration;
            // Pushing against current motion = braking/turnaround: use the stronger value.
            if (currentPlanar.sqrMagnitude > 0.25f && Vector3.Dot(currentPlanar.normalized, targetPlanar.normalized) < -0.2f)
                return c.turnaroundDeceleration;
            return c.groundAcceleration;
        }

        /// <summary>Gravity to apply this frame (positive number, applied downward).</summary>
        public static float SelectGravity(PlayerMotorConfig c, float verticalVelocity, bool isJumpRising, bool jumpHeld)
        {
            if (verticalVelocity < 0f) return c.gravity * c.fallGravityMultiplier;
            if (isJumpRising && !jumpHeld) return c.gravity * c.jumpCutGravityMultiplier;
            return c.gravity;
        }
    }
}
