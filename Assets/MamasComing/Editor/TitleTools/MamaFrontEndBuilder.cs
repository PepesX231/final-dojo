using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using K = MamasComing.MamaUIKit;

namespace MamasComing.Editor
{
    /// Authors the title + lobby screens as normal scene objects under "FrontEnd" in the open scene.
    /// Only adds parts that are missing - it never moves or overwrites what you already adjusted.
    public static class MamaFrontEndBuilder
    {
        const string Dir = "Assets/MamasComing/FrontEnd";
        const string ScenesDir = "Assets/MamasComing/Scenes";
        static string MenuPath => ScenesDir + "/" + MamaLaunch.MenuScene + ".unity";
        static string LobbyPath => ScenesDir + "/" + MamaLaunch.LobbyScene + ".unity";
        const string KitchenPath = "Assets/MamasComing/Scenes/MamasKitchen.unity";
        static readonly string[] SetRoots = { "Sun", "Rooms", "House", "Props", "Outside", "Global Volume" };

        static GameObject appleSrc, mopSrc;

        /// Creates MC_MainMenu + MC_Lobby (separate scenes, gameplay scene untouched) if they don't exist yet,
        /// and puts them first in Build Settings. Existing menu scenes are never overwritten.
        [MenuItem("Mama's Coming/Front End/Create menu + lobby scenes (keeps existing)")]
        public static void BuildScenes() => BuildScenesImpl(false);

        /// Deletes and recreates both menu scenes from these defaults (your edits in them are lost).
        public static void RebuildScenes() => BuildScenesImpl(true);

        static void BuildScenesImpl(bool force)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[FrontEnd] stop Play mode first"); return; }
            var kitchen = EditorSceneManager.GetActiveScene();
            if (kitchen.path != KitchenPath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                kitchen = EditorSceneManager.OpenScene(KitchenPath, OpenSceneMode.Single);
            }
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/MamasComing", "FrontEnd");

            // 1) shared assets from the gameplay scene (copies; the gameplay scene itself is not modified)
            appleSrc = SaveApple();
            var set = SaveHouseSet();
            var camSrc = GameObject.Find("Main Camera");
            var rs = CaptureRender();

            // 2) the two scenes
            if (force || !System.IO.File.Exists(MenuPath)) MakeScene(MenuPath, set, camSrc, rs, true);
            if (force || !System.IO.File.Exists(LobbyPath)) MakeScene(LobbyPath, set, camSrc, rs, false);
            EditorSceneManager.SetActiveScene(kitchen);

