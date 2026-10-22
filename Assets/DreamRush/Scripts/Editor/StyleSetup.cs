using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// Per-level style tech (M13/M14): installs the ComicPost full-screen pass on the URP renderers (idempotent) and
    /// builds Pip's outfit prefabs (Resources/DreamRush/Outfits) from the Blender FBX with DreamToon materials.
    /// </summary>
    public static class StyleSetup
    {
        const string ComicMatPath = "Assets/DreamRush/Materials/Look/M_ComicPost.mat";
        static readonly string[] Renderers = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };

        [MenuItem("DreamRush/Setup/Style tech (comic pass + outfits)")]
        public static void Ensure()
        {
            EnsureComicPass();
            EnsureOutfits();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("DreamRush/Setup/Diagnose comic pass")]
        public static void Diagnose()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ComicMatPath);
            var sh = mat != null ? mat.shader : null;
            Debug.Log($"[Style] mat={(mat != null)} shader={(sh != null ? sh.name : "null")} supported={(sh != null && sh.isSupported)} error={(sh != null && ShaderUtil.ShaderHasError(sh))} passes={(mat != null ? mat.passCount : -1)}");
            if (sh != null) foreach (var m in ShaderUtil.GetShaderMessages(sh)) Debug.Log("[Style] shader msg: " + m.message + " line " + m.line);
        }

        public static void EnsureComicPass()
        {
            var sh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DreamRush/Shaders/ComicPost.shader");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ComicMatPath);
            if (mat == null) { GrayboxAssets.EnsureFolder("Assets/DreamRush/Materials/Look"); mat = new Material(sh) { name = "M_ComicPost" }; AssetDatabase.CreateAsset(mat, ComicMatPath); }
            mat.shader = sh;
            foreach (var path in Renderers)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;
                FullScreenPassRendererFeature f = null;
                foreach (var rf in data.rendererFeatures) if (rf is FullScreenPassRendererFeature fs && rf.name == "DreamComic") f = fs;
                if (f == null)
                {
                    f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    f.name = "DreamComic";
                    AssetDatabase.AddObjectToAsset(f, data);
                    data.rendererFeatures.Add(f);
                    var so = new SerializedObject(data);
                    var map = so.FindProperty("m_RendererFeatureMap");
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long id);
                    map.arraySize = data.rendererFeatures.Count;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                f.passMaterial = mat;
                f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                f.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color;
                f.fetchColorBuffer = true;
                f.SetActive(true);
                EditorUtility.SetDirty(f); EditorUtility.SetDirty(data);
                Debug.Log("[Style] comic pass on " + path);
            }
        }

        public static void EnsureOutfits()
        {
            var mats = new Dictionary<string, Material>
            {
                ["M_Outfit_Ink"] = ArtKit.Mat("M_Outfit_Ink", new Color(0.09f, 0.07f, 0.13f), rim: 0.35f, spec: 0.5f),
                ["M_Outfit_White"] = ArtKit.Mat("M_Outfit_White", Color.white, rim: 0.2f),
                ["M_Outfit_Red"] = ArtKit.Mat("M_Outfit_Red", new Color(0.92f, 0.12f, 0.25f), spec: 0.4f),
                ["M_Outfit_Yellow"] = ArtKit.Mat("M_Outfit_Yellow", new Color(1f, 0.83f, 0.05f), rim: 0.2f, spec: 0.4f),
            };
            GrayboxAssets.EnsureFolder("Assets/DreamRush/Resources/DreamRush/Outfits");
            foreach (var n in new[] { "PIP_Outfit_Phantom", "PIP_Outfit_Densha" })
            {
                string fbx = $"Assets/DreamRush/Art/Characters/Outfits/{n}.fbx";
                var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
                if (mi == null) { Debug.LogWarning("[Style] missing " + fbx); continue; }
                mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                foreach (var kv in mats) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
                mi.SaveAndReimport();
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                PrefabUtility.SaveAsPrefabAsset(inst, $"Assets/DreamRush/Resources/DreamRush/Outfits/{n}.prefab");
                Object.DestroyImmediate(inst);
            }
        }
    }
}
