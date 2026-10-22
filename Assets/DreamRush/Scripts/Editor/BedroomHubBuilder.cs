using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Hub;
using DreamRush.Interaction;
using DreamRush.Look;
using DreamRush.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// THE NIGHT ROOM — the bedroom hub rebuilt as the game's lobby (docs/LOBBY_DESIGN.md). A cut-away toy diorama
    /// (back walls N + W, open S + E with a toy fence) floating in the dream sky on a three-layer cake of clouds.
    /// Every prop is a Blender model (build_bedroom_kit.py); walls/floor use world-space UV so the wallpaper density is
    /// constant. LobbyThemeDirector re-colours sky, light and walls by the real time of day. All gameplay pieces from
    /// the graybox survive (bed + poses, dream jar, lamp, shelf, tiny chair, posters, door, window) so the hub flow and
    /// its tests are unchanged. Camera shots / focus targets for the lobby menu: LobbyShot_* / LobbyFocus_*.
    /// Rebuild: DreamRush/Build/Bedroom Hub.
    /// </summary>
    public static class BedroomHubBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/BedroomHub.unity";
        const string ModelDir = "Assets/DreamRush/Art/Bedroom/Models";
        const string TexDir = "Assets/DreamRush/Art/Bedroom/Textures";
        const float RoomX = 5f, RoomZ = 4f, WallH = 3.8f, WallT = 0.25f;
        static Dictionary<string, Material> M;

        static Color H(int hex) => new Color(((hex >> 16) & 255) / 255f, ((hex >> 8) & 255) / 255f, (hex & 255) / 255f);

        static Texture2D Tex(string name)
        {
            string path = $"{TexDir}/{name}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.wrapMode != TextureWrapMode.Repeat || imp.anisoLevel < 8))
            { imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 8; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Mat(string name, Color c, float rim = 0.12f, float spec = 0f, Color? emission = null, string tex = null, float worldUV = 0f)
        {
            var m = ArtKit.Mat(name, c, null, null, rim, spec: spec, rampSoft: 0.32f);
            if (tex != null) { m.SetTexture("_BaseMap", Tex(tex)); m.SetFloat("_BaseMapStrength", 1f); }
            m.SetFloat("_WorldUV", worldUV);
            m.SetColor("_Emission", emission ?? Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Dictionary<string, Material> Materials()
        {
            return new Dictionary<string, Material>
            {
                ["M_Kit_BR_Wood"] = Mat("M_Kit_BR_Wood", H(0xE0A77C)),
                ["M_Kit_BR_WoodDark"] = Mat("M_Kit_BR_WoodDark", H(0x8E5238)),
                ["M_Kit_BR_Cream"] = Mat("M_Kit_BR_Cream", H(0xFFF2DC)),
                ["M_Kit_BR_Duvet"] = Mat("M_Kit_BR_Duvet", H(0x6C7BEA), rim: 0.2f),
                ["M_Kit_BR_DuvetB"] = Mat("M_Kit_BR_DuvetB", H(0xFFC94A)),
                ["M_Kit_BR_Pillow"] = Mat("M_Kit_BR_Pillow", H(0xFFFFFF), rim: 0.2f),
                ["M_Kit_BR_Red"] = Mat("M_Kit_BR_Red", H(0xE8343E), spec: 0.25f),
                ["M_Kit_BR_Leaf"] = Mat("M_Kit_BR_Leaf", H(0x5DBB4F)),
                ["M_Kit_BR_Gold"] = Mat("M_Kit_BR_Gold", H(0xF2C14E), spec: 0.6f),
                ["M_Kit_BR_Shade"] = Mat("M_Kit_BR_Shade", H(0xFFC97A), emission: H(0xFFB25A) * 1.3f),
                ["M_Kit_BR_Book1"] = Mat("M_Kit_BR_Book1", H(0x4F7BE8)),
                ["M_Kit_BR_Book2"] = Mat("M_Kit_BR_Book2", H(0xF5B730)),
                ["M_Kit_BR_Book3"] = Mat("M_Kit_BR_Book3", H(0x55B97A)),
                ["M_Kit_BR_Book4"] = Mat("M_Kit_BR_Book4", H(0xA46BE0)),
                ["M_Kit_BR_Paper"] = Mat("M_Kit_BR_Paper", H(0xFFFDF5)),
                ["M_Kit_BR_Glass"] = Mat("M_Kit_BR_Glass", H(0xBFE8FF), rim: 0.5f, spec: 0.7f),
                ["M_Kit_BR_Memory"] = Mat("M_Kit_BR_Memory", H(0xFFD86B), emission: H(0xFFC44A) * 2.2f),
                ["M_Kit_BR_Trim"] = Mat("M_Kit_BR_Trim", H(0xFFF7EE)),
                ["M_Kit_BR_Curtain"] = Mat("M_Kit_BR_Curtain", H(0x8C6CE0), rim: 0.2f),
                ["M_Kit_BR_Rug1"] = Mat("M_Kit_BR_Rug1", H(0xF07C8A)),
                ["M_Kit_BR_Rug2"] = Mat("M_Kit_BR_Rug2", H(0xFFD9A0)),
                ["M_Kit_BR_Rug3"] = Mat("M_Kit_BR_Rug3", H(0x7A63D9)),
                ["M_Kit_BR_Plush"] = Mat("M_Kit_BR_Plush", H(0x4A3F5C)),
                ["M_Kit_BR_Ink"] = Mat("M_Kit_BR_Ink", H(0x1A1229)),
                ["M_Kit_BR_Yellow"] = Mat("M_Kit_BR_Yellow", H(0xFFD400), spec: 0.3f),
                ["M_Kit_BR_Moon"] = Mat("M_Kit_BR_Moon", H(0xFFF1B8), emission: H(0xFFE9A0) * 0.9f),
                ["M_Kit_BR_Cloud"] = Mat("M_Kit_BR_Cloud", H(0xFFFFFF), rim: 0.45f),
                ["M_Kit_BR_Cake1"] = Mat("M_Kit_BR_Cake1", H(0xFFC4D6), rim: 0.2f),
                ["M_Kit_BR_Cake2"] = Mat("M_Kit_BR_Cake2", H(0xC7B3FF), rim: 0.2f),
                ["M_Kit_BR_Cake3"] = Mat("M_Kit_BR_Cake3", H(0x9CC9FF), rim: 0.2f),
                ["M_Kit_BR_Wallpaper"] = Mat("M_Kit_BR_Wallpaper", Color.white, rim: 0f, tex: "T_BR_Wallpaper", worldUV: 1.6f),
                ["M_BR_Wainscot"] = Mat("M_BR_Wainscot", H(0xF3EEFF), rim: 0f, tex: "T_BR_Wainscot", worldUV: 0.9f),
                ["M_BR_Floor"] = Mat("M_BR_Floor", Color.white, rim: 0f, tex: "T_BR_Floor", worldUV: 2.4f),
                ["M_BR_Poster1"] = Mat("M_BR_Poster1", Color.white, rim: 0f, tex: "T_BR_Poster1"),
                ["M_BR_Poster2"] = Mat("M_BR_Poster2", Color.white, rim: 0f, tex: "T_BR_Poster2"),
                ["M_BR_Poster3"] = Mat("M_BR_Poster3", Color.white, rim: 0f, tex: "T_BR_Poster3"),
                ["M_BR_Photo"] = Mat("M_BR_Photo", Color.white, rim: 0f, tex: "T_BR_Photo"),
            };
        }

        static GameObject LoadModel(string name)
        {
            string path = $"{ModelDir}/{name}.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[Hub] missing model " + path); return null; }
            bool dirty = false;
            if (mi.bakeAxisConversion || mi.importAnimation || mi.importCameras || mi.importLights || mi.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; dirty = true;
            }
            foreach (var kv in M)
            {
                if (!kv.Key.StartsWith("M_Kit_BR")) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
                if (!mi.GetExternalObjectMap().TryGetValue(id, out var ex) || ex != kv.Value) { mi.AddRemap(id, kv.Value); dirty = true; }
            }
            if (dirty) mi.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        /// <summary>Places a kit model; yaw 0 = its front faces +Z. outline in px (0 = none).</summary>
        static Transform Place(Transform parent, string name, Vector3 pos, float yaw, float scale = 1f, float outline = 1.8f, bool shadows = true)
        {
            var src = LoadModel(name);
            if (src == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (outline > 0f) DreamLook.AddOutline(go, outline);
            if (System.Array.IndexOf(Solid, name) >= 0) AutoBox(go.transform);
            return go.transform;
        }

        // furniture the follow camera must not slide into (walk mode: the camera ended up inside the plant)
        static readonly string[] Solid = { "PLANT_Pot", "WARDROBE", "SHELF_Books", "DESK", "TOY_Rocket", "TOY_Sheep", "TOY_Train", "NIGHTSTAND", "JAR_Dream" };

        static void AutoBox(Transform t)
        {
            var rs = t.GetComponentsInChildren<MeshRenderer>(); if (rs.Length == 0) return;
            Vector3 mn = Vector3.one * 1e9f, mx = -Vector3.one * 1e9f;
            foreach (var r in rs)
            {
                if (r.name == "InkOutline") continue;
                var b = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = t.InverseTransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                    mn = Vector3.Min(mn, c); mx = Vector3.Max(mx, c);
                }
            }
            var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = (mn + mx) * 0.5f; bc.size = (mx - mn) * 0.92f;
        }

        static void SolidBox(Transform t, Vector3 center, Vector3 size)
        {
            var bc = t.gameObject.AddComponent<BoxCollider>(); bc.center = center; bc.size = size;
        }

        [MenuItem("DreamRush/Build/Bedroom Hub")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode before building scenes."); return; }
            DreamLookSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M = Materials();
            var root = new GameObject("NIGHT_ROOM").transform;

            // ================================================================ diorama base + floor + walls
            Place(root, "DIORAMA_Base", Vector3.zero, 0f, 1f, outline: 0f);
            var floor = Box(root, "ENV_Floor", new Vector3(0, -0.04f, 0), new Vector3(RoomX * 2 + 0.3f, 0.08f, RoomZ * 2 + 0.3f), M["M_BR_Floor"]);
            var wallMat = M["M_Kit_BR_Wallpaper"]; var wains = M["M_BR_Wainscot"]; var trim = M["M_Kit_BR_Trim"];
            float wz = RoomZ + WallT / 2, wx = -RoomX - WallT / 2;
            const float winX = 0.4f, winY = 2.25f, plateBottom = 0.75f, plateW = 3.0f;
            // north wall with the round window panel
            float leftEnd = winX - plateW / 2, rightStart = winX + plateW / 2;
            Box(root, "ENV_WallN_L", new Vector3((wx - WallT / 2 + leftEnd) / 2, WallH / 2, wz), new Vector3(leftEnd - (wx - WallT / 2), WallH, WallT), wallMat);
            Box(root, "ENV_WallN_R", new Vector3((rightStart + RoomX) / 2, WallH / 2, wz), new Vector3(RoomX - rightStart, WallH, WallT), wallMat);
            Box(root, "ENV_WallN_Below", new Vector3(winX, plateBottom / 2, wz), new Vector3(plateW, plateBottom, WallT), wallMat);
            Box(root, "ENV_WallN_Above", new Vector3(winX, (plateBottom + 3f + WallH) / 2, wz), new Vector3(plateW, WallH - plateBottom - 3f, WallT), wallMat);
            Place(root, "WALLPLATE_Round", new Vector3(winX, plateBottom, wz), 180f, 1f, outline: 0f);
            Place(root, "WINDOW_Round", new Vector3(winX, winY, RoomZ - 0.02f), 180f, 1f, outline: 1.6f);
            // west wall
            Box(root, "ENV_WallW", new Vector3(wx, WallH / 2, (RoomZ + WallT - RoomZ) / 2), new Vector3(WallT, WallH, RoomZ * 2 + WallT), wallMat);
            // wainscot (0.8 m), rail, baseboard, crown
            const float wsH = 0.8f;
            Box(root, "ENV_WainscotN", new Vector3(-0.0f, wsH / 2, RoomZ - 0.03f), new Vector3(RoomX * 2, wsH, 0.06f), wains).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_WainscotW", new Vector3(-RoomX + 0.03f, wsH / 2, 0), new Vector3(0.06f, wsH, RoomZ * 2), wains).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_RailN", new Vector3(0, wsH, RoomZ - 0.05f), new Vector3(RoomX * 2, 0.07f, 0.1f), trim).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_RailW", new Vector3(-RoomX + 0.05f, wsH, 0), new Vector3(0.1f, 0.07f, RoomZ * 2), trim).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_BaseN", new Vector3(0, 0.07f, RoomZ - 0.06f), new Vector3(RoomX * 2, 0.14f, 0.12f), trim).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_BaseW", new Vector3(-RoomX + 0.06f, 0.07f, 0), new Vector3(0.12f, 0.14f, RoomZ * 2), trim).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_CrownN", new Vector3(-0.12f, WallH + 0.06f, wz), new Vector3(RoomX * 2 + WallT * 2 + 0.1f, 0.14f, WallT + 0.12f), trim).GetComponent<Collider>().enabled = false;
            Box(root, "ENV_CrownW", new Vector3(wx, WallH + 0.06f, 0.06f), new Vector3(WallT + 0.12f, 0.14f, RoomZ * 2 + WallT + 0.1f), trim).GetComponent<Collider>().enabled = false;
            // open edges: toy fence + invisible walls
            for (float x = -RoomX + 0.6f; x < RoomX; x += 1f) Place(root, "FENCE_Toy", new Vector3(x, 0, -RoomZ + 0.12f), 0f, 1f, outline: 1.4f);
            for (float z = -RoomZ + 0.6f; z < RoomZ - 0.4f; z += 1f) Place(root, "FENCE_Toy", new Vector3(RoomX - 0.12f, 0, z), 90f, 1f, outline: 1.4f);
            var bounds = new GameObject("ENV_InvisibleEdges").transform; bounds.SetParent(root, false);
            SolidBox(bounds, new Vector3(0, 1.5f, -RoomZ - 0.05f), new Vector3(RoomX * 2 + 1, 3f, 0.3f));
            SolidBox(bounds, new Vector3(RoomX + 0.05f, 1.5f, 0), new Vector3(0.3f, 3f, RoomZ * 2 + 1));

            // ================================================================ bed (play) — headboard against the north wall
            var bedRoot = Group(root, "PROP_Bed");
            bedRoot.SetPositionAndRotation(new Vector3(3.05f, 0, RoomZ - 1.45f), Quaternion.Euler(0, 180, 0));
            var bedModel = Place(bedRoot, "BED_Apple", bedRoot.position, 180f, 1f, outline: 2f);
            var bedCol = bedRoot.gameObject.AddComponent<BoxCollider>(); bedCol.center = new Vector3(0, 0.42f, 0); bedCol.size = new Vector3(1.95f, 0.84f, 2.6f);
            var headCol = new GameObject("HeadboardCollider").transform; headCol.SetParent(bedRoot, false); SolidBox(headCol, new Vector3(0, 1.1f, -1.36f), new Vector3(2.1f, 2.2f, 0.25f));
            var bed = bedRoot.gameObject.AddComponent<BedInteractable>();
            bed.SetPrompt("GO TO SLEEP");
            // bed-local: -Z = headboard, +Z = foot (towards the room)
            bed.sleepPose = Pose(bedRoot, "SleepPose", new Vector3(0, 0.74f, -0.05f), Quaternion.identity);
            bed.pillowCamera = LookPose(bedRoot, "PillowCamera", new Vector3(-1.4f, 2.1f, 0.15f), new Vector3(0, 0.9f, -0.5f));
            bed.wakePose = Pose(bedRoot, "WakePose", new Vector3(0, 0.05f, 1.85f), Quaternion.identity);
            BuildBedUpgradeVisuals(bedRoot);
            var zzz = Place(root, "PROP_ZZZ", bedRoot.position + new Vector3(-0.6f, 2.6f, -0.3f), 200f, 0.5f, outline: 1.2f, shadows: false);
            if (zzz != null) { var f = zzz.gameObject.AddComponent<LobbyFloat>(); f.bobHeight = 0.12f; f.spin = 0f; }

            // ================================================================ dream jar (bed upgrades) at the foot of the bed
            var jar = Group(root, "PROP_DreamJar");
            jar.position = new Vector3(1.75f, 0, 0.75f);
            Place(jar, "JAR_Dream", jar.position, 200f, 1.15f, outline: 1.6f);
            var jarLight = NewLight(jar, "JarGlow", new Vector3(0, 0.45f, 0), LightType.Point, H(0xFFD27A), 0.9f, 1.8f, false);
            var jarI = jar.gameObject.AddComponent<MenuInteractable>();
            jarI.kind = MenuKind.BedUpgrade; jarI.SetPrompt("UPGRADE BED (DREAM JAR)");
            jarI.focusPose = LookPose(jar, "FocusPose", new Vector3(-1.5f, 1.5f, -1.4f), new Vector3(1.0f, 0.6f, 1.6f));
            var jarCol = jar.gameObject.AddComponent<CapsuleCollider>(); jarCol.center = new Vector3(0, 0.35f, 0); jarCol.radius = 0.26f; jarCol.height = 0.72f;

            // ================================================================ nightstand + mushroom lamp (settings)
            var stand = Place(root, "NIGHTSTAND", new Vector3(4.45f, 0, RoomZ - 0.6f), 180f);
            if (stand != null) SolidBox(stand, new Vector3(0, 0.32f, 0), new Vector3(0.72f, 0.64f, 0.6f));
            var lamp = Group(root, "PROP_NightLamp");
            lamp.position = new Vector3(4.45f, 0.64f, RoomZ - 0.6f);
            Place(lamp, "LAMP_Mushroom", lamp.position, 180f, 1f, outline: 1.6f, shadows: false);
            var lampLight = NewLight(lamp, "LampLight", new Vector3(-0.1f, 0.38f, -0.12f), LightType.Point, H(0xFFB86A), 2.0f, 7.5f, true);
            var lampI = lamp.gameObject.AddComponent<MenuInteractable>();
            lampI.kind = MenuKind.Settings; lampI.SetPrompt("SETTINGS (LAMP)");
            lampI.focusPose = LookPose(lamp, "FocusPose", new Vector3(-1.6f, 0.9f, -1.3f), new Vector3(0, 0.4f, 0));
            var lampCol = lamp.gameObject.AddComponent<CapsuleCollider>(); lampCol.center = new Vector3(0, 0.35f, 0); lampCol.radius = 0.3f; lampCol.height = 0.75f;

            // ================================================================ bookshelf (diary) on the west wall
            var shelf = Group(root, "PROP_Bookshelf");
            shelf.SetPositionAndRotation(new Vector3(-RoomX + 0.32f, 0, 1.9f), Quaternion.Euler(0, 90, 0));
            Place(shelf, "SHELF_Books", shelf.position, 90f, 1f, outline: 1.6f);
            SolidBox(shelf, new Vector3(0, 1.25f, 0), new Vector3(2.0f, 2.5f, 0.55f));
            var shelfI = shelf.gameObject.AddComponent<MenuInteractable>();
            shelfI.kind = MenuKind.DreamDiary; shelfI.SetPrompt("DREAM DIARY (BOOKSHELF)");
            shelfI.focusPose = LookPose(shelf, "FocusPose", new Vector3(0.6f, 1.6f, 2.6f), new Vector3(0, 1.4f, 0));

            // ================================================================ wardrobe, plant, desk + tiny chair, door
            var wardrobe = Place(root, "WARDROBE", new Vector3(-2.9f, 0, RoomZ - 0.42f), 180f);
            if (wardrobe != null) SolidBox(wardrobe, new Vector3(0, 1.2f, 0), new Vector3(1.3f, 2.4f, 0.7f));
            var plant = Place(root, "PLANT_Pot", new Vector3(-4.45f, 0, RoomZ - 0.5f), 30f, 1.3f);
            if (plant != null) SolidBox(plant, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 0.6f, 0.5f));
            var desk = Place(root, "DESK", new Vector3(-RoomX + 0.5f, 0, -1.3f), 90f);
            if (desk != null) SolidBox(desk, new Vector3(0, 0.45f, 0), new Vector3(1.9f, 0.9f, 0.9f));
            NewLight(root, "DeskLight", new Vector3(-RoomX + 0.75f, 1.3f, -1.95f), LightType.Point, H(0xFFD9A0), 1.1f, 3.2f, false);
            var tiny = Group(root, "PROP_TinyChair");
            tiny.SetPositionAndRotation(new Vector3(-3.55f, 0, -1.15f), Quaternion.Euler(0, -95, 0));
            BuildTinyChair(tiny, M["M_Kit_BR_Wood"]);
            var tinyI = tiny.gameObject.AddComponent<WobbleInteractable>(); tinyI.SetPrompt("POKE THE LITTLE CHAIR");
            var tinyCol = tiny.gameObject.AddComponent<BoxCollider>(); tinyCol.center = new Vector3(0, 0.35f, 0); tinyCol.size = new Vector3(0.5f, 0.7f, 0.5f);
            var door = Group(root, "PROP_Door");
            door.SetPositionAndRotation(new Vector3(-RoomX + 0.02f, 0, -3.0f), Quaternion.Euler(0, 90, 0));
            Place(door, "DOOR_Arch", door.position, 90f, 1f, outline: 1.4f);
            var doorI = door.gameObject.AddComponent<MessageInteractable>();
            doorI.SetPrompt("OPEN DOOR"); doorI.title = "IT'S PAST BEDTIME";
            doorI.body = "เสียงแม่จากข้างล่าง: \"เข้านอนได้แล้วนะ เจ้าเมล็ดน้อย!\"\nประตูไม่ยอมเปิด แต่เตียงดูนุ่มมากเลย";
            var doorCol = door.gameObject.AddComponent<BoxCollider>(); doorCol.center = new Vector3(0, 1.1f, 0); doorCol.size = new Vector3(1.2f, 2.2f, 0.2f);

            // ================================================================ rug, toys
            Place(root, "RUG_Round", new Vector3(0.3f, 0, 0.1f), 0f, 1f, outline: 0f, shadows: false);
            Place(root, "TOY_Rocket", new Vector3(-1.35f, 0, RoomZ - 0.55f), 160f, 1.2f);
            Place(root, "TOY_Sheep", new Vector3(1.55f, 0, 2.05f), 230f, 1.3f);
            Place(root, "TOY_Train", new Vector3(1.0f, 0, -1.05f), 35f, 1.25f);
            Place(root, "TOY_Ball", new Vector3(-2.3f, 0, -2.7f), 0f, 1f);
            var blocks = Group(root, "PROP_ToyBlocks");
            ArtKit.Prop(blocks, "PROP_ToyBlock", new Vector3(-2.6f, 0, 0.9f), 20f, Vector3.one * 0.55f, outline: true);
            ArtKit.Prop(blocks, "PROP_ToyBlock", new Vector3(-2.15f, 0, 1.15f), 55f, Vector3.one * 0.5f, outline: true);
            ArtKit.Prop(blocks, "PROP_ToyBlock", new Vector3(-2.4f, 0.5f, 1.0f), 80f, Vector3.one * 0.45f, outline: true);

            // ================================================================ posters / photo / window (story beats)
            Poster(root, "PROP_Poster_DreamRush", M["M_BR_Poster2"], new Vector3(-RoomX + 0.02f, 2.25f, 0.2f), 90f, new Vector2(1.25f, 1.67f),
                "DREAM RUSH — LIVE!", "โปสเตอร์คอนเสิร์ตในฝัน: \"LIVE TONIGHT IN YOUR DREAMS\"\nมีคนเขียนตัวเล็ก ๆ ไว้ที่มุมว่า \"อย่าลืมเก้าอี้\"");
            Poster(root, "PROP_Poster_AppleAstronaut", M["M_BR_Poster1"], new Vector3(-1.45f, 2.35f, RoomZ - 0.02f), 180f, new Vector2(1.0f, 1.33f),
                "APPLE ASTRONAUT", "\"แอปเปิลลูกแรกบนดวงจันทร์!\"\nมีคนวาดเก้าอี้ตัวจิ๋วไว้ที่หน้าต่างจรวด");
            Poster(root, "PROP_Poster_Wanted", M["M_BR_Poster3"], new Vector3(4.55f, 2.55f, RoomZ - 0.02f), 180f, new Vector2(0.75f, 1.0f),
                "WANTED: THE CHAIR", "เก้าอี้ที่ไม่ยอมให้ใครนั่ง\nรางวัล: งีบหลับนุ่ม ๆ 1 ครั้ง");
            Poster(root, "PROP_FamilyPortrait", M["M_BR_Photo"], new Vector3(-RoomX + 0.03f, 2.1f, -1.3f), 90f, new Vector2(0.9f, 0.68f),
                "FAMILY PORTRAIT", "แม่ พ่อ ปู่ และเรา — แอปเปิลสี่ลูกหยีตาสู้แดด\nปู่ได้นั่งเก้าอี้ตัวที่นุ่มที่สุดเสมอ", frame: true);
            var windowI = Group(root, "PROP_Window").gameObject.AddComponent<MessageInteractable>();
            windowI.transform.position = new Vector3(winX, 1.1f, RoomZ - 0.4f);
            windowI.SetPrompt("LOOK OUTSIDE"); windowI.title = "THE DREAM SKY";
            windowI.body = "นอกหน้าต่างไม่ใช่เมืองแอปเปิลแล้ว — ห้องลอยอยู่กลางท้องฟ้าแห่งความฝัน\nดวงจันทร์ดูง่วง ๆ และเมฆก็นุ่มเหมือนหมอน";

            // ================================================================ the dream sky around the diorama
            var sky = Group(root, "SKY_Props");
            var rnd = new System.Random(7);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
                float r = 11f + (float)rnd.NextDouble() * 9f;
                var c = Place(sky, "CLOUD_Puff", new Vector3(Mathf.Sin(a) * r, -3.5f + (float)rnd.NextDouble() * 9f, Mathf.Cos(a) * r), (float)rnd.NextDouble() * 360f, 1.2f + (float)rnd.NextDouble() * 1.6f, outline: 0f, shadows: false);
                if (c != null) { var f = c.gameObject.AddComponent<LobbyFloat>(); f.bobHeight = 0.4f; f.spin = 0f; f.orbitDegPerSec = 1.5f; }
            }
            var moon = Place(sky, "MOON_Crescent", new Vector3(1.5f, 9f, 34f), 180f, 5.5f, outline: 0f, shadows: false);
            if (moon != null) { moon.rotation = Quaternion.Euler(0, 180, -25); var f = moon.gameObject.AddComponent<LobbyFloat>(); f.bobHeight = 0.5f; f.spin = 0f; }
            var stars = Group(sky, "Stars");
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f; float r = 8f + (float)rnd.NextDouble() * 14f;
                var st = ArtKit.Prop(stars, "PROP_GalStar", new Vector3(Mathf.Sin(a) * r, 2f + (float)rnd.NextDouble() * 9f, Mathf.Cos(a) * r), (float)rnd.NextDouble() * 360f, Vector3.one * (0.25f + (float)rnd.NextDouble() * 0.35f));
                if (st != null) { var f = st.gameObject.AddComponent<LobbyFloat>(); f.bobHeight = 0.3f; f.spin = 30f; }
            }
            foreach (var (pl, pos, sc) in new[] { ("PLANET_Book", new Vector3(-16f, 6f, 18f), 0.32f), ("PLANET_Toy", new Vector3(18f, 3f, 12f), 0.25f), ("PLANET_Dreamland", new Vector3(-20f, -2f, -6f), 0.3f) })
            {
                var p = ArtKit.Prop(sky, pl, pos, 0f, Vector3.one * sc);
                if (p != null) { var f = p.gameObject.AddComponent<LobbyFloat>(); f.bobHeight = 0.6f; f.spin = 4f; }
            }

            // ================================================================ light, sky, post
            var sun = new GameObject("Sun_Key").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
            var daySky = AssetDatabase.LoadAssetAtPath<Material>(ArtKit.MatDir + "/M_Sky_Lobby_Day.mat");
            if (daySky == null)
            {
                daySky = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/DreamRush/Shaders/FantasySky.shader")) { name = "M_Sky_Lobby_Day" };
                AssetDatabase.CreateAsset(daySky, ArtKit.MatDir + "/M_Sky_Lobby_Day.mat");
            }
            var nightSky = GalaxySelectBuilder.NebulaSky("M_Sky_Lobby_Night", H(0x070A26), H(0x3A2470), H(0x0C0A2A), H(0xD04BC8), H(0x00B4CC), 0.95f);
            var director = new GameObject("LobbyThemeDirector").AddComponent<LobbyThemeDirector>();
            director.sun = sun; director.daySky = daySky; director.nightSky = nightSky;
            director.lamps.Add(lampLight); director.lamps.Add(jarLight);
            director.wallMaterials.Add(wallMat); director.wallMaterials.Add(wains);
            director.floorMaterials.Add(M["M_BR_Floor"]);
            if (moon != null) director.nightOnly.Add(moon.gameObject);
            director.nightOnly.Add(stars.gameObject);
            RenderSettings.skybox = nightSky;
            Post(root);

            // ================================================================ player, camera, lobby shots
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var spawn = Pose(root, "PlayerSpawn", new Vector3(0.3f, 0.05f, -0.4f), Quaternion.Euler(0, 160, 0));
            Pose(root, "LobbyPipSpot", new Vector3(1.05f, 0.05f, 0.1f), Quaternion.Euler(0, 200, 0));   // by the foot of the bed: in the DREAM and BED shots
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var cam = PlayerRigBuilder.BuildHubCamera(player);
            cam.distance = 4.6f; cam.defaultPitch = 22f; cam.pitchMax = 55f;
            var c0 = cam.GetComponent<Camera>();
            c0.clearFlags = CameraClearFlags.Skybox; c0.farClipPlane = 400f;
            cam.transform.position = spawn.position + new Vector3(0, 2.4f, -4.2f);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = cam.transform;
            EditorUtility.SetDirty(motor);

            var shots = Group(root, "LobbyShots");
            // subjects sit left of centre (the menu owns the right third, the logo the bottom-left)
            Shot(shots, "DREAM", new Vector3(-0.35f, 1.45f, -2.05f), new Vector3(1.05f, 0.62f, 0.1f), 0.1f);   // Pip is the hero: centre frame, bed behind, clear of the logo
            Shot(shots, "WALK", new Vector3(10.5f, 7.0f, -12.5f), new Vector3(0f, 0.4f, 0.3f));
            Shot(shots, "BED", new Vector3(4.4f, 1.75f, -1.7f), new Vector3(1.9f, 0.55f, 1.0f));
            Shot(shots, "DIARY", new Vector3(-1.1f, 1.85f, -1.1f), new Vector3(-4.6f, 1.3f, 1.9f));
            Shot(shots, "SETTINGS", new Vector3(1.9f, 1.75f, 0.5f), new Vector3(4.45f, 0.9f, 3.4f));
            Shot(shots, "QUIT", new Vector3(0.9f, 1.15f, -2.3f), new Vector3(0.4f, 2.5f, 4.0f));
            Focus(shots, "DREAM", bedRoot.position + Vector3.up * 0.8f);
            Focus(shots, "WALK", new Vector3(9.5f, 1f, -11.5f));
            Focus(shots, "BED", jar.position);
            Focus(shots, "DIARY", shelf.position + Vector3.up * 1.2f);
            Focus(shots, "SETTINGS", lamp.position);
            Focus(shots, "QUIT", new Vector3(winX, winY, RoomZ));

            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>();
            ctx.kind = SceneKind.Hub;
            ctx.defaultSpawn = spawn;

            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath, first: true);
            Debug.Log("[DreamRush] Night Room lobby built: " + ScenePath);
        }

        /// <summary>Camera pose that frames <paramref name="subject"/> left of centre (aim point pushed right by 28% of the distance).</summary>
        static void Shot(Transform parent, string key, Vector3 pos, Vector3 subject, float offset = 0.28f)
        {
            var t = new GameObject("LobbyShot_" + key).transform; t.SetParent(parent, false);
            Vector3 dir = subject - pos; Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 aim = subject + right * dir.magnitude * offset;
            t.SetPositionAndRotation(pos, Quaternion.LookRotation(aim - pos));
        }

        static void Focus(Transform parent, string key, Vector3 pos)
        {
            var t = new GameObject("LobbyFocus_" + key).transform; t.SetParent(parent, false); t.position = pos;
        }

        static Light NewLight(Transform parent, string name, Vector3 localPos, LightType type, Color c, float intensity, float range, bool shadows)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false); l.transform.localPosition = localPos;
            l.type = type; l.color = c; l.intensity = intensity; l.range = range; l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        static void Poster(Transform root, string name, Material m, Vector3 pos, float yaw, Vector2 size, string title, string body, bool frame = false)
        {
            var g = Group(root, name);
            g.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Art"; q.transform.SetParent(g, false);
            q.transform.localPosition = new Vector3(0, 0, 0.012f); q.transform.localRotation = Quaternion.Euler(0, 180, 0);
            q.transform.localScale = new Vector3(size.x, size.y, 1);
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
            q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            if (frame)
            {
                LocalPrim(g, PrimitiveType.Cube, "Frame", new Vector3(0, 0, 0.0f), new Vector3(size.x + 0.12f, size.y + 0.12f, 0.03f), M["M_Kit_BR_Gold"]);
            }
            else
            {   // two pieces of tape on the top corners
                for (int s = -1; s <= 1; s += 2)
                {
                    var tp = LocalPrim(g, PrimitiveType.Cube, "Tape", new Vector3(s * (size.x / 2 - 0.08f), size.y / 2 - 0.02f, 0.016f), new Vector3(0.22f, 0.07f, 0.005f), M["M_Kit_BR_Cream"]);
                    tp.transform.localRotation = Quaternion.Euler(0, 0, s * 35f);
                }
            }
            var col = g.gameObject.AddComponent<BoxCollider>(); col.size = new Vector3(size.x, size.y, 0.1f); col.isTrigger = false; col.center = new Vector3(0, 0, 0.05f);
            var mi = g.gameObject.AddComponent<MessageInteractable>();
            mi.SetPrompt(frame ? "LOOK AT PHOTO" : "READ POSTER"); mi.title = title; mi.body = body;
        }

        static void Post(Transform root)
        {
            const string path = "Assets/DreamRush/Settings/PP_Lobby.asset";
            GrayboxAssets.EnsureFolder("Assets/DreamRush/Settings");
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.components.RemoveAll(x => x == null);   // v1 never saved its components (all post silently fell back to the URP default)
            T Get<T>() where T : VolumeComponent { if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); } c.active = true; return c; }
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(0f); ca.contrast.Override(28f); ca.saturation.Override(24f);
            var tm = Get<Tonemapping>(); tm.mode.Override(TonemappingMode.Neutral);   // the lit room goes past 1.0: Neutral keeps the floor from blowing out
            ca.postExposure.Override(0.28f);
            var bl = Get<Bloom>(); bl.intensity.Override(0.42f); bl.threshold.Override(1.05f); bl.scatter.Override(0.65f);
            var vg = Get<Vignette>(); vg.intensity.Override(0.28f); vg.smoothness.Override(0.5f); vg.color.Override(H(0x0B0618));
            var ab = Get<ChromaticAberration>(); ab.intensity.Override(0.08f);
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX"); go.transform.SetParent(root, false);
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 5; v.sharedProfile = p;
        }

        /// <summary>Bed looks (GDD §21.5): toggled by BedVisuals from the save. Bed-local: -Z headboard, +Z foot.</summary>
        static void BuildBedUpgradeVisuals(Transform bed)
        {
            var cloud = M["M_Kit_BR_Cloud"];
            Vector3[][] puffs =
            {
                new[] { new Vector3(0.95f, 0.8f, 0.9f), new Vector3(0.98f, 0.8f, 0.1f), new Vector3(0.95f, 0.8f, -0.7f) },
                new[] { new Vector3(-0.95f, 0.8f, 0.9f), new Vector3(-0.98f, 0.8f, 0.1f), new Vector3(-0.95f, 0.8f, -0.7f) },
                new[] { new Vector3(0.35f, 0.8f, 1.28f), new Vector3(-0.35f, 0.8f, 1.28f), new Vector3(0f, 0.88f, 1.25f) },
            };
            for (int lv = 0; lv < 3; lv++)
            {
                var g = Group(bed, "Upgrade_Mattress_" + (lv + 1));
                foreach (var p in puffs[lv])
                    LocalPrim(g, PrimitiveType.Sphere, "Puff", p, new Vector3(0.34f, 0.28f, 0.62f), cloud);
            }
            var pillow = Group(bed, "Upgrade_Pillow");
            LocalPrim(pillow, PrimitiveType.Sphere, "LuckyPillow", new Vector3(0.3f, 1.02f, -0.8f), new Vector3(0.6f, 0.2f, 0.42f), M["M_Kit_BR_DuvetB"]).transform.localRotation = Quaternion.Euler(8, 18, 0);
            var blanket = Group(bed, "Upgrade_Blanket");
            LocalPrim(blanket, PrimitiveType.Cube, "Quilt", new Vector3(0, 0.87f, 0.4f), new Vector3(2.0f, 0.05f, 1.6f), M["M_Kit_BR_Curtain"]);
            string[] patch = { "M_Kit_BR_DuvetB", "M_Kit_BR_Rug1", "M_Kit_BR_Leaf", "M_Kit_BR_Cream" };
            for (int i = 0; i < 6; i++)
                LocalPrim(blanket, PrimitiveType.Cube, "Patch", new Vector3(-0.6f + (i % 3) * 0.6f, 0.9f, 0.0f + (i / 3) * 0.75f), new Vector3(0.38f, 0.012f, 0.38f), M[patch[i % 4]]);
            bed.gameObject.AddComponent<Progression.BedVisuals>();
        }

        static GameObject LocalPrim(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.localPosition = localPos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static void BuildTinyChair(Transform p, Material wood)
        {
            // same design as the boss, half size, no face ... yet (foreshadowing §7.8)
            if (ChairCharacterSetup.ModelExists && ChairCharacterSetup.Setup() != null)
            {
                var m = ChairCharacterSetup.Instantiate(p, "BOSS_Chair_Model_Tiny", withFace: false);
                if (m != null) { m.localScale = Vector3.one * 0.42f; return; }
            }
            LocalPrim(p, PrimitiveType.Cube, "Seat", new Vector3(0, 0.32f, 0), new Vector3(0.42f, 0.05f, 0.42f), wood);
            foreach (var (x, z) in new[] { (-0.17f, -0.17f), (0.17f, -0.17f), (-0.17f, 0.17f), (0.17f, 0.17f) })
                LocalPrim(p, PrimitiveType.Cube, "Leg", new Vector3(x, 0.15f, z), new Vector3(0.05f, 0.3f, 0.05f), wood);
            LocalPrim(p, PrimitiveType.Cube, "Back", new Vector3(0, 0.55f, -0.19f), new Vector3(0.42f, 0.42f, 0.05f), wood);
        }

        // ---------------------------------------------------------------- helpers
        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>World-positioned box with collider.</summary>
        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = parent.InverseTransformPoint(pos);
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static Transform Pose(Transform parent, string name, Vector3 localPos, Quaternion localRot)
        {
            var t = Group(parent, name);
            t.localPosition = localPos;
            t.localRotation = localRot;
            return t;
        }

        static Transform LookPose(Transform parent, string name, Vector3 localPos, Vector3 localTarget)
        {
            var t = Group(parent, name);
            t.localPosition = localPos;
            t.rotation = Quaternion.LookRotation(parent.TransformPoint(localTarget) - t.position, Vector3.up);
            return t;
        }
    }
}
