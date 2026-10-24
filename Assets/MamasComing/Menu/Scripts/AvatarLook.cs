using System.Collections.Generic;
using UnityEngine;

namespace MamasComing.Menu
{
    /// Builds a dressed-up fruit kid for the menu and lobby: the real kid model (behaviours stripped) + a primitive hat/face accessory in the chosen colour.
    public class AvatarLook : MonoBehaviour
    {
        public const float TargetHeight = 1.05f;
        public Animator Anim { get; private set; }
        public float Height { get; private set; } = TargetHeight;
        public float Front { get; private set; } = .4f;
        public Cosmetic Look { get; private set; }
        Transform body, hat, face, propeller;
        bool built, walking; float phase;
        static Material baseMat;
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();
        static Mesh cone;

        public static AvatarLook Create(Transform parent, GameObject[] fruits, Material mat, Cosmetic c)
        {
            var go = new GameObject("Look"); go.transform.SetParent(parent, false);
            var look = go.AddComponent<AvatarLook>();
            look.Apply(fruits, mat, c);
            return look;
        }

        public void Apply(GameObject[] fruits, Material mat, Cosmetic c)
        {
            if (mat) baseMat = mat;
            bool sameBody = built && body && Look.fruit == c.fruit;
            Look = c; built = true;
            if (!sameBody)
            {
                if (body) Destroy(body.gameObject);
                body = BuildBody(KidLook.Prefab(c.fruit) ?? (fruits != null && fruits.Length > 0 ? fruits[c.fruit % fruits.Length] : null));
                walking = false; phase = Random.value * 6f;
            }
            if (hat) Destroy(hat.gameObject);
            if (face) Destroy(face.gameObject);
            propeller = null;
            hat = BuildHat(c.hat, c.Tint);
            face = BuildFace(c.face, c.Tint);
        }

        Transform BuildBody(GameObject prefab)
        {
            // measure in our own space: build with an identity world pose, restore afterwards
            var savedRot = transform.rotation; var savedScale = transform.localScale;
            transform.rotation = Quaternion.identity; transform.localScale = Vector3.one;
            try { return BuildBodyUpright(prefab); }
            finally { transform.rotation = savedRot; transform.localScale = savedScale; }
        }

