using UnityEngine;
using UnityEngine.InputSystem;

namespace DreamRush.Player
{
    /// <summary>
    /// Reads keyboard/mouse + gamepad through the Input System (GDD §9.2) and exposes it as
    /// <see cref="PlayerCommand"/> plus camera/look and UI intents. Actions are created in code so
    /// bindings can be overridden at runtime (remapping, GDD §23.6) via <see cref="Actions"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour, IPlayerCommandSource
    {
        public InputActionMap Actions { get; private set; }

        InputAction _move, _lookMouse, _lookStick, _jump, _sprint, _interact, _pause, _objective, _debug, _focus, _dash, _pound, _attack, _lock, _skill;
        float _sprintDownAt = -1f;

        /// <summary>Mouse delta in pixels this frame.</summary>
        public Vector2 LookMouseDelta => _lookMouse != null ? _lookMouse.ReadValue<Vector2>() : Vector2.zero;
        /// <summary>Right stick in [-1,1]².</summary>
        public Vector2 LookStick => _lookStick != null ? _lookStick.ReadValue<Vector2>() : Vector2.zero;
        public bool InteractPressed => _interact != null && _interact.WasPressedThisFrame();
        public bool InteractHeld => _interact != null && _interact.IsPressed();
        public bool PausePressed => _pause != null && _pause.WasPressedThisFrame();
        public bool ObjectivePressed => _objective != null && _objective.WasPressedThisFrame();
        public bool DebugTogglePressed => _debug != null && _debug.WasPressedThisFrame();
        /// <summary>Hold to frame the boss (F / right mouse / L1).</summary>
        public bool FocusHeld => _focus != null && _focus.IsPressed();
        public bool FocusPressed => _focus != null && _focus.WasPressedThisFrame();

        /// <summary>Gameplay commands are blocked (menus) but the reader keeps UI actions alive.</summary>
        public bool GameplayBlocked { get; set; }

        void Awake() => Build();

        void Build()
        {
            if (Actions != null) return;
            Actions = new InputActionMap("Gameplay");

            _move = Actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _lookMouse = Actions.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta", expectedControlLayout: "Vector2");
            _lookStick = Actions.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlLayout: "Vector2");

            _jump = Actions.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _jump.AddBinding("<Gamepad>/buttonSouth");

            _sprint = Actions.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            _sprint.AddBinding("<Gamepad>/leftTrigger");

            _interact = Actions.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/buttonWest");


            _pause = Actions.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            _pause.AddBinding("<Gamepad>/start");

            _objective = Actions.AddAction("Objective", InputActionType.Button, "<Keyboard>/tab");
            _objective.AddBinding("<Gamepad>/select");

            _debug = Actions.AddAction("DebugHUD", InputActionType.Button, "<Keyboard>/f3");

            _dash = Actions.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftCtrl");
            _dash.AddBinding("<Mouse>/forwardButton");
            _dash.AddBinding("<Gamepad>/rightTrigger");
            _pound = Actions.AddAction("Pound", InputActionType.Button, "<Keyboard>/c");
            _pound.AddBinding("<Gamepad>/buttonEast");

            _attack = Actions.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            _attack.AddBinding("<Keyboard>/j");
            _attack.AddBinding("<Gamepad>/rightShoulder");
            _lock = Actions.AddAction("LockOn", InputActionType.Button, "<Mouse>/middleButton");
            _lock.AddBinding("<Keyboard>/r");
            _lock.AddBinding("<Gamepad>/rightStickPress");
            _focus = Actions.AddAction("Focus", InputActionType.Button, "<Keyboard>/f");
            _focus.AddBinding("<Mouse>/rightButton");
            _focus.AddBinding("<Gamepad>/leftShoulder");
            _skill = Actions.AddAction("Skill", InputActionType.Button, "<Keyboard>/v");
            _skill.AddBinding("<Gamepad>/buttonNorth");
        }

        void OnEnable() { Build(); Actions.Enable(); }
        void OnDisable() { Actions?.Disable(); }
        void OnDestroy() { Actions?.Dispose(); Actions = null; }

        int _tapFrame = -1;
        /// <summary>Sprint released within 0.22 s of pressing = a tap (dodge roll). Stable for the whole frame.</summary>
        bool SprintTapped()
        {
            if (_sprint.WasPressedThisFrame()) _sprintDownAt = Time.unscaledTime;
            if (_sprint.WasReleasedThisFrame() && _sprintDownAt >= 0f && Time.unscaledTime - _sprintDownAt < 0.22f) _tapFrame = Time.frameCount;
            return _tapFrame == Time.frameCount;
        }

        public PlayerCommand ReadCommand()
        {
            if (GameplayBlocked || Actions == null) return PlayerCommand.None;
            return new PlayerCommand
            {
                Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f),
                JumpPressed = _jump.WasPressedThisFrame(),
                JumpHeld = _jump.IsPressed(),
                Sprint = _sprint.IsPressed(),
                DashPressed = _dash.WasPressedThisFrame(),
                PoundPressed = _pound.WasPressedThisFrame(),
                AttackPressed = _attack.WasPressedThisFrame(),
                AttackHeld = _attack.IsPressed(),
                LockPressed = _lock.WasPressedThisFrame() || _focus.WasPressedThisFrame(),
                RollPressed = SprintTapped(),
                SkillPressed = _skill != null && _skill.WasPressedThisFrame(),
            };
        }
    }
}
