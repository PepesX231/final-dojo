using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.Hub
{
    /// <summary>
    /// Applies the current <see cref="LobbyTheme"/> to the bedroom diorama: sky (FantasySky by day, NebulaSky at night),
    /// key light, ambient, fog, wall/floor tints and the practical lamps. Materials are instanced at runtime so play mode
    /// never edits the assets. Re-checks the clock every 30 s (the mood changes while you sit in the menu).
    /// </summary>
    public class LobbyThemeDirector : MonoBehaviour
    {
        public static LobbyThemeDirector Current { get; private set; }
        public Light sun;
        public List<Light> lamps = new List<Light>();
        public Material daySky, nightSky;
        public List<Material> wallMaterials = new List<Material>();
        public List<Material> floorMaterials = new List<Material>();
        [Tooltip("Shown only at night (stars, glowing moon...).")] public List<GameObject> nightOnly = new List<GameObject>();
        [Tooltip("Shown only by day (sun, birds...).")] public List<GameObject> dayOnly = new List<GameObject>();

        public LobbyTheme Theme { get; private set; }
        public event System.Action<LobbyTheme> ThemeChanged;
        readonly Dictionary<Material, Material> _inst = new Dictionary<Material, Material>();
        readonly Dictionary<Material, Color> _baseColor = new Dictionary<Material, Color>();
        Material _day, _night;
        readonly Dictionary<Light, float> _lampBase = new Dictionary<Light, float>();
        float _check;

        void Awake()
        {
            Current = this;
            foreach (var l in lamps) if (l != null) _lampBase[l] = l.intensity;
            if (daySky != null) _day = new Material(daySky);
            if (nightSky != null) _night = new Material(nightSky);
            var targets = new HashSet<Material>(wallMaterials); targets.UnionWith(floorMaterials);
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || !targets.Contains(m)) continue;
                    if (!_inst.TryGetValue(m, out var inst)) { inst = new Material(m); _inst[m] = inst; _baseColor[m] = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white; }
                    mats[i] = inst; changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
            Apply(LobbyTheme.Current);
        }

        void OnDestroy() { if (Current == this) Current = null; }

        void Update()
        {
            _check -= Time.unscaledDeltaTime;
            if (_check > 0f) return;
            _check = 30f;
            var t = LobbyTheme.Current;
            if (t != Theme) Apply(t);
        }

        public void Apply(LobbyTheme t)
        {
            Theme = t;
            if (sun != null)
            {
                sun.color = t.sunColor; sun.intensity = t.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(t.sunEuler);
            }
            foreach (var kv in _lampBase) if (kv.Key != null) kv.Key.intensity = kv.Value * t.lampIntensity;   // practicals matter most at night
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = t.ambientSky; RenderSettings.ambientEquatorColor = t.ambientEquator; RenderSettings.ambientGroundColor = t.ambientGround;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = t.fog;
            RenderSettings.fogStartDistance = 40f; RenderSettings.fogEndDistance = 160f;
            var sky = t.night ? _night : _day;
            if (sky != null)
            {
                if (!t.night)
                {
                    sky.SetColor("_Zenith", t.skyTop); sky.SetColor("_Horizon", t.skyHorizon); sky.SetColor("_Band", t.skyBand); sky.SetColor("_Ground", t.skyGround);
                    Vector3 toSun = -(Quaternion.Euler(t.sunEuler) * Vector3.forward);
                    sky.SetVector("_SunDir", new Vector4(toSun.x, Mathf.Max(0.08f, toSun.y), toSun.z, 0));
                    sky.SetColor("_SunColor", t.sunColor * 3.5f);
                    sky.SetFloat("_StarStrength", t.id == "sunset" ? 0.25f : 0f);
                }
                else
                {
                    sky.SetColor("_TopColor", t.skyTop); sky.SetColor("_HorizonColor", t.skyHorizon); sky.SetColor("_BottomColor", t.skyGround);
                    sky.SetColor("_NebulaA", t.skyBand); sky.SetColor("_NebulaB", t.primary * 0.8f);
                }
                RenderSettings.skybox = sky;
            }
            foreach (var kv in _inst)
            {
                bool wall = wallMaterials.Contains(kv.Key);
                Color tint = wall ? t.wallTint : t.floorTint;
                if (kv.Value.HasProperty("_BaseColor")) kv.Value.SetColor("_BaseColor", _baseColor[kv.Key] * tint);
            }
            foreach (var g in nightOnly) if (g != null) g.SetActive(t.night);
            foreach (var g in dayOnly) if (g != null) g.SetActive(!t.night);
            var cam = Camera.main;
            if (cam != null) { cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = t.skyHorizon; }
            ThemeChanged?.Invoke(t);
        }
    }
}
