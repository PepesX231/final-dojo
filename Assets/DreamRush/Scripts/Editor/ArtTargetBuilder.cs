using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Look;
using DreamRush.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// ART TARGET SCENE (docs/VISUAL_RECONSTRUCTION.md §3): one representative corner of the dream living room —
    /// apple, chair, book-page floor, pop-up walls, 4 furniture pieces, 2D cutouts, real lighting/post and the real
    /// gameplay camera. The visual standard is locked here before the full level is rebuilt.
    /// </summary>
    public static class ArtTargetBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Art_Target.unity";

        [MenuItem("DreamRush/Build/Art Target Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            DreamLookSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ART_TARGET").transform;

            // ---- ground: an open picture book (physics = flat invisible box; visuals = paper pages)
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Collider_Floor"; floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0, -0.25f, 0); floor.transform.localScale = new Vector3(18, 0.5f, 13);
            Object.DestroyImmediate(floor.GetComponent<MeshRenderer>());
            ArtKit.Book(root, "Book", Vector3.zero, 18f, 13f, bookmarkAt: new Vector3(-4.6f, 0, -6.5f));
            ArtKit.Rug(root, new Vector3(0.6f, 0, 0.3f), 6.2f);

            // ---- pop-up walls (outside on the left of a→b)
            ArtKit.WallRun(root, "Walls_North", new Vector3(-8.6f, 0, 6.15f), new Vector3(8.6f, 0, 6.15f), 1.75f, 4.3f, 1);
            ArtKit.WallRun(root, "Walls_West", new Vector3(-8.6f, 0, -5.9f), new Vector3(-8.6f, 0, 6.15f), 1.6f, 4.0f, 2);
            ArtKit.Cutout(root, "DEC_Window_Night", new Vector3(3.6f, 0.08f, 6.05f), 1.62f, 0, "wall");
            ArtKit.Cutout(root, "DEC_Poster_Grandpa", new Vector3(-1.6f, 0.42f, 6.06f), 1.15f, -2, "wall");

            // ---- furniture (the colliders of the real level stay simple boxes; here visuals only)
            ArtKit.Prop(root, "PROP_Sofa", new Vector3(-4.3f, 0, 4.7f), 0f);
            ArtKit.Prop(root, "PROP_CoffeeTable", new Vector3(-3.1f, 0, 1.7f), 8f);
            var lamp = ArtKit.Prop(root, "PROP_FloorLamp", new Vector3(6.6f, 0, 4.7f), 10f);
            ArtKit.Prop(root, "PROP_ToyBlock", new Vector3(4.3f, 0, -1.4f), 24f, null, outline: true);
            ArtKit.Prop(root, "PROP_Cushion", new Vector3(6.9f, 0, 0.6f), 12f, new Vector3(2.2f, 0.5f, 2.2f), outline: true);
            ArtKit.Prop(root, "PROP_Cushion", new Vector3(7.2f, 0, 3.0f), -8f, new Vector3(2.0f, 0.95f, 2.0f), outline: true);

            // ---- 2D cutouts with a job: plant frames the sofa corner; clouds + moon give depth beyond the book
            ArtKit.Cutout(root, "DEC_PaperPlant", new Vector3(-7.4f, 0, 5.0f), 2.1f, 0, "stand");
            ArtKit.Cutout(root, "DEC_PaperCloud_A", new Vector3(16f, -1.5f, 14f), 4.2f, -25, "stand");
            ArtKit.Cutout(root, "DEC_PaperCloud_B", new Vector3(-15f, 3.5f, 15f), 5.5f, 20, "stand");
            ArtKit.Cutout(root, "DEC_PaperMoon", new Vector3(9f, 7f, 24f), 3.5f, 0, "stand");
            // a few paper stars hung in the night beyond the book (depth + "it's a dream at night"), never over the play area
            Vector3[] stars = { new Vector3(-11f, 6f, 20f), new Vector3(-4f, 9f, 28f), new Vector3(14f, 9.5f, 30f), new Vector3(19f, 4f, 16f) };
            for (int i = 0; i < stars.Length; i++) ArtKit.Cutout(root, "DEC_PaperStar", stars[i], 1.1f + (i % 2) * 0.5f, i * 17f, "stand");
            BuildBackground(root);

            // ---- characters
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(-0.6f, 0.05f, -2.2f), Quaternion.Euler(0, 35f, 0));
            var chairRoot = new GameObject("BOSS_Chair (static)").transform;
            chairRoot.SetParent(root, false);
            chairRoot.localPosition = new Vector3(2.0f, 0, 1.2f);
            chairRoot.localRotation = Quaternion.Euler(0, 215f, 0);
            ChairCharacterSetup.Setup();
            var chairModel = ChairCharacterSetup.Instantiate(chairRoot, "BOSS_Chair_Model", withFace: true);
            if (chairModel != null) DreamLook.AddOutline(chairModel.gameObject, 2.6f);

            // ---- light, post, camera
            DreamLighting.Apply(root, lamp != null ? lamp.position + new Vector3(-0.35f, 2.35f, 0) : (Vector3?)null, new Vector3(0.5f, 0, 0.5f));
            DreamLookSetup.AddVolume(root, dream: true);
            var camGo = new GameObject("StageCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            camGo.AddComponent<AudioListener>();
            var stage = camGo.AddComponent<StageCameraController>();
            stage.target = player.transform;
            stage.input = player.GetComponent<PlayerInputReader>();
            CameraTuning.ApplyArena(stage);
            stage.secondaryTarget = chairRoot;
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = camGo.transform;
            EditorUtility.SetDirty(motor);
            var ctx = new GameObject("SceneContext").AddComponent<Flow.SceneContext>();
            ctx.kind = Flow.SceneKind.Test;
            new GameObject("SleepStability").AddComponent<Dream.SleepStability>();
            new GameObject("DreamHUD").AddComponent<UI.DreamHUD>();
            new GameObject("ArtTargetHUD").AddComponent<Debugging.ArtTargetHUD>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[DreamRush] Art Target built: " + ScenePath);
        }

        /// <summary>Far, low-saturation depth layer: closed books drifting in the night (fog does the rest).</summary>
        static void BuildBackground(Transform root)
        {
            var g = new GameObject("Background_FloatingBooks").transform;
            g.SetParent(root, false);
            var covers = new[]
            {
                ArtKit.Mat("M_Look_BgBookA", Color.Lerp(Palette.DreamLavender, Palette.MidnightPlum, 0.45f)),
                ArtKit.Mat("M_Look_BgBookB", Color.Lerp(Palette.SkyBlue, Palette.MidnightPlum, 0.55f)),
                ArtKit.Mat("M_Look_BgBookC", Color.Lerp(Palette.ApricotOrange, Palette.MidnightPlum, 0.6f)),
            };
            var pages = ArtKit.Mat("M_Look_BgPages", Color.Lerp(Palette.WarmCream, Palette.DreamLavender, 0.35f));
            (Vector3 p, Vector3 r, Vector3 s)[] books =
            {
                (new Vector3(-27, -7f, 20), new Vector3(8, 25, -6), new Vector3(7, 1.2f, 9)),
                (new Vector3(18, 1f, 20), new Vector3(-14, -30, 18), new Vector3(6, 1f, 8)),
                (new Vector3(-6, 6f, 34), new Vector3(20, 10, -12), new Vector3(9, 1.4f, 12)),
                (new Vector3(22, -8f, 4), new Vector3(-6, 50, 10), new Vector3(5, 0.9f, 7)),
            };
            for (int i = 0; i < books.Length; i++)
            {
                var b = new GameObject("FloatingBook_" + i).transform;
                b.SetParent(g, false);
                b.localPosition = books[i].p; b.localRotation = Quaternion.Euler(books[i].r);
                var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(cover.GetComponent<Collider>());
                cover.transform.SetParent(b, false); cover.transform.localScale = books[i].s;
                cover.GetComponent<MeshRenderer>().sharedMaterial = covers[i % covers.Length];
                var pg = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(pg.GetComponent<Collider>());
                pg.transform.SetParent(b, false);
                pg.transform.localScale = new Vector3(books[i].s.x - 0.4f, books[i].s.y * 0.7f, books[i].s.z - 0.3f);
                pg.transform.localPosition = new Vector3(0.25f, 0, 0);
                pg.GetComponent<MeshRenderer>().sharedMaterial = pages;
                foreach (var r in b.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }

    /// <summary>Dream lighting rig (Art Bible §5.5): one warm dominant key, cool lavender ambient, a warm focal pool, plum fog.</summary>
    public static class DreamLighting
    {
        public static readonly Color Background = new Color(0.17f, 0.14f, 0.26f);

        public static void Apply(Transform parent, Vector3? lampBulb, Vector3 focus)
        {
            var key = new GameObject("Light_Key (warm, upper-left-front)").AddComponent<Light>();
            key.transform.SetParent(parent, false);
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.9f, 0.76f);
            key.intensity = 0.8f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.85f;
            key.transform.rotation = Quaternion.Euler(52f, 32f, 0f);
            RenderSettings.sun = key;

            var pool = new GameObject("Light_FocusPool").AddComponent<Light>();
            pool.transform.SetParent(parent, false);
            pool.type = LightType.Spot;
            pool.color = new Color(1f, 0.86f, 0.66f);
            pool.intensity = 3.5f; pool.range = 22f; pool.spotAngle = 78f; pool.innerSpotAngle = 30f;
            pool.shadows = LightShadows.None;
            pool.transform.position = focus + new Vector3(0, 11f, -2f);
            pool.transform.rotation = Quaternion.LookRotation((focus - pool.transform.position).normalized);

            if (lampBulb.HasValue)
            {
                var practical = new GameObject("Light_FloorLamp").AddComponent<Light>();
                practical.transform.SetParent(parent, false);
                practical.type = LightType.Point;
                practical.color = new Color(1f, 0.78f, 0.5f);
                practical.intensity = 1.2f; practical.range = 4f;
                practical.shadows = LightShadows.None;
                practical.transform.position = lampBulb.Value;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.63f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.5f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.44f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Background;
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 75f;
            RenderSettings.skybox = null;
        }

        public static void ConfigureCamera(Camera cam)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.nearClipPlane = 0.2f; cam.farClipPlane = 200f;
            DreamLookSetup.ConfigureCamera(cam);
        }
    }
}
