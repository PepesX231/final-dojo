using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// Lush instanced grass (FF field look): tufts generated per 32 m chunk from DreamlandTerrain (deterministic),
    /// built lazily when first needed and drawn with RenderMeshInstanced around the camera with frustum + distance culling. DreamGrass shader sways them.
    /// </summary>
        public class GrassField : MonoBehaviour
    {
        public Material material;
        public float halfSize = 260f;
        public float chunkSize = 32f;
        [Tooltip("Tufts per square metre at full density.")]
        public float density = 0.55f;
        public float drawDistance = 150f;
        public float fadeStart = 85f;
        Mesh _mesh;
        Matrix4x4[][] _matrices;   // lazily generated per chunk (DreamlandTerrain is deterministic)
        Bounds[] _bounds;
        int _n;
        readonly Plane[] _planes = new Plane[6];
        public int DrawnLastFrame { get; private set; }
        public int ChunksBuilt { get; private set; }

        void OnEnable()
        {
            _mesh = BuildTuftMesh();
            _n = Mathf.CeilToInt(halfSize * 2f / chunkSize);
            _matrices = new Matrix4x4[_n * _n][];
            _bounds = new Bounds[_n * _n];
            for (int cz = 0; cz < _n; cz++)
                for (int cx = 0; cx < _n; cx++)
                {
                    float x0 = -halfSize + cx * chunkSize, z0 = -halfSize + cz * chunkSize;
                    float h = DreamlandTerrain.Height(x0 + chunkSize / 2, z0 + chunkSize / 2);
                    _bounds[cz * _n + cx] = new Bounds(new Vector3(x0 + chunkSize / 2, h, z0 + chunkSize / 2), new Vector3(chunkSize, 60f, chunkSize));
                }
        }

        Matrix4x4[] BuildChunk(int index)
        {
            int cx = index % _n, cz = index / _n;
            float x0 = -halfSize + cx * chunkSize, z0 = -halfSize + cz * chunkSize;
            var rnd = new System.Random(index * 7919 + 13);
            var list = new List<Matrix4x4>();
            int tries = Mathf.RoundToInt(chunkSize * chunkSize * density);
            float ymin = float.MaxValue, ymax = float.MinValue;
            for (int i = 0; i < tries; i++)
            {
                float x = x0 + (float)rnd.NextDouble() * chunkSize, z = z0 + (float)rnd.NextDouble() * chunkSize;
                float d = DreamlandTerrain.GrassDensity(x, z);
                if ((float)rnd.NextDouble() > d) continue;
                float y = DreamlandTerrain.Height(x, z);
                float s = 0.8f + (float)rnd.NextDouble() * 0.9f * d;
                list.Add(Matrix4x4.TRS(new Vector3(x, y - 0.05f, z), Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0), new Vector3(s, s * (0.8f + (float)rnd.NextDouble() * 0.5f), s)));
                ymin = Mathf.Min(ymin, y); ymax = Mathf.Max(ymax, y);
            }
            if (list.Count > 0) _bounds[index] = new Bounds(new Vector3(x0 + chunkSize / 2, (ymin + ymax) / 2, z0 + chunkSize / 2), new Vector3(chunkSize, ymax - ymin + 3f, chunkSize));
            ChunksBuilt++;
            return list.ToArray();
        }

        void Update()
        {
            if (material == null || _mesh == null || _matrices == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            Vector3 cp = cam.transform.position;
            var rp = new RenderParams(material) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, layer = gameObject.layer };
            int drawn = 0;
            for (int c = 0; c < _bounds.Length; c++)
            {
                var b = _bounds[c];
                if (b.SqrDistance(cp) > drawDistance * drawDistance) continue;
                if (!GeometryUtility.TestPlanesAABB(_planes, b)) continue;
                if (_matrices[c] == null) _matrices[c] = BuildChunk(c);
                var m = _matrices[c];
                if (m.Length == 0) continue;
                // thin far chunks: draw a prefix (tufts are shuffled when baked)
                float d = Mathf.Sqrt(b.SqrDistance(cp));
                int count = d < fadeStart ? m.Length : Mathf.Max(1, (int)(m.Length * Mathf.Lerp(1f, 0.12f, Mathf.Sqrt((d - fadeStart) / (drawDistance - fadeStart)))));
                for (int start = 0; start < count; start += 1023)
                    Graphics.RenderMeshInstanced(rp, _mesh, 0, m, Mathf.Min(1023, count - start), start);
                drawn += count;
            }
            DrawnLastFrame = drawn;
        }

        /// <summary>A tuft of 7 curved, tapered blades (uv.y = height 0..1 for wind and colour).</summary>
        public static Mesh BuildTuftMesh()
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            var rnd = new System.Random(3);
            for (int b = 0; b < 7; b++)
            {
                float a = b / 7f * Mathf.PI * 2f + (float)rnd.NextDouble();
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 side = new Vector3(-dir.z, 0, dir.x);
                Vector3 root = dir * (float)rnd.NextDouble() * 0.12f;
                float h = 0.45f + (float)rnd.NextDouble() * 0.35f, w = 0.07f, bend = 0.12f + (float)rnd.NextDouble() * 0.18f;
                int baseIdx = v.Count;
                const int seg = 3;
                for (int s = 0; s <= seg; s++)
                {
                    float k = s / (float)seg;
                    Vector3 c = root + Vector3.up * h * k + dir * bend * k * k;
                    float ww = w * (1f - k * 0.92f);
                    v.Add(c - side * ww); v.Add(c + side * ww);
                    uv.Add(new Vector2(0, k)); uv.Add(new Vector2(1, k));
                }
                for (int s = 0; s < seg; s++)
                {
                    int i0 = baseIdx + s * 2;
                    tri.AddRange(new[] { i0, i0 + 2, i0 + 1, i0 + 1, i0 + 2, i0 + 3 });
                }
            }
            var m = new Mesh { name = "GrassTuft" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            m.bounds = new Bounds(new Vector3(0, 0.5f, 0), new Vector3(2, 1.5f, 2));
            return m;
        }
    }
}
