using System;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Player
{
    /// <summary>
    /// Open-world move set (docs/OPENWORLD_DESIGN.md) layered on PlayerMotor3D — every move has a readable VFX/SFX:
    ///   Double jump  (Space in air)        flip + lavender ring
    ///   Dash         (L-Ctrl / RT)         1 in the air, cooldown on the ground; keeps momentum (sprint-jump-dash)
    ///   Glide        (hold Space falling)  pillow parachute, slow fall; updrafts lift you
    ///   Ground pound (C / B in air)        hang, slam, shockwave; jump right after landing = POUND JUMP (×1.5)
    /// Added only in scenes that want it (open world), so the P0 levels and their tests are unchanged.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public class PlayerAbilities : MonoBehaviour
    {
        public bool doubleJump = true, dash = true, glide = true, pound = true;
        public float doubleJumpScale = 0.92f;
        public float dashSpeed = 19f, dashTime = 0.17f, groundDashCooldown = 0.65f;
        public float glideFall = 2.3f, glideAirSpeed = 9.5f;
        public float poundSpeed = 30f, poundHang = 0.14f, poundJumpScale = 1.5f, poundJumpWindow = 0.3f;

        public bool UsedDoubleJump { get; private set; }
        public bool UsedAirDash { get; private set; }
        public bool Gliding { get; private set; }
        public bool Pounding { get; private set; }
        public float Updraft { get; set; }               // set by WindColumn each frame
        public event Action<Vector3> PoundLanded;
        public static event Action<Vector3, float> AnyPound;   // position, radius (stuns the Nightmare / trips the chair)

        PlayerMotor3D _m;
        IPlayerCommandSource _src;
        TrailRenderer _trail;
        Transform _model, _canopy;
        float _dashCd, _flipT = -1f, _poundT, _sincePoundLand = 99f, _jumpHeldTime;
        bool _wasGrounded;

        void Awake()
        {
            _m = GetComponent<PlayerMotor3D>();
            _src = GetComponent<IPlayerCommandSource>();
            _m.Jumped += OnJumped;
            _m.Landed += OnLanded;
            var vis = transform.Find("Visual");
            _model = vis != null && vis.childCount > 0 ? vis.GetChild(0) : vis;   // the model under the squash/stretch root
            BuildTrail();
            BuildCanopy();
        }

        void OnDestroy() { if (_m != null) { _m.Jumped -= OnJumped; _m.Landed -= OnLanded; } }

        void OnJumped()
        {
            if (_sincePoundLand < poundJumpWindow)
            {
                // POUND JUMP: the slam's energy goes back up
                var v = _m.Velocity; v.y *= poundJumpScale; _m.SetVelocity(v);
                VFX.Play(VFXKind.LandingRing, transform.position, 1.6f, VFX.Gold);
                VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 0.4f, 0.9f, VFX.Gold);
                Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 0.6f);
                _flipT = 0f;
            }
            _sincePoundLand = 99f;
        }

        void OnLanded(float impact)
        {
            UsedDoubleJump = false; UsedAirDash = false;
            if (Pounding)
            {
                Pounding = false;
                _sincePoundLand = 0f;
                VFX.Play(VFXKind.ImpactRing, transform.position, 1.1f, VFX.Lavender);
                VFX.Play(VFXKind.DustPuff, transform.position, 2f);
                CameraShake.Add(0.45f);
                Audio.AudioManager.Play(Audio.Sfx.Land, transform.position, 1f);
                PoundLanded?.Invoke(transform.position);
                AnyPound?.Invoke(transform.position, 5f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _dashCd -= dt; _sincePoundLand += dt;
            var src = _m.CommandSource ?? _src;
            var cmd = src != null ? src.ReadCommand() : PlayerCommand.None;
            if (!_m.InputEnabled) cmd = PlayerCommand.None;
            bool air = !_m.IsGrounded;
            _jumpHeldTime = cmd.JumpHeld ? _jumpHeldTime + dt : 0f;

            // ---- ground pound
            if (pound && air && cmd.PoundPressed && !Pounding && _m.AirTime > 0.08f)
            {
                Pounding = true; _poundT = 0f;
                _m.SetVelocity(Vector3.zero);
                Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 0.5f);
            }
            if (Pounding)
            {
                _poundT += dt;
                if (_poundT < poundHang) { _m.SetVelocity(Vector3.zero); _m.FallSpeedOverride = 0.01f; }
                else { _m.FallSpeedOverride = 0f; _m.SetVelocity(Vector3.down * poundSpeed); }
                SetModelSpin(Mathf.Clamp01(_poundT / poundHang) * 360f, Vector3.right);
                if (!air) Pounding = false;
                _wasGrounded = !air;
                return;
            }

            // ---- double jump (after coyote time — the motor owns the first jump)
            if (doubleJump && air && cmd.JumpPressed && !UsedDoubleJump && _m.AirTime > 0.13f)
            {
                UsedDoubleJump = true;
                var v = _m.PlanarVelocity;
                if (_m.MoveIntent.sqrMagnitude > 0.01f) v = _m.MoveIntent.normalized * Mathf.Max(v.magnitude, _m.Config.walkSpeed);
                _m.SetVelocity(v + Vector3.up * _m.Config.JumpVelocity * doubleJumpScale);
                _flipT = 0f;
                VFX.Play(VFXKind.LandingRing, transform.position, 0.9f, VFX.Lavender);
                VFX.Play(VFXKind.PaperBurst, transform.position + Vector3.up * 0.2f, 0.5f);
                Audio.AudioManager.Play(Audio.Sfx.Jump, transform.position, 0.9f);
            }

            // ---- dash
            if (dash && cmd.DashPressed && (air ? !UsedAirDash : _dashCd <= 0f))
            {
                if (air) UsedAirDash = true; else _dashCd = groundDashCooldown;
                Vector3 dir = _m.MoveIntent.sqrMagnitude > 0.01f ? _m.MoveIntent : transform.forward;
                _m.Dash(dir, dashSpeed, dashTime);
                if (_trail != null) { _trail.Clear(); _trail.emitting = true; }
                VFX.Play(VFXKind.Poof, transform.position + Vector3.up * 0.5f - dir.normalized * 0.4f, 0.7f, VFX.Lavender);
                Audio.AudioManager.Play(Audio.Sfx.Whoosh, transform.position, 1f);
                CameraShake.Add(0.12f);
            }
            if (_trail != null) _trail.emitting = _m.IsDashing || (_m.IsBallistic && air);

            // ---- glide (hold jump while falling, after the double jump or after 0.35 s of holding)
            bool wantGlide = glide && air && cmd.JumpHeld && _m.VerticalVelocity < 0.5f && (UsedDoubleJump || _jumpHeldTime > 0.35f) && !_m.IsDashing;
            Gliding = wantGlide;
            _m.FallSpeedOverride = Gliding ? glideFall : 0f;
            _m.ExternalVerticalAccel = Gliding ? Updraft : 0f;
            if (Gliding && _m.MoveIntent.sqrMagnitude > 0.01f)
            {
                Vector3 pv = _m.PlanarVelocity;
                Vector3 want = _m.MoveIntent.normalized * glideAirSpeed;
                _m.SetVelocity(Vector3.MoveTowards(pv, want, 14f * dt) + Vector3.up * _m.VerticalVelocity);
            }
            Updraft = 0f;
            if (_canopy != null)
            {
                float target = Gliding ? 1f : 0f;
                float s = Mathf.MoveTowards(_canopy.localScale.x, target, dt * 6f);
                _canopy.localScale = Vector3.one * s;
                _canopy.gameObject.SetActive(s > 0.01f);
                _canopy.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 4f) * 6f, 0, Mathf.Cos(Time.time * 3f) * 6f);
            }
            _wasGrounded = !air;
        }

        void LateUpdate()
        {
            if (_flipT >= 0f)
            {
                _flipT += Time.deltaTime / 0.38f;
                SetModelSpin(UI.UIStyle.OutCubic(_flipT) * 360f, Vector3.right);
                if (_flipT >= 1f) { _flipT = -1f; SetModelSpin(0f, Vector3.right); }
            }
        }

        void SetModelSpin(float deg, Vector3 axis)
        {
            if (_model == null) return;
            // spin around the body centre (0.6 m up), not the feet
            Vector3 pivot = new Vector3(0, 0.6f, 0);
            Quaternion q = Quaternion.AngleAxis(deg, axis);
            _model.localRotation = q;
            _model.localPosition = pivot - q * pivot;
        }

        void BuildTrail()
        {
            var go = new GameObject("DashTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0.6f, 0);
            _trail = go.AddComponent<TrailRenderer>();
            _trail.time = 0.28f; _trail.minVertexDistance = 0.1f; _trail.emitting = false;
            _trail.widthCurve = new AnimationCurve(new Keyframe(0, 0.75f), new Keyframe(1, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 1f), 0), new GradientColorKey(VFX.Lavender, 0.5f), new GradientColorKey(new Color(0.4f, 0.5f, 1f), 1) },
                      new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0f, 1) });
            _trail.colorGradient = g;
            var lib = VFXLibrary.Instance;
            _trail.sharedMaterial = lib != null && lib.glowAdd != null ? lib.glowAdd : null;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void BuildCanopy()
        {
            // a little pillow parachute (presentation): lavender puff + 2 strings
            var root = new GameObject("GlideCanopy").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0, 1.75f, 0);
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(puff.GetComponent<Collider>());
            puff.transform.SetParent(root, false);
            puff.transform.localScale = new Vector3(1.5f, 0.42f, 1.1f);
            puff.GetComponent<Renderer>().sharedMaterial = Look.DreamLook.Toon(new Color(0.75f, 0.64f, 1f), null, 0.35f, name: "M_GlidePillow");
            for (int s = -1; s <= 1; s += 2)
            {
                var str = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(str.GetComponent<Collider>());
                str.transform.SetParent(root, false);
                str.transform.localPosition = new Vector3(s * 0.45f, -0.5f, 0);
                str.transform.localRotation = Quaternion.Euler(0, 0, s * -22f);
                str.transform.localScale = new Vector3(0.025f, 0.9f, 0.025f);
                str.GetComponent<Renderer>().sharedMaterial = Look.DreamLook.Toon(new Color(0.95f, 0.92f, 0.85f), null, 0f, name: "M_GlideString");
            }
            root.localScale = Vector3.zero;
            root.gameObject.SetActive(false);
            _canopy = root;
        }
    }
}
