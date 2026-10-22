using System.Collections.Generic;
using System.Linq;
using DreamRush.CameraSystem;
using DreamRush.Core;
using DreamRush.Debugging;
using DreamRush.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// Builds the M1 graybox "Movement Gym" (GDD Phase B acceptance: moving around an empty room must feel
    /// enjoyable). Every station measures one part of the jump envelope. Rebuild any time from the menu.
    /// </summary>
    public static class MovementGymBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/Test_MovementGym.unity";
        public const string PlayerPrefabPath = GrayboxAssets.PrefabDir + "/PF_Player.prefab";

        [MenuItem("DreamRush/Build/Player Prefab")]
        public static GameObject BuildPlayerPrefab()
        {
            GrayboxAssets.EnsureFolder(GrayboxAssets.PrefabDir);
            var config = GrayboxAssets.PlayerConfig();
            var shadowMat = GrayboxAssets.BlobShadowMaterial();
            GameObject model = AppleCharacterSetup.ModelExists ? AppleCharacterSetup.Setup() : null;
            System.Func<Transform, GameObject> factory = null;
            if (model != null)
                factory = parent => (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            var go = PlayerRigBuilder.BuildPlayer(config, withInput: true, GrayboxAssets.Lit, shadowMat, factory);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[DreamRush] Player prefab saved: " + PlayerPrefabPath);
            return prefab;
        }

        [MenuItem("DreamRush/Build/Movement Gym")]
        public static void BuildMovementGym()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode before building scenes."); return; }
            var prefab = BuildPlayerPrefab();
            GrayboxAssets.EnsureFolder(GrayboxAssets.SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();

            var floorMat = GrayboxAssets.GridFloor("GRAYBOX_FloorGrid", Palette.GrayboxFloor,
                Color.Lerp(Palette.GrayboxFloor, Palette.MidnightPlum, 0.25f), 30f);
            var walk = GrayboxAssets.Lit("GRAYBOX_Walkable", Palette.GrayboxWalkable);
            var wall = GrayboxAssets.Lit("GRAYBOX_Wall", Palette.GrayboxWall);
            var accent = GrayboxAssets.Lit("GRAYBOX_Accent", Palette.GrayboxAccent);
            var hazard = GrayboxAssets.Lit("GRAYBOX_Hazard", Palette.GrayboxHazard);
            var interact = GrayboxAssets.Lit("GRAYBOX_Interactable", Palette.GrayboxInteractable);

            var env = new GameObject("GRAYBOX_Environment").transform;

            // Ground 60×60 with 2 m grid (texture 2 cells per tile, 30 tiles)
            Block(env, "Ground", new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60), floorMat);
            // Boundary walls
            Block(env, "Bound_N", new Vector3(0, 2, 30.5f), new Vector3(62, 4, 1), wall);
            Block(env, "Bound_S", new Vector3(0, 2, -30.5f), new Vector3(62, 4, 1), wall);
            Block(env, "Bound_E", new Vector3(30.5f, 2, 0), new Vector3(1, 4, 60), wall);
            Block(env, "Bound_W", new Vector3(-30.5f, 2, 0), new Vector3(1, 4, 60), wall);

            // Station 1 — Step heights (reachable ≤ jumpHeight)
            var steps = Group(env, "Station_StepHeights");
            float[] heights = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };
            for (int i = 0; i < heights.Length; i++)
            {
                float h = heights[i];
                var pos = new Vector3(5 + i * 3.5f, h * 0.5f, 10);
                Block(steps, $"Step_{h:F1}m", pos, new Vector3(2.2f, h, 2.2f), h > 2.1f ? hazard : walk);
                Label(steps, $"{h:F1} m", pos + new Vector3(0, h * 0.5f + 0.6f, -1.2f));
            }

            // Station 2 — Gap course (platforms at 1 m, gaps 3..7 m along +Z, east side)
            var gaps = Group(env, "Station_Gaps");
            float[] gapSizes = { 3f, 4f, 5f, 6f, 7f };
            const float gapX = 26f;
            float z = -25f;
            Block(gaps, "Gap_Start", new Vector3(gapX, 0.5f, z), new Vector3(3, 1, 3), walk);
            Label(gaps, "GAPS (walk 4 / sprint 6)", new Vector3(gapX, 1.9f, z - 1.6f));
            foreach (var g in gapSizes)
            {
                z += 1.5f + g + 1.5f;
                var p = new Vector3(gapX, 0.5f, z);
                Block(gaps, $"Gap_{g:F0}m_Landing", p, new Vector3(3, 1, 3), g >= 7f ? hazard : walk);
                Label(gaps, $"{g:F0} m", p + new Vector3(0, 1.4f, -1.6f));
            }

            // Station 3 — Slopes 20/35/50°
            var slopes = Group(env, "Station_Slopes");
            float[] angles = { 20f, 35f, 50f };
            for (int i = 0; i < angles.Length; i++)
            {
                float x = -16f - i * 4.5f;
                Ramp(slopes, $"Slope_{angles[i]:F0}deg", x, 2f, angles[i], 7f, angles[i] > 45f ? hazard : walk);
                Label(slopes, $"{angles[i]:F0}°", new Vector3(x, 1.0f, 1.2f));
            }

            // Station 4 — Low ceiling tunnel (bonk test) and camera corridor
            var tunnel = Group(env, "Station_TunnelAndCorridor");
            Block(tunnel, "Tunnel_Ceiling", new Vector3(8, 1.95f, -10), new Vector3(8, 0.3f, 3), wall);
            Block(tunnel, "Tunnel_WallA", new Vector3(8, 0.9f, -11.65f), new Vector3(8, 1.8f, 0.3f), wall);
            Block(tunnel, "Tunnel_WallB", new Vector3(8, 0.9f, -8.35f), new Vector3(8, 1.8f, 0.3f), wall);
            Label(tunnel, "LOW CEILING 1.8 m", new Vector3(8, 2.6f, -8.2f));

            Block(tunnel, "Corridor_WallA", new Vector3(14, 2f, -18), new Vector3(0.4f, 4f, 12), wall);
            Block(tunnel, "Corridor_WallB", new Vector3(16.4f, 2f, -18), new Vector3(0.4f, 4f, 12), wall);
            Label(tunnel, "CAMERA CORRIDOR", new Vector3(15.2f, 4.6f, -11.8f));

            // Station 5 — High ledge + drop (landing/hard landing, coyote off a tall edge)
            var ledge = Group(env, "Station_Ledge");
            Block(ledge, "Ledge_Stairs1", new Vector3(-6, 0.4f, -12), new Vector3(3, 0.8f, 3), walk);
            Block(ledge, "Ledge_Stairs2", new Vector3(-6, 0.8f, -15), new Vector3(3, 1.6f, 3), walk);
            Block(ledge, "Ledge_Stairs3", new Vector3(-6, 1.2f, -18), new Vector3(3, 2.4f, 3), walk);
            Block(ledge, "Ledge_Top", new Vector3(-12, 1.6f, -18), new Vector3(9, 3.2f, 3), walk);
            Block(ledge, "Ledge_FarIsland", new Vector3(-21.5f, 1.6f, -18), new Vector3(3, 3.2f, 3), accent);
            Label(ledge, "COYOTE: 3.5 m gap at 3.2 m", new Vector3(-17.5f, 4.6f, -19.6f));

            // Station 6 — Slalom pillars near spawn (turning / braking)
            var slalom = Group(env, "Station_Slalom");
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -1.5f : 1.5f, 0.75f, 7f + i * 3f);
                Cylinder(slalom, $"Pillar_{i}", p, new Vector3(0.8f, 0.75f, 0.8f), interact);
            }
            Label(slalom, "SLALOM", new Vector3(0, 2.2f, 5.5f));

            // Player + camera
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(0, 0.05f, 0), Quaternion.identity);
            var cam = PlayerRigBuilder.BuildHubCamera(player);
            cam.transform.position = new Vector3(0, 2.5f, -5.5f);
            var camComponent = cam.GetComponent<Camera>();
            camComponent.clearFlags = CameraClearFlags.SolidColor;
            camComponent.backgroundColor = Color.Lerp(Palette.DreamLavender, Palette.MidnightPlum, 0.35f);
            var motor = player.GetComponent<PlayerMotor3D>();
            motor.CameraTransform = cam.transform;
            EditorUtility.SetDirty(motor);

            var hud = new GameObject("DEV_MovementDebugHUD").AddComponent<MovementDebugHUD>();
            hud.motor = motor;
            hud.input = player.GetComponent<PlayerInputReader>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("[DreamRush] Movement Gym built: " + ScenePath);
        }

        // ---------------------------------------------------------------- helpers
        static void SetupLighting()
        {
            var lightGo = new GameObject("Sun_Key");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.Lerp(Color.white, Palette.ApricotOrange, 0.25f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(Palette.DreamLavender, Color.white, 0.35f);
            RenderSettings.ambientEquatorColor = Color.Lerp(Palette.WarmCream, Palette.DreamLavender, 0.5f) * 0.8f;
            RenderSettings.ambientGroundColor = Color.Lerp(Palette.MidnightPlum, Palette.WarmCream, 0.3f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        public static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 center, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // Primitive cylinder has a capsule collider; replace with a mesh-accurate one.
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<MeshCollider>().convex = true;
            return go;
        }

        /// <summary>Ramp whose top surface starts at ground level at z=zStart and rises along +Z.</summary>
        static void Ramp(Transform parent, string name, float x, float zStart, float angleDeg, float length, Material mat)
        {
            const float t = 0.4f;
            float a = angleDeg * Mathf.Deg2Rad;
            float cy = length * 0.5f * Mathf.Sin(a) - t * 0.5f * Mathf.Cos(a);
            float cz = zStart + t * 0.5f * Mathf.Sin(a) + length * 0.5f * Mathf.Cos(a);
            var go = Block(parent, name, new Vector3(x, cy, cz), new Vector3(3f, t, length), mat);
            go.transform.localRotation = Quaternion.Euler(-angleDeg, 0, 0);
            // Plateau at the top so the slope can be walked off
            float topY = length * Mathf.Sin(a);
            float topZ = zStart + length * Mathf.Cos(a);
            Block(parent, name + "_Plateau", new Vector3(x, topY * 0.5f, topZ + 1.5f), new Vector3(3f, topY, 3f), mat);
        }

        static void Label(Transform parent, string text, Vector3 pos)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Palette.DarkInk;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        public static void AddToBuildSettings(string path, bool first = false)
        {
            var list = EditorBuildSettings.scenes.Where(s => s.path != path && !string.IsNullOrEmpty(s.path)).ToList();
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) list.Insert(0, entry); else list.Add(entry);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
