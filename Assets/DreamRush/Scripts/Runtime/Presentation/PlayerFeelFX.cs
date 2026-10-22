using DreamRush.Audio;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// Player game-feel feedback (GDD §20.6, game-feel-polish skill): jump dust, landing puff scaled by impact,
    /// tiny shake on hard landings, footsteps synced to the procedural gait. Presentation only.
    /// </summary>
    public class PlayerFeelFX : MonoBehaviour
    {
        PlayerMotor3D _motor;
        AppleProceduralAnimator _anim;
        float _lastStepPhase;

        TrailRenderer _trailL, _trailR;
        bool _wasSprinting; float _sprintFx;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor3D>();
            _anim = GetComponentInChildren<AppleProceduralAnimator>();
        }

        void Start()
        {
            // M15 run VFX: speed ribbons from both heels while sprinting (anime "zoom" streaks)
            _trailL = HeelTrail("Leg_L_Lower", "Leg_L"); _trailR = HeelTrail("Leg_R_Lower", "Leg_R");
        }

        TrailRenderer HeelTrail(string part, string fallback)
        {
            var vis = transform.Find("Visual"); if (vis == null) return null;
            Transform leg = null;
            foreach (var t in vis.GetComponentsInChildren<Transform>(true)) { if (t.name == part) { leg = t; break; } if (t.name == fallback && leg == null) leg = t; }
            if (leg == null) return null;
            var go = new GameObject("HeelTrail"); go.transform.SetParent(leg, false);
            var mf = leg.GetComponent<MeshFilter>();
            var b = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(new Vector3(0, -0.15f, 0), Vector3.one * 0.2f);
            go.transform.localPosition = new Vector3(b.center.x, b.min.y + 0.05f, b.min.z + 0.02f);   // the heel tab (model front = +Z)
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.22f; tr.minVertexDistance = 0.04f; tr.emitting = false; tr.widthMultiplier = 0.1f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0.6f), new GradientColorKey(new Color(0.7f, 0.6f, 1f), 1) },
                      new[] { new GradientAlphaKey(0.85f, 0), new GradientAlphaKey(0f, 1) });
            tr.colorGradient = g;
            var lib = VFXLibrary.Instance; if (lib != null && lib.glowAdd != null) tr.sharedMaterial = lib.glowAdd;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        void OnEnable()
        {
            if (_motor == null) return;
            _motor.Jumped += OnJumped;
            _motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (_motor == null) return;
            _motor.Jumped -= OnJumped;
            _motor.Landed -= OnLanded;
        }

        Vector3 Feet => transform.position + Vector3.up * 0.15f;

        void OnJumped()
        {
            var fx = FXLibrary.Instance;
            if (fx != null) WorldFX.Spawn(fx.dust, Feet - transform.forward * 0.25f, 0.55f, 0.3f, null, 0f);
            AudioManager.Play(Sfx.Jump, transform.position);
        }

        void OnLanded(float impact)
        {
            float k = Mathf.InverseLerp(3f, 16f, impact);
            var fx = FXLibrary.Instance;
            if (fx != null && impact > 3f) WorldFX.Spawn(fx.dust, Feet, Mathf.Lerp(0.5f, 1.2f, k), 0.32f, null, 0f);
            if (impact > 3f) { VFX.Play(VFXKind.DustPuff, Feet, Mathf.Lerp(0.8f, 1.5f, k)); if (impact > 7f) VFX.Play(VFXKind.LandingRing, Feet + Vector3.up * 0.03f, Mathf.Lerp(0.8f, 1.4f, k)); }
            if (impact > 11f) CameraShake.Add(0.12f * k);
            AudioManager.Play(Sfx.Land, transform.position, Mathf.Lerp(0.5f, 1f, k));
        }

        float _lastStepTime; int _stepCount;

        void Update()
        {
            if (_motor != null)
            {
                bool fast = _motor.IsSprinting && _motor.PlanarVelocity.magnitude > _motor.Config.walkSpeed * 1.15f;
                _sprintFx = Mathf.MoveTowards(_sprintFx, fast ? 1f : 0f, Time.deltaTime * 4f);
                if (_trailL != null) _trailL.emitting = _sprintFx > 0.5f;
                if (_trailR != null) _trailR.emitting = _sprintFx > 0.5f;
                // kick-off: a whoosh, a dust burst and a ring the moment Pip breaks into a sprint
                if (fast && !_wasSprinting && _motor.IsGrounded)
                {
                    AudioManager.Play(Sfx.Whoosh, transform.position, 0.5f);
                    VFX.Play(VFXKind.DustPuff, transform.position + Vector3.up * 0.1f - transform.forward * 0.4f, 1.1f);
                    VFX.Play(VFXKind.LandingRing, transform.position + Vector3.up * 0.05f - transform.forward * 0.3f, 0.7f, VFX.Cream);
                }
                _wasSprinting = fast;
            }
            if (_anim == null || _motor == null || !_motor.IsGrounded) { if (_anim != null) _lastStepPhase = _anim.Phase; return; }
            // a footstep every half gait cycle (each foot), only while actually moving
            float half = Mathf.PI;
            if (Mathf.Floor(_anim.Phase / half) != Mathf.Floor(_lastStepPhase / half) && _motor.PlanarVelocity.magnitude > 0.8f && Time.time - _lastStepTime > 0.17f)
            {
                // soft pats, never a drum roll: ≥0.17 s apart, quiet, every other step skipped while wheel-running
                _lastStepTime = Time.time; _stepCount++;
                if (!_motor.IsSprinting || _stepCount % 2 == 0)
                    AudioManager.Play(Sfx.Footstep, transform.position, _motor.IsSprinting ? 0.32f : 0.24f);
                // sprint: a little dust kick on every step (wheel-run reads funnier with puffs)
                var fx = FXLibrary.Instance;
                if (_motor.IsSprinting && fx != null) WorldFX.Spawn(fx.dust, transform.position + Vector3.up * 0.12f - transform.forward * 0.35f, 0.42f, 0.28f, null, 0f);
                if (_anim.IsSkidding) VFX.Play(VFXKind.DustPuff, transform.position + Vector3.up * 0.1f + transform.forward * 0.3f, 0.8f);
                if (_anim.IsSkidding && fx != null) WorldFX.Spawn(fx.dust, transform.position + Vector3.up * 0.1f + transform.forward * 0.3f, 0.6f, 0.3f, null, 0f);
            }
            _lastStepPhase = _anim.Phase;
        }
    }
}
