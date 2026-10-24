using UnityEngine;

namespace MamasComing
{
    /// A fruit kid placed in the scene for the title / lobby shots. Move, rotate or scale this object in the
    /// Scene view; the pose below only adds motion on top of where you put it.
    [ExecuteAlways, DisallowMultipleComponent]
    public class MamaPosedKid : MonoBehaviour
    {
        public enum Pose { Hero, Hop, CoolLean, Asleep, Sneak, Sit, SitSlouch, SitBounce, Wave, Flex, FingerGuns, HandsOnHips, SitHandsBehindHead }

        [Tooltip("0 Apple, 1 Banana, 2 Orange, 3 Watermelon, 4 Wax apple")]
        [Range(0, KidLook.Count - 1)] public int kid;
        public Pose pose = Pose.Hero;
        [Tooltip("Animation speed / pose strength.")]
        public float energy = 1f;
        [Tooltip("Seconds of offset so the kids don't move in sync.")]
        public float phase;
        public bool partyHat;
        public Color hatColor = new Color(1f, 0.45f, 0.6f);
        [Tooltip("Short speech bubble shown next to the kid (optional).")]
        public string bubbleEn, bubbleTh;
        [Tooltip("Where name tags / bubbles attach (auto: top of the model).")]
        public Transform head;
        [Header("Sitting / standing on furniture")]
        [Tooltip("Lift and push the model so it rests on this object's origin (seat surface) without sinking into the backrest.")]
        public bool fitToSeat;
        [Tooltip("How far behind the origin (local -z) the model may reach before it is pushed forward (backrest).")]
        public float backClear = 0.18f;
        [Tooltip("Scale the model to this height in metres (0 = keep). Keeps every fruit the same size on the sofa.")]
        public float normalizeHeight;
        [Tooltip("The apple is mostly round body, so at the same height it reads much bigger than the tall fruits; this shrinks it to match.")]
        public float appleHeightScale = 0.8f;
        public static bool logFit;
        Vector3 baseOffset; int fitFrames; float popT = 1f; Renderer[] cachedRends; bool converted;

        [Tooltip("Model used for the apple (the scene kid's apple, saved as a prefab by the front-end builder).")]
        public GameObject appleSource;
        [HideInInspector] public int builtKid = -1;
        // arm rest pose captured when the model is built (the live transforms are posed every frame)
        [SerializeField, HideInInspector] Vector3[] restP = new Vector3[0];
        [SerializeField, HideInInspector] Quaternion[] restR = new Quaternion[0];
        Transform model, hat; Animator anim; Transform armR, armRLo, armL, armLLo;
        Vector3 armRP, armRLoP, armLP, armLLoP; Quaternion armRR, armRLoR, armLR, armLLoR;
        float t;

        public Transform Model => model;

        void OnEnable() { Bind(); if (Application.isPlaying && model && !converted) { var chr = model.childCount > 0 ? model.GetChild(0) : null; if (chr) AttachRig(chr); fitFrames = fitToSeat ? 3 : 0; } }

        /// Fruits get the apple-style limb rig (FruitLimbs) so poses and idle match the game, then are scaled to a common height.
        void AttachRig(Transform chr)
        {
            converted = true;
            if (chr && FruitLimbs.NeedsConversion(chr)) { FruitLimbs.Convert(chr); restP = new Vector3[0]; restR = new Quaternion[0]; Bind(); }
            if (normalizeHeight > 0f && model)
            {
                var b = TightBounds(model);
                float h = b.size.y / Mathf.Max(1e-3f, transform.lossyScale.y);
                if (h > 0.05f) chr.localScale *= normalizeHeight * (kid == KidLook.Apple ? appleHeightScale : 1f) / h;
                if (logFit) { string top = "", bot = ""; float ty = -1e9f, by = 1e9f; foreach (var r in model.GetComponentsInChildren<Renderer>()) { if (!r.enabled || r.forceRenderingOff) continue; if (r.bounds.max.y > ty) { ty = r.bounds.max.y; top = r.name + "/" + r.GetType().Name; } if (r.bounds.min.y < by) { by = r.bounds.min.y; bot = r.name; } }
                    Debug.Log("[PosedKid] " + chr.name + " h=" + h.ToString("F2") + " tight=" + b.min.y.ToString("F2") + ".." + b.max.y.ToString("F2") + " top=" + top + " bot=" + bot + " scale=" + chr.localScale.ToString("F3") + " lossy=" + transform.lossyScale.y); }
            }
        }

