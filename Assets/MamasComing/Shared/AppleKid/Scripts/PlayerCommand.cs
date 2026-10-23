using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// One frame of player intent. The motor only ever sees this struct — never the Input System —
    /// so tests, replays, cutscenes and AI can drive the same motor.
    /// </summary>
    public struct PlayerCommand
    {
        /// <summary>Stick/WASD in [-1,1]² (x = right, y = forward). Magnitude is respected (analog walk).</summary>
        public Vector2 Move;
        /// <summary>True only on the frame jump was pressed.</summary>
        public bool JumpPressed;
        /// <summary>True while jump is held (variable jump height).</summary>
        public bool JumpHeld;
        public bool Sprint;
        /// <summary>Dash pressed this frame (open-world move set).</summary>
        public bool DashPressed;
        /// <summary>Ground pound pressed this frame.</summary>
        public bool PoundPressed;
        /// <summary>Attack button went down this frame / is held (tap = light combo, hold = charged heavy).</summary>
        public bool AttackPressed, AttackHeld;
        /// <summary>Lock-on toggle pressed this frame.</summary>
        public bool LockPressed;
        /// <summary>Dodge roll (tap Sprint, like Elden Ring's tap-B) this frame.</summary>
        public bool RollPressed;
        /// <summary>DREAM BURST skill (V / gamepad Y) this frame.</summary>
        public bool SkillPressed;

        public static PlayerCommand None => default;
    }

    public interface IPlayerCommandSource
    {
        PlayerCommand ReadCommand();
    }
}
