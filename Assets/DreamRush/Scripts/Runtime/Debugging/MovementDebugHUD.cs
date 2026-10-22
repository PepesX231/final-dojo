using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Debugging
{
    /// <summary>
    /// Development overlay (F3) for tuning and test evidence: speed, grounded, coyote/buffer timers,
    /// measured jump apex. IMGUI on purpose — dev only, not part of the game UI.
    /// </summary>
    public class MovementDebugHUD : MonoBehaviour
    {
        public PlayerMotor3D motor;
        public PlayerInputReader input;
        public bool visible = true;

        float _jumpStartY, _apexY, _lastApexHeight;
        bool _tracking;
        GUIStyle _style;

        void Awake()
        {
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor3D>();
            if (input == null && motor != null) input = motor.GetComponent<PlayerInputReader>();
        }

        void OnEnable() { if (motor != null) { motor.Jumped += OnJump; motor.Landed += OnLand; } }
        void OnDisable() { if (motor != null) { motor.Jumped -= OnJump; motor.Landed -= OnLand; } }

        void OnJump() { _tracking = true; _jumpStartY = motor.transform.position.y; _apexY = _jumpStartY; }
        void OnLand(float _) { if (_tracking) _lastApexHeight = _apexY - _jumpStartY; _tracking = false; }

        void Update()
        {
            if (input != null && input.DebugTogglePressed) visible = !visible;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f4Key.wasPressedThisFrame) DevCommands.FaceCamera();
            if (kb != null && kb.f5Key.wasPressedThisFrame) DevCommands.BackCamera();
            if (_tracking && motor != null) _apexY = Mathf.Max(_apexY, motor.transform.position.y);
        }

        void OnGUI()
        {
            if (!visible || motor == null) return;
            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14 };
            var a = motor.Assist;
            string txt =
                $"<b>DREAM RUSH · Movement (F3)</b>  F4 face cam · F5 back cam\n" +
                $"speed  {motor.PlanarVelocity.magnitude:F2} m/s  {(motor.IsSprinting ? "SPRINT" : "")}\n" +
                $"vy     {motor.VerticalVelocity:F2}\n" +
                $"ground {(motor.IsGrounded ? "YES" : "no")}   air {motor.AirTime:F2}s\n" +
                $"coyote {(a != null && a.CanUseCoyote ? "ready" : "-")}  since-ground {(a != null ? Fmt(a.TimeSinceGrounded) : "-")}\n" +
                $"buffer {(a != null && a.HasBufferedJump ? "QUEUED" : "-")}\n" +
                $"last apex {_lastApexHeight:F2} m   land {motor.LastLandingSpeed:F1} m/s\n" +
                $"pos {motor.transform.position.x:F1}, {motor.transform.position.y:F2}, {motor.transform.position.z:F1}";
            _style.richText = true;
            GUI.Box(new Rect(10, 10, 430, 170), txt, _style);
        }

        static string Fmt(float v) => float.IsInfinity(v) ? "∞" : v.ToString("F2");
    }
}
