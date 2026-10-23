using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>White extinguisher powder on the floor. Mop it away.</summary>
    public class PowderMess : MonoBehaviour, IMess
    {
        static readonly List<PowderMess> all = new List<PowderMess>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); }

        public float amount = 1f;
        Vector3 baseScale;

        void OnEnable() { all.Add(this); MessRegistry.Add(this); }
        void OnDisable() { all.Remove(this); MessRegistry.Remove(this); }
        void Start() { baseScale = transform.localScale; }

        public static void Spawn(Vector3 at, Material m)
        {
            if (all.Count > 60) return;
            at.y = HouseLayout.FloorYAt(at) + 0.012f;
            foreach (var x in all) { Vector3 d = x.transform.position - at; if (Mathf.Abs(d.y) > 1f) continue; d.y = 0f; if (d.magnitude < 0.35f) { x.amount = 1f; return; } }
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = "Powder";
            g.transform.position = at;
            g.transform.localScale = new Vector3(Random.Range(0.5f, 0.9f), 0.002f, Random.Range(0.5f, 0.9f));
            g.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var r = g.GetComponent<Renderer>();
            if (m) r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.AddComponent<PowderMess>();
        }

        public static void CleanAt(Vector3 p, float r, float dt)
        {
            SplatMess.CleanAt(p, r, dt);
            for (int i = all.Count - 1; i >= 0; i--)
            {
                var x = all[i]; if (!x) continue;
                Vector3 d = x.transform.position - p; d.y = 0f;
                if (d.magnitude > r + 0.3f) continue;
                x.amount -= dt * 1.4f;
                x.transform.localScale = x.baseScale * Mathf.Max(0.05f, x.amount);
                if (x.amount <= 0f) Destroy(x.gameObject);
            }
        }

        public MessKind Kind => MessKind.Evidence;
        public bool IsProblem => amount > 0.1f;
        public bool IsHidden => CoverObject.CoverAt(transform.position, 0f) != null;
        public int Severity => 1;
        public string MomLine => "What is all this white POWDER on the floor?!";
        public string Label => "Extinguisher powder";
        public Vector3 Position => transform.position;
    }
}
