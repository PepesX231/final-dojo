using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing
{
    /// <summary>Thin wrapper over the Input System (project uses Input System only).</summary>
    public static class MamaInput
    {
        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
                }
                var g = Gamepad.current;
                if (g != null)
                {
                    Vector2 s = g.leftStick.ReadValue();
                    if (s.sqrMagnitude > v.sqrMagnitude) v = s;
                }
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public static bool Sprint =>
            (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed);

        public static bool InteractDown =>
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        public static bool InteractHeld =>
            (Keyboard.current != null && Keyboard.current.eKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.isPressed);

        public static bool DropDown =>
            (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        /// <summary>Debug/testing hook: force the 'use held item' button.</summary>
        public static bool ForceUse;

        public static bool UseHeld => ForceUse ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
            (Keyboard.current != null && Keyboard.current.fKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);

        public static bool ThrowDown =>
            (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        /// Shove / playful punch (G, gamepad RB).
        public static bool ShoveDown =>
            (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        public static bool ClickDown => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool PunchHeld => (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
            (Keyboard.current != null && Keyboard.current.gKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.rightShoulder.isPressed);

        public static Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static Vector2 StickLook => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
        public static float Zoom => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;

        public static bool AnyStart =>
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        public static bool RestartDown => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool EscDown => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        /// <summary>Dev shortcut: F5 makes Mom arrive almost immediately.</summary>
        /// <summary>Dev shortcut: F6 switches your kid to the next fruit (until the lobby picker sets it).</summary>
        public static bool DevNextKid => Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame;
        public static bool DevPreviousKid => Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame;
        public static bool DevSlip => Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
        public static int FruitPressed
        {
            get
            {
                var k = Keyboard.current; if (k == null) return -1;
                if (k.digit1Key.wasPressedThisFrame) return KidLook.Apple;
                if (k.digit2Key.wasPressedThisFrame) return KidLook.Banana;
                if (k.digit3Key.wasPressedThisFrame) return KidLook.Orange;
                if (k.digit4Key.wasPressedThisFrame) return KidLook.Watermelon;
                if (k.digit5Key.wasPressedThisFrame) return KidLook.WaxApple;
                return -1;
            }
        }
        public static bool DevSkip => Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
    }
}
