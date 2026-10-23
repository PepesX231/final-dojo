using System.Collections.Generic;
using System.IO;
using MamasComing.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.MenuEditor
{
    /// Builds the two front-end scenes (main menu + 3D lobby) and puts them first in the build list.
    /// Scenes only hold a camera, light, comic Global Volume and the bootstrap component; everything else is built at runtime.
    public static class MenuSceneBuilder
    {
        const string Root = "Assets/MamasComing/Menu";
        const string MenuPath = Root + "/Scenes/MC_MainMenu.unity", LobbyPath = Root + "/Scenes/MC_Lobby.unity";

        [MenuItem("Mama's Coming/Build Menu + Lobby Scenes")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Materials");
            var toon = Mat("MAT_MenuToon", "MamasComing/ToyToon", "Universal Render Pipeline/Lit", m =>
            {
                Set(m, "_ToonShadow", new Color(.6f, .58f, .9f)); SetF(m, "_ToonSoft", .06f); SetF(m, "_ToonAmbient", .6f); SetF(m, "_Smoothness", .2f);
            });
            var ink = Mat("MAT_MenuInk", "DreamRush/InkOutline", null, m => { Set(m, "_OutlineColor", new Color(.06f, .05f, .06f)); SetF(m, "_OutlineWidth", 3.2f); SetF(m, "_MinDistanceScale", .5f); });
            var profile = Profile();
            var fruits = Fruits();

            // ---- main menu
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = MakeCamera("Menu Camera", new Color(.55f, .82f, 1f), 40f);
            MakeLight(new Vector3(42, -30, 0), new Color(1f, .93f, .78f), 1.25f);
            MakeVolume(profile);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.62f, .62f, .66f);
            var menu = new GameObject("Main Menu").AddComponent<MainMenu>();
            menu.fruitPrefabs = fruits; menu.baseMaterial = toon; menu.inkMaterial = ink; menu.cam = cam;
            EditorSceneManager.SaveScene(s, MenuPath);

            // ---- lobby
            s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            cam = MakeCamera("Lobby Camera", new Color(.55f, .82f, 1f), 50f);
            MakeLight(new Vector3(50, -35, 0), new Color(1f, .95f, .8f), 1.3f);
            MakeVolume(profile);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.64f, .64f, .68f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(.6f, .84f, 1f);
            RenderSettings.fogStartDistance = 18f; RenderSettings.fogEndDistance = 45f;
            var go = new GameObject("Lobby");
            var lobby = go.AddComponent<Lobby>(); var net = go.AddComponent<LobbyNet>();
            lobby.fruitPrefabs = fruits; lobby.baseMaterial = toon; lobby.inkMaterial = ink; lobby.cam = cam; lobby.net = net; net.lobby = lobby;
            EditorSceneManager.SaveScene(s, LobbyPath);

            AddToBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[Menu] built " + MenuPath + " and " + LobbyPath + " (fruits=" + fruits.Length + ", toon=" + toon.shader.name + ")");
        }

        static GameObject[] Fruits()
        {
            var list = new List<GameObject> { AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/PF_AppleKid.prefab") };
            var cat = Resources.Load<KidCatalog>("MamaKidCatalog");
            for (int i = 1; i < KidLook.Count; i++) list.Add(cat ? cat.Prefab(i) : null);
            return list.ToArray();
        }

        static Camera MakeCamera(string name, Color bg, float fov)
        {
            var go = new GameObject(name); go.tag = "MainCamera";
            var c = go.AddComponent<Camera>(); c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = bg; c.fieldOfView = fov; c.nearClipPlane = .1f; c.farClipPlane = 200f;
            go.AddComponent<AudioListener>();
            var data = go.AddComponent<UniversalAdditionalCameraData>(); data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            go.transform.position = new Vector3(0, 6, -9); go.transform.LookAt(Vector3.zero);
            return c;
        }

        static void MakeLight(Vector3 euler, Color c, float intensity)
        {
            var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.color = c; l.intensity = intensity;
            l.shadows = LightShadows.Soft; l.shadowStrength = .75f; l.transform.rotation = Quaternion.Euler(euler.x, euler.y, euler.z);
        }

        static void MakeVolume(VolumeProfile p)
        {
            var v = new GameObject("Comic Global Volume").AddComponent<Volume>(); v.isGlobal = true; v.priority = 5; v.sharedProfile = p;
        }

        /// Bright kid-friendly comic grade: saturated, clean, warm sunny highlights, ink + halftone from the shared toon feature.
        static VolumeProfile Profile()
        {
            string path = Root + "/Materials/VP_MenuComic.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!p) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            var tm = Get<Tonemapping>(p); tm.mode.Override(TonemappingMode.Neutral);
            var ca = Get<ColorAdjustments>(p); ca.contrast.Override(12f); ca.saturation.Override(28f); ca.postExposure.Override(.2f);
            var st = Get<SplitToning>(p); st.shadows.Override(new Color(.45f, .6f, .85f)); st.highlights.Override(new Color(1f, .92f, .6f)); st.balance.Override(10f);
            var bl = Get<Bloom>(p); bl.intensity.Override(.35f); bl.threshold.Override(1.05f); bl.scatter.Override(.55f);
            var vg = Get<Vignette>(p); vg.intensity.Override(.16f); vg.smoothness.Override(.5f); vg.color.Override(new Color(.25f, .12f, .35f));
            var fg = Get<FilmGrain>(p); fg.type.Override(FilmGrainLookup.Medium3); fg.intensity.Override(.08f); fg.response.Override(.6f);
            var toon = Get<MamasComing.Style.MamaToonStyle>(p);
            toon.inkStrength.Override(.85f); toon.inkThickness.Override(1.8f); toon.inkColor.Override(new Color(.06f, .05f, .06f));
            toon.halftoneStrength.Override(.25f); toon.halftoneCell.Override(6f); toon.paintGrain.Override(.15f);
            toon.shadowTint.Override(new Color(.55f, .55f, .95f)); toon.shadowTintAmount.Override(.45f); toon.celSharpness.Override(.8f);
            EditorUtility.SetDirty(p);
            return p;
        }

        static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (p.TryGet<T>(out var c)) return c;
            c = p.Add<T>(true); c.name = typeof(T).Name; AssetDatabase.AddObjectToAsset(c, p); return c;
        }

        static Material Mat(string name, string shader, string fallback, System.Action<Material> setup)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var sh = Shader.Find(shader) ?? (fallback != null ? Shader.Find(fallback) : null);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else if (sh && m.shader != sh) m.shader = sh;
            setup(m); EditorUtility.SetDirty(m);
            return m;
        }
        static void Set(Material m, string p, Color c) { if (m.HasProperty(p)) m.SetColor(p, c); }
        static void SetF(Material m, string p, float f) { if (m.HasProperty(p)) m.SetFloat(p, f); }

        static void AddToBuild()
        {
            var want = new[] { MenuPath, LobbyPath, "Assets/MamasComing/Scenes/MamasKitchen.unity", "Assets/MamasComing/WitchTest/Scenes/AppleWitch_Test.unity" };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var w in want) if (File.Exists(w)) list.Add(new EditorBuildSettingsScene(w, true));
            foreach (var s in EditorBuildSettings.scenes) if (!list.Exists(x => x.path == s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
