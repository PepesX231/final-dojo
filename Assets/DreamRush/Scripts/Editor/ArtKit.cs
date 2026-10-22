using System.Collections.Generic;
using DreamRush.Core;
using DreamRush.Look;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// The locked visual standard (docs/VISUAL_RECONSTRUCTION.md) as reusable building blocks:
    /// DreamToon materials, the living-room prop kit, paper floors/walls, rugs and 2D cutouts.
    /// The Art Target scene and the real level builders both use ONLY these, so the look stays cohesive.
    /// </summary>
    public static class ArtKit
    {
        public const string TexDir = "Assets/DreamRush/Art/Environment/Textures";
        public const string PropDir = "Assets/DreamRush/Art/Environment/Props";
        public const string CutoutDir = "Assets/DreamRush/Art/Illustrations/Cutouts";
        public const string MatDir = "Assets/DreamRush/Materials/Look";
        public const string MeshDir = "Assets/DreamRush/Art/Environment/Meshes";

        /// <summary>Apple skin lift toward palette Apple Red (#E85D65) for screen readability; the baked texture is very dark.</summary>
        public static readonly Color AppleSkinLift = new Color(0.97f, 0.27f, 0.31f, 1f); // ≈ palette Apple Red, texture kept at 35% (gentle variation, §5.4)

        static Shader _toon;
        public static Shader ToonShader => _toon != null ? _toon : (_toon = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DreamRush/Shaders/DreamToon.shader"));

        public static Texture2D Tex(string name, bool repeat = true)
        {
            string path = $"{TexDir}/{name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                var wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (imp.wrapMode != wrap || imp.anisoLevel < 4 || !imp.alphaIsTransparency)
                {
                    imp.wrapMode = wrap; imp.anisoLevel = 4; imp.alphaIsTransparency = true; imp.mipmapEnabled = true;
                    imp.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void MakeToon(Material m, float rim = 0f)
        {
            if (m == null || ToonShader == null) return;
            if (m.shader != ToonShader) m.shader = ToonShader;
            m.SetFloat("_RimStrength", rim);
            m.SetColor("_ShadowColor", new Color(0.5f, 0.42f, 0.68f));
            m.SetFloat("_AmbientStrength", 0.35f);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Creates or updates a DreamToon material asset.</summary>
        public static Material Mat(string name, Color color, string texture = null, Vector2? tiling = null, float rim = 0f,
                                   bool alphaClip = false, bool doubleSided = false, float spec = 0f, float rampSoft = 0.35f)
        {
            GrayboxAssets.EnsureFolder(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(ToonShader) { name = name }; AssetDatabase.CreateAsset(m, path); }
            MakeToon(m, rim);
            m.SetColor("_BaseColor", color);
            var t = texture != null ? Tex(texture, !alphaClip) : null;
            m.SetTexture("_BaseMap", t);
            m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            m.SetFloat("_SpecStrength", spec);
            m.SetFloat("_RampSoftness", rampSoft);
            m.SetFloat("_AlphaClip", alphaClip ? 1 : 0);
            if (alphaClip) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", doubleSided ? 0 : 2);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material TransparentTex(string name, string tex)
        {
            GrayboxAssets.EnsureFolder(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = Core.RuntimeMaterials.CreateUnlitTransparent(Color.white, Tex(tex, false), name);
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetTexture("_BaseMap", Tex(tex, false));
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Unlit(string name, Color hdr)
        {
            GrayboxAssets.EnsureFolder(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", hdr);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ palette of materials (one per role)
        public static Dictionary<string, Material> Materials()
        {
            var cream = Palette.WarmCream;
            return new Dictionary<string, Material>
            {
                // paper world
                ["PageFloor"] = Mat("M_Look_PageFloor", new Color(1f, 0.97f, 0.92f), "T_PageFloor", null, rampSoft: 0.6f),
                ["PageEdge"] = Mat("M_Look_PageEdge", Color.white, "T_PageEdge", rampSoft: 0.5f),
                ["PageBack"] = Mat("M_Look_PageBack", Color.Lerp(cream, Color.white, 0.2f), null, rampSoft: 0.5f),
                ["Wallpaper"] = Mat("M_Look_Wallpaper", Color.white, "T_Wallpaper", rampSoft: 0.6f),
                ["BookCloth"] = Mat("M_Look_BookCloth", Color.white, "T_BookCloth", new Vector2(6, 6)),
                ["Rug"] = Mat("M_Look_Rug", Color.white, "T_RugRound", alphaClip: true, rampSoft: 0.6f),
                // kit (FBX material names)
                ["M_Kit_Fabric"] = Mat("M_Kit_Fabric", Color.white, "T_FabricRose", new Vector2(2, 2), rim: 0.08f),
                ["M_Kit_FabricLight"] = Mat("M_Kit_FabricLight", new Color(1.12f, 1.1f, 1.08f), "T_FabricRose", new Vector2(2, 2), rim: 0.08f),
                ["M_Kit_WoodDark"] = Mat("M_Kit_WoodDark", Palette.Hex(0x7A4E34)),
                ["M_Kit_PaintedBlue"] = Mat("M_Kit_PaintedBlue", Palette.Hex(0xF2CF7E), spec: 0.15f),   // butter-yellow painted table (keeps blues for cushions)
                ["M_Kit_WoodLight"] = Mat("M_Kit_WoodLight", Palette.Hex(0xDDB083)),
                ["M_Kit_Brass"] = Mat("M_Kit_Brass", Palette.Hex(0xD9A860), spec: 0.4f),
                ["M_Kit_LampShade"] = Mat("M_Kit_LampShade", Color.white, "T_LampShade", doubleSided: true, rampSoft: 0.8f),
                ["M_Kit_Bulb"] = Unlit("M_Kit_Bulb", new Color(2.2f, 1.7f, 1.0f)),
                ["M_Kit_ToyBlock"] = Mat("M_Kit_ToyBlock", Color.white, "T_ToyBlock", rim: 0.15f),
                ["M_Kit_Cushion"] = Mat("M_Kit_Cushion", Palette.Hex(0x5DB4E2), "T_Felt", new Vector2(1, 1), rim: 0.12f, rampSoft: 0.55f),
                ["M_Kit_CushionButton"] = Mat("M_Kit_CushionButton", Palette.Hex(0xF7EEDC)),
                // Dream Galaxy kit (build_galaxy_kit.py) — saturated storybook planets, strong rim for space light
                ["M_Kit_GalPaper"] = Mat("M_Kit_GalPaper", Palette.Hex(0xFBF1DD), rim: 0.3f, rampSoft: 0.45f),
                ["M_Kit_GalRed"] = Mat("M_Kit_GalRed", Palette.Hex(0xE5404F), rim: 0.3f),
                ["M_Kit_GalLav"] = Mat("M_Kit_GalLav", Palette.Hex(0xA98CF0), rim: 0.3f),
                ["M_Kit_GalBlue"] = Mat("M_Kit_GalBlue", Palette.Hex(0x5FB0EE), rim: 0.3f, spec: 0.2f),
                ["M_Kit_GalYellow"] = Mat("M_Kit_GalYellow", Palette.Hex(0xF6CB5C), rim: 0.3f),
                ["M_Kit_GalGreen"] = Mat("M_Kit_GalGreen", Palette.Hex(0x74C37A), rim: 0.3f),
                ["M_Kit_GalWood"] = Mat("M_Kit_GalWood", Palette.Hex(0x9C6542), rim: 0.2f),
                ["M_Kit_GalCloud"] = Mat("M_Kit_GalCloud", Palette.Hex(0xF3EEFF), rim: 0.35f, rampSoft: 0.6f),
                ["M_Kit_GalOrange"] = Mat("M_Kit_GalOrange", Palette.Hex(0xF5963A), rim: 0.3f),
            };
        }

        // ------------------------------------------------------------------ props
        public static GameObject LoadProp(string name)
        {
            string path = $"{PropDir}/{name}.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[ArtKit] missing prop " + path); return null; }
            bool dirty = false;
            if (mi.bakeAxisConversion || mi.importAnimation || mi.importCameras || mi.importLights || mi.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                dirty = true;
            }
            var mats = Materials();
            foreach (var kv in mats)
            {
                if (!kv.Key.StartsWith("M_Kit_")) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
                if (!mi.GetExternalObjectMap().TryGetValue(id, out var existing) || existing != kv.Value) { mi.AddRemap(id, kv.Value); dirty = true; }
            }
            if (dirty) mi.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Places a prop. <paramref name="size"/> (optional) rescales the 1×1×1 cushion to a gameplay box.</summary>
        public static Transform Prop(Transform parent, string name, Vector3 pos, float yaw = 0f, Vector3? scale = null, bool outline = false)
        {
            var src = LoadProp(name);
            if (src == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = scale ?? Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (outline) DreamLook.AddOutline(go, 2.2f);
            return go.transform;
        }

        /// <summary>Debug: lists sub-objects and materials of a prop FBX after import (DevBridge exec).</summary>
        public static string DescribeProp(string name)
        {
            var src = LoadProp(name);
            if (src == null) return "missing";
            var sb = new System.Text.StringBuilder();
            foreach (var r in src.GetComponentsInChildren<Renderer>())
                sb.Append($"[{r.name}: {string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m == null ? "null" : m.name + "/" + m.shader.name))}] ");
            var mi = (ModelImporter)AssetImporter.GetAtPath($"{PropDir}/{name}.fbx");
            foreach (var kv in mi.GetExternalObjectMap()) sb.Append($"map {kv.Key.name}->{(kv.Value ? kv.Value.name : "null")}; ");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ paper world
        public static Mesh SaveMesh(Mesh m, string key)
        {
            GrayboxAssets.EnsureFolder(MeshDir);
            string path = $"{MeshDir}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(m, existing); Object.DestroyImmediate(m); return existing; }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Open picture-book floor: two page slabs (gap = spine) on a plum cloth cover. Top surface at local y = 0.</summary>
        public static Transform Book(Transform parent, string name, Vector3 center, float width, float depth, float pageThickness = 0.42f, Vector3? bookmarkAt = null)
        {
            var mats = Materials();
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = center;
            float half = width / 2f - 0.006f;
            for (int s = -1; s <= 1; s += 2)
            {
                var mesh = SaveMesh(PaperGeometry.Slab(half, depth, pageThickness, 0.5f, 4f, new Vector2(center.x + s * half / 2f, center.z), 0.04f, s + 7),
                    $"Book_{name}_{(s < 0 ? "L" : "R")}");
                var page = new GameObject(s < 0 ? "Page_Left" : "Page_Right");
                page.transform.SetParent(root, false);
                page.transform.localPosition = new Vector3(s * (half / 2f + 0.006f), 0, 0);
                page.transform.localRotation = Quaternion.Euler(0, 0, s * 0.18f); // pages dip gently toward the spine
                page.AddComponent<MeshFilter>().sharedMesh = mesh;
                page.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats["PageFloor"], mats["PageEdge"], mats["PageBack"] };
            }
            // soft valley where the pages meet (printed shadow, not a hole)
            var spine = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(spine.GetComponent<Collider>());
            spine.name = "SpineShadow";
            spine.transform.SetParent(root, false);
            spine.transform.localPosition = new Vector3(0, 0.008f, 0);
            spine.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spine.transform.localScale = new Vector3(0.75f, depth - 0.2f, 1);
            var sr = spine.GetComponent<MeshRenderer>();
            sr.sharedMaterial = TransparentTex("M_Look_SpineShadow", "T_SpineShadow");
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var coverMesh = SaveMesh(PaperGeometry.Slab(width + 1.8f, depth + 1.4f, 0.18f, 0.9f, 2f, default, 0f, 3), $"Book_{name}_Cover");
            var cover = new GameObject("Cover");
            cover.transform.SetParent(root, false);
            cover.transform.localPosition = new Vector3(0, -pageThickness - 0.01f, 0);
            cover.AddComponent<MeshFilter>().sharedMesh = coverMesh;
            cover.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats["BookCloth"], mats["BookCloth"], mats["BookCloth"] };
            if (bookmarkAt.HasValue) Bookmark(root, bookmarkAt.Value - center, pageThickness);
            return root;
        }

        /// <summary>A satin ribbon bookmark lying across the page and spilling over the edge into the night (identity cue: "this is a book").</summary>
        static void Bookmark(Transform book, Vector3 localStart, float drop)
        {
            var m = Mat("M_Look_Bookmark", Palette.Hex(0xD9505E), spec: 0.35f, rim: 0.1f);
            var g = new GameObject("Bookmark").transform;
            g.SetParent(book, false);
            g.localPosition = localStart;
            g.localRotation = Quaternion.Euler(0, localStart.z < 0 ? 180f : 0f, 0);   // always spills over the nearest edge
            // segments: flat on the page → over the edge → hanging down, with a forked tail
            (Vector3 p, Vector3 r, float len)[] segs =
            {
                (new Vector3(0, 0.01f, -1.6f), new Vector3(0, 6, 0), 3.2f),
                (new Vector3(0.12f, -0.25f, 0.1f), new Vector3(70, 4, 0), 0.75f),
                (new Vector3(0.16f, -drop - 0.9f, 0.33f), new Vector3(88, 0, 2), 1.4f),
            };
            foreach (var (p, r, len) in segs)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(g, false);
                c.transform.localPosition = p; c.transform.localRotation = Quaternion.Euler(r);
                c.transform.localScale = new Vector3(0.42f, 0.02f, len);
                c.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
        }

        /// <summary>A run of pop-up wall pages from <paramref name="a"/> to <paramref name="b"/>. The outside of the room is on the LEFT of a→b (e.g. north wall: west→east).</summary>
        public static Transform WallRun(Transform parent, string name, Vector3 a, Vector3 b, float height = 1.7f, float panelWidth = 4.2f, int seed = 0)
        {
            var mats = Materials();
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            float len = Vector3.Distance(a, b);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / panelWidth));
            float w = len / n;
            var dir = (b - a).normalized;
            var rot = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up); // local +Z = outside (left of a→b); printed front (−Z) faces the play area
            var rnd = new System.Random(seed);
            for (int i = 0; i < n; i++)
            {
                float h = height + (float)(rnd.NextDouble() - 0.5) * 0.18f;
                var mesh = SaveMesh(PaperGeometry.Panel(w - 0.06f, h, 0.06f, 0.07f, 0.28f, 2f), $"Wall_{name}_{i}");
                var p = new GameObject($"Page_{i}");
                p.transform.SetParent(root, false);
                p.transform.localPosition = a + dir * (w * (i + 0.5f));
                p.transform.localRotation = rot * Quaternion.Euler((float)(rnd.NextDouble() - 0.5) * 3f, 0, (float)(rnd.NextDouble() - 0.5) * 1.5f);
                p.AddComponent<MeshFilter>().sharedMesh = mesh;
                p.AddComponent<MeshRenderer>().sharedMaterials = new[] { mats["Wallpaper"], mats["PageEdge"], mats["PageBack"] };
            }
            return root;
        }

        public static Transform Rug(Transform parent, Vector3 pos, float diameter)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Rug";
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos + Vector3.up * 0.012f;
            q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            q.transform.localScale = new Vector3(diameter, diameter, 1);
            q.GetComponent<MeshRenderer>().sharedMaterial = Materials()["Rug"];
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        /// <summary>
        /// 2D cutout (GDD §5.7 Type A) lit by the same DreamToon light. <paramref name="mode"/>: "wall" (flat, attached),
        /// "stand" (stands on a paper tab, Y-billboard limited), "floor" (lies flat).
        /// </summary>
        public static Transform Cutout(Transform parent, string texName, Vector3 pos, float height, float yaw = 0f, string mode = "stand")
        {
            string path = $"{CutoutDir}/{texName}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp))
            { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning("[ArtKit] missing cutout " + path); return null; }
            var m = Mat("M_Cut_" + texName, Color.white, null, alphaClip: true, doubleSided: true, rampSoft: 0.9f);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Cutoff", 0.4f);
            float aspect = (float)tex.width / tex.height;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = texName;
            q.transform.SetParent(parent, false);
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
            q.transform.localScale = new Vector3(height * aspect, height, 1);
            if (mode == "floor")
            {
                q.transform.localPosition = pos + Vector3.up * 0.015f;
                q.transform.localRotation = Quaternion.Euler(90, yaw, 0);
                q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                q.transform.localPosition = pos + Vector3.up * (height / 2f);
                q.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                if (mode == "stand") q.AddComponent<Dream.YBillboard>().maxAngle = 25f;
                if (mode == "wall") q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return q.transform;
        }
    }
}
