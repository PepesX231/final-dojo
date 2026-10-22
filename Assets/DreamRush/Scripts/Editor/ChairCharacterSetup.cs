using DreamRush.Core;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// Import + material setup for BOSS_Chair (ArtSource/Blender/Scripts/build_chair_boss.py).
    /// Flat hierarchy: Body, Leg_FL/FR/BL/BR (pivot at hip), Eye_L/R, Pupil_L/R, Brow_L/R, Face.
    /// </summary>
    public static class ChairCharacterSetup
    {
        public const string Folder = "Assets/DreamRush/Art/Characters/Chair";
        public const string ModelPath = Folder + "/BOSS_Chair.fbx";
        public const string WoodTexturePath = Folder + "/T_Chair_Wood.png";
        public const string MaterialFolder = "Assets/DreamRush/Materials/Characters";

        static readonly (string name, Color color, float smooth)[] Materials =
        {
            ("M_Chair_Wood", Color.white, 0.22f),               // painted wood texture
            ("M_Chair_WoodDark", Palette.Hex(0x9A5C36), 0.2f),
            ("M_Chair_EyeWhite", Palette.Hex(0xF7F1E4), 0.4f),
            ("M_Chair_Ink", Palette.Hex(0x2B2233), 0.45f),
            ("M_Chair_Blush", Palette.Hex(0xF29C8C), 0.15f),
            ("M_Chair_Cushion", Color.white, 0.1f),             // gingham texture (sit target)
        };

        public static bool ModelExists => AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null;

        [MenuItem("DreamRush/Build/Chair Boss Import")]
        public static GameObject Setup()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) { Debug.LogWarning("[DreamRush] Chair model missing at " + ModelPath); return null; }

            var ti = (TextureImporter)AssetImporter.GetAtPath(WoodTexturePath);
            if (ti != null && (ti.maxTextureSize != 512 || !ti.sRGBTexture))
            {
                ti.maxTextureSize = 512; ti.sRGBTexture = true; ti.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodTexturePath);

            GrayboxAssets.EnsureFolder(MaterialFolder);
            var mi = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            mi.globalScale = 1f; mi.useFileScale = true;
            mi.importCameras = false; mi.importLights = false;
            mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.importNormals = ModelImporterNormals.Import;
            mi.bakeAxisConversion = false; // exported with Apply Transform (blender-asset-pipeline skill)

            foreach (var (name, color, smooth) in Materials)
            {
                string path = $"{MaterialFolder}/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) { m = RuntimeMaterials.CreateLit(color, smooth, name); AssetDatabase.CreateAsset(m, path); }
                ArtKit.MakeToon(m, rim: name.Contains("Eye") || name.Contains("Ink") ? 0f : 0.22f);
                m.SetColor("_BaseColor", color);
                if (name == "M_Chair_Wood" && tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
                if (name == "M_Chair_Cushion") { var g = ArtKit.Tex("T_Gingham"); m.SetTexture("_BaseMap", g); m.SetTextureScale("_BaseMap", new Vector2(2.5f, 2.5f)); }
                EditorUtility.SetDirty(m);
                mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), m);
            }
            mi.SaveAndReimport();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }

        /// <summary>Instantiates the chair model under <paramref name="parent"/> (no colliders, no shadows from face bits).</summary>
        public static Transform Instantiate(Transform parent, string name, bool withFace)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            {
                bool face = t.name.StartsWith("Eye_") || t.name.StartsWith("Pupil_") || t.name.StartsWith("Brow_") || t.name == "Face";
                if (face && !withFace) t.gameObject.SetActive(false);
                var r = t.GetComponent<Renderer>();
                if (r != null && face) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go.transform;
        }

        [MenuItem("DreamRush/Validate/Chair Model")]
        public static string Validate()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) return "missing";
            var go = (GameObject)Object.Instantiate(model);
            var b = new Bounds(go.transform.position, Vector3.zero);
            int tris = 0;
            var sb = new System.Text.StringBuilder();
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) tris += mf.sharedMesh.triangles.Length / 3;
            foreach (var t in go.GetComponentsInChildren<Transform>())
                if (t != go.transform) sb.Append($"[{t.name} lp={t.localPosition:F2} lr={t.localEulerAngles:F0} mat={(t.GetComponent<Renderer>() ? t.GetComponent<Renderer>().sharedMaterial.name : "-")}] ");
            Object.DestroyImmediate(go);
            string s = $"bounds min={b.min:F3} max={b.max:F3} tris={tris} {sb}";
            Debug.Log("[DreamRush] Validate chair: " + s);
            return s;
        }
    }
}
