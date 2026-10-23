using DreamRush.Player;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Procedural action layer on top of the apple-kid's AppleProceduralAnimator (runs after it).
    /// Bends/twists/crouches the body and drives both arms for every action: picking things up,
    /// carrying, dragging, mopping, cranking valves, digging in drains, pushing windows, spraying...
    /// Also moves the hold anchor to the hands so carried items really sit in the kid's grip.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class KidActionAnimator : MonoBehaviour
    {
        public enum Clip
        {
            None, PickLow, PickHigh, PlaceDown, Throw, Twist, TwistLow, PushUp, Crank, Dig, PokeUp,
            Slam, Wring, Pour, ReachIn, Pull, Press, Scoop
        }

        public PlayerController player;
        public Transform chr;
        [Tooltip("Rotation pivot for leaning (local to the character root of the rig).")]
        public Vector3 pivot = new Vector3(0f, 0.12f, 0f);
        public float blend = 14f;
        [Tooltip("Scales how far in front of the body carried CarryGrip items sit (anchor depth, shoulder roll). 1 = apple kid.")]
        public float carryDepth = 1f;

        Transform body, leaf, armL, armR, armLLo, armRLo;
        Vector3 bodyRP, leafRP, armLRP, armRRP, armLLoRP, armRLoRP;
        Quaternion bodyRR, leafRR, armLRR, armRRR, armLLoRR, armRLoRR;
        // Skinned fruit rigs (Banana_Rigged.fbx): limbs live under a bone frame whose axes differ from
        // the character's, and Hand_L/Hand_R mark the wrists. The apple rig has neither, so it keeps
        // identity conversions and the original 0.16 forearm.
        Transform frame, handL, handR;
        Quaternion basis = Quaternion.identity;
        Vector3 pivotRig, handLRP, handRRP;
        float forearmL = .16f, forearmR = .16f;

        struct Pose
        {
            public float bp, by, br, crouch, fwd;   // body pitch / yaw / roll (deg), crouch & forward shift (m)
            public Vector3 l, r;                     // arm: pitch, yaw, open (deg)
            public float le, re;                     // elbow bend
            public float armW;                       // arm override weight
            public float handFollow;                 // move the hold anchor to the hands
            public Vector3 holdLocal;                // fallback anchor (root-local)

            public static Pose Lerp(Pose a, Pose b, float t)
            {
                return new Pose
                {
                    bp = Mathf.Lerp(a.bp, b.bp, t), by = Mathf.Lerp(a.by, b.by, t), br = Mathf.Lerp(a.br, b.br, t),
                    crouch = Mathf.Lerp(a.crouch, b.crouch, t), fwd = Mathf.Lerp(a.fwd, b.fwd, t),
                    l = Vector3.Lerp(a.l, b.l, t), r = Vector3.Lerp(a.r, b.r, t),
                    le = Mathf.Lerp(a.le, b.le, t), re = Mathf.Lerp(a.re, b.re, t),
                    armW = Mathf.Lerp(a.armW, b.armW, t), handFollow = Mathf.Lerp(a.handFollow, b.handFollow, t),
                    holdLocal = Vector3.Lerp(a.holdLocal, b.holdLocal, t)
                };
            }
        }

        Pose cur;
        Clip oneShot = Clip.None; float oneT, oneDur;
        Clip holdClip = Clip.None; int holdFrame = -10; float holdT;
        Vector3 defaultHold;
        public Vector3 LeftGripWorld { get; private set; }
        public Vector3 RightGripWorld { get; private set; }

        public bool Busy => oneShot != Clip.None && oneT < oneDur * 0.6f;
        public Clip Current => oneShot != Clip.None ? oneShot : holdClip;

        void Awake()
        {
            if (!player) player = GetComponent<PlayerController>();
            if (!chr) { foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "CHR_AppleChild") chr = t; }
            body = Find("Body"); leaf = Find("Leaf"); armL = Find("Arm_L"); armR = Find("Arm_R"); armLLo = Find("Arm_L_Lower"); armRLo = Find("Arm_R_Lower");
            Rest(body, out bodyRP, out bodyRR); Rest(leaf, out leafRP, out leafRR);
            Rest(armL, out armLRP, out armLRR); Rest(armR, out armRRP, out armRRR);
            Rest(armLLo, out armLLoRP, out armLLoRR); Rest(armRLo, out armRLoRP, out armRLoRR);
            frame = body ? body.parent : chr; pivotRig = pivot;
            if (frame && frame != chr)
            {
                basis = Quaternion.Inverse(frame.rotation) * chr.rotation;
                pivotRig = frame.InverseTransformPoint(chr.TransformPoint(pivot));
            }
            handL = Find("Hand_L"); handR = Find("Hand_R");
            if (handL && armLLo) { handLRP = frame.InverseTransformPoint(handL.position); forearmL = Vector3.Distance(handLRP, armLLoRP); }
            if (handR && armRLo) { handRRP = frame.InverseTransformPoint(handR.position); forearmR = Vector3.Distance(handRRP, armRLoRP); }
            if (player && player.holdPoint) defaultHold = player.holdPoint.localPosition;
        }

        Transform Find(string n) { foreach (var t in chr.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
        Quaternion ToRig(Quaternion q) => basis * q * Quaternion.Inverse(basis);
        Vector3 ToRig(Vector3 v) => frame == chr ? v : frame.InverseTransformVector(chr.TransformVector(v));
        // Apple limbs rotate about their own axes; bone limbs rotate about the character's axes.
        Quaternion Turn(Quaternion rest, Quaternion e) => frame == chr ? rest * e : ToRig(e) * rest;
        static void Rest(Transform t, out Vector3 p, out Quaternion r) { p = t ? t.localPosition : Vector3.zero; r = t ? t.localRotation : Quaternion.identity; }

        /// <summary>Play a one-shot action (pick up, slam, push...).</summary>
        public void Play(Clip c, float duration = 0.45f)
        {
            oneShot = c; oneT = 0f; oneDur = Mathf.Max(0.1f, duration);
        }

        /// <summary>Call every frame while a hold-interaction is in progress.</summary>
        public void Hold(Clip c)
        {
            if (holdClip != c) holdT = 0f;
            holdClip = c; holdFrame = Time.frameCount;
        }

        // ------------------------------------------------------------------ poses
        static Pose Base() => new Pose { holdLocal = new Vector3(0f, 0.62f, 0.5f) };

        Pose CarryPose(float t)
        {
            var p = Base();
            var h = player ? player.Held : null;
            if (h == null) return p;
            float walk = player.PlanarSpeed;
            float bob = Mathf.Sin(t * 9f) * Mathf.Clamp01(walk / 3f);
            p.armW = 1f; p.handFollow = 1f;
            switch (h.tool)
            {
                case ToolType.Mop:
                case ToolType.Squeegee:
                    if (player.Scrubbing) return h.tool == ToolType.Mop ? MopPose(h as Mop, t) : PushPose(t);
                    // both hands on the stick, leaning it forward
                    p.l = new Vector3(-55f, 0f, -18f); p.r = new Vector3(-75f, 0f, -22f); p.le = 35f; p.re = 25f;
                    p.bp = 6f; p.handFollow = 0f; break;
                case ToolType.Rug:
                    // hauling the rug behind: arms back gripping the edge, leaning into the pull
                    p.l = new Vector3(38f + bob * 6f, 0f, -6f); p.r = new Vector3(38f - bob * 6f, 0f, -6f); p.le = -5f; p.re = -5f;
                    p.bp = 16f + Mathf.Clamp01(walk / 2f) * 8f; p.crouch = 0.05f; p.br = bob * 4f; p.handFollow = 0f; break;
                case ToolType.Chair:
                    // pushing / scooting the chair in front
                    p.l = new Vector3(-55f, 0f, -14f); p.r = new Vector3(-55f, 0f, -14f); p.le = 15f; p.re = 15f;
                    p.bp = 10f + bob * 3f; p.crouch = 0.03f; p.handFollow = 0f; break;
                case ToolType.Bucket:
                    // heavy: arms down in front, leaning back, waddle
                    p.l = new Vector3(-62f, 0f, -24f); p.r = new Vector3(-62f, 0f, -24f); p.le = 25f; p.re = 25f;
                    p.bp = -9f; p.br = bob * 7f; p.crouch = 0.03f; break;
                case ToolType.Extinguisher:
                    if (MamaInput.UseHeld)
                    {
                        float shake = Mathf.Sin(t * 70f) * 2.5f;
                        p.l = new Vector3(-80f + shake, 0f, -25f); p.r = new Vector3(-68f - shake, 0f, -20f); p.le = 20f; p.re = 35f;
                        p.bp = -9f + shake * 0.4f; p.fwd = -0.03f; break;
                    }
                    p.l = new Vector3(-60f, 0f, -22f); p.r = new Vector3(-60f, 0f, -22f); p.le = 40f; p.re = 40f; break;
                case ToolType.Pot:
                    if (player.Scrubbing) return ScrubPose(t);
                    p.l = new Vector3(-75f, 0f, -30f); p.r = new Vector3(-75f, 0f, -30f); p.le = 45f; p.re = 45f; p.bp = -4f; break;
                case ToolType.Towel:
                case ToolType.Lid:
                    if (player.Scrubbing) return ScrubPose(t);
                    // one hand
                    p.armW = 1f; p.l = new Vector3(bob * 18f, 0f, 8f); p.r = new Vector3(-80f, 0f, -8f); p.le = 15f; p.re = 25f;
                    p.handFollow = 0.6f; p.holdLocal = new Vector3(0.22f, 0.6f, 0.45f); break;
                default:
                    p.l = new Vector3(-72f, 0f, -26f); p.r = new Vector3(-72f, 0f, -26f); p.le = 40f; p.re = 40f; p.bp = -3f; break;
            }
            return p;
        }

        Pose MopPose(Mop m, float t)
        {
            float ph = m ? m.Phase : t * 15f;
            float s = Mathf.Sin(ph), c = Mathf.Cos(ph * 2f);
            var p = Base();
            p.armW = 1f; p.handFollow = 0f;
            p.bp = 20f + c * 4f;           // hunched over the mop, bobbing with each stroke
            p.by = s * 30f;                // whole body twists into the stroke
            p.br = -s * 7f;
            p.crouch = 0.07f + Mathf.Abs(c) * 0.03f;
            p.l = new Vector3(-62f - c * 10f, 0f, -14f + s * 22f);
            p.r = new Vector3(-80f + c * 10f, 0f, -16f - s * 22f);
            p.le = 35f + s * 15f; p.re = 25f - s * 15f;
            return p;
        }

        Pose PushPose(float t)
        {
            float step = Mathf.Sin(t * 10f);
            var p = Base();
            p.armW = 1f; p.handFollow = 0f;
            p.bp = 22f + step * 3f; p.fwd = 0.05f; p.crouch = 0.05f; p.br = step * 4f;
            p.l = new Vector3(-70f, 0f, -18f); p.r = new Vector3(-70f, 0f, -18f); p.le = 5f; p.re = 5f;
            return p;
        }

        Pose ScrubPose(float t)
        {
            var p = Base();
            float a = t * 22f;
            p.armW = 1f; p.handFollow = 1f;
            p.bp = 12f + Mathf.Sin(a) * 2f; p.by = Mathf.Sin(a * 0.5f) * 6f;
            p.l = new Vector3(-70f + Mathf.Sin(a) * 14f, 0f, -26f + Mathf.Cos(a) * 12f);
            p.r = new Vector3(-70f - Mathf.Sin(a) * 14f, 0f, -26f - Mathf.Cos(a) * 12f);
            p.le = 50f + Mathf.Cos(a) * 15f; p.re = 50f - Mathf.Cos(a) * 15f;
            return p;
        }

        /// <summary>Clip pose at normalised time u (0..1). Hold clips loop on their own clock.</summary>
        Pose ClipPose(Clip c, float u, float t)
        {
            var p = Base(); p.armW = 1f;
            float up = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);           // 0 -> 1 -> 0
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 2f));  // 0 -> 1 (first half)
            switch (c)
            {
                case Clip.PickLow:
                    p.bp = 45f * up; p.crouch = 0.2f * up; p.fwd = 0.06f * up;
                    p.l = new Vector3(-55f * up - 10f, 0f, -10f); p.r = new Vector3(-55f * up - 10f, 0f, -10f); p.le = 10f; p.re = 10f;
                    p.handFollow = 1f; break;
                case Clip.PickHigh:
                    p.bp = 8f * up; p.l = new Vector3(-95f * up, 0f, -14f); p.r = new Vector3(-95f * up, 0f, -14f); p.le = 15f; p.re = 15f; p.handFollow = 1f; break;
                case Clip.PlaceDown:
                    p.bp = 38f * up; p.crouch = 0.16f * up; p.fwd = 0.05f * up;
                    p.l = new Vector3(-50f - 20f * up, 0f, -20f); p.r = new Vector3(-50f - 20f * up, 0f, -20f); p.le = 20f; p.re = 20f; p.handFollow = 1f; break;
                case Clip.Throw:
                    {
                        float wind = u < 0.4f ? u / 0.4f : 1f - (u - 0.4f) / 0.6f;
                        float fling = u < 0.4f ? 0f : Mathf.Sin((u - 0.4f) / 0.6f * Mathf.PI);
                        p.bp = -15f * wind + 25f * fling; p.by = -20f * wind + 15f * fling;
                        p.l = new Vector3(-150f * wind - 100f * fling, 0f, -10f); p.r = new Vector3(-160f * wind - 95f * fling, 0f, -10f);
                        p.le = 60f * wind; p.re = 60f * wind; p.handFollow = 1f; break;
                    }
                case Clip.Twist: // turn a knob / tap: one hand forward, wrist twisting
                    p.bp = 12f * rise; p.r = new Vector3(-85f, Mathf.Sin(t * 18f) * 25f * rise, -10f + Mathf.Sin(t * 18f) * 20f); p.re = 15f;
                    p.l = new Vector3(-10f, 0f, 12f); p.le = 20f; break;
                case Clip.TwistLow:
                    p.bp = 30f * rise; p.crouch = 0.12f * rise;
                    p.r = new Vector3(-70f, 0f, -10f + Mathf.Sin(t * 16f) * 25f); p.re = 10f;
                    p.l = new Vector3(-20f, 0f, 20f); p.le = 30f; break;
                case Clip.PushUp: // window sash: reach up and heave
                    {
                        float heave = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI * 1.5f);
                        p.bp = -8f * heave; p.crouch = -0.04f * heave;
                        p.l = new Vector3(-150f + 20f * (1f - rise), 0f, -10f); p.r = new Vector3(-150f + 20f * (1f - rise), 0f, -10f); p.le = 25f * (1f - heave); p.re = 25f * (1f - heave);
                        break;
                    }
                case Clip.Crank: // big valve wheel: both arms circling
                    {
                        float a = t * 9f;
                        p.bp = 8f + Mathf.Sin(a) * 4f; p.by = Mathf.Sin(a) * 10f; p.br = Mathf.Cos(a) * 6f;
                        p.l = new Vector3(-90f + Mathf.Sin(a) * 25f, 0f, -12f + Mathf.Cos(a) * 15f);
                        p.r = new Vector3(-90f - Mathf.Sin(a) * 25f, 0f, -12f - Mathf.Cos(a) * 15f);
                        p.le = 30f; p.re = 30f; break;
                    }
                case Clip.Dig: // crouched, pulling hair out of a drain in tugs
                    {
                        float tug = Mathf.Abs(Mathf.Sin(t * 7f));
                        p.bp = 55f - tug * 15f; p.crouch = 0.25f; p.fwd = 0.08f;
                        p.l = new Vector3(-30f - tug * 25f, 0f, -12f); p.r = new Vector3(-35f - tug * 30f, 0f, -12f); p.le = 20f + tug * 40f; p.re = 20f + tug * 45f;
                        break;
                    }
                case Clip.PokeUp: // jab the ceiling alarm with the stick
                    {
                        float jab = Mathf.Abs(Mathf.Sin(t * 8f));
                        p.bp = -14f; p.crouch = -0.05f * jab;
                        p.l = new Vector3(-165f, 0f, -8f); p.r = new Vector3(-150f - jab * 15f, 0f, -8f); p.le = 30f * (1f - jab); p.re = 30f * (1f - jab);
                        break;
                    }
                case Clip.Slam:
                    {
                        float lift = u < 0.45f ? u / 0.45f : 0f;
                        float down = u < 0.45f ? 0f : Mathf.Sin((u - 0.45f) / 0.55f * Mathf.PI);
                        p.bp = -8f * lift + 30f * down; p.crouch = 0.08f * down;
                        p.l = new Vector3(-150f * lift - 75f * down, 0f, -15f); p.r = new Vector3(-150f * lift - 75f * down, 0f, -15f); p.le = 20f; p.re = 20f;
                        p.handFollow = 1f; break;
                    }
                case Clip.Wring:
                    {
                        float tw = Mathf.Sin(t * 14f);
                        p.bp = 18f; p.by = tw * 12f;
                        p.l = new Vector3(-80f, 0f, -30f + tw * 15f); p.r = new Vector3(-80f, 0f, -30f - tw * 15f); p.le = 55f; p.re = 55f;
                        p.handFollow = 1f; break;
                    }
                case Clip.Pour:
                    p.bp = 30f * up; p.crouch = 0.05f * up;
                    p.l = new Vector3(-70f - 40f * up, 0f, -18f); p.r = new Vector3(-50f - 30f * up, 0f, -18f); p.le = 20f; p.re = 20f; p.handFollow = 1f; break;
                case Clip.Scoop:
                    {
                        float dip = Mathf.Abs(Mathf.Sin(t * 4f));
                        p.bp = 40f + dip * 15f; p.crouch = 0.12f;
                        p.l = new Vector3(-45f - dip * 30f, 0f, -18f); p.r = new Vector3(-45f - dip * 30f, 0f, -18f); p.le = 15f; p.re = 15f; p.handFollow = 1f;
                        break;
                    }
                case Clip.ReachIn:
                    p.bp = 30f * up; p.crouch = 0.1f * up; p.fwd = 0.08f * up;
                    p.r = new Vector3(-80f * up - 10f, 0f, -10f); p.l = new Vector3(-60f * up, 0f, -10f); p.le = 10f; p.re = 10f; p.handFollow = 1f; break;
                case Clip.Pull: // open a door: reach, grab, pull back
                    {
                        float grab = Mathf.Clamp01(u * 2.5f), back = Mathf.Clamp01((u - 0.35f) * 2.2f) * (1f - Mathf.Clamp01((u - 0.8f) * 5f));
                        p.bp = 10f * grab - 14f * back;
                        p.r = new Vector3(-85f * grab + 30f * back, -20f * back, -10f); p.re = 10f + 50f * back;
                        p.l = new Vector3(-20f * back, 0f, 15f); p.le = 20f; break;
                    }
                case Clip.Press:
                    p.bp = 8f * up; p.r = new Vector3(-90f * up, 0f, -6f); p.re = 5f; p.l = new Vector3(0f, 0f, 10f); p.le = 15f; break;
            }
            return p;
        }

        // ------------------------------------------------------------------ apply
        void LateUpdate()
        {
            if (!body || !player) return;
            float dt = Time.deltaTime, t = Time.time;
            if (dt <= 0f) return;

            if (oneShot != Clip.None) { oneT += dt; if (oneT >= oneDur) oneShot = Clip.None; }
            bool holding = holdFrame >= Time.frameCount - 1 && holdClip != Clip.None;
            if (!holding) holdClip = Clip.None; else holdT += dt;

            Pose target = CarryPose(t);
            if (player.Held && player.Held.Grabbing && player.Held.GetComponent<CarryGrip>())
                target = ClipPose(player.Held.transform.position.y - transform.position.y < .45f ? Clip.PickLow : Clip.PickHigh, player.Held.GrabProgress, t);
            if (holdClip != Clip.None) target = Pose.Lerp(target, ClipPose(holdClip, Mathf.Clamp01(holdT * 2f), t), 1f);
            if (oneShot != Clip.None)
            {
                float u = oneT / oneDur;
                float w = Mathf.Clamp01(Mathf.Min(u, 1f - u) * 6f + 0.35f);
                target = Pose.Lerp(target, ClipPose(oneShot, u, t), w);
            }
            if (player.Stunned) target = Base();

            cur = Pose.Lerp(cur, target, 1f - Mathf.Exp(-blend * dt));
            ApplyPose(cur);
        }

        void ApplyPose(Pose p)
        {
            // 1) extra body lean / twist / crouch around the feet, carried by body, leaf and arms
            Quaternion extra = ToRig(Quaternion.Euler(p.bp, p.by, p.br));
            Vector3 off = ToRig(new Vector3(0f, -p.crouch, p.fwd));
            TransformAround(body, extra, off);
            TransformAround(leaf, extra, off);
            TransformAround(armL, extra, off); TransformAround(armR, extra, off);
            TransformAround(armLLo, extra, off); TransformAround(armRLo, extra, off);

            // 2) arm override
            if (p.armW > 0.001f)
            {
                Quaternion bodyDelta = body.localRotation * Quaternion.Inverse(bodyRR);
                SetArm(armL, armLRP, armLRR, armLLo, armLLoRP, armLLoRR, bodyDelta, p.l, p.le, -1f, p.armW);
                SetArm(armR, armRRP, armRRR, armRLo, armRLoRP, armRLoRR, bodyDelta, p.r, p.re, 1f, p.armW);
            }

            // 3) hold anchor follows the hands
            if (player.holdPoint)
            {
                var contacts = player.Held ? player.Held.GetComponent<CarryGrip>() : null;
                if (contacts)
                {
                    player.holdPoint.localPosition = Vector3.Lerp(player.holdPoint.localPosition, Vector3.Scale(contacts.carryAnchor, new Vector3(1f, 1f, carryDepth)), 1f - Mathf.Exp(-25f * Time.deltaTime));
                    LeftGripWorld = ReachArm(armL, armLLo, armLRP, armLLoRP, armLRR, armLLoRR, contacts.LeftWorld, -1f, handL ? handLRP - armLLoRP : Vector3.zero, forearmL);
                    RightGripWorld = ReachArm(armR, armRLo, armRRP, armRLoRP, armRRR, armRLoRR, contacts.RightWorld, 1f, handR ? handRRP - armRLoRP : Vector3.zero, forearmR);
                    return;
                }
                Vector3 baseLocal = p.holdLocal;
                if (p.handFollow > 0.01f && armLLo && armRLo)
                {
                    Vector3 hands = (HandWorld(armLLo, armL, handL) + HandWorld(armRLo, armR, handR)) * 0.5f;
                    if (player.Held && (player.Held.tool == ToolType.Towel || player.Held.tool == ToolType.Lid)) hands = HandWorld(armRLo, armR, handR);
                    Vector3 handLocal = transform.InverseTransformPoint(hands) + new Vector3(0f, -0.04f, 0.1f);
                    baseLocal = Vector3.Lerp(baseLocal, handLocal, p.handFollow);
                }
                player.holdPoint.localPosition = Vector3.Lerp(player.holdPoint.localPosition, baseLocal, 1f - Mathf.Exp(-25f * Time.deltaTime));
            }
        }

        Vector3 HandWorld(Transform lower, Transform upper, Transform hand)
        {
            if (hand) return hand.position;
            // tip of the lower arm: continue from elbow along the arm direction
            Vector3 elbow = lower.position;
            Vector3 dir = (lower.position - upper.position);
            if (dir.sqrMagnitude < 1e-6f) dir = -lower.up;
            return elbow + dir.normalized * 0.16f;
        }

        Vector3 ReachArm(Transform upper, Transform lower, Vector3 restUpper, Vector3 restLower, Quaternion upperRestRotation, Quaternion lowerRestRotation, Vector3 worldTarget, float side, Vector3 restForearm, float second)
        {
            if (!upper || !lower) return worldTarget;
            var frame = upper.parent;
            // Apple torso has depth: roll the shoulders forward for a cradle rather than
            // pulling the object through the face just to keep it inside the arm span.
            float cradle = player.Held && player.Held.Grabbing ? Mathf.SmoothStep(0, 1, player.Held.GrabProgress / .34f) : 1f;
            var grip=player.Held?player.Held.GetComponent<CarryGrip>():null;
            upper.localPosition += frame.InverseTransformVector((transform.forward * (grip?grip.shoulderForward:.45f) - transform.right * side * (grip?grip.shoulderInward:.17f)) * cradle * carryDepth);
            Vector3 start = upper.localPosition;
            Vector3 desired = frame.InverseTransformPoint(worldTarget) - start;
            float first = Vector3.Distance(restUpper, restLower);
            if (first < .01f || desired.sqrMagnitude < .00001f) return lower.position;
            float distance = Mathf.Clamp(desired.magnitude, Mathf.Abs(first - second) + .003f, first + second - .003f);
            Vector3 direction = desired.normalized;
            Vector3 bend = ToRig(Vector3.down + Vector3.right * side * .65f);
            bend = Vector3.ProjectOnPlane(bend, direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.back;
            float along = (first * first - second * second + distance * distance) / (2f * distance);
            Vector3 elbow = start + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, first * first - along * along));
            Vector3 wrist = start + direction * distance;
            Vector3 restDirection = restLower - restUpper;
            Vector3 restLowerDirection = restForearm.sqrMagnitude > 1e-8f ? restForearm : restDirection;
            upper.localRotation = Quaternion.FromToRotation(restDirection, elbow - start) * upperRestRotation;
            lower.localPosition = elbow;
            lower.localRotation = Quaternion.FromToRotation(restLowerDirection, wrist - elbow) * lowerRestRotation;
            return frame.TransformPoint(wrist);
        }

        void TransformAround(Transform tr, Quaternion extra, Vector3 off)
        {
            if (!tr) return;
            tr.localPosition = pivotRig + extra * (tr.localPosition - pivotRig) + off;
            tr.localRotation = extra * tr.localRotation;
        }

        void SetArm(Transform up, Vector3 upRP, Quaternion upRR, Transform lo, Vector3 loRP, Quaternion loRR, Quaternion bodyDelta, Vector3 a, float elbow, float side, float w)
        {
            if (!up) return;
            Vector3 tp = body.localPosition + bodyDelta * (upRP - bodyRP);
            Quaternion tr = bodyDelta * Turn(upRR, Quaternion.Euler(a.x, a.y * side, side * a.z));
            up.localPosition = Vector3.Lerp(up.localPosition, tp, w);
            up.localRotation = Quaternion.Slerp(up.localRotation, tr, w);
            if (!lo) return;
            Quaternion d = up.localRotation * Quaternion.Inverse(upRR);
            Vector3 lp = up.localPosition + d * (loRP - upRP);
            Quaternion lr = d * Turn(loRR, Quaternion.Euler(-elbow, 0f, 0f));
            lo.localPosition = Vector3.Lerp(lo.localPosition, lp, w);
            lo.localRotation = Quaternion.Slerp(lo.localRotation, lr, w);
        }
    }
}
