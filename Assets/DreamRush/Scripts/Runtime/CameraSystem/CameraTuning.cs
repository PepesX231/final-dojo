namespace DreamRush.CameraSystem
{
    /// <summary>
    /// Locked camera values from the visual reconstruction (docs/VISUAL_RECONSTRUCTION.md §2 Camera):
    /// closer, lower 3/4 so the apple reads (≥ 12% screen height) while escape routes stay visible.
    /// </summary>
    public static class CameraTuning
    {
        public const float ArenaPitch = 33f, ArenaDistance = 10.5f, ArenaFov = 40f;
        public const float PathPitch = 30f, PathDistance = 9.5f, PathFov = 42f;

        public static void ApplyArena(StageCameraController c)
        {
            c.pitch = ArenaPitch; c.distance = ArenaDistance; c.fieldOfView = ArenaFov;
            c.focusHeight = 0.7f; c.lookAhead = 1.6f;
            c.secondaryWeight = 0.3f; c.maxExtraDistance = 3.5f;
        }

        /// <summary>
        /// v2 (user feedback 2026-10-17: "too far, looks like top view"): close third-person behind the apple,
        /// Mario-Odyssey/Astro-Bot spirit. Free orbit + auto-follow, jump deadzone, sprint pull-out, boss focus.
        /// </summary>
        public static void ApplyFollow(ThirdPersonCameraController c)
        {
            c.fieldOfView = 52f; c.distance = 4.6f; c.defaultPitch = 13f; c.Pitch = 13f;
            c.pitchMin = -12f; c.pitchMax = 55f; c.targetHeight = 1.05f; c.minDistance = 1.2f;
            c.autoFollowYawSpeed = 75f; c.autoFollowDelay = 1.1f;
            c.verticalDeadzone = 1.6f; c.sprintExtraDistance = 1.3f;
            c.focusRadius = 24f; c.focusMaxExtraDistance = 3.2f;
            c.followSmoothTime = 0.07f;
        }

        public static void ApplyPath(StageCameraController c)
        {
            c.pitch = PathPitch; c.distance = PathDistance; c.fieldOfView = PathFov;
            c.focusHeight = 0.7f; c.lookAhead = 1.8f;
        }
    }
}