            // 3) build order: menu, lobby, then the rest
            var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuPath, true), new EditorBuildSettingsScene(LobbyPath, true) };
            foreach (var b in EditorBuildSettings.scenes) if (b.path != MenuPath && b.path != LobbyPath) list.Add(b);
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[FrontEnd] menu scenes ready: " + MenuPath + ", " + LobbyPath);
        }

        static GameObject Strip(GameObject go)
        {
            for (int pass = 0; pass < 3; pass++)
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (!mb) continue;
                    var ns = mb.GetType().Namespace ?? "";
                    if (ns.StartsWith("MamasComing") || ns.StartsWith("DreamRush") || ns.StartsWith("Unity.Netcode")) { try { Object.DestroyImmediate(mb); } catch { } }
                }
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) { rb.isKinematic = true; rb.useGravity = false; }
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.playOnAwake = false; }
            foreach (var a in go.GetComponentsInChildren<AudioSource>(true)) a.playOnAwake = false;
            return go;
        }

        static GameObject SaveApple()
        {
            string path = Dir + "/PF_MenuApple.prefab";
            Transform chr = null;
            foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var t in pc.GetComponentsInChildren<Transform>(true)) if (t.name == "CHR_AppleChild") { chr = t; break; }
            if (!chr) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var copy = Strip(Object.Instantiate(chr.gameObject));
            copy.name = "CHR_AppleChild"; copy.transform.position = Vector3.zero; copy.transform.rotation = Quaternion.identity; copy.transform.localScale = chr.lossyScale;
            var pf = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);
            return pf;
        }

        static GameObject SaveHouseSet()
        {
            string path = Dir + "/PF_MenuHouseSet.prefab";
            var root = new GameObject("MenuHouseSet (copy of the house - edit freely)");
            foreach (var n in SetRoots)
            {
                var src = GameObject.Find(n);
                if (!src || src.transform.parent) continue;
                var c = Object.Instantiate(src, root.transform); c.name = n;
                Strip(c);
            }
            var pf = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return pf;
        }

        class RS { public Material sky; public UnityEngine.Rendering.AmbientMode mode; public Color sky1, eq, gr, amb; public float ambI, refl; public bool fog; public Color fogC; public FogMode fogM; public float fogD, fogS, fogE; public LightingSettings lighting; }
        static RS CaptureRender()
        {
            var r = new RS { sky = RenderSettings.skybox, mode = RenderSettings.ambientMode, sky1 = RenderSettings.ambientSkyColor, eq = RenderSettings.ambientEquatorColor, gr = RenderSettings.ambientGroundColor, amb = RenderSettings.ambientLight, ambI = RenderSettings.ambientIntensity, refl = RenderSettings.reflectionIntensity, fog = RenderSettings.fog, fogC = RenderSettings.fogColor, fogM = RenderSettings.fogMode, fogD = RenderSettings.fogDensity, fogS = RenderSettings.fogStartDistance, fogE = RenderSettings.fogEndDistance };
            try { r.lighting = Lightmapping.lightingSettings; } catch { }
            return r;
        }
        static void ApplyRender(RS r)
        {
            RenderSettings.skybox = r.sky; RenderSettings.ambientMode = r.mode; RenderSettings.ambientSkyColor = r.sky1; RenderSettings.ambientEquatorColor = r.eq; RenderSettings.ambientGroundColor = r.gr; RenderSettings.ambientLight = r.amb; RenderSettings.ambientIntensity = r.ambI; RenderSettings.reflectionIntensity = r.refl;
            RenderSettings.fog = r.fog; RenderSettings.fogColor = r.fogC; RenderSettings.fogMode = r.fogM; RenderSettings.fogDensity = r.fogD; RenderSettings.fogStartDistance = r.fogS; RenderSettings.fogEndDistance = r.fogE;
            if (r.lighting) try { Lightmapping.lightingSettings = r.lighting; } catch { }
        }

        static void MakeScene(string path, GameObject set, GameObject camSrc, RS rs, bool title)
        {
            if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(sc);
            ApplyRender(rs);
            var house = (GameObject)PrefabUtility.InstantiatePrefab(set, sc);
            var sun = house.transform.Find("Sun"); if (sun) RenderSettings.sun = sun.GetComponent<Light>();
            if (camSrc)
            {
                var cam = Strip(Object.Instantiate(camSrc)); cam.name = "Main Camera"; cam.tag = "MainCamera";
            }
            var mop = house.transform.Find("Props/Mop"); mopSrc = mop ? mop.gameObject : null;
            var rootGO = new GameObject("FrontEnd");
            var fe = rootGO.AddComponent<MamaFrontEnd>();
            rootGO.AddComponent<MamaThemeSkip>();
            rootGO.AddComponent<MamaHowToPlay>();
            if (title) { fe.screen = MamaFrontEnd.Screen.Title; fe.title = BuildTitle(rootGO.transform); }
            else { fe.screen = MamaFrontEnd.Screen.Lobby; fe.lobby = BuildLobby(rootGO.transform); }
            EditorSceneManager.SaveScene(sc, path);
            EditorSceneManager.CloseScene(sc, true);
        }

        [MenuItem("Mama's Coming/Front End/Rebuild kid models in open scene")]
        public static void RebuildKids()
        {
            foreach (var k in Object.FindObjectsByType<MamaPosedKid>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { k.Rebuild(); EditorUtility.SetDirty(k); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        static Font Disp => MamaTheme.Display;
        static Font Bold => MamaTheme.Bold;
        static Color Yellow => new Color(1f, 0.87f, 0.45f);

        static MamaShot Shot(Transform parent, string name, Vector3 pos, Vector3 euler, float fov)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false);
            t.SetPositionAndRotation(pos, Quaternion.Euler(euler));
            var s = t.gameObject.AddComponent<MamaShot>(); s.fov = fov; return s;
        }

        static MamaPosedKid Kid(Transform parent, string name, int id, Vector3 pos, float yaw, MamaPosedKid.Pose pose, float phase, bool hat, Color hatCol, string en, string th, float scale = 1f)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); go.transform.localScale = Vector3.one * scale;
            var k = go.AddComponent<MamaPosedKid>();
            k.kid = id; k.pose = pose; k.phase = phase; k.partyHat = hat; k.hatColor = hatCol; k.bubbleEn = en; k.bubbleTh = th;
            k.appleSource = appleSrc;
            k.Rebuild();
            return k;
        }

        static float FaceCam(Vector3 from, Vector3 cam) { var d = cam - from; return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg; }

        // ------------------------------------------------------------------------------------------------ TITLE
        /// Point in front of a shot: d metres forward, r metres right (horizontal), at height y.
        static Vector3 At(Transform shot, float d, float r, float y)
        {
            var f = shot.forward; f.y = 0; f.Normalize(); var rt = new Vector3(f.z, 0, -f.x);
            var p = shot.position + f * d + rt * r; p.y = y; return p;
        }
        static Quaternion Facing(Transform shot, Vector3 at, float roll = 0f, float pitch = 0f)
        {
            var d = at - shot.position; d.y = 0;
            return Quaternion.LookRotation(d.normalized, Vector3.up) * Quaternion.Euler(pitch, 0, roll);
        }

        /// World-space canvas: `size` in canvas units, `metresWide` sets the scale.
        static Canvas WorldCanvas(string name, Transform parent, Vector3 pos, Quaternion rot, Vector2 size, float metresWide)
        {
            var c = K.Canvas(name, parent, 60);
            c.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)c.transform; rt.sizeDelta = size; rt.position = pos; rt.rotation = rot; rt.localScale = Vector3.one * (metresWide / size.x);
            var sc = c.GetComponent<CanvasScaler>(); sc.dynamicPixelsPerUnit = 3f; sc.referencePixelsPerUnit = 100f;
            return c;
        }

        /// Lit material using the hand-painted plank texture (saved as an asset so it is easy to tweak).
        static Material SignMaterial()
        {
            string path = Dir + "/M_MenuSignWood.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            Material lit = null;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name == "Universal Render Pipeline/Lit") { lit = r.sharedMaterial; break; }
            m = lit ? new Material(lit) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "M_MenuSignWood";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/tex_menu_wood.png");
            if (tex) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
            m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static Material FindMaterial(params string[] names)
        {
            foreach (var n in names)
                foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if (r.name == n && r.sharedMaterial) return r.sharedMaterial;
            return null;
        }

        static MamaShot FocusShot(Transform parent, string name, MamaShot main, Vector3 move, Vector3 turn, float fov)
        {
            var t = main.transform;
            var s = Shot(parent, name, t.position + t.right * move.x + Vector3.up * move.y + t.forward * move.z, (t.rotation * Quaternion.Euler(turn)).eulerAngles, fov);
            s.sway = main.sway; return s;
        }

        static MamaTitleScreen BuildTitle(Transform root)
        {
            var rig = new GameObject("TitleRig").transform; rig.SetParent(root, false);
            var ts = rig.gameObject.AddComponent<MamaTitleScreen>();
            ts.shot = Shot(rig, "TitleShot (main camera pose)", new Vector3(1.1f, 1.15f, 5.75f), new Vector3(4f, 143f, 0f), 46f);
            var sh = ts.shot.transform;
            var camPos = sh.position;

            // ---- the kids, in front of the sunset window; the apple strikes a pose on the coffee table
            var kidsRoot = new GameObject("Kids (move / pose me)").transform; kidsRoot.SetParent(rig, false);
            Vector3 apple = new Vector3(2.62f, 0.455f, 2.8f);   // standing on the coffee table
            Vector3 orange = At(sh, 4.6f, 1.9f, 0f), banana = At(sh, 4.6f, -0.1f, 0f), wax = At(sh, 5.6f, -0.4f, 0f), melon = At(sh, 3.7f, 1.45f, 0f);
            var appleKid = Kid(kidsRoot, "Kid_Apple", KidLook.Apple, apple, FaceCam(apple, camPos) + 12f, MamaPosedKid.Pose.Hero, 0f, true, new Color(1f, .42f, .58f), "LEAVE IT TO ME!", "ปล่อยให้ข้าจัดการ!");
            ts.kids = new[]
            {
                appleKid,
                Kid(kidsRoot, "Kid_Orange", KidLook.Orange, orange, FaceCam(orange, camPos) + 15f, MamaPosedKid.Pose.Hop, 0.7f, true, new Color(.3f, .75f, .8f), "PARTY!!", "ปาร์ตี้!!"),
                Kid(kidsRoot, "Kid_Banana", KidLook.Banana, banana, FaceCam(banana, camPos) - 15f, MamaPosedKid.Pose.FingerGuns, 1.9f, false, Color.white, "chill...", "ชิลๆ..."),
                Kid(kidsRoot, "Kid_WaxApple", KidLook.WaxApple, wax, FaceCam(wax, camPos) + 70f, MamaPosedKid.Pose.Sneak, 2.4f, true, new Color(.95f, .8f, .3f), "shh...", "ชู่ว..."),
                Kid(kidsRoot, "Kid_Watermelon", KidLook.Watermelon, melon, FaceCam(melon, camPos) + 100f, MamaPosedKid.Pose.Asleep, 0.3f, false, Color.white, "zzz", "คร่อก..."),
            };
            // the mop from the house becomes the apple's "sword"
            if (mopSrc)
            {
                var mop = Object.Instantiate(mopSrc, appleKid.transform); mop.name = "Hand prop (mop)";
                foreach (var col in mop.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                foreach (var rb in mop.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
                appleKid.handProp = mop.transform; appleKid.propOffset = new Vector3(0.06f, -1.8f, 0.06f); appleKid.propEuler = Vector3.zero;
            }

            // ---- speech bubbles: small world-space canvases over each kid (billboarded)
            var bubbles = new List<RectTransform>();
            foreach (var k in ts.kids)
            {
                if (string.IsNullOrEmpty(k.bubbleEn)) continue;
                var b = MamaPosedKid.Bounds(k.Model ? k.Model : k.transform);
                bool shout = k.pose == MamaPosedKid.Pose.Hop || k.pose == MamaPosedKid.Pose.Hero;
                var bc = WorldCanvas("Bubble", k.transform, new Vector3(b.center.x, b.max.y + 0.12f, b.center.z), Quaternion.identity, shout ? new Vector2(420, 230) : new Vector2(320, 160), shout ? 0.62f : 0.46f);
                var bb = bc.gameObject.AddComponent<MamaBillboard>(); bb.follow = k;
                var br = (RectTransform)bc.transform; br.pivot = new Vector2(0.15f, 0f);
                var img = K.Stretch("Shape", br);
                K.Img(img, shout ? MamaTheme.BurstS : MamaTheme.BubbleS, k.pose == MamaPosedKid.Pose.Hop ? MamaTheme.Mustard : Color.white);
                K.Txt(K.Rect("Text", br, K.CC, K.CC, shout ? new Vector2(-6, 4) : new Vector2(0, 18), shout ? new Vector2(330, 120) : new Vector2(280, 90)), k.bubbleEn, k.bubbleTh, shout ? Disp : Bold, shout ? 44 : 40, MamaTheme.WoodInk);
                bubbles.Add(br);
            }
            ts.bubbles = bubbles.ToArray();

            // ---- logo: a hand-made party banner hanging in the room (upper-left of the shot)
            Vector3 logoAt = At(sh, 5.6f, -1.55f, 2.15f);
            var logoC = WorldCanvas("Logo banner (3D)", rig, logoAt, Facing(sh, logoAt, 3f), new Vector2(2300, 520), 2.9f);
            var lr = (RectTransform)logoC.transform; ts.logo = lr;
            var paper = K.Stretch("Paper", lr); K.Img(paper, MamaTheme.TornS, Color.white, 60f);
            K.Txt(K.Rect("Title", lr, K.CC, K.CC, new Vector2(0, 50), new Vector2(2200, 300)), "MAMA'S COMING!", null, Disp, 250, MamaTheme.Brick, TextAnchor.MiddleCenter, 8f, MamaTheme.WoodInk);
            K.Txt(K.Rect("Tag", lr, K.BC, K.BC, new Vector2(0, 70), new Vector2(2000, 90)), "fix it or hide it. your call.", "ซ่อมจริง หรือแค่ซ่อน เลือกเอง", Bold, 74, MamaTheme.Teal, TextAnchor.MiddleCenter);
            MamaTheme.Tape(lr, new Vector2(0.06f, 1f), new Vector2(0, -20), 30f, 300f);
            MamaTheme.Tape(lr, new Vector2(0.94f, 1f), new Vector2(0, -20), -26f, 300f);
            for (int i = -1; i <= 1; i += 2)
            {
                var str = K.Rect("String", lr, new Vector2(0.5f + i * 0.44f, 1f), new Vector2(0.5f, 0f), new Vector2(0, -10), new Vector2(8, 900), i * -6f);
                K.Img(str, null, new Color(0.25f, 0.16f, 0.1f, 0.9f));
            }

            // ---- the menu: a 3D signpost standing in the room; every sign is a clickable object
            var woodMat = SignMaterial();
            var postMat = FindMaterial("ConsoleLeg", "Leg", "LampPole");
            Vector3 postAt = At(sh, 2.75f, -0.95f, 0f);
            var post = new GameObject("Menu signpost (3D)").transform; post.SetParent(rig, false);
            post.SetPositionAndRotation(postAt, Facing(sh, postAt));
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pole.name = "Pole"; pole.transform.SetParent(post, false);
            pole.transform.localPosition = new Vector3(0, 0.72f, 0.06f); pole.transform.localScale = new Vector3(0.07f, 0.72f, 0.07f);
            Object.DestroyImmediate(pole.GetComponent<Collider>()); if (postMat) pole.GetComponent<Renderer>().sharedMaterial = postMat;
            var foot = GameObject.CreatePrimitive(PrimitiveType.Cube); foot.name = "Foot"; foot.transform.SetParent(post, false);
            foot.transform.localPosition = new Vector3(0, 0.03f, 0.06f); foot.transform.localScale = new Vector3(0.36f, 0.06f, 0.36f);
            Object.DestroyImmediate(foot.GetComponent<Collider>()); if (woodMat) foot.GetComponent<Renderer>().sharedMaterial = woodMat;
            var items = new List<MamaMenuItem>();
            var focusRoot = new GameObject("Item camera shots").transform; focusRoot.SetParent(rig, false);
            // top -> bottom; y = height on the post, x = sideways shift, yaw/roll = how each sign is nailed on
            (MamaMenuItem.Action a, string en, string th, float y, float x, float w, float h, float yaw, float roll, Vector3 move, Vector3 turn, float fov)[] defs =
            {
                (MamaMenuItem.Action.Play, "PLAY", "เล่นเลย!", 1.30f, 0.10f, 0.86f, 0.30f, -10f, 4f, new Vector3(0.06f, 0f, 0.1f), new Vector3(-1f, 1.5f, 0f), 45f),
                (MamaMenuItem.Action.Coop, "CO-OP", "เล่นกับเพื่อน", 1.02f, -0.12f, 0.80f, 0.23f, 12f, -3f, new Vector3(0.05f, 0f, 0.05f), new Vector3(0f, 3.5f, -2f), 45f),
                (MamaMenuItem.Action.HowTo, "HOW TO PLAY", "วิธีเล่น", 0.78f, 0.08f, 0.92f, 0.21f, -6f, 2f, new Vector3(0f, 0.05f, 0.05f), new Vector3(2.5f, 1.5f, 1f), 45f),
                (MamaMenuItem.Action.Options, "OPTIONS", "ตั้งค่า", 0.55f, -0.08f, 0.72f, 0.20f, 9f, -2.5f, new Vector3(0f, -0.1f, 0f), new Vector3(-2f, -2.5f, -1.5f), 46f),
                (MamaMenuItem.Action.Quit, "QUIT", "ออก", 0.33f, 0.05f, 0.50f, 0.18f, -4f, 5f, new Vector3(-0.08f, 0f, 0f), new Vector3(1f, -4f, 2.5f), 47f),
            };
            foreach (var d in defs)
            {
                var it = new GameObject("Sign_" + d.a).transform; it.SetParent(post, false);
                it.localPosition = new Vector3(d.x, d.y, 0f); it.localRotation = Quaternion.Euler(0, d.yaw, d.roll);
                var sign = it.gameObject.AddComponent<MamaSign3D>();
                sign.en = d.en; sign.th = d.th; sign.latinFont = Disp; sign.thaiFont = Bold; sign.plank = new Vector3(d.w, d.h, 0.06f);
                sign.Build();
                if (woodMat) it.Find("Plank").GetComponent<Renderer>().sharedMaterial = woodMat;
                var col = it.gameObject.AddComponent<BoxCollider>(); col.size = sign.plank + new Vector3(0.02f, 0.02f, 0.04f);
                var mi = it.gameObject.AddComponent<MamaMenuItem>(); mi.action = d.a; mi.sign = sign; mi.selected = new Color(1f, 0.83f, 0.25f);
                mi.focus = FocusShot(focusRoot, "Shot_" + d.a, ts.shot, d.move, d.turn, d.fov);
                items.Add(mi);
            }
            ts.items = items.ToArray(); ts.startItem = 0;


            // ---- flat overlay: control hints + status (kept on screen for readability)
            var hud = K.Canvas("TitleHUD (screen)", rig, 70); var c = hud.transform;
            var st = K.Rect("StatusTag", c, K.TR, new Vector2(1, 1), new Vector2(-56, -50), new Vector2(520, 76), -2f);
            K.Img(st, MamaTheme.PaintS, MamaUI.Paper, 18f);
            MamaTheme.Tape(st, new Vector2(0.5f, 1f), new Vector2(0, -2), 4f, 120f);
            ts.status = K.Txt(K.Rect("Text", st, K.CC, K.CC, Vector2.zero, new Vector2(480, 60)), "SOLO · 1-3 PLAYERS", null, Bold, 26, MamaTheme.WoodInk);
            var hints = K.Rect("Hints", c, K.BL, new Vector2(0, 0), new Vector2(64, 40), new Vector2(1100, 132));
            K.Label("Navigate", hints, K.TL, new Vector2(0, 1), Vector2.zero, new Vector2(1100, 40), "NAVIGATION:  W / S  ·  ↑ / ↓  ·  MOUSE  ·  D-PAD", "เลือก:  W / S  ·  ↑ / ↓  ·  เมาส์  ·  ปุ่มลูกศรจอย", Bold, 26, MamaTheme.Pink, TextAnchor.MiddleLeft, 2f);
            K.Label("Confirm", hints, K.TL, new Vector2(0, 1), new Vector2(0, -42), new Vector2(1100, 40), "CONFIRM:  ENTER  ·  SPACE  ·  E  ·  CLICK  ·  (A)", "ยืนยัน:  ENTER  ·  SPACE  ·  E  ·  คลิก  ·  (A)", Bold, 26, MamaTheme.Pink, TextAnchor.MiddleLeft, 2f);
            K.Label("Back", hints, K.TL, new Vector2(0, 1), new Vector2(0, -84), new Vector2(1100, 40), "OPTIONS:  ESC  ·  (B)", "ตั้งค่า:  ESC  ·  (B)", Bold, 26, MamaTheme.Pink, TextAnchor.MiddleLeft, 2f);
            ts.canvas = hud;
            ts.worldCanvases = new[] { logoC };
            return ts;
        }

        // ------------------------------------------------------------------------------------------------ LOBBY
        static MamaLobbyScreen BuildLobby(Transform root)
        {
            var rig = new GameObject("LobbyRig").transform; rig.SetParent(root, false);
            var ls = rig.gameObject.AddComponent<MamaLobbyScreen>();
            Vector3 camPos = new Vector3(3.15f, 1.3f, 3.0f);
            ls.shot = Shot(rig, "LobbyShot (main camera pose)", camPos, new Vector3(15f, 90f, 0f), 56f);
            var seatsRoot = new GameObject("Seats (on the sofa)").transform; seatsRoot.SetParent(rig, false);
            ls.seats = new[]
            {
                Kid(seatsRoot, "Seat_P1", KidLook.Apple, new Vector3(5.5f, 0.6f, 3.0f), -90f, MamaPosedKid.Pose.Wave, 0f, false, Color.white, null, null),
                Kid(seatsRoot, "Seat_P2", KidLook.Banana, new Vector3(5.5f, 0.6f, 2.25f), -100f, MamaPosedKid.Pose.Sit, 1.1f, false, Color.white, null, null),
                Kid(seatsRoot, "Seat_P3", KidLook.Orange, new Vector3(5.5f, 0.6f, 3.7f), -80f, MamaPosedKid.Pose.SitBounce, 2.2f, false, Color.white, null, null),
            };

            foreach (var st in ls.seats) { st.fitToSeat = true; st.backClear = 0.36f; }
            var canvas = K.Canvas("LobbyCanvas", rig, 70); ls.canvas = canvas; var c = canvas.transform;
            var buttons = new List<MamaMenuItem>();

            // left icon rail
            var rail = K.Rect("LeftRail", c, K.TL, new Vector2(0, 1), new Vector2(44, -60), new Vector2(150, 560));
            (MamaMenuItem.Action a, string glyph, string en, string th)[] icons =
            {
                (MamaMenuItem.Action.Back, "<", "BACK", "กลับ"),
                (MamaMenuItem.Action.HowTo, "?", "HOW TO", "วิธีเล่น"),
                (MamaMenuItem.Action.Options, "", "OPTIONS", "ตั้งค่า"),
            };
            for (int i = 0; i < icons.Length; i++)
            {
                var b = K.Rect("Icon_" + icons[i].a, rail, K.TL, new Vector2(0, 1), new Vector2(0, -i * 175), new Vector2(124, 124), i % 2 == 0 ? -2f : 2f);
                var bg = K.Img(b, MamaTheme.PaintS, new Color(0.30f, 0.20f, 0.14f), 22f, true);
                if (icons[i].a == MamaMenuItem.Action.Options) K.Img(K.Rect("Gear", b, K.CC, K.CC, Vector2.zero, new Vector2(86, 86)), MamaTheme.GearS, MamaTheme.Mustard);
                var lbl = K.Txt(K.Rect("Glyph", b, K.CC, K.CC, new Vector2(0, 2), new Vector2(110, 110)), icons[i].glyph, null, Disp, 72, MamaTheme.Cream, TextAnchor.MiddleCenter, 3f);
                K.Txt(K.Rect("Caption", b, K.BC, new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(170, 34)), icons[i].en, icons[i].th, Bold, 22, MamaTheme.Cream, TextAnchor.MiddleCenter, 2f);
                var mi = b.gameObject.AddComponent<MamaMenuItem>(); mi.action = icons[i].a; mi.label = lbl; mi.selected = MamaTheme.Pink; mi.normal = MamaTheme.Cream;
                buttons.Add(mi);
            }

            // name tags over the seats
            var tags = K.Stretch("NameTags", c);
            ls.tagNames = new Text[ls.seats.Length]; ls.tagFruits = new Text[ls.seats.Length];
            for (int i = 0; i < ls.seats.Length; i++)
            {
                var t = K.Rect("Tag_P" + (i + 1), tags, K.CC, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(380, 130));
                ls.tagNames[i] = K.Txt(K.Rect("Name", t, K.TC, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(380, 56)), "P" + (i + 1), null, Disp, 46, MamaTheme.Cream, TextAnchor.MiddleCenter, 3f);
                ls.tagFruits[i] = K.Txt(K.Rect("Fruit", t, K.TC, new Vector2(0.5f, 1f), new Vector2(0, -54), new Vector2(380, 34)), "APPLE", null, Bold, 26, MamaTheme.Cream, TextAnchor.MiddleCenter, 2f);
                K.Txt(K.Rect("Chevron", t, K.BC, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(80, 56)), "▼", null, Bold, 46, MamaTheme.Pink, TextAnchor.MiddleCenter, 2f);
                var wa = t.gameObject.AddComponent<MamaWorldAnchor>(); wa.kid = ls.seats[i]; wa.worldOffset = new Vector3(0, -0.14f, 0); wa.hideWhenKidHidden = false;
            }

            // room info (top-right)
            var room = K.Rect("RoomPanel", c, K.TR, new Vector2(1, 1), new Vector2(-50, -44), new Vector2(620, 140), -1f);
            K.Img(room, MamaTheme.PlankDarkS, Color.white, 30f);
            K.Label("RoomLabel", room, K.TL, new Vector2(0, 1), new Vector2(46, -18), new Vector2(380, 32), "ROOM CODE", "รหัสห้อง", Bold, 24, MamaTheme.Cream, TextAnchor.MiddleLeft);
            ls.roomCode = K.Label("Code", room, K.TL, new Vector2(0, 1), new Vector2(46, -48), new Vector2(400, 70), "NO ROOM YET", null, Disp, 50, Yellow, TextAnchor.MiddleLeft, 3f);
            var copy = K.Rect("Copy", room, K.BL, new Vector2(0, 1), new Vector2(330, -14), new Vector2(130, 46));
            K.Img(copy, MamaTheme.PaintS, MamaTheme.Mustard, 14f, true);
            ls.copyLabel = K.Txt(K.Rect("Label", copy, K.CC, K.CC, Vector2.zero, new Vector2(124, 40)), "COPY", null, Bold, 22, MamaTheme.WoodInk);
            ls.copyButton = copy.gameObject.AddComponent<Button>();
            var hex = K.Rect("Players", room, K.MR, new Vector2(0.5f, 0.5f), new Vector2(-90, 4), new Vector2(140, 140));
            K.Img(hex, MamaTheme.HexS, MamaTheme.Teal);
            ls.playerCount = K.Txt(K.Rect("Count", hex, K.CC, K.CC, new Vector2(-3, 0), new Vector2(120, 70)), "1/3", null, Disp, 44, Color.white, TextAnchor.MiddleCenter, 3f);
            ls.countPips = new Image[3];
            for (int i = 0; i < 3; i++) ls.countPips[i] = K.Img(K.Rect("Pip" + i, room, K.BC, K.CC, new Vector2(-240 + i * 36, -4), new Vector2(30, 30)), MamaTheme.CoinS, MamaTheme.Pink);

            // fruit picker (right)
            var pick = K.Rect("FruitPicker", c, K.TR, new Vector2(1, 1), new Vector2(-50, -210), new Vector2(520, 190), 1.5f);
            K.Img(pick, MamaTheme.PaperS, Color.white, 30f);
            MamaTheme.Tape(pick, new Vector2(0.5f, 1f), new Vector2(0, -4), -5f, 130f);
            K.Label("Caption", pick, K.TC, new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(460, 32), "YOUR FRUIT", "ผลไม้ของคุณ", Bold, 24, MamaTheme.WoodInk);
            ls.fruitName = K.Label("Name", pick, K.CC, K.CC, new Vector2(0, -2), new Vector2(300, 70), "APPLE", null, Disp, 54, MamaTheme.Brick, TextAnchor.MiddleCenter, 0f);
            ls.fruitHint = K.Label("Hint", pick, K.BC, new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(460, 30), "Q / E  change fruit", null, Bold, 22, MamaTheme.WoodInk);
            for (int s = -1; s <= 1; s += 2)
            {
                var ab = K.Rect(s < 0 ? "Prev" : "Next", pick, K.CC, K.CC, new Vector2(s * 200, -2), new Vector2(84, 84));
                K.Img(ab, MamaTheme.HexS, MamaTheme.Mustard, 0f, true);
                var al = K.Txt(K.Rect("Glyph", ab, K.CC, K.CC, new Vector2(-3, 0), new Vector2(80, 80)), s < 0 ? "<" : ">", null, Disp, 50, MamaTheme.WoodInk);
                var mi = ab.gameObject.AddComponent<MamaMenuItem>(); mi.action = s < 0 ? MamaMenuItem.Action.PrevKid : MamaMenuItem.Action.NextKid; mi.label = al; mi.normal = MamaTheme.WoodInk; mi.selected = MamaTheme.Brick;
                buttons.Add(mi);
            }

            // online box (right, under the picker)
            var net = K.Rect("OnlineBox", c, K.BL, new Vector2(0, 0), new Vector2(220, 36), new Vector2(520, 270), 1f);
            K.Img(net, MamaTheme.PaintS, new Color(0.96f, 0.91f, 0.80f), 26f);
            K.Label("Caption", net, K.TC, new Vector2(0.5f, 1f), new Vector2(0, -16), new Vector2(480, 32), "PLAY WITH FRIENDS", "เล่นกับเพื่อน", Bold, 24, MamaTheme.WoodInk);
            buttons.Add(Btn(net, "HostOnline", MamaMenuItem.Action.HostOnline, new Vector2(-122, 44), new Vector2(230, 64), MamaTheme.Teal, "HOST ONLINE", "สร้างห้องออนไลน์"));
            buttons.Add(Btn(net, "HostLan", MamaMenuItem.Action.HostLan, new Vector2(122, 44), new Vector2(230, 64), MamaTheme.Olive, "HOST LAN", "สร้างห้อง LAN"));
            var field = K.Rect("CodeField", net, K.CC, K.CC, new Vector2(-70, -40), new Vector2(330, 62));
            K.Img(field, MamaTheme.PaintS, Color.white, 16f, true);
            var ft = K.Txt(K.Rect("Text", field, K.CC, K.CC, Vector2.zero, new Vector2(300, 50)), "", null, Bold, 28, MamaTheme.WoodInk, TextAnchor.MiddleLeft);
            ft.supportRichText = false;
            var ph = K.Txt(K.Rect("Placeholder", field, K.CC, K.CC, Vector2.zero, new Vector2(300, 50)), "Room code / IP", "รหัสห้อง / IP", Bold, 24, new Color(0.45f, 0.38f, 0.32f), TextAnchor.MiddleLeft);
            var inf = field.gameObject.AddComponent<InputField>(); inf.textComponent = ft; inf.placeholder = ph; inf.characterLimit = 40; ls.codeField = inf;
            buttons.Add(Btn(net, "Join", MamaMenuItem.Action.Join, new Vector2(170, -40), new Vector2(140, 62), MamaTheme.Brick, "JOIN", "เข้าร่วม"));
            ls.statusText = K.Label("Status", net, K.BC, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(480, 54), "", null, Bold, 20, MamaTheme.WoodInk, TextAnchor.MiddleCenter);
            ls.statusText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // start / leave (bottom-right)
            var start = K.Rect("Start", c, K.BR, new Vector2(1, 0), new Vector2(-50, 56), new Vector2(420, 112), -1.5f);
            K.Img(start, MamaTheme.PaintS, MamaTheme.Brick, 26f, true);
            MamaTheme.Tape(start, new Vector2(0.1f, 1f), new Vector2(0, -6), 25f, 110f);
            ls.startLabel = K.Txt(K.Rect("Label", start, K.CC, K.CC, new Vector2(0, 2), new Vector2(400, 100)), "START!", null, Disp, 64, MamaTheme.Cream, TextAnchor.MiddleCenter, 4f);
            var smi = start.gameObject.AddComponent<MamaMenuItem>(); smi.action = MamaMenuItem.Action.Start; smi.label = ls.startLabel; smi.normal = MamaTheme.Cream; smi.selected = Yellow;
            buttons.Add(smi);
            buttons.Add(Btn(c, "Leave", MamaMenuItem.Action.Leave, Vector2.zero, new Vector2(250, 76), new Color(0.3f, 0.2f, 0.14f), "LEAVE ROOM", "ออกจากห้อง", K.BR, new Vector2(-500, 74)));
            K.Label("Hint", c, K.BR, new Vector2(1, 0), new Vector2(-56, 18), new Vector2(900, 34), "ENTER start  ·  Q / E fruit  ·  ESC back", "ENTER เริ่ม  ·  Q / E เปลี่ยนผลไม้  ·  ESC กลับ", Bold, 22, MamaTheme.Pink, TextAnchor.MiddleRight, 2f);

            ls.buttons = buttons.ToArray();
            return ls;
        }

        static MamaMenuItem Btn(Transform parent, string name, MamaMenuItem.Action a, Vector2 pos, Vector2 size, Color col, string en, string th, Vector2? anchor = null, Vector2? at = null)
        {
            var anc = anchor ?? K.CC;
            var b = K.Rect(name, parent, anc, anchor.HasValue ? new Vector2(anc.x, anc.y) : K.CC, at ?? pos, size);
            K.Img(b, MamaTheme.PaintS, col, 18f, true);
            var lbl = K.Txt(K.Rect("Label", b, K.CC, K.CC, Vector2.zero, size - new Vector2(16, 8)), en, th, Bold, 24, Color.white, TextAnchor.MiddleCenter, 2f);
            var mi = b.gameObject.AddComponent<MamaMenuItem>(); mi.action = a; mi.label = lbl; mi.normal = Color.white; mi.selected = new Color(1f, 0.93f, 0.6f);
            return mi;
        }
    }
}