        /// Change the pose at runtime (lobby picks one per fruit).
        public void SetPose(Pose p) { pose = p; fitFrames = fitToSeat ? 3 : 0; }

#if UNITY_EDITOR
        // change "kid" or "party hat" in the Inspector -> the model is rebuilt in the scene
        void OnValidate()
        {
            if (Application.isPlaying) return;
            bool hasHat = transform.Find("Model/PartyHat");
            if (builtKid == kid && hasHat == partyHat) { UnityEditor.EditorApplication.delayCall += () => { if (this) PoseInEditor(); }; return; }
            UnityEditor.EditorApplication.delayCall += () => { if (this && !Application.isPlaying) { Rebuild(); UnityEditor.EditorUtility.SetDirty(this); } };
        }
        [ContextMenu("Rebuild model")] void RebuildMenu() { Rebuild(); UnityEditor.EditorUtility.SetDirty(this); }
#endif

        /// Builds (or rebuilds) the visible model for `kid` as a child named "Model".
        public void Rebuild()
        {
            var old = transform.Find("Model");
            if (old) { if (Application.isPlaying) { old.name = "Model (old)"; old.gameObject.SetActive(false); old.SetParent(null, false); Destroy(old.gameObject); } else DestroyImmediate(old.gameObject); }
            if (old) old.name = "_old";
            var src = Source(kid, out Vector3 lp, out Quaternion lr, out Vector3 ls);
            if (!src) { builtKid = -1; return; }
            var holder = new GameObject("Model").transform;
            holder.SetParent(transform, false);
            var go = Instantiate(src, holder, false);
            go.name = src.name; go.SetActive(true);
            go.transform.localPosition = lp; go.transform.localRotation = lr; go.transform.localScale = ls;
            for (int pass = 0; pass < 3; pass++)
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) { if (!mb) continue; if (Application.isPlaying) Destroy(mb); else { try { DestroyImmediate(mb); } catch { } } }
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            builtKid = kid;
            if (partyHat) MakeHat(holder);
            cachedRends = null; baseOffset = Vector3.zero;
            model = holder;
            if (Application.isPlaying) { AttachRig(go.transform); popT = 0f; fitFrames = fitToSeat ? 3 : 0; }
            restP = new Vector3[0]; restR = new Quaternion[0];
            Bind();
        }

        GameObject Source(int id, out Vector3 lp, out Quaternion lr, out Vector3 ls)
        {
            lp = Vector3.zero; lr = Quaternion.identity; ls = Vector3.one;
            GameObject src = null;
            if (id == KidLook.Apple && appleSource) { ls = appleSource.transform.localScale; return appleSource; }
            if (id == KidLook.Apple)
            {
                // the apple is the scene kid's own model
                foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    foreach (var t in pc.GetComponentsInChildren<Transform>(true)) if (t.name == "CHR_AppleChild") { src = t.gameObject; break; }
                if (!src && KidLook.AppleTemplate) src = KidLook.AppleTemplate;
                if (src) { ls = src.transform.lossyScale; return src; }
                return null;
            }
            var cat = Resources.Load<KidCatalog>("MamaKidCatalog");
            var p = cat ? cat.Prefab(id) : null;
            if (!p) return null;
            foreach (var t in p.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("CHR_")) { src = t.gameObject; lp = t.localPosition; lr = t.localRotation; ls = t.localScale; break; }
            return src;
        }

        void MakeHat(Transform holder)
        {
            var b = Bounds(holder);
            var mr = holder.GetComponentInChildren<Renderer>();
            if (!mr) return;
            var go = new GameObject("PartyHat", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(holder, false);
            go.transform.position = new Vector3(b.center.x, b.max.y - b.size.y * 0.06f, b.center.z);
            go.transform.localRotation = Quaternion.Euler(0, 0, -16f);
            float r = Mathf.Max(0.08f, b.size.x * 0.16f);
            go.GetComponent<MeshFilter>().sharedMesh = Cone(r, r * 2.6f);
            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = HatMaterial(hatColor);
        }

        /// Plain lit material in the hat colour (copied from a URP Lit material already used in the scene).
        static Material HatMaterial(Color c)
        {
            Material src = null;
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial;
                if (m && m.shader && m.shader.name == "Universal Render Pipeline/Lit" && !m.mainTexture) { src = m; break; }
            }
            var mat = src ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "PartyHat";
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            return mat;
        }

