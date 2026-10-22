using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Look
{
    /// <summary>
    /// Per-level art style (user brief 2026-10-20: "each level has its own art style and Pip changes to match").
    /// Drives the ComicPost full-screen pass (globals) and re-dresses Pip: ramp hardness, rim colour, ink width and an
    /// outfit accessory (Densha cap + headphones in World 1, phantom-thief domino mask in Midnight Palace).
    /// Scenes without a StylePack get everything reset to 0 (SceneContext calls <see cref="ResetGlobals"/>).
    /// </summary>
    public class StylePack : MonoBehaviour
    {
        public enum Outfit { None, DenshaCap, PhantomMask }
        public static StylePack Current { get; private set; }

        [Header("Comic pass")]
        [Range(0, 1)] public float ink = 0.85f;
        public Color inkColor = new Color(0.07f, 0.05f, 0.12f);
        [Range(0.5f, 4)] public float inkWidth = 1.4f;
        public float inkFar = 120f;
        [Range(0, 1)] public float halftone = 0.5f;
        public Color halftoneColor = new Color(0.1f, 0.08f, 0.3f, 0.75f);
        public float halftoneScale = 6f;
        [Range(0, 1)] public float posterize = 0.4f;
        [Range(-1, 1)] public float saturate = 0.15f;
        public Color speedColor = new Color(1f, 1f, 1f, 0.55f);
        [Range(0, 1)] public float celHard = 0f;                       // razor cel edges on every DreamToon material
        public Color shadowTint = new Color(0.4f, 0.45f, 0.85f, 0f);   // level-wide shadow colour (a = strength)
        [Range(0, 1)] public float sketch = 0f;                        // World 3 Pencil Sketch / Cross-Hatching & Screentone pass
        public Color paperColor = new Color(0.96f, 0.94f, 0.90f, 1f);  // Warm tactile sketch paper tint
        public bool speedLines = true;
        [Header("Pip")]
        public Outfit outfit = Outfit.None;
        public Color pipRim = new Color(1f, 0.95f, 0.85f);
        [Range(0, 1)] public float pipRimStrength = 0.35f;
        [Range(0.01f, 1)] public float pipRampSoftness = 0.12f;
        public Color pipShadow = new Color(0.45f, 0.35f, 0.7f);

        public float Glitch { get; set; }
        PlayerMotor3D _player;
        float _speed;

        static readonly int Ink = Shader.PropertyToID("_DR_Ink"), InkC = Shader.PropertyToID("_DR_InkColor"), InkW = Shader.PropertyToID("_DR_InkWidth"), InkF = Shader.PropertyToID("_DR_InkFar"),
            Half = Shader.PropertyToID("_DR_Halftone"), HalfC = Shader.PropertyToID("_DR_HalftoneColor"), HalfS = Shader.PropertyToID("_DR_HalftoneScale"),
            Post = Shader.PropertyToID("_DR_Posterize"), Sat = Shader.PropertyToID("_DR_Saturate"), Spd = Shader.PropertyToID("_DR_Speed"), SpdC = Shader.PropertyToID("_DR_SpeedColor"),
            Gl = Shader.PropertyToID("_DR_Glitch"), Cel = Shader.PropertyToID("_DR_CelHard"), ShT = Shader.PropertyToID("_DR_ShadowTint"),
            Sk = Shader.PropertyToID("_DR_Sketch"), PapC = Shader.PropertyToID("_DR_PaperColor");

        public static void ResetGlobals()
        {
            Shader.SetGlobalFloat(Ink, 0); Shader.SetGlobalFloat(Half, 0); Shader.SetGlobalFloat(Post, 0); Shader.SetGlobalFloat(Sat, 0);
            Shader.SetGlobalFloat(Spd, 0); Shader.SetGlobalFloat(Gl, 0); Shader.SetGlobalFloat(InkW, 1.4f); Shader.SetGlobalFloat(InkF, 100f); Shader.SetGlobalFloat(HalfS, 6f);
            Shader.SetGlobalFloat(Cel, 0); Shader.SetGlobalColor(ShT, new Color(0, 0, 0, 0));
            Shader.SetGlobalFloat(Sk, 0); Shader.SetGlobalColor(PapC, new Color(0.96f, 0.94f, 0.90f, 1f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            ResetGlobals();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (Current == null) ResetGlobals(); };   // un-styled scenes stay clean
        }

        void OnEnable() { Current = this; Apply(); }
        void OnDisable() { if (Current == this) { Current = null; ResetGlobals(); } }

        void Start()
        {
            _player = FindFirstObjectByType<PlayerMotor3D>();
            if (_player != null) DressPip(_player.gameObject);
            Apply();
        }

        public void Apply()
        {
            Shader.SetGlobalFloat(Ink, ink); Shader.SetGlobalColor(InkC, inkColor); Shader.SetGlobalFloat(InkW, inkWidth); Shader.SetGlobalFloat(InkF, inkFar);
            Shader.SetGlobalFloat(Half, halftone); Shader.SetGlobalColor(HalfC, halftoneColor); Shader.SetGlobalFloat(HalfS, halftoneScale);
            Shader.SetGlobalFloat(Post, posterize); Shader.SetGlobalFloat(Sat, saturate); Shader.SetGlobalColor(SpdC, speedColor);
            Shader.SetGlobalFloat(Cel, celHard); Shader.SetGlobalColor(ShT, shadowTint);
            Shader.SetGlobalFloat(Sk, sketch); Shader.SetGlobalColor(PapC, paperColor);
        }

        void Update()
        {
            float target = 0f;
            if (speedLines && _player != null)
            {
                float v = _player.PlanarVelocity.magnitude;
                target = Mathf.InverseLerp(9.5f, 16f, v);
                if (_player.IsDashing) target = 1f;
            }
            _speed = Mathf.MoveTowards(_speed, target, Time.unscaledDeltaTime * 4f);
            Shader.SetGlobalFloat(Spd, _speed);
            Shader.SetGlobalFloat(Gl, Glitch);
        }

        // ================================================================== Pip's look for this level
        public void DressPip(GameObject pip)
        {
            var vis = pip.transform.Find("Visual");
            if (vis == null) return;
            foreach (var r in vis.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "InkOutline" || r is TrailRenderer) continue;
                var mats = r.materials;   // instances: the level's look never edits the shared asset
                foreach (var m in mats)
                {
                    if (m == null || !m.HasProperty("_RampSoftness")) continue;
                    m.SetFloat("_RampSoftness", pipRampSoftness);
                    m.SetColor("_RimColor", pipRim); m.SetFloat("_RimStrength", pipRimStrength);
                    m.SetColor("_ShadowColor", pipShadow);
                }
                r.materials = mats;
            }
            var old = FindDeep(vis, "Outfit");
            if (old != null) Destroy(old.gameObject);
            if (outfit == Outfit.None) return;
            var body = FindDeep(vis, "Body") ?? (vis.childCount > 0 ? vis.GetChild(0) : vis);
            var root = new GameObject("Outfit").transform; root.SetParent(body, false);
            var model = Resources.Load<GameObject>(outfit == Outfit.DenshaCap ? "DreamRush/Outfits/PIP_Outfit_Densha" : "DreamRush/Outfits/PIP_Outfit_Phantom");
            if (model != null)
            {
                var go = Instantiate(model, root); go.name = model.name;
                // fit to the body: the outfit models are authored around a 1 m apple centred at the body origin
                float s = body.lossyScale.x > 0.0001f ? 1f / body.lossyScale.x : 1f;
                root.localScale = Vector3.one * s;
                DreamLook.AddOutline(go, 1.6f);
            }
        }

        static Transform FindDeep(Transform t, string n)
        {
            if (t.name == n) return t;
            foreach (Transform c in t) { var r = FindDeep(c, n); if (r != null) return r; }
            return null;
        }
    }
}
