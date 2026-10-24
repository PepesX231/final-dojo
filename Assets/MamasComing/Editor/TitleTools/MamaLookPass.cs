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

        public static void ListLights()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var p = l.transform; string path = l.name; while (p.parent) { p = p.parent; path = p.name + "/" + path; }
                Debug.Log("[LookPass] light " + path + " type=" + l.type + " I=" + l.intensity.ToString("F2") + " range=" + l.range.ToString("F1") + " shadows=" + l.shadows + " pos=" + l.transform.position.ToString("F1") + " col=" + l.color + " active=" + l.isActiveAndEnabled);
            }
        }

        [MenuItem("Mama's Coming/Apply bright look pass (kitchen)")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[LookPass] stop Play mode first"); return; }
            var scene = EditorSceneManager.OpenScene(Kitchen, OpenSceneMode.Single);
            Debug.Log("[LookPass] before: fog " + RenderSettings.fogDensity + " " + RenderSettings.fogColor + " ambient " + RenderSettings.ambientSkyColor + "/" + RenderSettings.ambientEquatorColor + "/" + RenderSettings.ambientGroundColor);
            // v2 (owner's call): keep the warm orange-pink sunset mood so the lamps still matter, just lift it out of the mud
            RenderSettings.fogDensity = 0.005f;
            RenderSettings.fogColor = new Color(.93f, .66f, .62f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.72f, .66f, .86f);
            RenderSettings.ambientEquatorColor = new Color(.98f, .70f, .55f);
            RenderSettings.ambientGroundColor = new Color(.42f, .30f, .35f);
            RenderSettings.ambientIntensity = 1.1f;
            bool lampsDone = EditorPrefs.GetBool("MamaLookPass.v2.lamps");
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional)
                {
                    Debug.Log("[LookPass] sun '" + l.name + "' before: color " + l.color + " intensity " + l.intensity + " shadow " + l.shadowStrength);
                    l.color = new Color(1f, .66f, .42f); l.intensity = 1.85f; l.shadowStrength = .8f; l.shadows = LightShadows.Soft;
                }
                else if (!lampsDone && (l.type == LightType.Point || l.type == LightType.Spot))
                {
                    // house lamps: a touch stronger and warmer so they read as real light sources at dusk
                    l.intensity *= 1.2f;
                    if (l.color.b > l.color.r) continue;
                    l.color = Color.Lerp(l.color, new Color(1f, .82f, .6f), .3f);
                }
                EditorUtility.SetDirty(l);
            }
            EditorPrefs.SetBool("MamaLookPass.v2.lamps", true);
            // the gameplay camera drives fog density every frame (smoke thickens it) - its base value must match
            foreach (var tpc in Object.FindObjectsByType<ThirdPersonCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { tpc.baseFog = RenderSettings.fogDensity; EditorUtility.SetDirty(tpc); }
            // v3: the ceiling lamp of each room casts soft shadows and is the main light indoors (furniture gets
            // contact shadows and pools of light, like a real room at dusk)
            RenderSettings.ambientIntensity = 1.0f;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (l.type != LightType.Point || !l.transform.parent || l.transform.parent.name != "Rooms" || !l.name.EndsWith("Light") && !l.name.StartsWith("HallLight")) continue;
                if (l.name == "LampLight") { l.intensity = 2.0f; l.range = 5f; l.shadows = LightShadows.Soft; l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low; }
                else { l.intensity = Mathf.Max(l.intensity, 2.3f); l.shadows = LightShadows.Soft; l.shadowStrength = .7f; l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium; l.shadowNearPlane = .2f; }
                EditorUtility.SetDirty(l);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Grade);
            if (p)
            {
                if (p.TryGet(out ColorAdjustments ca)) { ca.postExposure.Override(.3f); ca.contrast.Override(13f); ca.saturation.Override(22f); }
                if (p.TryGet(out Bloom bl)) { bl.threshold.Override(.95f); bl.intensity.Override(.6f); }
                if (p.TryGet(out Vignette vg)) { vg.intensity.Override(.2f); vg.color.Override(new Color(.22f, .08f, .1f)); }
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
