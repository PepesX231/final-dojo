using System.Collections.Generic;
using DreamRush.Boss;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.Palace;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using DreamRush.World1;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// WORLD 1 — "The Chair Who Wouldn't Be Sat On" (docs/LEVEL1_DESIGN.md). One big scene, five chapters, each
    /// drawn in a different style, each teaching one mechanic (teach → develop → twist), ending in the boss.
    ///   Ch1 Pop-up Picture Book  (0,0,0)    pop-up stairs → timed pages → catapult pages
    ///   Ch2 Paper Theater 2.5D   (0,0,400)  side rail camera: sliding sets → crumbling bridge → back-layer door
    ///   Ch3 Toy Galaxy           (0,0,800)  bounce → chained launches → orbiting islands
    ///   Ch4 Phantom Shelf-tops   (0,0,1200) CHASE the chair, jump the rolling books
    ///   Ch5 Cathedral of Throne  (0,0,1600) BOSS: THE CHAIR + skills (stomp, volley, musical chairs)
    /// The Throne Tower landmark is visible ahead in every chapter.
    /// </summary>
    public static class World1Builder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Dream_World1.unity";
        const string ProfileDir = "Assets/DreamRush/Settings/World1";
        static Dictionary<string, Material> M;
        static Material Mt(string k) => M[k];
        static int _cpOrder;

        [MenuItem("DreamRush/Build/World 1 (big level)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            CardAssetsBuilder.Build();
            DreamLookSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            M = Materials();
            _cpOrder = 0; _sectionOrder = 0;
            var root = new GameObject("WORLD1").transform;
            var level = new GameObject("DreamLevelManager").AddComponent<DreamLevelManager>();
            level.killY = -10f;
            var director = new GameObject("ChapterDirector").AddComponent<ChapterDirector>();
            var psDirector = new GameObject("ProblemSolvingDirector").AddComponent<ProblemSolvingDirector>();

            director.chapters.Add(Chapter1(root));
            director.chapters.Add(Chapter2(root));
            director.chapters.Add(Chapter3(root));
            director.chapters.Add(Chapter4(root));
            director.chapters.Add(Chapter5(root, out var encounter));

            // player + close follow camera
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(0, 7f, 0f), Quaternion.identity);
            var tpc = PlayerRigBuilder.BuildHubCamera(player);
            CameraTuning.ApplyFollow(tpc);
            var cam = tpc.GetComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 700f;
            tpc.transform.position = player.transform.position + new Vector3(0, 2, -5);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = tpc.transform;
            if (player.GetComponent<GrindController>() == null) player.AddComponent<GrindController>();   // Densha train-track rails
            EditorUtility.SetDirty(motor);
            level.player = motor;
            encounter.player = motor;

            new GameObject("DreamArrival").AddComponent<DreamArrival>().player = motor;
            new GameObject("SleepStability").AddComponent<SleepStability>();
            new GameObject("DreamHUD").AddComponent<DreamHUD>();
            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>();
            ctx.kind = SceneKind.Dream; ctx.music = DreamRush.Audio.Music.World1;

            DenshaStyle(new GameObject("StylePack").AddComponent<StylePack>());
            RenderSettings.skybox = director.chapters[0].skybox;
            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[DreamRush] World 1 built: " + ScenePath);
        }

        // =====================================================================================================
        static Dictionary<string, Material> Materials()
        {
            var m = ArtKit.Materials();
            Color H(int hex) => Palette.Hex(hex);
            // chapter palettes (DreamToon, hand-picked; one accent per chapter)
            m["Cardboard"] = ArtKit.Mat("M_W1_Cardboard", H(0xFFC24A), rim: 0.15f);
            m["CardboardDark"] = ArtKit.Mat("M_W1_CardboardDark", H(0xE58A2E), rim: 0.1f);
            m["StageWood"] = ArtKit.Mat("M_W1_StageWood", H(0x2B3A8C), "T_ToyBlock", new Vector2(0.1f, 0.1f), rim: 0.1f);
            m["Curtain"] = ArtKit.Mat("M_W1_Curtain", H(0xFF3355), "T_Felt", new Vector2(2, 6), rim: 0.25f, rampSoft: 0.6f);
            m["Gold"] = ArtKit.Mat("M_W1_Gold", H(0xFFD21F), spec: 0.6f, rim: 0.3f);
            m["Backdrop"] = ArtKit.Mat("M_W1_Backdrop", H(0x173A9E), rampSoft: 0.8f);
            m["BackdropHill"] = ArtKit.Mat("M_W1_BackdropHill", H(0x0E8A9A), rampSoft: 0.8f);
            m["Silhouette"] = ArtKit.Mat("M_W1_Silhouette", H(0x0B1240), rampSoft: 0.9f);
            m["ToyRed"] = ArtKit.Mat("M_W1_ToyRed", H(0xFF3D5A), rim: 0.35f, spec: 0.25f);
            m["ToyBlue"] = ArtKit.Mat("M_W1_ToyBlue", H(0x1F8BFF), rim: 0.35f, spec: 0.25f);
            m["ToyYellow"] = ArtKit.Mat("M_W1_ToyYellow", H(0xFFD21F), rim: 0.35f, spec: 0.25f);
            m["ToyGreen"] = ArtKit.Mat("M_W1_ToyGreen", H(0x19C9A8), rim: 0.35f, spec: 0.25f);
            m["ToyWhite"] = ArtKit.Mat("M_W1_ToyWhite", H(0xF6F1FF), rim: 0.35f);
            m["Pad"] = ArtKit.Mat("M_W1_Pad", H(0x6FD9FF), "T_Felt", rim: 0.4f, rampSoft: 0.5f);
            m["Ink"] = ArtKit.Mat("M_W1_Ink", H(0x101A4A), rim: 0.0f, rampSoft: 0.2f);
            m["InkRed"] = ArtKit.Mat("M_W1_InkRed", H(0xFFC21F), rim: 0.2f, rampSoft: 0.2f);
            m["InkWhite"] = ArtKit.Mat("M_W1_InkWhite", H(0xFFFFFF), rim: 0.1f, rampSoft: 0.2f);
            m["Stone"] = ArtKit.Mat("M_W1_Stone", H(0x3F5BC8), "T_PageEdge", new Vector2(2, 2), rim: 0.12f, rampSoft: 0.5f);
            m["StoneDark"] = ArtKit.Mat("M_W1_StoneDark", H(0x1C2A78), rim: 0.1f, rampSoft: 0.5f);
            m["Banner"] = ArtKit.Mat("M_W1_Banner", H(0x13B5C2), "T_Felt", new Vector2(1, 3), doubleSided: true, rampSoft: 0.6f);
            m["Candle"] = ArtKit.Mat("M_W1_Candle", H(0xF3E6CC), rim: 0.2f);
            m["Glass"] = ArtKit.Mat("M_W1_Glass", H(0x7AD1FF), spec: 0.6f, rim: 0.4f);
            m["Flame"] = Unlit("M_W1_Flame", new Color(2.4f, 1.5f, 0.6f));
            m["Glow"] = Unlit("M_W1_Glow", new Color(1.6f, 1.3f, 2.2f));
            m["Beam"] = AssetDatabase.LoadAssetAtPath<Material>(ArtKit.MatDir + "/M_VFX_BeamAdd.mat");
            m["Rail"] = ArtKit.Mat("M_W1_Rail", H(0xFFE14A), rim: 0.3f, spec: 0.5f);
            m["Rail"].SetColor("_Emission", new Color(0.9f, 0.6f, 0.05f));
            m["RailPost"] = ArtKit.Mat("M_W1_RailPost", H(0x101A4A), rim: 0.1f);
            // god rays: v1 blew the frame out to white → a soft tinted copy for World 1
            if (m["Beam"] != null)
            {
                string bp = ArtKit.MatDir + "/M_W1_BeamSoft.mat";
                var soft = AssetDatabase.LoadAssetAtPath<Material>(bp);
                if (soft == null) { soft = new Material(m["Beam"]) { name = "M_W1_BeamSoft" }; AssetDatabase.CreateAsset(soft, bp); }
                soft.CopyPropertiesFromMaterial(m["Beam"]);
                soft.SetColor("_BaseColor", new Color(1f, 0.92f, 0.6f, 0.22f));
                EditorUtility.SetDirty(soft);
                m["Beam"] = soft;
            }
            return m;
        }

        static Material Unlit(string name, Color hdr)
        {
            string path = $"{ArtKit.MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", hdr);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>World 1 look (user brief 2026-10-20, Densha-Attack refs): thick ink, razor cel shading, teal print dots,
        /// posterized light, high-key yellow/teal/blue colour, speed lines when Pip runs, Pip in yellow headphones.</summary>
        public static void DenshaStyle(StylePack sp)
        {
            sp.ink = 1f; sp.inkColor = new Color(0.05f, 0.06f, 0.16f); sp.inkWidth = 2.1f; sp.inkFar = 110f;
            sp.halftone = 0.55f; sp.halftoneColor = new Color(0.0f, 0.55f, 0.75f, 0.9f); sp.halftoneScale = 5.5f;
            sp.posterize = 0.45f; sp.saturate = 0.3f;
            sp.celHard = 1f; sp.shadowTint = new Color(0.36f, 0.5f, 0.95f, 0.85f);    // shadows go cool blue, never grey
            sp.speedColor = new Color(1f, 1f, 1f, 0.75f); sp.speedLines = true;
            sp.outfit = StylePack.Outfit.DenshaCap;
            sp.pipRim = new Color(1f, 0.92f, 0.3f); sp.pipRimStrength = 0.5f; sp.pipRampSoftness = 0.03f; sp.pipShadow = new Color(0.45f, 0.42f, 0.95f);
        }

        /// <summary>Anime sky (FantasySky shader): clean gradient, a hot band at the horizon, big white cumulus.</summary>
        internal static Material AnimeSky(string name, Color zenith, Color horizon, Color band, Color ground, float clouds, Color cloudShade, float stars = 0f, Vector3? sun = null)
        {
            string path = $"{ArtKit.MatDir}/{name}.mat";
            var sh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DreamRush/Shaders/FantasySky.shader");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetColor("_Zenith", zenith); m.SetColor("_Horizon", horizon); m.SetColor("_Band", band); m.SetColor("_Ground", ground);
            m.SetColor("_CloudColor", Color.white); m.SetColor("_CloudShade", cloudShade); m.SetFloat("_CloudCover", clouds); m.SetFloat("_CloudScale", 1.3f);
            m.SetFloat("_StarStrength", stars); m.SetFloat("_HaloStrength", 0.8f);
            m.SetVector("_SunDir", (Vector4)(sun ?? new Vector3(-0.4f, 0.35f, 0.85f)).normalized);
            EditorUtility.SetDirty(m);
            return m;
        }

        // =====================================================================================================
        // CHAPTER 1 — Pop-up Picture Book (soft storybook 3D). Teach: pop-up stairs. Develop: timed pages. Twist: catapults.
        static ChapterInfo Chapter1(Transform root)
        {
            var g = Group(root, "CH1_PopUpBook");
            var look = Group(g, "Look");
            var ch = new ChapterInfo
            {
                title = "THE POP-UP PICTURE BOOK", tag = "STORYBOOK",
                // Densha morning: deep anime blue zenith → pale cyan horizon, lemon band, big white clouds
                skybox = AnimeSky("M_Sky_W1_Ch1", Palette.Hex(0x1466D8), Palette.Hex(0xBFF3FF), Palette.Hex(0xFFE95C), Palette.Hex(0x2A5FB8), 0.58f, Palette.Hex(0x8FB8F0)),
                fogColor = Palette.Hex(0xA9E6FA), fogStart = 70, fogEnd = 320,
                ambientSky = Palette.Hex(0xCFEFFF), ambientEquator = Palette.Hex(0xA8C8F0), ambientGround = Palette.Hex(0x5A6AB0),
                halftoneColor = new Color(0.0f, 0.55f, 0.8f, 0.9f), shadowTint = new Color(0.38f, 0.55f, 0.98f, 0.85f),
                lookRoot = look.gameObject,
            };
            Sun(look, new Vector3(48, -30, 0), Palette.Hex(0xFFF4D6), 1.35f);
            Fill(look, new Vector3(-15, 150, 0), Palette.Hex(0x9CC8FF), 0.12f);
            Profile(look, "PP_W1_Ch1", exposure: -0.35f, contrast: 26, saturation: 24, bloom: 0.18f, vignette: 0.12f, chroma: 0.05f);

            // A — landing spread (living-room page) + pillow fire
            Page(g, "A_Landing", new Vector3(0, 0, 6), 24, 18, 0.6f, 1);
            ArtKit.Rug(g, new Vector3(0, 0, 4), 6.5f);
            ch.spawn = PillowFire(g, new Vector3(0, 0, 2), 0, true);
            ArtKit.Prop(g, "PROP_Sofa", new Vector3(-7.5f, 0, 10f), 160f, null, true);
            ArtKit.Prop(g, "PROP_FloorLamp", new Vector3(8.5f, 0, 11f), 200f, null, true);
            ArtKit.Prop(g, "PROP_CoffeeTable", new Vector3(5, 0, 6), 15f, null, true);
            for (int i = 0; i < 4; i++) ArtKit.Prop(g, "PROP_ToyBlock", new Vector3(-3 + i * 1.3f, 0, 12 + (i % 2) * 0.8f), i * 23f, Vector3.one * (0.7f + 0.1f * i), true);
            ColliderBox(g, "Sofa_Col", new Vector3(-7.5f, 0.6f, 10f), new Vector3(5f, 1.2f, 1.8f), 160f);
            ColliderBox(g, "Table_Col", new Vector3(5, 0.35f, 6), new Vector3(2.6f, 0.7f, 1.6f), 15f);
            Star(g, new Vector3(-9.2f, 0.9f, 12.6f), 1);   // hidden behind the sofa
            Sign(g, new Vector3(0, 0, 14.5f), "ไปทางนี้ ▶");

            // B — teach: pop-up stairs rise from the void as you approach
            Page(g, "B_Approach", new Vector3(0, 0, 21.5f), 12, 11, 0.6f, 2);
            CognitiveTotem(g, "Trial_Ch1_Observe", new Vector3(-3.8f, 0f, 19.5f),
                "TRIAL 01-A // PATTERN OBSERVATION",
                "วิเคราะห์การทำงานของหน้ากระดาษ: แถบมุมสีฟ้าจะเด้งขึ้นเมื่อเดินเข้าใกล้ในระยะ 7 เมตร!",
                null);
            float[] tops = { 0.8f, 1.6f, 2.4f, 3.2f };
            for (int i = 0; i < 4; i++)
            {
                var pg = PopPage(g, $"B_PopStair{i}", new Vector3(0, tops[i], 28.6f + i * 2.5f), new Vector2(4.2f, 2.4f), PopUpPage.Mode.Stairs, i * 0.12f);
                pg.riseHeight = 1.6f + i * 0.4f;
            }
            Page(g, "B_Plateau", new Vector3(0, 3.2f, 42.5f), 14, 10, 0.8f, 3);
            PillowFire(g, new Vector3(0, 3.2f, 40f), 0, false);
            ArtKit.Prop(g, "PROP_Cushion", new Vector3(4.5f, 3.2f, 45f), 20f, new Vector3(2.2f, 0.6f, 2.2f), true);

            // C — develop: timed pages (fold back 3 s after you land) zig-zag over the void
            var safetyBridgeCh1 = Box(g, "C_LogicBridge", new Vector3(0.4f, 4.4f, 59f), new Vector3(2.6f, 0.35f, 22f), Mt("Pad"));
            CognitiveTotem(g, "Trial_Ch1_RuleDeduction", new Vector3(-4.2f, 3.2f, 43.2f),
                "TRIAL 01-B // RULE DEDUCTION & BYPASS",
                "เปิดใช้งานโหนดตรรกะสำเร็จ! สร้างสะพานพลังงานเชื่อมผ่านหน้ากระดาษพับเวลา และชี้เป้าทางลัดรางรถไฟสีเหลือง!",
                safetyBridgeCh1);
            Vector3[] tp = { new Vector3(3, 3.6f, 50.5f), new Vector3(-2, 4.0f, 54f), new Vector3(2.5f, 4.4f, 57.5f), new Vector3(-1.5f, 4.8f, 61f), new Vector3(2, 5.2f, 64.5f), new Vector3(0, 5.6f, 68f) };
            for (int i = 0; i < tp.Length; i++) PopPage(g, $"C_Timed{i}", tp[i], new Vector2(3.0f, 3.0f), PopUpPage.Mode.Timed, 0f).riseHeight = 1.4f;
            Star(g, new Vector3(-3.6f, 6.4f, 61f), 1);
            Page(g, "C_Plateau", new Vector3(0, 5.6f, 74f), 12, 8, 0.8f, 4);
            // rail line: a yellow train track that sweeps over the timed pages — the fast (and showy) way across
            Rail(g, "C_RailLine", new Vector3(-5.5f, 4.1f, 44.5f), new Vector3(-7.5f, 5.6f, 51f), new Vector3(-8.5f, 7.4f, 58f), new Vector3(-7.5f, 7.6f, 65f), new Vector3(-4.5f, 6.5f, 71.5f));
            PillowFire(g, new Vector3(-3, 5.6f, 72f), 0, false);

            // D — twist: catapult page flings you onto the high shelf; a second one reaches the lamp shade star
            var shelf = new Vector3(0, 10f, 93f);
            Catapult(g, "D_Catapult", new Vector3(0, 5.6f, 76.6f), shelf + new Vector3(0, 0, -2.5f), 4.5f);
            Bookshelf(g, "D_Shelf", shelf, 10, 8, 10f);
            Catapult(g, "D_CatapultStar", new Vector3(3.6f, 10f, 95.5f), new Vector3(12f, 13.6f, 97f), 3f);
            var shade = Box(g, "D_LampShadeTop", new Vector3(12f, 13.3f, 97f), new Vector3(3.2f, 0.6f, 3.2f), Mt("M_Kit_LampShade"));
            Box(g, "D_LampPole", new Vector3(12f, 6.6f, 97f), new Vector3(0.4f, 13f, 0.4f), Mt("M_Kit_Brass"), collider: false);
            Star(g, new Vector3(12f, 14.8f, 97f), 1);
            Box(g, "D_ShelfBridge", new Vector3(0, 9.75f, 99f), new Vector3(4f, 0.5f, 4f), Mt("M_Kit_WoodLight"));

            // E — the chair glimpse, then it dives into the page-portal
            Page(g, "E_Spread", new Vector3(0, 10f, 105f), 16, 10, 0.8f, 5);
            PillowFire(g, new Vector3(-5, 10f, 102f), 0, false);
            var portal = Portal(g, new Vector3(0, 10f, 109.2f), 1, Palette.Hex(0xB89EFF));
            Cameo(g, new Vector3(2, 10f, 106.8f), portal, "THE CHAIR!? — มันวิ่งหนีเข้าไปในหน้าหนังสือ!");

            // dressing: floating giant books, clouds, star bits, the landmark tower ahead
            for (int i = 0; i < 14; i++)
            {
                var rnd = new System.Random(i * 31 + 7);
                Vector3 p = new Vector3((rnd.Next(0, 2) == 0 ? -1 : 1) * (18 + (float)rnd.NextDouble() * 30), -6 + (float)rnd.NextDouble() * 26, (float)rnd.NextDouble() * 120);
                var b = FloatingBook(g, p, 2.5f + (float)rnd.NextDouble() * 5f, i);
                b.localRotation = Quaternion.Euler((float)rnd.NextDouble() * 50 - 25, (float)rnd.NextDouble() * 360, (float)rnd.NextDouble() * 40 - 20);
            }
            Clouds(g, 0, 120, 10);
            StarBits(g, new Vector3(0, 8, 60), new Vector3(90, 40, 140), 30);
            Tower(g, new Vector3(30, -20, 230), 2.2f);

            Section(g, "1-A", "Observe: Story World", "ขั้นที่ 1 (สังเกต): สำรวจโลกนิทานและสังเกตสัญลักษณ์นำทาง", new Vector3(0, 6, 8), new Vector3(30, 30, 22));
            Section(g, "1-B", "Hypothesis: Pop-up Rule", "ขั้นที่ 2 (ตั้งสมมติฐาน): เดินเข้าใกล้หน้ากระดาษขอบฟ้าเพื่อกระตุ้นบันไดเด้ง", new Vector3(0, 6, 32), new Vector3(30, 30, 18));
            Section(g, "1-C", "Experiment: Time vs Rail", "ขั้นที่ 3 (ทดลองแก้ปัญหา): เลือกจังหวะกระโดด 3 วิ หรือเหยียบสวิตช์ตรรกะ/ขึ้นรางรถไฟลัด!", new Vector3(0, 8, 60), new Vector3(30, 30, 20));
            Section(g, "1-D", "Trajectory Vector", "ขั้นที่ 4 (คำนวณวิถี): ใช้แผ่นดีดสปริงส่งตัวขึ้นสู่ชั้นหนังสือด้านบน", new Vector3(0, 10, 86), new Vector3(40, 40, 22));
            Section(g, "1-E", "Target Pursuit", "ขั้นที่ 5 (สรุปผล): ไล่ตามเก้าอี้เป้าหมายเข้าสู่ประตูมิติหน้าถัดไป!", new Vector3(0, 12, 104), new Vector3(30, 20, 14));
            return ch;
        }

        // =====================================================================================================
        // CHAPTER 2 — Paper Theater (2.5D side view, golden spotlights, cardboard). Runs along +X at z = 400.
        static ChapterInfo Chapter2(Transform root)
        {
            var g = Group(root, "CH2_PaperTheater");
            var look = Group(g, "Look");
            const float Z = 400f;
            var ch = new ChapterInfo
            {
                title = "THE PAPER THEATER", tag = "2.5D STAGE",
                // a night-show anime stage: deep blue house, yellow follow-spots, teal painted sky flat
                skybox = null, background = Palette.Hex(0x0A1640), fogColor = Palette.Hex(0x0F2258), fogStart = 34, fogEnd = 110,
                ambientSky = Palette.Hex(0x8FB0FF), ambientEquator = Palette.Hex(0x5A6FC0), ambientGround = Palette.Hex(0x2A2F6A),
                halftoneColor = new Color(0.05f, 0.2f, 0.6f, 0.9f), shadowTint = new Color(0.3f, 0.36f, 0.85f, 0.85f),
                sideRail = true, railDistance = 15.5f, railHeight = 3.4f, railFov = 34f, railPitch = 9f, lookRoot = look.gameObject,
            };
            Sun(look, new Vector3(35, 25, 0), Palette.Hex(0xFFE8B0), 1.05f);
            Profile(look, "PP_W1_Ch2", exposure: 0.15f, contrast: 28, saturation: 32, bloom: 0.25f, vignette: 0.28f, dofFocus: 15.5f, chroma: 0.07f);
            ch.spawn = PillowFire(g, new Vector3(2, 0, Z), 90f, false);

            // stage floor segments (front lane z=400, depth 5)
            Stage(g, "S0", new Vector3(5, 0, Z), 14);
            // teach: sliding cardboard sets (vertical / horizontal / vertical)
            Slider(g, "S1_Lift", new Vector3(16f, 0.4f, Z), new Vector3(3, 0.5f, 4), new Vector3(0, 2.2f, 0), 3.2f, 0f);
            Slider(g, "S1_Glide", new Vector3(21f, 2.6f, Z), new Vector3(3, 0.5f, 4), new Vector3(5, 0, 0), 4f, 0.25f);
            Slider(g, "S1_Drop", new Vector3(31.5f, 3.4f, Z), new Vector3(3, 0.5f, 4), new Vector3(0, -1.6f, 0), 3f, 0.5f);
            Stage(g, "S2", new Vector3(41, 3, Z), 12);
            PillowFire(g, new Vector3(38, 3, Z), 90f, false);
            // develop: crumbling cardboard bridge
            for (int i = 0; i < 10; i++)
            {
                var plank = new GameObject($"S3_Plank{i}");
                plank.transform.SetParent(g, false);
                plank.transform.position = new Vector3(48.5f + i * 2.0f, 2.75f, Z);
                var vis = Box(plank.transform, "Vis", plank.transform.position, new Vector3(1.85f, 0.5f, 3.2f), i % 2 == 0 ? Mt("Cardboard") : Mt("CardboardDark"));
                vis.transform.localPosition = Vector3.zero;
                var cp = plank.AddComponent<CrumblePlatform>(); cp.visual = vis.transform;
            }
            Star(g, new Vector3(57.5f, 6.2f, Z), 2);
            Stage(g, "S4", new Vector3(73, 3, Z), 10);
            PillowFire(g, new Vector3(70, 3, Z), 90f, false);
            var ch2GuideBeam = Box(g, "S5_BackGuide", new Vector3(76.5f, 3.35f, Z + 1.8f), new Vector3(1.8f, 0.1f, 3.2f), Mt("Gold"), collider: false);
            CognitiveTotem(g, "Trial_Ch2_Lateral", new Vector3(74.2f, 3f, Z - 1.2f),
                "TRIAL 02 // LATERAL PERSPECTIVE SHIFT",
                "วิเคราะห์มิติที่ซ่อนอยู่: เมื่อกำแพงฉากหน้ากั้นทาง ให้ใช้ประตูมิติลึก (Z-Axis) เดินอ้อมไปหลังฉากละคร!",
                ch2GuideBeam);
            // twist: the front lane is blocked by a scenery wall with a door → the back layer continues (z = 405)
            Box(g, "S5_SceneryWall", new Vector3(79.5f, 6.5f, Z), new Vector3(1, 7, 5), Mt("Cardboard"));
            Box(g, "S5_DoorStep", new Vector3(76.5f, 3.2f, Z + 3.5f), new Vector3(3, 0.4f, 3), Mt("StageWood"));
            Arch(g, new Vector3(76.5f, 3f, Z + 2.4f));
            Stage(g, "S5_BackLane", new Vector3(88, 3, Z + 5.5f), 22, depth: 3.2f);
            Slider(g, "S5_BackLift", new Vector3(101.5f, 3.2f, Z + 5.5f), new Vector3(3, 0.5f, 3), new Vector3(0, 2.6f, 0), 3.4f, 0f);
            Stage(g, "S6", new Vector3(109, 5.8f, Z), 10);
            // reward for the curious: the front lane past the wall hides a star (reach it from the back lift)
            Box(g, "S5_FrontLedge", new Vector3(86, 3f, Z), new Vector3(10, 0.5f, 4), Mt("StageWood"));
            Star(g, new Vector3(88, 4.4f, Z), 2);
            Portal(g, new Vector3(112.5f, 5.8f, Z), 2, Palette.Hex(0xF2C14E));

            // theatre dressing: curtains, proscenium, backdrop flats, foreground silhouettes, spotlights + beams
            for (int s = -1; s <= 1; s += 2) Box(g, "Curtain", new Vector3(s < 0 ? -15 : 128, 8, Z + 1), new Vector3(10, 18, 1), Mt("Curtain"), collider: false);
            Box(g, "Proscenium", new Vector3(57, 17, Z - 2.5f), new Vector3(130, 3, 1), Mt("Curtain"), collider: false);
            Box(g, "Backdrop", new Vector3(57, 8, Z + 9), new Vector3(140, 22, 0.5f), Mt("Backdrop"), collider: false);
            for (int i = 0; i < 9; i++)
                Box(g, "BackHill" + i, new Vector3(i * 15f, 1.5f + (i % 3), Z + 8.4f), new Vector3(18, 7 + (i % 2) * 3, 0.4f), Mt("BackdropHill"), collider: false).transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? 8 : -6));
            var moon = Prim(g, PrimitiveType.Sphere, "PaperMoon", new Vector3(60, 13, Z + 8.6f), new Vector3(4, 4, 0.3f), Mt("Gold"), collider: false);
            for (int i = 0; i < 12; i++) Box(g, "FgGrass" + i, new Vector3(i * 10f, -0.5f, Z - 4.5f), new Vector3(5, 2 + (i % 3), 0.2f), Mt("Silhouette"), collider: false).transform.localRotation = Quaternion.Euler(0, 0, (i % 2) * 12 - 6);
            for (int i = 0; i < 5; i++)
            {
                Vector3 lp = new Vector3(8 + i * 24f, 14, Z - 4);
                Spot(look, lp, new Vector3(lp.x + 2, 2, Z), Palette.Hex(0xFFD58A), 45f, 60f);
                Beam(g, lp, new Vector3(lp.x + 2, 0, Z), 4.5f);
            }
            var silhouette = Cameo(g, new Vector3(10, 3.5f, Z + 7.6f), null, "", patrol: true);
            silhouette.patrolOffset = new Vector3(90, 0, 0); silhouette.patrolPeriod = 14f;
            foreach (var r in silhouette.GetComponentsInChildren<MeshRenderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = Mt("Silhouette"); r.sharedMaterials = mats; }
            Tower(g, new Vector3(150, -10, Z + 40), 1.6f);

            Section(g, "2-A", "Observe: Dynamic Stage", "สังเกตคาบการเลื่อนของฉากกระดาษและเชือกชักรอกก่อนกระโดด", new Vector3(18, 6, Z), new Vector3(36, 30, 20));
            Section(g, "2-B", "Execute: Momentum Bridge", "รักษาโมเมนตัมการวิ่งต่อเนื่องเพื่อข้ามสะพานกระดาษที่ยุบตัว", new Vector3(58, 6, Z), new Vector3(30, 30, 20));
            Section(g, "2-C", "Deduce: Behind the Curtain", "คิดนอกกรอบ (Lateral Thinking): ทางข้างหน้าตัน! สังเกตซุ้มประตูแล้วเดินอ้อมไปหลังฉาก", new Vector3(95, 6, Z), new Vector3(46, 30, 20));
            return ch;
        }

        // =====================================================================================================
        // CHAPTER 3 — Toy Galaxy (floating toy planetoids in a nebula). Bounce → chained launches → orbiting islands.
        static ChapterInfo Chapter3(Transform root)
        {
            var g = Group(root, "CH3_ToyGalaxy");
            var look = Group(g, "Look");
            const float Z = 800f;
            var ch = new ChapterInfo
            {
                title = "THE TOY GALAXY", tag = "SPACE",
                // high noon above the clouds: saturated cobalt, white cumulus sea below, a few daytime stars
                skybox = AnimeSky("M_Sky_W1_Ch3", Palette.Hex(0x0B4FD6), Palette.Hex(0x7FE1FF), Palette.Hex(0xFFF7B0), Palette.Hex(0xF2F8FF), 0.66f, Palette.Hex(0x9CB6EE), stars: 0.25f, sun: new Vector3(0.3f, 0.6f, 0.7f)),
                fog = true, fogColor = Palette.Hex(0xBFEFFF), fogStart = 90, fogEnd = 420,
                ambientSky = Palette.Hex(0xD6F2FF), ambientEquator = Palette.Hex(0xA9C6F5), ambientGround = Palette.Hex(0x6A7AD0),
                halftoneColor = new Color(0.0f, 0.5f, 0.85f, 0.85f), shadowTint = new Color(0.4f, 0.55f, 1f, 0.85f),
                lookRoot = look.gameObject,
            };
            Sun(look, new Vector3(52, -60, 0), Palette.Hex(0xFFF6DC), 1.4f);
            Fill(look, new Vector3(-10, 120, 0), Palette.Hex(0x9FD8FF), 0.12f);
            Profile(look, "PP_W1_Ch3", exposure: -0.4f, contrast: 26, saturation: 18, bloom: 0.2f, vignette: 0.1f, chroma: 0.05f);

            var spawnIsland = Island(g, "I0_Spawn", new Vector3(0, 0, Z), 6.5f, Mt("ToyBlue"), Mt("ToyYellow"));
            ch.spawn = PillowFire(g, new Vector3(-2, 0, Z - 2), 0, false);
            // teach: one bounce to a near island
            var b = Island(g, "I1", new Vector3(0, 3, Z + 15), 3.6f, Mt("ToyRed"), Mt("ToyWhite"));
            Pad(g, "P0", new Vector3(0, 0, Z + 4.6f), b.position + Vector3.up * 0.1f, 4f);
            // develop: chained launches across the void, guided by star bits along each arc
            var c = Island(g, "I2", new Vector3(9, 6, Z + 30), 3.2f, Mt("ToyGreen"), Mt("ToyWhite"));
            Pad(g, "P1", new Vector3(0, 3, Z + 16.5f), c.position + Vector3.up * 0.1f, 5f);
            var d = Island(g, "I3", new Vector3(-5, 9, Z + 46), 4.2f, Mt("ToyYellow"), Mt("ToyRed"));
            Pad(g, "P2", new Vector3(9, 6, Z + 31.5f), d.position + Vector3.up * 0.1f, 5f);
            PillowFire(g, new Vector3(-7, 9, Z + 45), 0, false);
            var orbitBridge = Box(g, "Orbit_LogicBridge", new Vector3(-4.5f, 9.1f, Z + 63.5f), new Vector3(2.8f, 0.4f, 27f), Mt("Pad"));
            CognitiveTotem(g, "Trial_Ch3_Orbit", new Vector3(-2.2f, 9f, Z + 47.2f),
                "TRIAL 03 // ORBITAL SYNCHRONIZATION",
                "จำลองวิถีโคจรสำเร็จ! เปิดสะพานพลังงานเชื่อมศูนย์กลางวงโคจร หรือใช้ราง SkyRail ด้านซ้ายเพื่อพุ่งอ้อม!",
                orbitBridge);
            Star(g, new Vector3(16, 10, Z + 22), 3);                       // between arcs: a brave mid-air grab
            // twist: three islands orbit between I3 and the final island — time your jumps
            Vector3 oc = new Vector3(-4, 9, Z + 63);
            for (int i = 0; i < 3; i++)
            {
                var o = Island(g, $"Orbit{i}", oc + new Vector3(Mathf.Sin(i * 120 * Mathf.Deg2Rad), 0, Mathf.Cos(i * 120 * Mathf.Deg2Rad)) * 9f, 2.6f, i == 1 ? Mt("ToyRed") : Mt("ToyBlue"), Mt("ToyWhite"));
                var mp = o.gameObject.AddComponent<MovingPlatform>();
                mp.orbitCenter = oc; mp.orbitRadius = 9f; mp.orbitDegPerSec = 22f; mp.orbitPhaseDeg = i * 120f; mp.carryRadius = 2.6f;
            }
            Star(g, oc + Vector3.up * 2.5f, 3);
            // sky rail around the orbit ring: skip the timing puzzle at speed, if you can stick the landing
            Rail(g, "SkyRail", new Vector3(-6.5f, 10.1f, Z + 48.5f), new Vector3(-17f, 12.5f, Z + 57f), new Vector3(-18.5f, 13.5f, Z + 66f), new Vector3(-14f, 12.2f, Z + 74f), new Vector3(-7.5f, 10.2f, Z + 78.5f));
            var f = Island(g, "I4_Final", new Vector3(-4, 9, Z + 81), 5.5f, Mt("ToyGreen"), Mt("ToyYellow"));
            PillowFire(g, new Vector3(-7, 9, Z + 79), 0, false);
            Star(g, new Vector3(-4, 15.5f, Z + 92), 3);
            Pad(g, "P3_Star", new Vector3(-1, 9, Z + 84), new Vector3(-4, 14.3f, Z + 92), 3f);
            var top = Island(g, "I5_StarPerch", new Vector3(-4, 14.2f, Z + 92), 1.8f, Mt("ToyRed"), Mt("ToyWhite"));
            Portal(g, new Vector3(-4, 9, Z + 85.6f), 3, Palette.Hex(0xFF5FAE));
            Cameo(g, new Vector3(-6, 9, Z + 84f), null, "เก้าอี้โบกมือลาจากดาวดวงสุดท้าย… ไปต่อ!");

            // galaxy dressing: the planets from the level select, rings, star bits, dust
            ArtKit.Prop(g, "PLANET_Kitchen", new Vector3(60, 10, Z + 70), 0, Vector3.one * 2.5f, true);
            ArtKit.Prop(g, "PLANET_Bath", new Vector3(-70, -20, Z + 40), 0, Vector3.one * 3f, true);
            ArtKit.Prop(g, "PLANET_Toy", new Vector3(-40, 30, Z + 130), 0, Vector3.one * 2.2f, true);
            var ring = ArtKit.Prop(g, "PROP_GalRing", oc + Vector3.down * 0.5f, 0, Vector3.one * 9f);
            StarBits(g, new Vector3(0, 8, Z + 50), new Vector3(140, 70, 160), 50);
            Tower(g, new Vector3(-10, -30, Z + 220), 2.4f);

            Section(g, "3-A", "Observe: Launch Vectors", "สังเกตเส้นดาวนำทางเพื่ออ่านวิถีโค้งของเบาะดีดตัว", new Vector3(0, 6, Z + 8), new Vector3(40, 40, 30));
            Section(g, "3-B", "Chain: Multi-Vector Arcs", "เชื่อมต่อจุดส่งตัวต่อเนื่องข้ามห้วงอวกาศของเล่น", new Vector3(0, 8, Z + 35), new Vector3(60, 50, 30));
            Section(g, "3-C", "Solve: Orbital Timing", "คำนวณจังหวะเกาะหมุนรอบวงโคจร หรือแก้สวิตช์ตรรกะเพื่อสร้างสะพานลัด!", new Vector3(-4, 10, Z + 70), new Vector3(60, 50, 40));
            return ch;
        }

        // =====================================================================================================
        // CHAPTER 4 — Phantom Shelf-tops CHASE (red / black / white comic panels, halftone).
        static ChapterInfo Chapter4Impl(Transform root)
        {
            var g = Group(root, "CH4_ShelfChase");
            var look = Group(g, "Look");
            const float Z = 1200f, Y = 6f;
            var ch = new ChapterInfo
            {
                title = "THE PHANTOM SHELF-TOPS", tag = "THE CHASE",
                // the chase at golden hour: indigo zenith, blazing orange horizon, magenta band, navy skyline (anime sunset)
                skybox = AnimeSky("M_Sky_W1_Ch4", Palette.Hex(0x23207A), Palette.Hex(0xFF9A3C), Palette.Hex(0xFF4F7A), Palette.Hex(0x2A1A50), 0.42f, Palette.Hex(0xB0508A), stars: 0.15f, sun: new Vector3(0f, 0.08f, 1f)),
                fogColor = Palette.Hex(0xF08A5A), fogStart = 50, fogEnd = 260,
                ambientSky = Palette.Hex(0xFFD0A8), ambientEquator = Palette.Hex(0xD07090), ambientGround = Palette.Hex(0x3A2A70),
                halftoneColor = new Color(0.55f, 0.1f, 0.45f, 0.85f), shadowTint = new Color(0.45f, 0.35f, 0.9f, 0.85f), speedColor = new Color(1f, 0.95f, 0.6f, 0.85f),
                lookRoot = look.gameObject,
            };
            Sun(look, new Vector3(14, 0, 0), Palette.Hex(0xFFC27A), 1.5f);   // low sun straight down the run: long anime shadows
            Fill(look, new Vector3(-20, -40, 0), Palette.Hex(0x7A6CFF), 0.15f);
            Profile(look, "PP_W1_Ch4", exposure: 0.05f, contrast: 30, saturation: 30, bloom: 0.25f, vignette: 0.25f, chroma: 0.1f);

            var pts = new[] { new Vector3(0, Y, Z + 2), new Vector3(0, Y, Z + 40), new Vector3(12, Y, Z + 55), new Vector3(12, Y, Z + 90),
                              new Vector3(-6, Y, Z + 110), new Vector3(-6, Y, Z + 145), new Vector3(4, Y, Z + 165), new Vector3(4, Y, Z + 200) };
            for (int i = 1; i < pts.Length; i++) ShelfRun(g, $"Run{i}", pts[i - 1], pts[i], i);
            // the right shelf edge is a live rail on every run: grind it to close the gap on the chair (and skip the holes)
            for (int i = 1; i < pts.Length; i++)
            {
                Vector3 a = pts[i - 1], b = pts[i], dir = (b - a).normalized, side = Vector3.Cross(Vector3.up, dir) * 2.55f;
                Rail(g, $"EdgeRail{i}", a + side + dir * 1.5f + Vector3.up * 0.55f, b + side - dir * 1.5f + Vector3.up * 0.55f);
            }
            ch.spawn = PillowFire(g, new Vector3(0, Y, Z - 1.5f), 0, false);
            Box(g, "StartLanding", new Vector3(0, Y - 0.25f, Z - 1), new Vector3(6, 0.5f, 6), Mt("Ink"));
            CognitiveTotem(g, "Trial_Ch4_RouteOpt", new Vector3(2.1f, Y, Z + 0.5f),
                "TRIAL 04 // ROUTE OPTIMIZATION",
                "วิเคราะห์เส้นทางความเร็วสูง: ขึ้นรางไฟสีเหลืองที่ขอบขวาของชั้นหนังสือเพื่อข้ามหลุมและย่นเวลาไล่ล่า!",
                null);
            var runnerGo = new GameObject("CHASE_Chair");
            runnerGo.transform.SetParent(g, false);
            runnerGo.transform.position = pts[0] + new Vector3(0, 0, 8);
            var vis = Group(runnerGo.transform, "Visual");
            ChairModel(vis);
            var runner = runnerGo.AddComponent<ChaseRunner>();
            runner.visual = vis;
            var wps = Group(g, "ChaseWaypoints");
            foreach (var p in pts) { var w = Group(wps, "WP"); w.position = p; runner.waypoints.Add(w); }
            runner.waypoints[0].position = pts[0] + new Vector3(0, 0, 8);
            var restart = Group(g, "ChaseRestart"); restart.position = pts[0]; runner.playerRestart = restart;
            Star(g, new Vector3(16, Y + 2.2f, Z + 72), 4);
            Star(g, new Vector3(-10, Y + 2.2f, Z + 128), 4);
            Star(g, new Vector3(8, Y + 3.5f, Z + 182), 4);
            // stained glass at the end; the portal opens when the chair crashes through
            var glass = Group(g, "StainedGlass");
            for (int i = 0; i < 7; i++)
                Box(glass, "Pane" + i, new Vector3(4 + (i - 3) * 1.4f, Y + 3 + Mathf.Abs(i - 3) * -0.6f + 3f, Z + 204), new Vector3(1.3f, 6 - Mathf.Abs(i - 3), 0.2f), i % 2 == 0 ? Mt("Glass") : Mt("InkRed"), collider: false);
            runner.endPortal = Portal(g, new Vector3(4, Y, Z + 202.5f), 4, Palette.Hex(0xE5293D)).GetComponent<PagePortal>();

            // Persona dressing: giant red slashes and halftone panels in the sky, white crescent, black skyline of books
            for (int i = 0; i < 8; i++)
            {
                var s = Box(g, "Slash" + i, new Vector3((i % 2 == 0 ? -1 : 1) * (30 + i * 4), 20 + i * 3, Z + 20 + i * 25), new Vector3(60, 3 + (i % 3), 0.5f), i % 3 == 0 ? Mt("InkWhite") : Mt("InkRed"), collider: false);
                s.transform.localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 25 : -25), i % 2 == 0 ? 28 : -32);
            }
            Prim(g, PrimitiveType.Sphere, "Crescent", new Vector3(-40, 45, Z + 230), new Vector3(16, 16, 2), Mt("InkWhite"), collider: false);
            for (int i = 0; i < 18; i++)
            {
                var rnd = new System.Random(i * 13);
                float h = 10 + (float)rnd.NextDouble() * 30;
                Box(g, "Skyline" + i, new Vector3((i % 2 == 0 ? -1 : 1) * (25 + (float)rnd.NextDouble() * 30), h / 2 - 15, Z + i * 12), new Vector3(6, h, 4), Mt("Ink"), collider: false);
            }
            Tower(g, new Vector3(4, -15, Z + 330), 2.2f);

            Section(g, "4-A", "The Chase", "ไล่ตามเก้าอี้! วิ่ง (SHIFT) — กระโดดหลบหนังสือที่กลิ้งมา", new Vector3(3, Y + 4, Z + 100), new Vector3(60, 30, 220));
            return ch;
        }

        static ChapterInfo Chapter4(Transform root) => Chapter4Impl(root);

        // =====================================================================================================
        // CHAPTER 5 — Cathedral of the Throne (gothic fog, candles) — BOSS ARENA Ø44 m.
        static ChapterInfo Chapter5(Transform root, out BossEncounter encounter)
        {
            var g = Group(root, "CH5_Cathedral");
            var look = Group(g, "Look");
            const float Z = 1600f;
            Vector3 C = new Vector3(0, 0, Z + 70f);   // arena centre
            const float R = 22f;
            var ch = new ChapterInfo
            {
                title = "CATHEDRAL OF THE THRONE", tag = "BOSS",
                // blue hour: navy zenith, teal afterglow, the throne lit in gold
                skybox = AnimeSky("M_Sky_W1_Ch5", Palette.Hex(0x0A1450), Palette.Hex(0x3FD0E0), Palette.Hex(0xFFD45C), Palette.Hex(0x0A1030), 0.3f, Palette.Hex(0x40508A), stars: 1.0f, sun: new Vector3(0.2f, 0.05f, 1f)),
                fogColor = Palette.Hex(0x1E3A7A), fogStart = 30, fogEnd = 150,
                ambientSky = Palette.Hex(0x9AC8FF), ambientEquator = Palette.Hex(0x4A60B0), ambientGround = Palette.Hex(0x1A1E4A),
                halftoneColor = new Color(0.0f, 0.15f, 0.45f, 0.9f), shadowTint = new Color(0.25f, 0.32f, 0.8f, 0.85f),
                lookRoot = look.gameObject,
            };
            Sun(look, new Vector3(50, 20, 0), Palette.Hex(0xBFE6FF), 0.95f);
            Profile(look, "PP_W1_Ch5", exposure: -0.15f, contrast: 28, saturation: 26, bloom: 0.3f, vignette: 0.3f, chroma: 0.07f);

            // fog gate + hall of candles
            ch.spawn = PillowFire(g, new Vector3(0, 0, Z + 2), 0, false);
            Box(g, "Hall_Floor", new Vector3(0, -0.25f, Z + 22), new Vector3(12, 0.5f, 46), Mt("Stone"));
            for (int k = 0; k < 3; k++)
            {
                var fogq = Box(g, "FogGate", new Vector3(-1.5f + k * 1.5f, 3, Z + 8 + k * 0.3f), new Vector3(3.2f, 6.5f, 0.02f), Mt("Beam"), collider: false);
                fogq.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            for (int i = 0; i < 5; i++)
            {
                float z = Z + 10 + i * 7f;
                for (int s = -1; s <= 1; s += 2)
                {
                    Prim(g, PrimitiveType.Cylinder, "Column", new Vector3(s * 5.6f, 7, z), new Vector3(1.3f, 7, 1.3f), Mt("StoneDark"));
                    Candle(g, look, new Vector3(s * 4.2f, 0, z + 3.5f));
                    var banner = Box(g, "Banner", new Vector3(s * 5.6f, 9, z + 0.75f), new Vector3(1.6f, 5, 0.05f), Mt("Banner"), collider: false);
                }
            }
            Box(g, "Hall_WallL", new Vector3(-6.5f, 7, Z + 22), new Vector3(1, 14, 46), Mt("StoneDark"));
            Box(g, "Hall_WallR", new Vector3(6.5f, 7, Z + 22), new Vector3(1, 14, 46), Mt("StoneDark"));
            Star(g, new Vector3(-4.5f, 3.4f, Z + 30), 5);   // on a candle ledge
            Box(g, "CandleLedge", new Vector3(-4.6f, 2.2f, Z + 30), new Vector3(2, 0.4f, 2), Mt("Stone"));
            Box(g, "LedgeStep", new Vector3(-4.6f, 1.0f, Z + 27.5f), new Vector3(2, 0.4f, 2), Mt("Stone"));

            // the arena (NavMesh baked at runtime from the colliders in this group)
            var arena = Group(g, "Arena");
            var floor = Prim(arena, PrimitiveType.Cylinder, "ArenaFloor", C + Vector3.down * 0.5f, new Vector3(R * 2, 0.5f, R * 2), Mt("Stone"));
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.AddComponent<MeshCollider>();
            Box(arena, "ArenaEntry", new Vector3(0, -0.25f, Z + 47.5f), new Vector3(8, 0.5f, 3), Mt("Stone"));
            int segs = 28;
            for (int i = 0; i < segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                if (Vector3.Angle(dir, Vector3.back) < 9f) continue;                  // entrance from the hall (south)
                var w = Box(arena, "Wall" + i, C + dir * (R + 0.6f) + Vector3.up * 3f, new Vector3(5.2f, 6f, 1.2f), Mt("StoneDark"));
                w.transform.rotation = Quaternion.LookRotation(dir);
                if (i % 2 == 0) Prim(g, PrimitiveType.Cylinder, "Buttress", C + dir * (R + 1.6f) + Vector3.up * 8f, new Vector3(1.6f, 8f, 1.6f), Mt("Stone"), collider: false);
            }
            // Throne Tower: a rotating stack of giant books on a stone core (obstacle in P1/P2). In P3 (ThroneClimb)
            // the chair leaps to the top and 8 books slide out of the stack as a rotating spiral staircase.
            var core = Prim(arena, PrimitiveType.Cylinder, "TowerCore", C + Vector3.up * 4.5f, new Vector3(6.0f, 4.5f, 6.0f), Mt("StoneDark"));
            core.GetComponent<MeshRenderer>().enabled = false;   // collider only: the books are the look
            var climbGo = new GameObject("ThroneClimb"); climbGo.transform.SetParent(g, false); climbGo.transform.position = C;
            var tower = Group(climbGo.transform, "ThroneTower_Books"); tower.position = C;
            string[] cols = { "ToyRed", "Banner", "ToyBlue", "ToyYellow", "ToyRed", "Pad", "ToyGreen", "ToyRed" };
            // 8 fat round books, hand-stacked (a little off-centre and tilted) — covers + cream page block + gold band
            for (int i = 0; i < 8; i++)
            {
                float y = 0.56f + i * 1.125f;
                var layer = Group(tower, "Book" + i);
                layer.position = C + new Vector3(((i * 37) % 5 - 2) * 0.05f, y, ((i * 53) % 5 - 2) * 0.05f);
                layer.rotation = Quaternion.Euler(((i % 3) - 1) * 1.2f, i * 41f, ((i % 2) * 2 - 1) * 1.0f);
                Prim(layer, PrimitiveType.Cylinder, "CoverBottom", layer.position + layer.up * -0.47f, new Vector3(6.32f, 0.06f, 6.32f), Mt(cols[i]), collider: false);
                Prim(layer, PrimitiveType.Cylinder, "CoverTop", layer.position + layer.up * 0.47f, new Vector3(6.32f, 0.06f, 6.32f), Mt(cols[i]), collider: false);
                Prim(layer, PrimitiveType.Cylinder, "Pages", layer.position, new Vector3(6.12f, 0.42f, 6.12f), Mt("PageBack"), collider: false);
                var spine = Box(layer, "Spine", layer.position + layer.forward * 3.0f, new Vector3(2.4f, 1.0f, 0.5f), Mt(cols[i]), collider: false);
                var band = Box(layer, "SpineBand", layer.position + layer.forward * 3.27f, new Vector3(2.0f, 0.12f, 0.05f), Mt("Gold"), collider: false);
            }
            var climb = climbGo.AddComponent<ThroneClimb>();
            climb.stepsRoot = tower; climb.topHeight = 9f; climb.topRadius = 3.2f;
            for (int i = 0; i < 8; i++)
            {
                float a = (200f + i * 45f) * Mathf.Deg2Rad;      // first step faces the hall entrance side
                Vector3 dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                float top = 1.1f * (i + 1);
                var step = new GameObject("ClimbStep" + i).transform;
                step.SetParent(tower, false);
                step.SetPositionAndRotation(C + dir * climb.stepInRadius + Vector3.up * (top - 0.225f), Quaternion.LookRotation(dir));
                Box(step, "Cover", step.position, new Vector3(2.6f, 0.45f, 2.4f), Mt(cols[(i + 3) % cols.Length]));
                Box(step, "Pages", step.position + dir * 0.07f + Vector3.up * 0.0f, new Vector3(2.44f, 0.34f, 2.36f), Mt("PageBack"), collider: false);
                var mp = step.gameObject.AddComponent<MovingPlatform>(); mp.AutoMove = false; mp.carryRadius = 1.55f;
                climb.steps.Add(step);
            }
            var throne = Group(g, "ThroneTop"); throne.position = C + Vector3.up * 9.6f;
            ChairModel(throne, scale: 1.6f, gold: true);
            climb.throneDecor = throne.gameObject;
            var tl = new GameObject("ThroneLight").AddComponent<Light>(); tl.transform.SetParent(look, false); tl.transform.position = C + Vector3.up * 12f;
            tl.type = LightType.Point; tl.range = 26f; tl.intensity = 6f; tl.color = Palette.Hex(0xFFD58A); tl.shadows = LightShadows.None;
            // cover + shortcut ledges (drop on the chair from above = Shortcut opening)
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                Vector3 p = C + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 13.5f;
                Prim(arena, PrimitiveType.Cylinder, "Pillar" + i, p + Vector3.up * 3f, new Vector3(1.8f, 3f, 1.8f), Mt("Stone"));
                Candle(g, look, p + (C - p).normalized * 2.2f);
            }
            Box(arena, "Ledge_W", C + new Vector3(-16, 0.9f, 4), new Vector3(5, 1.8f, 7), Mt("Stone"));
            Box(arena, "Ledge_W_Step", C + new Vector3(-12.6f, 0.45f, 4), new Vector3(2, 0.9f, 3), Mt("Stone"));
            Box(arena, "Ledge_E", C + new Vector3(16, 1.2f, -5), new Vector3(5, 2.4f, 7), Mt("Stone"));
            Box(arena, "Ledge_E_Step", C + new Vector3(12.6f, 0.6f, -5), new Vector3(2, 1.2f, 3), Mt("Stone"));
            var surface = arena.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            // god rays through the clerestory
            for (int i = 0; i < 5; i++) Beam(g, C + new Vector3(-14 + i * 7, 24, 14), C + new Vector3(-10 + i * 6, 0, -4 + (i % 2) * 6), 5f);

            // the boss
            var dyn = Group(g, "Boss_Dynamic");
            var chairGo = new GameObject("BOSS_Chair");
            chairGo.transform.SetParent(dyn, false);
            chairGo.transform.position = C + new Vector3(0, 0, 9f);
            chairGo.transform.rotation = Quaternion.Euler(0, 180f, 0);
            DreamChairBuilder.BuildGrayboxChair(chairGo.transform);
            var col = chairGo.AddComponent<BoxCollider>(); col.center = new Vector3(0, 0.55f, 0); col.size = new Vector3(1.1f, 0.9f, 1.05f);
            var rb = chairGo.AddComponent<Rigidbody>(); rb.isKinematic = true;
            var ctrl = chairGo.AddComponent<BossChairController>();
            ctrl.config = GrayboxAssets.ChairConfig();
            ctrl.arenaBounds = new Bounds(C + Vector3.up * 0.5f, new Vector3(R * 2 - 1, 6, R * 2 - 1));
            chairGo.AddComponent<DreamLogic.ChairEffectReceiver>();
            var anim = chairGo.AddComponent<ChairAnimator>();
            anim.chair = ctrl; anim.fx = GrayboxAssets.FxLibrary(); anim.visual = chairGo.transform.Find("Visual");
            new GameObject("DreamLawDirector").AddComponent<Cards.DreamLawDirector>();
            encounter = new GameObject("BossEncounter").AddComponent<BossEncounter>();
            encounter.chair = ctrl; encounter.arenaSurface = surface;
            encounter.revealTriggerZ = C.z - R + 2.5f;
            var seat = chairGo.AddComponent<ChairSeatInteraction>(); seat.chair = ctrl; seat.encounter = encounter;
            var skills = chairGo.AddComponent<ChairSkills>(); skills.chair = ctrl; skills.encounter = encounter;
            climb.chair = ctrl; climb.skills = skills; climb.encounter = encounter;

            Section(g, "5-A", "Synthesize: Hall of Analysis", "ขั้นสรุป: ทบทวนคุณสมบัติการ์ดแก้ปัญหา (ปุ่ม Q) ก่อนเข้าสู่ห้องทดสอบสุดท้าย", new Vector3(0, 6, Z + 24), new Vector3(16, 20, 46));
            Section(g, "5-B", "Final Exam: Throne of Refusal", "ประยุกต์ใช้การ์ดแก้ปัญหา + ปรับตัวตามกฎความฝัน (Dream Law) เพื่อบังคับนั่งเก้าอี้!", new Vector3(0, 6, C.z), new Vector3(50, 30, 50));
            CognitiveTotem(g, "Trial_Ch5_Synthesis", new Vector3(3.4f, 0f, Z + 38f),
                "TRIAL 05 // MULTI-RULE SYNTHESIS",
                "พร้อมทำข้อสอบสุดท้าย! ใช้การ์ดแก้ปัญหา [Q] สร้างช่องโหว่ให้เก้าอี้ แล้วกด [E] นั่งค้าง 2 วินาทีเพื่อพิชิตฝัน!",
                null);
            return ch;
        }

        // =====================================================================================================
        // building blocks
        static Transform Group(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

        static GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 worldPos, Vector3 scale, Material m, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static GameObject Box(Transform parent, string name, Vector3 worldPos, Vector3 size, Material m, bool collider = true) => Prim(parent, PrimitiveType.Cube, name, worldPos, size, m, collider);

        static void ColliderBox(Transform parent, string name, Vector3 center, Vector3 size, float yaw)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0, yaw, 0));
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>Thick paper page (walkable): printed top at <paramref name="topCenter"/>.y, with a box collider.</summary>
        static Transform Page(Transform parent, string name, Vector3 topCenter, float w, float d, float thick, int seed)
        {
            var mesh = ArtKit.SaveMesh(PaperGeometry.Slab(w, d, thick, 0.6f, 4f, new Vector2(topCenter.x, topCenter.z), 0.05f, seed), "W1_Page_" + name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = topCenter;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { Mt("PageFloor"), Mt("PageEdge"), Mt("PageBack") };
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -thick / 2, 0); bc.size = new Vector3(w, thick, d);
            DreamLook.AddOutline(go, 1.6f);
            return go.transform;
        }

        static PopUpPage PopPage(Transform parent, string name, Vector3 top, Vector2 size, PopUpPage.Mode mode, float delay)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = top;
            var vis = Page(go.transform, "Vis", top, size.x, size.y, 0.5f, name.GetHashCode() & 255);
            vis.localPosition = Vector3.zero;
            Object.DestroyImmediate(vis.GetComponent<BoxCollider>());
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, -0.25f, 0); bc.size = new Vector3(size.x, 0.5f, size.y);
            // a printed corner tab tells "this page moves"
            var tab = Box(vis, "Tab", top + new Vector3(size.x / 2 - 0.35f, 0.02f, size.y / 2 - 0.35f), new Vector3(0.5f, 0.04f, 0.5f), mode == PopUpPage.Mode.Timed ? Mt("InkRed") : Mt("Pad"), collider: false);
            var p = go.AddComponent<PopUpPage>();
            p.mode = mode; p.delay = delay; p.visual = vis; p.triggerRadius = 7f;
            return p;
        }

        static void Catapult(Transform parent, string name, Vector3 at, Vector3 target, float apex)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at;
            var vis = Group(go.transform, "Visual"); vis.position = at;
            var page = Box(vis, "Spring", at + Vector3.up * 0.15f, new Vector3(2.6f, 0.3f, 2.6f), Mt("Pad"), collider: false);
            Box(vis, "Arrow", at + new Vector3(0, 0.32f, 0.5f), new Vector3(0.5f, 0.04f, 1.2f), Mt("InkRed"), collider: false);
            var tgt = Group(go.transform, "Target"); tgt.position = target;
            var lp = go.AddComponent<LaunchPad>();
            lp.target = tgt; lp.apexHeight = apex; lp.windup = 0.45f; lp.visual = vis; lp.radius = 1.4f;
            GuideArc(parent, at + Vector3.up * 0.6f, target + Vector3.up * 0.6f, apex);
        }

        static void Pad(Transform parent, string name, Vector3 at, Vector3 target, float apex)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = at;
            var vis = Group(go.transform, "Visual"); vis.position = at;
            var src = ArtKit.Prop(vis, "PROP_Cushion", Vector3.zero, 0, new Vector3(2.4f, 0.55f, 2.4f), true);
            if (src != null)
            {
                foreach (var r in src.GetComponentsInChildren<MeshRenderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Cushion") && !mats[i].name.Contains("Button")) mats[i] = Mt("Pad"); r.sharedMaterials = mats; }
                src.position = at;
            }
            var tgt = Group(go.transform, "Target"); tgt.position = target;
            var lp = go.AddComponent<LaunchPad>(); lp.target = tgt; lp.apexHeight = apex; lp.visual = vis; lp.radius = 1.3f;
            GuideArc(parent, at + Vector3.up * 0.6f, target + Vector3.up * 0.6f, apex);
        }

        /// <summary>Sparkle star-bits along the launch arc: the route is readable before you commit (Galaxy spirit).</summary>
        static void GuideArc(Transform parent, Vector3 from, Vector3 to, float apex)
        {
            var g = Group(parent, "GuideArc");
            const float rise = 24f, fall = 24f * 1.35f;
            Vector3 v = LaunchPad.ArcVelocity(from, to, apex, rise, fall);
            float tUp = v.y / rise, top = from.y + v.y * v.y / (2 * rise), tDown = Mathf.Sqrt(2 * Mathf.Max(0.01f, top - to.y) / fall), T = tUp + tDown;
            Vector3 flat = new Vector3(v.x, 0, v.z);
            for (int i = 1; i < 10; i++)
            {
                float t = T * i / 10f;
                float y = t < tUp ? from.y + v.y * t - 0.5f * rise * t * t : top - 0.5f * fall * (t - tUp) * (t - tUp);
                Vector3 p = from + flat * t; p.y = y;
                var s = ArtKit.Prop(g, "PROP_GalStar", Vector3.zero, 0, Vector3.one * 0.35f);
                if (s != null) { s.position = p; s.gameObject.AddComponent<Spinner>().speed = 90f; s.GetComponent<Spinner>().bob = 0.05f; }
            }
        }

        static Transform PillowFire(Transform parent, Vector3 at, float yaw, bool isStart)
        {
            var go = new GameObject("PillowFire_" + _cpOrder);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var cp = go.AddComponent<Checkpoint>(); cp.order = _cpOrder++; cp.isStart = isStart; cp.radius = 3f;
            var pillow = ArtKit.Prop(go.transform, "PROP_Cushion", new Vector3(1.6f, 0, 0), 15f, new Vector3(1.2f, 0.4f, 0.9f), true);
            var flame = Prim(go.transform, PrimitiveType.Sphere, "Flame", at + go.transform.right * 1.6f + Vector3.up * 0.62f, new Vector3(0.16f, 0.3f, 0.16f), Mt("Flame"), collider: false);
            flame.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            var l = new GameObject("FireLight").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.transform.position = flame.transform.position + Vector3.up * 0.3f;
            l.type = LightType.Point; l.range = 6f; l.intensity = 2.5f; l.color = Palette.Hex(0xFFB066); l.shadows = LightShadows.None;
            var pf = go.AddComponent<PillowFire>(); pf.checkpoint = cp; pf.flame = l; pf.flameVisual = flame.transform;
            return go.transform;
        }

        static void Star(Transform parent, Vector3 at, int chapter)
        {
            var s = ArtKit.Prop(parent, "PROP_GalStar", Vector3.zero, 0, Vector3.one * 1.1f, true);
            if (s == null) return;
            s.position = at; s.name = "DreamStar_Ch" + chapter;
            s.gameObject.AddComponent<DreamStar>().chapter = chapter;
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(s, false);
            l.type = LightType.Point; l.range = 3.5f; l.intensity = 1.6f; l.color = Palette.Hex(0xFFE08A); l.shadows = LightShadows.None;
        }

        static Transform Portal(Transform parent, Vector3 at, int toChapter, Color tint)
        {
            var go = new GameObject("PagePortal_to" + toChapter);
            go.transform.SetParent(parent, false); go.transform.position = at;
            var frame = Group(go.transform, "Frame"); frame.position = at;
            var page = Box(frame, "Page", at + Vector3.up * 2.4f, new Vector3(3.6f, 4.8f, 0.12f), Mt("PageBack"), collider: false);
            var glow = Box(frame, "Glow", at + Vector3.up * 2.4f + Vector3.back * 0.08f, new Vector3(3.0f, 4.2f, 0.02f), Mt("Glow"), collider: false);
            var l = new GameObject("PortalLight").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.transform.position = at + new Vector3(0, 2.4f, -1.2f); l.type = LightType.Point; l.range = 8; l.intensity = 3f; l.color = tint; l.shadows = LightShadows.None;
            var p = go.AddComponent<PagePortal>(); p.toChapter = toChapter; p.radius = 2f;
            return go.transform;
        }

        static ChairCameo Cameo(Transform parent, Vector3 at, Transform runTo, string line, bool patrol = false)
        {
            var go = new GameObject("ChairCameo");
            go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, 180, 0));
            var vis = Group(go.transform, "Visual");
            ChairModel(vis);
            var c = go.AddComponent<ChairCameo>(); c.runTo = runTo; c.line = line; c.patrol = patrol; c.visual = vis;
            return c;
        }

        static void ChairModel(Transform parent, float scale = 1f, bool gold = false)
        {
            if (ChairCharacterSetup.ModelExists && ChairCharacterSetup.Setup() != null)
            {
                var t = ChairCharacterSetup.Instantiate(parent, "Chair_Model", withFace: true);
                if (t != null)
                {
                    t.localScale = Vector3.one * scale;
                    if (gold) foreach (var r in t.GetComponentsInChildren<MeshRenderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Wood")) mats[i] = Mt("Gold"); r.sharedMaterials = mats; }
                    foreach (var c in t.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                    return;
                }
            }
            Box(parent, "Seat", parent.position + Vector3.up * 0.62f * scale, new Vector3(1.1f, 0.16f, 1.05f) * scale, Mt("M_Kit_WoodDark"), collider: false);
            Box(parent, "Back", parent.position + new Vector3(0, 1.2f, -0.45f) * scale, new Vector3(1.1f, 1.05f, 0.14f) * scale, Mt("M_Kit_WoodDark"), collider: false);
        }

        static void Sign(Transform parent, Vector3 at, string text)
        {
            var post = Box(parent, "SignPost", at + Vector3.up * 0.7f, new Vector3(0.12f, 1.4f, 0.12f), Mt("M_Kit_WoodDark"), collider: false);
            var board = Box(parent, "SignBoard", at + Vector3.up * 1.35f, new Vector3(1.6f, 0.5f, 0.08f), Mt("InkRed"), collider: false);
            board.transform.localRotation = Quaternion.Euler(0, 0, -4);
        }

        static CognitiveTrialNode CognitiveTotem(Transform parent, string name, Vector3 at, string code, string solvedMsg, GameObject helperBridge)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;

            // Pressure pad base + ring
            Prim(go.transform, PrimitiveType.Cylinder, "PadBase", at + Vector3.up * 0.06f, new Vector3(2.2f, 0.06f, 2.2f), Mt("Ink"), collider: false);
            Prim(go.transform, PrimitiveType.Cylinder, "PadRing", at + Vector3.up * 0.11f, new Vector3(1.75f, 0.05f, 1.75f), Mt("Gold"), collider: false);
            Prim(go.transform, PrimitiveType.Cylinder, "Pedestal", at + Vector3.up * 0.65f, new Vector3(0.55f, 0.55f, 0.55f), Mt("StoneDark"), collider: false);

            // Floating Holographic Logic Crystal + Orbiting Ring
            var crystalRoot = Group(go.transform, "CrystalVisual");
            crystalRoot.position = at + Vector3.up * 2.1f;
            var diamond = Box(crystalRoot, "CoreDiamond", crystalRoot.position, new Vector3(0.68f, 0.68f, 0.68f), Mt("ToyRed"), collider: false);
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            var ring1 = Box(crystalRoot, "LogicFrameA", crystalRoot.position, new Vector3(1.05f, 0.10f, 1.05f), Mt("Gold"), collider: false);
            ring1.transform.localRotation = Quaternion.Euler(22f, 0f, 22f);
            var ring2 = Box(crystalRoot, "LogicFrameB", crystalRoot.position, new Vector3(1.05f, 0.10f, 1.05f), Mt("Pad"), collider: false);
            ring2.transform.localRotation = Quaternion.Euler(-22f, 45f, -22f);

            var l = new GameObject("TotemBeacon").AddComponent<Light>();
            l.transform.SetParent(go.transform, false);
            l.transform.position = at + Vector3.up * 2.4f;
            l.type = LightType.Point;
            l.range = 6.5f;
            l.intensity = 2.8f;
            l.color = Palette.Hex(0xFFD21F);
            l.shadows = LightShadows.None;

            if (helperBridge != null) helperBridge.SetActive(false);

            var node = go.AddComponent<CognitiveTrialNode>();
            node.TrialCode = code;
            node.SolvedMessage = solvedMsg;
            node.CrystalVisual = crystalRoot;
            node.BeaconLight = l;
            node.HelperBridge = helperBridge;
            return node;
        }

        static Transform FloatingBook(Transform parent, Vector3 at, float size, int i)
        {
            var g = Group(parent, "FloatingBook" + i); g.position = at;
            string[] cols = { "ToyRed", "Pad", "ToyBlue", "ToyYellow", "Banner" };
            Box(g, "Cover", at, new Vector3(size, size * 0.18f, size * 1.35f), Mt(cols[i % cols.Length]), collider: false);
            Box(g, "Pages", at + new Vector3(size * 0.04f, 0, 0), new Vector3(size * 0.94f, size * 0.15f, size * 1.3f), Mt("PageBack"), collider: false);
            g.gameObject.AddComponent<Spinner>().speed = (i % 2 == 0 ? 4 : -3);
            return g;
        }

        static void Clouds(Transform parent, float z0, float z1, int n)
        {
            var g = Group(parent, "Clouds");
            for (int i = 0; i < n; i++)
            {
                var rnd = new System.Random(i * 17 + 3);
                Vector3 p = new Vector3((rnd.Next(0, 2) == 0 ? -1 : 1) * (14 + (float)rnd.NextDouble() * 25), 6 + (float)rnd.NextDouble() * 14, z0 + (z1 - z0) * i / n);
                var c = ArtKit.Cutout(g, i % 2 == 0 ? "DEC_PaperCloud_A" : "DEC_PaperCloud_B", p, 5f + (float)rnd.NextDouble() * 4f, 180f, "wall");
                if (c != null) c.gameObject.AddComponent<YBillboard>();
            }
        }

        static void StarBits(Transform parent, Vector3 center, Vector3 size, int n)
        {
            var g = Group(parent, "StarBits");
            var rnd = new System.Random(n * 7 + (int)center.z);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = center + Vector3.Scale(size, new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f));
                if (Mathf.Abs(p.x - center.x) < 9f) p.x += Mathf.Sign(p.x - center.x + 0.01f) * 9f;   // keep the play lane clear
                var s = ArtKit.Prop(g, "PROP_GalStar", Vector3.zero, 0, Vector3.one * (0.4f + (float)rnd.NextDouble() * 1.2f));
                if (s != null) { s.position = p; s.gameObject.AddComponent<Spinner>().speed = 20 + (float)rnd.NextDouble() * 60; }
            }
        }

        /// <summary>The Throne Tower landmark ("weenie"): a giant stack of books with a glowing throne, far ahead.</summary>
        static void Tower(Transform parent, Vector3 basePos, float scale)
        {
            var g = Group(parent, "Landmark_ThroneTower"); g.position = basePos;
            string[] cols = { "ToyRed", "Banner", "ToyBlue", "ToyYellow", "Pad", "ToyRed", "ToyGreen", "Banner", "ToyBlue", "ToyRed" };
            float y = 0;
            for (int i = 0; i < 10; i++)
            {
                float h = 2.6f * scale;
                var b = Box(g, "Book" + i, basePos + Vector3.up * (y + h / 2), new Vector3((10 - i * 0.5f) * scale, h, (7 - i * 0.3f) * scale), Mt(cols[i]), collider: false);
                b.transform.rotation = Quaternion.Euler(0, i * 17f, (i % 2 == 0 ? 1.5f : -1.5f));
                y += h;
            }
            var top = Group(g, "Throne"); top.position = basePos + Vector3.up * y;
            ChairModel(top, 3f * scale, gold: true);
            var l = new GameObject("TowerGlow").AddComponent<Light>(); l.transform.SetParent(g, false);
            l.transform.position = top.position + Vector3.up * 4f * scale; l.type = LightType.Point; l.range = 30f * scale; l.intensity = 8f; l.color = Palette.Hex(0xFFD58A); l.shadows = LightShadows.None;
            var halo = Prim(g, PrimitiveType.Sphere, "Halo", top.position + Vector3.up * 3f * scale, Vector3.one * 7f * scale, Mt("Glow"), collider: false);
            halo.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            g.gameObject.AddComponent<Spinner>().speed = 3f;
            g.GetComponent<Spinner>().bob = 0f;
        }

        static void Bookshelf(Transform parent, string name, Vector3 top, float w, float d, float height)
        {
            var g = Group(parent, name);
            Box(g, "Top", top + Vector3.down * 0.25f, new Vector3(w, 0.5f, d), Mt("M_Kit_WoodLight"));
            Box(g, "Body", top + Vector3.down * (height / 2 + 0.5f), new Vector3(w - 0.4f, height, d - 0.4f), Mt("M_Kit_WoodDark"), collider: false);
            string[] cols = { "ToyRed", "Banner", "ToyBlue", "ToyYellow", "Pad", "ToyGreen" };
            for (int shelf = 0; shelf < 3; shelf++)
                for (int i = 0; i < 12; i++)
                {
                    float bh = 1.4f + (i * 7 % 5) * 0.15f;
                    var b = Box(g, "Spine", top + new Vector3(-w / 2 + 0.6f + i * (w - 1.2f) / 12f, -1.6f - shelf * 3f - (1.6f - bh) / 2f, -d / 2 + 0.05f), new Vector3(0.62f, bh, 0.3f), Mt(cols[(i + shelf) % cols.Length]), collider: false);
                    b.transform.localRotation = Quaternion.Euler(0, 0, (i % 5 == 0 ? 6 : 0));
                }
        }

        static void Stage(Transform parent, string name, Vector3 topCenter, float length, float depth = 5f)
        {
            Box(parent, name, topCenter + Vector3.down * 0.25f, new Vector3(length, 0.5f, depth), Mt("StageWood"));
            Box(parent, name + "_Apron", topCenter + new Vector3(0, -1.3f, -depth / 2 + 0.1f), new Vector3(length, 2.1f, 0.2f), Mt("Curtain"), collider: false);
        }

        static void Slider(Transform parent, string name, Vector3 at, Vector3 size, Vector3 offset, float period, float phase)
        {
            var go = Box(parent, name, at, size, Mt("Cardboard"));
            var mp = go.AddComponent<MovingPlatform>(); mp.offset = offset; mp.period = period; mp.phase = phase;
            // stagehand rope: reads as "this moves on purpose"
            Box(go.transform, "Rope", at + Vector3.up * 6f, new Vector3(0.06f / size.x, 12f / size.y, 0.06f / size.z), Mt("M_Kit_WoodLight"), collider: false).transform.localPosition = new Vector3(0, (6f + size.y / 2) / size.y, 0);
        }

        static void Arch(Transform parent, Vector3 at)
        {
            Box(parent, "ArchL", at + new Vector3(-1.7f, 2.2f, 0), new Vector3(0.4f, 4.4f, 0.4f), Mt("Gold"), collider: false);
            Box(parent, "ArchR", at + new Vector3(1.7f, 2.2f, 0), new Vector3(0.4f, 4.4f, 0.4f), Mt("Gold"), collider: false);
            Box(parent, "ArchTop", at + new Vector3(0, 4.5f, 0), new Vector3(3.8f, 0.4f, 0.4f), Mt("Gold"), collider: false);
        }

        static Transform Island(Transform parent, string name, Vector3 top, float radius, Material body, Material band)
        {
            var go = Prim(parent, PrimitiveType.Cylinder, name, top + Vector3.down * 0.6f, new Vector3(radius * 2, 0.6f, radius * 2), body);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var mc = go.AddComponent<MeshCollider>();
            var bottom = Prim(go.transform, PrimitiveType.Sphere, "Underside", top + Vector3.down * 1.4f, Vector3.one, body, collider: false);
            bottom.transform.localScale = new Vector3(0.95f, 2.4f, 0.95f);
            bottom.transform.localPosition = new Vector3(0, -1.4f, 0);
            var b = Prim(go.transform, PrimitiveType.Cylinder, "Band", top + Vector3.down * 0.35f, Vector3.one, band, collider: false);
            b.transform.localScale = new Vector3(1.03f, 0.25f, 1.03f); b.transform.localPosition = new Vector3(0, 0.45f, 0);
            DreamLook.AddOutline(go, 2f);
            return go.transform;
        }

        /// <summary>Train-track grind rail (Densha energy): yellow neon tube on navy posts. Points are world positions.</summary>
        static GrindRail Rail(Transform parent, string name, params Vector3[] pts)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var r = go.AddComponent<GrindRail>(); r.points = new List<Vector3>(pts); r.neon = new Color(1f, 0.85f, 0.2f);
            r.BuildMesh(Mt("Rail"), Mt("RailPost"));
            return r;
        }

        static void ShelfRun(Transform parent, string name, Vector3 a, Vector3 b, int i)
        {
            Vector3 mid = (a + b) / 2; float len = Vector3.Distance(a, b) + 5f;
            var rot = Quaternion.LookRotation(b - a);
            // two jump gaps on long runs keep the chase active
            bool gap = len > 30f && i % 2 == 1;
            if (!gap)
            {
                var top = Box(parent, name, mid + Vector3.down * 0.25f, new Vector3(5f, 0.5f, len), Mt("Ink")); top.transform.rotation = rot;
            }
            else
            {
                float l1 = len * 0.5f - 1.0f;
                var t1 = Box(parent, name + "_a", Vector3.Lerp(a, b, 0.25f) - (b - a).normalized * 1.25f + Vector3.down * 0.25f, new Vector3(5f, 0.5f, l1), Mt("Ink")); t1.transform.rotation = rot;
                var t2 = Box(parent, name + "_b", Vector3.Lerp(a, b, 0.75f) + (b - a).normalized * 1.25f + Vector3.down * 0.25f, new Vector3(5f, 0.5f, l1), Mt("Ink")); t2.transform.rotation = rot;
            }
            for (int s = -1; s <= 1; s += 2)
            {
                var edge = Box(parent, name + "_Edge", mid + rot * new Vector3(s * 2.55f, 0.02f, 0), new Vector3(0.12f, 0.06f, len), Mt("InkRed"), collider: false);
                edge.transform.rotation = rot;
            }
            var body = Box(parent, name + "_Shelf", mid + Vector3.down * 9f, new Vector3(4.6f, 17f, len - 1f), Mt("M_Kit_WoodDark"), collider: false);
            body.transform.rotation = rot;
            for (int k = 0; k < (int)(len / 1.2f); k++)
            {
                var sp = Box(parent, "Spine", mid + rot * new Vector3(2.35f, -2.2f - (k % 3) * 0.2f, -len / 2 + 0.6f + k * 1.2f), new Vector3(0.3f, 2.4f, 0.9f), k % 4 == 0 ? Mt("InkRed") : (k % 4 == 2 ? Mt("InkWhite") : Mt("Ink")), collider: false);
                sp.transform.rotation = rot;
            }
        }

        static void Candle(Transform parent, Transform look, Vector3 at)
        {
            Prim(parent, PrimitiveType.Cylinder, "CandleStand", at + Vector3.up * 0.6f, new Vector3(0.5f, 0.6f, 0.5f), Mt("StoneDark"), collider: false);
            Prim(parent, PrimitiveType.Cylinder, "Candle", at + Vector3.up * 1.55f, new Vector3(0.28f, 0.35f, 0.28f), Mt("Candle"), collider: false);
            Prim(parent, PrimitiveType.Sphere, "CandleFlame", at + Vector3.up * 2.05f, new Vector3(0.16f, 0.3f, 0.16f), Mt("Flame"), collider: false);
            var l = new GameObject("CandleLight").AddComponent<Light>(); l.transform.SetParent(look, false);
            l.transform.position = at + Vector3.up * 2.3f; l.type = LightType.Point; l.range = 7f; l.intensity = 2.2f; l.color = Palette.Hex(0xFFA552); l.shadows = LightShadows.None;
        }

        static void Sun(Transform look, Vector3 euler, Color c, float intensity)
        {
            var l = new GameObject("Key").AddComponent<Light>(); l.transform.SetParent(look, false);
            l.type = LightType.Directional; l.color = c; l.intensity = intensity; l.shadows = LightShadows.Soft; l.transform.rotation = Quaternion.Euler(euler);
        }

        static void Fill(Transform look, Vector3 euler, Color c, float intensity)
        {
            var l = new GameObject("Fill").AddComponent<Light>(); l.transform.SetParent(look, false);
            l.type = LightType.Directional; l.color = c; l.intensity = intensity; l.shadows = LightShadows.None; l.transform.rotation = Quaternion.Euler(euler);
        }

        static void Spot(Transform look, Vector3 from, Vector3 to, Color c, float angle, float range)
        {
            var l = new GameObject("Spot").AddComponent<Light>(); l.transform.SetParent(look, false);
            l.type = LightType.Spot; l.color = c; l.intensity = 9f; l.spotAngle = angle; l.range = range; l.shadows = LightShadows.Soft;
            l.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
        }

        /// <summary>Fake volumetric god ray: an additive soft beam quad from <paramref name="from"/> to <paramref name="to"/> (RESEARCH_V2 §2).</summary>
        static void Beam(Transform parent, Vector3 from, Vector3 to, float width)
        {
            if (Mt("Beam") == null) return;
            for (int k = 0; k < 2; k++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.name = "GodRay";
                q.transform.SetParent(parent, false);
                Vector3 mid = (from + to) / 2, dir = (to - from);
                q.transform.position = mid;
                q.transform.rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.right).normalized, -dir.normalized) * Quaternion.Euler(0, k * 90, 0);
                q.transform.localScale = new Vector3(width, dir.magnitude, 1);
                var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = Mt("Beam"); r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        static void Section(Transform parent, string id, string name, string objective, Vector3 pos, Vector3 size)
        {
            var s = new GameObject("Section_" + id).AddComponent<DreamSection>();
            s.transform.SetParent(parent, false); s.transform.position = pos;
            s.sectionId = id; s.displayName = name; s.objective = objective; s.size = size;
            s.order = _sectionOrder++;
        }
        static int _sectionOrder;

        /// <summary>Per-chapter post volume (only active while the chapter's Look root is enabled).</summary>
        static void Profile(Transform look, string name, float exposure, float contrast, float saturation, float bloom, float vignette, float dofFocus = 0f, float chroma = 0f)
        {
            GrayboxAssets.EnsureFolder(ProfileDir);
            string path = $"{ProfileDir}/{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.components.RemoveAll(x => x == null);   // v1 never saved its components (all post silently fell back to the URP default)
            T Get<T>() where T : VolumeComponent { if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); } c.active = true; return c; }
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(exposure); ca.contrast.Override(contrast); ca.saturation.Override(saturation);
            var tm = Get<Tonemapping>(); tm.mode.Override(TonemappingMode.None);
            var bl = Get<Bloom>(); bl.intensity.Override(bloom); bl.threshold.Override(1.15f); bl.scatter.Override(0.55f);
            var cab = Get<ChromaticAberration>(); cab.intensity.Override(chroma);
            var vg = Get<Vignette>(); vg.intensity.Override(vignette); vg.smoothness.Override(0.45f); vg.color.Override(Palette.MidnightPlum);
            if (dofFocus > 0f)
            {
                var dof = Get<DepthOfField>(); dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(dofFocus + 6f); dof.gaussianEnd.Override(dofFocus + 22f); dof.gaussianMaxRadius.Override(1.4f);
            }
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX"); go.transform.SetParent(look, false);
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 5; v.sharedProfile = p;
        }
    }
}
