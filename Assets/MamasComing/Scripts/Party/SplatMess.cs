using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Flat mess on the floor. Wiped away by mopping / squeegee / towel (hooked through PowderMess.CleanAt).
    public class SplatMess : MonoBehaviour, IMess
    {
        public static readonly List<SplatMess> AllSplats = new List<SplatMess>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { AllSplats.Clear(); }

        public SplatKind kind;
        public float amount = 1f;
        public bool sticky;        // soda: slows you down
        Vector3 baseScale;

        void OnEnable() { AllSplats.Add(this); MessRegistry.Add(this); }
        void OnDisable() { AllSplats.Remove(this); MessRegistry.Remove(this); }
        void Start() { baseScale = transform.localScale; if (baseScale == Vector3.zero) baseScale = Vector3.one; }

        public static SplatMess Spawn(SplatKind k, Vector3 at, float size, Material m)
        {
            at.y = 0.014f + AllSplats.Count * 0.0002f;
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = k + " splat";
            g.transform.position = at;
            g.transform.localScale = new Vector3(size * Random.Range(0.8f, 1.2f), 0.003f, size * Random.Range(0.8f, 1.2f));
            g.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var r = g.GetComponent<Renderer>(); if (m) { r.sharedMaterial = m; RememberMat(k, m); } r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var s = g.AddComponent<SplatMess>(); s.kind = k; s.sticky = k == SplatKind.Soda;
            return s;
        }

        static readonly Dictionary<SplatKind, Material> mats = new Dictionary<SplatKind, Material>();
        public static Material MatFor(SplatKind k)
        {
            if (mats.TryGetValue(k, out var m) && m) return m;
            foreach (var s in AllSplats) if (s && s.kind == k) { var r = s.GetComponent<Renderer>(); if (r && r.sharedMaterial) { mats[k] = r.sharedMaterial; return r.sharedMaterial; } }
            var nm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            nm.color = k == SplatKind.Frosting ? new Color(1f, 0.82f, 0.89f) : k == SplatKind.Foam ? new Color(0.96f, 0.98f, 1f) : k == SplatKind.Soda ? new Color(0.45f, 0.22f, 0.08f) : new Color(1f, 0.44f, 0.71f);
            mats[k] = nm; return nm;
        }
        public static void RememberMat(SplatKind k, Material m) { if (m) mats[k] = m; }

        public static int Count(SplatKind k) { int n = 0; foreach (var s in AllSplats) if (s && s.kind == k && s.amount > 0.1f) n++; return n; }

        public static void CleanAt(Vector3 p, float r, float dt)
        {
            for (int i = AllSplats.Count - 1; i >= 0; i--)
            {
                var x = AllSplats[i]; if (!x) continue;
                Vector3 d = x.transform.position - p; d.y = 0f;
                if (d.magnitude > r + 0.35f) continue;
                x.amount -= dt * (x.kind == SplatKind.Foam ? 2.2f : 1.3f);
                x.transform.localScale = new Vector3(x.baseScale.x * Mathf.Max(0.1f, x.amount), x.baseScale.y, x.baseScale.z * Mathf.Max(0.1f, x.amount));
                if (x.amount <= 0f) { MamaAudio.Play("pop", x.transform.position, 0.25f, 1.6f); Destroy(x.gameObject); }
            }
        }

        public static float StickyAt(Vector3 p)
        {
            foreach (var s in AllSplats) if (s && s.sticky) { Vector3 d = s.transform.position - p; d.y = 0; if (d.magnitude < s.transform.localScale.x * 0.5f) return 0.55f; }
            return 1f;
        }

        public MessKind Kind => MessKind.Evidence;
        public bool IsProblem => amount > 0.1f;
        public bool IsHidden => CoverObject.CoverAt(transform.position, 0f) != null;
        public int Severity => 1;
        public string MomLine => kind == SplatKind.Frosting ? (Loc.Thai ? "ทำไมมีครีมเค้กเต็มพื้น?!" : "Why is there CAKE FROSTING on the floor?!")
                               : kind == SplatKind.Confetti ? (Loc.Thai ? "กระดาษสีพวกนี้มาจากไหน?! มีปาร์ตี้เหรอ?!" : "Confetti?! Was there a PARTY in here?!")
                               : kind == SplatKind.Foam ? (Loc.Thai ? "ฟองสบู่อะไรเต็มห้องซักผ้า?!" : "Why is the laundry full of SOAP FOAM?!")
                               : (Loc.Thai ? "พื้นเหนียวหนึบ... น้ำอัดลมหกเหรอ?!" : "The floor is STICKY... is that soda?!");
        public string Label => kind.ToString();
        public Vector3 Position => transform.position;
    }
}
