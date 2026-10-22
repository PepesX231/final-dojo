using DreamRush.CameraSystem;
using DreamRush.Combat;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.Player;
using DreamRush.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M12 GRAYBOX: COUNT MIDNIGHT's arena on its own (hard rule: graybox + prototype before art). A 22 m clock-dial
    /// plaza: XII numeral blocks as low cover, an outer ring of pillars, a time-stop post volume (weight 0 until the
    /// boss stops the clock). Pip gets the combat kit + the open-world move set (dash → roll), 6 moons.
    /// Rebuild: DreamRush/Build/Boss Arena — Count Midnight (graybox).
    /// </summary>
    public static class BossArenaBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Test_BossMidnight.unity";
        public const float R = 22f;

        [MenuItem("DreamRush/Build/Boss Arena — Count Midnight (graybox)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            DreamLookSetup.Ensure();
            StyleSetup.Ensure();
            var prefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("BOSS_ARENA_GRAYBOX").transform;
            BuildArena(root, Vector3.zero);
            var boss = BuildBoss(root, new Vector3(0, 0, 8f));

            var level = new GameObject("DreamLevelManager").AddComponent<DreamLevelManager>();
            level.killY = -20f;
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(0, 0.1f, -R + 3f), Quaternion.identity);
            SetupFighter(player);
            var tpc = PlayerRigBuilder.BuildHubCamera(player);
            CameraTuning.ApplyFollow(tpc);
            tpc.distance = 6.2f; tpc.defaultPitch = 16f;
            var cam = tpc.GetComponent<Camera>();
            DreamLighting.ConfigureCamera(cam);
            cam.clearFlags = CameraClearFlags.Skybox; cam.farClipPlane = 600f;
            tpc.transform.position = player.transform.position + new Vector3(0, 2.5f, -6f);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = tpc.transform; EditorUtility.SetDirty(motor);
            level.player = motor;
            var st = new GameObject("SleepStability").AddComponent<SleepStability>(); st.baseMax = 6;
            new GameObject("DreamHUD").AddComponent<DreamHUD>();
            var ctx = new GameObject("SceneContext").AddComponent<SceneContext>(); ctx.kind = SceneKind.Dream;
            PhantomStyle(new GameObject("StylePack").AddComponent<StylePack>());

            EditorSceneManager.SaveScene(scene, ScenePath);
            MovementGymBuilder.AddToBuildSettings(ScenePath);
            Debug.Log("[DreamRush] Boss arena (graybox) built: " + ScenePath);
        }

        /// <summary>Midnight Palace look: Persona-ish ink + print dots, posterized light, Pip in a phantom-thief mask.</summary>
        public static void PhantomStyle(StylePack sp)
        {
            sp.ink = 0.9f; sp.inkColor = new Color(0.04f, 0.02f, 0.08f); sp.inkWidth = 1.5f; sp.inkFar = 140f;
            sp.halftone = 0.5f; sp.halftoneColor = new Color(0.12f, 0.05f, 0.35f, 0.8f); sp.halftoneScale = 5f;
            sp.posterize = 0.35f; sp.saturate = 0.2f;
            sp.outfit = StylePack.Outfit.PhantomMask;
            sp.pipRim = new Color(1f, 0.25f, 0.45f); sp.pipRimStrength = 0.55f; sp.pipRampSoftness = 0.06f; sp.pipShadow = new Color(0.35f, 0.22f, 0.6f);
        }

        /// <summary>Combat kit on a player instance (shared with the level-2 builder).</summary>
        public static void SetupFighter(GameObject player)
        {
            var ab = player.GetComponent<PlayerAbilities>(); if (ab == null) ab = player.AddComponent<PlayerAbilities>();
            ab.dash = false;   // Ctrl / RT = dodge roll in combat levels
            if (player.GetComponent<PlayerCombat>() == null) player.AddComponent<PlayerCombat>();
        }

        public static void BuildArena(Transform root, Vector3 c)
        {
            var stone = ArtKit.Mat("M_CM_ArenaStone", new Color(0.32f, 0.28f, 0.5f), rampSoft: 0.4f);
            var dial = ArtKit.Mat("M_CM_ArenaDial", new Color(0.95f, 0.9f, 1f), rim: 0f);
            var gold = ArtKit.Mat("M_CM_ArenaGold", new Color(1f, 0.8f, 0.35f), spec: 0.5f);
            var g = new GameObject("Arena").transform; g.SetParent(root, false); g.position = c;
            Prim(g, PrimitiveType.Cylinder, "Base", c + Vector3.down * 0.6f, new Vector3(R * 2 + 6, 0.6f, R * 2 + 6), stone, true);
            var floor = Prim(g, PrimitiveType.Cylinder, "Dial", c + Vector3.down * 0.1f, new Vector3(R * 2, 0.1f, R * 2), dial, true);
            Prim(g, PrimitiveType.Cylinder, "DialRing", c + Vector3.down * 0.05f, new Vector3(R * 2 - 3f, 0.08f, R * 2 - 3f), stone, false);
            Prim(g, PrimitiveType.Cylinder, "DialInner", c + Vector3.down * 0.02f, new Vector3(R * 2 - 4f, 0.08f, R * 2 - 4f), dial, false);
            for (int i = 0; i < 60; i++)
            {
                float a = i / 60f * Mathf.PI * 2f; Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var tick = Prim(g, PrimitiveType.Cube, "Tick" + i, c + d * (R - 1.0f) + Vector3.up * 0.03f, new Vector3(0.18f, 0.06f, i % 5 == 0 ? 1.4f : 0.6f), stone, false);
                tick.transform.rotation = Quaternion.LookRotation(d);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f; Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                // numeral blocks (low cover vs bullets) and the outer pillar ring
                var blk = Prim(g, PrimitiveType.Cube, "Numeral" + (i == 0 ? 12 : i), c + d * (R - 4.2f) + Vector3.up * 0.6f, new Vector3(1.4f, 1.2f, 0.7f), gold, true);
                blk.transform.rotation = Quaternion.LookRotation(d);
                Prim(g, PrimitiveType.Cylinder, "Pillar" + i, c + d * (R + 1.4f) + Vector3.up * 4f, new Vector3(1.6f, 4f, 1.6f), stone, true);
                Prim(g, PrimitiveType.Sphere, "PillarLamp" + i, c + d * (R + 1.4f) + Vector3.up * 8.4f, Vector3.one * 1.1f, gold, false);
            }
            // invisible boundary ring
            for (int i = 0; i < 36; i++)
            {
                float a = i / 36f * Mathf.PI * 2f; Vector3 d = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var w = new GameObject("Wall" + i); w.transform.SetParent(g, false);
                w.transform.SetPositionAndRotation(c + d * (R + 0.6f) + Vector3.up * 4f, Quaternion.LookRotation(d));
                w.AddComponent<BoxCollider>().size = new Vector3(4.4f, 8f, 0.6f);
            }
            // sky, light, post
            RenderSettings.skybox = GalaxySelectBuilder.NebulaSky("M_Sky_Midnight", Palette.Hex(0x05061A), Palette.Hex(0x2B1A5E), Palette.Hex(0x080620), Palette.Hex(0xFF2E88), Palette.Hex(0x00E1FF), 1.1f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Palette.Hex(0x6A62C8); RenderSettings.ambientEquatorColor = Palette.Hex(0x5A3A8C); RenderSettings.ambientGroundColor = Palette.Hex(0x1A1430);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Palette.Hex(0x221A50); RenderSettings.fogStartDistance = 40; RenderSettings.fogEndDistance = 220;
            var sun = new GameObject("Moonlight").AddComponent<Light>(); sun.transform.SetParent(g, false);
            sun.type = LightType.Directional; sun.color = Palette.Hex(0xBFD0FF); sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, 200, 0);
            var rim = new GameObject("RimLight").AddComponent<Light>(); rim.transform.SetParent(g, false);
            rim.type = LightType.Directional; rim.color = Palette.Hex(0xFF4FA8); rim.intensity = 0.55f; rim.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(20, 30, 0);
            Profile(g, "PP_BossMidnight", 0.05f, 18f, 12f, 0.8f, 0.32f, 5);
        }

        public static CountMidnight BuildBoss(Transform root, Vector3 pos)
        {
            var go = new GameObject("BOSS_CountMidnight");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, 180, 0));
            var boss = go.AddComponent<CountMidnight>();
            CountMidnightSetup.Attach(boss);
            boss.arenaCenter = new Vector3(0, pos.y, 0); boss.arenaRadius = R;
            var vol = Profile(root, "PP_TimeStop", -0.15f, 30f, -85f, 1.1f, 0.5f, 50, chroma: 0.9f);
            vol.weight = 0f;
            boss.timeStopVolume = vol;
            return boss;
        }

        static Volume Profile(Transform parent, string name, float exposure, float contrast, float saturation, float bloom, float vignette, int priority, float chroma = 0.1f)
        {
            const string dir = "Assets/DreamRush/Settings";
            GrayboxAssets.EnsureFolder(dir);
            string path = $"{dir}/{name}.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path); }
            p.components.RemoveAll(x => x == null);   // v1 never saved its components (all post silently fell back to the URP default)
            T Get<T>() where T : VolumeComponent { if (!p.TryGet<T>(out var c)) { c = p.Add<T>(true); AssetDatabase.AddObjectToAsset(c, p); } c.active = true; return c; }
            var ca = Get<ColorAdjustments>(); ca.postExposure.Override(exposure); ca.contrast.Override(contrast); ca.saturation.Override(saturation);
            var tm = Get<Tonemapping>(); tm.mode.Override(TonemappingMode.None);   // comic flat colour: Neutral capped whites at ~0.82
            var bl = Get<Bloom>(); bl.intensity.Override(bloom); bl.threshold.Override(0.9f); bl.scatter.Override(0.7f);
            var vg = Get<Vignette>(); vg.intensity.Override(vignette); vg.smoothness.Override(0.45f); vg.color.Override(Palette.Hex(0x0A0620));
            var ab = Get<ChromaticAberration>(); ab.intensity.Override(chroma);
            EditorUtility.SetDirty(p); AssetDatabase.SaveAssets();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = priority; v.sharedProfile = p;
            return v;
        }

        static GameObject Prim(Transform parent, PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m, bool collider)
        {
            var go = GameObject.CreatePrimitive(t); go.name = name;
            go.transform.SetParent(parent, false); go.transform.position = pos; go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (t == PrimitiveType.Cylinder) { Object.DestroyImmediate(go.GetComponent<Collider>()); go.AddComponent<MeshCollider>(); }
            return go;
        }
    }
}
