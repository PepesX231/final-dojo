using System.Text;
using DreamRush.Core;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// Import + material setup for CHR_AppleChild (exported by ArtSource/Blender/Scripts/build_apple_game.py).
    /// Blender materials are remapped to URP materials owned by the project, so the look is controlled here.
    /// </summary>
    public static class AppleCharacterSetup
    {
        public const string Folder = "Assets/DreamRush/Art/Characters/AppleChild";
        public const string ModelPath = Folder + "/CHR_AppleChild.fbx";
        public const string SkinTexturePath = Folder + "/T_AppleChild_Skin.png";
        public const string MaterialFolder = "Assets/DreamRush/Materials/Characters";

        // sRGB colours converted from the source .blend (linear) and nudged to the palette where noted.
        static readonly (string name, Color color, float smooth)[] Materials =
        {
            ("M_Apple_Skin", Color.white, 0.32f),                      // baked texture (soft, not wet)
            ("M_Apple_Limb", Palette.Hex(0xF2C79A), 0.25f),
            ("M_Apple_Glove", Palette.Hex(0xF7F1E4), 0.2f),
            ("M_Apple_EyeWhite", Palette.Hex(0xEBEADB), 0.35f),
            ("M_Apple_Pupil", Palette.Hex(0x1A1520), 0.5f),
            ("M_Apple_PupilShine", Palette.Hex(0xFFFFFF), 0.6f),
            // v2 model (build_apple_v2.py)
            ("M_Apple_Cheek", Palette.Hex(0xFF8C98), 0.3f),
            ("M_Apple_Tongue", Palette.Hex(0xF2566A), 0.3f),
            ("M_Apple_Shoe", Palette.Hex(0x9C86F2), 0.3f),
            ("M_Apple_Sole", Palette.Hex(0xFBF5EA), 0.3f),
            ("M_Apple_Ink", Palette.Hex(0x4D0812), 0.2f),
            ("M_Apple_Leaf", Palette.Hex(0x6FAA5E), 0.3f),             // palette Leaf Green family
            ("M_Apple_Stem", Palette.Hex(0x824D29), 0.2f),
            // v3 model (build_apple_v3.py): iris, leaf vein, star accents, dream-pillow backpack
            ("M_Apple_PupilIris", Palette.Hex(0x8C3A1E), 0.5f),
            ("M_Apple_LeafVein", Palette.Hex(0xC8E68A), 0.3f),
            ("M_Apple_Accent", Palette.Hex(0xFFD23A), 0.4f),
            ("M_Apple_Pack", Palette.Hex(0x7FA0F5), 0.25f),
            ("M_Apple_PackTrim", Palette.Hex(0xFBF5EA), 0.25f),
            // v4 limbs: pajama sleeves / shorts, striped socks
            ("M_Apple_Sleeve", Palette.Hex(0x9C8CF7), 0.25f),
            ("M_Apple_Sock", Palette.Hex(0xFBF8F2), 0.25f),
        };

        public static bool ModelExists => AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null;

        [MenuItem("DreamRush/Build/Apple Character Import")]
        public static GameObject Setup()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogWarning("[DreamRush] Apple model missing at " + ModelPath); return null; }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SkinTexturePath);
            var ti = (TextureImporter)AssetImporter.GetAtPath(SkinTexturePath);
            if (ti != null && (ti.maxTextureSize != 1024 || !ti.sRGBTexture))
            {
                ti.maxTextureSize = 1024; ti.sRGBTexture = true; ti.SaveAndReimport();
            }

            GrayboxAssets.EnsureFolder(MaterialFolder);
            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.importNormals = ModelImporterNormals.Import;
            mi.bakeAxisConversion = false; // FBX is exported with Apply Transform; see blender-asset-pipeline skill

            foreach (var (name, color, smooth) in Materials)
            {
                string path = $"{MaterialFolder}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = RuntimeMaterials.CreateLit(color, smooth, name);
                    AssetDatabase.CreateAsset(m, path);
                }
                bool body = name == "M_Apple_Skin" || name == "M_Apple_Limb" || name == "M_Apple_Glove" || name == "M_Apple_Shoe" || name == "M_Apple_Sole" || name == "M_Apple_Pack" || name == "M_Apple_Sleeve" || name == "M_Apple_Sock";
                ArtKit.MakeToon(m, rim: body ? 0.35f : 0f);
                // v2 skin texture is already painted apple-red (streaks + warm shoulders): use it as-is
                m.SetColor("_BaseColor", name == "M_Apple_Skin" ? Color.white : color);
                m.SetFloat("_BaseMapStrength", 1f);
                if (name == "M_Apple_Skin") m.SetFloat("_SpecStrength", 0.35f);   // glossy apple highlight
                if (name == "M_Apple_PupilShine" || name == "M_Apple_EyeWhite") m.SetFloat("_AmbientStrength", 0.6f);
                if (name == "M_Apple_Skin" && tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
                EditorUtility.SetDirty(m);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), m);
            }
            mi.SaveAndReimport();
            AssetDatabase.SaveAssets();
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Debug.Log("[DreamRush] Apple character imported. " + Describe(model));
            return model;
        }

        /// <summary>Logs hierarchy, bounds and facing so axis/scale can be verified (asset quality gate).</summary>
        [MenuItem("DreamRush/Validate/Apple Character")]
        public static string Validate()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            string r = model == null ? "missing" : Describe(model);
            Debug.Log("[DreamRush] Validate apple: " + r);
            return r;
        }

        static string Describe(GameObject model)
        {
            var go = (GameObject)Object.Instantiate(model);
            var sb = new StringBuilder();
            var b = new Bounds(go.transform.position, Vector3.zero);
            int tris = 0;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) tris += mf.sharedMesh.triangles.Length / 3;
            sb.Append($"bounds min={b.min:F3} max={b.max:F3} size={b.size:F3} tris={tris}; ");
            foreach (var t in go.GetComponentsInChildren<Transform>())
            {
                if (t == go.transform) continue;
                var r = t.GetComponent<Renderer>();
                sb.Append($"[{t.name} lp={t.localPosition:F3} lr={t.localEulerAngles:F0} ls={t.localScale:F2}" +
                          (r != null ? $" c={r.bounds.center:F2} mats={string.Join("/", System.Array.ConvertAll(r.sharedMaterials, m => m ? m.name : "null"))}" : "") + "] ");
            }
            Object.DestroyImmediate(go);
            return sb.ToString();
        }
    }
}
