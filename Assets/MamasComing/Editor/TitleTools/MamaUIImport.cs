using UnityEditor;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Import rules for the generated craft UI sprites + fonts in Resources/MamaUI (sprite type, 9-slice borders, fallbacks).
    public class MamaUIImport : AssetPostprocessor
    {
        const string Dir = "Assets/MamasComing/Resources/MamaUI/";

        static Vector4 Border(string n)
        {
            // x=left, y=bottom, z=right, w=top (pixels of the source png)
            switch (n)
            {
                case "ui_paper": case "ui_paper_torn": case "ui_paint": return new Vector4(80, 80, 80, 80);
                case "ui_plank": case "ui_plank_dark": return new Vector4(64, 40, 64, 40);
                case "ui_board": return new Vector4(150, 150, 150, 150);
                case "ui_slot": return new Vector4(32, 26, 32, 26);
                case "ui_round": return new Vector4(80, 80, 80, 80);
                case "ui_round_soft": return new Vector4(96, 96, 96, 96);
                case "ui_woodframe": return new Vector4(100, 100, 100, 100);
                case "ui_plank2": return new Vector4(60, 30, 60, 30);
                case "ui_slip": return new Vector4(40, 40, 40, 40);
                case "ui_gauge_track": case "ui_gauge_fill": return new Vector4(23, 23, 23, 23);
                default: return Vector4.zero;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = 100;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; s.spriteBorder = Border(System.IO.Path.GetFileNameWithoutExtension(assetPath));
            ti.SetTextureSettings(s);
        }

        public static void Reimport()
        {
            foreach (var g in AssetDatabase.FindAssets("", new[] { Dir.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".png") || p.EndsWith(".ttf")) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            }
            Debug.Log("[MamaUIImport] reimported");
        }

        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(Dir) || !assetPath.EndsWith(".ttf")) return;
            if (assetImporter is TrueTypeFontImporter fi)
            {
                fi.fontTextureCase = FontTextureCase.Dynamic;
                fi.includeFontData = true;
                fi.fontNames = new[] { "Segoe UI Symbol", "Segoe UI Emoji", "Leelawadee UI", "Arial" };
            }
        }
    }
}
