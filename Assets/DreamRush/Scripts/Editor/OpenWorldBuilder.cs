using System.Collections.Generic;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.OpenWorld;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using DreamRush.World1;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// DREAMLAND — open world (docs/OPENWORLD_DESIGN.md). 640 m painted field at golden hour: toon mesh terrain from
    /// DreamlandTerrain, instanced grass, lake + waterfalls, crystal cliffs, ruins, pine hills, floating islands with
    /// speed rings and updrafts. THE CHAIR flees between landmarks (3 Dream Keys), THE ALARM hunts the player.
    /// </summary>
    public static class OpenWorldBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Dream_Openworld.unity";
        const string ModelDir = "Assets/DreamRush/Art/OpenWorld/Models";
        const string TexDir = "Assets/DreamRush/Art/OpenWorld/Textures";
        const string OwMeshDir = "Assets/DreamRush/Art/OpenWorld/Meshes";
        static readonly Vector3 SunEuler = new Vector3(24f, -152f, 0f);
        static Dictionary<string, Material> M;
        static System.Random R;
        static int _cp;

        static float H(float x, float z) => DreamlandTerrain.Height(x, z);
        static Vector3 G(float x, float z) => DreamlandTerrain.Ground(x, z);

        [MenuItem("DreamRush/Build/Dreamland (open world)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            DreamLookSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            R = new System.Random(2026); _cp = 0;
            M = Materials();
            var root = new GameObject("DREAMLAND").transform;
            var world = Group(root, "World_Static");          // everything the NavMesh is baked from

            Sky(root);
            Terrain(world);
            Water(root);
            var level = new GameObject("DreamLevelManager").AddComponent<DreamLevelManager>();
            level.killY = -25f;
            level.anyOrderCheckpoints = true;

            // ---- regions dressing
            var fires = new List<Transform>();
            Meadow(world, root, fires);
            CrystalCliffs(world, root, fires);
            Lake(world, root, fires);
            Ruins(world, root, fires);
            Hills(world, root, fires);
            Rim(world);

            // ---- NavMesh (baked now, saved as an asset; the lake is not walkable)
            var lakeBlock = new GameObject("NavBlock_Lake").AddComponent<NavMeshModifierVolume>();
            lakeBlock.transform.SetParent(world, false);
            lakeBlock.transform.position = new Vector3(DreamlandTerrain.Lake.x, -3f, DreamlandTerrain.Lake.y);
            lakeBlock.size = new Vector3(130, 10, 130); lakeBlock.area = 1;
            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            GrayboxAssets.EnsureFolder(OwMeshDir);
            string navPath = OwMeshDir + "/NavMesh_Dreamland.asset";
            if (AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath) != null) AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);

            // ---- actors
            var chair = Chair(root);
            var alarm = Alarm(root);

            // ---- player + camera + HUD
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Vector3 spawn = G(0, -12) + Vector3.up * 8f;
            player.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            player.AddComponent<PlayerAbilities>();
            var tpc = PlayerRigBuilder.BuildHubCamera(player);
            CameraTuning.ApplyFollow(tpc);
            tpc.distance = 5.4f; tpc.sprintExtraDistance = 2f;   // a bit wider for the open world
            var cam = tpc.GetComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            cam.nearClipPlane = 0.15f; cam.farClipPlane = 1200f; cam.clearFlags = CameraClearFlags.Skybox;
            tpc.transform.position = spawn + new Vector3(0, 2, -6);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = tpc.transform;
            EditorUtility.SetDirty(motor);
            level.player = motor;
            new GameObject("DreamArrival").AddComponent<DreamArrival>().player = motor;
            new GameObject("SleepStability").AddComponent<SleepStability>();
            new GameObject("DreamHUD").AddComponent<DreamHUD>();
            new GameObject("AmbientMotes").AddComponent<AmbientMotes>();
            new GameObject("WaterRescue").AddComponent<WaterRescue>();
            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>(); ctx.kind = SceneKind.Dream;
            var dir = new GameObject("OpenWorldDirector").AddComponent<OpenWorldDirector>();
            dir.chair = chair; dir.alarm = alarm; dir.pillowFires = fires;
            dir.regions = new List<OpenWorldDirector.Region>
            {
                new OpenWorldDirector.Region { name = "STARFALL MEADOW", thai = "ทุ่งดาวตก", center = new Vector3(0, 0, 0), radius = 60 },
                new OpenWorldDirector.Region { name = "CRYSTAL CLIFFS", thai = "หน้าผาคริสตัล", center = new Vector3(DreamlandTerrain.Cliffs.x, 0, DreamlandTerrain.Cliffs.y), radius = 80 },
                new OpenWorldDirector.Region { name = "MIRROR LAKE", thai = "ทะเลสาบกระจก", center = new Vector3(DreamlandTerrain.Lake.x, 0, DreamlandTerrain.Lake.y), radius = 95 },
                new OpenWorldDirector.Region { name = "CLOCKWORK RUINS", thai = "ซากเมืองนาฬิกา — ถิ่นของ THE ALARM", center = new Vector3(DreamlandTerrain.Ruins.x, 0, DreamlandTerrain.Ruins.y), radius = 90 },
                new OpenWorldDirector.Region { name = "PINE HILLS", thai = "เนินสน", center = new Vector3(DreamlandTerrain.Hills.x, 0, DreamlandTerrain.Hills.y), radius = 100 },
            };
            var grass = new GameObject("GrassField").AddComponent<GrassField>();
            grass.drawDistance = 150f; grass.fadeStart = 85f;
            grass.material = GrassMat();

            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DreamRush] Dreamland built: " + ScenePath);
        }

        // =========================================================================================== look
        static Dictionary<string, Material> Materials()
        {
            Color X(int h) => Palette.Hex(h);
            var m = new Dictionary<string, Material>
            {
                ["M_Kit_OWBark"] = ArtKit.Mat("M_Kit_OWBark", X(0x6B4A3A), rim: 0.1f),
                ["M_Kit_OWLeaf"] = ArtKit.Mat("M_Kit_OWLeaf", X(0x3A8A46), rim: 0.14f, rampSoft: 0.5f),
                ["M_Kit_OWLeafLight"] = ArtKit.Mat("M_Kit_OWLeafLight", X(0x6DAE45), rim: 0.15f, rampSoft: 0.5f),
                ["M_Kit_OWPine"] = ArtKit.Mat("M_Kit_OWPine", X(0x2E6E58), rim: 0.25f, rampSoft: 0.5f),
                ["M_Kit_OWCrystal"] = ArtKit.Mat("M_Kit_OWCrystal", X(0x86DCFF), rim: 0.6f, spec: 0.8f),
                ["M_Kit_OWCrystalPink"] = ArtKit.Mat("M_Kit_OWCrystalPink", X(0xFF9AD8), rim: 0.6f, spec: 0.8f),
                ["M_Kit_OWRock"] = ArtKit.Mat("M_Kit_OWRock", X(0x7F8794), rim: 0.12f, rampSoft: 0.5f),
                ["M_Kit_OWGrassTop"] = ArtKit.Mat("M_Kit_OWGrassTop", X(0x74B24E), rim: 0.2f, rampSoft: 0.55f),
                ["M_Kit_OWStone"] = ArtKit.Mat("M_Kit_OWStone", X(0xD6CBB6), rim: 0.15f, rampSoft: 0.5f),
                ["M_Kit_OWClockBody"] = ArtKit.Mat("M_Kit_OWClockBody", X(0xD8323F), rim: 0.3f, spec: 0.55f),
                ["M_Kit_OWClockFace"] = ArtKit.Mat("M_Kit_OWClockFace", X(0xF7F0DE), rim: 0.1f),
                ["M_Kit_OWEyeGlow"] = ArtKit.Mat("M_Kit_OWEyeGlow", X(0xFFE066)),
                ["M_Kit_OWBrass"] = ArtKit.Mat("M_Kit_OWBrass", X(0xD9A24A), rim: 0.3f, spec: 0.6f),
                ["M_Kit_OWInk"] = ArtKit.Mat("M_Kit_OWInk", X(0x1A1420)),
                ["M_Kit_OWGold"] = ArtKit.Mat("M_Kit_OWGold", X(0xF2C14E), rim: 0.4f, spec: 0.7f),
            };
            m["M_Kit_OWCrystal"].SetColor("_Emission", new Color(0.25f, 0.6f, 0.95f) * 0.9f);
            m["M_Kit_OWCrystalPink"].SetColor("_Emission", new Color(0.95f, 0.3f, 0.7f) * 0.8f);
            m["M_Kit_OWEyeGlow"].SetColor("_Emission", new Color(2.4f, 1.9f, 0.5f));
            m["M_Kit_OWGold"].SetColor("_Emission", new Color(0.25f, 0.18f, 0.02f));
            foreach (var kv in m) EditorUtility.SetDirty(kv.Value);
            // terrain
            var tm = LoadOrCreate("M_OW_Terrain", "Assets/DreamRush/Shaders/DreamTerrain.shader");
            tm.SetTexture("_NoiseTex", Noise());
            tm.SetFloat("_WaterLevel", DreamlandTerrain.WaterLevel);
            tm.SetFloat("_GoldAmount", 0.22f);
            tm.SetColor("_Rock", new Color(0.80f, 0.66f, 0.54f)); tm.SetColor("_RockDark", new Color(0.68f, 0.53f, 0.46f));
            tm.SetColor("_GrassA", new Color(0.20f, 0.47f, 0.25f)); tm.SetColor("_GrassB", new Color(0.50f, 0.74f, 0.31f));
            m["Terrain"] = tm;
            var water = LoadOrCreate("M_OW_Water", "Assets/DreamRush/Shaders/DreamWater.shader");
            water.SetTexture("_NoiseTex", Noise());
            water.SetVector("_SunDir", SunDir());
            m["Water"] = water;
            m["Fall"] = ArtKit.Mat("M_OW_Waterfall", X(0xBFEFFF), rim: 0.5f, rampSoft: 0.9f);
            m["Fall"].SetColor("_Emission", new Color(0.25f, 0.4f, 0.5f));
            m["Beam"] = AssetDatabase.LoadAssetAtPath<Material>(ArtKit.MatDir + "/M_VFX_BeamAdd.mat");
            m["Flame"] = AssetDatabase.LoadAssetAtPath<Material>(ArtKit.MatDir + "/M_W1_Flame.mat") ?? m["M_Kit_OWEyeGlow"];
            return m;
        }

        static Vector4 SunDir() { var f = Quaternion.Euler(SunEuler) * Vector3.forward; return -f; }

        static Texture2D Noise()
        {
            string p = TexDir + "/T_WorldNoise.png";
            var imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null && (imp.sRGBTexture || imp.wrapMode != TextureWrapMode.Repeat)) { imp.sRGBTexture = false; imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        static Material LoadOrCreate(string name, string shaderPath)
        {
            string path = $"{ArtKit.MatDir}/{name}.mat";
            var sh = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GrassMat()
        {
            var g = LoadOrCreate("M_OW_Grass", "Assets/DreamRush/Shaders/DreamGrass.shader");
            g.SetTexture("_NoiseTex", Noise());
            g.enableInstancing = true;
            return g;
        }

        static void Sky(Transform root)
        {
            var sky = LoadOrCreate("M_Sky_Dreamland", "Assets/DreamRush/Shaders/FantasySky.shader");
            sky.SetVector("_SunDir", SunDir());
            RenderSettings.skybox = sky;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex(0xA9B8F0);
            RenderSettings.ambientEquatorColor = Palette.Hex(0xF2C6A0);
            RenderSettings.ambientGroundColor = Palette.Hex(0x5A4A62);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Hex(0xE7C2B8); RenderSettings.fogStartDistance = 110f; RenderSettings.fogEndDistance = 760f;
            var look = Group(root, "Look");
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.transform.SetParent(look, false);
            sun.type = LightType.Directional; sun.color = Palette.Hex(0xFFD7A8); sun.intensity = 1.55f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(SunEuler);
            RenderSettings.sun = sun;
            var fill = new GameObject("SkyFill").AddComponent<Light>(); fill.transform.SetParent(look, false);
            fill.type = LightType.Directional; fill.color = Palette.Hex(0x8FA0FF); fill.intensity = 0.35f; fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(50, 30, 0);
            // post: warm golden-hour grade, generous bloom (crystals, sun, water glints)
            GrayboxAssets.EnsureFolder("Assets/DreamRush/Settings/OpenWorld");
            string path = "Assets/DreamRush/Settings/OpenWorld/PP_Dreamland.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.components.RemoveAll(x => x == null);   // v1 never saved its components (all post silently fell back to the URP default)
            T Get<T>() where T : VolumeComponent { if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); } c.active = true; return c; }
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(-0.05f); ca.contrast.Override(20f); ca.saturation.Override(18f); ca.colorFilter.Override(new Color(1f, 0.97f, 0.93f));
            Get<Tonemapping>().mode.Override(TonemappingMode.ACES);
            var bl = Get<Bloom>(); bl.intensity.Override(0.75f); bl.threshold.Override(0.95f); bl.scatter.Override(0.72f); bl.tint.Override(new Color(1f, 0.9f, 0.8f));
            var vg = Get<Vignette>(); vg.intensity.Override(0.22f); vg.smoothness.Override(0.5f); vg.color.Override(Palette.Hex(0x2A1A3A));
            var sm = Get<ShadowsMidtonesHighlights>(); sm.shadows.Override(new Vector4(0.92f, 0.9f, 1.08f, 0f)); sm.highlights.Override(new Vector4(1.05f, 1f, 0.92f, 0f));
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            var v = new GameObject("PostFX").AddComponent<Volume>(); v.transform.SetParent(look, false);
            v.isGlobal = true; v.priority = 2; v.sharedProfile = p;
        }

        // =========================================================================================== terrain
        static void Terrain(Transform world)
        {
            var g = Group(world, "Terrain");
            const float chunk = 64f, res = 2f;
            int n = Mathf.CeilToInt(DreamlandTerrain.Half * 2 / chunk);
            int q = Mathf.RoundToInt(chunk / res);
            GrayboxAssets.EnsureFolder(OwMeshDir);
            for (int cz = 0; cz < n; cz++)
                for (int cx = 0; cx < n; cx++)
                {
                    float x0 = -DreamlandTerrain.Half + cx * chunk, z0 = -DreamlandTerrain.Half + cz * chunk;
                    var verts = new Vector3[(q + 1) * (q + 1)];
                    var norms = new Vector3[verts.Length];
                    var cols = new Color[verts.Length];
                    for (int j = 0; j <= q; j++)
                        for (int i = 0; i <= q; i++)
                        {
                            float x = x0 + i * res, z = z0 + j * res;
                            int k = j * (q + 1) + i;
                            verts[k] = new Vector3(i * res, H(x, z), j * res);
                            norms[k] = DreamlandTerrain.Normal(x, z);
                            cols[k] = new Color(DreamlandTerrain.PathWeight(x, z), Mathf.Clamp01(1.4f - Vector2.Distance(new Vector2(x, z), DreamlandTerrain.Lake) / 80f), 0, 1);
                        }
                    var tris = new int[q * q * 6];
                    int t = 0;
                    for (int j = 0; j < q; j++)
                        for (int i = 0; i < q; i++)
                        {
                            int a = j * (q + 1) + i, b = a + 1, c = a + q + 1, d = c + 1;
                            tris[t++] = a; tris[t++] = c; tris[t++] = b; tris[t++] = b; tris[t++] = c; tris[t++] = d;
                        }
                    var mesh = new Mesh { name = $"OW_Terrain_{cx}_{cz}" };
                    mesh.vertices = verts; mesh.normals = norms; mesh.colors = cols; mesh.triangles = tris;
                    mesh.RecalculateBounds();
                    string mp = $"{OwMeshDir}/{mesh.name}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
                    if (existing != null) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                    else AssetDatabase.CreateAsset(mesh, mp);
                    var go = new GameObject(mesh.name);
                    go.transform.SetParent(g, false);
                    go.transform.position = new Vector3(x0, 0, z0);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = Mt("Terrain");
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                    go.isStatic = true;
                }
        }

        static void Water(Transform root)
        {
            var g = Group(root, "Water");
            var q = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "MirrorLake"; q.transform.SetParent(g, false);
            q.transform.position = new Vector3(DreamlandTerrain.Lake.x, DreamlandTerrain.WaterLevel, DreamlandTerrain.Lake.y);
            q.transform.localScale = new Vector3(22, 1, 22);
            var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = Mt("Water"); r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // =========================================================================================== regions
        static void Meadow(Transform world, Transform root, List<Transform> fires)
        {
            var g = Group(world, "Meadow");
            fires.Add(PillowFire(root, G(4, -6)));
            for (int i = 0; i < 34; i++)
            {
                var p = RandIn(Vector2.zero, 25, 125);
                if (!Ok(p, 0.22f, true)) continue;
                Place(g, $"TREE_Round{i % 3}", G(p.x, p.y) - Vector3.up * 0.2f, Rand(0, 360), Rand(0.85f, 1.35f), collider: "trunk");
            }
            for (int i = 0; i < 10; i++)
            {
                var p = RandIn(Vector2.zero, 18, 110);
                if (!Ok(p, 0.3f, true)) continue;
                var c = Place(g, $"CRYSTAL_Cluster{i % 2}", G(p.x, p.y) - Vector3.up * 0.3f, Rand(0, 360), Rand(0.8f, 1.6f), collider: "box");
            }
            for (int i = 0; i < 22; i++)
            {
                var p = RandIn(Vector2.zero, 20, 140);
                if (!Ok(p, 0.4f, true)) continue;
                Place(g, $"ROCK_Big{i % 2}", G(p.x, p.y) - Vector3.up * 0.6f, Rand(0, 360), Rand(0.6f, 1.7f), collider: "box");
            }
            // a lone giant tree on a knoll: the meadow landmark
            Place(g, "TREE_Round1", G(-38, 30) - Vector3.up * 0.3f, 20, 3.2f, collider: "trunk");
            Star(root, G(-38, 30) + Vector3.up * 17.5f);
            Place(g, "CRYSTAL_Spire", G(42, -38) - Vector3.up * 0.5f, 30, 0.9f, collider: "box", light: new Color(0.5f, 0.8f, 1f));
            Wind(root, G(42, -32), 26f);
            Star(root, G(42, -38) + Vector3.up * 19f);
        }

        static void CrystalCliffs(Transform world, Transform root, List<Transform> fires)
        {
            var g = Group(world, "CrystalCliffs");
            var c = DreamlandTerrain.Cliffs;
            fires.Add(PillowFire(root, G(c.x + 6, c.y - 6)));
            for (int i = 0; i < 4; i++)
            {
                var p = c + new Vector2(Mathf.Sin(i * 1.7f) * 32, Mathf.Cos(i * 1.7f) * 26);
                Place(g, "CRYSTAL_Spire", G(p.x, p.y) - Vector3.up * 0.5f, Rand(0, 360), Rand(1.4f, 2.4f), collider: "box", light: i % 2 == 0 ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.5f, 0.85f));
            }
            for (int i = 0; i < 30; i++)
            {
                var p = RandIn(c, 0, 60);
                if (!Ok(p, 0.35f, false)) continue;
                Place(g, $"CRYSTAL_Cluster{i % 2}", G(p.x, p.y) - Vector3.up * 0.3f, Rand(0, 360), Rand(1f, 2.4f), collider: "box");
            }
            for (int i = 0; i < 16; i++)
            {
                var p = RandIn(c, 10, 70);
                if (!Ok(p, 0.3f, false)) continue;
                Place(g, "TREE_Pine", G(p.x, p.y) - Vector3.up * 0.2f, Rand(0, 360), Rand(0.9f, 1.5f), collider: "trunk");
            }
            // updrafts up the cliff face from the meadow side (south edge) — glide into them to climb
            foreach (var p in new[] { new Vector2(c.x - 22, c.y - 78), new Vector2(c.x + 30, c.y - 74), new Vector2(c.x - 60, c.y - 40) })
                Wind(root, G(p.x, p.y), 34f);
            Star(root, G(c.x, c.y) + Vector3.up * 2.5f);
            Star(root, G(c.x + 32 * Mathf.Sin(1.7f), c.y + 26 * Mathf.Cos(1.7f)) + Vector3.up * 30f);
            Beam(root, G(c.x, c.y) + new Vector3(-20, 60, 30), G(c.x, c.y), 14f);
        }

        static void Lake(Transform world, Transform root, List<Transform> fires)
        {
            var g = Group(world, "MirrorLake");
            var c = DreamlandTerrain.Lake;
            fires.Add(PillowFire(root, G(c.x - 72, c.y - 18)));
            for (int i = 0; i < 26; i++)
            {
                float a = Rand(0, Mathf.PI * 2), r = Rand(78, 110);
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!Ok(p, 0.3f, true)) continue;
                Place(g, i % 3 == 0 ? "TREE_Pine" : $"TREE_Round{i % 3}", G(p.x, p.y) - Vector3.up * 0.2f, Rand(0, 360), Rand(0.9f, 1.3f), collider: "trunk");
            }
            for (int i = 0; i < 8; i++)
            {
                float a = Rand(0, Mathf.PI * 2), r = Rand(70, 90);
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                Place(g, $"CRYSTAL_Cluster{i % 2}", G(p.x, p.y) - Vector3.up * 0.3f, Rand(0, 360), Rand(1f, 1.8f), collider: "box");
            }
            // floating islands spiral up over the lake: glide / double-jump / speed-ring route, waterfalls into the lake
            var isl = Group(root, "FloatingIslands");
            Vector3 prev = G(c.x - 62, c.y - 10) + Vector3.up * 1.5f;
            for (int i = 0; i < 7; i++)
            {
                float a = i * 0.85f;
                Vector3 p = new Vector3(c.x + Mathf.Cos(a + 3.3f) * (45 - i * 4), 14 + i * 7.5f, c.y + Mathf.Sin(a + 3.3f) * (45 - i * 4));
                var island = Place(isl, i % 3 == 0 ? "ISLAND_Float1" : "ISLAND_Float0", p, Rand(0, 360), i % 3 == 0 ? 1f : 1.15f, collider: "mesh");
                Place(isl, $"TREE_Round{i % 3}", p, Rand(0, 360), 0.8f, collider: "trunk");
                if (i % 2 == 0) Waterfall(root, p + new Vector3(4.5f, -0.6f, 0), p.y + 1f);
                // speed ring halfway between consecutive islands
                Vector3 mid = (prev + p) * 0.5f + Vector3.up * 2.5f;
                Ring(root, mid, (p - prev).normalized);
                prev = p;
                if (i == 6) Star(root, p + Vector3.up * 2.5f);
            }
            Star(root, new Vector3(c.x, 4f, c.y));   // over the lake centre: glide off the islands to get it
        }

        static void Ruins(Transform world, Transform root, List<Transform> fires)
        {
            var g = Group(world, "ClockworkRuins");
            var c = DreamlandTerrain.Ruins;
            fires.Add(PillowFire(root, G(c.x + 58, c.y + 4)));
            for (int i = 0; i < 10; i++)                     // ring of arches round the plaza
            {
                float a = i / 10f * Mathf.PI * 2f;
                var p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 30f;
                Place(g, "RUIN_Arch", G(p.x, p.y) - Vector3.up * 0.4f, -a * Mathf.Rad2Deg + 90f, Rand(0.9f, 1.15f), collider: "arch");
            }
            for (int i = 0; i < 36; i++)
            {
                var p = RandIn(c, 8, 80);
                bool fallen = Rand(0, 1) < 0.3f;
                var t = Place(g, "RUIN_Pillar", G(p.x, p.y) - Vector3.up * 0.3f, Rand(0, 360), Rand(0.8f, 1.4f), collider: "box");
                if (fallen && t != null) t.rotation = Quaternion.Euler(Rand(80, 95), Rand(0, 360), 0);
            }
            Place(g, "CRYSTAL_Spire", G(c.x, c.y) - Vector3.up * 0.5f, 0, 1.6f, collider: "box", light: new Color(1f, 0.55f, 0.85f));
            Star(root, G(c.x + 30, c.y) + Vector3.up * 10.6f);   // on top of an arch
            Star(root, G(c.x - 50, c.y + 45) + Vector3.up * 1.5f);
            Star(root, G(c.x - 30, c.y) + Vector3.up * 10.6f);   // the opposite arch
        }

        static void Hills(Transform world, Transform root, List<Transform> fires)
        {
            var g = Group(world, "PineHills");
            var c = DreamlandTerrain.Hills;
            fires.Add(PillowFire(root, G(c.x + 4, c.y + 60)));
            for (int i = 0; i < 150; i++)
            {
                var p = RandIn(c, 10, 140);
                if (!Ok(p, 0.32f, true)) continue;
                Place(g, i % 5 == 0 ? $"TREE_Round{i % 3}" : "TREE_Pine", G(p.x, p.y) - Vector3.up * 0.2f, Rand(0, 360), Rand(0.8f, 1.6f), collider: "trunk");
            }
            for (int i = 0; i < 18; i++)
            {
                var p = RandIn(c, 10, 140);
                if (!Ok(p, 0.4f, true)) continue;
                Place(g, $"ROCK_Big{i % 2}", G(p.x, p.y) - Vector3.up * 0.5f, Rand(0, 360), Rand(0.8f, 2f), collider: "box");
            }
            // highest hilltop gets a star
            Vector2 best = c; float bh = -999;
            for (int i = 0; i < 200; i++) { var p = RandIn(c, 0, 120); float h = H(p.x, p.y); if (h > bh) { bh = h; best = p; } }
            Star(root, G(best.x, best.y) + Vector3.up * 1.6f);
            Star(root, G(c.x + 60, c.y - 40) + Vector3.up * 1.4f);
            Star(root, G(-120, -120) + Vector3.up * 1.4f);
        }

        static void Rim(Transform world)
        {
            var g = Group(world, "RimForest");
            for (int i = 0; i < 160; i++)
            {
                float a = Rand(0, Mathf.PI * 2), r = Rand(235, 262);
                var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (H(p.x, p.y) < 1f) continue;
                Place(g, "TREE_Pine", G(p.x, p.y) - Vector3.up * 0.3f, Rand(0, 360), Rand(1.2f, 2.2f), collider: null);
            }
        }

        // =========================================================================================== actors
        static OpenWorldChair Chair(Transform root)
        {
            var go = new GameObject("OW_Chair");
            go.transform.SetParent(root, false);
            var start = G(10, 34);
            go.transform.position = start;
            var vis = Group(go.transform, "Visual");
            if (ChairCharacterSetup.ModelExists && ChairCharacterSetup.Setup() != null)
            {
                var t = ChairCharacterSetup.Instantiate(vis, "Chair_Model", withFace: true);
                if (t != null) foreach (var c in t.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            }
            else DreamChairBuilder.BuildGrayboxChair(vis);
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.55f; agent.height = 1.6f; agent.baseOffset = 0f;
            var ch = go.AddComponent<OpenWorldChair>();
            ch.visual = vis;
            var lms = Group(root, "ChairLandmarks");
            Vector2[] pts =
            {
                new Vector2(10, 34), new Vector2(-40, 26), new Vector2(60, -30), new Vector2(-70, -60), new Vector2(110, 10), new Vector2(150, -40),
                new Vector2(130, 95), new Vector2(-150, 6), new Vector2(-175, -40), new Vector2(-120, 70), new Vector2(0, -150), new Vector2(-40, -190),
                new Vector2(45, -210), new Vector2(-20, 120), new Vector2(40, 130), new Vector2(-80, 150)
            };
            foreach (var p in pts)
            {
                var l = Group(lms, "Landmark"); l.position = Snap(G(p.x, p.y));
                ch.landmarks.Add(l);
            }
            return ch;
        }

        static NightmareAlarm Alarm(Transform root)
        {
            var go = new GameObject("OW_TheAlarm");
            go.transform.SetParent(root, false);
            go.transform.position = Snap(G(DreamlandTerrain.Ruins.x + 10, DreamlandTerrain.Ruins.y - 10));
            var vis = Group(go.transform, "Visual");
            var model = Place(vis, "MONSTER_Alarm", go.transform.position, 0, 1.15f, collider: null);   // FBX front = +Z = the way it walks
            var eye = new GameObject("EyeLight").AddComponent<Light>(); eye.transform.SetParent(vis, false);
            eye.transform.localPosition = new Vector3(0, 3.6f, 1.8f); eye.type = LightType.Point; eye.range = 14f; eye.intensity = 4f; eye.shadows = LightShadows.None;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 1.6f; agent.height = 5f; agent.speed = 3.2f;
            var a = go.AddComponent<NightmareAlarm>();
            a.visual = vis; a.eyeLight = eye;
            var rp = Group(root, "AlarmRoute");
            Vector2[] pts = { new Vector2(-200, 10), new Vector2(-160, -50), new Vector2(-120, 40), new Vector2(-60, -20), new Vector2(-30, -100),
                              new Vector2(30, -150), new Vector2(-90, -140), new Vector2(-140, 90), new Vector2(-40, 60), new Vector2(70, -60) };
            foreach (var p in pts) { var t = Group(rp, "Roam"); t.position = Snap(G(p.x, p.y)); a.roamPoints.Add(t); }
            return a;
        }

        // =========================================================================================== pieces
        static Transform PillowFire(Transform root, Vector3 at)
        {
            var go = new GameObject("PillowFire_" + _cp);
            go.transform.SetParent(root, false); go.transform.position = at;
            var cp = go.AddComponent<Checkpoint>(); cp.order = _cp; cp.isStart = _cp == 0; cp.radius = 4f; _cp++;
            go.AddComponent<SafeZone>().radius = 9f;
            ArtKit.Prop(go.transform, "PROP_Cushion", new Vector3(0, 0, 0), 15f, new Vector3(1.6f, 0.5f, 1.2f), true);
            var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(flame.GetComponent<Collider>());
            flame.name = "Flame"; flame.transform.SetParent(go.transform, false); flame.transform.localPosition = new Vector3(0, 0.85f, 0);
            flame.transform.localScale = new Vector3(0.2f, 0.36f, 0.2f); flame.GetComponent<MeshRenderer>().sharedMaterial = Mt("Flame");
            var l = new GameObject("FireLight").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.transform.localPosition = new Vector3(0, 1.3f, 0); l.type = LightType.Point; l.range = 10f; l.intensity = 3f; l.color = Palette.Hex(0xFFB066); l.shadows = LightShadows.None;
            var pf = go.AddComponent<PillowFire>(); pf.checkpoint = cp; pf.flame = l; pf.flameVisual = flame.transform;
            // a faint warm ring on the ground shows the safe zone
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.name = "SafeRing"; ring.transform.SetParent(go.transform, false); ring.transform.localPosition = new Vector3(0, 0.35f, 0);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0); ring.transform.localScale = new Vector3(18, 18, 1);
            var lib = AssetDatabase.LoadAssetAtPath<VFXLibrary>("Assets/DreamRush/Resources/" + VFXLibrary.ResourcePath + ".asset");
            if (lib != null) { var rm = new Material(lib.ring) { name = "M_SafeRing" }; rm.color = new Color(1f, 0.72f, 0.4f, 0.28f); string rp = ArtKit.MatDir + "/M_OW_SafeRing.mat"; if (AssetDatabase.LoadAssetAtPath<Material>(rp) == null) AssetDatabase.CreateAsset(rm, rp); else { var ex = AssetDatabase.LoadAssetAtPath<Material>(rp); ex.CopyPropertiesFromMaterial(rm); rm = ex; } ring.GetComponent<MeshRenderer>().sharedMaterial = rm; }
            ring.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go.transform;
        }

        static void Star(Transform root, Vector3 at)
        {
            var s = ArtKit.Prop(root, "PROP_GalStar", Vector3.zero, 0, Vector3.one * 1.3f, true);
            if (s == null) return;
            s.position = at; s.name = "DreamStar";
            s.gameObject.AddComponent<DreamStar>();
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(s, false);
            l.type = LightType.Point; l.range = 5f; l.intensity = 2f; l.color = Palette.Hex(0xFFE08A); l.shadows = LightShadows.None;
        }

        static void Wind(Transform root, Vector3 at, float height)
        {
            var go = new GameObject("Updraft"); go.transform.SetParent(root, false); go.transform.position = at;
            var w = go.AddComponent<WindColumn>(); w.radius = 3.6f; w.height = height;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 2.2f; main.startSpeed = new ParticleSystem.MinMaxCurve(height / 2.4f, height / 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f); main.startColor = new Color(0.85f, 0.95f, 1f, 0.7f);
            var em = ps.emission; em.rateOverTime = 40f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 3.2f; sh.rotation = new Vector3(-90, 0, 0);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.12f; r.lengthScale = 2f;
            var lib = AssetDatabase.LoadAssetAtPath<VFXLibrary>("Assets/DreamRush/Resources/" + VFXLibrary.ResourcePath + ".asset");
            if (lib != null) r.sharedMaterial = lib.glowAdd;
            var swirl = ArtKit.Prop(go.transform, "PROP_GalRing", Vector3.up * 0.4f, 0, Vector3.one * 3.6f);
            if (swirl != null) swirl.gameObject.AddComponent<Spinner>().speed = 120f;
        }

        static void Ring(Transform root, Vector3 at, Vector3 forward)
        {
            var t = Place(root, "PROP_SpeedRing", at, 0, 1f, collider: null);
            if (t == null) return;
            t.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(90, 0, 0);   // torus lies in its local XZ → stand it up facing the route
            var holder = new GameObject("SpeedRing"); holder.transform.SetParent(root, false);
            holder.transform.SetPositionAndRotation(at, Quaternion.LookRotation(forward));
            t.SetParent(holder.transform, true);
            holder.AddComponent<SpeedRing>();
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(holder.transform, false);
            l.type = LightType.Point; l.range = 6f; l.intensity = 2f; l.color = Palette.Hex(0xFFD27A); l.shadows = LightShadows.None;
        }

        static void Waterfall(Transform root, Vector3 top, float drop)
        {
            var g = new GameObject("Waterfall"); g.transform.SetParent(root, false);
            var col = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(col.GetComponent<Collider>());
            col.transform.SetParent(g.transform, false);
            col.transform.position = top + Vector3.down * drop * 0.5f;
            col.transform.localScale = new Vector3(2.2f, drop * 0.5f, 0.6f);
            col.GetComponent<MeshRenderer>().sharedMaterial = Mt("Fall");
            col.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var mist = new GameObject("Mist"); mist.transform.SetParent(g.transform, false); mist.transform.position = new Vector3(top.x, DreamlandTerrain.WaterLevel + 0.3f, top.z);
            var ps = mist.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 2.5f; main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.6f); main.startColor = new Color(1, 1, 1, 0.5f); main.gravityModifier = -0.05f;
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 1.6f;
            var lib = AssetDatabase.LoadAssetAtPath<VFXLibrary>("Assets/DreamRush/Resources/" + VFXLibrary.ResourcePath + ".asset");
            if (lib != null) ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = lib.puff;
        }

        static void Beam(Transform root, Vector3 from, Vector3 to, float width)
        {
            if (Mt("Beam") == null) return;
            for (int k = 0; k < 2; k++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "GodRay"; q.transform.SetParent(root, false);
                Vector3 dir = to - from;
                q.transform.position = (from + to) / 2;
                q.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.right).normalized, -dir.normalized) * Quaternion.Euler(0, k * 90, 0);
                q.transform.localScale = new Vector3(width, dir.magnitude, 1);
                var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = Mt("Beam"); r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // =========================================================================================== helpers
        static Material Mt(string k) => M.TryGetValue(k, out var m) ? m : null;
        static Transform Group(Transform p, string n) { var t = new GameObject(n).transform; t.SetParent(p, false); return t; }
        static float Rand(float a, float b) => a + (float)R.NextDouble() * (b - a);

        static Vector2 RandIn(Vector2 c, float r0, float r1)
        {
            float a = Rand(0, Mathf.PI * 2), r = Mathf.Sqrt(Rand(r0 * r0, r1 * r1));
            return c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        static bool Ok(Vector2 p, float maxSlope, bool avoidPath)
        {
            if (H(p.x, p.y) < DreamlandTerrain.WaterLevel + 1f) return false;
            if (1f - DreamlandTerrain.Normal(p.x, p.y).y > maxSlope) return false;
            if (avoidPath && DreamlandTerrain.PathWeight(p.x, p.y) > 0.1f) return false;
            if (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) > 250f) return false;
            return true;
        }

        static Vector3 Snap(Vector3 p) => NavMesh.SamplePosition(p, out var hit, 6f, NavMesh.AllAreas) ? hit.position : p;

        static GameObject LoadModel(string name)
        {
            string path = $"{ModelDir}/{name}.fbx";
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[OpenWorld] missing model " + path); return null; }
            bool dirty = false;
            if (mi.bakeAxisConversion || mi.importAnimation || mi.importCameras || mi.importLights || mi.materialImportMode != ModelImporterMaterialImportMode.ImportStandard)
            {
                mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; dirty = true;
            }
            foreach (var kv in M)
            {
                if (!kv.Key.StartsWith("M_Kit_OW")) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key);
                if (!mi.GetExternalObjectMap().TryGetValue(id, out var ex) || ex != kv.Value) { mi.AddRemap(id, kv.Value); dirty = true; }
            }
            if (dirty) mi.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();

        /// <summary>Places an open-world model (or a kit prop) at a world position. collider: null | trunk | box | mesh | arch.</summary>
        static Transform Place(Transform parent, string name, Vector3 pos, float yaw, float scale, string collider, Color? light = null)
        {
            GameObject src;
            if (!_models.TryGetValue(name, out src) || src == null) { src = LoadModel(name) ?? ArtKit.LoadProp(name); _models[name] = src; }
            if (src == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            go.transform.localScale = Vector3.one * scale;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            DreamLook.AddOutline(go, name.StartsWith("TREE_Pine") || name.StartsWith("CRYSTAL") ? 1.2f : 1.8f);
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) { b.Encapsulate(r.bounds); r.shadowCastingMode = name.StartsWith("TREE") || name.StartsWith("ISLAND") || name.StartsWith("RUIN") || name.StartsWith("MONSTER") ? ShadowCastingMode.On : ShadowCastingMode.On; }
            switch (collider)
            {
                case "trunk":
                {
                    var cap = go.AddComponent<CapsuleCollider>();
                    cap.radius = 0.45f; cap.height = 4f; cap.center = new Vector3(0, 2f, 0);
                    break;
                }
                case "box":
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = go.transform.InverseTransformPoint(b.center);
                    Vector3 s = go.transform.InverseTransformVector(b.size);
                    bc.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) * 0.8f;
                    break;
                }
                case "mesh":
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    break;
                case "arch":
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var leg = new GameObject("LegCol"); leg.transform.SetParent(go.transform, false);
                        leg.transform.localPosition = new Vector3(s * 3.2f, 3.6f, 0);
                        leg.AddComponent<BoxCollider>().size = new Vector3(1.6f, 7.2f, 1.6f);
                    }
                    var top = new GameObject("TopCol"); top.transform.SetParent(go.transform, false);
                    top.transform.localPosition = new Vector3(0, 9.6f, 0); top.AddComponent<BoxCollider>().size = new Vector3(8.4f, 1.2f, 1.6f);
                    break;
            }
            if (light.HasValue)
            {
                var l = new GameObject("CrystalLight").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
                l.transform.localPosition = new Vector3(0, 6f, 0); l.type = LightType.Point; l.range = 22f * scale; l.intensity = 3.5f; l.color = light.Value; l.shadows = LightShadows.None;
                var glow = go.AddComponent<CrystalPulse>(); glow.l = l;
            }
            return go.transform;
        }
    }
}
