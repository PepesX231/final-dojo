using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Look
{
    /// <summary>
    /// Procedural paper geometry for the pop-up storybook world (docs/VISUAL_RECONSTRUCTION.md §2):
    /// thick page slabs (floors) and standing pop-up panels (walls). Rounded corners, visible thickness,
    /// three submeshes: 0 = printed face (UV in metres / tile), 1 = paper edge (stacked pages), 2 = back.
    /// </summary>
    public static class PaperGeometry
    {
        /// <summary>Rounded-rectangle outline in XZ, counter-clockwise seen from above.</summary>
        static List<Vector2> RoundedRect(float w, float d, float r, int seg)
        {
            r = Mathf.Min(r, Mathf.Min(w, d) * 0.5f - 0.001f);
            var pts = new List<Vector2>();
            Vector2[] centers = { new Vector2(w / 2 - r, d / 2 - r), new Vector2(-w / 2 + r, d / 2 - r), new Vector2(-w / 2 + r, -d / 2 + r), new Vector2(w / 2 - r, -d / 2 + r) };
            for (int c = 0; c < 4; c++)
                for (int i = 0; i <= seg; i++)
                {
                    float a = (c * 90f + i * 90f / seg) * Mathf.Deg2Rad;
                    pts.Add(centers[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            return pts;
        }

        /// <summary>
        /// A thick page slab lying flat; top surface at y = 0, extends down by <paramref name="thickness"/>.
        /// Top UV = world metres / <paramref name="tile"/> (+ offset) so prints line up across slabs.
        /// </summary>
        public static Mesh Slab(float width, float depth, float thickness, float corner, float tile, Vector2 uvOffset = default, float edgeWobble = 0.03f, int seed = 0)
        {
            var outline = RoundedRect(width, depth, corner, 6);
            var rnd = new System.Random(seed);
            // slightly irregular hand-cut edge
            for (int i = 0; i < outline.Count; i++)
                outline[i] += outline[i].normalized * (float)(rnd.NextDouble() - 0.5) * edgeWobble;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>();
            var top = new List<int>(); var edge = new List<int>(); var bottom = new List<int>();
            int count = outline.Count;
            // top fan
            int ct = v.Count; v.Add(Vector3.zero); n.Add(Vector3.up); uv.Add(uvOffset / tile);
            for (int i = 0; i < count; i++) { var p = outline[i]; v.Add(new Vector3(p.x, 0, p.y)); n.Add(Vector3.up); uv.Add((p + uvOffset) / tile); }
            for (int i = 0; i < count; i++) { top.Add(ct); top.Add(ct + 1 + (i + 1) % count); top.Add(ct + 1 + i); }
            // bottom fan
            int cb = v.Count; v.Add(new Vector3(0, -thickness, 0)); n.Add(Vector3.down); uv.Add(Vector2.zero);
            for (int i = 0; i < count; i++) { var p = outline[i]; v.Add(new Vector3(p.x, -thickness, p.y)); n.Add(Vector3.down); uv.Add(p / tile); }
            for (int i = 0; i < count; i++) { bottom.Add(cb); bottom.Add(cb + 1 + i); bottom.Add(cb + 1 + (i + 1) % count); }
            // edge strip (u = perimeter metres, v = 0..1 across thickness → stacked-page stripes)
            float run = 0f;
            for (int i = 0; i <= count; i++)
            {
                var p = outline[i % count];
                if (i > 0) run += Vector2.Distance(outline[i - 1], p);
                Vector3 nn = new Vector3(p.x, 0, p.y).normalized;
                v.Add(new Vector3(p.x, 0, p.y)); n.Add(nn); uv.Add(new Vector2(run / 2f, 1));
                v.Add(new Vector3(p.x, -thickness, p.y)); n.Add(nn); uv.Add(new Vector2(run / 2f, 0));
            }
            int es = v.Count - (count + 1) * 2;
            for (int i = 0; i < count; i++)
            {
                int a = es + i * 2, b = es + (i + 1) * 2;
                edge.Add(a); edge.Add(b); edge.Add(a + 1);
                edge.Add(b); edge.Add(b + 1); edge.Add(a + 1);
            }
            var m = new Mesh { name = $"PaperSlab_{width:0.#}x{depth:0.#}" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            m.subMeshCount = 3;
            m.SetTriangles(top, 0); m.SetTriangles(edge, 1); m.SetTriangles(bottom, 2);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// A standing pop-up page: width along X, height along Y (from y=0), thickness along Z, bowed slightly,
        /// rounded top corners, a fold tab at the base. Front (−Z side faces the camera) carries the print:
        /// UV u = metres / tile, v = 0..1 over height.
        /// </summary>
        public static Mesh Panel(float width, float height, float thickness = 0.06f, float bow = 0.06f, float topCorner = 0.25f, float tile = 2f, float tab = 0.22f)
        {
            int cols = Mathf.Max(8, Mathf.CeilToInt(width / 0.25f));
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var front = new List<int>(); var edge = new List<int>(); var back = new List<int>();
            float Z(float x) => -bow * (1f - Mathf.Pow(2f * x / width, 2f)); // bows toward the camera in the middle
            float Top(float x)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x) - (width / 2f - topCorner));
                return height - (topCorner - Mathf.Sqrt(Mathf.Max(0f, topCorner * topCorner - dx * dx)));
            }
            // front & back faces as column strips
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                float zoff = side == 0 ? -thickness / 2f : thickness / 2f;
                for (int i = 0; i <= cols; i++)
                {
                    float x = -width / 2f + width * i / cols;
                    float h = Top(x);
                    v.Add(new Vector3(x, 0, Z(x) + zoff)); uv.Add(new Vector2((x + width / 2f) / tile, 0));
                    v.Add(new Vector3(x, h, Z(x) + zoff)); uv.Add(new Vector2((x + width / 2f) / tile, h / height));
                }
                var list = side == 0 ? front : back;
                for (int i = 0; i < cols; i++)
                {
                    int a = start + i * 2, b = start + (i + 1) * 2;
                    if (side == 0) { list.Add(a); list.Add(a + 1); list.Add(b); list.Add(b); list.Add(a + 1); list.Add(b + 1); }
                    else { list.Add(a); list.Add(b); list.Add(a + 1); list.Add(b); list.Add(b + 1); list.Add(a + 1); }
                }
            }
            // top edge strip + side edges
            int fs = 0, bs = (cols + 1) * 2;
            for (int i = 0; i < cols; i++)
            {
                int fa = fs + i * 2 + 1, fb = fs + (i + 1) * 2 + 1, ba = bs + i * 2 + 1, bb = bs + (i + 1) * 2 + 1;
                int o = v.Count;
                v.Add(v[fa]); v.Add(v[fb]); v.Add(v[ba]); v.Add(v[bb]);
                float u0 = (float)i / cols * width / 2f, u1 = (float)(i + 1) / cols * width / 2f;
                uv.Add(new Vector2(u0, 1)); uv.Add(new Vector2(u1, 1)); uv.Add(new Vector2(u0, 0)); uv.Add(new Vector2(u1, 0));
                edge.Add(o); edge.Add(o + 2); edge.Add(o + 1); edge.Add(o + 1); edge.Add(o + 2); edge.Add(o + 3);
            }
            for (int s = 0; s < 2; s++)
            {
                int i = s == 0 ? 0 : cols;
                int fa = fs + i * 2, ba = bs + i * 2;
                int o = v.Count;
                v.Add(v[fa]); v.Add(v[fa + 1]); v.Add(v[ba]); v.Add(v[ba + 1]);
                uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(height / 2f, 1)); uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(height / 2f, 0));
                if (s == 0) { edge.Add(o); edge.Add(o + 2); edge.Add(o + 1); edge.Add(o + 1); edge.Add(o + 2); edge.Add(o + 3); }
                else { edge.Add(o); edge.Add(o + 1); edge.Add(o + 2); edge.Add(o + 1); edge.Add(o + 3); edge.Add(o + 2); }
            }
            // fold tab lying on the floor in front (glued tab of a pop-up)
            if (tab > 0f)
            {
                int o = v.Count;
                float zf = -thickness / 2f;
                v.Add(new Vector3(-width / 2f + 0.05f, 0.012f, zf)); v.Add(new Vector3(width / 2f - 0.05f, 0.012f, zf));
                v.Add(new Vector3(-width / 2f + 0.12f, 0.012f, zf - tab)); v.Add(new Vector3(width / 2f - 0.12f, 0.012f, zf - tab));
                for (int k = 0; k < 4; k++) uv.Add(new Vector2(k % 2 == 0 ? 0 : width / 2f, k < 2 ? 1 : 0));
                edge.Add(o); edge.Add(o + 1); edge.Add(o + 2); edge.Add(o + 1); edge.Add(o + 3); edge.Add(o + 2);
            }
            var m = new Mesh { name = $"PaperPanel_{width:0.#}x{height:0.#}" };
            m.SetVertices(v); m.SetUVs(0, uv);
            m.subMeshCount = 3;
            m.SetTriangles(front, 0); m.SetTriangles(edge, 1); m.SetTriangles(back, 2);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
