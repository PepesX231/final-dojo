using System.Collections.Generic;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// A neon grind rail (Densha Attack / Jet Set energy, shared by World 1 and Midnight Palace). Land on it (or touch it
    /// while falling) → Pip snaps on and slides along it at ≥ <see cref="minSpeed"/>, gaining speed downhill; sparks,
    /// speed lines, a hum. Jump = hop off with a boost; at the end you are launched along the tangent.
    /// Points are world positions; the mesh is generated (a glowing tube on posts).
    /// </summary>
    public class GrindRail : MonoBehaviour
    {
        public static readonly List<GrindRail> All = new List<GrindRail>();
        public List<Vector3> points = new List<Vector3>();
        public float minSpeed = 13f, maxSpeed = 24f, snapRadius = 1.0f;
        public Color neon = new Color(0f, 0.9f, 1f);
        public float Length { get; private set; }
        readonly List<float> _cum = new List<float>();

        void OnEnable() { All.Add(this); Rebuild(); }
        void OnDisable() => All.Remove(this);

        public void Rebuild()
        {
            _cum.Clear(); Length = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) Length += Vector3.Distance(points[i - 1], points[i]);
                _cum.Add(Length);
            }
        }

        public Vector3 PointAt(float s, out Vector3 tangent)
        {
            s = Mathf.Clamp(s, 0f, Length);
            for (int i = 1; i < points.Count; i++)
            {
                if (s <= _cum[i] || i == points.Count - 1)
                {
                    float seg = Mathf.Max(0.0001f, _cum[i] - _cum[i - 1]);
                    float t = Mathf.Clamp01((s - _cum[i - 1]) / seg);
                    tangent = (points[i] - points[i - 1]).normalized;
                    return Vector3.Lerp(points[i - 1], points[i], t);
                }
            }
            tangent = Vector3.forward; return points.Count > 0 ? points[0] : transform.position;
        }

        /// <summary>Closest arc length to <paramref name="p"/> and the distance to it.</summary>
        public float Closest(Vector3 p, out float dist)
        {
            float best = float.MaxValue, bestS = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = points[i - 1], b = points[i], ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                float d = Vector3.Distance(p, a + ab * t);
                if (d < best) { best = d; bestS = _cum[i - 1] + t * ab.magnitude; }
            }
            dist = best; return bestS;
        }

        /// <summary>Generates the tube + posts (call from builders, edit mode or play).</summary>
        public void BuildMesh(Material railMat, Material postMat, float postEvery = 6f, float radius = 0.13f)
        {
            Rebuild();
            var mf = gameObject.GetComponent<MeshFilter>(); if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.GetComponent<MeshRenderer>(); if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            var v = new List<Vector3>(); var tri = new List<int>(); var nrm = new List<Vector3>();
            const int seg = 8;
            int rings = 0;
            for (float s = 0; s <= Length + 0.001f; s += 0.6f)
            {
                Vector3 p = PointAt(s, out var tan) - transform.position;
                Vector3 side = Vector3.Cross(Vector3.up, tan).normalized; if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                Vector3 up = Vector3.Cross(tan, side);
                for (int k = 0; k < seg; k++)
                {
                    float a = k / (float)seg * Mathf.PI * 2f;
                    Vector3 n = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    v.Add(p + n * radius); nrm.Add(n);
                }
                if (rings > 0)
                {
                    int b0 = (rings - 1) * seg, b1 = rings * seg;
                    for (int k = 0; k < seg; k++)
                    {
                        int k1 = (k + 1) % seg;
                        tri.AddRange(new[] { b0 + k, b1 + k, b1 + k1, b0 + k, b1 + k1, b0 + k1 });
                    }
                }
                rings++;
            }
            var m = new Mesh { name = "GrindRail" };
            m.SetVertices(v); m.SetNormals(nrm); m.SetTriangles(tri, 0); m.RecalculateBounds();
            mf.sharedMesh = m; mr.sharedMaterial = railMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (postMat == null) return;
            for (float s = 0; s <= Length; s += postEvery)
            {
                Vector3 p = PointAt(s, out _);
                if (!Physics.Raycast(p + Vector3.down * 0.3f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(post.GetComponent<Collider>());
                post.name = "Post"; post.transform.SetParent(transform, true);
                float h = p.y - hit.point.y;
                post.transform.position = (p + hit.point) * 0.5f; post.transform.localScale = new Vector3(0.12f, h * 0.5f, 0.12f);
                post.GetComponent<MeshRenderer>().sharedMaterial = postMat;
            }
        }
    }
}
