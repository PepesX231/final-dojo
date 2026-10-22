using UnityEngine;

namespace DreamRush.OpenWorld
{
    /// <summary>
    /// Deterministic height field of DREAMLAND (640 × 640 m, centred on the origin). Shared by the editor builder
    /// (terrain mesh, scatter, NavMesh) and the runtime (grass placement), so they always agree.
    /// Regions: Starfall Meadow (centre), Crystal Cliffs plateau (north), Mirror Lake basin (east),
    /// Clockwork Ruins terraces (west), Pine Hills (south), mountain rim at the edge.
    /// </summary>
    public static class DreamlandTerrain
    {
        public const float Half = 320f;
        public const float WaterLevel = 0f;
        public static readonly Vector2 Meadow = new Vector2(0, 0), Cliffs = new Vector2(10, 205), Lake = new Vector2(205, 25), Ruins = new Vector2(-200, 10), Hills = new Vector2(0, -205);

        // paths from the meadow to each region (polyline points, xz)
        public static readonly Vector2[][] Paths =
        {
            new[] { new Vector2(0, -6), new Vector2(18, 40), new Vector2(6, 95), new Vector2(-14, 140) },
            new[] { new Vector2(6, 0), new Vector2(60, 12), new Vector2(115, 4), new Vector2(140, 22) },
            new[] { new Vector2(-6, 0), new Vector2(-62, -10), new Vector2(-118, 8), new Vector2(-150, 6) },
            new[] { new Vector2(0, -8), new Vector2(-20, -60), new Vector2(12, -112), new Vector2(0, -160) },
        };

        static float N(float x, float z, float s, float ox = 0, float oz = 0) => Mathf.PerlinNoise(x * s + 1000 + ox, z * s + 1000 + oz);

        static float Fbm(float x, float z, float s)
        {
            float a = 0.5f, sum = 0f;
            for (int i = 0; i < 4; i++) { sum += (N(x, z, s, i * 37.1f, i * 11.3f) - 0.5f) * a; s *= 2.03f; a *= 0.5f; }
            return sum;
        }

        static float Mask(Vector2 p, Vector2 c, float r0, float r1) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(r0, r1, Vector2.Distance(p, c)));

        public static float Height(float x, float z)
        {
            var p = new Vector2(x, z);
            float h = 2f + Fbm(x, z, 1f / 95f) * 10f;                          // rolling meadow
            // Pine Hills: big soft hills
            h += Mask(p, Hills, 40f, 150f) * (Fbm(x, z, 1f / 55f) * 26f + 6f);
            // Clockwork Ruins: terraces (stepped)
            float ruins = Mask(p, Ruins, 50f, 105f);
            float terrace = Mathf.Floor((8f + Fbm(x, z, 1f / 40f) * 6f) / 2.5f) * 2.5f;
            h = Mathf.Lerp(h, terrace, ruins * 0.85f);
            // Crystal Cliffs: plateau ~26 m with noisy, steep cliff edges
            float edge = 62f + Fbm(x, z, 1f / 28f) * 40f;
            float cliff = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - 6f, edge + 6f, Vector2.Distance(p, Cliffs)));
            h = Mathf.Lerp(h, 26f + Fbm(x, z, 1f / 30f) * 6f, cliff);
            // Mirror Lake basin
            float lake = Mask(p, Lake, 45f, 95f);
            h = Mathf.Lerp(h, -7f + Fbm(x, z, 1f / 30f) * 3f, lake);
            // meadow centre stays gentle and readable
            h = Mathf.Lerp(h, 2f + Fbm(x, z, 1f / 70f) * 3f, Mask(p, Meadow, 25f, 70f));
            // paths flatten their corridor a little
            float path = PathWeight(x, z);
            h = Mathf.Lerp(h, h * 0.85f + 0.3f, path * 0.4f);
            // mountain rim (world boundary)
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(255f, 315f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))));
            h += rim * (55f + Fbm(x, z, 1f / 40f) * 30f);
            return h;
        }

        public static Vector3 Normal(float x, float z)
        {
            const float e = 0.8f;
            float hx = Height(x + e, z) - Height(x - e, z);
            float hz = Height(x, z + e) - Height(x, z - e);
            return new Vector3(-hx, 2f * e, -hz).normalized;
        }

        public static float PathWeight(float x, float z)
        {
            var p = new Vector2(x, z);
            float best = 99f;
            foreach (var path in Paths)
                for (int i = 1; i < path.Length; i++)
                {
                    Vector2 a = path[i - 1], b = path[i], ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }
            float wob = (N(x, z, 0.15f) - 0.5f) * 1.2f;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.6f, 3.4f, best + wob));
        }

        public static Vector3 Ground(float x, float z) => new Vector3(x, Height(x, z), z);

        /// <summary>Grass density 0..1 at a spot (no grass on cliffs, paths, sand, lake, rim, ruins floor).</summary>
        public static float GrassDensity(float x, float z)
        {
            float h = Height(x, z);
            if (h < WaterLevel + 1.2f) return 0f;
            float slope = 1f - Normal(x, z).y;
            if (slope > 0.32f) return 0f;
            float d = 1f - PathWeight(x, z);
            var p = new Vector2(x, z);
            float ruins = Mask(p, Ruins, 45f, 90f);
            float rim = Mathf.InverseLerp(250f, 270f, Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)));
            float clump = Mathf.SmoothStep(0.2f, 0.65f, N(x, z, 1f / 14f));
            return d * (1f - ruins * 0.7f) * (1f - rim) * (0.35f + 0.65f * clump);
        }
    }
}