        static Mesh Cone(float r, float h)
        {
            const int n = 18; var v = new Vector3[n + 2]; var tri = new int[n * 6];
            v[n] = new Vector3(0, h, 0); v[n + 1] = Vector3.zero;
            for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; v[i] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); }
            for (int i = 0; i < n; i++) { int j = (i + 1) % n; tri[i * 6] = i; tri[i * 6 + 1] = n; tri[i * 6 + 2] = j; tri[i * 6 + 3] = j; tri[i * 6 + 4] = n + 1; tri[i * 6 + 5] = i; }
            var m = new Mesh { name = "PartyHatCone", vertices = v, triangles = tri }; m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        public static Bounds Bounds(Transform root) => Bounds(root.GetComponentsInChildren<Renderer>(), root);
        public static Bounds Bounds(Renderer[] rs, Transform fallback = null)
        {
            if (rs.Length == 0) return new Bounds(fallback ? fallback.position : Vector3.zero, Vector3.one * 0.5f);
            Bounds b = default; bool any = false;
            foreach (var r in rs) { if (!r || r.name == "PartyHat" || !r.enabled || r.forceRenderingOff) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            return any ? b : new Bounds(fallback ? fallback.position : Vector3.zero, Vector3.one * 0.5f);
        }

        /// Bounds from the actual vertices: skinned meshes report loose, animation-sized bounds that made some fruits
        /// tiny and others huge when normalised.
        /// Test hook: logs the real (vertex) height of every posed kid.
        public static void LogHeights()
        {
            foreach (var k in FindObjectsByType<MamaPosedKid>(FindObjectsSortMode.None))
            {
                if (!k.model) continue; var b = TightBounds(k.model);
                string top = ""; float ty = -1e9f; foreach (var r in k.model.GetComponentsInChildren<Renderer>()) if (r.enabled && !r.forceRenderingOff && r.bounds.max.y > ty) { ty = r.bounds.max.y; top = r.name; }
                Debug.Log("[PosedKid] now " + k.name + " kid=" + k.kid + " y=" + b.min.y.ToString("F2") + ".." + b.max.y.ToString("F2") + " w=" + b.size.x.ToString("F2") + " top=" + top + " modelScale=" + k.model.localScale.ToString("F2"));
            }
        }

        public static Bounds TightBounds(Transform root)
        {
            Bounds b = default; bool any = false; var tmp = new Mesh();
            void Add(Vector3 p) { if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p); }
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r || r.name == "PartyHat" || !r.enabled || r.forceRenderingOff) continue;
                if (r is SkinnedMeshRenderer smr && smr.sharedMesh)
                {
                    smr.BakeMesh(tmp, true); var v = tmp.vertices; var m = smr.transform.localToWorldMatrix;
                    for (int i = 0; i < v.Length; i += 3) Add(m.MultiplyPoint3x4(v[i]));
                }
                else if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh && mf.sharedMesh.isReadable)
                {
                    var v = mf.sharedMesh.vertices; var m = r.transform.localToWorldMatrix;
                    for (int i = 0; i < v.Length; i += 3) Add(m.MultiplyPoint3x4(v[i]));
                }
                else { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            }
            if (Application.isPlaying) Destroy(tmp); else DestroyImmediate(tmp);
            return any ? b : new Bounds(root.position, Vector3.one * 0.5f);
        }

        void PoseInEditor() { if (!Application.isPlaying && model) ApplyPose(0.6f); }

        void Bind()
        {
            model = transform.Find("Model");
            if (!model) return;
            anim = model.GetComponentInChildren<Animator>(true);
            hat = model.Find("PartyHat");
            armR = Find(model, "Arm_R"); armRLo = Find(model, "Arm_R_Lower"); armL = Find(model, "Arm_L"); armLLo = Find(model, "Arm_L_Lower");
            var arms = new[] { armR, armRLo, armL, armLLo };
            if (restP == null || restP.Length != 4)
            {
                restP = new Vector3[4]; restR = new Quaternion[4];
                for (int i = 0; i < 4; i++) if (arms[i]) { restP[i] = arms[i].localPosition; restR[i] = arms[i].localRotation; }
            }
            armRP = restP[0]; armRR = restR[0]; armRLoP = restP[1]; armRLoR = restR[1]; armLP = restP[2]; armLR = restR[2]; armLLoP = restP[3]; armLLoR = restR[3];
            if (anim && Application.isPlaying)
            {
                anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                bool walk = pose == Pose.Hop || pose == Pose.Sneak;
                if (anim.runtimeAnimatorController) anim.Play(walk ? "Walk" : "Idle", 0, Mathf.Repeat(phase * 0.37f, 1f));
                anim.speed = pose == Pose.Sneak ? 0.35f : pose == Pose.Hop ? 1.5f : pose == Pose.Asleep ? 0.3f : 1f;
            }
            if (!Application.isPlaying && model) ApplyPose(0.6f);
        }

        static Transform Find(Transform r, string n) { foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t; return null; }

        public Vector3 HeadPoint
        {
            get
            {
                if (head) return head.position;
                if (!model) return transform.position + Vector3.up;
                if (cachedRends == null || cachedRends.Length == 0 || !cachedRends[0]) cachedRends = model.GetComponentsInChildren<Renderer>();
                var b = Bounds(cachedRends, model); return new Vector3(b.center.x, b.max.y, b.center.z);
            }
        }

        [Header("Hand prop (e.g. the mop held up like a sword)")]
        public Transform handProp;
        public Vector3 propOffset = new Vector3(0f, -0.17f, 0f);
        public Vector3 propEuler = new Vector3(0f, 0f, 180f);

        void LateUpdate()
        {
            if (!model) { Bind(); if (!model) return; }
            if (!Application.isPlaying) return;   // edit mode: posed once in OnValidate / OnEnable (keeps the Editor light)
            t += Time.unscaledDeltaTime * Mathf.Max(0.1f, energy);
            ApplyPose(t);
            if (fitFrames > 0 && popT >= 1f && --fitFrames == 0) FitToSeat();
            if (popT < 1f) { popT = Mathf.Min(1f, popT + Time.unscaledDeltaTime * 3.2f); float e = 1f + Mathf.Sin(popT * Mathf.PI * 2.5f) * (1f - popT) * 0.35f; model.localScale = model.localScale * Mathf.Lerp(0.2f, 1f, Mathf.Min(1f, popT * 2.5f)) * e; }
        }

        /// Poses the model for time `time` (also used in Edit mode so the Scene view shows the pose).
        public void ApplyPose(float time)
        {
            if (!model) { Bind(); if (!model) return; }
            float s = time + phase;
            Vector3 p = Vector3.zero; Vector3 e = Vector3.zero; Vector3 sc = Vector3.one;
            switch (pose)
            {
                case Pose.Hero:
                    // victory "V": both arms thrown up and out (the right hand holds the mop up like a sword), chin up
                    sc = new Vector3(1f, 1f + Mathf.Sin(s * 2.2f) * 0.02f, 1f); e = new Vector3(-10f, 0, -5f + Mathf.Sin(s * 1.1f) * 1.5f);
                    p.y = Mathf.Max(0f, Mathf.Sin(s * 2.2f)) * 0.02f;
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 112f + Mathf.Sin(s * 2.2f) * 3f, -42f, true);
                    Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 108f - Mathf.Sin(s * 2.2f) * 3f, -38f, false);
                    break;
                case Pose.Wave:
                    e = new Vector3(0, 0, Mathf.Sin(s * 1.6f) * 4f);
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 135f, 25f + Mathf.Sin(s * 9f) * 30f, true);
                    break;
                case Pose.Hop:
                    { float h = Mathf.Abs(Mathf.Sin(s * 3.3f)); p.y = h * 0.42f; float land = 1f - Mathf.Clamp01(h * 5f);
                      sc = new Vector3(1f + land * 0.12f, 1f - land * 0.14f + h * 0.06f, 1f + land * 0.12f); e = new Vector3(0, Mathf.Sin(s * 1.2f) * 25f, Mathf.Sin(s * 3.3f) * 6f); }
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 130f, 30f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 130f, 30f, false);
                    break;
                case Pose.CoolLean:
                    e = new Vector3(0, 0, 13f + Mathf.Sin(s * 1.4f) * 1.5f); p.y = Mathf.Abs(Mathf.Sin(s * 2.4f)) * 0.02f;
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 40f, 120f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 40f, 120f, false);
                    break;
                case Pose.Asleep:
                    e = new Vector3(-82f, 0, 0); p = new Vector3(0, 0.12f, 0); sc = new Vector3(1f + Mathf.Sin(s * 1.3f) * 0.03f, 1f, 1f + Mathf.Sin(s * 1.3f) * 0.03f);
                    break;
                case Pose.Sneak:
                    e = new Vector3(16f, Mathf.Sin(s * 0.9f) * 18f, 0); p.y = Mathf.Abs(Mathf.Sin(s * 2.1f)) * 0.03f;
                    break;
                case Pose.Sit:
                    sc = new Vector3(1f, 0.93f + Mathf.Sin(s * 1.8f) * 0.015f, 1f); e = new Vector3(-6f, 0, 0);
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 20f, 30f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 20f, 30f, false);
                    break;
                case Pose.SitSlouch:
                    sc = new Vector3(1.03f, 0.9f, 1.03f); e = new Vector3(-14f, 0, -9f + Mathf.Sin(s * 0.8f) * 2f);
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 95f, 100f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 95f, 100f, false);
                    break;
                case Pose.Flex:   // strongman: right arm flexed up, left fist on hip, puffed chest
                    sc = new Vector3(1.02f, 1.02f + Mathf.Sin(s * 2f) * 0.02f, 1.02f); e = new Vector3(-8f, 10f, -4f);
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 85f, -95f + Mathf.Sin(s * 2f) * 8f, true);
                    Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 45f, 110f, false);
                    break;
                case Pose.FingerGuns:
                    e = new Vector3(-6f, -14f, 8f + Mathf.Sin(s * 1.6f) * 2f);
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 70f + Mathf.Max(0f, Mathf.Sin(s * 1.7f)) * 12f, 20f, true);
                    Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 70f, 20f, false);
                    break;
                case Pose.HandsOnHips:
                case Pose.SitHandsBehindHead:
                    e = new Vector3(pose == Pose.SitHandsBehindHead ? 8f : -6f, 6f, 4f + Mathf.Sin(s * 1.3f) * 1.5f);
                    sc = new Vector3(1f, pose == Pose.SitHandsBehindHead ? 0.94f : 1f + Mathf.Sin(s * 2f) * 0.015f, 1f);
                    if (pose == Pose.SitHandsBehindHead) { Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 150f, 120f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 150f, 120f, false); }
                    else { Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 45f, 110f, true); Arm(armL, armLLo, armLP, armLR, armLLoP, armLLoR, 45f, 110f, false); }
                    break;
                case Pose.SitBounce:
                    { float b = Mathf.Abs(Mathf.Sin(s * 4f)); p.y = b * 0.06f; sc = new Vector3(1f, 0.93f + b * 0.05f, 1f); e = new Vector3(-4f, 0, Mathf.Sin(s * 4f) * 4f); }
                    Arm(armR, armRLo, armRP, armRR, armRLoP, armRLoR, 120f + Mathf.Sin(s * 8f) * 20f, 20f, true);
                    break;
            }
            model.localPosition = baseOffset + p; model.localRotation = Quaternion.Euler(e); model.localScale = sc;
            if (handProp && armRLo)
            {
                handProp.position = armRLo.TransformPoint(propOffset);
                handProp.rotation = armRLo.rotation * Quaternion.Euler(propEuler);
            }
        }

        /// Rests the model's lowest point on this object's origin and keeps it in front of the backrest.
        void FitToSeat()
        {
            cachedRends = model.GetComponentsInChildren<Renderer>();
            float lo = float.MaxValue, back = float.MaxValue;
            foreach (var rr in cachedRends)
            {
                if (!rr || !rr.enabled || rr.forceRenderingOff || rr.name == "PartyHat") continue;
                var bb = rr.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = new Vector3((i & 1) == 0 ? bb.min.x : bb.max.x, (i & 2) == 0 ? bb.min.y : bb.max.y, (i & 4) == 0 ? bb.min.z : bb.max.z);
                    var lp = transform.InverseTransformPoint(c); lo = Mathf.Min(lo, lp.y); back = Mathf.Min(back, lp.z);
                }
            }
            if (lo == float.MaxValue) return;
            Vector3 off = baseOffset;
            off.y -= lo;
            if (back < -backClear) off.z += -backClear - back;
            baseOffset = off;
        }

        /// Apple-style arm (separate upper / lower meshes): `shoulder` raises the whole arm out to the side,
        /// `elbow` bends the forearm further (degrees, positive = toward the head).
        void Arm(Transform up, Transform lo, Vector3 upP, Quaternion upR, Vector3 loP, Quaternion loR, float shoulder, float elbow, bool right)
        {
            if (!up) return;
            float sgn = right ? 1f : -1f;
            var q = Quaternion.AngleAxis(sgn * shoulder, Vector3.forward);
            up.localRotation = q * upR; up.localPosition = upP;
            if (!lo) return;
            var qe = Quaternion.AngleAxis(-sgn * elbow, Vector3.forward);   // positive elbow folds the forearm back toward the body
            lo.localPosition = upP + q * (loP - upP);
            lo.localRotation = qe * q * loR;
        }
    }
}
