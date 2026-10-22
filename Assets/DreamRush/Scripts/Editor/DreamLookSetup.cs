using DreamRush.Core;
using DreamRush.Look;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// Shading foundation for the visual reconstruction (docs/VISUAL_RECONSTRUCTION.md): DreamLook settings in
    /// Resources, ink outline material, the dream post-processing profile, pipeline quality (MSAA for ink lines).
    /// </summary>
    public static class DreamLookSetup
    {
        const string ShaderDir = "Assets/DreamRush/Shaders";
        const string LookMatDir = "Assets/DreamRush/Materials/Look";
        public const string DreamProfilePath = "Assets/DreamRush/Settings/PP_Dream.asset";
        public const string BedroomProfilePath = "Assets/DreamRush/Settings/PP_Bedroom.asset";

        [MenuItem("DreamRush/Build/Dream Look (shaders, post)")]
        public static DreamLookSettings Ensure()
        {
            GrayboxAssets.EnsureFolder(LookMatDir);
            GrayboxAssets.EnsureFolder("Assets/DreamRush/Settings");
            GrayboxAssets.EnsureFolder("Assets/DreamRush/Resources/DreamRush");
            var toon = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "/DreamToon.shader");
            var outline = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "/InkOutline.shader");

            string omPath = LookMatDir + "/M_InkOutline.mat";
            var om = AssetDatabase.LoadAssetAtPath<Material>(omPath);
            if (om == null && outline != null) { om = new Material(outline) { name = "M_InkOutline" }; AssetDatabase.CreateAsset(om, omPath); }

            string sPath = "Assets/DreamRush/Resources/" + DreamLookSettings.ResourcePath + ".asset";
            var s = AssetDatabase.LoadAssetAtPath<DreamLookSettings>(sPath);
            if (s == null) { s = ScriptableObject.CreateInstance<DreamLookSettings>(); AssetDatabase.CreateAsset(s, sPath); }
            s.toon = toon; s.outline = outline; s.outlineMaterial = om;
            EditorUtility.SetDirty(s);

            BuildVfxLibrary();
            BuildProfile(DreamProfilePath, dream: true);
            BuildProfile(BedroomProfilePath, dream: false);

            // ink lines need anti-aliasing
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.msaaSampleCount < 4)
            {
                urp.msaaSampleCount = 4;
                EditorUtility.SetDirty(urp);
            }
            if (QualitySettings.renderPipeline is UniversalRenderPipelineAsset q && q.msaaSampleCount < 4)
            {
                q.msaaSampleCount = 4;
                EditorUtility.SetDirty(q);
            }
            AssetDatabase.SaveAssets();
            return s;
        }

        const string VfxTexDir = "Assets/DreamRush/Art/VFX/Textures";

        static Material ParticleMat(string name, string tex, bool additive)
        {
            string path = $"{LookMatDir}/{name}.mat";
            var sh = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "/DreamParticle.shader");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            string tp = $"{VfxTexDir}/{tex}.png";
            var imp = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp)) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Presentation.VFXLibrary BuildVfxLibrary()
        {
            string path = "Assets/DreamRush/Resources/" + Presentation.VFXLibrary.ResourcePath + ".asset";
            var lib = AssetDatabase.LoadAssetAtPath<Presentation.VFXLibrary>(path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<Presentation.VFXLibrary>(); AssetDatabase.CreateAsset(lib, path); }
            lib.puff = ParticleMat("M_VFX_Puff", "VFX_Puff", false);
            lib.sparkleAdd = ParticleMat("M_VFX_Sparkle", "VFX_Sparkle", false);
            lib.glowAdd = ParticleMat("M_VFX_GlowAdd", "VFX_Glow", true);
            lib.ring = ParticleMat("M_VFX_Ring", "VFX_Ring", false);
            lib.telegraph = ParticleMat("M_VFX_Telegraph", "VFX_Telegraph", false);
            lib.streakAdd = ParticleMat("M_VFX_StreakAdd", "VFX_Streak", true);
            lib.beamAdd = ParticleMat("M_VFX_BeamAdd", "VFX_Beam", true);
            lib.beamAdd.SetFloat("_NearFade", 4f);
            lib.confetti = ParticleMat("M_VFX_Confetti", "VFX_Confetti", false);
            lib.z = ParticleMat("M_VFX_Z", "VFX_Z", false);
            EditorUtility.SetDirty(lib);
            return lib;
        }

        static void BuildProfile(string path, bool dream)
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            T Get<T>() where T : VolumeComponent
            {
                if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); }
                c.active = true;
                return c;
            }
            var tm = Get<Tonemapping>();
            tm.mode.Override(TonemappingMode.Neutral);
            var ca = Get<ColorAdjustments>();
            ca.postExposure.Override(dream ? -0.1f : 0f);
            ca.contrast.Override(dream ? 16f : 8f);
            ca.saturation.Override(dream ? 12f : -4f);
            ca.colorFilter.Override(dream ? new Color(1f, 0.985f, 0.97f) : new Color(0.97f, 0.96f, 1f));
            var smh = Get<ShadowsMidtonesHighlights>();
            smh.shadows.Override(new Vector4(0.86f, 0.8f, 1.05f, 0f));    // plum shadows (§5.5: soft, never black)
            smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.95f, 0f)); // warm highlights
            var vg = Get<Vignette>();
            vg.intensity.Override(dream ? 0.2f : 0.3f);
            vg.smoothness.Override(0.45f);
            vg.color.Override(Palette.MidnightPlum);
            var bl = Get<Bloom>();
            bl.threshold.Override(1.05f);
            bl.intensity.Override(dream ? 0.15f : 0.35f);   // only lamps bloom; no glow on everything (§5.10)
            bl.scatter.Override(0.6f);
            EditorUtility.SetDirty(p);
        }

        /// <summary>Adds the global post volume + camera post flag to the open scene.</summary>
        public static void AddVolume(Transform parent, bool dream)
        {
            var go = new GameObject(dream ? "PostFX_Dream" : "PostFX_Bedroom");
            if (parent != null) go.transform.SetParent(parent, false);
            var v = go.AddComponent<Volume>();
            v.isGlobal = true; v.priority = 1;
            v.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(dream ? DreamProfilePath : BedroomProfilePath);
        }

        public static void ConfigureCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None; // MSAA handles the ink
        }
    }
}
