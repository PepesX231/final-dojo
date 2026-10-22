using UnityEngine;

namespace DreamRush.Core
{
    /// <summary>Tiny procedural meshes for runtime accessories (glasses rims etc.).</summary>
    public static class ProceduralMeshes
    {
        static Mesh _ring;

        /// <summary>Flat ring in the XY plane facing -Z, outer radius 0.5 (scale it), thickness 0.1 of radius.</summary>
        public static Mesh Ring()
        {
            if (_ring != null) return _ring;
            const int n = 28;
            float ro = 0.5f, ri = 0.38f, d = 0.04f;
            var v = new Vector3[n * 4];
            var t = new int[n * 6 * 4];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                v[i * 4 + 0] = dir * ro + Vector3.back * d;
                v[i * 4 + 1] = dir * ri + Vector3.back * d;
                v[i * 4 + 2] = dir * ro + Vector3.forward * d;
                v[i * 4 + 3] = dir * ri + Vector3.forward * d;
            }
            int k = 0;
            for (int i = 0; i < n; i++)
            {
                int a = i * 4, b = ((i + 1) % n) * 4;
                // front face (-Z)
                t[k++] = a; t[k++] = b + 1; t[k++] = b; t[k++] = a; t[k++] = a + 1; t[k++] = b + 1;
                // back face
                t[k++] = a + 2; t[k++] = b + 2; t[k++] = b + 3; t[k++] = a + 2; t[k++] = b + 3; t[k++] = a + 3;
                // outer rim
                t[k++] = a; t[k++] = b; t[k++] = b + 2; t[k++] = a; t[k++] = b + 2; t[k++] = a + 2;
                // inner rim
                t[k++] = a + 1; t[k++] = a + 3; t[k++] = b + 3; t[k++] = a + 1; t[k++] = b + 3; t[k++] = b + 1;
            }
            _ring = new Mesh { name = "Ring", vertices = v, triangles = t };
            _ring.RecalculateNormals();
            _ring.RecalculateBounds();
            return _ring;
        }
    }
}
