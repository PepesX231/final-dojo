using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Coarse 2D smoke field through the house. Smoke is produced by fires, diffuses room to room
    /// through doorways (walls block it), banks down from the ceiling as it thickens, is sucked out of
    /// open windows/doors and pushed by fans. Drawn with a particle system sampled from the field.
    /// </summary>
    public class SmokeSim : MonoBehaviour, IMess
    {
        public static SmokeSim I { get; private set; }

        public Vector2 origin = new Vector2(-0.5f, -0.5f);
        public float cell = 0.5f;
        public int width = 47;
        public int height = 31;
        public List<Vector4> walls = new List<Vector4>();

        public float diffusion = 0.14f;
        public float decay = 0.004f;
        public ParticleSystem smokeFx;
        public float ceiling = 2.8f;

        float[] c, n;
        bool[] blocked;
        float simAcc, fxAcc;
        readonly List<SmokeVent> vents = new List<SmokeVent>();
        Fan[] fans;

        public float Total { get; private set; }
        public float Peak { get; private set; }

        void Awake()
        {
            I = this;
            c = new float[width * height]; n = new float[width * height]; blocked = new bool[width * height];
            foreach (var w in walls)
            {
                int x0 = Mathf.FloorToInt((Mathf.Min(w.x, w.z) - origin.x) / cell), x1 = Mathf.CeilToInt((Mathf.Max(w.x, w.z) - origin.x) / cell) - 1;
                int z0 = Mathf.FloorToInt((Mathf.Min(w.y, w.w) - origin.y) / cell), z1 = Mathf.CeilToInt((Mathf.Max(w.y, w.w) - origin.y) / cell) - 1;
                // only block cells mostly covered (thin walls block the cell they cross)
                for (int z = Mathf.Max(0, z0); z <= Mathf.Min(height - 1, z1); z++)
                    for (int x = Mathf.Max(0, x0); x <= Mathf.Min(width - 1, x1); x++)
                        blocked[z * width + x] = true;
            }
        }

        void OnDestroy() { if (I == this) I = null; }
        void OnEnable() { MessRegistry.Add(this); }
        void OnDisable() { MessRegistry.Remove(this); }

        void Start() { fans = FindObjectsByType<Fan>(FindObjectsSortMode.None); }

        public void Register(SmokeVent v) { if (!vents.Contains(v)) vents.Add(v); }

        bool Cell(Vector3 p, out int i)
        {
            int x = Mathf.FloorToInt((p.x - origin.x) / cell), z = Mathf.FloorToInt((p.z - origin.y) / cell);
            i = z * width + x;
            return x >= 0 && z >= 0 && x < width && z < height;
        }

        public byte[] Pack() { var b = new byte[c.Length]; for (int i = 0; i < c.Length; i++) b[i] = (byte)Mathf.Clamp(Mathf.RoundToInt(c[i] / 3f * 255f), 0, 255); return b; }
        public void Unpack(byte[] b) { if (b == null || b.Length != c.Length) return; float tot = 0f, pk = 0f; for (int i = 0; i < c.Length; i++) { c[i] = b[i] * (3f / 255f); tot += c[i]; if (c[i] > pk) pk = c[i]; } Total = tot; Peak = pk; }

        public float At(Vector3 p)
        {
            if (!Cell(p, out int i)) return 0f;
            if (!blocked[i]) return c[i];
            // inside a wall cell: average of neighbours
            float s = 0f; int k = 0;
            int x = i % width, z = i / width;
            for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            {
                int xx = x + dx, zz = z + dz; if (xx < 0 || zz < 0 || xx >= width || zz >= height) continue;
                int j = zz * width + xx; if (blocked[j]) continue; s += c[j]; k++;
            }
            return k > 0 ? s / k : 0f;
        }

        public void Add(Vector3 p, float amount)
        {
            if (!Cell(p, out int i)) return;
            if (blocked[i]) return;
            c[i] += amount;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool sim = GameManager.I == null || GameManager.I.SimRunning;
            simAcc += dt;
            while (sim && NetGame.IsAuthority && simAcc >= 0.1f) { simAcc -= 0.1f; Step(0.05f); Step(0.05f); }
            fxAcc += dt;
            if (fxAcc >= 0.08f) { EmitFx(fxAcc); fxAcc = 0f; }
        }

        void Step(float dt)
        {
            int W = width, H = height;
            float k = diffusion;
            float total = 0f, peak = 0f;
            for (int z = 0; z < H; z++)
                for (int x = 0; x < W; x++)
                {
                    int i = z * W + x;
                    if (blocked[i]) { n[i] = 0f; continue; }
                    float ci = c[i], acc = 0f;
                    if (x > 0 && !blocked[i - 1]) acc += c[i - 1] - ci;
                    if (x < W - 1 && !blocked[i + 1]) acc += c[i + 1] - ci;
                    if (z > 0 && !blocked[i - W]) acc += c[i - W] - ci;
                    if (z < H - 1 && !blocked[i + W]) acc += c[i + W] - ci;
                    float v = ci + acc * k;
                    v -= v * decay * dt;
                    n[i] = v > 0f ? v : 0f;
                    total += n[i]; if (n[i] > peak) peak = n[i];
                }
            var t = c; c = n; n = t;
            Total = total; Peak = peak;

            foreach (var v in vents)
            {
                if (!v || !v.IsOpen) continue;
                Vent(v.transform.position, v.radius, v.rate * dt);
            }
            if (fans != null)
                foreach (var f in fans) if (f && f.on && PowerGrid.On) Blow(f.HeadPos, f.HeadForward, f.range, dt);
        }

        void Vent(Vector3 p, float r, float frac)
        {
            int cx = Mathf.FloorToInt((p.x - origin.x) / cell), cz = Mathf.FloorToInt((p.z - origin.y) / cell);
            int rr = Mathf.CeilToInt(r / cell);
            for (int z = cz - rr; z <= cz + rr; z++) for (int x = cx - rr; x <= cx + rr; x++)
            {
                if (x < 0 || z < 0 || x >= width || z >= height) continue;
                int i = z * width + x; if (blocked[i]) continue;
                c[i] *= 1f - Mathf.Clamp01(frac);
            }
        }

        void Blow(Vector3 o, Vector3 f, float range, float dt)
        {
            f.y = 0f; f.Normalize();
            int steps = Mathf.CeilToInt(range / cell);
            for (int s = steps; s >= 0; s--)
                for (int side = -2; side <= 2; side++)
                {
                    Vector3 p = o + f * (s * cell) + Vector3.Cross(Vector3.up, f) * (side * cell * (0.3f + s * 0.12f));
                    if (!Cell(p, out int i) || blocked[i]) continue;
                    if (!Cell(p + f * cell, out int j) || blocked[j]) continue;
                    float m = c[i] * Mathf.Clamp01(1.4f * dt);
                    c[i] -= m; c[j] += m * 0.9f;
                }
        }

        void EmitFx(float dt)
        {
            if (!smokeFx) return;
            // Keep a visible ceiling layer without stacking hundreds of opaque cards over tools.
            int budget = Mathf.Min(48, 480 - smokeFx.particleCount);
            if (budget <= 0) return;
            for (int i = 0; i < c.Length && budget > 0; i++)
            {
                float v = c[i];
                if (v < 0.035f) continue;
                float expected = v * dt * 5f;
                int count = (int)expected + (Random.value < expected - (int)expected ? 1 : 0);
                for (int k = 0; k < count && budget > 0; k++, budget--)
                {
                    float x = origin.x + (i % width + Random.value) * cell, z = origin.y + (i / width + Random.value) * cell;
                    float bank = Mathf.Lerp(ceiling - 0.4f, 0.7f, Mathf.Clamp01(v / 2.2f)); // thick smoke banks down
                    float y = Random.Range(bank, ceiling);
                    float a = Mathf.Clamp(0.035f + v * 0.065f, 0.035f, 0.22f);
                    Vector3 vel = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.02f, 0.08f), Random.Range(-0.15f, 0.15f));
                    // smoke visibly drifts toward any open window / door nearby
                    foreach (var vent in vents)
                    {
                        if (!vent || !vent.IsOpen) continue;
                        Vector3 to = vent.transform.position - new Vector3(x, y, z); to.y = 0f;
                        float dist = to.magnitude;
                        if (dist < 5f && dist > 0.01f) vel += to / dist * Mathf.Lerp(0.9f, 0.2f, dist / 5f);
                    }
                    var ep = new ParticleSystem.EmitParams
                    {
                        position = new Vector3(x, y, z),
                        velocity = vel,
                        startSize = Random.Range(0.7f, 1.25f),
                        startLifetime = Random.Range(1.8f, 2.8f),
                        startColor = new Color(0.32f, 0.3f, 0.29f, a),
                        rotation = Random.Range(0f, 360f)
                    };
                    smokeFx.Emit(ep, 1);
                }
            }
        }

        // IMess: lingering smoke / burnt smell
        public MessKind Kind => MessKind.Smoke;
        public bool IsProblem => Peak > 0.25f;
        public bool IsHidden => false;
        public int Severity => Peak > 1.2f ? 3 : (Peak > 0.6f ? 2 : 1);
        public string MomLine => Peak > 1.2f ? "WHY IS MY HOUSE FULL OF SMOKE?!" : (Peak > 0.6f ? "It's so smoky in here!" : "Hmm... why does it smell burnt?");
        public string Label => "Smoke";
        public Vector3 Position => transform.position;
    }
}
