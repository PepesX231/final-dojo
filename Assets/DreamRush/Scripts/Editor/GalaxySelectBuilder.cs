using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// DREAM GALAXY (level select after sleeping, Astro Bot reference): nebula sky, four storybook planets,
    /// Pip on a flying pillow, drifting star bits and dust. Runtime logic: <see cref="GalaxySelect"/>.
    /// </summary>
    public static class GalaxySelectBuilder
    {
        public const string ScenePath = GrayboxAssets.SceneDir + "/DreamGalaxy.unity";

        public static Material NebulaSky(string name, Color top, Color horizon, Color bottom, Color a, Color b, float strength = 0.9f)
        {
            GrayboxAssets.EnsureFolder(ArtKit.MatDir);
            string path = $"{ArtKit.MatDir}/{name}.mat";
            var sh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/DreamRush/Shaders/NebulaSky.shader");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetColor("_TopColor", top); m.SetColor("_HorizonColor", horizon); m.SetColor("_BottomColor", bottom);
            m.SetColor("_NebulaA", a); m.SetColor("_NebulaB", b); m.SetFloat("_NebulaStrength", strength);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("DreamRush/Build/Dream Galaxy (level select)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[DreamRush] Stop play mode first."); return; }
            DreamLookSetup.Ensure();
            var playerPrefab = MovementGymBuilder.BuildPlayerPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("DREAM_GALAXY").transform;

            // ---- sky + light: deep plum night, pink/blue nebula, warm key from the "sun" star, lavender fill
            RenderSettings.skybox = NebulaSky("M_Sky_Galaxy", new Color(0.05f, 0.04f, 0.14f), new Color(0.3f, 0.16f, 0.42f), new Color(0.08f, 0.05f, 0.18f),
                new Color(0.95f, 0.35f, 0.7f), new Color(0.25f, 0.5f, 1f), 1.0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.48f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.32f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.14f, 0.3f);
            RenderSettings.fog = false;
            var sun = new GameObject("Key_Sun").AddComponent<Light>();
            sun.transform.SetParent(root); sun.type = LightType.Directional; sun.intensity = 1.25f; sun.color = new Color(1f, 0.88f, 0.72f);
            sun.transform.rotation = Quaternion.Euler(38, -42, 0); sun.shadows = LightShadows.Soft;
            var fill = new GameObject("Fill_Lav").AddComponent<Light>();
            fill.transform.SetParent(root); fill.type = LightType.Directional; fill.intensity = 0.45f; fill.color = new Color(0.62f, 0.55f, 1f);
            fill.transform.rotation = Quaternion.Euler(-20, 150, 0); fill.shadows = LightShadows.None;

            // ---- camera
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 42f; cam.nearClipPlane = 0.2f; cam.farClipPlane = 900f; cam.clearFlags = CameraClearFlags.Skybox;
            DreamLookSetup.ConfigureCamera(cam);
            DreamLookSetup.AddVolume(root, dream: true);

            // ---- planets
            var gs = new GameObject("GalaxySelect").AddComponent<GalaxySelect>();
            gs.transform.SetParent(root); gs.cam = cam;
            Planet(gs, root, "PLANET_Book", "W1", "THE DREAM BOOK", "ด่านแรก  ·  บอส: THE CHAIR",
                "◆  CH0  ร่วงลงสู่ฝัน\n◆  CH1  หนังสือป๊อปอัป\n◆  CH2  โรงละครกระดาษ 2.5D\n◆  CH3  กาแล็กซีของเล่น\n◆  CH4  ไล่ล่าบนชั้นหนังสือ\n◆  CH5  วิหารบัลลังก์ — <color=#E5293D>THE CHAIR</color>",
                "Dream_World1", false, new Vector3(0, 0, 0), 1.0f, ring: true);
            Planet(gs, root, "PLANET_Dreamland", "W2", "MIDNIGHT PALACE", "OPEN WORLD  ·  บอส: <color=#00E1FF>COUNT MIDNIGHT</color>",
                "◆  เมืองกลางคืนที่เวลาหยุดตอน 11:59 — 4 ย่าน 4 สี\n◆  ตามหา STOLEN HOURS 3 ชิ้น: วิ่งหลังคา · เฟืองหมุน · ซุ่มโจมตี\n◆  ต่อสู้แบบใหม่: ล็อกเป้า · ต่อยคอมโบ · กลิ้งหลบ · ไถราง\n◆  ปีนหอนาฬิกาขึ้นไปหาหน้าปัดบนฟ้า",
                "Dream_MidnightPalace", false, new Vector3(30, 5, 18), 1.0f);
            Planet(gs, root, "PLANET_Toy", "W3", "TOY BOX MOON", "เร็ว ๆ นี้", "◆  ตึกบล็อกไม้ที่ล้มเองได้\n◆  ลูกข่างยักษ์\n◆  บอส: ???", "", true, new Vector3(-62, 12, 70), 0.9f);
            Planet(gs, root, "PLANET_Bath", "W4", "BUBBLE BATH SEA", "เร็ว ๆ นี้", "◆  ทะเลฟองสบู่\n◆  เป็ดยางยักษ์\n◆  บอส: ???", "", true, new Vector3(-28, -4, 26), 0.9f);
            Planet(gs, root, "PLANET_Kitchen", "W5", "MIDNIGHT TEACUP", "เร็ว ๆ นี้", "◆  ถ้วยชาหมุนติ้ว\n◆  ช้อนกระโดด\n◆  บอส: ???", "", true, new Vector3(6, 9, 52), 0.85f);

            // ---- Pip on the pillow ship
            var ship = new GameObject("PillowShip").transform; ship.SetParent(root);
            var pillow = ArtKit.Prop(ship, "PROP_PillowShip", Vector3.zero, 0, null, outline: true);
            if (playerPrefab != null)
            {
                var apple = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, ship);
                PrefabUtility.UnpackPrefabInstance(apple, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                apple.name = "Pip";
                // strip gameplay: dependants first (RequireComponent), so walk the list backwards a few times
                for (int pass = 0; pass < 4; pass++)
                {
                    var mbs = apple.GetComponentsInChildren<MonoBehaviour>(true);
                    for (int i = mbs.Length - 1; i >= 0; i--)
                        if (mbs[i] != null && !(mbs[i] is Player.AppleFace)) Object.DestroyImmediate(mbs[i], true);
                }
                foreach (var c in apple.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var cc = apple.GetComponent<CharacterController>(); if (cc != null) Object.DestroyImmediate(cc);
                foreach (var t in apple.GetComponentsInChildren<Transform>(true)) if (t.name.ToLower().Contains("shadow")) t.gameObject.SetActive(false);
                apple.transform.localPosition = new Vector3(0, 0.2f, 0); apple.transform.localScale = Vector3.one * 0.85f;
            }
            ship.position = new Vector3(0, 0, -11);
            gs.ship = ship;

            // ---- decoration: big ring of drifting star bits, distant small stars, sparkle dust
            var rnd = new System.Random(5);
            for (int i = 0; i < 26; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, rr = 14f + (float)rnd.NextDouble() * 60f;
                var p = new Vector3(Mathf.Cos(a) * rr, -12f + (float)rnd.NextDouble() * 30f, 20f + Mathf.Sin(a) * rr);
                var st = ArtKit.Prop(root, "PROP_GalStar", p, (float)rnd.NextDouble() * 360f, Vector3.one * (0.8f + (float)rnd.NextDouble() * 2.2f));
                if (st != null) { st.name = "StarBit_" + i; st.gameObject.AddComponent<Spinner>().speed = 20f + (float)rnd.NextDouble() * 40f; }
            }
            Dust(root, cam.transform);

            MovementGymBuilder.AddToBuildSettings(ScenePath);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[DreamRush] Dream Galaxy built: " + ScenePath);
        }

        static void Planet(GalaxySelect gs, Transform root, string prop, string id, string title, string sub, string chapters, string sceneName, bool locked, Vector3 pos, float scale, bool ring = false)
        {
            var holder = new GameObject(id + "_" + prop).transform; holder.SetParent(root); holder.position = pos;
            var spin = new GameObject("Spin").transform; spin.SetParent(holder, false);
            // Blender Z-up planets import upright (bake_space_transform); tilt slightly for drama
            var model = ArtKit.Prop(spin, prop, Vector3.zero, 0, Vector3.one * scale, outline: true);
            spin.localRotation = Quaternion.Euler(-8f, 0, 6f);
            if (ring)
            {
                var r = ArtKit.Prop(holder, "PROP_GalRing", Vector3.zero, 0, Vector3.one * 8.5f * scale);
                if (r != null) { r.localRotation = Quaternion.Euler(14f, 0, -10f); r.gameObject.AddComponent<Spinner>().speed = -8f; }
            }
            if (locked)
            {
                // locked worlds are dimmed with a lavender veil so World 1 is clearly "the one"
                foreach (var mr in model.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = LockedMat(mats[i]);
                    mr.sharedMaterials = mats;
                }
            }
            var gp = holder.gameObject.AddComponent<GalaxyPlanet>();
            gp.worldId = id; gp.title = title; gp.subtitle = sub; gp.chapters = chapters; gp.sceneName = sceneName; gp.locked = locked;
            gp.spinSpeed = locked ? 4f : 7f;
            // camera frames the planet left of centre (UI panel lives on the right)
            var camPose = new GameObject("CamPose").transform; camPose.SetParent(root);
            Vector3 eye = pos + new Vector3(9f, 6f, -19f) * scale;
            camPose.position = eye;
            camPose.rotation = Quaternion.LookRotation(pos + new Vector3(5.5f, 1.4f, 0) * scale - eye);
            gp.cameraPose = camPose;
            var shipPt = new GameObject("ShipPoint").transform; shipPt.SetParent(root);
            shipPt.position = pos + new Vector3(4.8f, 3.0f, -7.5f) * scale;
            gp.shipPoint = shipPt;
            gs.planets.Add(gp);
        }

        static Material LockedMat(Material src)
        {
            if (src == null) return null;
            string path = $"{ArtKit.MatDir}/{src.name}_Locked.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src) { name = src.name + "_Locked" }; AssetDatabase.CreateAsset(m, path); }
            m.CopyPropertiesFromMaterial(src);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(src.GetColor("_BaseColor"), new Color(0.42f, 0.36f, 0.62f), 0.65f));
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Dust(Transform root, Transform cam)
        {
            var go = new GameObject("DustMotes"); go.transform.SetParent(root); go.transform.position = new Vector3(0, 2, 10);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = 9f; main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.28f); main.maxParticles = 600;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.7f, 0.8f), new Color(0.75f, 0.7f, 1f, 0.8f));
            var em = ps.emission; em.rateOverTime = 50f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(110, 50, 90);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var lib = AssetDatabase.LoadAssetAtPath<Presentation.VFXLibrary>("Assets/DreamRush/Resources/" + Presentation.VFXLibrary.ResourcePath + ".asset");
            if (lib != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = lib.glowAdd;
            ps.Play();
        }
    }
}
