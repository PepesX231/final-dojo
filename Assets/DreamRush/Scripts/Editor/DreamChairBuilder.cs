using DreamRush.Boss;
using DreamRush.CameraSystem;
using Unity.AI.Navigation;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Enemies;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M4 graybox of "THE CHAIR THAT REFUSED" (GDD §11, §13, §14). Linear along +Z over the dream void:
    ///   A Arrival (z −3..11) → B Tutorial: walk / ottoman / cushion gaps (z 11..35) → C Little Stool + paper bridge (z 37..51)
    ///   → D Moving toy (z 57.5..69) → E Boss arena 32×25 (z 69..94).
    /// Jump distances respect the M1 envelope (max 2.1 high; gaps ≤ 2.5 in tutorial). Rebuild: DreamRush/Build/Dream Chair.
    /// </summary>
    public static class DreamChairBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Dream_Chair.unity";
        public static readonly Vector3 ChairStart = new Vector3(6f, 0f, 78f);
        public const float ArenaZ0 = 69f, ArenaZ1 = 94f, ArenaHalfW = 16f;

        static Material _walk, _carpet, _cushion, _wood, _wall, _void, _accent, _interact, _hazard, _bg;

        [MenuItem("DreamRush/Build/Dream Chair")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode before building scenes."); return; }
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            _carpet = GrayboxAssets.GridFloor("GRAYBOX_DreamCarpet", Color.Lerp(Palette.DreamLavender, Palette.WarmCream, 0.55f),
                Color.Lerp(Palette.DreamLavender, Palette.MidnightPlum, 0.25f), 10f);
            _walk = GrayboxAssets.Lit("GRAYBOX_Walkable", Palette.GrayboxWalkable);
            _cushion = GrayboxAssets.Lit("GRAYBOX_Cushion", Color.Lerp(Palette.SkyBlue, Palette.WarmCream, 0.35f));
            _wood = GrayboxAssets.Lit("GRAYBOX_Wood", Color.Lerp(Palette.ApricotOrange, Palette.MidnightPlum, 0.35f));
            _wall = GrayboxAssets.Lit("GRAYBOX_DreamWall", Color.Lerp(Palette.DreamLavender, Palette.MidnightPlum, 0.2f));
            _accent = GrayboxAssets.Lit("GRAYBOX_Accent", Palette.GrayboxAccent);
            _interact = GrayboxAssets.Lit("GRAYBOX_Interactable", Palette.GrayboxInteractable);
            _hazard = GrayboxAssets.Lit("GRAYBOX_Hazard", Palette.GrayboxHazard);
            _bg = GrayboxAssets.Lit("GRAYBOX_DreamBackground", Color.Lerp(Palette.DreamLavender, Palette.SkyBlue, 0.3f) * 0.85f);

            var root = new GameObject("GRAYBOX_Dream_Chair").transform;
            var level = new GameObject("DreamLevelManager").AddComponent<DreamLevelManager>();
            level.killY = -6f;

            BuildSectionA(root);
            BuildSectionB(root);
            var stool = BuildSectionC(root);
            BuildSectionD(root);
            BuildArena(root);
            BuildBackground(root);
            BuildSectionsAndCheckpoints(root);
            BuildBoss(root);
            SetupLighting();

            // player + stage camera
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(0, 5f, -0.5f), Quaternion.identity);
            // v2 camera: close third-person behind the apple (docs/RESEARCH_V2.md §5)
            var tpc = PlayerRigBuilder.BuildHubCamera(player);
            CameraTuning.ApplyFollow(tpc);
            var camGo = tpc.gameObject;
            var cam = camGo.GetComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            cam.backgroundColor = Color.Lerp(Palette.DreamLavender, Palette.SkyBlue, 0.45f);
            cam.nearClipPlane = 0.1f;
            camGo.transform.position = player.transform.position + new Vector3(0, 2, -5);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = camGo.transform;
            EditorUtility.SetDirty(motor);
            level.player = motor;

            new GameObject("DreamArrival").AddComponent<DreamArrival>().player = motor;
            new GameObject("SleepStability").AddComponent<SleepStability>();
            var enc = Object.FindFirstObjectByType<BossEncounter>();
            enc.player = motor;
            new GameObject("DreamHUD").AddComponent<DreamHUD>();
            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>();
            ctx.kind = SceneKind.Dream;

            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath);
            Debug.Log("[DreamRush] Dream_Chair graybox built: " + ScenePath);
        }

        // ------------------------------------------------------------------ sections
        static void BuildSectionA(Transform root)
        {
            var a = Group(root, "Section_A_Arrival");
            Box(a, "ArrivalCarpet", new Vector3(0, -0.25f, 4f), new Vector3(12, 0.5f, 14), _carpet);
            // soft rug circle marking the landing spot (readable landing area, §2.1 3D Land)
            var rug = Prim(a, PrimitiveType.Cylinder, "LandingRug", new Vector3(0, 0.01f, -0.5f), new Vector3(3.2f, 0.01f, 3.2f), _accent);
            Object.DestroyImmediate(rug.GetComponent<Collider>());
            Cutout(a, "DEC_IllustratedPlant", new Vector3(-4.8f, 1.1f, 6f), 2.2f, billboard: true);
            Cutout(a, "DEC_IllustratedPlant", new Vector3(4.9f, 1.0f, 9f), 2.0f, billboard: true);
        }

        static void BuildSectionB(Transform root)
        {
            var b = Group(root, "Section_B_Tutorial");
            // Challenge 1: walk
            Box(b, "Runner_Walk", new Vector3(0, -0.25f, 15f), new Vector3(5, 0.5f, 8), _carpet);
            // Challenge 2: low ottoman spanning the runner (must hop: 0.6 > step offset)
            Box(b, "Runner_Ottoman", new Vector3(0, -0.25f, 23f), new Vector3(5, 0.5f, 8), _carpet);
            Box(b, "Ottoman", new Vector3(0, 0.3f, 22.5f), new Vector3(5, 0.6f, 2), _cushion);
            // Challenge 3: cushion hops over the dream void (gaps 1.2 / 1.3 / 2.0)
            Box(b, "Cushion_A", new Vector3(0, -0.4f, 29.5f), new Vector3(3.2f, 0.8f, 2.6f), _cushion);
            Box(b, "Cushion_B", new Vector3(0.6f, -0.4f, 33.4f), new Vector3(3.2f, 0.8f, 2.6f), _cushion);
            Cutout(b, "DEC_PaperStar", new Vector3(-3.2f, 2.2f, 31f), 1.2f, billboard: true);
        }

        static LittleStoolController BuildSectionC(Transform root)
        {
            var c = Group(root, "Section_C_LittleStool");
            Box(c, "StoolRoom_Floor", new Vector3(0, -0.25f, 44f), new Vector3(14, 0.5f, 14), _carpet);
            // low fence rails so the stool chase reads as a "room" (not a wall: hoppable 0.5)
            Box(c, "Rail_W", new Vector3(-7.25f, 0.25f, 44f), new Vector3(0.5f, 0.5f, 14), _wood);
            Box(c, "Rail_E", new Vector3(7.25f, 0.25f, 44f), new Vector3(0.5f, 0.5f, 14), _wood);

            var stoolGo = new GameObject("ENEMY_LittleStool");
            stoolGo.transform.SetParent(c, false);
            stoolGo.transform.localPosition = new Vector3(3.5f, 0, 47.5f);
            var visual = Group(stoolGo.transform, "Visual");
            Box(visual, "Seat", new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.12f, 0.7f), _wood).GetComponent<Collider>().enabled = false;
            foreach (var (x, z) in new[] { (-0.25f, -0.25f), (0.25f, -0.25f), (-0.25f, 0.25f), (0.25f, 0.25f) })
                Box(visual, "Leg", new Vector3(x, 0.2f, z), new Vector3(0.09f, 0.4f, 0.09f), _wood).GetComponent<Collider>().enabled = false;
            Prim(visual, PrimitiveType.Sphere, "Eye_L", new Vector3(-0.13f, 0.53f, 0.33f), new Vector3(0.14f, 0.14f, 0.05f), GrayboxAssets.Lit("GRAYBOX_EyeWhite", Color.white)).GetComponent<Collider>().enabled = false;
            Prim(visual, PrimitiveType.Sphere, "Eye_R", new Vector3(0.13f, 0.53f, 0.33f), new Vector3(0.14f, 0.14f, 0.05f), GrayboxAssets.Lit("GRAYBOX_EyeWhite", Color.white)).GetComponent<Collider>().enabled = false;
            var stool = stoolGo.AddComponent<LittleStoolController>();
            var wps = new Transform[4];
            Vector3[] p = { new Vector3(-4, 0, 40), new Vector3(4, 0, 40), new Vector3(4, 0, 48), new Vector3(-4, 0, 48) };
            for (int i = 0; i < 4; i++) wps[i] = Pose(c, $"StoolWaypoint_{i}", p[i]);
            stool.waypoints = wps;

            // exit: 6.5 m void gap — only the paper bridge (unfolds when the stool is caught) crosses it
            var hinge = Group(c, "PaperBridge_Hinge");
            hinge.localPosition = new Vector3(0, 0f, 51f);
            var plank = Box(hinge, "PaperPlank", new Vector3(0, -0.06f, 3.35f), new Vector3(3f, 0.12f, 6.7f), GrayboxAssets.Lit("GRAYBOX_Paper", Palette.WarmCream));
            var bridge = hinge.gameObject.AddComponent<PaperBridge>();
            bridge.trigger = stool;
            Cutout(c, "DEC_PaperCloud_B", new Vector3(5f, 3.2f, 52f), 3f, billboard: true);
            return stool;
        }

        static void BuildSectionD(Transform root)
        {
            var d = Group(root, "Section_D_MovingToy");
            Box(d, "ToyRoom_Floor", new Vector3(0, -0.25f, 63.25f), new Vector3(12, 0.5f, 11.5f), _carpet);
            // A patrolling wind-up toy teaches observation and safe timing.
            var toy = Group(d, "PROP_WindUpToy");
            toy.localPosition = new Vector3(-3f, 0, 63f);
            Box(toy, "Body", new Vector3(0, 0.4f, 0), new Vector3(0.9f, 0.8f, 1.2f), _interact);
            Prim(toy, PrimitiveType.Cylinder, "Key", new Vector3(0, 0.95f, -0.3f), new Vector3(0.12f, 0.15f, 0.12f), _accent);
            var mover = toy.gameObject.AddComponent<PingPongMover>();
            mover.offset = new Vector3(6f, 0, 0);
            mover.period = 4f;
            toy.gameObject.AddComponent<DreamLogic.ToyEffectReceiver>().mover = mover;
            Cutout(d, "DEC_PaperMoon", new Vector3(4.5f, 3f, 66f), 1.8f, billboard: true);
        }

        static void BuildArena(Transform root)
        {
            var e = Group(root, "Section_E_BossArena");
            float cz = (ArenaZ0 + ArenaZ1) * 0.5f, depth = ArenaZ1 - ArenaZ0, w = ArenaHalfW * 2f;
            Box(e, "Arena_CentralCarpet", new Vector3(0, -0.25f, cz), new Vector3(w, 0.5f, depth), _carpet);
            // boundaries (3 m) with an entrance in the south wall
            Box(e, "Wall_N", new Vector3(0, 1.5f, ArenaZ1 + 0.25f), new Vector3(w + 1, 3, 0.5f), _wall);
            Box(e, "Wall_E", new Vector3(ArenaHalfW + 0.25f, 1.5f, cz), new Vector3(0.5f, 3, depth), _wall);
            Box(e, "Wall_W", new Vector3(-ArenaHalfW - 0.25f, 1.5f, cz), new Vector3(0.5f, 3, depth), _wall);
            Box(e, "Wall_S_L", new Vector3(-9f, 1.5f, ArenaZ0 - 0.25f), new Vector3(14f, 3, 0.5f), _wall);
            Box(e, "Wall_S_R", new Vector3(9f, 1.5f, ArenaZ0 - 0.25f), new Vector3(14f, 3, 0.5f), _wall);
            // rounded corners (soft shapes, GDD §14.2)
            foreach (var (x, z) in new[] { (-ArenaHalfW, ArenaZ1), (ArenaHalfW, ArenaZ1), (-ArenaHalfW, ArenaZ0), (ArenaHalfW, ArenaZ0) })
                Prim(e, PrimitiveType.Cylinder, "Corner", new Vector3(x, 1.5f, z), new Vector3(1.6f, 1.5f, 1.6f), _wall);

            // A — low platform (skilled shortcut), NW
            Box(e, "A_LowPlatform", new Vector3(-11f, 0.5f, 89f), new Vector3(7, 1, 5), _walk);
            // B — balcony / shortcut overlooking the chair's route, NE, reached by cushion steps
            Box(e, "B_Balcony", new Vector3(11f, 1f, 89.5f), new Vector3(8, 2, 4), _walk);
            Box(e, "B_Step1", new Vector3(14.3f, 0.3f, 82.5f), new Vector3(2.4f, 0.6f, 2.4f), _cushion);
            Box(e, "B_Step2", new Vector3(14.3f, 0.65f, 85.2f), new Vector3(2.4f, 1.3f, 2.4f), _cushion);
            // D — cushion route (alternative lane), SW → up to A
            Box(e, "D_Cushion1", new Vector3(-13.5f, 0.25f, 72.5f), new Vector3(2.6f, 0.5f, 2.6f), _cushion);
            Box(e, "D_Cushion2", new Vector3(-13.5f, 0.25f, 76f), new Vector3(2.6f, 0.5f, 2.6f), _cushion);
            Box(e, "D_Cushion3", new Vector3(-12f, 0.25f, 79.5f), new Vector3(2.6f, 0.5f, 2.6f), _cushion);
            Box(e, "D_Cushion4", new Vector3(-11.5f, 0.35f, 83f), new Vector3(2.6f, 0.7f, 2.6f), _cushion);
            // furniture obstacles (cover / cornering)
            Box(e, "Obstacle_Sofa", new Vector3(-5f, 0.6f, 82f), new Vector3(5f, 1.2f, 1.8f), _wood);
            Box(e, "Obstacle_CoffeeTable", new Vector3(-1f, 0.3f, 76f), new Vector3(2.6f, 0.6f, 1.6f), _wood);
            Prim(e, PrimitiveType.Cylinder, "Obstacle_FloorLamp", new Vector3(5f, 1.5f, 86f), new Vector3(0.7f, 1.5f, 0.7f), _accent);
            // switch (future: raises a paper wall to block a lane)
            Box(e, "Switch", new Vector3(-15.75f, 0.6f, 70.6f), new Vector3(0.2f, 0.5f, 0.5f), _hazard);

            // arena NavMesh (baked at runtime by BossEncounter from the static colliders in this group)
            var surface = e.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

            // posters on the walls (attached, not billboarded — §5.8)
            for (int i = 0; i < 4; i++)
                Cutout(e, "DEC_PaperStar", new Vector3(-12f + i * 8f, 2.1f, ArenaZ1 - 0.02f), 1.1f, billboard: false, faceSouth: true);

            var zone = new GameObject("CameraZone_Arena").AddComponent<StageCameraZone>();
            zone.transform.SetParent(e, false);
            zone.transform.localPosition = new Vector3(0, 5, cz);
            zone.size = new Vector3(w + 2, 20, depth + 2);
            zone.priority = 10;
            zone.pitch = 42f; zone.distance = 15.5f; zone.fieldOfView = 45f;
        }

        static void BuildBoss(Transform root)
        {
            var dynamicGroup = Group(root, "Arena_Dynamic");   // NOT part of the NavMesh bake
            // E — movable toy block (Obstacle method §16.7), SE
            var block = Box(dynamicGroup, "E_MovableBlock", new Vector3(10f, 0.6f, 73f), new Vector3(1.2f, 1.2f, 1.2f), _interact);
            block.AddComponent<PushableBlock>();

            // toy cars rolling lanes during the chase (pitch: obstacles cost distance)
            ToyCar(dynamicGroup, "HAZ_ToyCar_A", new Vector3(-12f, 0f, 78.5f), new Vector3(10f, 0f, 78.5f), Palette.ApricotOrange, 3.2f);
            ToyCar(dynamicGroup, "HAZ_ToyCar_B", new Vector3(-9.5f, 0f, 72f), new Vector3(-9.5f, 0f, 90f), Palette.SkyBlue, 2.6f);

            var chairGo = new GameObject("BOSS_Chair");
            chairGo.transform.SetParent(dynamicGroup, false);
            chairGo.transform.localPosition = ChairStart;
            chairGo.transform.localRotation = Quaternion.Euler(0, 200f, 0);
            BuildGrayboxChair(chairGo.transform);
            var col = chairGo.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.55f, 0); col.size = new Vector3(1.1f, 0.9f, 1.05f);
            var rb = chairGo.AddComponent<Rigidbody>(); rb.isKinematic = true;
            var ctrl = chairGo.AddComponent<BossChairController>();
            ctrl.config = GrayboxAssets.ChairConfig();
            ctrl.arenaBounds = new Bounds(new Vector3(0, 0.5f, (ArenaZ0 + ArenaZ1) * 0.5f), new Vector3(ArenaHalfW * 2f - 0.5f, 6f, ArenaZ1 - ArenaZ0 - 0.5f));
            chairGo.AddComponent<DreamLogic.ChairEffectReceiver>();
            var anim = chairGo.AddComponent<ChairAnimator>();
            anim.chair = ctrl;
            anim.fx = GrayboxAssets.FxLibrary();
            anim.visual = chairGo.transform.Find("Visual");

            var enc = new GameObject("BossEncounter").AddComponent<BossEncounter>();
            enc.chair = ctrl;
            enc.arenaSurface = root.Find("Section_E_BossArena").GetComponent<NavMeshSurface>();
            var seat = chairGo.AddComponent<ChairSeatInteraction>();
            seat.chair = ctrl;
            seat.encounter = enc;
        }

        static void ToyCar(Transform parent, string name, Vector3 from, Vector3 to, Color color, float speed)
        {
            var car = new GameObject(name);
            car.transform.SetParent(parent, false);
            car.transform.position = from;
            var v = Group(car.transform, "Visual");
            var body = GrayboxAssets.Lit("GRAYBOX_Toy_" + name, color);
            var ink = GrayboxAssets.Lit("GRAYBOX_Ink", Palette.DarkInk);
            var cream = GrayboxAssets.Lit("GRAYBOX_Cream", Palette.WarmCream);
            Box(v, "Body", new Vector3(0, 0.26f, 0), new Vector3(0.6f, 0.26f, 0.95f), body);
            Box(v, "Cabin", new Vector3(0, 0.47f, -0.08f), new Vector3(0.5f, 0.2f, 0.5f), cream);
            foreach (var (x, z) in new[] { (-0.3f, 0.3f), (0.3f, 0.3f), (-0.3f, -0.3f), (0.3f, -0.3f) })
                Prim(v, PrimitiveType.Cylinder, "Wheel", new Vector3(x, 0.13f, z), new Vector3(0.24f, 0.04f, 0.24f), ink).transform.localRotation = Quaternion.Euler(0, 0, 90);
            Box(v, "KeyStem", new Vector3(0, 0.66f, -0.08f), new Vector3(0.05f, 0.18f, 0.05f), ink);
            Box(v, "Key", new Vector3(0, 0.78f, -0.08f), new Vector3(0.3f, 0.12f, 0.04f), body);
            foreach (var c in v.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            // painted toy-track strip: telegraphs the lane so the hazard is learnable, not a surprise
            Vector3 mid = (from + to) * 0.5f;
            var track = Box(parent, name + "_Track", mid + Vector3.up * 0.006f, new Vector3(0.95f, 0.012f, Vector3.Distance(from, to) + 1.2f),
                GrayboxAssets.Lit("GRAYBOX_ToyTrack", Color.Lerp(color, Palette.WarmCream, 0.55f)));
            track.transform.rotation = Quaternion.LookRotation(to - from);
            Object.DestroyImmediate(track.GetComponent<Collider>());
            track.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var h = car.AddComponent<RollingToyHazard>();
            h.from = from; h.to = to; h.speed = speed;
        }

        public static void BuildGrayboxChair(Transform p)
        {
            var visual = Group(p, "Visual");
            // M7: hand-made chair model (build_chair_boss.py). Graybox stays as fallback when the FBX is missing.
            if (ChairCharacterSetup.ModelExists && ChairCharacterSetup.Setup() != null &&
                ChairCharacterSetup.Instantiate(visual, "BOSS_Chair_Model", withFace: true) != null)
                return;
            Box(visual, "Seat", new Vector3(0, 0.62f, 0), new Vector3(1.1f, 0.16f, 1.05f), _wood);
            foreach (var (x, z) in new[] { (-0.42f, -0.4f), (0.42f, -0.4f), (-0.42f, 0.4f), (0.42f, 0.4f) })
                Box(visual, "Leg", new Vector3(x, 0.28f, z), new Vector3(0.13f, 0.56f, 0.13f), _wood);
            Box(visual, "Backrest", new Vector3(0, 1.22f, -0.45f), new Vector3(1.1f, 1.05f, 0.14f), _wood);
            var white = GrayboxAssets.Lit("GRAYBOX_EyeWhite", Color.white);
            var ink = GrayboxAssets.Lit("GRAYBOX_Ink", Palette.DarkInk);
            Prim(visual, PrimitiveType.Sphere, "Eye_L", new Vector3(-0.24f, 1.36f, -0.37f), new Vector3(0.26f, 0.3f, 0.06f), white);
            Prim(visual, PrimitiveType.Sphere, "Eye_R", new Vector3(0.24f, 1.36f, -0.37f), new Vector3(0.26f, 0.3f, 0.06f), white);
            Prim(visual, PrimitiveType.Sphere, "Pupil_L", new Vector3(-0.22f, 1.34f, -0.34f), new Vector3(0.1f, 0.12f, 0.04f), ink);
            Prim(visual, PrimitiveType.Sphere, "Pupil_R", new Vector3(0.22f, 1.34f, -0.34f), new Vector3(0.1f, 0.12f, 0.04f), ink);
            foreach (var c in visual.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        }

        static void BuildBackground(Transform root)
        {
            var bg = Group(root, "Background_FloatingFurniture");
            // giant furniture tilted in the dream sky — low saturation so the play space stays readable (§5.3)
            Box(bg, "GiantSofa", new Vector3(-24f, 4f, 22f), new Vector3(12f, 4f, 5f), _bg).transform.localRotation = Quaternion.Euler(12, 35, -18);
            Box(bg, "GiantBookA", new Vector3(22f, 7f, 40f), new Vector3(6f, 1f, 8f), _bg).transform.localRotation = Quaternion.Euler(-25, 10, 30);
            Prim(bg, PrimitiveType.Cylinder, "GiantLamp", new Vector3(26f, 2f, 70f), new Vector3(2f, 9f, 2f), _bg).transform.localRotation = Quaternion.Euler(0, 0, 14);
            Box(bg, "GiantDoorFrame", new Vector3(-28f, 6f, 70f), new Vector3(1f, 14f, 8f), _bg).transform.localRotation = Quaternion.Euler(0, 0, -10);
            foreach (var c in bg.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var clouds = Group(root, "Background_PaperClouds");
            Vector3[] cp = { new Vector3(-10, 6, 8), new Vector3(12, 8, 18), new Vector3(-14, 9, 40), new Vector3(15, 7, 58), new Vector3(-6, 12, 100), new Vector3(10, 11, 102) };
            for (int i = 0; i < cp.Length; i++)
                Cutout(clouds, i % 2 == 0 ? "DEC_PaperCloud_A" : "DEC_PaperCloud_B", cp[i], 5f + (i % 3), billboard: true);
        }

        static void BuildSectionsAndCheckpoints(Transform root)
        {
            var g = Group(root, "Logic_SectionsAndCheckpoints");
            Section(g, "A", "Arrival", "Where am I...? Head toward the strange living room.", 0, new Vector3(0, 3, 4), new Vector3(14, 14, 16));
            Section(g, "B", "Tutorial", "Hop over the ottoman and across the cushions.", 1, new Vector3(0, 3, 23.5f), new Vector3(12, 14, 23));
            Section(g, "C", "Little Stool", "That stool won't hold still. Catch it!", 2, new Vector3(0, 3, 45.5f), new Vector3(16, 14, 19));
            Section(g, "D", "Moving Toy", "Watch the moving toy and find a safe route past it.", 3, new Vector3(0, 3, 62.5f), new Vector3(14, 14, 13));
            Section(g, "E", "The Living Room", "???", 4, new Vector3(0, 3, 81.5f), new Vector3(34, 14, 26));
            Check(g, 0, new Vector3(0, 0, 0), true);
            Check(g, 1, new Vector3(0, 0, 13), false);
            Check(g, 2, new Vector3(0, 0, 38.5f), false);
            Check(g, 3, new Vector3(0, 0, 59f), false);
            Check(g, 4, new Vector3(0, 0, 70.5f), false);
        }

        static void SetupLighting()
        {
            var sun = new GameObject("DreamKeyLight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.Lerp(Color.white, Palette.ApricotOrange, 0.3f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55, -25, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(Palette.SkyBlue, Color.white, 0.4f);
            RenderSettings.ambientEquatorColor = Color.Lerp(Palette.DreamLavender, Color.white, 0.3f);
            RenderSettings.ambientGroundColor = Color.Lerp(Palette.MidnightPlum, Palette.DreamLavender, 0.5f);
            RenderSettings.skybox = null;
        }

        // ------------------------------------------------------------------ helpers
        static void Section(Transform p, string id, string name, string objective, int order, Vector3 pos, Vector3 size)
        {
            var s = new GameObject("Section_" + id).AddComponent<DreamSection>();
            s.transform.SetParent(p, false);
            s.transform.localPosition = pos;
            s.sectionId = id; s.displayName = name; s.objective = objective; s.order = order; s.size = size;
        }

        static void Check(Transform p, int order, Vector3 pos, bool start)
        {
            var c = new GameObject("Checkpoint_" + order).AddComponent<Checkpoint>();
            c.transform.SetParent(p, false);
            c.transform.localPosition = pos;
            c.order = order; c.isStart = start;
        }

        static void Cutout(Transform parent, string tex, Vector3 pos, float height, bool billboard, bool faceSouth = true)
        {
            var mat = GrayboxAssets.Cutout(tex);
            var t = mat.mainTexture;
            float aspect = t != null ? (float)t.width / t.height : 1f;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = tex;
            Object.DestroyImmediate(q.GetComponent<Collider>()); // decorative Type A: never blocks movement (§5.7)
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos;
            q.transform.localScale = new Vector3(height * aspect, height, 1f);
            q.transform.localRotation = faceSouth ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;
            if (billboard) q.AddComponent<YBillboard>();
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Transform Pose(Transform parent, string name, Vector3 pos)
        {
            var t = Group(parent, name);
            t.localPosition = pos;
            return t;
        }

        static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material m) => Prim(parent, PrimitiveType.Cube, name, pos, size, m);

        static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }
    }
}
