using UnityEditor;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Writes the ComicFx sprite materials (Art/ComicFx/Resources/ComicFx/*.mat) that ComicFx loads at runtime.
    /// Textures come from ArtSource/Blender/Scripts/sculpt/comic_fx_textures.py.
    public static class MamaComicFxBuilder
    {
        const string Tex = "Assets/MamasComing/Art/ComicFx/Textures/";
        const string Out = "Assets/MamasComing/Art/ComicFx/Resources/ComicFx/";
        static readonly string[] Names = { "FX_ComicBurst", "FX_ComicBurstB", "FX_InkPuff", "FX_Star", "FX_SpeedLines", "FX_Splash", "FX_Ring" };

        [MenuItem("Mama's Coming/Comic FX/Build Materials")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Out);
            var shader = Shader.Find("MamasComing/ComicSprite");
            foreach (var n in Names)
            {
                string tp = Tex + n + ".png";
                var imp = (TextureImporter)AssetImporter.GetAtPath(tp);
                if (!imp) { Debug.LogError("[Mama's Coming] missing " + tp); continue; }
                imp.textureType = TextureImporterType.Default; imp.alphaIsTransparency = true; imp.mipmapEnabled = true;
                imp.wrapMode = TextureWrapMode.Clamp; imp.sRGBTexture = true;
                imp.SaveAndReimport();
                string mp = Out + n + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, mp); }
                m.shader = shader;
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
                m.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] comic FX materials written to " + Out);
        }
    }
}
