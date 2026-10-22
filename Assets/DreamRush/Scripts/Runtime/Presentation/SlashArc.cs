using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// M15 skill VFX: an anime crescent slash — a ring-sector mesh that sweeps open along the swing (0 → arc in the first
    /// 35% of its life), then thins and fades from the tail. Additive, warm core, bright leading edge; pooled.
    /// Reference (user, 14:40): golden HD-2D light, embers, flare. Presentation only.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SlashArc : MonoBehaviour
    {
        const int Segs = 28;
        static readonly Stack<SlashArc> Pool = new Stack<SlashArc>();
        static Material _mat;
        public static int Spawned { get; private set; }

        Mesh _mesh; Vector3[] _v; Color[] _c; float _t, _life, _radius, _width, _arc; Color _col;

        /// <summary>
        /// <paramref name="rot"/>: forward = the middle of the arc, up = the arc plane's normal (roll it for diagonal cuts).
        /// </summary>
        public static SlashArc Spawn(Vector3 center, Quaternion rot, float radius, float arcDeg, Color color, float width = 0.55f, float life = 0.26f)
        {
            var lib = VFXLibrary.Instance;
            SlashArc s = null;
            while (Pool.Count > 0 && s == null) s = Pool.Pop();
            if (s == null)
            {
                var go = new GameObject("FX_SlashArc"); Object.DontDestroyOnLoad(go);
                s = go.AddComponent<SlashArc>();
                if (_mat == null && lib != null && lib.streakAdd != null) { _mat = new Material(lib.streakAdd) { name = "M_VFX_SlashArc" }; _mat.SetTexture("_BaseMap", Texture2D.whiteTexture); _mat.SetColor("_BaseColor", new Color(1.6f, 1.6f, 1.6f, 1f)); }
                var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = _mat != null ? _mat : (lib != null ? lib.glowAdd : null);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                s.Init();
            }
            s.transform.SetPositionAndRotation(center, rot);
            s._t = 0f; s._life = life; s._radius = radius; s._width = width; s._arc = Mathf.Clamp(arcDeg, 10f, 360f); s._col = color;
            s.gameObject.SetActive(true);
            s.Rebuild(0f);
            Spawned++;
            return s;
        }

        void Init()
        {
            _mesh = new Mesh { name = "SlashArc" }; _mesh.MarkDynamic();
            _v = new Vector3[(Segs + 1) * 3]; _c = new Color[_v.Length];
            var uv = new Vector2[_v.Length]; var tris = new List<int>();
            for (int i = 0; i <= Segs; i++)
            {
                float u = i / (float)Segs;
                uv[i * 3] = new Vector2(u, 0f); uv[i * 3 + 1] = new Vector2(u, 0.5f); uv[i * 3 + 2] = new Vector2(u, 1f);
                if (i == Segs) continue;
                for (int k = 0; k < 2; k++)
                {
                    int a = i * 3 + k, b = a + 1, c = a + 3, d = a + 4;
                    tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            _mesh.vertices = _v; _mesh.uv = uv; _mesh.colors = _c; _mesh.SetTriangles(tris, 0);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        void Rebuild(float k)
        {
            float open = Mathf.Clamp01(k / 0.35f); open = 1f - (1f - open) * (1f - open) * (1f - open);   // ease-out sweep
            float tail = Mathf.Clamp01((k - 0.3f) / 0.7f);                                                    // tail eats the arc
            float half = _arc * 0.5f;
            float w = _width * (1f - 0.6f * tail);
            float sweep = _arc * open;
            for (int i = 0; i <= Segs; i++)
            {
                float u = i / (float)Segs;
                float ang = (-half + sweep * u) * Mathf.Deg2Rad;     // swing from right to left along +X → forward
                Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                // crescent: thick at the leading edge (u=1), thin at the tail
                float thick = w * Mathf.Lerp(0.15f, 1f, Mathf.SmoothStep(0f, 1f, u));
                _v[i * 3] = dir * (_radius - thick * 0.6f);
                _v[i * 3 + 1] = dir * _radius;
                _v[i * 3 + 2] = dir * (_radius + thick * 0.4f);
                float a = Mathf.Clamp01(u * 1.6f - tail * 1.4f) * (1f - tail * tail);
                Color core = Color.Lerp(_col, Color.white, 0.55f * u);
                _c[i * 3] = new Color(_col.r, _col.g, _col.b, a * 0.35f);
                _c[i * 3 + 1] = new Color(core.r, core.g, core.b, a);
                _c[i * 3 + 2] = new Color(_col.r, _col.g, _col.b, a * 0.6f);
            }
            _mesh.vertices = _v; _mesh.colors = _c; _mesh.RecalculateBounds();
        }

        void Update()
        {
            _t += Mathf.Max(Time.deltaTime, Time.unscaledDeltaTime * 0.2f);   // hit-stop slows the slash, never freezes it
            float k = _t / Mathf.Max(0.01f, _life);
            if (k >= 1f) { gameObject.SetActive(false); Pool.Push(this); return; }
            Rebuild(k);
        }
    }
}
