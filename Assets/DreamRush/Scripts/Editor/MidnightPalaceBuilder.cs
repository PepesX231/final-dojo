using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Combat;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Hub;
using DreamRush.Look;
using DreamRush.OpenWorld;
using DreamRush.Palace;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// LEVEL 2 — MIDNIGHT PALACE (docs/LEVEL2_DESIGN.md §5). A 340 m night city where time stopped at 11:59, Persona
    /// style (StylePack: ink + print dots + posterized light, Pip in a phantom-thief mask). Five districts with their own
    /// colour moods around the CLOCK TOWER; three challenges hold the STOLEN HOURS (Rooftop Run E: rails + pads + rings,
    /// Gear Works N: spinning gear stairs over a glass canal, Shadow Ambush W: minion waves); the tower climb (gear spiral →
    /// updraft → rails) leads to a clock-dial arena in the sky where COUNT MIDNIGHT waits. WonderDirector guarantees
    /// something new every ≤ 10 s. Rebuild: DreamRush/Build/Midnight Palace (level 2).
    /// </summary>
    public static partial class MidnightPalaceBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Dream_MidnightPalace.unity";
        const string ModelDir = "Assets/DreamRush/Art/MidnightPalace/Models";
        const string TexDir = "Assets/DreamRush/Art/MidnightPalace/Textures";
        public const float Half = 170f, Pitch = 50f, Street = 12f, ArenaY = 92f;
        static Dictionary<string, Material> M;
        static System.Random R;
        static Color H(int h) => Palette.Hex(h);

        // ================================================================== materials
        static Texture2D Tex(string n)
        {
            string path = $"{TexDir}/{n}.png";
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.wrapMode != TextureWrapMode.Repeat || imp.anisoLevel < 8)) { imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 8; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Mat(string name, Color c, string tex = null, float worldUV = 0f, string emis = null, Color? emission = null, float rim = 0.2f, float spec = 0f, Vector2? tiling = null)
        {
            var m = ArtKit.Mat(name, c, null, null, rim, spec: spec, rampSoft: 0.1f);
            if (tex != null) { m.SetTexture("_BaseMap", Tex(tex)); m.SetFloat("_BaseMapStrength", 1f); m.SetTextureScale("_BaseMap", tiling ?? Vector2.one); }
            m.SetFloat("_WorldUV", worldUV);
            m.SetColor("_Emission", emission ?? Color.black);
            if (emis != null) { m.SetTexture("_EmissionMap", Tex(emis)); m.SetFloat("_EmissionMapStrength", 1f); } else m.SetFloat("_EmissionMapStrength", 0f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Dictionary<string, Material> Materials()
        {
            var m = new Dictionary<string, Material>
            {
                ["Ground"] = Mat("M_MP_Sidewalk", new Color(0.62f, 0.64f, 0.7f), "T_MP_Sidewalk", 3f),
                ["Road"] = Mat("M_MP_Road", Color.white, "T_MP_Road", 0f, tiling: new Vector2(1f, 28f)),
                ["Roof"] = Mat("M_MP_Roof", H(0x231C3E), rim: 0.1f),
                ["Glass"] = Mat("M_MP_Canal", H(0x203A78), rim: 0.8f, spec: 0.9f, emission: H(0x0A1838)),
                ["Plaza"] = Mat("M_MP_Plaza", H(0x2A2250), "T_MP_Sidewalk", 4f),
                ["Rail"] = Mat("M_MP_Rail", H(0x7FF6FF), rim: 0f, emission: H(0x00D8FF) * 2.4f),
                ["RailPost"] = Mat("M_MP_RailPost", H(0x1A1530)),
                ["Gate"] = Mat("M_MP_Gate", H(0xFF2E6A), rim: 0f, emission: H(0xFF1E5A) * 2.2f),
                ["Billboard"] = Mat("M_MP_BillFrame", H(0x14101F), spec: 0.4f),
                ["Card"] = Mat("M_MP_Card", Color.white, "T_MP_Bill0"),
            };
            string[] names = { "A", "B", "C" };
            foreach (var n in names) m["Facade" + n] = Mat("M_MP_Facade" + n, new Color(0.5f, 0.48f, 0.6f), "T_MP_Facade" + n, 11f, "T_MP_Facade" + n + "_E", Color.white * 1.8f, rim: 0.05f);
            for (int i = 0; i < 12; i++) m["Sign" + i] = Mat($"M_MP_Sign{i:00}", Color.white, $"T_MP_Sign{i:00}", 0f, $"T_MP_Sign{i:00}_E", Color.white * 2.2f, rim: 0f);
            for (int i = 0; i < 4; i++) m["Bill" + i] = Mat($"M_MP_Bill{i}", Color.white, $"T_MP_Bill{i}", rim: 0f, emission: Color.white * 0.15f);
            Color[] neon = { H(0xFF2E9A), H(0x00E1FF), H(0xFFD400), H(0x8C6BFF), H(0xFF6A1A), H(0x2EFFB0) };
            for (int i = 0; i < neon.Length; i++) m["Neon" + i] = Mat("M_MP_Neon" + i, neon[i], rim: 0f, emission: neon[i] * 2.4f);
            // kit (Blender) materials
            m["M_Kit_MP_Stone"] = Mat("M_Kit_MP_Stone", H(0x4A3F72));
            m["M_Kit_MP_Brick"] = Mat("M_Kit_MP_Brick", H(0x6B2A48), "T_MP_FacadeC", 4f, "T_MP_FacadeC_E", Color.white * 1.2f);
            m["M_Kit_MP_Dark"] = Mat("M_Kit_MP_Dark", H(0x14101F), spec: 0.3f);
            m["M_Kit_MP_Face"] = Mat("M_Kit_MP_Face", H(0xFFF3D6), emission: H(0xFFE7B0) * 0.9f);
            m["M_Kit_MP_Gold"] = Mat("M_Kit_MP_Gold", H(0xFFC64E), spec: 0.7f, rim: 0.3f);
            m["M_Kit_MP_Roof"] = Mat("M_Kit_MP_Roof", H(0x2C3A86), rim: 0.3f);
            m["M_Kit_MP_Glow"] = Mat("M_Kit_MP_Glow", H(0x9FF6FF), emission: H(0x5FE8FF) * 3f);
            m["M_Kit_MP_Window"] = Mat("M_Kit_MP_Window", H(0xFFD58A), emission: H(0xFFB65A) * 2f);
            m["M_Kit_MP_Wood"] = Mat("M_Kit_MP_Wood", H(0x6A4030));
            m["M_Kit_MP_Red"] = Mat("M_Kit_MP_Red", H(0xE0213F), spec: 0.3f);
            m["M_Kit_MP_Metal"] = Mat("M_Kit_MP_Metal", H(0x8A90A8), spec: 0.5f);
            m["M_Kit_MP_LampGlow"] = Mat("M_Kit_MP_LampGlow", H(0xFFE6A8), emission: H(0xFFD27A) * 3f);
            m["M_Kit_MP_Shard"] = Mat("M_Kit_MP_Shard", H(0xA8F4FF), rim: 0.8f, emission: H(0x4FE0FF) * 2.6f);
            m["M_Kit_MP_MinionBody"] = Mat("M_Kit_MP_MinionBody", H(0xE02450), spec: 0.4f, rim: 0.3f);
            m["M_Kit_MP_Eye"] = Mat("M_Kit_MP_Eye", H(0xFFE14A), emission: H(0xFFD400) * 2.5f);
            m["M_Kit_MP_Mask"] = Mat("M_Kit_MP_Mask", H(0xFAF8F2));
            m["M_Kit_MP_Whale"] = Mat("M_Kit_MP_Whale", H(0x26307A), rim: 0.6f);
            m["M_Kit_MP_WhaleBelly"] = Mat("M_Kit_MP_WhaleBelly", H(0xB8C8FF), emission: H(0x4060C0) * 0.6f);
            m["M_Kit_MP_Paper"] = Mat("M_Kit_MP_Paper", H(0xFFFFFF), rim: 0.3f, emission: H(0x404060));
            m["M_Kit_MP_Tram"] = Mat("M_Kit_MP_Tram", H(0xFFD400), spec: 0.4f);
            // M14 enemy roster (build_palace_enemies.py)
            m["M_Kit_MP_Brute"] = Mat("M_Kit_MP_Brute", H(0x1F3D66), spec: 0.3f, rim: 0.4f);
            m["M_Kit_MP_Brass"] = Mat("M_Kit_MP_Brass", H(0xD39A3E), spec: 0.6f, rim: 0.3f);
            m["M_Kit_MP_EyeRed"] = Mat("M_Kit_MP_EyeRed", H(0xFF2A3A), emission: H(0xFF1030) * 2.6f);
            m["M_Kit_MP_Cuckoo"] = Mat("M_Kit_MP_Cuckoo", H(0x8A3242), rim: 0.3f);
            m["M_Kit_MP_Feather"] = Mat("M_Kit_MP_Feather", H(0x7048E0), rim: 0.5f);
            m["M_Kit_MP_Orange"] = Mat("M_Kit_MP_Orange", H(0xFF8A1F), emission: H(0xFF6A00) * 0.4f);
            m["M_Kit_MP_Pyjama"] = Mat("M_Kit_MP_Pyjama", H(0x8FA8FF), rim: 0.4f);
            // M21 interior kit (build_interior_kit.py): one matte illustrated palette shared with the v3 facades
            m["M_Kit_MP_IntWood"] = Mat("M_Kit_MP_IntWood", H(0xB98E68), rim: 0.06f);
            m["M_Kit_MP_IntWoodDark"] = Mat("M_Kit_MP_IntWoodDark", H(0x7A5238), rim: 0.06f);
            m["M_Kit_MP_IntCream"] = Mat("M_Kit_MP_IntCream", H(0xF1E5CE), rim: 0.06f);
            m["M_Kit_MP_IntTeal"] = Mat("M_Kit_MP_IntTeal", H(0x5E9A93), rim: 0.08f);
            m["M_Kit_MP_IntRose"] = Mat("M_Kit_MP_IntRose", H(0xD98C8C), rim: 0.08f);
            m["M_Kit_MP_IntLilac"] = Mat("M_Kit_MP_IntLilac", H(0xA895D1), rim: 0.08f);
            m["M_Kit_MP_IntMustard"] = Mat("M_Kit_MP_IntMustard", H(0xE2B65A), rim: 0.08f);
            m["M_Kit_MP_IntInk"] = Mat("M_Kit_MP_IntInk", H(0x39374D), rim: 0.05f);
            m["M_Kit_MP_IntLeaf"] = Mat("M_Kit_MP_IntLeaf", H(0x6FA272), rim: 0.1f);
            m["M_Kit_MP_IntGlass"] = Mat("M_Kit_MP_IntGlass", H(0xCFEAF0), rim: 0.5f, spec: 0.6f, emission: H(0x203038));
            m["M_Kit_MP_IntCake"] = Mat("M_Kit_MP_IntCake", H(0xF4C7CF), rim: 0.1f);
            m["M_Kit_MP_IntTatami"] = Mat("M_Kit_MP_IntTatami", H(0xCDBF85), rim: 0.05f);
            m["M_Kit_MP_IntCopper"] = Mat("M_Kit_MP_IntCopper", H(0xC07A4E), rim: 0.2f, spec: 0.4f);
            // M16 city v2: new facade set (the old three tiled everywhere = "the same building")
            CityMaterials(m);
            m["FacadeA"] = m["Concrete"]; m["FacadeB"] = m["Glass"]; m["FacadeC"] = m["Balcony"];
            m["Road"] = Mat("M_MP_Road", Color.white, "T_MP_RoadWet", 0f, tiling: new Vector2(1f, 28f), rim: 0.2f, spec: 0.18f);
            m["RoadX"] = Mat("M_MP_RoadDeck", Color.white, "T_MP_RoadWet", 0f, tiling: new Vector2(1f, 20f), rim: 0.2f, spec: 0.18f);
            // M15 doors: one glowing colour per room type (readable from down the street)
            foreach (RoomType t in System.Enum.GetValues(typeof(RoomType)))
            { var c = BuildingDoor.TypeColor(t); m["Door" + t] = Mat("M_MP_Door" + t, c, rim: 0f, emission: c * 1.15f); }
            return m;
        }

        static GameObject LoadModel(string dir, string name)
        {
            string path = $"{dir}/{name}.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[Palace] missing model " + path); return null; }
            bool dirty = mi.bakeAxisConversion || mi.importAnimation || mi.materialImportMode != ModelImporterMaterialImportMode.ImportStandard;
            mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var kv in M)
            {
                if (!kv.Key.StartsWith("M_Kit_")) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
                if (!mi.GetExternalObjectMap().TryGetValue(id, out var ex) || ex != kv.Value) { mi.AddRemap(id, kv.Value); dirty = true; }
            }
            if (dirty) mi.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();
        static Transform Place(Transform parent, string name, Vector3 pos, float yaw = 0f, float scale = 1f, bool collider = false, string dir = ModelDir)
        {
            if (!_models.TryGetValue(name, out var src) || src == null) { src = LoadModel(dir, name); _models[name] = src; }
            if (src == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (collider) foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            return go.transform;
        }

        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material m, bool collider = true, float yaw = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, Quaternion.Euler(0, yaw, 0)); go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Quad(Transform parent, string name, Vector3 center, Vector2 size, Quaternion rot, Material m)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot); go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static Transform Group(Transform p, string n) { var t = new GameObject(n).transform; t.SetParent(p, false); return t; }
        static float Rf(float a, float b) => a + (float)R.NextDouble() * (b - a);

        // ================================================================== districts
        struct DistrictDef { public string name, thai; public Vector3 c; public string facade; public int neonA, neonB; public float hMin, hMax; public Color fog, sky, eq, dots; }
        static readonly DistrictDef[] Districts =
        {
            new DistrictDef { name = "NEON ALLEY", thai = "ซอยนีออนเซอร์เรียล — ถอดรหัสสัญญาณไฟและเส้นทางลับ", c = new Vector3(0, 0, -112), facade = "B", neonA = 0, neonB = 1, hMin = 14, hMax = 46, fog = Palette.Hex(0x0D0826), sky = Palette.Hex(0x281858), eq = Palette.Hex(0x7A1858), dots = new Color(0.95f, 0.12f, 0.45f, 0.85f) },
            new DistrictDef { name = "CANDY ROOFTOPS", thai = "ดาดฟ้าป๊อปอาร์ต — คำนวณวิถีไถลรางและวงแหวนความเร็ว", c = new Vector3(112, 0, 0), facade = "A", neonA = 0, neonB = 2, hMin = 10, hMax = 32, fog = Palette.Hex(0x1F0924), sky = Palette.Hex(0x4E1646), eq = Palette.Hex(0x9A2848), dots = new Color(1.0f, 0.78f, 0.1f, 0.85f) },
            new DistrictDef { name = "CLOCKWORK CANAL", thai = "คลองปริซึม — วิเคราะห์จังหวะฟันเฟืองย้อนเวลา", c = new Vector3(0, 0, 112), facade = "C", neonA = 2, neonB = 5, hMin = 10, hMax = 28, fog = Palette.Hex(0x051828), sky = Palette.Hex(0x103E5A), eq = Palette.Hex(0x187A78), dots = new Color(0.0f, 0.92f, 1.0f, 0.85f) },
            new DistrictDef { name = "MOONLIT PLAZA", thai = "ลานโมโนลิธ — แก้ปริศนาแผ่นถ่วงน้ำหนักเพื่อปลดผนึกเงา!", c = new Vector3(-112, 0, 0), facade = "B", neonA = 1, neonB = 3, hMin = 14, hMax = 42, fog = Palette.Hex(0x090D2A), sky = Palette.Hex(0x1E2868), eq = Palette.Hex(0x4A2E78), dots = new Color(0.45f, 0.25f, 1.0f, 0.85f) },
        };

        static int DistrictOf(Vector3 p)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < Districts.Length; i++) { float d = (p - Districts[i].c).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            return best;
        }

        /// <summary>Same texel density as the old full-length road (28 tiles over 340 m) for a segment of <paramref name="len"/> m.</summary>
        static Material RoadMat(float len)
        {
            string k = "Road_" + Mathf.RoundToInt(len);
            if (!M.TryGetValue(k, out var m)) M[k] = m = Mat("M_MP_Road_" + Mathf.RoundToInt(len), Color.white, "T_MP_RoadWet", 0f, tiling: new Vector2(1f, 28f * len / 340f), rim: 0.2f, spec: 0.18f);
            return m;
        }

        internal static bool InCanal(Vector3 p) => Mathf.Abs(p.x) < 40f && p.z > 85f && p.z < 150f;

        static bool Reserved(Vector3 c)
        {
            if (Mathf.Abs(c.x) < 46 && Mathf.Abs(c.z) < 46) return true;                       // tower plaza
            if (new Vector2(c.x + 115, c.z).magnitude < 34) return true;                           // ambush plaza
            if (Mathf.Abs(c.x) < 40 && c.z > 85 && c.z < 145) return true;                          // gear works / canal
            if (c.x > 80 && c.x < 160 && Mathf.Abs(c.z) < 40) return true;                           // rooftop run lane
            if (Mathf.Abs(c.x) < 22 || Mathf.Abs(c.z) < 22) return true;                           // the four grand avenues
            if (new Vector2(c.x - 105, c.z + 95).magnitude < 5) return true;                       // M16: the ferris wheel lot
            return false;
        }

        // ================================================================== build
        [MenuItem("DreamRush/Build/Midnight Palace (level 2)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            DreamLookSetup.Ensure();
            StyleSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M = Materials(); _models.Clear();
            R = new System.Random(1159); _doorN = 0; _cacheN = 0;
            var root = new GameObject("MIDNIGHT_PALACE").transform;

            Streets(root);
            CityV2(root);
            var wonders = Group(root, "Wonders");
            var templates = Group(root, "Templates");
            var minion = MinionTemplate(templates);
            var brute = EnemyTemplate<BellBrute>(templates, "BRUTE_Bell", 0f);
            var roller = EnemyTemplate<GearRoller>(templates, "ROLLER_Gear", 0.86f);
            var cuckoo = EnemyTemplate<CuckooSniper>(templates, "CUCKOO_Sniper", 1.1f);
            _enemy = new Dictionary<char, GameObject> { ['T'] = minion, ['B'] = brute, ['G'] = roller, ['C'] = cuckoo };
            var hours = new List<StolenHour>();
            hours.Add(RooftopRun(root));
            hours.Add(GearWorks(root));
            hours.Add(ShadowAmbush(root, minion));
            var amb = root.GetComponentInChildren<AmbushChallenge>();
            amb.bruteTemplate = brute; amb.rollerTemplate = roller; amb.cuckooTemplate = cuckoo;
            amb.waveMix = new[] { "TTT", "TGTC", "BTTG" };   // every wave changes the shape of the fight
            var (gate, arenaCenter, boss) = Tower(root);
            Sky(root, out var moon);
            Squads(root, minion);
            EdgeV2(root);
            StreetLife(root);
            Monorail(root);
            Highway(root, new Vector3(-Half - 14, 0, -120), new Vector3(Half + 14, 0, -120), 9f, "Highway_South", 4);
            Highway(root, new Vector3(-120, 0, -Half - 14), new Vector3(-120, 0, Half + 14), 14f, "Highway_West", 4);
            Holograms(root);
            Signatures(root);
            var interiors = Interiors(root, templates, minion, brute, roller, cuckoo);

            // ---- director, wonders, player
            var level = new GameObject("DreamLevelManager").AddComponent<DreamLevelManager>();
            level.killY = -20f; level.anyOrderCheckpoints = true;
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Vector3 spawn = new Vector3(0, 0.2f, -150f);
            player.transform.SetPositionAndRotation(spawn + Vector3.up * 6f, Quaternion.identity);
            BossArenaBuilder.SetupFighter(player);
            player.AddComponent<GrindController>();
            var tpc = PlayerRigBuilder.BuildHubCamera(player);
            CameraTuning.ApplyFollow(tpc);
            tpc.distance = 5.6f; tpc.sprintExtraDistance = 2f;
            var cam = tpc.GetComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            cam.clearFlags = CameraClearFlags.Skybox; cam.farClipPlane = 900f; cam.nearClipPlane = 0.15f;
            tpc.transform.position = spawn + new Vector3(0, 3, -6);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = tpc.transform; EditorUtility.SetDirty(motor);
            level.player = motor;
            Checkpoint(root, spawn, true);
            new GameObject("DreamArrival").AddComponent<DreamArrival>().player = motor;
            var st = new GameObject("SleepStability").AddComponent<SleepStability>(); st.baseMax = 6;
            new GameObject("DreamHUD").AddComponent<DreamHUD>();
            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>(); ctx.kind = SceneKind.Dream; ctx.music = DreamRush.Audio.Music.Palace;
            var spk = new GameObject("StylePack").AddComponent<StylePack>(); BossArenaBuilder.PhantomStyle(spk);
            // Sonny Boy / Pop-Surrealism Graphic Style: razor cel-shading, deep obsidian ink, vibrant posterized colour planes
            spk.ink = 1f; spk.inkColor = new Color(0.02f, 0.01f, 0.05f); spk.inkWidth = 2.0f; spk.inkFar = 140f;
            spk.halftone = 0.28f; spk.halftoneColor = new Color(0.95f, 0.12f, 0.45f, 0.85f); spk.halftoneScale = 6.0f;
            spk.posterize = 0.32f; spk.saturate = 0.35f;
            spk.celHard = 1f; spk.shadowTint = new Color(0.06f, 0.04f, 0.18f, 0.92f);
            spk.pipRim = new Color(0.0f, 0.95f, 1.0f); spk.pipRimStrength = 0.65f; spk.pipRampSoftness = 0.02f;
            spk.outfit = StylePack.Outfit.None;
            SonnyBoySurrealSkyscape(root);

            var wd = new GameObject("WonderDirector").AddComponent<WonderDirector>();
            wd.planeTemplate = Place(templates, "PAPER_Plane", Vector3.zero)?.gameObject;
            wd.whaleTemplate = Place(templates, "SKY_Whale", Vector3.zero)?.gameObject;
            wd.tramTemplate = Place(templates, "SKY_Tram", Vector3.zero)?.gameObject;
            var card = Quad(templates, "CallingCard", Vector3.zero, new Vector2(0.9f, 0.45f), Quaternion.identity, M["Bill0"]);
            card.GetComponent<MeshRenderer>().sharedMaterial = M["Bill" + 0];
            wd.cardTemplate = card; wd.minionTemplate = minion; wd.moon = moon;
            foreach (var t in new[] { wd.planeTemplate, wd.whaleTemplate, wd.tramTemplate, card }) if (t != null) t.SetActive(false);
            Wonders(wonders, wd);

            var pd = new GameObject("PalaceDirector").AddComponent<PalaceDirector>();
            pd.hours = hours; pd.towerGate = gate; pd.arenaCenter = arenaCenter; pd.arenaRadius = BossArenaBuilder.R; pd.boss = boss;
            foreach (var d in Districts)
                pd.districts.Add(new PalaceDirector.District { name = d.name, thai = d.thai, center = d.c, radius = 80f, fog = d.fog, ambientSky = d.sky, ambientEquator = d.eq, halftone = d.dots });
            new GameObject("PalaceHUD").AddComponent<PalaceHUD>();
            Quests(root, pd, hours);
            pd.districts.Add(new PalaceDirector.District { name = "THE CLOCK TOWER", thai = "หอนาฬิกา — ที่ซ่อนของชั่วโมงที่สิบสอง", center = Vector3.zero, radius = 44f, fog = H(0x2A0710), ambientSky = H(0x581A2C), ambientEquator = H(0x3E0E1A), halftone = new Color(0.35f, 0.02f, 0.08f, 0.85f) });

            Tidy(root);
            PalaceStylePass.DressBuiltScene();
            MarkStatic();
            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DreamRush] Midnight Palace built: " + ScenePath);
        }

        // ================================================================== ground + streets
        static void Streets(Transform root)
        {
            var g = Group(root, "Streets");
            Box(g, "Ground", new Vector3(0, -0.5f, 0), new Vector3(Half * 2 + 40, 1f, Half * 2 + 40), M["Ground"]);
            // streets run between the block columns (blocks are centred at -145 + 50k, 36 m wide → streets at -120 + 50k)
            // M20: roads are built segment by segment so they don't run across the gear-works canal
            var cuts = new List<float> { -Half }; for (float v = -Half + 50; v < Half; v += Pitch) cuts.Add(v); cuts.Add(Half);
            for (float x = -Half + 50; x < Half; x += Pitch)
                for (int i = 0; i + 1 < cuts.Count; i++)
                {
                    float z0 = cuts[i], z1 = cuts[i + 1]; var mid = new Vector3(x, 0.01f, (z0 + z1) / 2f);
                    if (InCanal(mid)) continue;
                    Box(g, "RoadZ", mid, new Vector3(Street, 0.04f, z1 - z0 + Street), RoadMat(z1 - z0 + Street), false);
                }
            for (float z = -Half + 50; z < Half; z += Pitch)
                for (int i = 0; i + 1 < cuts.Count; i++)
                {
                    float x0 = cuts[i], x1 = cuts[i + 1]; var mid = new Vector3((x0 + x1) / 2f, 0.012f, z);
                    if (InCanal(mid)) continue;
                    Box(g, "RoadX", mid, new Vector3(Street, 0.04f, x1 - x0 + Street), RoadMat(x1 - x0 + Street), false, 90f);
                }
            // the four grand avenues to the tower (wide, glowing lane lines)
            for (int k = 0; k < 4; k++)
            {
                float yaw = k * 90f; var rot = Quaternion.Euler(0, yaw, 0);
                for (float d = 46f; d < Half; d += 8f)
                    Box(g, "AvenueLight", rot * new Vector3(0, 0.03f, d), new Vector3(0.5f, 0.05f, 3.5f), M["Neon" + (k % 2 == 0 ? 1 : 0)], false, yaw);
            }
        }

        // ================================================================== procedural blocks
        static void City(Transform root)
        {
            var g = Group(root, "City");
            for (float bx = -Half + 25; bx <= Half - 25; bx += Pitch)
                for (float bz = -Half + 25; bz <= Half - 25; bz += Pitch)
                {
                    var c = new Vector3(bx, 0, bz);
                    if (Reserved(c)) continue;
                    Block(g, c, Pitch - Street - 2f);
                }
        }

        static void Block(Transform parent, Vector3 c, float size)
        {
            var d = Districts[DistrictOf(c)];
            var blk = Group(parent, "Block");
            int n = R.NextDouble() < 0.35 ? 1 : 2;
            float lot = size / n;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var lc = c + new Vector3((i - (n - 1) * 0.5f) * lot, 0, (j - (n - 1) * 0.5f) * lot);
                    float w = lot - Rf(1.5f, 4f), dpt = lot - Rf(1.5f, 4f);
                    float h = Rf(d.hMin, d.hMax) * (n == 1 ? 1.25f : 1f);
                    string fac = R.NextDouble() < 0.7 ? d.facade : (R.NextDouble() < 0.5 ? "A" : "C");
                    Vector3 outw = lc - c;
                    if (n == 1) outw = R.Next(4) switch { 0 => Vector3.forward, 1 => Vector3.back, 2 => Vector3.right, _ => Vector3.left };
                    else outw = R.NextDouble() < 0.5 ? new Vector3(Mathf.Sign(outw.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(outw.z));
                    Building(blk, lc, w, dpt, h, fac, R.NextDouble() < 0.5 ? d.neonA : d.neonB, outw);
                }
            // the alley between the lots of a 2×2 block hides a few minutes
            if (n == 2 && R.NextDouble() < 0.45) Cache(blk, c + Vector3.up * 0.6f, 2);
        }

        static void Building(Transform parent, Vector3 c, float w, float d, float h, string facade, int neon, Vector3? doorSide = null)
        {
            var b = Box(parent, "Building", c + Vector3.up * h / 2, new Vector3(w, h, d), M["Facade" + facade]);
            Box(parent, "Roof", c + Vector3.up * (h + 0.25f), new Vector3(w + 0.6f, 0.5f, d + 0.6f), M["Roof"]);
            // neon edge strips on the roof line
            if (R.NextDouble() < 0.6)
            {
                var nm = M["Neon" + neon];
                Box(parent, "NeonN", c + new Vector3(0, h + 0.55f, d / 2 + 0.31f), new Vector3(w + 0.6f, 0.12f, 0.12f), nm, false);
                Box(parent, "NeonS", c + new Vector3(0, h + 0.55f, -d / 2 - 0.31f), new Vector3(w + 0.6f, 0.12f, 0.12f), nm, false);
                Box(parent, "NeonE", c + new Vector3(w / 2 + 0.31f, h + 0.55f, 0), new Vector3(0.12f, 0.12f, d + 0.6f), nm, false);
                Box(parent, "NeonW", c + new Vector3(-w / 2 - 0.31f, h + 0.55f, 0), new Vector3(0.12f, 0.12f, d + 0.6f), nm, false);
            }
            // a neon sign on one street face
            if (R.NextDouble() < 0.75)
            {
                int side = R.Next(4);
                Vector3 n = side == 0 ? Vector3.forward : side == 1 ? Vector3.back : side == 2 ? Vector3.right : Vector3.left;
                float half = (side < 2 ? d : w) / 2f;
                float sy = Rf(4f, Mathf.Min(h - 2f, 14f));
                bool vertical = R.NextDouble() < 0.35;
                var size = vertical ? new Vector2(1.6f, 4.2f) : new Vector2(5.2f, 1.95f);
                var q = Quad(parent, "Sign", c + n * (half + 0.06f) + Vector3.up * sy, size, Quaternion.LookRotation(-n) * (vertical ? Quaternion.Euler(0, 0, 90) : Quaternion.identity), M["Sign" + R.Next(12)]);
                if (vertical) q.transform.localScale = new Vector3(size.y, size.x, 1);
            }
            // rooftop clutter
            double roll = R.NextDouble();
            Vector3 top = c + Vector3.up * (h + 0.5f);
            if (roll < 0.3) Place(parent, "WATER_Tank", top + new Vector3(Rf(-w / 4, w / 4), 0, Rf(-d / 4, d / 4)), Rf(0, 360));
            else if (roll < 0.55) Place(parent, "AC_Unit", top + new Vector3(Rf(-w / 3, w / 3), 0, Rf(-d / 3, d / 3)), Rf(0, 360), 1.3f);
            if (R.NextDouble() < 0.35) Place(parent, "ANTENNA", top + new Vector3(Rf(-w / 3, w / 3), 0, Rf(-d / 3, d / 3)), 0, 1.2f);
            if (R.NextDouble() < 0.16) Billboard(parent, top, w, d);
            // M15: every city building can be entered, and its roof may hide minutes
            // (M17: doors + walk-in rooms are built by Tower in MidnightPalaceCity)
            if (R.NextDouble() < 0.22) Cache(parent, top + new Vector3(Rf(-w / 4, w / 4), 0.6f, Rf(-d / 4, d / 4)), 3);
        }

        static void Billboard(Transform parent, Vector3 top, float w, float d)
        {
            float yaw = R.Next(4) * 90f;
            var rot = Quaternion.Euler(0, yaw, 0);
            Vector3 c = top + Vector3.up * 5.2f;
            var frame = Box(parent, "BillboardFrame", c - rot * Vector3.forward * 0.15f, new Vector3(12.4f, 6.4f, 0.25f), M["Billboard"], false, yaw);
            Quad(parent, "Billboard", c + rot * Vector3.forward * 0.0f, new Vector2(12f, 6f), rot * Quaternion.Euler(0, 180, 0), M["Bill" + R.Next(4)]);
            for (int s = -1; s <= 1; s += 2) Box(parent, "Leg", top + rot * new Vector3(s * 4f, 1.1f, -0.3f), new Vector3(0.3f, 2.2f, 0.3f), M["Billboard"], false, yaw);
            var l = new GameObject("BillLight").AddComponent<Light>(); l.transform.SetParent(parent, false);
            l.type = LightType.Spot; l.spotAngle = 70f; l.range = 14f; l.intensity = 6f; l.color = H(0xFFE6C8); l.shadows = LightShadows.None;
            l.transform.position = c + rot * new Vector3(0, -4.2f, -4f); l.transform.LookAt(c);
        }

        // ================================================================== challenges
        static StolenHour Hour(Transform parent, Vector3 at, string challenge, bool hidden = false)
        {
            var t = Place(parent, "HOUR_Shard", at, 0f, 1.2f);
            var sh = t.gameObject.AddComponent<StolenHour>(); sh.challenge = challenge; sh.hiddenUntilUnlocked = hidden;
            var l = new GameObject("ShardLight").AddComponent<Light>(); l.transform.SetParent(t, false); l.transform.localPosition = Vector3.up * 1f;
            l.type = LightType.Point; l.range = 10f; l.intensity = 4f; l.color = H(0x6FEAFF); l.shadows = LightShadows.None;
            return sh;
        }

        static void Checkpoint(Transform parent, Vector3 at, bool start = false)
        {
            var go = new GameObject("Checkpoint"); go.transform.SetParent(parent, false); go.transform.position = at;
            var cp = go.AddComponent<Checkpoint>(); cp.isStart = start; cp.radius = 4f; cp.order = _cp++;
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(flame.GetComponent<Collider>());
            flame.transform.SetParent(go.transform, false); flame.transform.localPosition = new Vector3(0, 0.6f, 0); flame.transform.localScale = new Vector3(0.4f, 0.7f, 0.4f);
            flame.GetComponent<MeshRenderer>().sharedMaterial = M["Neon1"];
        }
        static int _cp;

        static GrindRail Rail(Transform parent, string name, params Vector3[] pts)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var r = go.AddComponent<GrindRail>(); r.points = new List<Vector3>(pts);
            r.BuildMesh(M["Rail"], M["RailPost"]);
            return r;
        }

        static void Pad(Transform parent, Vector3 at, Vector3 target, float apex)
        {
            var go = new GameObject("LaunchPad"); go.transform.SetParent(parent, false); go.transform.position = at;
            var vis = Box(go.transform, "Pad", at + Vector3.up * 0.1f, new Vector3(2.6f, 0.2f, 2.6f), M["Neon2"], false).transform;
            var tgt = new GameObject("Target").transform; tgt.SetParent(go.transform, false); tgt.position = target;
            var lp = go.AddComponent<LaunchPad>(); lp.target = tgt; lp.apexHeight = apex; lp.visual = vis; lp.radius = 1.4f;
        }

        static void Ring(Transform parent, Vector3 at, Vector3 forward)
        {
            var t = Place(parent, "PROP_SpeedRing", at, 0f, 1f, false, "Assets/DreamRush/Art/OpenWorld/Models") ?? new GameObject("SpeedRing").transform;
            t.SetParent(parent, true); t.rotation = Quaternion.LookRotation(forward);
            t.gameObject.AddComponent<SpeedRing>();
        }

        /// <summary>EAST: a rising chain of rooftops joined by grind rails, pads and speed rings; the shard waits on the tallest roof.</summary>
        static StolenHour RooftopRun(Transform root)
        {
            var g = Group(root, "Challenge_RooftopRun");
            Vector3[] roofs = { new Vector3(92, 0, -26), new Vector3(112, 0, -14), new Vector3(132, 0, -2), new Vector3(148, 0, 14), new Vector3(130, 0, 30), new Vector3(108, 0, 26) };
            float[] hts = { 9, 13, 17, 21, 25, 30 };
            for (int i = 0; i < roofs.Length; i++) Building(g, roofs[i], 12f, 12f, hts[i], "A", i % 2 == 0 ? 0 : 2);
            Pad(g, new Vector3(84, 0.05f, -36), roofs[0] + Vector3.up * (hts[0] + 0.6f), 13f);
            for (int i = 0; i < roofs.Length - 1; i++)
            {
                Vector3 a = roofs[i] + Vector3.up * (hts[i] + 1.4f), b = roofs[i + 1] + Vector3.up * (hts[i + 1] + 1.4f);
                Vector3 dir = (b - a); dir.y = 0; dir.Normalize();
                // rail from the edge of one roof, sagging toward the next, rising to its edge (you keep speed; a final jump lands you)
                Vector3 s0 = a + dir * 4.5f, s1 = b - dir * 6f;
                Rail(g, "Rail" + i, s0, Vector3.Lerp(s0, s1, 0.33f) + Vector3.down * 0.6f, Vector3.Lerp(s0, s1, 0.66f) + Vector3.down * 0.3f, s1 + Vector3.up * 1.0f);
                if (i % 2 == 1) Ring(g, Vector3.Lerp(a, b, 0.5f) + Vector3.up * 2.2f, dir);
            }
            Checkpoint(g, new Vector3(84, 0, -42));
            return Hour(g, roofs[5] + Vector3.up * (hts[5] + 0.6f), "RooftopRun");
        }

        /// <summary>NORTH: a glass canal you can walk on (it's a dream) and a spiral of spinning gears up to the shard.</summary>
        static StolenHour GearWorks(Transform root)
        {
            var g = Group(root, "Challenge_GearWorks");
            Box(g, "Canal", new Vector3(0, 0.03f, 115), new Vector3(70, 0.06f, 22), M["Glass"], false);
            for (int s = -1; s <= 1; s += 2) Box(g, "CanalEdge", new Vector3(0, 0.35f, 115 + s * 11.5f), new Vector3(70, 0.7f, 1f), M["M_Kit_MP_Stone"]);
            var pillar = Box(g, "GearPillar", new Vector3(0, 13, 120), new Vector3(4, 26, 4), M["M_Kit_MP_Brick"]);
            for (int i = 0; i < 11; i++)
            {
                float a = i * 34f * Mathf.Deg2Rad; float y = 1.4f + i * 2.25f;
                Vector3 p = new Vector3(Mathf.Sin(a) * 8f, y, 120 + Mathf.Cos(a) * 8f);
                var gear = Place(g, "GEAR_Platform", p, Rf(0, 360), 0.85f, true);
                if (gear != null)
                {
                    var mp = gear.gameObject.AddComponent<MovingPlatform>(); mp.spinDegPerSec = (i % 2 == 0 ? 1 : -1) * (12f + i * 2f); mp.carryRadius = 2.7f;
                }
            }
            Checkpoint(g, new Vector3(0, 0.1f, 100));
            return Hour(g, new Vector3(0, 36.9f, 120), "GearWorks");   // M17: up the rewind stairs
        }

        /// <summary>WEST: moonlit plaza with a fountain — step in and the Tick-Tock shadows ambush you; clear both waves.</summary>
        static StolenHour ShadowAmbush(Transform root, GameObject minion)
        {
            var g = Group(root, "Challenge_ShadowAmbush");
            var c = new Vector3(-115, 0, 0);
            var plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder); plaza.name = "Plaza"; plaza.transform.SetParent(g, false);
            plaza.transform.position = c + Vector3.up * 0.02f; plaza.transform.localScale = new Vector3(56, 0.04f, 56);
            plaza.GetComponent<MeshRenderer>().sharedMaterial = M["Plaza"]; Object.DestroyImmediate(plaza.GetComponent<Collider>());
            var basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder); basin.name = "Fountain"; basin.transform.SetParent(g, false);
            basin.transform.position = c + Vector3.up * 0.4f; basin.transform.localScale = new Vector3(7, 0.4f, 7); basin.GetComponent<MeshRenderer>().sharedMaterial = M["M_Kit_MP_Stone"];
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder); water.name = "FountainWater"; Object.DestroyImmediate(water.GetComponent<Collider>());
            water.transform.SetParent(g, false); water.transform.position = c + Vector3.up * 0.82f; water.transform.localScale = new Vector3(6.2f, 0.02f, 6.2f); water.GetComponent<MeshRenderer>().sharedMaterial = M["Glass"];
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                Place(g, "STREET_Lamp", c + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 24f, -i * 45f + 90f, 1.2f);
            }
            var shard = Hour(g, c + Vector3.up * 1.2f, "ShadowAmbush", hidden: true);
            var amb = new GameObject("AmbushChallenge").AddComponent<AmbushChallenge>();
            amb.transform.SetParent(g, false); amb.transform.position = c;
            amb.minionTemplate = minion; amb.reward = shard; amb.radius = 13f; amb.waves = new[] { 3, 4 };
            Checkpoint(g, c + new Vector3(24, 0, 0));
            return shard;
        }

        static GameObject MinionTemplate(Transform templates)
        {
            var go = new GameObject("TickMinion_Template"); go.transform.SetParent(templates, false);
            var model = Place(go.transform, "MINION_Tick", Vector3.zero);
            if (model != null) DreamLook.AddOutline(model.gameObject, 1.6f);
            go.AddComponent<TickMinion>();
            go.SetActive(false);
            return go;
        }

        static Dictionary<char, GameObject> _enemy;

        /// <summary>Generic enemy template: an inactive root with the component and its model (lifted by <paramref name="lift"/> on a pivot).</summary>
        static GameObject EnemyTemplate<T>(Transform templates, string model, float lift) where T : EnemyBase
        {
            var go = new GameObject(model + "_Template"); go.transform.SetParent(templates, false);
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(go.transform, false); pivot.localPosition = Vector3.up * lift;
            var m = Place(pivot, model, Vector3.zero);
            if (m != null) { m.localPosition = Vector3.zero; DreamLook.AddOutline(m.gameObject, 1.6f); }
            go.AddComponent<T>();
            go.SetActive(false);
            return go;
        }

        static void Spawn(Transform g, char kind, Vector3 at, float yaw = 0f)
        {
            var m = (GameObject)Object.Instantiate(_enemy[kind], g);
            m.name = kind == 'T' ? "TickMinion" : kind == 'B' ? "BellBrute" : kind == 'G' ? "GearRoller" : "CuckooSniper";
            m.SetActive(true); m.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
        }

        /// <summary>Street squads — every group is a different mix (M14: "I fight the same monster over and over").</summary>
        static void Squads(Transform root, GameObject template)
        {
            var g = Group(root, "Squads");
            (Vector3 at, string mix)[] squads =
            {
                (new Vector3(-25, 0, -80), "TTT"), (new Vector3(25, 0, -32), "TTG"), (new Vector3(70, 0, 75), "TTC"),
                (new Vector3(-75, 0, 75), "TTB"), (new Vector3(-70, 0, -75), "TTG"),
            };
            foreach (var (at, mix) in squads)
                for (int k = 0; k < mix.Length; k++) Spawn(g, mix[k], at + new Vector3((k - 1) * 3f, 0, (k % 2) * 2f), R.Next(0, 360));
            // two Bell Brutes guard the clock-tower gate (they wake when you come for the climb)
            Spawn(g, 'B', new Vector3(-9, 0, -34), 180f); Spawn(g, 'B', new Vector3(9, 0, -34), 180f);
            // cuckoo snipers perch over the rooftop run; gear rollers patrol the glass canal
            Spawn(g, 'C', new Vector3(132, 18.2f, -2), 200f);
            Spawn(g, 'C', new Vector3(130, 26.2f, 30), 160f);
            Spawn(g, 'G', new Vector3(-18, 0.1f, 112), 90f); Spawn(g, 'G', new Vector3(18, 0.1f, 118), -90f);
        }

        // ================================================================== the tower, the climb, the dial in the sky
        static (GameObject gate, Transform arena, CountMidnight boss) Tower(Transform root)
        {
            var g = Group(root, "ClockTower");
            var plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder); plaza.name = "TowerPlaza"; plaza.transform.SetParent(g, false);
            plaza.transform.position = new Vector3(0, 0.03f, 0); plaza.transform.localScale = new Vector3(84, 0.04f, 84);
            plaza.GetComponent<MeshRenderer>().sharedMaterial = M["Plaza"]; Object.DestroyImmediate(plaza.GetComponent<Collider>());
            var tower = Place(g, "CLOCK_Tower", Vector3.zero, 0f, 1f, true);
            // hands on the four faces keep ticking (and spin during TIME SKIP) — the tower model's hands are baked, add live ones
            for (int s = 0; s < 4; s++)
            {
                float a = s * 90f;
                var pivot = new GameObject("LiveHand" + s).transform; pivot.SetParent(g, false);
                Vector3 n = Quaternion.Euler(0, a, 0) * Vector3.back;
                pivot.position = new Vector3(0, 51, 0) + n * 9.9f; pivot.rotation = Quaternion.LookRotation(n);
                var hand = Box(pivot, "Hand", pivot.position + Vector3.up * 2.5f, new Vector3(0.45f, 5.2f, 0.2f), M["Neon0"], false);
                hand.transform.rotation = pivot.rotation;
                pivot.gameObject.AddComponent<ClockSpin>().degPerSec = 30f;
            }
            // the gate: a glowing ring around the climb start, gone when 3 hours are back
            var gate = new GameObject("TowerGate"); gate.transform.SetParent(g, false); gate.transform.position = new Vector3(0, 0, -20);
            var ringA = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ringA.name = "Barrier"; ringA.transform.SetParent(gate.transform, false);
            ringA.transform.localPosition = new Vector3(0, 4, 0); ringA.transform.localScale = new Vector3(7, 4, 7); ringA.GetComponent<MeshRenderer>().sharedMaterial = M["Gate"];
            // climb: gear spiral (y 1.5 → 37) around the tower
            Checkpoint(g, new Vector3(0, 0.1f, -20));
            float r = 17.5f; int steps = 16;
            for (int i = 0; i < steps; i++)
            {
                float a = (180f + i * 24f) * Mathf.Deg2Rad; float y = 1.4f + i * 2.3f;
                Vector3 p = new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r);
                var gear = Place(g, "GEAR_Platform", p, i * 17f, 0.9f, true);
                if (gear != null && i % 3 == 2) { var mp = gear.gameObject.AddComponent<MovingPlatform>(); mp.spinDegPerSec = 20f; mp.carryRadius = 2.8f; }
            }
            // ledge platform at 37 m + an updraft vent to the crown (glide up), then neon rails to the sky dial
            Vector3 ledge = new Vector3(Mathf.Sin((180f + steps * 24f) * Mathf.Deg2Rad) * r, 38f, Mathf.Cos((180f + steps * 24f) * Mathf.Deg2Rad) * r);
            Box(g, "Ledge", ledge + Vector3.down * 0.4f, new Vector3(8, 0.8f, 8), M["M_Kit_MP_Stone"]);
            Checkpoint(g, ledge + Vector3.up * 0.1f);
            var vent = new GameObject("Updraft"); vent.transform.SetParent(g, false); vent.transform.position = ledge + Vector3.up * 0.2f;
            var wc = vent.AddComponent<WindColumn>(); wc.radius = 3.4f; wc.height = 30f; wc.accel = 40f;
            var ps = vent.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 2.2f; main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f); main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f); main.startColor = new Color(1f, 0.6f, 0.8f, 0.7f);
            var em = ps.emission; em.rateOverTime = 50f; var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3f; sh.rotation = new Vector3(-90, 0, 0);
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = VFXLibraryMaterial();
            Vector3 top = ledge + Vector3.up * 29f;
            Box(g, "SkyStep", top + Vector3.down * 0.4f, new Vector3(6, 0.8f, 6), M["M_Kit_MP_Gold"]);
            Vector3 arenaC = new Vector3(0, ArenaY, 0);
            Vector3 rim = arenaC + (new Vector3(top.x, 0, top.z)).normalized * (BossArenaBuilder.R + 1.5f);
            Rail(g, "SkyRail", top + Vector3.up * 1.2f, Vector3.Lerp(top, rim, 0.35f) + Vector3.up * 4f, Vector3.Lerp(top, rim, 0.7f) + Vector3.up * 9f, rim + Vector3.up * 2.2f);
            // the arena (clock dial floating above the spire) + boss
            var arenaRoot = Group(root, "SkyDial");
            BossArenaBuilder.BuildArena(arenaRoot, arenaC);
            var boss = BossArenaBuilder.BuildBoss(arenaRoot, arenaC + new Vector3(0, 0, 8f));
            var ac = new GameObject("ArenaCenter").transform; ac.SetParent(arenaRoot, false); ac.position = arenaC;
            Checkpoint(arenaRoot, rim + (arenaC - rim).normalized * 4f + Vector3.up * 0.2f);
            return (gate, ac, boss);
        }

        static Material VFXLibraryMaterial()
        {
            var lib = AssetDatabase.LoadAssetAtPath<Presentation.VFXLibrary>("Assets/DreamRush/Resources/" + Presentation.VFXLibrary.ResourcePath + ".asset");
            return lib != null ? lib.puff : null;
        }

        // ================================================================== sky
        static void Sky(Transform root, out Transform moonT)
        {
            // Sonny Boy / Graphic Pop-Surrealism Sky: stark obsidian zenith, electric magenta/cyan horizon, vibrant pop contrast
            var skyMat = World1Builder.AnimeSky("M_Sky_PalaceDusk", H(0x070614), H(0xFF2A6D), H(0xFFE600), H(0x05040E), 0.28f, H(0x7928CA), 0.85f, new Vector3(-0.35f, 0.12f, 0.93f));
            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 75; RenderSettings.fogEndDistance = 720;
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.transform.SetParent(root, false);
            sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.88f;
            var rim = new GameObject("NeonRim").AddComponent<Light>(); rim.transform.SetParent(root, false);
            rim.type = LightType.Directional; rim.color = H(0x00F0FF); rim.intensity = 0.55f; rim.shadows = LightShadows.None; rim.transform.rotation = Quaternion.Euler(28, 35, 0);
            var clock = new GameObject("SkyClock").AddComponent<SkyClock>(); clock.transform.SetParent(root, false);
            clock.sky = skyMat; clock.sun = sun; clock.azimuth = 200f;
            clock.keys = new[]
            {
                new SkyClock.Key { zenith = H(0x070614), horizon = H(0xFF2A6D), band = H(0xFFE600), sun = new Color(5f, 3.6f, 1.8f), light = H(0xFFF3C4), lightIntensity = 1.55f, elevation = 14f,
                                   fog = H(0x160B2E), fogEnd = 760f, ambSky = H(0x2A1B54), ambEq = H(0x781D4E), ambGround = H(0x090716), stars = 0.65f },
                new SkyClock.Key { zenith = H(0x050410), horizon = H(0x7928CA), band = H(0xFF1E56), sun = new Color(4f, 1.8f, 2.6f), light = H(0xFFD6E8), lightIntensity = 1.25f, elevation = 18f,
                                   fog = H(0x12092A), fogEnd = 720f, ambSky = H(0x1F1448), ambEq = H(0x5A185C), ambGround = H(0x080614), stars = 0.85f },
                new SkyClock.Key { zenith = H(0x03020A), horizon = H(0x00F0FF), band = H(0x7000FF), sun = new Color(1.6f, 2.8f, 4.2f), light = H(0xB8F2FF), lightIntensity = 1.15f, elevation = 36f,
                                   fog = H(0x090B24), fogEnd = 680f, ambSky = H(0x120E36), ambEq = H(0x2B1954), ambGround = H(0x05040E), stars = 1f },
            };
            clock.Apply();
            moonT = Place(root, "MOON_Crescent", new Vector3(-60, 150, 300), 180f, 34f, false, "Assets/DreamRush/Art/Bedroom/Models");
            if (moonT != null) moonT.rotation = Quaternion.Euler(0, 200, -20);
            // Global Volume (Sonny Boy high-contrast pop surrealism)
            const string path = "Assets/DreamRush/Settings/PP_MidnightPalace.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.components.RemoveAll(x => x == null);
            T Get<T>() where T : VolumeComponent { if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); } c.active = true; return c; }
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(0.12f); ca.contrast.Override(36f); ca.saturation.Override(32f);
            var st = Get<SplitToning>(); st.shadows.Override(H(0x120A38)); st.highlights.Override(H(0xFFE866)); st.balance.Override(-14f);
            var tm = Get<Tonemapping>(); tm.mode.Override(TonemappingMode.None);
            var bl = Get<Bloom>(); bl.intensity.Override(0.72f); bl.threshold.Override(0.92f); bl.scatter.Override(0.68f);
            var vg = Get<Vignette>(); vg.intensity.Override(0.28f); vg.smoothness.Override(0.42f); vg.color.Override(H(0x050312));
            var ab = Get<ChromaticAberration>(); ab.intensity.Override(0.16f);
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            var vol = new GameObject("PostFX").AddComponent<Volume>(); vol.transform.SetParent(root, false); vol.isGlobal = true; vol.priority = 6; vol.sharedProfile = p;
        }

        /// <summary>
        /// Sonny Boy / Pop-Surrealism visual composition layer: floating geometric color monoliths, surreal tilted sky-frames,
        /// and ambient rising neon-dust particles across the avenues (zero colliders added so all nav/tests stay 100% intact).
        /// </summary>
        static void SonnyBoySurrealSkyscape(Transform root)
        {
            var g = Group(root, "SonnyBoy_SurrealArt");
            string[] neonKeys = { "Neon0", "Neon1", "Neon2", "Neon3", "Neon4", "Neon5" };
            var rng = new System.Random(2026);

            // 1. Surreal floating geometric monoliths & tilted color planes framing the skyline
            for (int i = 0; i < 24; i++)
            {
                float ang = i / 24f * Mathf.PI * 2f;
                float dist = 72f + (i % 3) * 38f;
                float y = 34f + (i % 4) * 16f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * dist, y, Mathf.Sin(ang) * dist);
                Material mat = i % 3 == 0 ? M["M_Kit_MP_Dark"] : M[neonKeys[i % neonKeys.Length]];
                Vector3 scale = i % 2 == 0 ? new Vector3(4.5f, 22f + (i % 3) * 8f, 1.2f) : new Vector3(9f, 9f, 0.6f);
                var shard = Box(g, "SurrealMonolith_" + i, pos, scale, mat, collider: false, yaw: i * 15f);
                shard.transform.localRotation = Quaternion.Euler(18f * Mathf.Sin(i), i * 27f, 22f * Mathf.Cos(i));
            }

            // 2. Giant graphic rings & suspended horizon frames around the Clock Tower
            for (int r = 0; r < 3; r++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "SurrealSkyHalo_" + r;
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                ring.transform.SetParent(g, false);
                ring.transform.position = new Vector3(0, 56f + r * 18f, 0);
                ring.transform.localScale = new Vector3(62f - r * 12f, 0.18f, 62f - r * 12f);
                ring.transform.localRotation = Quaternion.Euler(8f + r * 6f, r * 45f, -10f + r * 5f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = M[neonKeys[r % neonKeys.Length]];
            }

            // 3. Ambient Pop-Surreal floating light motes along the grand avenues
            var vfxGo = new GameObject("SurrealNeonMotes");
            vfxGo.transform.SetParent(g, false);
            vfxGo.transform.position = new Vector3(0, 6f, -80f);
            var ps = vfxGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startColor = new ParticleSystem.MinMaxGradient(H(0x00F0FF), H(0xFF2E9A));
            main.maxParticles = 180;
            var em = ps.emission;
            em.rateOverTime = 28f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(220f, 30f, 220f);
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (VFXLibraryMaterial() != null) psr.sharedMaterial = VFXLibraryMaterial();
        }

        // ================================================================== authored wonders along the routes
        static void Wonders(Transform g, WonderDirector wd)
        {
            void W(Vector3 at, string kind, float radius = 16f, int count = 3)
            {
                var go = new GameObject("Wonder_" + kind); go.transform.SetParent(g, false); go.transform.position = at;
                var e = go.AddComponent<WonderEvent>(); e.kind = kind; e.radius = radius; e.count = count;
                wd.events.Add(e);
            }
            W(new Vector3(0, 0, -128), "PaperPlanes", 14f);
            W(new Vector3(0, 0, -95), "CallingCards", 14f);
            W(new Vector3(0, 0, -60), "Fireworks", 18f);
            W(new Vector3(60, 0, -45), "TramPass", 20f);
            W(new Vector3(95, 0, 50), "SkyWhale", 22f);
            W(new Vector3(40, 0, 95), "ShootingStars", 20f);
            W(new Vector3(-45, 0, 95), "PaperPlanes", 18f);
            W(new Vector3(-90, 0, 50), "MoonWink", 22f);
            W(new Vector3(-90, 0, -50), "TimeSkip", 18f);
            W(new Vector3(-45, 0, -95), "SkyWhale", 20f);
            W(new Vector3(0, 0, -30), "TimeSkip", 16f);
        }
    }
}
