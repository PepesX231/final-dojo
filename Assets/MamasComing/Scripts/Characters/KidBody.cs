using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Whole-body layer on the kid's FallPivot (runs after every limb animator):
    ///  - a per-character walk style (Banana = goofy noodle wobble, Orange = proud waddle, ...);
    ///  - knock-downs: the kid curls back into a plain fruit (limbs pulled in), rocks on the floor,
    ///    then the limbs pop back out and it hops up;
    ///  - every fruit is normalised to the same height so the cast reads as one family.
    /// Works for local and remote avatars: it only reads PlayerController.KnockedDown and how fast the kid moves.
    [DefaultExecutionOrder(650)]
    public class KidBody : MonoBehaviour
    {
        public const float TargetHeight = 1.15f;
        public enum Style { Apple, Banana, Orange, Watermelon, WaxApple }

        [System.Serializable]
        public struct Gait
        {
            public float stride;     // metres per full step cycle (two steps)
            public float bounce;     // vertical hop per step (m)
            public float roll;       // side-to-side lean (deg)
            public float pitch;      // forward/back rock per step (deg)
            public float twist;      // hip yaw swing (deg)
            public float lean;       // constant lean while moving (deg, + = forward)
            public float limp;       // 0..1 uneven rhythm (one step longer than the other)
            public float wobble;     // high-frequency noodle jiggle (deg)
        }

        public PlayerController player;
        public Style style;
        public Gait gait;

        const float PopTime = 0.22f, HopTime = 0.28f;   // PopTime + HopTime == PlayerController.RecoverTime

        struct Limb { public Transform t; public Vector3 srcPos, srcScale, wrotePos, wroteScale; public bool wrote; public Renderer[] renderers; public bool[] visible; }
        readonly List<Limb> limbs = new List<Limb>();
        Transform pivot, chr;
        float height = 1f, sizeK = 1f;
        float phase, moveW, curl, tip, recoverT = -1f, downT, rollDir = 1f;
        bool wasDown;
        Vector3 lastPos;
        Vector3 coreLocal = new Vector3(0, .6f, 0);
        float rollRadius = .3f;
        Quaternion tumble = Quaternion.identity, recoverRotation = Quaternion.identity;
        public float RolledDegrees { get; private set; }

        public bool Recovering => recoverT >= 0f;
        public float LimbsOut { get; private set; } = 1f;

        public static Gait GaitFor(Style s)
        {
            switch (s)
            {
                // goofy, gangly noodle walk: big sway, hip swing, uneven rhythm and a constant jiggle
                case Style.Banana:     return new Gait { stride = 1.5f, bounce = 0.025f, roll = 6f, pitch = 3f, twist = 5f, lean = -2f, limp = 0.12f, wobble = 0f };
                // proud waddle, chest out
                case Style.Orange:     return new Gait { stride = 1.4f, bounce = 0.022f, roll = 4f, pitch = 2f, twist = 3f, lean = -2f, limp = 0f, wobble = 0f };
                // heavy stomp, leaning into each step
                case Style.Watermelon: return new Gait { stride = 1.6f, bounce = 0.03f, roll = 3f, pitch = 2.5f, twist = 3f, lean = 3f, limp = .05f, wobble = 0f };
                // dainty quick prance
                case Style.WaxApple:   return new Gait { stride = 1.3f, bounce = 0.025f, roll = 3f, pitch = 2f, twist = 4f, lean = 2f, limp = 0f, wobble = 0f };
                // apple: bouncy skip
                default:               return new Gait { stride = 0.85f, bounce = 0.045f, roll = 4f,  pitch = 3f, twist = 3f,  lean = 3f,  limp = 0f,    wobble = 0f };
            }
        }

        void Awake()
        {
            if (!player) player = GetComponent<PlayerController>();
            Rebind();
            lastPos = transform.position;
        }

        /// Re-scan the character model (call after the CHR_ model under FallPivot/Visual changes).
        public void Rebind()
        {
            RestoreLimbs();
            limbs.Clear();
            pivot = player ? player.fallPivot : null;
            if (!pivot) return;
            chr = null;
            foreach (var t in pivot.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("CHR_") && t.gameObject.activeSelf) { chr = t; break; }
            style = StyleOf(chr ? chr.name : "");
            gait = GaitFor(style);
            if (chr) CollectLimbs(chr);

            // same height for every fruit, measured in the rest pose with the pivot upright
            pivot.localRotation = Quaternion.identity; pivot.localPosition = Vector3.zero; pivot.localScale = Vector3.one;
            float h = MeasureHeight();
            height = h > 0.2f ? h : TargetHeight;
            sizeK = Mathf.Clamp(TargetHeight / height, 0.6f, 1.6f);
            pivot.localScale = Vector3.one * sizeK;
            // Rotate around the fruit's centre rather than its feet. Limbs are
            // excluded where possible so a long glove does not become its radius.
            Bounds bounds = new Bounds(transform.position + Vector3.up * .6f, Vector3.one * .5f);
            bool haveBounds = false;
            if (chr) foreach (var renderer in chr.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || !renderer.enabled) continue;
                bool limb = false;
                for (var p = renderer.transform; p && p != chr; p = p.parent) if (IsLimb(p.name)) { limb = true; break; }
                if (limb) continue;
                if (!haveBounds) { bounds = renderer.bounds; haveBounds = true; } else bounds.Encapsulate(renderer.bounds);
            }
            coreLocal = pivot.InverseTransformPoint(bounds.center);
            rollRadius = Mathf.Clamp(Mathf.Min(bounds.extents.x, bounds.extents.z), .2f, .5f);
            tumble = Quaternion.identity; RolledDegrees = 0f;
        }

        public static Style StyleOf(string chrName)
        {
            if (chrName.Contains("Banana")) return Style.Banana;
            if (chrName.Contains("Orange")) return Style.Orange;
            if (chrName.Contains("Watermelon")) return Style.Watermelon;
            if (chrName.Contains("WaxApple")) return Style.WaxApple;
            return Style.Apple;
        }

        float MeasureHeight()
        {
            if (!chr) return 0f;
            bool any = false; float hi = 0f;
            foreach (var r in chr.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || !r.enabled) continue;
                float top = r.bounds.max.y;
                hi = any ? Mathf.Max(hi, top) : top; any = true;
            }
            return any ? hi - transform.position.y : 0f;
        }

        static bool IsLimb(string n)
        {
            if (n.StartsWith("Arm") || n.StartsWith("Leg") || n.StartsWith("Foot") || n.StartsWith("Hand")) return true;
            return n.Contains("duck foot") || n.Contains("swinging glove");
        }

        void CollectLimbs(Transform root)
        {
            foreach (Transform c in root)
            {
                if (IsLimb(c.name))
                {
                    var renderers = c.GetComponentsInChildren<Renderer>(true); var visible = new bool[renderers.Length];
                    for (int i = 0; i < visible.Length; i++) visible[i] = renderers[i].enabled;
                    limbs.Add(new Limb { t = c, renderers = renderers, visible = visible }); continue;
                }
                CollectLimbs(c);
            }
        }

        void OnDisable()
        {
            RestoreLimbs();
            if (pivot) { pivot.localRotation = Quaternion.identity; pivot.localPosition = Vector3.zero; }
        }

        void RestoreLimbs()
        {
            for (int i = 0; i < limbs.Count; i++)
            {
                var l = limbs[i]; if (!l.t || !l.wrote) continue;
                for (int r = 0; r < l.renderers.Length; r++) if (l.renderers[r]) l.renderers[r].enabled = l.visible[r];
                if (l.t.localPosition == l.wrotePos) l.t.localPosition = l.srcPos;
                if (l.t.localScale == l.wroteScale) l.t.localScale = l.srcScale;
                l.wrote = false; limbs[i] = l;
            }
        }

        // ------------------------------------------------------------------ per frame
        void LateUpdate()
        {
            if (!player || !pivot) return;
            float dt = Time.deltaTime; if (dt <= 0f) return;

            bool down = player.KnockedDown;
            if (down && !wasDown) { downT = 0f; recoverT = -1f; tumble = Quaternion.identity; RolledDegrees = 0f; rollDir = 1f; }
            if (!down && wasDown) recoverT = 0f;
            wasDown = down;

            // ---- curl / pop timeline
            float limbK;          // 0 = limbs in (plain fruit), 1 = normal, >1 overshoot
            float hop = 0f, squash = 0f;
            if (down)
            {
                downT += dt;
                curl = Mathf.MoveTowards(curl, 1f, dt / 0.14f);
                tip = Mathf.MoveTowards(tip, 1f, dt / 0.18f);
                limbK = 1f - curl;
                squash = downT < 0.14f ? Mathf.Sin(downT / 0.14f * Mathf.PI) * 0.2f : 0f;   // splat as it lands
            }
            else if (recoverT >= 0f)
            {
                recoverT += dt;
                float u = recoverT;
                curl = 0f;
                if (u < PopTime)
                {
                    // limbs spring out with an elastic overshoot while the fruit is still lying there
                    float p = u / PopTime;
                    float spring = 1f + Mathf.Sin(p * Mathf.PI * 1.5f) * 0.35f * (1f - p * 0.5f);
                    limbK = Mathf.Lerp(0f, spring, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 1.6f)));
                    squash = -Mathf.Sin(p * Mathf.PI) * 0.08f;
                }
                else if (u < PopTime + HopTime)
                {
                    // spring up: tip upright mid-air with a stretch, land with a squash
                    float p = (u - PopTime) / HopTime;
                    limbK = 1f + Mathf.Sin(p * Mathf.PI) * 0.08f;
                    hop = Mathf.Sin(p * Mathf.PI) * 0.42f;
                    tip = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p * 1.6f));
                    squash = p < 0.75f ? -0.12f * Mathf.Sin(p / 0.75f * Mathf.PI) : 0.16f * Mathf.Sin((p - 0.75f) / 0.25f * Mathf.PI);
                }
                else { recoverT = -1f; limbK = 1f; tip = 0f; }
            }
            else
            {
                curl = 0f; tip = Mathf.MoveTowards(tip, 0f, dt * 4f);
                limbK = 1f;
            }
            LimbsOut = limbK;

            // ---- walk style (only while upright)
            Vector3 p0 = transform.position; Vector3 dv = p0 - lastPos; dv.y = 0f; lastPos = p0;
            if (down && dv.sqrMagnitude > .000001f && dv.magnitude < 2f)
            {
                float angle = dv.magnitude / rollRadius * Mathf.Rad2Deg;
                Vector3 axis = transform.InverseTransformDirection(Vector3.Cross(Vector3.up, dv.normalized));
                tumble = Quaternion.AngleAxis(angle, axis) * tumble; RolledDegrees += angle;
            }
            float speed = Mathf.Min(dv.magnitude / dt, 12f);
            bool grounded = !player.isLocal || !player.Motor || player.Motor.IsGrounded;
            float want = speed > 0.25f && grounded && !down && recoverT < 0f ? Mathf.Clamp01(speed / 3f) : 0f;
            moveW = Mathf.MoveTowards(moveW, want, dt * 5f);
            phase += speed * dt / Mathf.Max(0.2f, gait.stride) * Mathf.PI * 2f;
            if (phase > 1000f) phase -= Mathf.PI * 300f;

            Vector3 gaitPos = Vector3.zero, gaitEuler = Vector3.zero;
            if (moveW > 0.001f)
            {
                float ph = phase + gait.limp * Mathf.Sin(phase) * 0.9f;   // uneven rhythm
                float step = Mathf.Abs(Mathf.Sin(ph));                      // 0..1, twice per cycle
                float side = Mathf.Sin(ph);
                float w = moveW;
                gaitPos.y = step * gait.bounce * w;
                gaitPos.x = side * gait.roll * 0.0025f * w;
                gaitEuler.x = (gait.lean + Mathf.Cos(ph * 2f) * gait.pitch) * w;
                gaitEuler.y = Mathf.Cos(ph) * gait.twist * w;
                gaitEuler.z = -side * gait.roll * w;
                if (gait.wobble > 0f)
                {
                    float t = Time.time;
                    gaitEuler.z += (Mathf.Sin(t * 17f) + Mathf.Sin(t * 23.3f) * 0.6f) * gait.wobble * w;
                    gaitEuler.x += Mathf.Sin(t * 13.1f) * gait.wobble * 0.6f * w;
                }
            }

            // ---- knocked-down fruit: lies on its back, rocks side to side and settles
            float rock = 0f;
            if (tip > 0.001f)
            {
                float t = down ? downT : 1.5f;
                rock = (Mathf.Sin(t * 7f) * Mathf.Exp(-t * 1.6f) * 25f + Mathf.Sin(t * 2.2f) * 8f) * rollDir;
                if (recoverT >= 0f && recoverT < PopTime) rock += Mathf.Sin(recoverT * 60f) * 6f;   // shiver before springing up
            }
            float tipEase = tip * tip * (3f - 2f * tip);
            Quaternion rot = Quaternion.Euler(gaitEuler);
            if (down) { rot = Quaternion.Slerp(Quaternion.identity, tumble * Quaternion.Euler(-82f, 0f, rock), tipEase); recoverRotation = rot; }
            else if (recoverT >= 0f) rot = Quaternion.Slerp(Quaternion.identity, recoverRotation, tipEase);
            pivot.localRotation = rot;
            Vector3 centre = coreLocal * sizeK;
            Vector3 turnOffset = tipEase > 0f ? centre - rot * centre + Vector3.up * (rollRadius + .04f - centre.y) * tipEase : Vector3.zero;
            pivot.localPosition = gaitPos + Vector3.up * hop + turnOffset;
            float sy = 1f - squash, sxz = 1f + squash * 0.6f;
            pivot.localScale = new Vector3(sxz, sy, sxz) * sizeK;

            ApplyLimbs(limbK);
        }

        void ApplyLimbs(float k)
        {
            bool rest = Mathf.Abs(k - 1f) < 0.0005f;
            Vector3 core = chr ? chr.position + chr.up * height * sizeK * 0.45f : transform.position;
            for (int i = 0; i < limbs.Count; i++)
            {
                var l = limbs[i]; if (!l.t) continue;
                for (int r = 0; r < l.renderers.Length; r++) if (l.renderers[r]) l.renderers[r].enabled = l.visible[r] && k > .06f;
                // this frame's animated pose is the source, unless nobody touched what we wrote last frame
                Vector3 cp = l.t.localPosition, cs = l.t.localScale;
                if (!l.wrote || cp != l.wrotePos) l.srcPos = cp;
                if (!l.wrote || cs != l.wroteScale) l.srcScale = cs;
                if (rest)
                {
                    if (l.wrote) { l.t.localPosition = l.srcPos; l.t.localScale = l.srcScale; l.wrote = false; }
                    limbs[i] = l; continue;
                }
                float inK = Mathf.Clamp01(1f - k);   // how far pulled into the body
                Vector3 corePos = l.t.parent ? l.t.parent.InverseTransformPoint(core) : core;
                l.wrotePos = Vector3.LerpUnclamped(l.srcPos, corePos, inK * 0.85f);
                l.wroteScale = l.srcScale * Mathf.Max(0.001f, k);
                l.t.localPosition = l.wrotePos; l.t.localScale = l.wroteScale;
                l.wrote = true; limbs[i] = l;
            }
        }
    }
}
