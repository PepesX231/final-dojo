using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Gives every fruit kid the same rig as the apple so they all share its procedural animation
    /// (AppleProceduralAnimator: natural walk with arm swing, pumping run, jumps, idle) and the apple's
    /// action poses (KidActionAnimator: carry, punch, pick up...).
    ///
    /// The apple is built from rigid parts named Body, Arm_L, Arm_L_Lower, Arm_R, Arm_R_Lower, Leg_L, Leg_L_Lower,
    /// Leg_R, Leg_R_Lower, all siblings in a y-up frame. This builds that frame ("AppleStyleRig") for a fruit:
    ///  - puppet fruits (Orange, Watermelon, Wax apple): their limbs were blend-shape meshes that only knew one baked
    ///    walk, so they are replaced by real two-part limbs (thin black arms/legs with elbows and knees) carrying the
    ///    fruit's own gloves and sneakers;
    ///  - Banana (skinned bones): the bones are straightened into a neutral stance and then follow apple-style proxies.
    /// </summary>
    public static class FruitLimbs
    {
        public const string RigName = "AppleStyleRig";

        public static bool NeedsConversion(Transform chr)
        {
            if (!chr || chr.Find(RigName)) return false;
            foreach (var t in chr.GetComponentsInChildren<Transform>(true)) if (t.name.Contains("swinging glove") || t.name == "Banana_Rig") return true;
            return false;
        }

        static Transform Find(Transform root, string part) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == part || t.name.Contains(part)) return t; return null; }

        /// Builds the apple-style rig under `chr`. Returns the rig root (or null when the model is not a fruit rig).
        public static Transform Convert(Transform chr)
        {
            if (!NeedsConversion(chr)) return chr ? chr.Find(RigName) : null;
            var anim = chr.GetComponentInChildren<Animator>(true);
            if (anim) { anim.Rebind(); anim.Update(0f); anim.enabled = false; }   // the authored walk frame -> frozen
            var rig = new GameObject(RigName).transform;
            rig.SetParent(chr, false);
            rig.localPosition = Vector3.zero; rig.localRotation = Quaternion.identity;
            rig.localScale = Vector3.one / Mathf.Max(1e-4f, chr.localScale.x);
            var bananaRig = Find(chr, "Banana_Rig");
            if (bananaRig) BuildBanana(chr, rig, bananaRig); else BuildPuppet(chr, rig);
            return rig;
        }

        // ------------------------------------------------------------------ puppet fruits
        static void BuildPuppet(Transform chr, Transform rig)
        {
            var body = Find(chr, "CTRL • body waddle");
            Transform footL = Find(chr, "CTRL • duck foot L"), footR = Find(chr, "CTRL • duck foot R");
            Transform gloveL = Find(chr, "CTRL • swinging glove L"), gloveR = Find(chr, "CTRL • swinging glove R");
            if (!body || !footL || !footR || !gloveL || !gloveR) return;
            var walkFrame = body.parent;
            Material ink = null;
            foreach (var smr in chr.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.name.StartsWith("Arm") || smr.name.StartsWith("Leg")) { if (!ink) ink = smr.sharedMaterial; smr.gameObject.SetActive(false); }

            // measure in the rig frame (y up, z forward, metres-ish)
            Bounds core = CoreBounds(body);
            Vector3 L(Vector3 world) => rig.InverseTransformPoint(world);
            Vector3 cMin = L(core.min), cMax = L(core.max), cC = L(core.center);
            float coreH = Mathf.Abs(cMax.y - cMin.y), coreW = Mathf.Abs(cMax.x - cMin.x);
            float ground = Mathf.Min(L(Centre(footL, "sole", -1f)).y, L(Centre(footR, "sole", -1f)).y);
            float ankleY = ground + 0.11f * coreH;
            float hipY = Mathf.Min(cMin.y, cMax.y) + coreH * 0.16f;
            float hipX = coreW * 0.2f;
            float legHalf = Mathf.Max(0.02f, (hipY - ankleY) * 0.5f);
            Vector3 shL = L(Centre(body, "Shoulder", 0f, -1)), shR = L(Centre(body, "Shoulder", 0f, 1));
            if (shL.x > shR.x) { var tmp = shL; shL = shR; shR = tmp; }   // L = -x like the apple
            float armHalf = Mathf.Max(0.03f, (shL.y - (hipY + coreH * 0.05f)) * 0.5f);
            float thick = Mathf.Clamp(coreH * 0.07f, 0.02f, 0.06f);

            // Body pivot near the hips so leans rotate around the waist
            var bodyP = new GameObject("Body").transform; bodyP.SetParent(rig, false);
            bodyP.localPosition = new Vector3(cC.x, hipY + coreH * 0.15f, cC.z);
            body.SetParent(bodyP, true);

            // the gloves / sneakers that sit on the left (-x) side
            bool gloveLLeft = L(gloveL.position).x < L(gloveR.position).x;
            var gL = gloveLLeft ? gloveL : gloveR; var gR = gloveLLeft ? gloveR : gloveL;
            bool footLLeft = L(footL.position).x < L(footR.position).x;
            var fL = footLLeft ? footL : footR; var fR = footLLeft ? footR : footL;

            Limb(rig, "Arm_L", shL, armHalf, armHalf, thick * 0.85f, ink, gL, "cuff", walkFrame, true, -1f);
            Limb(rig, "Arm_R", shR, armHalf, armHalf, thick * 0.85f, ink, gR, "cuff", walkFrame, true, 1f);
            Limb(rig, "Leg_L", new Vector3(cC.x - hipX, hipY, cC.z), legHalf, legHalf, thick, ink, fL, "upper", walkFrame, false, -1f);
            Limb(rig, "Leg_R", new Vector3(cC.x + hipX, hipY, cC.z), legHalf, legHalf, thick, ink, fR, "upper", walkFrame, false, 1f);
        }

        /// Upper part at `joint`, lower part at the elbow / knee, the glove / sneaker at the end of the lower part.
        static void Limb(Transform rig, string name, Vector3 joint, float upper, float lower, float thick, Material ink, Transform end, string endPart, Transform walkFrame, bool arm, float side)
        {
            var up = new GameObject(name).transform; up.SetParent(rig, false); up.localPosition = joint;
            Tube(up, upper, thick, ink);
            var lo = new GameObject(name + "_Lower").transform; lo.SetParent(rig, false); lo.localPosition = joint + Vector3.down * upper;
            Tube(lo, lower, thick * 0.95f, ink);
            if (!end) return;
            // glove / sneaker: upright in the fruit's frame, attached at the wrist / ankle
            end.SetParent(lo, true);
            end.localRotation = Quaternion.Inverse(lo.rotation) * walkFrame.rotation * (arm ? Quaternion.Euler(0, 0, side * 8f) : Quaternion.identity);
            Vector3 attach = Centre(end, endPart, arm ? 0f : 1f);
            Vector3 target = lo.TransformPoint(Vector3.down * lower);
            end.position += target - attach;
        }

        static void Tube(Transform parent, float length, float thick, Material ink)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var col = go.GetComponent<Collider>(); if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            go.name = "Limb";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.down * length * 0.5f;
            go.transform.localScale = new Vector3(thick, (length + thick) * 0.5f, thick);
            var r = go.GetComponent<MeshRenderer>(); if (ink) r.sharedMaterial = ink;
        }

        static Bounds CoreBounds(Transform body)
        {
            Bounds b = default; bool any = false;
            foreach (var r in body.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                if (n.Contains("Shoulder") || n.Contains("Eye") || n.Contains("Smile") || n.Contains("leaf") || n.Contains("stem") || n.Contains("vein")) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b : new Bounds(body.position, Vector3.one * 0.3f);
        }

        /// Centre of the renderers whose name contains `part` (bias -1 bottom .. 1 top); `pickSide` -1/1 picks one of two by x.
        static Vector3 Centre(Transform root, string part, float bias, int pickSide = 0)
        {
            var list = new List<Bounds>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) if (r.name.Contains(part)) list.Add(r.bounds);
            if (list.Count == 0) return root.position;
            if (pickSide != 0 && list.Count >= 2)
            {
                list.Sort((a, b) => Vector3.Dot(a.center - b.center, root.right).CompareTo(0f));
                var pick = pickSide < 0 ? list[0] : list[list.Count - 1];
                list.Clear(); list.Add(pick);
            }
            var bb = list[0]; foreach (var x in list) bb.Encapsulate(x);
            return bb.center + Vector3.up * bb.extents.y * bias;
        }

        // ------------------------------------------------------------------ banana (bones follow proxies)
        static void BuildBanana(Transform chr, Transform rig, Transform bananaRig)
        {
            Transform B(string n) { foreach (var t in bananaRig.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }
            Transform body = B("Body"), legL = B("Leg_L"), kneeL = B("Leg_L_Lower"), footL = B("Foot_L"), legR = B("Leg_R"), kneeR = B("Leg_R_Lower"), footR = B("Foot_R");
            Transform armL = B("Arm_L"), elbowL = B("Arm_L_Lower"), handL = B("Hand_L"), armR = B("Arm_R"), elbowR = B("Arm_R_Lower"), handR = B("Hand_R");
            if (!body || !legL || !kneeL || !footL || !legR || !kneeR || !footR || !armL || !elbowL || !armR || !elbowR) return;
            Vector3 down = -rig.up;
            Upright(chr, rig, bananaRig, body, new[] { legL, kneeL, footL, legR, kneeR, footR });
            // neutral stance: legs straight down, arms down and a little out (the prefab is frozen mid-stride)
            // the banana's hips sit close together, so the legs splay a touch (feet a shoulder-width apart, never crossed)
            // legs hang straight down (no slant); hips that sit too close together are moved apart instead,
            // so the feet stand a little apart like a person's
            float legSide = Mathf.Sign(rig.InverseTransformPoint(legL.position).x - rig.InverseTransformPoint(legR.position).x);
            float legLen = Vector3.Distance(legL.position, kneeL.position) + Vector3.Distance(kneeL.position, footL.position);
            Vector3 across = Vector3.ProjectOnPlane(legL.position - legR.position, rig.up);
            float gap = across.magnitude, want = legLen * 0.42f;
            if (gap < want)
            {
                Vector3 push = rig.right * legSide * (want - gap) * 0.5f;
                var l = new[] { legL, kneeL, footL }; var r = new[] { legR, kneeR, footR };
                foreach (var t in l) if (t && System.Array.IndexOf(l, t.parent) < 0) t.position += push;
                foreach (var t in r) if (t && System.Array.IndexOf(r, t.parent) < 0) t.position -= push;
            }
            Straighten(legL, kneeL, footL, down);
            Straighten(legR, kneeR, footR, down);
            // long arms: hang them out from the body so the gloves rest at hip height, not by the shins
            float armSide = Mathf.Sign(rig.InverseTransformPoint(armL.position).x - rig.InverseTransformPoint(armR.position).x);
            Straighten(armL, elbowL, handL, (down + rig.right * armSide * 0.55f).normalized);
            Straighten(armR, elbowR, handR, (down - rig.right * armSide * 0.55f).normalized);
            // bones get unique names so the action animator binds to the proxies, not the bones
            foreach (var t in new[] { body, legL, kneeL, footL, legR, kneeR, footR, armL, elbowL, handL, armR, elbowR, handR }) if (t) t.name += "_bone";
            var follow = chr.gameObject.AddComponent<FruitBoneFollow>();
            Transform P(string n, Transform bone)
            {
                var p = new GameObject(n).transform; p.SetParent(rig, false);
                p.position = bone.position; p.rotation = rig.rotation;
                follow.Add(p, bone);
                return p;
            }
            P("Body", body);
            var pl = P("Leg_L", legL); var pkl = P("Leg_L_Lower", kneeL); follow.Add(pkl, footL);
            var pr = P("Leg_R", legR); var pkr = P("Leg_R_Lower", kneeR); follow.Add(pkr, footR);
            P("Arm_L", armL); P("Arm_L_Lower", elbowL);
            P("Arm_R", armR); P("Arm_R_Lower", elbowR);
            // apple convention: L on -x
            if (rig.InverseTransformPoint(legL.position).x > 0f) foreach (Transform c in rig) c.name = c.name.Contains("_L") ? c.name.Replace("_L", "_R") : c.name.Replace("_R", "_L");
        }

        /// The banana model leans like a crescent. Stand it up: rotate everything but the legs so the body's long axis
        /// is vertical, and slide it so the body's centre sits right above the hips.
        static void Upright(Transform chr, Transform rig, Transform bananaRig, Transform body, Transform[] legs)
        {
            var smr = chr.GetComponentInChildren<SkinnedMeshRenderer>(true); if (!smr || !smr.sharedMesh) return;
            int bi = System.Array.IndexOf(smr.bones, body); if (bi < 0) return;
            var baked = new Mesh(); smr.BakeMesh(baked, true);
            var v = baked.vertices; var w = smr.sharedMesh.boneWeights; if (w.Length != v.Length) { Object.Destroy(baked); return; }
            var m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            var pts = new List<Vector3>();
            for (int i = 0; i < v.Length; i++)
            {
                var bw = w[i]; float wt = bw.boneIndex0 == bi ? bw.weight0 : bw.boneIndex1 == bi ? bw.weight1 : bw.boneIndex2 == bi ? bw.weight2 : bw.boneIndex3 == bi ? bw.weight3 : 0f;
                if (wt > .6f) pts.Add(rig.InverseTransformPoint(m.MultiplyPoint3x4(v[i])));
            }
            if (Application.isPlaying) Object.Destroy(baked); else Object.DestroyImmediate(baked);
            if (pts.Count < 20) return;
            Vector3 c = Vector3.zero; foreach (var p in pts) c += p; c /= pts.Count;
            // principal axis (power iteration on the covariance)
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (var p0 in pts) { var p = p0 - c; xx += p.x * p.x; xy += p.x * p.y; xz += p.x * p.z; yy += p.y * p.y; yz += p.y * p.z; zz += p.z * p.z; }
            Vector3 a = Vector3.up;
            for (int k = 0; k < 30; k++) a = new Vector3(xx * a.x + xy * a.y + xz * a.z, xy * a.x + yy * a.y + yz * a.z, xz * a.x + yz * a.y + zz * a.z).normalized;
            if (a.y < 0) a = -a;
            var q = Quaternion.FromToRotation(a, Vector3.up);
            Debug.Log("[FruitLimbs] banana lean " + Vector3.Angle(a, Vector3.up).ToString("F1") + "°");
            Vector3 hips = Vector3.zero; hips = (rig.InverseTransformPoint(legs[0].position) + rig.InverseTransformPoint(legs[3].position)) * .5f;
            Vector3 pivot = new Vector3(c.x, hips.y, c.z);
            Vector3 c2 = pivot + q * (c - pivot);
            Vector3 shift = new Vector3(hips.x - c2.x, 0, hips.z - c2.z);
            var qW = rig.rotation * q * Quaternion.Inverse(rig.rotation);
            Vector3 pivotW = rig.TransformPoint(pivot), shiftW = rig.TransformVector(shift);
            var legSet = new HashSet<Transform>(legs);
            var moved = new List<Transform>();
            foreach (var b in smr.bones)
            {
                if (!b || legSet.Contains(b) || b == bananaRig || !b.IsChildOf(bananaRig)) continue;
                bool parentMoves = false; for (var t = b.parent; t && t != bananaRig; t = t.parent) if (System.Array.IndexOf(smr.bones, t) >= 0 && !legSet.Contains(t)) { parentMoves = true; break; }
                if (parentMoves) continue;
                moved.Add(b);
            }
            foreach (var b in moved) { b.position = pivotW + qW * (b.position - pivotW) + shiftW; b.rotation = qW * b.rotation; }
        }

        /// Rotates upper (and with it lower / end) so the chain hangs along `dir`.
        static void Straighten(Transform upper, Transform lower, Transform end, Vector3 dir)
        {
            if (!upper || !lower) return;
            float lu = Vector3.Distance(upper.position, lower.position);
            Vector3 endPos = end ? end.position : lower.position + (lower.position - upper.position);
            float ll = Vector3.Distance(lower.position, endPos);
            Quaternion qU = Quaternion.FromToRotation(lower.position - upper.position, dir);
            upper.rotation = qU * upper.rotation;
            Vector3 knee = upper.position + dir * lu;
            Quaternion qL = Quaternion.FromToRotation(endPos - lower.position, dir);
            Quaternion lowerRot = qL * lower.rotation;
            Quaternion endRel = end ? Quaternion.Inverse(lower.rotation) * end.rotation : Quaternion.identity;
            bool endIsChild = end && end.parent == lower;
            lower.position = knee; lower.rotation = lowerRot;
            if (end && !endIsChild) { end.position = knee + dir * ll; end.rotation = lowerRot * endRel; }
        }
    }

    /// Bones (banana) copy the motion of their apple-style proxies every frame, after all animation layers.
    [DefaultExecutionOrder(900)]
    public class FruitBoneFollow : MonoBehaviour
    {
        struct Link { public Transform proxy, bone; public Vector3 pos; public Quaternion rot; }
        readonly List<Link> links = new List<Link>();

        public void Add(Transform proxy, Transform bone)
        {
            if (!proxy || !bone) return;
            links.Add(new Link { proxy = proxy, bone = bone, pos = proxy.InverseTransformPoint(bone.position), rot = Quaternion.Inverse(proxy.rotation) * bone.rotation });
        }

        void LateUpdate()
        {
            foreach (var l in links)
            {
                if (!l.proxy || !l.bone) continue;
                l.bone.SetPositionAndRotation(l.proxy.TransformPoint(l.pos), l.proxy.rotation * l.rot);
            }
        }
    }
}