        Transform BuildBodyUpright(GameObject prefab)
        {
            var root = new GameObject("Body").transform; root.SetParent(transform, false);
            if (!prefab)
            {
                var s = Prim(PrimitiveType.Sphere, root, new Vector3(0, .5f, 0), Vector3.one * .9f, new Color(1f, .55f, .2f));
                s.name = "Fallback fruit"; Height = TargetHeight; Front = .45f; Anim = null; return root;
            }
            // instantiate under an inactive holder so no gameplay Awake/OnEnable ever runs, then strip everything but visuals
            var holder = new GameObject("holder"); holder.SetActive(false);
            var inst = Instantiate(prefab, holder.transform);
            inst.name = prefab.name + " (look)";
            Strip(inst);
            inst.transform.SetParent(root, false);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = prefab.transform.localScale;
            Destroy(holder);
            Anim = inst.GetComponentInChildren<Animator>(true);
            if (Anim) { Anim.applyRootMotion = false; Anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            // normalise height so every fruit stands about kid-sized, feet on the ground
            var b = Bounds(inst);
            if (b.size.y > .01f)
            {
                float k = TargetHeight / b.size.y;
                inst.transform.localScale *= k;
                b = Bounds(inst);
                inst.transform.position += new Vector3(transform.position.x - b.center.x, transform.position.y - b.min.y, transform.position.z - b.center.z);
                b = Bounds(inst);
                Height = b.size.y; Front = Mathf.Max(.15f, b.max.z - transform.position.z);
            }
            return root;
        }

        static void Strip(GameObject go)
        {
            // MonoBehaviours may depend on each other ([RequireComponent]); later components usually depend on earlier ones
            for (int pass = 0; pass < 3; pass++)
            {
                var all = go.GetComponentsInChildren<MonoBehaviour>(true);
                if (all.Length == 0) break;
                for (int i = all.Length - 1; i >= 0; i--) if (all[i]) DestroyImmediate(all[i]);
            }
            foreach (var j in go.GetComponentsInChildren<Joint>(true)) DestroyImmediate(j);
            foreach (var cc in go.GetComponentsInChildren<CharacterController>(true)) DestroyImmediate(cc);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(a);
            foreach (var p in go.GetComponentsInChildren<ParticleSystem>(true)) p.gameObject.SetActive(false);
        }

        static Bounds Bounds(GameObject go)
        {
            bool any = false; var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                if (r.name.ToLowerInvariant().Contains("shadow")) continue;   // blob shadow quads are wider than the kid
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ------------------------------------------------------------ accessories (children of the body so they squash and spin with it)
        Transform BuildHat(int kind, Color c)
        {
            if (kind == 0 || !body) return null;
            var root = new GameObject("Hat").transform; root.SetParent(body, false);
            root.localPosition = new Vector3(0, Height * .93f, 0);
            Color dark = Color.Lerp(c, Color.black, .45f), light = Color.Lerp(c, Color.white, .55f), gold = new Color(1f, .8f, .2f);
            switch (kind)
            {
                case 1: // party cone with a pompom
                    var cn = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer)); cn.transform.SetParent(root, false);
                    cn.GetComponent<MeshFilter>().sharedMesh = Cone(); cn.GetComponent<MeshRenderer>().sharedMaterials = InkMaterial ? new[] { Mat(c), InkMaterial } : new[] { Mat(c) };
                    cn.transform.localScale = new Vector3(.32f, .42f, .32f); cn.transform.localRotation = Quaternion.Euler(0, 0, -8f);
                    Prim(PrimitiveType.Sphere, root, new Vector3(-.06f, .42f, 0), Vector3.one * .1f, light);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .01f, 0), new Vector3(.3f, .012f, .3f), light);
                    break;
                case 2: // top hat
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .02f, 0), new Vector3(.52f, .02f, .52f), dark);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .2f, 0), new Vector3(.32f, .18f, .32f), dark);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .07f, 0), new Vector3(.33f, .035f, .33f), c);
                    break;
                case 3: // crown
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .05f, 0), new Vector3(.36f, .05f, .36f), gold);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3f; var p = new Vector3(Mathf.Cos(a) * .16f, .13f, Mathf.Sin(a) * .16f);
                        var spike = Prim(PrimitiveType.Cube, root, p, new Vector3(.07f, .07f, .07f), gold); spike.transform.localRotation = Quaternion.Euler(45f, -a * Mathf.Rad2Deg, 45f);
                        Prim(PrimitiveType.Sphere, root, p + Vector3.up * .06f, Vector3.one * .045f, c);
                    }
                    break;
                case 4: // chef hat
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .08f, 0), new Vector3(.3f, .08f, .3f), Color.white);
                    Prim(PrimitiveType.Sphere, root, new Vector3(0, .24f, 0), new Vector3(.42f, .26f, .42f), Color.white);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .03f, 0), new Vector3(.31f, .02f, .31f), c);
                    break;
                case 5: // propeller beanie
                    Prim(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(.4f, .26f, .4f), c);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(0, .15f, 0), new Vector3(.03f, .04f, .03f), dark);
                    propeller = new GameObject("Propeller").transform; propeller.SetParent(root, false); propeller.localPosition = new Vector3(0, .2f, 0);
                    Prim(PrimitiveType.Cube, propeller, new Vector3(.12f, 0, 0), new Vector3(.24f, .012f, .06f), new Color(1f, .3f, .3f));
                    Prim(PrimitiveType.Cube, propeller, new Vector3(-.12f, 0, 0), new Vector3(.24f, .012f, .06f), new Color(.25f, .55f, 1f));
                    break;
                case 6: // giant bow
                    Prim(PrimitiveType.Sphere, root, new Vector3(-.13f, .08f, 0), new Vector3(.22f, .16f, .1f), c).transform.localRotation = Quaternion.Euler(0, 0, 20f);
                    Prim(PrimitiveType.Sphere, root, new Vector3(.13f, .08f, 0), new Vector3(.22f, .16f, .1f), c).transform.localRotation = Quaternion.Euler(0, 0, -20f);
                    Prim(PrimitiveType.Sphere, root, new Vector3(0, .08f, 0), Vector3.one * .09f, light);
                    break;
            }
            return root;
        }

        Transform BuildFace(int kind, Color c)
        {
            if (kind == 0 || !body) return null;
            var root = new GameObject("Face").transform; root.SetParent(body, false);
            root.localPosition = new Vector3(0, Height * .58f, Front - .02f);
            var black = new Color(.08f, .06f, .08f);
            switch (kind)
            {
                case 1: // shades
                    Prim(PrimitiveType.Cylinder, root, new Vector3(-.1f, 0, 0), new Vector3(.15f, .012f, .11f), black).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Prim(PrimitiveType.Cylinder, root, new Vector3(.1f, 0, 0), new Vector3(.15f, .012f, .11f), black).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Prim(PrimitiveType.Cube, root, new Vector3(0, .02f, 0), new Vector3(.36f, .025f, .02f), black);
                    break;
                case 2: // star glasses
                    for (int s = -1; s <= 1; s += 2)
                        for (int i = 0; i < 2; i++)
                            Prim(PrimitiveType.Cube, root, new Vector3(s * .11f, 0, 0), new Vector3(.12f, .12f, .02f), c).transform.localRotation = Quaternion.Euler(0, 0, i * 45f);
                    Prim(PrimitiveType.Cube, root, new Vector3(0, .02f, 0), new Vector3(.12f, .02f, .02f), c);
                    break;
                case 3: // mustache
                    root.localPosition = new Vector3(0, Height * .42f, Front);
                    Prim(PrimitiveType.Sphere, root, new Vector3(-.07f, 0, 0), new Vector3(.15f, .06f, .05f), black).transform.localRotation = Quaternion.Euler(0, 0, 12f);
                    Prim(PrimitiveType.Sphere, root, new Vector3(.07f, 0, 0), new Vector3(.15f, .06f, .05f), black).transform.localRotation = Quaternion.Euler(0, 0, -12f);
                    break;
            }
            return root;
        }

        public static Material InkMaterial;

        /// Primitive in a toon colour; with an ink material set it also gets an inverted-hull comic outline.
        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, bool keepCollider = false, bool noInk = false)
        {
            var g = GameObject.CreatePrimitive(t); g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            var col = g.GetComponent<Collider>();
            if (col && !keepCollider) Destroy(col);
            // a scaled CapsuleCollider is not a disc (a wide flat cylinder becomes a giant ball), so cylinders collide with their mesh
            else if (col && t == PrimitiveType.Cylinder) { DestroyImmediate(col); g.AddComponent<MeshCollider>().sharedMesh = g.GetComponent<MeshFilter>().sharedMesh; }
            var r = g.GetComponent<Renderer>();
            r.sharedMaterials = InkMaterial && !noInk ? new[] { Mat(c), InkMaterial } : new[] { Mat(c) };
            return g;
        }

        public static Material Mat(Color c)
        {
            if (mats.TryGetValue(c, out var m) && m) return m;
            m = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .2f);
            mats[c] = m; return m;
        }
        public static void SetBaseMaterial(Material m) { if (m) baseMat = m; }

        public static Mesh Cone()
        {
            if (cone) return cone;
            const int N = 20; var v = new List<Vector3> { new Vector3(0, 1, 0) }; var t = new List<int>();
            for (int i = 0; i <= N; i++) { float a = i * Mathf.PI * 2 / N; v.Add(new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f)); }
            for (int i = 1; i <= N; i++) { t.Add(0); t.Add(i + 1); t.Add(i); }
            int c = v.Count; v.Add(Vector3.zero);
            for (int i = 1; i <= N; i++) { t.Add(c); t.Add(i); t.Add(i + 1); }
            cone = new Mesh { name = "Party cone" }; cone.SetVertices(v); cone.SetTriangles(t, 0); cone.RecalculateNormals(); cone.RecalculateBounds();
            return cone;
        }

        // ------------------------------------------------------------ animation
        /// speed: planar m/s. dance: emote spin. airborne: stretched.
        public void Tick(float speed, bool dance, bool airborne)
        {
            bool w = speed > .25f && !airborne;
            if (Anim && Anim.runtimeAnimatorController)
            {
                if (w != walking)
                {
                    walking = w; string st = w ? "Walk" : "Idle";
                    if (Anim.HasState(0, Animator.StringToHash(st))) Anim.CrossFadeInFixedTime(st, .12f);
                }
                Anim.speed = w ? Mathf.Clamp(speed / 3.2f, .6f, 1.9f) : 1f;
            }
            if (!body) return;
            float t = Time.time;
            float bob = w ? Mathf.Abs(Mathf.Sin(t * speed * 3.1f + phase)) * .06f : 0f;
            float squash = airborne ? -.06f : w ? 0f : Mathf.Sin(t * 3.2f + phase) * .025f;
            body.localScale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f);
            body.localPosition = new Vector3(0, bob, 0);
            body.localRotation = dance ? Quaternion.Euler(0, t * 540f, Mathf.Sin(t * 12f) * 10f) : Quaternion.Euler(0, 0, w ? Mathf.Sin(t * speed * 3.1f + phase) * 4f : 0f);
            if (propeller) propeller.localRotation = Quaternion.Euler(0, t * (300f + speed * 400f), 0);
        }
    }
}
