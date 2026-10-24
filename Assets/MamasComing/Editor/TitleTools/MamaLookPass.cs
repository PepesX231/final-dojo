using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.Editor
{
    /// Brighter, cleaner gameplay look for MamasKitchen: whiter sun with firmer shadows, cooler sky ambient,
    /// much less pink haze, more contrast / colour in the grade and stronger contact shadows (SSAO).
    /// Only lighting values change - no objects are added, moved or removed. Old values are logged so they can be restored.
    public static class MamaLookPass
    {
        const string Kitchen = "Assets/MamasComing/Scenes/MamasKitchen.unity";
        const string Grade = "Assets/MamasComing/Art/Recast/Mama_Recast_Grade.asset";
        const string Renderer = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("Mama's Coming/Apply bright look pass (kitchen)")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[LookPass] stop Play mode first"); return; }
            var scene = EditorSceneManager.OpenScene(Kitchen, OpenSceneMode.Single);
            Debug.Log("[LookPass] before: fog " + RenderSettings.fogDensity + " " + RenderSettings.fogColor + " ambient " + RenderSettings.ambientSkyColor + "/" + RenderSettings.ambientEquatorColor + "/" + RenderSettings.ambientGroundColor);
            RenderSettings.fogDensity = 0.0045f;
            RenderSettings.fogColor = new Color(1f, .86f, .8f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.80f, .82f, .93f);
            RenderSettings.ambientEquatorColor = new Color(.96f, .80f, .68f);
            RenderSettings.ambientGroundColor = new Color(.46f, .37f, .36f);
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                Debug.Log("[LookPass] sun '" + l.name + "' before: color " + l.color + " intensity " + l.intensity + " shadow " + l.shadowStrength);
                l.color = new Color(1f, .87f, .74f); l.intensity = 1.95f; l.shadowStrength = .85f; l.shadows = LightShadows.Soft;
                EditorUtility.SetDirty(l);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Grade);
            if (p)
            {
                if (p.TryGet(out ColorAdjustments ca)) { ca.postExposure.Override(.2f); ca.contrast.Override(16f); ca.saturation.Override(20f); }
                if (p.TryGet(out Bloom bl)) { bl.threshold.Override(1.05f); bl.intensity.Override(.5f); }
                if (p.TryGet(out Vignette vg)) { vg.intensity.Override(.2f); vg.color.Override(new Color(.25f, .1f, .08f)); }
                EditorUtility.SetDirty(p);
            }
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Renderer);
            if (rd)
                foreach (var f in rd.rendererFeatures)
                {
                    if (!f || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                    var so = new SerializedObject(f);
                    so.FindProperty("m_Settings.Intensity").floatValue = .85f;
                    so.FindProperty("m_Settings.Radius").floatValue = .45f;
                    so.FindProperty("m_Settings.DirectLightingStrength").floatValue = .35f;
                    so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(f);
                }
            AssetDatabase.SaveAssets();
            Debug.Log("[LookPass] applied");
        }
    }
}
