using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// 2D height-field water simulation over the whole house floor.
    /// Water spreads cell to cell toward lower water levels, is blocked by walls, spills over door
    /// thresholds once deep enough, and keeps a thin surface-tension film (so it forms real puddle edges
    /// instead of spreading forever). Rendered as one transparent plane driven by a depth texture.
    /// Units: depth in meters; 1 L over one 0.2 m cell = 25 mm.
    /// </summary>
    public class FloodSim : MonoBehaviour
    {
        public static FloodSim I { get; private set; }

        [Header("Grid")]
        public Vector2 origin = new Vector2(-0.5f, -0.5f);
        public float cell = 0.2f;
        public int width = 116;
        public int height = 76;

        [Header("Blocking (x0,z0,x1,z1 rects, world)")]
        public List<Vector4> walls = new List<Vector4>();
        [Tooltip("x0,z0,x1,z1 rects and w = ground offset in meters (thresholds > 0, sunken floors < 0)")]
        public List<Vector4> groundRects = new List<Vector4>();
        public List<float> groundOffsets = new List<float>();

        [Header("Physics")]
        public float flowRate = 0.22f;
        [Tooltip("Surface tension film: water thinner than this does not keep spreading.")]
        public float film = 0.0011f;
        public float evaporation = 0.000002f;
        public int substeps = 2;

        [Header("Render")]
        public Renderer surface;
        public Color shallow = new Color(0.12f, 0.6f, 0.7f, 0.0f);
        public Color deep = new Color(0.03f, 0.34f, 0.48f, 0.82f);
        public ParticleSystem splashFx;

        float[] depth, ground, next;
        bool[] blocked;
        float cellArea;
        Texture2D tex;
        Color32[] px;
        float simAcc, texAcc;
        Texture2D ripple;
        Material mat;
        readonly List<Drain> drains = new List<Drain>();

        public float CellArea => cellArea;
        public float TotalLiters { get; private set; }
        public float WetArea { get; private set; }

        void Awake()
        {
            I = this;
            cellArea = cell * cell;
            int n = width * height;
            depth = new float[n]; next = new float[n]; ground = new float[n]; blocked = new bool[n];
            foreach (var w in walls) RasterRect(w, (i) => blocked[i] = true);
            for (int r = 0; r < groundRects.Count && r < groundOffsets.Count; r++)
            {
                float off = groundOffsets[r];
                RasterRect(groundRects[r], (i) => ground[i] = off);
            }
            tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            px = new Color32[n];
            if (surface)
            {
                var m = surface.material;
                m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.93f);
                m.SetFloat("_EnvironmentReflections", 1f); m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                ripple = MakeRipple(128);
                m.SetTexture("_BumpMap", ripple); m.SetFloat("_BumpScale", 0.9f); m.EnableKeyword("_NORMALMAP");
                m.SetTextureScale("_BumpMap", new Vector2(width * cell / 1.6f, height * cell / 1.6f));
                var caus = MakeCaustics(128);
                m.SetTexture("_DetailAlbedoMap", caus); m.SetFloat("_DetailAlbedoMapScale", 1f); m.EnableKeyword("_DETAIL_MULX2");
                m.SetTextureScale("_DetailAlbedoMap", new Vector2(width * cell / 2.2f, height * cell / 2.2f));
                mat = m;
            }
        }

        static Texture2D MakeCaustics(int n)
        {
            var rnd = new System.Random(3); int k = 5; var pts = new Vector2[k * k];
            for (int j = 0; j < k; j++) for (int i = 0; i < k; i++) pts[j * k + i] = new Vector2((i + (float)rnd.NextDouble()) / k, (j + (float)rnd.NextDouble()) / k);
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    Vector2 u = new Vector2((float)x / n, (float)y / n); float f1 = 9, f2 = 9;
                    foreach (var q in pts) for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
                            { float d = (u - (q + new Vector2(ox, oy))).magnitude; if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d; }
                    float e = Mathf.Clamp01(1f - (f2 - f1) * k * 1.6f); e = e * e;
                    float v = 0.5f + e * 0.22f;
                    px[y * n + x] = new Color(v * 0.96f, v, v, 1f);
                }
            t.SetPixels(px); t.Apply(true); return t;
        }

        void OnDestroy() { if (I == this) I = null; }

        public void Register(Drain d) { if (!drains.Contains(d)) drains.Add(d); }

        void RasterRect(Vector4 r, System.Action<int> act)
        {
            int x0 = Mathf.FloorToInt((Mathf.Min(r.x, r.z) - origin.x) / cell);
            int x1 = Mathf.CeilToInt((Mathf.Max(r.x, r.z) - origin.x) / cell) - 1;
            int z0 = Mathf.FloorToInt((Mathf.Min(r.y, r.w) - origin.y) / cell);
            int z1 = Mathf.CeilToInt((Mathf.Max(r.y, r.w) - origin.y) / cell) - 1;
            for (int z = Mathf.Max(0, z0); z <= Mathf.Min(height - 1, z1); z++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++)
                    act(z * width + x);
        }

        public bool ToCell(Vector3 p, out int x, out int z)
        {
            x = Mathf.FloorToInt((p.x - origin.x) / cell);
            z = Mathf.FloorToInt((p.z - origin.y) / cell);
            return x >= 0 && z >= 0 && x < width && z < height;
        }

        public Vector3 CellCenter(int x, int z) => new Vector3(origin.x + (x + 0.5f) * cell, 0f, origin.y + (z + 0.5f) * cell);

        public float DepthAt(Vector3 p)
        {
            if (!ToCell(p, out int x, out int z)) return 0f;
            return depth[z * width + x];
        }

        /// <summary>Max depth in a radius (for slipping checks with feet spread).</summary>
        public float DepthAround(Vector3 p, float r)
        {
            float m = 0f;
            ForCells(p, r, i => { if (depth[i] > m) m = depth[i]; });
            return m;
        }

        void ForCells(Vector3 p, float r, System.Action<int> act)
        {
            int cx0 = Mathf.FloorToInt((p.x - r - origin.x) / cell), cx1 = Mathf.FloorToInt((p.x + r - origin.x) / cell);
            int cz0 = Mathf.FloorToInt((p.z - r - origin.y) / cell), cz1 = Mathf.FloorToInt((p.z + r - origin.y) / cell);
            float r2 = r * r;
            for (int z = Mathf.Max(0, cz0); z <= Mathf.Min(height - 1, cz1); z++)
                for (int x = Mathf.Max(0, cx0); x <= Mathf.Min(width - 1, cx1); x++)
                {
                    float dx = origin.x + (x + 0.5f) * cell - p.x, dz = origin.y + (z + 0.5f) * cell - p.z;
                    if (dx * dx + dz * dz > r2) continue;
                    int i = z * width + x;
                    if (!blocked[i]) act(i);
                }
        }

        /// <summary>Pour liters of water at a point (spread over a small radius).</summary>
        public void AddLiters(Vector3 p, float liters, float radius = 0.15f)
        {
            if (liters <= 0f) return;
            var cells = new List<int>(8);
            ForCells(p, Mathf.Max(radius, cell * 0.6f), i => cells.Add(i));
            if (cells.Count == 0) return;
            float d = liters * 0.001f / cellArea / cells.Count;
            foreach (var i in cells) depth[i] += d;
        }

        /// <summary>Remove up to liters within radius; returns liters actually removed.</summary>
        public float RemoveLiters(Vector3 p, float radius, float liters)
        {
            if (liters <= 0f) return 0f;
            float avail = 0f;
            ForCells(p, radius, i => avail += depth[i]);
            if (avail <= 1e-7f) return 0f;
            float wantDepth = liters * 0.001f / cellArea;
            float k = Mathf.Min(1f, wantDepth / avail);
            float got = 0f;
            ForCells(p, radius, i => { float t = depth[i] * k; depth[i] -= t; got += t; });
            return got * cellArea * 1000f;
        }

        /// <summary>Shove water from a disc toward a direction (squeegee / running mop).</summary>
        public float Push(Vector3 p, Vector3 dir, float radius, float liters, float distance)
        {
            float got = RemoveLiters(p, radius, liters);
            if (got > 0f)
            {
                dir.y = 0f;
                Vector3 to = p + dir.normalized * distance;
                if (ToCell(to, out int x, out int z) && !blocked[z * width + x]) AddLiters(to, got, radius * 0.8f);
                else AddLiters(p, got, radius);
            }
            return got;
        }

        public void Splash(Vector3 p, float strength)
        {
            if (!splashFx || DepthAt(p) < 0.0009f) return;
            var ep = new ParticleSystem.EmitParams { position = p + Vector3.up * 0.03f, applyShapeToPosition = true };
            splashFx.Emit(ep, Mathf.RoundToInt(6 * strength));
        }

        /// <summary>Liters of visible water (not hidden by rugs/towels) within radius.</summary>
        public float VisibleLitersIn(Vector3 c, float radius, out float wetArea)
        {
            float l = 0f, a = 0f;
            ForCells(c, radius, i =>
            {
                if (depth[i] < 0.0010f) return;
                Vector3 cc = CellCenter(i % width, i / width);
                if (CoverObject.CoverAt(cc, 0f) != null) return;
                l += depth[i]; a += 1f;
            });
            wetArea = a * cellArea;
            return l * cellArea * 1000f;
        }

        void Update()
        {
            simAcc += Time.deltaTime;
            float step = 1f / 30f;
            int guard = 0;
            while (simAcc >= step && guard++ < 3)
            {
                simAcc -= step;
                if (NetGame.IsAuthority) for (int s = 0; s < substeps; s++) Step(step / substeps);
            }
            if (mat) { mat.SetTextureOffset("_BumpMap", new Vector2(Time.time * 0.03f, Time.time * 0.021f)); mat.SetTextureOffset("_DetailAlbedoMap", new Vector2(Mathf.Sin(Time.time * 0.31f) * 0.08f + Time.time * 0.012f, Mathf.Cos(Time.time * 0.27f) * 0.08f)); }
            texAcc += Time.deltaTime;
            if (texAcc > 0.05f) { texAcc = 0f; Render(); }
        }

        void Step(float dt)
        {
            int W = width, H = height;
            System.Array.Copy(depth, next, depth.Length);
            float k = flowRate;
            for (int z = 0; z < H; z++)
            {
                int row = z * W;
                for (int x = 0; x < W; x++)
                {
                    int i = row + x;
                    if (blocked[i]) continue;
                    // right & up neighbours, flux both directions
                    if (x + 1 < W) Exchange(i, i + 1, k);
                    if (z + 1 < H) Exchange(i, i + W, k);
                }
            }
            var t = depth; depth = next; next = t;

            // Drains & absorption
            foreach (var d in drains)
            {
                if (!d) continue;
                float rate = d.CurrentRate;
                if (rate <= 0f) continue;
                d.drained += RemoveLiters(d.transform.position, d.radius, rate * dt);
            }
            foreach (var c in CoverObject.All)
            {
                if (!c || c.absorbRate <= 0f || c.Saturated || !c.OnFloor) continue;
                float got = RemoveLiters(c.transform.position, Mathf.Max(c.halfSize.x, c.halfSize.y), c.absorbRate * dt);
                c.Absorb(got);
            }
            if (evaporation > 0f)
                for (int i = 0; i < depth.Length; i++) if (depth[i] > 0f) depth[i] = Mathf.Max(0f, depth[i] - evaporation * dt);
        }

        void Exchange(int a, int b, float k)
        {
            if (blocked[b]) return;
            float da = depth[a], db = depth[b];
            if (da <= 0f && db <= 0f) return;
            float ha = ground[a] + da, hb = ground[b] + db;
            float diff = ha - hb;
            if (diff > 0f)
            {
                // surface tension only holds back the leading edge (spreading onto dry floor)
                float avail = da - (db < film ? film : 0f); if (avail <= 0f) return;
                float f = Mathf.Min(diff * k, avail * 0.24f);
                next[a] -= f; next[b] += f;
            }
            else if (diff < 0f)
            {
                float avail = db - (da < film ? film : 0f); if (avail <= 0f) return;
                float f = Mathf.Min(-diff * k, avail * 0.24f);
                next[b] -= f; next[a] += f;
            }
        }

        void Render()
        {
            float total = 0f; int wet = 0;
            for (int i = 0; i < depth.Length; i++)
            {
                float d = depth[i];
                total += d;
                if (d < 0.0008f) { px[i] = new Color32(0, 0, 0, 0); continue; }
                wet++;
                float t = Mathf.InverseLerp(0.0008f, 0.007f, d);
                Color c = Color.Lerp(shallow, deep, Mathf.Sqrt(t));
                c.a = Mathf.Lerp(0.58f, deep.a, t);
                float sp = Mathf.PerlinNoise(i % width * 0.35f + Time.time * 0.6f, i / width * 0.35f - Time.time * 0.4f);
                c = Color.Lerp(c, new Color(0.7f, 0.88f, 1f), Mathf.Clamp01(sp - 0.62f) * 0.9f);
                // bright meniscus along the puddle edge so it reads as water
                int x = i % width, z = i / width;
                bool edge = (x > 0 && depth[i - 1] < 0.0008f && !blocked[i - 1]) || (x < width - 1 && depth[i + 1] < 0.0008f && !blocked[i + 1]) ||
                            (z > 0 && depth[i - width] < 0.0008f && !blocked[i - width]) || (z < height - 1 && depth[i + width] < 0.0008f && !blocked[i + width]);
                if (edge) { c = Color.Lerp(c, new Color(0.88f, 0.97f, 1f), 0.7f); c.a = 0.9f; }
                px[i] = c;
            }
            TotalLiters = total * cellArea * 1000f;
            WetArea = wet * cellArea;
            tex.SetPixels32(px);
            tex.Apply(false);
        }

        static Texture2D MakeRipple(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, true, true);
            t.wrapMode = TextureWrapMode.Repeat;
            var h = new float[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float u = (float)x / n * Mathf.PI * 2f, v = (float)y / n * Mathf.PI * 2f;
                    h[y * n + x] = Mathf.Sin(u * 3f + Mathf.Sin(v * 2f)) * 0.5f + Mathf.Sin(v * 4f + Mathf.Cos(u * 3f) * 1.5f) * 0.35f + Mathf.Sin((u + v) * 5f) * 0.15f;
                }
            var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float dx = h[y * n + (x + 1) % n] - h[y * n + (x - 1 + n) % n];
                    float dy = h[((y + 1) % n) * n + x] - h[((y - 1 + n) % n) * n + x];
                    Vector3 nn = new Vector3(-dx, -dy, 1.2f).normalized;
                    px[y * n + x] = new Color(nn.x * 0.5f + 0.5f, nn.y * 0.5f + 0.5f, nn.z * 0.5f + 0.5f, 1f);
                }
            t.SetPixels(px); t.Apply(true);
            return t;
        }

        /// <summary>Closest visible (not covered) puddle cell deeper than minDepth.</summary>
        public float VisibleLitersInRect(Rect r)
        {
            float l = 0f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt((r.xMin - origin.x) / cell)), x1 = Mathf.Min(width - 1, Mathf.FloorToInt((r.xMax - origin.x) / cell));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((r.yMin - origin.y) / cell)), z1 = Mathf.Min(height - 1, Mathf.FloorToInt((r.yMax - origin.y) / cell));
            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    int i = z * width + x; if (depth[i] < 0.0010f) continue;
                    if (CoverObject.CoverAt(CellCenter(x, z), 0f) != null) continue;
                    l += depth[i];
                }
            return l * cellArea * 1000f;
        }

        // ---- network: depth field quantized to bytes (0..2.5 cm)
        public byte[] PackDepth()
        {
            var b = new byte[depth.Length];
            for (int i = 0; i < depth.Length; i++) b[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(depth[i] / 0.025f * 255f), 0, 255);
            return b;
        }
        public void UnpackDepth(byte[] b)
        {
            if (b == null || b.Length != depth.Length) return;
            for (int i = 0; i < depth.Length; i++) depth[i] = b[i] * (0.025f / 255f);
        }

        public bool NearestWet(Vector3 from, float minDepth, bool visibleOnly, out Vector3 pos)
        {
            pos = from; float best = float.MaxValue; bool found = false;
            for (int z = 0; z < height; z += 2)
                for (int x = 0; x < width; x += 2)
                {
                    int i = z * width + x;
                    if (depth[i] < minDepth) continue;
                    Vector3 c = CellCenter(x, z);
                    float d = (c.x - from.x) * (c.x - from.x) + (c.z - from.z) * (c.z - from.z);
                    if (d >= best) continue;
                    if (visibleOnly && CoverObject.CoverAt(c, 0f) != null) continue;
                    best = d; pos = c; found = true;
                }
            return found;
        }

        /// <summary>Pre-flood an area (used to start the game mid-disaster).</summary>
        public void Seed(Vector3 p, float radius, float depthM)
        {
            ForCells(p, radius, i => depth[i] = Mathf.Max(depth[i], depthM));
        }
    }
}
