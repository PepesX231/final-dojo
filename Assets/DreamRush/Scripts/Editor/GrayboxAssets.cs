using System.IO;
using DreamRush.Core;
using DreamRush.Player;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>Creates/loads the shared graybox assets (materials, textures, configs) as real project assets.</summary>
    public static class GrayboxAssets
    {
        public const string Root = "Assets/DreamRush";
        public const string MaterialDir = Root + "/Materials/Graybox";
        public const string TextureDir = Root + "/Art/Textures";
        public const string ConfigDir = Root + "/ScriptableObjects/Player";
        public const string PrefabDir = Root + "/Prefabs/Player";
        public const string SceneDir = Root + "/Scenes";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Lit material asset, created once per name. Colour is updated if it changed.</summary>
        public static Material Lit(string name, Color color)
        {
            EnsureFolder(MaterialDir);
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = RuntimeMaterials.CreateLit(color, 0.2f, name);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.GetColor("_BaseColor") != color)
            {
                m.SetColor("_BaseColor", color);
                m.color = color;
                EditorUtility.SetDirty(m);
            }
            return m;
        }

        public static Material GridFloor(string name, Color baseColor, Color lineColor, float tiles)
        {
            EnsureFolder(MaterialDir);
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var tex = GridTexture("T_GRAYBOX_Grid", baseColor, lineColor);
            if (m == null)
            {
                m = RuntimeMaterials.CreateLit(Color.white, 0.1f, name);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            m.SetTextureScale("_BaseMap", new Vector2(tiles, tiles));
            m.mainTextureScale = new Vector2(tiles, tiles);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Texture2D GridTexture(string name, Color baseColor, Color lineColor)
        {
            string path = $"{TextureDir}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            EnsureFolder(TextureDir);
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool major = x < 3 || y < 3;
                bool minor = (x % 64) < 1 || (y % 64) < 1;
                px[y * size + x] = major ? lineColor : minor ? Color.Lerp(baseColor, lineColor, 0.35f) : baseColor;
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Material BlobShadowMaterial()
        {
            EnsureFolder(MaterialDir);
            EnsureFolder(TextureDir);
            string texPath = $"{TextureDir}/T_BlobShadow.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                var t = RuntimeMaterials.CreateRadialTexture(128, 0.3f);
                File.WriteAllBytes(texPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }
            string path = $"{MaterialDir}/M_BlobShadow.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = RuntimeMaterials.CreateUnlitTransparent(Palette.MidnightPlum, tex, "M_BlobShadow");
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public const string CutoutDir = Root + "/Art/Illustrations/Cutouts";

        /// <summary>Cutout material for a PNG in Art/Illustrations/Cutouts (import settings fixed for paper sprites).</summary>
        public static Material Cutout(string textureName)
        {
            string texPath = $"{CutoutDir}/{textureName}.png";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            EnsureFolder(MaterialDir);
            string path = $"{MaterialDir}/M_{textureName}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = RuntimeMaterials.CreateCutout(tex, "M_" + textureName);
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public const string FxDir = Root + "/Art/Illustrations/FX";

        public static Material FxMaterial(string textureName)
        {
            string texPath = $"{FxDir}/{textureName}.png";
            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp))
            {
                imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            EnsureFolder(Root + "/Materials/FX");
            string path = $"{Root}/Materials/FX/M_{textureName}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = RuntimeMaterials.CreateUnlitCutout(tex, "M_" + textureName); AssetDatabase.CreateAsset(m, path); }
            return m;
        }

        public static Presentation.FXLibrary FxLibrary()
        {
            // M7: the library lives in Resources so runtime presenters (cards, player feel) can load it
            EnsureFolder(Root + "/Resources/DreamRush");
            string path = Root + "/Resources/DreamRush/FXLibrary.asset";
            string oldPath = Root + "/ScriptableObjects/FX/FXLibrary.asset";
            if (AssetDatabase.LoadAssetAtPath<Presentation.FXLibrary>(path) == null && AssetDatabase.LoadAssetAtPath<Presentation.FXLibrary>(oldPath) != null)
                AssetDatabase.MoveAsset(oldPath, path); // keep the GUID so scene references survive
            var lib = AssetDatabase.LoadAssetAtPath<Presentation.FXLibrary>(path);
            if (lib == null) { lib = ScriptableObject.CreateInstance<Presentation.FXLibrary>(); AssetDatabase.CreateAsset(lib, path); }
            lib.exclaim = FxMaterial("FX_Exclaim"); lib.sweat = FxMaterial("FX_Sweat"); lib.dizzy = FxMaterial("FX_DizzyStars");
            lib.poof = FxMaterial("FX_Poof"); lib.tired = FxMaterial("FX_Tired");
            lib.flash = FxMaterial("FX_Flash"); lib.glueSplat = FxMaterial("FX_GlueSplat"); lib.dust = FxMaterial("FX_Dust");
            lib.heart = FxMaterial("FX_Heart"); lib.confetti = FxMaterial("FX_Confetti"); lib.ouch = FxMaterial("FX_Ouch");
            lib.decoyStandee = FxMaterial("FX_DecoyStandee");
            EditorUtility.SetDirty(lib);
            return lib;
        }

        public static Boss.ChairAIConfig ChairConfig()
        {
            EnsureFolder(Root + "/ScriptableObjects/Boss");
            string path = Root + "/ScriptableObjects/Boss/ChairAIConfig.asset";
            var c = AssetDatabase.LoadAssetAtPath<Boss.ChairAIConfig>(path);
            if (c == null) { c = ScriptableObject.CreateInstance<Boss.ChairAIConfig>(); AssetDatabase.CreateAsset(c, path); }
            return c;
        }

        public static PlayerMotorConfig PlayerConfig()
        {
            EnsureFolder(ConfigDir);
            string path = $"{ConfigDir}/PlayerMotorConfig.asset";
            var c = AssetDatabase.LoadAssetAtPath<PlayerMotorConfig>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<PlayerMotorConfig>();
                AssetDatabase.CreateAsset(c, path);
            }
            return c;
        }
    }
}
