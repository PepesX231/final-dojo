using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Builds the particle materials for MamaVfx from the hand-made sprites in Resources/MamaVfx
    /// (saved as assets so the shaders are always in builds). Safe to run again.
    public static class MamaVfxSetup
    {
        const string Dir = "Assets/MamasComing/Resources/MamaVfx";
        static readonly (string tex, bool add)[] Mats =
        {
            ("vfx_soft", true), ("vfx_star", false), ("vfx_spark", true), ("vfx_flame", false),
            ("vfx_puff", false), ("vfx_smoke", false), ("vfx_ring", false), ("vfx_drop", false), ("vfx_bang", false), ("vfx_chip", false),
        };

        [MenuItem("Mama's Coming/VFX/Build particle materials")]
        public static void Build()
        {
            foreach (var (tex, add) in Mats)
            {
                string tp = Dir + "/" + tex + ".png";
                var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
                if (ti) { ti.textureType = TextureImporterType.Default; ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true; ti.sRGBTexture = true; ti.SaveAndReimport(); }
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
                string mp = Dir + "/M_" + tex + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(m, mp); }
                m.SetTexture("_BaseMap", t); m.mainTexture = t; m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", add ? 2 : 0); m.SetFloat("_ZWrite", 0); m.SetFloat("_Cull", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", add ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (add) m.EnableKeyword("_BLENDMODE_ADD"); else m.DisableKeyword("_BLENDMODE_ADD");
                m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = 3000 + (add ? 10 : 0);
                // soft particles off (works without the depth texture); colour from the particle
                m.SetFloat("_SoftParticlesEnabled", 0); m.SetFloat("_ColorMode", 0);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[VFX] materials ready");
        }
    }
}
