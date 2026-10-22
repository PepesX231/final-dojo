using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>
    /// M18 FISTS (user: "drop the sword, use fists"). Runs after the procedural animator and turns Pip's arms into
    /// punches: per move it picks which fist(s) strike, the punch direction and an extension curve
    /// (wind-up → snap → hold → recover). The elbow straightens on the snap, the mitt balloons on impact (cartoon),
    /// the other fist keeps a guard. Idle / roll / hurt hand control back to the animator.
    /// </summary>
    [DefaultExecutionOrder(80)]
    public class FistRig : MonoBehaviour
    {
        PlayerCombat _c; Transform _armL, _armR, _loL, _loR; Vector3 _handL, _handR, _loScaleL, _loScaleR, _upScaleL, _upScaleR, _axL, _axR;
        float _wL, _wR, _eL, _eR, _emberT;
        TrailRenderer _fireL, _fireR;
        public Vector3 HandR => _loR != null ? _loR.TransformPoint(_handR) : transform.position + transform.right * 0.5f + Vector3.up * 0.6f;
        public Vector3 HandL => _loL != null ? _loL.TransformPoint(_handL) : transform.position - transform.right * 0.5f + Vector3.up * 0.6f;
        public float ExtendR => _eR * _wR;
        public Transform MittR => _loR != null ? _loR : _armR;
        public Transform MittL => _loL != null ? _loL : _armL;
        public float ExtendL => _eL * _wL;

        void Start()
        {
            _c = GetComponent<PlayerCombat>();
            var vis = transform.Find("Visual"); if (vis == null) return;
            _armL = Find(vis, "Arm_L"); _armR = Find(vis, "Arm_R"); _loL = Find(vis, "Arm_L_Lower"); _loR = Find(vis, "Arm_R_Lower");
            _handL = Hand(_loL != null ? _loL : _armL, _loL != null); _handR = Hand(_loR != null ? _loR : _armR, _loR != null);
            if (_loL != null) _loScaleL = _loL.localScale; if (_loR != null) _loScaleR = _loR.localScale;
            if (_armL != null) { _upScaleL = _armL.localScale; _axL = Axis(_armL); }
            if (_armR != null) { _upScaleR = _armR.localScale; _axR = Axis(_armR); }
            _fireR = Fire("FistFire_R"); _fireL = Fire("FistFire_L");
        }

        /// <summary>M20 comet punch (user ref): a fat flame trail streams behind the mitt while it snaps out.</summary>
        TrailRenderer Fire(string n)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.32f; tr.minVertexDistance = 0.02f; tr.emitting = false; tr.numCapVertices = 4;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 0.9f), new Keyframe(0.35f, 0.6f), new Keyframe(1, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 1f, 0.85f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0.25f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.65f), new GradientColorKey(new Color(0.9f, 0.2f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.4f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.sharedMaterial = FlyingFist.FlameMat();
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tr;
        }

        void Flame(TrailRenderer tr, Vector3 at, bool on)
        {
            if (tr == null) return;
            tr.transform.position = at;
            if (on && !tr.emitting) tr.Clear();
            tr.emitting = on;
        }

        static Vector3 Hand(Transform part, bool lower)
        {
            if (part == null) return Vector3.zero;
            var mf = part.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.center * (lower ? 1.45f : 1.6f) : new Vector3(0.05f, -0.33f, 0f);
        }

        /// <summary>The upper arm's long axis in its local space (from the mesh centre), as |x|,|y|,|z| weights.</summary>
        static Vector3 Axis(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            Vector3 c = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.center : Vector3.down;
            if (c.sqrMagnitude < 1e-6f) c = Vector3.down;
            c = c.normalized; return new Vector3(Mathf.Abs(c.x), Mathf.Abs(c.y), Mathf.Abs(c.z));
        }

        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r != null) return r; } return null; }

        /// <summary>wind-up (pull back) → snap out (overshoot) → hold → ease back. k in 0..1 of the move's active window.</summary>
        static float Punch(float k, float wind = 0.3f)
        {
            if (k < wind) return -0.35f * Mathf.Sin(k / wind * Mathf.PI * 0.5f);
            float s = Mathf.Clamp01((k - wind) / 0.25f);
            float snap = 1f + 0.12f * Mathf.Sin(s * Mathf.PI);                    // a hair of overshoot
            return Mathf.Lerp(-0.35f, 1f, 1f - (1f - s) * (1f - s) * (1f - s)) * snap;
        }

        void LateUpdate()
        {
            if (_c == null || _armR == null) return;
            float t = _c.ActTime; var st = _c.State;
            Vector3 f = transform.forward, up = Vector3.up, rt = transform.right;
            Vector3 guard = (f * 0.8f + up * 0.55f).normalized;
            Vector3 dR = guard, dL = guard; float wR = 0f, wL = 0f, eR = 0f, eL = 0f;
            const float LW = 0.1f, LA = 0.1f, LR = 0.24f;
            switch (st)
            {
                case PlayerCombat.Act.Attack:
                {
                    float k = Mathf.Clamp01(t / (LW + LA + LR * 0.5f));
                    float p = Punch(k);
                    if (_c.ComboStep == 0) { dR = Vector3.Slerp(-f * 0.4f + up * 0.2f, f + up * 0.05f, Mathf.Clamp01(p)).normalized; eR = Mathf.Clamp01(p); wR = 1f; dL = guard; eL = 0.15f; wL = 0.8f; }            // right jab
                    else if (_c.ComboStep == 1) { dL = Vector3.Slerp(-f * 0.4f + up * 0.2f, f + up * 0.05f, Mathf.Clamp01(p)).normalized; eL = Mathf.Clamp01(p); wL = 1f; dR = guard; eR = 0.15f; wR = 0.8f; }  // left cross
                    else { dR = Vector3.Slerp((f * 0.4f - up + rt * 0.3f).normalized, (f * 0.9f + up).normalized, Mathf.Clamp01(p)); eR = Mathf.Clamp01(p); wR = 1f; dL = guard; eL = 0.2f; wL = 0.8f; } // uppercut
                    break;
                }
                case PlayerCombat.Act.Charge: dR = (up + rt * 0.25f).normalized; dL = (up - rt * 0.25f).normalized; eR = eL = 0.85f + Mathf.Sin(t * 30f) * 0.05f; wR = wL = 1f; break;   // both fists high, trembling
                case PlayerCombat.Act.Heavy:
                {
                    float k = Mathf.Clamp01(t / 0.45f);
                    Vector3 slam = (f * 0.7f - up).normalized;
                    dR = Vector3.Slerp((up + rt * 0.2f).normalized, slam, k * k); dL = Vector3.Slerp((up - rt * 0.2f).normalized, slam, k * k); eR = eL = 1f; wR = wL = 1f; break;   // double-fist hammer
                }
                case PlayerCombat.Act.AirSpin: dR = (rt + f * 0.2f).normalized; dL = (-rt + f * 0.2f).normalized; eR = eL = 1f; wR = wL = 1f; break;   // spinning top, fists out
                case PlayerCombat.Act.Rising: { float p = Punch(Mathf.Clamp01(t / 0.25f), 0.15f); dR = Vector3.Slerp(f, up, Mathf.Clamp01(p)); eR = Mathf.Clamp01(p); wR = 1f; dL = (-up + f * 0.3f).normalized; eL = 0.6f; wL = 0.9f; break; }
                case PlayerCombat.Act.Thrust: dR = (f + up * 0.05f).normalized; eR = 1f; wR = 1f; dL = (-f - up * 0.2f).normalized; eL = 0.8f; wL = 0.9f; break;    // rocket punch
                case PlayerCombat.Act.Burst:
                {
                    if (t < 0.12f) { float j = Mathf.Repeat(t * 30f, 1f); dR = f; dL = f; eR = j < 0.5f ? 1f : 0.2f; eL = j < 0.5f ? 0.2f : 1f; }      // barrage
                    else { dR = (f + rt * 0.15f).normalized; dL = (f - rt * 0.15f).normalized; eR = eL = 1f; }                                         // double palm blast
                    wR = wL = 1f; break;
                }
                default: break;   // idle / roll / hurt: the animator owns the arms
            }
            float dt = Time.deltaTime;
            bool striking = st != PlayerCombat.Act.Idle && st != PlayerCombat.Act.Roll && st != PlayerCombat.Act.Hurt;
            _wR = Mathf.MoveTowards(_wR, wR, dt * (striking ? 30f : 5f)); _wL = Mathf.MoveTowards(_wL, wL, dt * (striking ? 30f : 5f));
            _eR = Mathf.Lerp(_eR, eR, 1f - Mathf.Exp(-40f * dt)); _eL = Mathf.Lerp(_eL, eL, 1f - Mathf.Exp(-40f * dt));
            Apply(_armR, _loR, _handR, _loScaleR, _upScaleR, _axR, dR, _wR, _eR);
            Apply(_armL, _loL, _handL, _loScaleL, _upScaleL, _axL, dL, _wL, _eL);
            bool fR = striking && _eR * _wR > 0.55f, fL = striking && _eL * _wL > 0.55f;
            Flame(_fireR, HandR, fR); Flame(_fireL, HandL, fL);
            _emberT -= dt;
            if ((fR || fL) && _emberT <= 0f) { _emberT = 0.06f; Presentation.VFX.Play(Presentation.VFXKind.Embers, fR ? HandR : HandL, 0.35f); }
        }

        /// <summary>Rubber-hose reach: on the snap the upper arm stretches along its length (cartoon punch).</summary>
        public float stretch = 0.75f;

        void Apply(Transform arm, Transform lo, Vector3 hand, Vector3 loScale, Vector3 upScale, Vector3 axis, Vector3 dir, float w, float ext)
        {
            if (arm == null || w < 0.001f)
            {
                if (lo != null && loScale != Vector3.zero) lo.localScale = loScale;
                if (arm != null && upScale != Vector3.zero) arm.localScale = upScale;
                return;
            }
            float st = stretch * Mathf.Clamp01(ext - 0.35f) / 0.65f * w;     // only the real snap stretches, guard poses stay short
            if (upScale != Vector3.zero) arm.localScale = upScale + Vector3.Scale(upScale, axis) * st;
            if (lo != null) lo.position = arm.position + (lo.position - arm.position) * (1f + st);
            Vector3 shoulder = arm.position;
            var handPart = lo != null ? lo : arm;
            if (lo != null)
            {
                // straighten the elbow on the snap
                Vector3 elbow = lo.position, h = lo.TransformPoint(hand);
                Vector3 upDir = (elbow - shoulder).normalized, loDir = (h - elbow).normalized;
                if (upDir.sqrMagnitude > 0.5f && loDir.sqrMagnitude > 0.5f)
                    lo.rotation = Quaternion.FromToRotation(loDir, Vector3.Slerp(loDir, upDir, Mathf.Clamp01(ext) * w)) * lo.rotation;
                lo.localScale = loScale * (1f + 0.35f * Mathf.Clamp01(ext) * w);   // the mitt balloons on impact
            }
            Vector3 cur = (handPart.TransformPoint(hand) - shoulder).normalized;
            if (cur.sqrMagnitude < 0.5f) return;
            Vector3 want = Vector3.Slerp(cur, dir.normalized, w);
            Quaternion r = Quaternion.FromToRotation(cur, want);
            arm.rotation = r * arm.rotation;
            if (lo != null) { lo.position = shoulder + r * (lo.position - shoulder); lo.rotation = r * lo.rotation; }
        }
    }
}
