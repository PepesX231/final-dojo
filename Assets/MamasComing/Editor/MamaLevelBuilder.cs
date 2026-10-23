using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.EditorTools
{
    /// <summary>
    /// Builds the "Mama's Coming!" house: living room, hallway, bedroom, bathroom (flood source),
    /// kitchen (fire source), laundry (valve / extinguisher / cleaning gear) and dining room.
    /// Menu: Mama's Coming > Build House Level. Re-running rebuilds everything.
    /// </summary>
    public static partial class MamaLevelBuilder
    {
        const string Root = "Assets/MamasComing";
        const string MatDir = Root + "/Materials";
        const string TexDir = Root + "/Textures";
        const string ScenePath = Root + "/Scenes/MamasKitchen.unity";
        const string PlayerPrefab = "Assets/DreamRush/Prefabs/Player/PF_Player.prefab";
        const float WallH = 2.8f, WallT = 0.15f, DoorH = 2.2f;

        static Dictionary<string, Material> mats;
        static Shader lit;
        static Texture2D texSoft;
        static readonly List<KeyValuePair<Material, string>> texFix = new List<KeyValuePair<Material, string>>();
        static Transform gHouse, gProps, gSystems, gOutside, gRooms;
        static readonly List<Vector4> blockRects = new List<Vector4>();
        static readonly List<Vector4> groundRects = new List<Vector4>();
        static readonly List<float> groundOffs = new List<float>();

        // key objects
        static WaterTap tap; static MainValve valve; static Stove stove; static CookPot pot; static Door frontDoor;
        static SmokeAlarm alarm; static PlayerController kid; static MomController mom; static Material puddleMat, powderMat;
        static Transform carpetT; static ParticleSystem splashFx; static Window kitchenWinN, kitchenWinE;

        [MenuItem("Mama's Coming/Build House Level")]
        public static void BuildDeferred() { EditorApplication.delayCall += Build; }

        public static void Build()
        {
            EnsureFolder(Root); EnsureFolder(MatDir); EnsureFolder(TexDir); EnsureFolder(Root + "/Scenes");
            mats = new Dictionary<string, Material>();
            lit = Shader.Find("Universal Render Pipeline/Lit");
            blockRects.Clear(); groundRects.Clear(); groundOffs.Clear();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.StartAssetEditing();
            try
            {
                Textures();
                puddleMat = MT("Water", new Color(1f, 1f, 1f, 0f), 0.95f);
                powderMat = M("Powder", C("F4F4F0"), 0.05f);
                gHouse = G("House"); gRooms = G("Rooms"); gProps = G("Props"); gSystems = G("Systems"); gOutside = G("Outside");

                Shell();
                LivingRoom();
                Hallway();
                Bedroom();
                Bathroom();
                Kitchen();
                Laundry();
                Dining();
                Outside();
                Clutter();
                PartyWing();
                PartyHazards();
                Kid();
                Mom();
                Systems();
                Environment();
            }
            finally { AssetDatabase.StopAssetEditing(); }
            // textures created during asset-editing can come out unassigned: re-link them from disk
            foreach (var kv in texFix)
            {
                var tx = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "/" + kv.Value + ".asset");
                if (kv.Key && tx) { kv.Key.SetTexture("_BaseMap", tx); EditorUtility.SetDirty(kv.Key); }
            }
            texFix.Clear();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] House built: " + ScenePath);
        }

        // =====================================================================  SHELL
        static void Shell()
        {
            Cube("FloorCollider", gHouse, V(15f, -0.1f, 7f), V(30.4f, 0.2f, 14.4f), M("Hidden", C("777777")), true).GetComponent<Renderer>().enabled = false;

            Floor("Living", 0, 0, 13, 6, Tex("WoodWarm", 1), V2(13, 6));
            Floor("Laundry", 13, 0, 17, 6, Tex("TileGrey", 4), V2(4 / 0.6f, 6 / 0.6f));
            Floor("Dining", 17, 0, 22, 6, Tex("WoodWarm", 1), V2(5, 6));
            Floor("Hallway", 0, 6, 30, 8, Tex("WoodLight", 2), V2(30, 2));
            Floor("Garage", 22, 0, 30, 6, Tex("TileGrey", 4), V2(8 / 0.9f, 6 / 0.9f));
            Floor("GameRoom", 22, 8, 30, 14, Tex("WoodWarm", 1), V2(8, 6));
            Floor("Bedroom", 0, 8, 7, 14, Tex("WoodLight", 2), V2(7, 6));
            Floor("Bathroom", 7, 8, 12, 14, Tex("TileBathWarm", 13), V2(5 / 0.5f, 6 / 0.5f));
            Floor("Kitchen", 12, 8, 22, 14, Tex("Checker", 0), V2(10 / 1.2f, 6 / 1.2f));

            Material wLiving = Wall("Living", Tex("StripeMint", 6)), wHall = Wall("Hall", Tex("StripeButter", 7)), wBed = Wall("Bed", Tex("StripeLilac", 8)),
                wBath = Wall("Bath", Tex("WallTile", 9)), wKit = Wall("Kitchen", Tex("StripeOrange", 10)), wLaun = Wall("Laundry", Tex("StripeSky", 11)),
                wDin = Wall("Dining", Tex("StripePeach", 12)), wOut = M("Siding", C("FFF1D6")),
                wGar = M("GarageWall", C("D8D2C6")), wGame = M("GameRoomWall", C("5B4B8A"));

            // exterior
            WallZ(0f, 0f, 6f, null, wOut, wLiving);
            WallZ(0f, 6f, 8f, null, wOut, wHall);
            WallZ(0f, 8f, 14f, null, wOut, wBed);
            WallZ(22f, 0f, 6f, null, wDin, wGar);
            WallZ(22f, 8f, 14f, null, wKit, wGame);
            WallZ(30f, 0f, 6f, null, wGar, wOut);
            WallZ(30f, 6f, 8f, null, wHall, wOut);
            WallZ(30f, 8f, 14f, new[] { V2(10f, 11.5f) }, wGame, wOut, true);
            WallX(0f, 22f, 30f, null, wOut, wGar);
            WallX(14f, 22f, 30f, new[] { V2(25.5f, 27f) }, wGame, wOut, new[] { true });
            WallX(6f, 22f, 30f, new[] { V2(23.2f, 24.6f) }, wGar, wHall);
            WallX(8f, 22f, 30f, new[] { V2(23.5f, 25.5f) }, wHall, wGame);
            WallX(0f, 0f, 13f, new[] { V2(2f, 3.5f), V2(8f, 9.2f) }, wOut, wLiving, new[] { true, false });
            WallX(0f, 13f, 17f, null, wOut, wLaun);
            WallX(0f, 17f, 22f, new[] { V2(19f, 20.5f) }, wOut, wDin, new[] { true });
            WallX(14f, 0f, 7f, new[] { V2(4f, 5.4f) }, wBed, wOut, new[] { true });
            WallX(14f, 7f, 12f, new[] { V2(10.3f, 11.3f) }, wBath, wOut, new[] { true });
            WallX(14f, 12f, 22f, new[] { V2(16.3f, 17.7f) }, wKit, wOut, new[] { true });

            // interior
            WallX(6f, 0f, 13f, new[] { V2(4f, 6f) }, wLiving, wHall);
            WallX(6f, 13f, 17f, new[] { V2(14.4f, 15.6f) }, wLaun, wHall);
            WallX(6f, 17f, 22f, new[] { V2(18.5f, 20.5f) }, wDin, wHall);
            WallX(8f, 0f, 7f, new[] { V2(2.4f, 3.6f) }, wHall, wBed);
            WallX(8f, 7f, 12f, new[] { V2(8.9f, 10.1f) }, wHall, wBath);
            WallX(8f, 12f, 22f, new[] { V2(15f, 17f) }, wHall, wKit);
            WallZ(7f, 8f, 14f, null, wBed, wBath);
            WallZ(12f, 8f, 14f, null, wBath, wKit);
            WallZ(13f, 0f, 6f, null, wLiving, wLaun);
            WallZ(17f, 0f, 6f, null, wLaun, wDin);

            // thresholds & sunken bathroom (water has to pile up before it pours out)
            GroundOff(7.1f, 8.1f, 11.9f, 13.9f, -0.0005f);
            GroundOff(8.9f, 7.9f, 10.1f, 8.1f, 0.001f);
            GroundOff(8.4f, 10.0f, 10.4f, 12.0f, -0.0015f);
            GroundOff(8.9f, 10.5f, 9.9f, 11.5f, -0.003f);
            Cube("BathThreshold", gHouse, V(9.5f, 0.006f, 8f), V(1.2f, 0.012f, 0.18f), M("Marble", C("F2EEE6"), 0.6f));

            // interior doors (open, for looks)
            InteriorDoor(V(2.4f, 0, 8f), 1.2f, true, C("F2C6DE"));
            InteriorDoor(V(8.9f, 0, 8f), 1.2f, true, C("BFE8F0"));
            InteriorDoor(V(14.4f, 0, 6f), 1.2f, false, C("CFE3FF"));

            // front door
            var door = G("FrontDoor", gHouse, V(8f, 0f, 0f));
            var piv = G("Pivot", door, V(8f, 0f, 0f));
            Cube("Slab", piv, V(8.6f, 1.05f, 0f), V(1.18f, 2.1f, 0.08f), M("DoorTeal", C("3FB7B0")), true);
            Cube("PanelTop", piv, V(8.6f, 1.55f, 0.05f), V(0.8f, 0.6f, 0.02f), M("DoorPanel", C("5ED1C9")));
            Cube("PanelBottom", piv, V(8.6f, 0.55f, 0.05f), V(0.8f, 0.6f, 0.02f), M("DoorPanel", C("5ED1C9")));
            Sph("Knob", piv, V(9.05f, 1.0f, 0.07f), Vector3.one * 0.08f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            frontDoor = door.gameObject.AddComponent<Door>();
            frontDoor.pivot = piv; frontDoor.openAngle = 100f;
            Vent(V(8.6f, 1f, 0.3f), 0.9f, 0.9f, null, frontDoor, Vector3.back, 1.1f);
        }

        static Material Tex(string n, int kind) => MTex(n, MakeTex(n, kind), kind >= 6 ? new Vector2(5f, 1f) : Vector2.one, kind == 5 || kind == 0 ? 0.45f : 0.25f);
        static Material Wall(string n, Material m) => m;

        static void Floor(string n, float x0, float z0, float x1, float z1, Material m, Vector2 tiling)
        {
            var mm = M("Floor_" + n, Color.white, m.GetFloat("_Smoothness"));
            var src = m.GetTexture("_BaseMap");
            mm.SetTexture("_BaseMap", src);
            texFix.Add(new KeyValuePair<Material, string>(mm, src ? src.name : m.name));
            mm.SetTextureScale("_BaseMap", tiling);
            Cube("Floor_" + n, gRooms, V((x0 + x1) / 2, -0.01f, (z0 + z1) / 2), V(x1 - x0, 0.02f, z1 - z0), mm);
        }

        static void GroundOff(float x0, float z0, float x1, float z1, float off)
        {
            groundRects.Add(new Vector4(x0, z0, x1, z1)); groundOffs.Add(off);
        }

        /// <summary>Wall along X at z, from x0 to x1, with openings. matA faces -Z (south), matB faces +Z.</summary>
        static void WallX(float z, float x0, float x1, Vector2[] gaps, Material matA, Material matB, bool[] windowFlags = null)
        {
            var cuts = new List<float> { x0 };
            if (gaps != null) foreach (var g in gaps) { cuts.Add(g.x); cuts.Add(g.y); }
            cuts.Add(x1);
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a > 0.01f) WallPieceX(z, a, b, 0f, WallH, matA, matB, true);
            }
            if (gaps != null)
                for (int g = 0; g < gaps.Length; g++)
                {
                    bool win = windowFlags != null && g < windowFlags.Length && windowFlags[g];
                    float a = gaps[g].x, b = gaps[g].y;
                    if (win)
                    {
                        WallPieceX(z, a, b, 0f, 0.95f, matA, matB, true);
                        WallPieceX(z, a, b, 2.25f, WallH, matA, matB, false);
                        WindowX(z, a, b);
                    }
                    else
                    {
                        WallPieceX(z, a, b, DoorH, WallH, matA, matB, false);
                        Trim(V((a + b) / 2, DoorH + 0.04f, z), V(b - a + 0.16f, 0.08f, WallT + 0.06f));
                        Trim(V(a - 0.04f, DoorH / 2, z), V(0.08f, DoorH, WallT + 0.06f));
                        Trim(V(b + 0.04f, DoorH / 2, z), V(0.08f, DoorH, WallT + 0.06f));
                    }
                }
        }

        static void WallZ(float x, float z0, float z1, Vector2[] gaps, Material matA, Material matB, bool windowsAll = false)
        {
            var cuts = new List<float> { z0 };
            if (gaps != null) foreach (var g in gaps) { cuts.Add(g.x); cuts.Add(g.y); }
            cuts.Add(z1);
            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float a = cuts[i], b = cuts[i + 1];
                if (b - a > 0.01f) WallPieceZ(x, a, b, 0f, WallH, matA, matB, true);
            }
            if (gaps != null)
                foreach (var g in gaps)
                {
                    if (windowsAll)
                    {
                        WallPieceZ(x, g.x, g.y, 0f, 0.95f, matA, matB, true);
                        WallPieceZ(x, g.x, g.y, 2.25f, WallH, matA, matB, false);
                        WindowZ(x, g.x, g.y);
                    }
                    else WallPieceZ(x, g.x, g.y, DoorH, WallH, matA, matB, false);
                }
        }

        static void WallPieceX(float z, float a, float b, float y0, float y1, Material ma, Material mb, bool blocks)
        {
            var grp = G("Wall", gHouse, V((a + b) / 2, (y0 + y1) / 2, z));
            var cw = grp.gameObject.AddComponent<CutawayWall>();
            cw.normal = Vector3.forward; cw.halfExtent = new Vector2((b - a) / 2, WallH / 2);
            grp.position = V((a + b) / 2, WallH / 2, z);
            float h = y1 - y0, cy = (y0 + y1) / 2;
            Cube("SideS", grp, V((a + b) / 2, cy, z - WallT / 4), V(b - a, h, WallT / 2), ma, true);
            Cube("SideN", grp, V((a + b) / 2, cy, z + WallT / 4), V(b - a, h, WallT / 2), mb, true);
            if (blocks && y0 < 0.01f)
            {
                blockRects.Add(new Vector4(a, z - WallT / 2, b, z + WallT / 2));
                Cube("Base", grp, V((a + b) / 2, 0.06f, z), V(b - a, 0.12f, WallT + 0.03f), M("Baseboard", C("FFFFFF")));
            }
            if (y1 >= WallH - 0.01f) Cube("Top", grp, V((a + b) / 2, WallH, z), V(b - a, 0.06f, WallT + 0.04f), M("TopTrim", C("FFF4E0")));
        }

        static void WallPieceZ(float x, float a, float b, float y0, float y1, Material ma, Material mb, bool blocks)
        {
            var grp = G("Wall", gHouse, V(x, WallH / 2, (a + b) / 2));
            var cw = grp.gameObject.AddComponent<CutawayWall>();
            cw.normal = Vector3.right; cw.halfExtent = new Vector2((b - a) / 2, WallH / 2);
            float h = y1 - y0, cy = (y0 + y1) / 2;
            Cube("SideW", grp, V(x - WallT / 4, cy, (a + b) / 2), V(WallT / 2, h, b - a), ma, true);
            Cube("SideE", grp, V(x + WallT / 4, cy, (a + b) / 2), V(WallT / 2, h, b - a), mb, true);
            if (blocks && y0 < 0.01f)
            {
                blockRects.Add(new Vector4(x - WallT / 2, a, x + WallT / 2, b));
                Cube("Base", grp, V(x, 0.06f, (a + b) / 2), V(WallT + 0.03f, 0.12f, b - a), M("Baseboard", C("FFFFFF")));
            }
            if (y1 >= WallH - 0.01f) Cube("Top", grp, V(x, WallH, (a + b) / 2), V(WallT + 0.04f, 0.06f, b - a), M("TopTrim", C("FFF4E0")));
        }

        static void Trim(Vector3 p, Vector3 s) => Cube("Trim", gHouse, p, s, M("Trim", C("FFFFFF")));

        static void WindowX(float z, float a, float b)
        {
            var root = G("Window", gHouse, V((a + b) / 2, 1.6f, z));
            var frame = M("WindowFrame", C("8FD3F5"));
            var glass = MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f);
            float w = b - a, cx = (a + b) / 2;
            Cube("Sill", root, V(cx, 0.97f, z), V(w + 0.2f, 0.06f, WallT + 0.18f), frame, true);
            Cube("Head", root, V(cx, 2.23f, z), V(w + 0.1f, 0.06f, WallT + 0.06f), frame);
            Cube("L", root, V(a + 0.03f, 1.6f, z), V(0.06f, 1.3f, WallT + 0.06f), frame);
            Cube("R", root, V(b - 0.03f, 1.6f, z), V(0.06f, 1.3f, WallT + 0.06f), frame);
            Cube("UpperGlass", root, V(cx, 1.92f, z + 0.03f), V(w - 0.06f, 0.6f, 0.02f), glass);
            var sash = G("Sash", root, V(cx, 1.3f, z - 0.03f));
            Cube("Glass", sash, V(cx, 1.3f, z - 0.03f), V(w - 0.08f, 0.6f, 0.02f), glass);
            Cube("Bar", sash, V(cx, 1.6f, z - 0.03f), V(w - 0.06f, 0.05f, 0.05f), frame);
            Cube("Bar", sash, V(cx, 1.0f, z - 0.03f), V(w - 0.06f, 0.05f, 0.05f), frame);
            var curtain = M("Curtain", C("FFB3C1"));
            float side = z > 7 ? -0.12f : 0.12f;
            Cube("CurtainL", root, V(a - 0.1f, 1.55f, z + side), V(0.25f, 1.5f, 0.04f), curtain);
            Cube("CurtainR", root, V(b + 0.1f, 1.55f, z + side), V(0.25f, 1.5f, 0.04f), curtain);
            Cyl("Rod", root, V(cx, 2.35f, z + side), V(0.03f, (w + 0.7f) / 2, 0.03f), M("Brass", C("F2C14E"), 0.7f, 0.6f), false, V(0, 0, 90));
            var win = root.gameObject.AddComponent<Window>();
            win.displayName = "Window"; win.sash = sash; win.closedLocal = sash.localPosition; win.openLocal = sash.localPosition + V(0, 0.6f, 0);
            win.interactRadius = 1.7f;
            win.interactAnchor = G("Anchor", root, V(cx, 0, z + (z > 7 ? -0.6f : 0.6f)));
            Vent(V(cx, 1f, z + (z > 7 ? -0.3f : 0.3f)), 1.0f, 0.7f, win, null, z > 7 ? Vector3.forward : Vector3.back, w);
            if (Mathf.Abs(z - 14f) < 0.01f && cx > 15f && cx < 18f) kitchenWinN = win;
        }

        static void WindowZ(float x, float a, float b)
        {
            var root = G("Window", gHouse, V(x, 1.6f, (a + b) / 2));
            var frame = M("WindowFrame", C("8FD3F5"));
            var glass = MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f);
            float w = b - a, cz = (a + b) / 2;
            Cube("Sill", root, V(x, 0.97f, cz), V(WallT + 0.18f, 0.06f, w + 0.2f), frame, true);
            Cube("Head", root, V(x, 2.23f, cz), V(WallT + 0.06f, 0.06f, w + 0.1f), frame);
            Cube("UpperGlass", root, V(x + 0.03f, 1.92f, cz), V(0.02f, 0.6f, w - 0.06f), glass);
            var sash = G("Sash", root, V(x - 0.03f, 1.3f, cz));
            Cube("Glass", sash, V(x - 0.03f, 1.3f, cz), V(0.02f, 0.6f, w - 0.08f), glass);
            Cube("Bar", sash, V(x - 0.03f, 1.6f, cz), V(0.05f, 0.05f, w - 0.06f), frame);
            var win = root.gameObject.AddComponent<Window>();
            win.displayName = "Window"; win.sash = sash; win.closedLocal = sash.localPosition; win.openLocal = sash.localPosition + V(0, 0.6f, 0);
            win.interactRadius = 1.7f;
            win.interactAnchor = G("Anchor", root, V(x - 0.9f, 0, cz));
            Vent(V(x - 0.3f, 1f, cz), 1.0f, 0.7f, win, null, Vector3.right, w);
            kitchenWinE = win;
        }

        static void InteriorDoor(Vector3 hinge, float w, bool alongX, Color col)
        {
            var g = G("InteriorDoor", gHouse, hinge);
            var m = M("Door_" + ColorUtility.ToHtmlStringRGB(col), col);
            if (alongX)
            {
                // swung open into the room on +Z side, resting against the wall
                // swung fully open, resting flat against the wall on the +Z side
                Cube("Slab", g, hinge + V(-w / 2, 1.05f, 0.11f), V(w - 0.05f, 2.1f, 0.05f), m, true);
                Sph("Knob", g, hinge + V(-w + 0.12f, 1.0f, 0.16f), Vector3.one * 0.07f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            }
            else
            {
                Cube("Slab", g, hinge + V(-w / 2, 1.05f, -0.11f), V(w - 0.05f, 2.1f, 0.05f), m, true);
                Sph("Knob", g, hinge + V(-w + 0.12f, 1.0f, -0.16f), Vector3.one * 0.07f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            }
        }

        static void Vent(Vector3 p, float r, float rate, Window w, Door d, Vector3 outward, float width)
        {
            var v = G("SmokeVent", gSystems, p).gameObject.AddComponent<SmokeVent>();
            v.radius = Mathf.Max(1.2f, r); v.rate = Mathf.Max(1.4f, rate); v.window = w; v.door = d; v.outward = outward; v.openingWidth = width;
        }

        // =====================================================================  LIVING ROOM (x0..13, z0..6)
        static void LivingRoom()
        {
            var t = G("LivingRoom", gRooms);
            var wood = M("WoodDark", C("8A4B2A"));
            // big carpet that soaks water (damage!)
            var carpet = G("Living room carpet", t, V(3.2f, 0.008f, 3.0f));
            Cube("Border", carpet, V(3.2f, 0.006f, 3.0f), V(4.4f, 0.012f, 3.6f), M("CarpetBorder", C("E07A5F")));
            Cube("Field", carpet, V(3.2f, 0.008f, 3.0f), V(4.0f, 0.012f, 3.2f), M("CarpetField", C("F2CC8F")));
            Cube("Medallion", carpet, V(3.2f, 0.01f, 3.0f), V(1.6f, 0.012f, 1.2f), M("CarpetMed", C("81B29A")));
            var cc = carpet.gameObject.AddComponent<CoverObject>();
            cc.halfSize = V2(2.2f, 1.8f); cc.absorbRate = 0.6f; cc.capacityLiters = 18f; cc.hidesWater = true;
            cc.tintRenderers = carpet.GetComponentsInChildren<Renderer>();
            carpetT = carpet;

            // TV wall (west)
            Cube("TVStand", t, V(0.4f, 0.28f, 3.0f), V(0.55f, 0.56f, 1.9f), M("TVStand", C("F29B4F")), true);
            Cube("TVStandDoor", t, V(0.68f, 0.28f, 2.55f), V(0.01f, 0.4f, 0.8f), M("PanelLight", C("FFB46E")));
            Cube("TVStandDoor", t, V(0.68f, 0.28f, 3.45f), V(0.01f, 0.4f, 0.8f), M("PanelLight", C("FFB46E")));
            Cube("TV", t, V(0.35f, 1.05f, 3.0f), V(0.12f, 0.75f, 1.35f), M("TVBody", C("2E2A3A"), 0.6f), true);
            Cube("TVScreen", t, V(0.42f, 1.05f, 3.0f), V(0.01f, 0.66f, 1.25f), ME("Screen", C("1C3A4A"), new Color(0.05f, 0.25f, 0.35f)));
            Cube("Console", t, V(0.45f, 0.6f, 3.6f), V(0.25f, 0.06f, 0.3f), M("Console", C("FFFFFF")));
            Cube("Speaker", t, V(0.35f, 0.45f, 1.7f), V(0.3f, 0.9f, 0.3f), M("Speaker", C("3A3A44")), true);
            Cube("Speaker", t, V(0.35f, 0.45f, 4.3f), V(0.3f, 0.9f, 0.3f), M("Speaker", C("3A3A44")), true);

            // Sofa facing west (kid fell asleep in front of it)
            var sofa = M("Sofa", C("FF8FB1")); var cush = M("SofaCushion", C("FFB3C9"));
            var s = G("Sofa", t, V(5.6f, 0, 3.0f));
            Cube("Base", s, V(5.6f, 0.3f, 3.0f), V(0.95f, 0.3f, 2.3f), sofa, true);
            Cube("SeatA", s, V(5.5f, 0.52f, 2.45f), V(0.8f, 0.16f, 1.1f), cush, true);
            Cube("SeatB", s, V(5.5f, 0.52f, 3.55f), V(0.8f, 0.16f, 1.1f), cush, true);
            Cube("Back", s, V(6.0f, 0.8f, 3.0f), V(0.22f, 0.75f, 2.3f), sofa, true);
            Cube("ArmA", s, V(5.6f, 0.62f, 1.78f), V(0.95f, 0.38f, 0.2f), sofa, true);
            Cube("ArmB", s, V(5.6f, 0.62f, 4.22f), V(0.95f, 0.38f, 0.2f), sofa, true);
            Sph("Pillow", s, V(5.85f, 0.85f, 2.1f), V(0.18f, 0.38f, 0.38f), M("Pillow", C("43CFC4")), false, V(0, 0, 15));
            Sph("Pillow", s, V(5.85f, 0.85f, 3.9f), V(0.18f, 0.38f, 0.38f), M("Pillow2", C("FFD447")), false, V(0, 0, 15));
            Cube("Blanket", s, V(5.4f, 0.61f, 3.4f), V(0.7f, 0.03f, 0.8f), M("Blanket", C("7A5CD6")), false, V(0, 12, -6));
            foreach (var p in new[] { V(5.2f, 0.08f, 2.0f), V(6.0f, 0.08f, 2.0f), V(5.2f, 0.08f, 4.0f), V(6.0f, 0.08f, 4.0f) }) Cyl("Leg", s, p, V(0.07f, 0.08f, 0.07f), wood);
            var under = Hide(s, "Sofa", "under the sofa", 0.25f, V(5.6f, 0.08f, 3.0f), V(0.8f, 0.14f, 2.0f), V(5.6f, 0.04f, 3.0f), false);
            under.interactAnchor = G("Anchor", s, V(4.8f, 0, 3.0f));

            // Coffee table
            var lw = M("LightWood", C("E8B27A"));
            Cube("CoffeeTop", t, V(2.6f, 0.42f, 3.0f), V(0.75f, 0.05f, 1.25f), lw, true);
            Cube("CoffeeShelf", t, V(2.6f, 0.12f, 3.0f), V(0.65f, 0.03f, 1.15f), lw, true);
            foreach (var p in new[] { V(2.27f, 0.2f, 2.42f), V(2.93f, 0.2f, 2.42f), V(2.27f, 0.2f, 3.58f), V(2.93f, 0.2f, 3.58f) }) Cube("Leg", t, p, V(0.05f, 0.4f, 0.05f), wood, true);

            // Armchair + side table + lamp
            var arm = M("Armchair", C("43CFC4"));
            Cube("ArmchairBase", t, V(2.2f, 0.3f, 5.1f), V(0.85f, 0.4f, 0.8f), arm, true, V(0, -25, 0));
            Cube("ArmchairBack", t, V(2.38f, 0.75f, 5.45f), V(0.85f, 0.6f, 0.2f), arm, true, V(0, -25, 0));
            Cube("ArmchairCushion", t, V(2.2f, 0.53f, 5.05f), V(0.65f, 0.1f, 0.6f), M("ArmCushion", C("FFE07A")), false, V(0, -25, 0));
            FloorLamp(t, V(0.6f, 0, 5.4f));
            Plant(t, V(0.5f, 0, 0.5f), 1.9f, C("E0623B"));
            Plant(t, V(6.6f, 0, 0.5f), 1.5f, C("4FA3E0"));

            // entry / reading corner (x7..13)
            Cube("ShoeRack", t, V(10.2f, 0.2f, 0.3f), V(1.0f, 0.4f, 0.32f), M("ShoeRack", C("E8B27A")), true);
            Color[] shoe = { C("FF6F61"), C("43CFC4"), C("FFD447"), C("7A5CD6") };
            for (int i = 0; i < 4; i++) Cube("Shoe", t, V(9.85f + i * 0.24f, 0.45f, 0.3f), V(0.11f, 0.08f, 0.25f), M("Shoe" + i, shoe[i]));
            Cube("CoatRail", t, V(10.2f, 1.75f, 0.1f), V(1.0f, 0.06f, 0.06f), wood);
            Cap("Coat", t, V(10.0f, 1.45f, 0.18f), V(0.35f, 0.3f, 0.18f), M("Coat", C("FF8FB1")));
            Cap("Coat", t, V(10.45f, 1.45f, 0.18f), V(0.3f, 0.28f, 0.16f), M("Coat2", C("4A7BE0")));
            Cube("Mirror", t, V(7.3f, 1.5f, 0.09f), V(0.7f, 1.0f, 0.02f), M("Mirror", C("D8EEF5"), 0.95f, 0.5f));
            Cube("Console", t, V(7.3f, 0.75f, 0.25f), V(1.0f, 0.05f, 0.35f), lw, true);
            Cube("ConsoleLeg", t, V(6.85f, 0.37f, 0.25f), V(0.05f, 0.74f, 0.3f), lw, true);
            Cube("ConsoleLeg", t, V(7.75f, 0.37f, 0.25f), V(0.05f, 0.74f, 0.3f), lw, true);
            Cyl("KeyBowl", t, V(7.5f, 0.8f, 0.25f), V(0.2f, 0.03f, 0.2f), M("Bowl", C("FF8FB1"), 0.5f));
            Cube("Doormat", t, V(8.6f, 0.006f, 0.55f), V(1.1f, 0.012f, 0.6f), M("Doormat", C("C98B4E")));

            var bs = M("Bookshelf", C("C47A45"));
            Cube("Bookshelf", t, V(12.7f, 1.0f, 3.0f), V(0.4f, 2.0f, 1.6f), bs, true);
            Color[] bc = { C("FF6F61"), C("FFD447"), C("43CFC4"), C("7A5CD6"), C("FF8FB1"), C("6CC63A") };
            for (int r = 0; r < 4; r++)
            {
                Cube("Board", t, V(12.48f, 0.42f + r * 0.45f, 3.0f), V(0.02f, 0.03f, 1.52f), wood);
                float z = 2.3f;
                for (int b = 0; b < 8; b++)
                {
                    float wB = 0.08f + (b * 7 + r * 3) % 4 * 0.025f, hB = 0.25f + (b * 5 + r) % 3 * 0.05f;
                    Cube("Book", t, V(12.5f, 0.45f + r * 0.45f + hB / 2, z + wB / 2), V(0.03f, hB, wB), M("Book" + ((b + r) % bc.Length), bc[(b + r) % bc.Length]));
                    z += wB + 0.03f;
                }
            }
            Sph("Beanbag", t, V(11.4f, 0.3f, 2.2f), V(1.0f, 0.6f, 1.0f), M("Beanbag", C("FFD447")), true);
            FloorLamp(t, V(12.4f, 0, 5.3f));
            Frame(t, V(3.0f, 1.9f, 5.92f), 1.2f, 0.7f, Vector3.back, C("E07A3F"));
            Frame(t, V(9.5f, 1.8f, 5.92f), 0.6f, 0.8f, Vector3.back, C("7A5CD6"));
            Frame(t, V(11.2f, 1.9f, 5.92f), 0.5f, 0.5f, Vector3.back, C("43CFC4"));
            Clock(t, V(0.09f, 2.2f, 1.3f), Vector3.right);
        }

        static void FloorLamp(Transform t, Vector3 p)
        {
            Cyl("LampBase", t, p + V(0, 0.02f, 0), V(0.35f, 0.02f, 0.35f), M("WoodDark", C("8A4B2A")), true);
            Cyl("LampPole", t, p + V(0, 0.75f, 0), V(0.04f, 0.75f, 0.04f), M("Brass", C("F2C14E"), 0.7f, 0.6f), true);
            Cyl("LampShade", t, p + V(0, 1.62f, 0), V(0.5f, 0.17f, 0.5f), ME("LampShade", C("FFF1C9"), new Color(1.2f, 0.95f, 0.6f)));
            PointLight("LampLight", p + V(0, 1.45f, 0), C("FFD49A"), 4f, 1.0f, gRooms);
        }

        // =====================================================================  HALLWAY (z6..8)
        static void Hallway()
        {
            var t = G("Hallway", gRooms);
            var runner = Prop<Rug>("Hallway runner", V(11.5f, 0.012f, 7.0f), 3f, "Hallway runner");
            var r1 = L(PrimitiveType.Cube, "Runner", runner.transform, V(0, 0, 0), V(4.5f, 0.008f, 0.95f), M("RunnerRed", C("C8553D")));
            var r2 = L(PrimitiveType.Cube, "Stripe", runner.transform, V(0, 0.002f, 0), V(4.3f, 0.008f, 0.12f), M("RunnerCream", C("FFE6C7")));
            var r3 = L(PrimitiveType.Cube, "Edge", runner.transform, V(-2.1f, 0.002f, 0), V(0.12f, 0.008f, 0.95f), M("RunnerCream", C("FFE6C7")));
            var r4 = L(PrimitiveType.Cube, "Edge", runner.transform, V(2.1f, 0.002f, 0), V(0.12f, 0.008f, 0.95f), M("RunnerCream", C("FFE6C7")));
            BoxCol(runner.gameObject, Vector3.zero, V(4.5f, 0.02f, 0.95f));
            var rc = runner.GetComponent<CoverObject>();
            rc.halfSize = V2(2.25f, 0.48f); rc.absorbRate = 0.35f; rc.capacityLiters = 8f;
            rc.tintRenderers = new[] { r1.GetComponent<Renderer>(), r2.GetComponent<Renderer>(), r3.GetComponent<Renderer>(), r4.GetComponent<Renderer>() };
            runner.tool = ToolType.Rug; runner.dragOnFloor = true; runner.holdOffset = V(0, 0.012f, 1.6f); runner.interactRadius = 2.4f;

            var lw = M("LightWood", C("E8B27A"));
            Cube("HallConsole", t, V(20.6f, 0.78f, 7.72f), V(1.3f, 0.05f, 0.35f), lw, true);
            Cube("HallConsoleLegs", t, V(20.6f, 0.38f, 7.72f), V(1.2f, 0.76f, 0.3f), M("PanelLight", C("FFB46E")), true);
            Cyl("Vase", t, V(20.2f, 0.95f, 7.72f), V(0.16f, 0.15f, 0.16f), M("Vase", C("43CFC4"), 0.6f));
            Sph("Flowers", t, V(20.2f, 1.18f, 7.72f), V(0.3f, 0.2f, 0.3f), M("Bloom", C("FF7AA8")));
            Plant(t, V(21.5f, 0, 6.4f), 1.6f, C("F28C38"));
            Plant(t, V(0.5f, 0, 6.5f), 1.4f, C("4FA3E0"));
            Cyl("CoatStand", t, V(1.0f, 0.9f, 7.5f), V(0.05f, 0.9f, 0.05f), M("WoodDark", C("8A4B2A")), true);
            Cap("Scarf", t, V(1.0f, 1.55f, 7.5f), V(0.25f, 0.3f, 0.12f), M("Scarf", C("FFD447")));
            Frame(t, V(6.5f, 1.8f, 7.92f), 0.6f, 0.45f, Vector3.back, C("FFC93C"));
            Frame(t, V(12.8f, 1.8f, 7.92f), 0.8f, 0.55f, Vector3.back, C("FF7AA8"));
            Frame(t, V(11.0f, 1.8f, 6.08f), 0.5f, 0.6f, Vector3.forward, C("6EC6FF"));
            Frame(t, V(21.0f, 1.8f, 6.08f), 0.7f, 0.5f, Vector3.forward, C("A4E05A"));

            // smoke alarm (ceiling, right outside the kitchen)
            var al = G("Smoke alarm", t, V(16.0f, 2.74f, 7.0f));
            Cyl("Body", al, V(16.0f, 2.74f, 7.0f), V(0.22f, 0.025f, 0.22f), M("AlarmWhite", C("F7F7F2")));
            var led = Sph("LED", al, V(16.0f, 2.71f, 7.06f), Vector3.one * 0.035f, ME("LED", C("FF2222"), new Color(3f, 0.1f, 0.1f)));
            var ll = new GameObject("LEDLight").AddComponent<Light>();
            ll.transform.SetParent(al, false); ll.transform.position = V(16.0f, 2.5f, 7.0f);
            ll.type = LightType.Point; ll.color = Color.red; ll.range = 3f; ll.intensity = 2.5f; ll.enabled = false;
            alarm = al.gameObject.AddComponent<SmokeAlarm>();
            alarm.displayName = "Smoke alarm"; alarm.threshold = 0.18f; alarm.led = led.GetComponent<Renderer>(); alarm.ledLight = ll;
            alarm.interactRadius = 1.6f; alarm.interactAnchor = G("Anchor", al, V(16.0f, 0, 7.0f));
            Cube("CeilingLightPlate", t, V(5f, 2.79f, 7f), V(0.4f, 0.02f, 0.4f), ME("Bulb", C("FFF6DD"), new Color(1.5f, 1.3f, 1f)));
        }

        // =====================================================================  BEDROOM (x0..7, z8..14)
        static void Bedroom()
        {
            var t = G("Bedroom", gRooms);
            var wood = M("BedWood", C("C98B5A"));
            // bed along west wall, head at north
            var bed = G("Bed", t, V(1.15f, 0, 12.4f));
            Cube("Frame", bed, V(1.15f, 0.22f, 12.4f), V(1.25f, 0.3f, 2.2f), wood, true);
            Cube("Mattress", bed, V(1.15f, 0.47f, 12.4f), V(1.15f, 0.2f, 2.1f), M("Sheet", C("FFFFFF")), true);
            Cube("Duvet", bed, V(1.15f, 0.59f, 12.0f), V(1.2f, 0.08f, 1.45f), M("Duvet", C("6EC6FF")), true);
            Cube("DuvetFold", bed, V(1.15f, 0.62f, 12.75f), V(1.2f, 0.06f, 0.25f), M("Sheet", C("FFFFFF")));
            Cube("Pillow", bed, V(1.15f, 0.64f, 13.15f), V(0.8f, 0.14f, 0.4f), M("Pillow", C("FFFFFF")));
            Cube("Headboard", bed, V(1.15f, 0.75f, 13.55f), V(1.35f, 1.1f, 0.1f), M("Headboard", C("FF8FB1")), true);
            Sph("HeadboardStar", bed, V(1.15f, 1.15f, 13.49f), V(0.25f, 0.25f, 0.04f), M("StarYellow", C("FFE04A")));
            Hide(bed, "Bed", "under the bed", 0.3f, V(1.15f, 0.04f, 12.4f), V(1.1f, 0.06f, 2.0f), V(1.15f, 0.03f, 12.4f), false).interactAnchor = G("Anchor", bed, V(2.2f, 0, 12.0f));

            Cube("Nightstand", t, V(2.25f, 0.27f, 13.45f), V(0.5f, 0.54f, 0.45f), wood, true);
            Cyl("LampBase", t, V(2.25f, 0.6f, 13.45f), V(0.12f, 0.06f, 0.12f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
            Cyl("LampShade", t, V(2.25f, 0.78f, 13.45f), V(0.28f, 0.11f, 0.28f), ME("LampShadePink", C("FFD1E0"), new Color(1.1f, 0.7f, 0.8f)));

            // desk under window
            var lw = M("DeskWood", C("F2C08A"));
            Cube("DeskTop", t, V(4.7f, 0.74f, 13.55f), V(1.4f, 0.05f, 0.6f), lw, true);
            Cube("DeskSideL", t, V(4.05f, 0.36f, 13.55f), V(0.05f, 0.72f, 0.58f), lw, true);
            Cube("DeskDrawers", t, V(5.2f, 0.36f, 13.55f), V(0.4f, 0.72f, 0.58f), M("DeskDrawer", C("FF7AA8")), true);
            Cyl("DeskLamp", t, V(4.2f, 0.95f, 13.7f), V(0.05f, 0.2f, 0.05f), M("Teal", C("43CFC4")));
            Sph("DeskLampHead", t, V(4.3f, 1.12f, 13.6f), V(0.2f, 0.12f, 0.2f), M("Teal", C("43CFC4")));
            Cube("Globe", t, V(5.25f, 0.85f, 13.7f), V(0.05f, 0.18f, 0.05f), M("WoodDark", C("8A4B2A")));
            Sph("GlobeBall", t, V(5.25f, 1.0f, 13.7f), Vector3.one * 0.22f, M("Globe", C("4FA3E0")));
            MakeChair(V(4.7f, 0, 12.75f), 180f, "Desk chair");

            // wardrobe (hiding spot)
            var wr = G("Wardrobe", t, V(6.6f, 0, 11.4f));
            var ww = M("Wardrobe", C("FFF1D6"));
            Cube("Back", wr, V(6.88f, 1.05f, 11.4f), V(0.04f, 2.1f, 1.4f), ww, true);
            Cube("SideA", wr, V(6.6f, 1.05f, 10.72f), V(0.6f, 2.1f, 0.04f), ww, true);
            Cube("SideB", wr, V(6.6f, 1.05f, 12.08f), V(0.6f, 2.1f, 0.04f), ww, true);
            Cube("Top", wr, V(6.6f, 2.08f, 11.4f), V(0.6f, 0.04f, 1.4f), ww, true);
            Cube("Bottom", wr, V(6.6f, 0.06f, 11.4f), V(0.6f, 0.12f, 1.4f), ww, true);
            Cube("Inside", wr, V(6.84f, 1.0f, 11.4f), V(0.01f, 1.9f, 1.3f), M("WardrobeIn", C("C9B79C")));
            Cube("Shirt", wr, V(6.6f, 1.4f, 11.1f), V(0.35f, 0.55f, 0.08f), M("Shirt1", C("FF6F61")));
            Cube("Shirt", wr, V(6.6f, 1.4f, 11.6f), V(0.35f, 0.55f, 0.08f), M("Shirt2", C("6CC63A")));
            var pivW = G("DoorPivot", wr, V(6.3f, 1.05f, 10.72f));
            Cube("Door", pivW, V(6.3f, 1.05f, 11.4f), V(0.04f, 2.0f, 1.34f), M("WardrobeDoor", C("B8E1F2")), true);
            Sph("Knob", pivW, V(6.26f, 1.05f, 11.95f), Vector3.one * 0.06f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            var cab = Hide(wr, "Wardrobe", "in the wardrobe", 0.35f, V(6.6f, 0.5f, 11.4f), V(0.5f, 0.8f, 1.2f), V(6.6f, 0.18f, 11.4f), true);
            cab.doorPivot = pivW; cab.openAngle = -100f; cab.interactAnchor = G("Anchor", wr, V(5.7f, 0, 11.4f));

            // toy box (hiding spot with lid)
            var tb = G("Toy box", t, V(5.9f, 0, 8.6f));
            Cube("Box", tb, V(5.9f, 0.25f, 8.6f), V(0.9f, 0.5f, 0.5f), M("ToyBox", C("FFD447")), true);
            Cube("Stripe", tb, V(5.9f, 0.3f, 8.34f), V(0.9f, 0.1f, 0.01f), M("ToyStripe", C("FF6F61")));
            var pivL = G("LidPivot", tb, V(5.9f, 0.5f, 8.85f));
            Cube("Lid", pivL, V(5.9f, 0.52f, 8.6f), V(0.94f, 0.05f, 0.54f), M("ToyLid", C("43CFC4")), true);
            var tcab = Hide(tb, "Toy box", "in the toy box", 0.4f, V(5.9f, 0.25f, 8.6f), V(0.8f, 0.4f, 0.4f), V(5.9f, 0.15f, 8.6f), true);
            tcab.doorPivot = pivL; tcab.openAxis = Vector3.right; tcab.openAngle = -80f; tcab.interactAnchor = G("Anchor", tb, V(5.9f, 0, 9.3f));

            var rug = G("BedroomRug", t, V(3.4f, 0.008f, 11.0f));
            Cyl("Rug", rug, V(3.4f, 0.006f, 11.0f), V(2.2f, 0.006f, 2.2f), M("RugPurple", C("B39DDB")));
            Cyl("RugIn", rug, V(3.4f, 0.009f, 11.0f), V(1.6f, 0.006f, 1.6f), M("RugCream", C("FFE6C7")));
            var rcov = rug.gameObject.AddComponent<CoverObject>(); rcov.halfSize = V2(1f, 1f); rcov.absorbRate = 0.3f; rcov.capacityLiters = 6f;

            Frame(t, V(3.2f, 1.9f, 13.92f), 0.5f, 0.7f, Vector3.back, C("43CFC4"));
            Frame(t, V(0.08f, 1.8f, 10.0f), 0.8f, 0.55f, Vector3.right, C("FF6F61"));
            for (int i = 0; i < 7; i++) Sph("FairyLight", t, V(0.12f, 2.3f - Mathf.Sin(i * 0.9f) * 0.08f, 10.9f + i * 0.35f), Vector3.one * 0.06f, ME("Fairy", C("FFE9A8"), new Color(2f, 1.6f, 0.6f)));
            Plant(t, V(6.6f, 0, 13.6f), 1.2f, C("FF7AA8"));
            PointLight("BedroomLight", V(3.5f, 2.6f, 11f), C("FFE2C4"), 7f, 1.2f, gRooms);
        }

        // =====================================================================  BATHROOM (x7..12, z8..14) - FLOOD SOURCE
        static void Bathroom()
        {
            var t = G("Bathroom", gRooms);
            var porcelain = M("Porcelain", C("FBFBF8"), 0.75f);
            var chrome = M("Chrome", C("D5DAE0"), 0.9f, 0.9f);

            // Bathtub against the north wall
            var tub = G("Bathtub", t, V(8.55f, 0, 13.45f));
            Cube("Front", tub, V(8.55f, 0.28f, 13.1f), V(2.8f, 0.56f, 0.1f), porcelain, true);
            Cube("Back", tub, V(8.55f, 0.28f, 13.85f), V(2.8f, 0.56f, 0.08f), porcelain, true);
            Cube("EndW", tub, V(7.2f, 0.28f, 13.47f), V(0.1f, 0.56f, 0.7f), porcelain, true);
            Cube("EndE", tub, V(9.9f, 0.28f, 13.47f), V(0.1f, 0.56f, 0.7f), porcelain, true);
            Cube("Bottom", tub, V(8.55f, 0.05f, 13.47f), V(2.7f, 0.1f, 0.68f), porcelain, true);
            Cube("RimTop", tub, V(8.55f, 0.57f, 13.1f), V(2.85f, 0.03f, 0.14f), porcelain);
            var water = Cube("TubWater", tub, V(8.55f, 0.53f, 13.47f), V(2.62f, 0.02f, 0.62f), MT("TubWater", new Color(0.45f, 0.72f, 0.95f, 0.75f), 0.95f));
            var spill = G("Overflow", tub, V(8.55f, 0.3f, 13.03f));
            Cube("Sheet", spill, V(8.55f, 0.3f, 13.035f), V(2.6f, 0.55f, 0.012f), MT("WaterSheet", new Color(0.55f, 0.8f, 1f, 0.45f), 0.95f));
            for (int i = 0; i < 5; i++) Cyl("Stream", spill, V(7.6f + i * 0.47f, 0.3f, 13.0f), V(0.05f, 0.27f, 0.05f), MT("WaterStream", new Color(0.6f, 0.85f, 1f, 0.6f), 0.95f));
            // tap on the west end
            Cyl("TapPipe", tub, V(7.32f, 0.82f, 13.47f), V(0.035f, 0.08f, 0.035f), chrome, false, V(0, 0, 90));
            Cube("Spout", tub, V(7.42f, 0.78f, 13.47f), V(0.14f, 0.05f, 0.06f), chrome);
            var handle = G("Handle", tub, V(7.28f, 0.95f, 13.47f));
            Cube("Lever", handle, V(7.3f, 0.97f, 13.47f), V(0.04f, 0.04f, 0.16f), M("RedPlastic", C("FF5A5A")));
            var broken = Cube("BrokenHandle", tub, V(7.6f, 0.012f, 12.8f), V(0.16f, 0.03f, 0.04f), M("RedPlastic", C("FF5A5A")), false, V(0, 35, 0));
            broken.SetActive(false);
            var stream = Cyl("Stream", tub, V(7.48f, 0.66f, 13.47f), V(0.04f, 0.12f, 0.04f), MT("WaterStream", new Color(0.6f, 0.85f, 1f, 0.6f), 0.95f));
            Cube("Bubbles", tub, V(8.0f, 0.55f, 13.45f), V(0.9f, 0.04f, 0.45f), M("Foam", C("FFFFFF")));
            Sph("Duck", tub, V(9.0f, 0.58f, 13.4f), V(0.13f, 0.1f, 0.13f), M("Duck", C("FFD447")));
            Cyl("CurtainRod", tub, V(8.55f, 2.1f, 13.0f), V(0.02f, 1.45f, 0.02f), chrome, false, V(0, 0, 90));
            Cube("ShowerCurtain", tub, V(9.65f, 1.35f, 13.0f), V(0.55f, 1.5f, 0.03f), M("ShowerCurtain", C("BFE8F0")));
            Cyl("ShowerHead", tub, V(7.25f, 1.9f, 13.47f), V(0.12f, 0.02f, 0.12f), chrome, false, V(0, 0, 70));

            tap = tub.gameObject.AddComponent<WaterTap>();
            tap.displayName = "Bathtub tap"; tap.interactRadius = 1.4f;
            tap.interactAnchor = G("Anchor", tub, V(7.8f, 0, 12.75f));
            tap.tubWater = water.transform; tap.tubWaterFullY = water.transform.localPosition.y; tap.tubWaterEmptyY = water.transform.localPosition.y - 0.42f;
            tap.stream = stream; tap.overflowSheet = spill.gameObject; tap.handle = handle; tap.brokenHandle = broken;
            tap.overflowPoints = new[] { G("Over1", tub, V(7.7f, 0, 12.92f)), G("Over2", tub, V(8.55f, 0, 12.92f)), G("Over3", tub, V(9.4f, 0, 12.92f)) };
            tap.litersPerSecond = 1.5f; tap.tubCapacity = 160f; tap.tubLevel = 160f;
            var tubDump = G("TubDump", tub, V(9.2f, 0, 12.75f)).gameObject.AddComponent<WaterDump>();
            tubDump.displayName = "Bathtub"; tubDump.tub = tap; tubDump.interactRadius = 1.2f;

            // Toilet (east wall, facing west)
            var to = G("Toilet", t, V(11.5f, 0, 12.9f));
            Cube("Tank", to, V(11.8f, 0.75f, 12.9f), V(0.22f, 0.45f, 0.5f), porcelain, true);
            Cyl("Base", to, V(11.45f, 0.2f, 12.9f), V(0.35f, 0.2f, 0.35f), porcelain, true);
            Cyl("Bowl", to, V(11.4f, 0.4f, 12.9f), V(0.48f, 0.04f, 0.42f), porcelain, true);
            Cyl("Seat", to, V(11.4f, 0.44f, 12.9f), V(0.46f, 0.02f, 0.4f), M("ToiletSeat", C("BFE8F0")));
            var dumpT = to.gameObject.AddComponent<WaterDump>(); dumpT.displayName = "Toilet"; dumpT.interactRadius = 1.2f;
            dumpT.interactAnchor = G("Anchor", to, V(10.8f, 0, 12.9f));
            Cyl("PaperRoll", t, V(11.9f, 0.7f, 12.3f), V(0.12f, 0.06f, 0.12f), M("Paper", C("FFFEF6")), false, V(90, 0, 0));

            // Vanity + mirror (east wall)
            var va = G("Vanity", t, V(11.6f, 0, 10.3f));
            Cube("Cabinet", va, V(11.6f, 0.4f, 10.3f), V(0.55f, 0.8f, 1.0f), M("VanityWood", C("43CFC4")), true);
            Cube("Top", va, V(11.58f, 0.82f, 10.3f), V(0.6f, 0.04f, 1.05f), M("Marble", C("F2EEE6"), 0.6f), true);
            Cyl("Basin", va, V(11.55f, 0.84f, 10.3f), V(0.38f, 0.02f, 0.38f), porcelain);
            Cyl("Faucet", va, V(11.78f, 0.95f, 10.3f), V(0.03f, 0.1f, 0.03f), chrome);
            Cube("Mirror", va, V(11.9f, 1.55f, 10.3f), V(0.02f, 0.75f, 0.85f), M("Mirror", C("D8EEF5"), 0.95f, 0.5f));
            Cube("MirrorFrame", va, V(11.91f, 1.55f, 10.3f), V(0.02f, 0.85f, 0.95f), M("Frame_FF8FB1", C("FF8FB1")));
            Cyl("Toothbrushes", va, V(11.75f, 0.92f, 9.95f), V(0.06f, 0.07f, 0.06f), M("Cup", C("FFD447")));
            var dumpV = va.gameObject.AddComponent<WaterDump>(); dumpV.displayName = "Bathroom sink"; dumpV.hasTap = true; dumpV.interactRadius = 1.2f;
            dumpV.interactAnchor = G("Anchor", va, V(11.0f, 0, 10.3f));

            // Floor drain (clogged!)
            var dr = G("Floor drain", t, V(9.4f, 0.004f, 11.0f));
            Cyl("Grate", dr, V(9.4f, 0.004f, 11.0f), V(0.2f, 0.004f, 0.2f), chrome);
            for (int i = 0; i < 3; i++) Cube("Slot", dr, V(9.35f + i * 0.05f, 0.006f, 11.0f), V(0.015f, 0.003f, 0.14f), M("Black", C("1A1A1F"), 0.8f));
            var hair = Sph("Hair", dr, V(9.42f, 0.02f, 11.02f), V(0.18f, 0.04f, 0.15f), M("Hair", C("3A2418")));
            var drain = dr.gameObject.AddComponent<Drain>();
            drain.displayName = "Drain"; drain.hairClump = hair; drain.radius = 0.35f; drain.clearRate = 1.2f; drain.cloggedRate = 0.06f; drain.interactRadius = 1.2f;

            // Bath mat (soaks a bit) + towel rack + hamper
            var mat = G("Bath mat", t, V(8.5f, 0.008f, 12.45f));
            Cube("Mat", mat, V(8.5f, 0.008f, 12.45f), V(1.0f, 0.016f, 0.55f), M("BathMat", C("FF8FB1")));
            var mcov = mat.gameObject.AddComponent<CoverObject>(); mcov.halfSize = V2(0.5f, 0.28f); mcov.absorbRate = 0.2f; mcov.capacityLiters = 1.5f;
            Cyl("TowelBar", t, V(7.12f, 1.25f, 10.4f), V(0.02f, 0.45f, 0.02f), chrome, false, V(90, 0, 0));
            MakeTowel(V(7.2f, 1.0f, 10.4f), V(0, 90, 0), C("FFD447"), true, "Bath towel");
            var hm = G("Laundry hamper", t, V(8.0f, 0, 9.0f));
            Cyl("Basket", hm, V(8.0f, 0.3f, 9.0f), V(0.5f, 0.3f, 0.5f), M("Wicker", C("D9B27C")), true);
            Cube("Sock", hm, V(8.0f, 0.62f, 9.05f), V(0.15f, 0.04f, 0.08f), M("Shirt1", C("FF6F61")));
            Hide(hm, "Laundry hamper", "in the laundry hamper", 0.35f, V(8.0f, 0.3f, 9.0f), V(0.45f, 0.5f, 0.45f), V(8.0f, 0.25f, 9.0f), false);
            Plant(t, V(11.5f, 0, 8.5f), 0.9f, C("43CFC4"));
            PointLight("BathLight", V(9.5f, 2.6f, 11f), C("EAF6FF"), 7f, 1.3f, gRooms);
        }

        // =====================================================================  KITCHEN (x12..22, z8..14) - FIRE SOURCE
        static void Kitchen()
        {
            var t = G("Kitchen", gRooms);
            var body = M("CounterBody", C("6CC63A"));
            var top = M("CounterTop", C("F2EEE6"), 0.6f);
            var door = M("CabinetDoor", C("5BB12F"));
            var knob = M("Knob", C("FFD447"), 0.6f);

            // north counter run (x 12.2..20.6) with stove at 14..15 and sink at 16.4..17.6
            CounterX(t, 12.2f, 14.0f, 13.5f, body, top, door, knob);
            CounterX(t, 15.0f, 16.4f, 13.5f, body, top, door, knob);
            CounterX(t, 17.6f, 20.6f, 13.5f, body, top, door, knob);
            CounterZ(t, 8.6f, 12.9f, 21.5f, body, top, door, knob);

            // Upper cabinets (wood - can catch fire)
            var upper = M("UpperCab", C("FFF1D6"));
            Cube("UpperA", t, V(13.1f, 1.95f, 13.7f), V(1.75f, 0.7f, 0.35f), upper, true);
            Cube("UpperB", t, V(15.7f, 1.95f, 13.7f), V(1.3f, 0.7f, 0.35f), upper, true);
            Cube("UpperC", t, V(19.2f, 1.95f, 13.7f), V(2.8f, 0.7f, 0.35f), upper, true);
            for (int i = 0; i < 4; i++) Cube("UpperDoor", t, V(18.15f + i * 0.7f, 1.95f, 13.52f), V(0.64f, 0.62f, 0.01f), M("UpperDoor", C("FFE0A8")));
            Cube("UpperE", t, V(21.7f, 1.95f, 9.8f), V(0.35f, 0.7f, 2.2f), upper, true);
            Cube("Backsplash", t, V(16.4f, 1.2f, 13.88f), V(8.4f, 0.55f, 0.02f), MTex("BacksplashTile", MakeTex("TileBath", 5), V2(14, 1), 0.5f));

            // ---- Stove + hood + burning pot
            var st = G("Stove", t, V(14.5f, 0, 13.5f));
            Cube("Body", st, V(14.5f, 0.45f, 13.5f), V(1.0f, 0.9f, 0.7f), M("StoveBody", C("8FE3D6")), true);
            Cube("Top", st, V(14.5f, 0.915f, 13.5f), V(1.0f, 0.03f, 0.7f), M("StoveTop", C("2E3338"), 0.6f), true);
            Cube("OvenDoor", st, V(14.5f, 0.42f, 13.14f), V(0.8f, 0.55f, 0.02f), M("OvenDoor", C("7ACFC2")));
            Cube("OvenWindow", st, V(14.5f, 0.45f, 13.13f), V(0.5f, 0.25f, 0.01f), M("Black", C("1A1A1F"), 0.8f));
            Cyl("OvenHandle", st, V(14.5f, 0.74f, 13.1f), V(0.03f, 0.33f, 0.03f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f), false, V(0, 0, 90));
            Transform knobT = null;
            for (int i = 0; i < 4; i++)
            {
                var k = G("Knob" + i, st, V(14.17f + i * 0.22f, 0.84f, 13.13f));
                Cyl("Dial", k, V(14.17f + i * 0.22f, 0.84f, 13.13f), V(0.07f, 0.02f, 0.07f), M("Black", C("1A1A1F"), 0.8f), false, V(90, 0, 0));
                Cube("Mark", k, V(14.17f + i * 0.22f, 0.865f, 13.115f), V(0.01f, 0.03f, 0.005f), M("White", C("FFFFFF")));
                if (i == 0) knobT = k;
            }
            Vector3 burnerPos = V(14.27f, 0.94f, 13.35f);
            foreach (var bp in new[] { burnerPos, V(14.73f, 0.94f, 13.35f), V(14.27f, 0.94f, 13.68f), V(14.73f, 0.94f, 13.68f) })
                Cyl("Burner", st, bp, V(0.24f, 0.01f, 0.24f), M("Burner", C("101214"), 0.5f));
            var burner = G("BurnerPoint", st, burnerPos);
            var flame = G("GasFlame", st, burnerPos);
            Cyl("Ring", flame, burnerPos + V(0, 0.01f, 0), V(0.22f, 0.012f, 0.22f), ME("GasBlue", C("6FB6FF"), new Color(0.4f, 1.0f, 3f)));
            Cube("Hood", t, V(14.5f, 2.0f, 13.62f), V(1.05f, 0.25f, 0.55f), M("Hood", C("C8CDD3"), 0.7f, 0.6f), true);
            Cube("HoodChimney", t, V(14.5f, 2.5f, 13.78f), V(0.4f, 0.75f, 0.25f), M("Hood", C("C8CDD3"), 0.7f, 0.6f));

            // the curry pot (grease fire)
            pot = Prop<CookPot>("CurryPot", burnerPos + V(0, 0.01f, 0), 1.6f, "Curry pot");
            var potMat = M("PotMetal", C("FFFFFF"), 0.6f, 0.4f);
            var pb = L(PrimitiveType.Cylinder, "Body", pot.transform, V(0, 0.13f, 0), V(0.4f, 0.13f, 0.4f), potMat);
            var pr = L(PrimitiveType.Cylinder, "Rim", pot.transform, V(0, 0.26f, 0), V(0.43f, 0.015f, 0.43f), potMat);
            L(PrimitiveType.Cube, "HandleL", pot.transform, V(-0.26f, 0.2f, 0), V(0.11f, 0.035f, 0.05f), M("Black", C("1A1A1F"), 0.8f));
            L(PrimitiveType.Cube, "HandleR", pot.transform, V(0.26f, 0.2f, 0), V(0.11f, 0.035f, 0.05f), M("Black", C("1A1A1F"), 0.8f));
            var curry = L(PrimitiveType.Cylinder, "Curry", pot.transform, V(0, 0.25f, 0), V(0.37f, 0.01f, 0.37f), M("BurntCurry", C("4A2510"), 0.3f));
            BoxCol(pot.gameObject, V(0, 0.14f, 0), V(0.45f, 0.28f, 0.45f));
            pot.renderers = new[] { pb.GetComponent<Renderer>(), pr.GetComponent<Renderer>() };
            pot.curry = curry; pot.burn = 0.45f; pot.tool = ToolType.Pot; pot.holdOffset = V(0, -0.15f, 0.1f);
            pot.lidSocket = G("LidSocket", pot.transform, pot.transform.position + V(0, 0.27f, 0));
            pot.interactRadius = 1.5f;
            pot.fire = MakeFire("Pot fire", pot.transform.position + V(0, 0.28f, 0), FireType.Grease, 0.28f, 0.9f, pot.transform, 1.0f);
            pot.fire.growth = 0.007f; pot.fire.spreadThreshold = 0.6f; pot.fire.spreadDelay = 18f; pot.fire.smokeRate = 1.8f;

            // things the fire can spread to
            var dishTowel = G("DishTowelHook", t, V(13.55f, 1.3f, 13.8f));
            Cube("Towel", dishTowel, V(13.55f, 1.1f, 13.78f), V(0.3f, 0.45f, 0.02f), M("DishTowel", C("FF6F61")));
            Cube("TowelStripe", dishTowel, V(13.55f, 1.0f, 13.77f), V(0.3f, 0.05f, 0.005f), M("White", C("FFFFFF")));
            var f2 = MakeFire("Dish towel", V(13.55f, 1.15f, 13.7f), FireType.Ordinary, 0.18f, 0.7f, dishTowel, 0.8f);
            f2.charRenderers = dishTowel.GetComponentsInChildren<Renderer>(); f2.growth = 0.015f; f2.spreadDelay = 16f;
            var f3 = MakeFire("Kitchen cabinet", V(13.1f, 1.8f, 13.6f), FireType.Ordinary, 0.6f, 1.2f, t, 1.3f);
            f3.charRenderers = new[] { t.Find("UpperA").GetComponent<Renderer>() }; f3.growth = 0.01f; f3.smokeRate = 1.4f;
            pot.fire.spreadTo.Add(f2); f2.spreadTo.Add(f3); pot.fire.spreadTo.Add(f3);
            var scorch = Cube("Scorch", t, V(14.5f, 1.25f, 13.865f), V(0.9f, 0.6f, 0.005f), M("Soot", C("2A1E1A")));
            scorch.SetActive(false); pot.fire.scorch = scorch;

            stove = st.gameObject.AddComponent<Stove>();
            stove.displayName = "Stove"; stove.burner = burner; stove.flame = flame.gameObject; stove.pot = pot; stove.knob = knobT;
            stove.smokeColumn = MakeSmokeColumn(st, burnerPos + V(0, 0.9f, 0));
            stove.igniteAfter = 45f; stove.interactRadius = 1.4f; stove.interactAnchor = G("Anchor", st, V(14.5f, 0, 12.8f));

            // lid on the counter, right next to the stove
            var lid = Prop<Lid>("Lid", V(15.55f, 0.94f, 13.5f), 0.4f, "Pot lid");
            L(PrimitiveType.Cylinder, "Lid", lid.transform, V(0, 0.01f, 0), V(0.44f, 0.012f, 0.44f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            L(PrimitiveType.Sphere, "Knob", lid.transform, V(0, 0.04f, 0), V(0.07f, 0.05f, 0.07f), M("Black", C("1A1A1F"), 0.8f));
            BoxCol(lid.gameObject, V(0, 0.015f, 0), V(0.42f, 0.03f, 0.42f));
            lid.tool = ToolType.Lid; lid.holdOffset = V(0, 0, 0.05f); lid.holdEuler = V(-70, 0, 0);
            AtHome(lid);

            // ---- Sink under the north window
            var sk = G("Sink", t, V(17.0f, 0, 13.5f));
            Cube("SinkBody", sk, V(17.0f, 0.42f, 13.5f), V(1.2f, 0.84f, 0.7f), body, true);
            Cube("RimF", sk, V(17.0f, 0.89f, 13.18f), V(1.2f, 0.06f, 0.1f), top, true);
            Cube("RimB", sk, V(17.0f, 0.89f, 13.82f), V(1.2f, 0.06f, 0.1f), top, true);
            Cube("RimL", sk, V(16.47f, 0.89f, 13.5f), V(0.14f, 0.06f, 0.56f), top, true);
            Cube("RimR", sk, V(17.53f, 0.89f, 13.5f), V(0.14f, 0.06f, 0.56f), top, true);
            Cube("Basin", sk, V(17.0f, 0.78f, 13.5f), V(0.92f, 0.02f, 0.5f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cyl("Tap", sk, V(17.0f, 1.05f, 13.82f), V(0.04f, 0.15f, 0.04f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cube("Spout", sk, V(17.0f, 1.19f, 13.72f), V(0.04f, 0.04f, 0.22f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            var kstream = Cyl("Stream", sk, V(17.0f, 1.0f, 13.62f), V(0.03f, 0.18f, 0.03f), MT("WaterStream", new Color(0.6f, 0.85f, 1f, 0.6f), 0.95f));
            Cube("Dishes", sk, V(16.85f, 0.82f, 13.5f), V(0.3f, 0.06f, 0.3f), M("Plate", C("FBF7EE"), 0.5f));
            Cube("SinkDoor", sk, V(16.72f, 0.45f, 13.14f), V(0.54f, 0.6f, 0.02f), door);
            Cube("SinkDoor", sk, V(17.28f, 0.45f, 13.14f), V(0.54f, 0.6f, 0.02f), door);
            var ks = sk.gameObject.AddComponent<KitchenSink>();
            ks.displayName = "Kitchen sink"; ks.hasTap = true; ks.stream = kstream; ks.interactRadius = 1.4f;
            ks.interactAnchor = G("Anchor", sk, V(17.0f, 0, 12.8f));
            Hide(sk, "Under-sink cabinet", "under the sink", 0.35f, V(17.0f, 0.4f, 13.5f), V(1.0f, 0.6f, 0.5f), V(17.0f, 0.15f, 13.5f), false).interactAnchor = G("Anchor2", sk, V(17.4f, 0, 12.9f));

            // ---- Fridge
            var fr = G("Fridge", t, V(21.25f, 0, 13.4f));
            Cube("Body", fr, V(21.25f, 1.0f, 13.4f), V(1.0f, 2.0f, 0.85f), M("Fridge", C("F4F08A"), 0.4f), true);
            Cube("Split", fr, V(21.25f, 1.32f, 12.97f), V(0.96f, 0.01f, 0.02f), M("FridgeLine", C("C9C46A")));
            Cyl("Handle", fr, V(20.9f, 1.0f, 12.94f), V(0.04f, 0.25f, 0.04f), M("HandleTeal", C("43CFC4"), 0.5f));
            Cyl("Handle", fr, V(20.9f, 1.65f, 12.94f), V(0.04f, 0.15f, 0.04f), M("HandleTeal", C("43CFC4"), 0.5f));
            Color[] notes = { C("FF8FB1"), C("FFE45C"), C("8FD3F5"), C("B8E986") };
            for (int i = 0; i < 7; i++) Cube("Note", fr, V(20.95f + (i % 4) * 0.15f, 0.9f + (i / 4) * 0.3f + (i % 2) * 0.05f, 12.97f), V(0.1f, 0.1f, 0.005f), M("Note" + (i % 4), notes[i % 4]), false, V(0, 0, (i * 37) % 20 - 10));

            // ---- Pantry (hiding spot) on the west wall
            var pa = G("Pantry", t, V(12.45f, 0, 9.9f));
            var pw = M("PantryWood", C("E8873A"));
            Cube("Back", pa, V(12.12f, 1.0f, 9.9f), V(0.04f, 2.0f, 1.2f), pw, true);
            Cube("SideA", pa, V(12.45f, 1.0f, 9.32f), V(0.66f, 2.0f, 0.04f), pw, true);
            Cube("SideB", pa, V(12.45f, 1.0f, 10.48f), V(0.66f, 2.0f, 0.04f), pw, true);
            Cube("Top", pa, V(12.45f, 1.98f, 9.9f), V(0.66f, 0.04f, 1.2f), pw, true);
            Cube("Bottom", pa, V(12.45f, 0.05f, 9.9f), V(0.66f, 0.1f, 1.2f), pw, true);
            Cube("Shelf", pa, V(12.45f, 1.0f, 9.9f), V(0.6f, 0.03f, 1.12f), pw, true);
            Cube("Shelf", pa, V(12.45f, 1.5f, 9.9f), V(0.6f, 0.03f, 1.12f), pw, true);
            Color[] jc = { C("FFE07A"), C("FF8FA3"), C("8FD3F5"), C("B8E986") };
            for (int i = 0; i < 4; i++)
            {
                Cyl("Jar", pa, V(12.4f, 1.12f, 9.5f + i * 0.25f), V(0.14f, 0.1f, 0.14f), M("Jar" + i, jc[i]));
                Cube("Box", pa, V(12.4f, 1.65f, 9.5f + i * 0.25f), V(0.14f, 0.26f, 0.18f), M("Cereal" + i, jc[(i + 2) % 4]));
            }
            var pivP = G("DoorPivot", pa, V(12.78f, 1.0f, 9.32f));
            Cube("Door", pivP, V(12.78f, 1.0f, 9.9f), V(0.04f, 1.95f, 1.14f), M("PantryDoor", C("F29B4F")), true);
            Sph("Knob", pivP, V(12.82f, 1.0f, 10.35f), Vector3.one * 0.07f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            var pcab = Hide(pa, "Pantry", "in the pantry", 0.3f, V(12.45f, 0.5f, 9.9f), V(0.55f, 0.8f, 1.05f), V(12.45f, 0.2f, 9.9f), true);
            pcab.doorPivot = pivP; pcab.openAngle = 100f; pcab.interactAnchor = G("Anchor", pa, V(13.3f, 0, 9.9f));

            // ---- Kitchen table
            var tableTop = M("TableTop", C("FFBE5C")); var wood = M("Wood", C("C47A45"));
            Cube("TableTop", t, V(17.3f, 0.75f, 10.4f), V(1.6f, 0.06f, 1.0f), tableTop, true);
            Cube("TableCloth", t, V(17.3f, 0.783f, 10.4f), V(0.7f, 0.004f, 1.04f), M("TableCloth", C("FF8FA3")));
            foreach (var p in new[] { V(16.6f, 0.36f, 10.0f), V(18.0f, 0.36f, 10.0f), V(16.6f, 0.36f, 10.8f), V(18.0f, 0.36f, 10.8f) }) Cyl("Leg", t, p, V(0.08f, 0.36f, 0.08f), wood, true);
            MakeChair(V(15.95f, 0, 10.4f), 90f, "Chair");
            MakeChair(V(18.65f, 0, 10.4f), -90f, "Chair");
            MakeChair(V(17.3f, 0, 9.4f), 0f, "Chair");

            // ---- Fan (aim it at the open window!)
            var fanRoot = G("Fan", t, V(13.0f, 0, 8.8f));
            var white = M("FanBody", C("F5F5F0"), 0.5f);
            Cyl("Base", fanRoot, V(13.0f, 0.03f, 8.8f), V(0.42f, 0.03f, 0.42f), white, true);
            Cyl("Pole", fanRoot, V(13.0f, 0.55f, 8.8f), V(0.06f, 0.5f, 0.06f), white, true);
            var yawRoot = G("Yaw", fanRoot, V(13.0f, 1.08f, 8.8f));
            yawRoot.rotation = Quaternion.Euler(0, 50f, 0);
            var head = G("Head", yawRoot, V(13.0f, 1.08f, 8.8f));
            head.localRotation = Quaternion.identity;
            L(PrimitiveType.Sphere, "Motor", head, V(0, 0, -0.06f), V(0.2f, 0.2f, 0.26f), white);
            L(PrimitiveType.Cylinder, "Cage", head, V(0, 0, 0.1f), V(0.62f, 0.008f, 0.62f), MT("FanCage", new Color(0.6f, 0.85f, 0.95f, 0.35f), 0.6f), false, V(90, 0, 0));
            var blades = new GameObject("Blades").transform; blades.SetParent(head, false); blades.localPosition = V(0, 0, 0.04f);
            for (int i = 0; i < 3; i++) L(PrimitiveType.Cube, "Blade", blades, V(Mathf.Sin(i * 2.094f) * 0.13f, Mathf.Cos(i * 2.094f) * 0.13f, 0), V(0.11f, 0.24f, 0.01f), M("FanBlade", C("7FD3F0"), 0.5f), false, V(0, 0, -i * 120f));
            var fan = fanRoot.gameObject.AddComponent<Fan>();
            fan.displayName = "Fan"; fan.head = head; fan.blades = blades; fan.interactRadius = 1.4f; fan.range = 6f;

            Cyl("TrashBin", t, V(20.0f, 0.3f, 8.5f), V(0.4f, 0.3f, 0.4f), M("Bin", C("4CAF6E")), true);
            Plant(t, V(13.7f, 0.92f, 13.55f), 0.6f, C("D9534F"));
            Plant(t, V(19.9f, 0.92f, 13.6f), 0.55f, C("4FA3E0"));
            Cube("KitchenMat", t, V(17.0f, 0.006f, 12.75f), V(1.2f, 0.012f, 0.5f), M("RugPink", C("FF6F9C")));
            Clock(t, V(18.3f, 2.45f, 8.09f), Vector3.forward);
            Frame(t, V(20.5f, 1.8f, 8.09f), 0.6f, 0.45f, Vector3.forward, C("FF7AA8"));
            PointLight("KitchenLight", V(17f, 2.6f, 11f), C("FFE2B8"), 9f, 1.5f, gRooms);
        }

        static void CounterX(Transform t, float x0, float x1, float z, Material body, Material top, Material door, Material knob)
        {
            float cx = (x0 + x1) / 2, len = x1 - x0;
            Cube("CounterBody", t, V(cx, 0.43f, z), V(len, 0.86f, 0.7f), body, true);
            Cube("CounterTop", t, V(cx, 0.89f, z - 0.02f), V(len, 0.05f, 0.76f), top, true);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.6f)); float w = len / n;
            for (int i = 0; i < n; i++)
            {
                float px = x0 + w * (i + 0.5f);
                Cube("Door", t, V(px, 0.47f, z - 0.36f), V(w - 0.06f, 0.66f, 0.02f), door);
                Sph("Knob", t, V(px + (i % 2 == 0 ? 0.15f : -0.15f), 0.68f, z - 0.38f), Vector3.one * 0.05f, knob);
            }
        }

        static void CounterZ(Transform t, float z0, float z1, float x, Material body, Material top, Material door, Material knob)
        {
            float cz = (z0 + z1) / 2, len = z1 - z0;
            Cube("CounterBody", t, V(x, 0.43f, cz), V(0.7f, 0.86f, len), body, true);
            Cube("CounterTop", t, V(x - 0.02f, 0.89f, cz), V(0.76f, 0.05f, len), top, true);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.6f)); float w = len / n;
            for (int i = 0; i < n; i++)
            {
                float pz = z0 + w * (i + 0.5f);
                Cube("Door", t, V(x - 0.36f, 0.47f, pz), V(0.02f, 0.66f, w - 0.06f), door);
                Sph("Knob", t, V(x - 0.38f, 0.68f, pz + (i % 2 == 0 ? 0.15f : -0.15f)), Vector3.one * 0.05f, knob);
            }
            Cyl("Kettle", t, V(x, 1.02f, z0 + 0.6f), V(0.2f, 0.12f, 0.2f), M("Kettle", C("FF6F61"), 0.6f));
            Cube("Toaster", t, V(x, 1.0f, z0 + 1.5f), V(0.25f, 0.18f, 0.35f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cube("CuttingBoard", t, V(x, 0.925f, z0 + 2.6f), V(0.35f, 0.02f, 0.5f), M("LightWood", C("E8B27A")));
            Cube("Microwave", t, V(x + 0.05f, 1.06f, z1 - 0.5f), V(0.4f, 0.3f, 0.55f), M("Microwave", C("FFFFFF")));
        }

        // =====================================================================  LAUNDRY (x13..17, z0..6) - TOOLS
        static void Laundry()
        {
            var t = G("Laundry", gRooms);
            var white = M("Appliance", C("F7F7F2"), 0.5f);
            // washer (hiding spot) + dryer, front facing north
            var wa = G("Washer", t, V(13.75f, 0, 0.5f));
            Cube("Body", wa, V(13.75f, 0.43f, 0.45f), V(0.75f, 0.86f, 0.7f), white, true);
            Cube("Panel", wa, V(13.75f, 0.8f, 0.82f), V(0.73f, 0.1f, 0.03f), M("PanelBlue", C("8FD3F5")));
            var pivWa = G("DoorPivot", wa, V(13.55f, 0.45f, 0.82f));
            Cyl("Door", pivWa, V(13.75f, 0.45f, 0.82f), V(0.42f, 0.02f, 0.42f), MT("WasherGlass", new Color(0.7f, 0.85f, 0.95f, 0.6f), 0.9f), true, V(90, 0, 0));
            Cyl("DoorRing", pivWa, V(13.75f, 0.45f, 0.815f), V(0.46f, 0.01f, 0.46f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f), false, V(90, 0, 0));
            var wcab = Hide(wa, "Washing machine", "in the washing machine", 0.25f, V(13.75f, 0.45f, 0.45f), V(0.55f, 0.5f, 0.5f), V(13.75f, 0.35f, 0.45f), true);
            wcab.doorPivot = pivWa; wcab.openAngle = -110f; wcab.interactAnchor = G("Anchor", wa, V(13.75f, 0, 1.35f));
            Cube("Dryer", t, V(14.6f, 0.43f, 0.45f), V(0.75f, 0.86f, 0.7f), white, true);
            Cyl("DryerDoor", t, V(14.6f, 0.45f, 0.82f), V(0.42f, 0.02f, 0.42f), M("DryerDoor", C("C8CDD3"), 0.7f), false, V(90, 0, 0));
            Cube("DetergentShelf", t, V(14.2f, 1.6f, 0.2f), V(1.6f, 0.04f, 0.3f), M("ShelfGreen", C("4FBF6A")), true);
            Cube("Detergent", t, V(13.8f, 1.75f, 0.2f), V(0.2f, 0.26f, 0.12f), M("Detergent", C("FF6F61")));
            Cube("Detergent", t, V(14.15f, 1.72f, 0.2f), V(0.15f, 0.2f, 0.1f), M("Detergent2", C("43CFC4")));

            // utility sink
            var us = G("Utility sink", t, V(16.45f, 0, 0.5f));
            Cube("Tub", us, V(16.45f, 0.45f, 0.45f), V(0.75f, 0.9f, 0.6f), M("Utility", C("D8DCE0"), 0.6f), true);
            Cyl("Tap", us, V(16.45f, 1.05f, 0.2f), V(0.04f, 0.15f, 0.04f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            var dumpU = us.gameObject.AddComponent<WaterDump>(); dumpU.displayName = "Utility sink"; dumpU.hasTap = true; dumpU.interactRadius = 1.3f;
            dumpU.interactAnchor = G("Anchor", us, V(16.45f, 0, 1.2f));

            // MAIN WATER VALVE on the west wall
            var vg = G("Main water valve", t, V(13.15f, 0.8f, 3.0f));
            var copper = M("Copper", C("D9814A"), 0.7f, 0.7f);
            Cyl("PipeV", vg, V(13.13f, 1.0f, 3.0f), V(0.07f, 1.0f, 0.07f), copper);
            Cyl("PipeH", vg, V(13.13f, 0.8f, 3.25f), V(0.06f, 0.25f, 0.06f), copper, false, V(90, 0, 0));
            Cube("ValveBody", vg, V(13.18f, 0.8f, 3.0f), V(0.14f, 0.14f, 0.14f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
            var wheel = G("Wheel", vg, V(13.3f, 0.8f, 3.0f));
            wheel.rotation = Quaternion.Euler(0, 90, 0);
            L(PrimitiveType.Cylinder, "Rim", wheel, Vector3.zero, V(0.3f, 0.02f, 0.3f), M("ValveRed", C("E53935"), 0.4f), false, V(90, 0, 0));
            L(PrimitiveType.Cube, "Spoke", wheel, Vector3.zero, V(0.28f, 0.03f, 0.03f), M("ValveRed", C("E53935"), 0.4f));
            L(PrimitiveType.Cube, "Spoke", wheel, Vector3.zero, V(0.03f, 0.28f, 0.03f), M("ValveRed", C("E53935"), 0.4f));
            Cube("Tag", vg, V(13.1f, 1.15f, 3.0f), V(0.01f, 0.12f, 0.22f), M("TagYellow", C("FFD447")));
            valve = vg.gameObject.AddComponent<MainValve>();
            valve.displayName = "Main valve"; valve.wheel = wheel; valve.interactRadius = 1.4f; valve.interactAnchor = G("Anchor", vg, V(13.75f, 0, 3.0f));
            tap.mainValve = valve;

            // towel shelf (east wall)
            Cube("TowelShelf", t, V(16.75f, 0.9f, 2.8f), V(0.4f, 0.04f, 1.0f), M("ShelfWhite", C("FFFFFF")), true);
            Cube("TowelShelf2", t, V(16.75f, 1.4f, 2.8f), V(0.4f, 0.04f, 1.0f), M("ShelfWhite", C("FFFFFF")), true);
            MakeTowel(V(16.75f, 0.95f, 2.5f), V(0, 0, 0), C("43CFC4"), false, "Towel");
            MakeTowel(V(16.75f, 0.95f, 3.1f), V(0, 0, 0), C("FF8FB1"), false, "Towel");
            MakeTowel(V(16.75f, 1.45f, 2.8f), V(0, 0, 0), C("B39DDB"), false, "Big towel");

            // FIRE EXTINGUISHER on the wall by the door
            var ex = Prop<Extinguisher>("Fire extinguisher", V(16.8f, 0.45f, 5.3f), 4f, "Fire extinguisher");
            L(PrimitiveType.Capsule, "Body", ex.transform, V(0, 0.25f, 0), V(0.16f, 0.25f, 0.16f), M("ExtRed", C("D62828"), 0.6f));
            L(PrimitiveType.Cube, "Head", ex.transform, V(0, 0.55f, 0), V(0.08f, 0.08f, 0.12f), M("Black", C("1A1A1F"), 0.8f));
            L(PrimitiveType.Cube, "Label", ex.transform, V(0, 0.25f, 0.075f), V(0.1f, 0.12f, 0.01f), M("White", C("FFFFFF")));
            var nozzle = new GameObject("Nozzle").transform; nozzle.SetParent(ex.transform, false); nozzle.localPosition = V(0, 0.62f, 0f); nozzle.localRotation = Quaternion.Euler(-90, 0, 0);
            BoxCol(ex.gameObject, V(0, 0.3f, 0), V(0.18f, 0.6f, 0.18f));
            ex.nozzle = nozzle; ex.powderMat = powderMat; ex.tool = ToolType.Extinguisher;
            ex.holdOffset = V(0, -0.35f, 0.05f); ex.holdEuler = V(80, 0, 0); ex.startKinematic = true;
            ex.spray = MakeSpray(nozzle);
            Cube("ExtBracket", t, V(16.92f, 0.6f, 5.3f), V(0.04f, 0.3f, 0.12f), M("Black", C("1A1A1F"), 0.8f));
            Cube("ExtSign", t, V(16.92f, 1.4f, 5.3f), V(0.01f, 0.25f, 0.25f), M("ExtSign", C("D62828")));

            // cleaning gear corner
            var mop = Prop<Mop>("Mop", V(13.35f, 0f, 5.55f), 1.0f, "Mop");
            var mh = L(PrimitiveType.Cube, "Strings", mop.transform, V(0, 0.04f, 0), V(0.45f, 0.08f, 0.24f), M("MopHead", C("F5F2E6")));
            L(PrimitiveType.Cube, "Holder", mop.transform, V(0, 0.1f, 0), V(0.14f, 0.06f, 0.08f), M("MopBlue", C("4A7BE0")));
            L(PrimitiveType.Cylinder, "Stick", mop.transform, V(0, 0.85f, 0), V(0.035f, 0.75f, 0.035f), M("Wood", C("C47A45")));
            BoxCol(mop.gameObject, V(0, 0.05f, 0), V(0.45f, 0.1f, 0.24f));
            BoxCol(mop.gameObject, V(0, 0.85f, 0), V(0.05f, 1.5f, 0.05f));
            mop.head = mop.transform; mop.headRenderer = mh.GetComponent<Renderer>();
            mop.tool = ToolType.Mop; mop.dragOnFloor = true; mop.holdOffset = V(0, 0.02f, 0.1f); mop.holdEuler = V(-30, 0, 0); mop.startKinematic = true;
            mop.transform.rotation = Quaternion.Euler(-10, 0, 0);

            var sq = Prop<Squeegee>("Squeegee", V(13.75f, 0f, 5.65f), 0.8f, "Squeegee");
            L(PrimitiveType.Cube, "Blade", sq.transform, V(0, 0.04f, 0), V(0.6f, 0.08f, 0.05f), M("SqueegeeBlade", C("FFD447")));
            L(PrimitiveType.Cube, "Rubber", sq.transform, V(0, 0.005f, 0), V(0.6f, 0.02f, 0.03f), M("Black", C("1A1A1F"), 0.8f));
            L(PrimitiveType.Cylinder, "Stick", sq.transform, V(0, 0.8f, 0), V(0.03f, 0.75f, 0.03f), M("SqueegeeStick", C("43CFC4")));
            BoxCol(sq.gameObject, V(0, 0.04f, 0), V(0.6f, 0.08f, 0.05f));
            BoxCol(sq.gameObject, V(0, 0.8f, 0), V(0.04f, 1.5f, 0.04f));
            sq.blade = sq.transform; sq.tool = ToolType.Squeegee; sq.dragOnFloor = true; sq.holdOffset = V(0, 0.01f, 0.15f); sq.holdEuler = V(-35, 0, 0); sq.startKinematic = true;
            sq.transform.rotation = Quaternion.Euler(-10, 0, 0);

            var bucket = Prop<Bucket>("Bucket", V(13.9f, 0f, 4.7f), 2.5f, "Bucket");
            L(PrimitiveType.Cylinder, "Body", bucket.transform, V(0, 0.27f, 0), V(0.46f, 0.27f, 0.46f), M("BucketBlue", C("3D8BFF"), 0.4f));
            L(PrimitiveType.Cylinder, "Rim", bucket.transform, V(0, 0.54f, 0), V(0.5f, 0.02f, 0.5f), M("BucketRim", C("2A63C9")));
            L(PrimitiveType.Cube, "Handle", bucket.transform, V(0, 0.68f, 0), V(0.46f, 0.02f, 0.02f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            var bw = L(PrimitiveType.Cylinder, "Water", bucket.transform, V(0, 0.1f, 0), V(0.42f, 0.01f, 0.42f), MT("TubWater", new Color(0.45f, 0.72f, 0.95f, 0.75f), 0.95f));
            BoxCol(bucket.gameObject, V(0, 0.27f, 0), V(0.46f, 0.54f, 0.46f));
            bucket.waterVisual = bw.transform; bucket.liters = 0f; bucket.capacity = 10f; bucket.waterMinY = 0.04f; bucket.waterMaxY = 0.5f;
            bucket.tool = ToolType.Bucket; bucket.holdOffset = V(0, -0.42f, 0.08f);
            bucket.momLine = "Why is the mop bucket out?! What were you DOING?";
            AtHome(bucket);

            var lb = G("Laundry basket", t, V(15.7f, 0, 3.6f));
            Cube("Basket", lb, V(15.7f, 0.25f, 3.6f), V(0.6f, 0.5f, 0.45f), M("Wicker", C("D9B27C")), true);
            Cube("Clothes", lb, V(15.7f, 0.5f, 3.6f), V(0.5f, 0.08f, 0.38f), M("Shirt2", C("6CC63A")));
            Hide(lb, "Laundry basket", "in the laundry basket", 0.3f, V(15.7f, 0.3f, 3.6f), V(0.5f, 0.45f, 0.4f), V(15.7f, 0.42f, 3.6f), false);
            PointLight("LaundryLight", V(15f, 2.6f, 3f), C("EAF6FF"), 6f, 1.2f, gRooms);
        }

        // =====================================================================  DINING (x17..22, z0..6)
        static void Dining()
        {
            var t = G("Dining", gRooms);
            var wood = M("DiningWood", C("A0623A"));
            Cube("Table", t, V(19.5f, 0.75f, 2.8f), V(1.9f, 0.06f, 1.0f), wood, true);
            foreach (var p in new[] { V(18.7f, 0.36f, 2.4f), V(20.3f, 0.36f, 2.4f), V(18.7f, 0.36f, 3.2f), V(20.3f, 0.36f, 3.2f) }) Cube("Leg", t, p, V(0.07f, 0.72f, 0.07f), wood, true);
            Cube("Runner", t, V(19.5f, 0.785f, 2.8f), V(1.7f, 0.005f, 0.3f), M("TableRunner", C("81B29A")));
            Cyl("Candle", t, V(19.3f, 0.88f, 2.8f), V(0.06f, 0.1f, 0.06f), M("Candle", C("FFF6DD")));
            Cyl("Candle", t, V(19.7f, 0.9f, 2.8f), V(0.06f, 0.12f, 0.06f), M("Candle", C("FFF6DD")));
            MakeChair(V(18.9f, 0, 1.85f), 0f, "Chair"); MakeChair(V(20.1f, 0, 1.85f), 0f, "Chair");
            MakeChair(V(18.9f, 0, 3.75f), 180f, "Chair"); MakeChair(V(20.1f, 0, 3.75f), 180f, "Chair");
            var cab = M("China", C("FFF1D6"));
            Cube("ChinaCabinet", t, V(21.65f, 1.0f, 3.0f), V(0.5f, 2.0f, 1.4f), cab, true);
            Cube("ChinaGlass", t, V(21.39f, 1.4f, 3.0f), V(0.01f, 0.9f, 1.25f), MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f));
            for (int r = 0; r < 2; r++) for (int i = 0; i < 4; i++)
                    Cyl("Plate", t, V(21.55f, 1.15f + r * 0.4f, 2.55f + i * 0.3f), V(0.25f, 0.006f, 0.25f), M("Plate", C("FBF7EE"), 0.5f), false, V(0, 0, 80));
            Plant(t, V(17.5f, 0, 0.5f), 1.8f, C("E0623B"));
            Frame(t, V(19.5f, 1.8f, 5.92f), 1.1f, 0.6f, Vector3.back, C("FFC93C"));
            PointLight("DiningLight", V(19.5f, 2.6f, 2.8f), C("FFD9A8"), 7f, 1.3f, gRooms);
            Sph("Chandelier", t, V(19.5f, 2.45f, 2.8f), V(0.5f, 0.25f, 0.5f), ME("Chandelier", C("FFF1C9"), new Color(1.4f, 1.1f, 0.7f)));
        }

        // =====================================================================  OUTSIDE
        static void Outside()
        {
            Cube("Ground", gOutside, V(11f, -0.35f, 7f), V(70f, 0.2f, 50f), M("Grass", C("86CF5A")), true);
            Cube("Porch", gOutside, V(8.6f, -0.1f, -1.3f), V(2.6f, 0.2f, 2.4f), M("Porch", C("EBD8BC")), true);
            Cube("Path", gOutside, V(8.6f, -0.24f, -5.5f), V(1.4f, 0.04f, 6f), M("PathStone", C("D9C7A6")));
            var bush = M("Bush", C("4FAF4F")); var bush2 = M("Bush2", C("6CC63A"));
            Vector3[] b = { V(3f, 0.45f, -0.9f), V(5f, 0.4f, -0.9f), V(12f, 0.45f, -0.9f), V(15f, 0.5f, -0.9f), V(-0.9f, 0.5f, 3f), V(30.9f, 0.5f, 4f), V(4f, 0.5f, 14.9f), V(18f, 0.5f, 14.9f), V(-0.9f, 0.5f, 11f) };
            for (int i = 0; i < b.Length; i++) Sph("Bush", gOutside, b[i], V(1.2f, 0.9f, 1.0f), i % 2 == 0 ? bush : bush2);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = V(-4f + i * 9f, 0f, 18f);
                Cyl("Trunk", gOutside, p + V(0, 1f, 0), V(0.35f, 1f, 0.35f), M("Trunk", C("8A5A3A")));
                Sph("Crown", gOutside, p + V(0, 2.8f, 0), V(2.6f, 2.4f, 2.6f), bush2);
            }
            Cube("Mailbox", gOutside, V(10.5f, 0.6f, -4f), V(0.3f, 0.25f, 0.45f), M("Mailbox", C("4A7BE0")));
            Cyl("MailPost", gOutside, V(10.5f, 0.25f, -4f), V(0.06f, 0.25f, 0.06f), M("WoodDark", C("8A4B2A")));
        }

        // =====================================================================  CLUTTER (stuff that got knocked around)
        static void Clutter()
        {
            // living room
            var ctrl = Prop<MessyProp>("Game controller", V(4.6f, 0.03f, 2.2f), 0.2f, "Game controller");
            L(PrimitiveType.Cube, "Body", ctrl.transform, V(0, 0.02f, 0), V(0.16f, 0.04f, 0.1f), M("Black", C("1A1A1F"), 0.8f));
            L(PrimitiveType.Sphere, "GripL", ctrl.transform, V(-0.07f, 0.02f, -0.03f), V(0.06f, 0.05f, 0.08f), M("Black", C("1A1A1F"), 0.8f));
            L(PrimitiveType.Sphere, "GripR", ctrl.transform, V(0.07f, 0.02f, -0.03f), V(0.06f, 0.05f, 0.08f), M("Black", C("1A1A1F"), 0.8f));
            BoxCol(ctrl.gameObject, V(0, 0.02f, 0), V(0.2f, 0.04f, 0.1f));
            SetHome(ctrl, V(0.45f, 0.62f, 3.3f), V(0, 90, 0), V(4.6f, 0.03f, 2.2f), V(0, 30, 0));
            ctrl.momLine = "Were you playing video games ALL afternoon?!";

            var chips = Prop<MessyProp>("Chips bag", V(3.5f, 0.05f, 3.9f), 0.15f, "Chips bag");
            L(PrimitiveType.Cube, "Bag", chips.transform, V(0, 0.05f, 0), V(0.22f, 0.1f, 0.3f), M("Chips", C("FFD447")));
            BoxCol(chips.gameObject, V(0, 0.05f, 0), V(0.22f, 0.1f, 0.3f));
            SetHome(chips, V(20.0f, 0.15f, 8.5f), Vector3.zero, V(3.5f, 0.05f, 3.9f), V(0, 0, 90));
            chips.homeSet = true; chips.momLine = "Chips for lunch?! Where's the curry I made?";

            var teddy = Prop<MessyProp>("Teddy", V(7.2f, 0f, 3.6f), 0.4f, "Teddy bear");
            var fur = M("Teddy", C("B5793F"));
            L(PrimitiveType.Sphere, "Body", teddy.transform, V(0, 0.15f, 0), V(0.26f, 0.28f, 0.22f), fur);
            L(PrimitiveType.Sphere, "Head", teddy.transform, V(0, 0.36f, 0), V(0.2f, 0.19f, 0.18f), fur);
            L(PrimitiveType.Sphere, "EarL", teddy.transform, V(-0.08f, 0.45f, 0), V(0.07f, 0.07f, 0.04f), fur);
            L(PrimitiveType.Sphere, "EarR", teddy.transform, V(0.08f, 0.45f, 0), V(0.07f, 0.07f, 0.04f), fur);
            L(PrimitiveType.Sphere, "Snout", teddy.transform, V(0, 0.34f, 0.08f), V(0.08f, 0.06f, 0.06f), M("TeddySnout", C("F2D2A9")));
            BoxCol(teddy.gameObject, V(0, 0.2f, 0), V(0.24f, 0.4f, 0.2f));
            SetHome(teddy, V(1.2f, 0.66f, 12.9f), V(0, 180, 0), V(7.2f, 0f, 3.6f), V(0, 0, 80));
            teddy.momLine = "Why is Mr. Fluffy lying in the middle of the floor?";

            // hallway books & homework (fan will blow papers around)
            Color[] bk = { C("FF6F61"), C("43CFC4"), C("FFD447") };
            for (int i = 0; i < 3; i++)
            {
                var book = Prop<MessyProp>("Book", V(7.0f + i * 0.4f, 0.03f, 6.6f + (i % 2) * 0.3f), 0.3f, "Schoolbook");
                L(PrimitiveType.Cube, "Cover", book.transform, V(0, 0.02f, 0), V(0.2f, 0.04f, 0.27f), M("Book" + (10 + i), bk[i]));
                L(PrimitiveType.Cube, "Pages", book.transform, V(0.005f, 0.02f, 0), V(0.19f, 0.032f, 0.26f), M("Pages", C("FFFDF4")));
                BoxCol(book.gameObject, V(0, 0.02f, 0), V(0.2f, 0.04f, 0.27f));
                SetHome(book, V(4.4f + i * 0.25f, 0.79f + i * 0.045f, 13.45f), V(0, 90, 0), book.transform.position, V(0, i * 50f, 0));
            }
            var rnd = new System.Random(5);
            for (int i = 0; i < 5; i++)
            {
                var pos = V(16.8f + i * 0.2f, 0.79f + i * 0.006f, 10.2f + (float)rnd.NextDouble() * 0.4f);
                var paper = Prop<MessyProp>("Homework", pos, 0.03f, "Homework");
                L(PrimitiveType.Cube, "Sheet", paper.transform, V(0, 0.003f, 0), V(0.22f, 0.006f, 0.3f), M("Paper", C("FFFEF6")));
                L(PrimitiveType.Cube, "Scribble", paper.transform, V(0, 0.0065f, 0.05f), V(0.15f, 0.001f, 0.015f), M("Ink", C("4A7BE0")));
                L(PrimitiveType.Cube, "Grade", paper.transform, V(0.06f, 0.0065f, -0.09f), V(0.05f, 0.001f, 0.04f), M("RedPlastic", C("FF5A5A")));
                BoxCol(paper.gameObject, V(0, 0.003f, 0), V(0.22f, 0.008f, 0.3f));
                paper.isLight = true; paper.displaceTolerance = 0.5f;
                var rb = paper.GetComponent<Rigidbody>(); rb.linearDamping = 3f; rb.angularDamping = 2f;
                paper.momLine = "Is that your HOMEWORK all over the floor?!";
                AtHome(paper, V(0, (float)rnd.NextDouble() * 40f - 20f, 0));
            }
            // kitchen: things the smoke panic knocked over
            var bottle = Prop<MessyProp>("Soy sauce", V(15.6f, 0.06f, 12.3f), 0.5f, "Soy sauce");
            L(PrimitiveType.Cylinder, "Body", bottle.transform, V(0, 0.14f, 0), V(0.12f, 0.14f, 0.12f), M("BottleDark", C("3A1F0E"), 0.6f));
            L(PrimitiveType.Cylinder, "Cap", bottle.transform, V(0, 0.31f, 0), V(0.06f, 0.03f, 0.06f), M("RedPlastic", C("FF5A5A")));
            BoxCol(bottle.gameObject, V(0, 0.16f, 0), V(0.12f, 0.33f, 0.12f));
            SetHome(bottle, V(15.9f, 0.92f, 13.6f), Vector3.zero, V(15.6f, 0.06f, 12.3f), V(0, 30, 90));
            bottle.momLine = "Why is the soy sauce on the FLOOR?!";
            MakePlate(V(18.2f, 0.92f, 13.6f), V(18.6f, 0.02f, 12.0f));
            MakePlate(V(18.5f, 0.92f, 13.6f), V(13.9f, 0.02f, 11.2f));
            var bowl = Prop<MessyProp>("Fruit bowl", V(17.6f, 0.78f, 10.5f), 0.6f, "Fruit bowl");
            L(PrimitiveType.Cylinder, "Bowl", bowl.transform, V(0, 0.04f, 0), V(0.36f, 0.04f, 0.36f), M("BowlPink", C("FF8FB1"), 0.5f));
            L(PrimitiveType.Sphere, "Apple", bowl.transform, V(0.07f, 0.11f, 0.03f), V(0.11f, 0.1f, 0.11f), M("Apple", C("E8413C"), 0.5f));
            L(PrimitiveType.Sphere, "Orange", bowl.transform, V(-0.07f, 0.11f, -0.03f), V(0.11f, 0.11f, 0.11f), M("OrangeFruit", C("FF9A2E"), 0.4f));
            BoxCol(bowl.gameObject, V(0, 0.06f, 0), V(0.36f, 0.12f, 0.36f));
            AtHome(bowl);
        }

        static void MakePlate(Vector3 home, Vector3 start)
        {
            var plate = Prop<MessyProp>("Plate", start, 0.3f, "Plate");
            L(PrimitiveType.Cylinder, "Rim", plate.transform, V(0, 0.008f, 0), V(0.3f, 0.008f, 0.3f), M("PlateRim", C("4A7BE0"), 0.5f));
            L(PrimitiveType.Cylinder, "Plate", plate.transform, V(0, 0.012f, 0), V(0.25f, 0.008f, 0.25f), M("Plate", C("FBF7EE"), 0.5f));
            BoxCol(plate.gameObject, V(0, 0.01f, 0), V(0.28f, 0.02f, 0.28f));
            SetHome(plate, home, Vector3.zero, start, Vector3.zero);
        }

        static void MakeChair(Vector3 pos, float yaw, string name)
        {
            var ch = Prop<MessyProp>(name, pos, 4f, "Chair");
            var wood = M("ChairWood", C("FF7A59"));
            L(PrimitiveType.Cube, "Seat", ch.transform, V(0, 0.45f, 0), V(0.44f, 0.05f, 0.44f), wood, true);
            L(PrimitiveType.Cube, "Cushion", ch.transform, V(0, 0.485f, 0.01f), V(0.38f, 0.03f, 0.38f), M("ChairCushion", C("FFE07A")));
            foreach (var x in new[] { -0.18f, 0.18f }) foreach (var z in new[] { -0.18f, 0.18f }) L(PrimitiveType.Cube, "Leg", ch.transform, V(x, 0.21f, z), V(0.05f, 0.42f, 0.05f), wood, true);
            L(PrimitiveType.Cube, "Back", ch.transform, V(0, 0.8f, -0.2f), V(0.44f, 0.12f, 0.05f), wood, true);
            L(PrimitiveType.Cube, "PostL", ch.transform, V(-0.18f, 0.65f, -0.2f), V(0.05f, 0.4f, 0.05f), wood, true);
            L(PrimitiveType.Cube, "PostR", ch.transform, V(0.18f, 0.65f, -0.2f), V(0.05f, 0.4f, 0.05f), wood, true);
            ch.tool = ToolType.Chair; ch.dragOnFloor = true; ch.holdOffset = V(0, 0, 0.15f); ch.carrySpeedMultiplier = 0.75f; ch.displaceTolerance = 0.6f;
            ch.momLine = "Why is this chair all the way over HERE?";
            var crb = ch.GetComponent<Rigidbody>(); crb.automaticCenterOfMass = false; crb.centerOfMass = V(0, 0.2f, 0);
            var cov = ch.gameObject.AddComponent<CoverObject>(); cov.halfSize = V2(0.25f, 0.25f);
            AtHome(ch, V(0, yaw, 0));
        }

        static void MakeTowel(Vector3 pos, Vector3 euler, Color col, bool hanging, string name)
        {
            var tw = Prop<Towel>(name, pos, 0.5f, name);
            var cl = L(PrimitiveType.Cube, "Cloth", tw.transform, V(0, 0.03f, 0), hanging ? V(0.05f, 0.5f, 0.5f) : V(0.35f, 0.07f, 0.45f), M("Towel_" + ColorUtility.ToHtmlStringRGB(col), col));
            var st = L(PrimitiveType.Cube, "Stripe", tw.transform, hanging ? V(0, -0.1f, 0) : V(0, 0.067f, 0.15f), hanging ? V(0.055f, 0.05f, 0.5f) : V(0.35f, 0.005f, 0.05f), M("White", C("FFFFFF")));
            BoxCol(tw.gameObject, V(0, 0.03f, 0), hanging ? V(0.05f, 0.5f, 0.5f) : V(0.35f, 0.07f, 0.45f));
            tw.transform.rotation = Quaternion.Euler(euler);
            tw.tool = ToolType.Towel; tw.holdOffset = V(0, -0.1f, 0.1f); tw.startKinematic = true; tw.displaceTolerance = 99f;
            tw.cloth = new[] { cl.GetComponent<Renderer>(), st.GetComponent<Renderer>() };
            var cov = tw.GetComponent<CoverObject>();
            if (!cov) cov = tw.gameObject.AddComponent<CoverObject>();
            cov.halfSize = V2(0.45f, 0.45f); cov.absorbRate = 0.9f; cov.capacityLiters = 4f;
            cov.tintRenderers = tw.cloth;
            tw.ownFire = MakeFire(name + " fire", pos, FireType.Ordinary, 0.15f, 0.5f, tw.transform, 0.6f);
            tw.ownFire.growth = 0.03f;
            AtHome(tw, euler);
        }

        static Cabinet Hide(Transform root, string display, string phrase, float chance, Vector3 zoneCenter, Vector3 zoneSize, Vector3 stashPos, bool door)
        {
            var cab = root.gameObject.AddComponent<Cabinet>();
            cab.displayName = display; cab.stashPhrase = phrase; cab.interactRadius = 1.5f; cab.hasDoor = door;
            var zg = new GameObject("HideZone"); zg.transform.SetParent(root, false); zg.transform.position = zoneCenter;
            var bc = zg.AddComponent<BoxCollider>(); bc.size = zoneSize; bc.isTrigger = true;
            var hz = zg.AddComponent<HideZone>(); hz.cabinet = cab; hz.momCheckChance = chance;
            cab.zone = hz;
            cab.stashPoint = G("StashPoint", root, stashPos);
            return cab;
        }

        // =====================================================================  CHARACTERS
        static void Kid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "Kid (PF_Player)";
            go.transform.SetPositionAndRotation(V(4.4f, 0.02f, 3.0f), Quaternion.Euler(0, -90f, 0));
            foreach (var n in new[] { "DreamRush.UI.PlayerUIBridge", "DreamRush.Interaction.InteractionSystem", "DreamRush.Presentation.PlayerFeelFX" })
            {
                foreach (var c in go.GetComponents<MonoBehaviour>()) if (c && c.GetType().FullName == n) Object.DestroyImmediate(c);
            }
            var motor = go.GetComponent<DreamRush.Player.PlayerMotor3D>();
            string cfgPath = Root + "/KidMotorConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<DreamRush.Player.PlayerMotorConfig>(cfgPath);
            if (cfg == null)
            {
                cfg = Object.Instantiate(motor.Config);
                AssetDatabase.CreateAsset(cfg, cfgPath);
            }
            cfg.walkSpeed = 3.2f; cfg.sprintSpeed = 5.6f; cfg.jumpHeight = 0.8f; cfg.rotationSpeedDegrees = 600f;
            EditorUtility.SetDirty(cfg);
            motor.Config = cfg;

            var visual = go.transform.Find("Visual");
            var fall = new GameObject("FallPivot").transform;
            fall.SetParent(go.transform, false);
            visual.SetParent(fall, false);
            var hold = new GameObject("HoldPoint").transform; hold.SetParent(go.transform, false); hold.localPosition = V(0, 0.62f, 0.5f);
            var drag = new GameObject("DragPoint").transform; drag.SetParent(go.transform, false); drag.localPosition = V(0, 0f, 0.95f);
            kid = go.AddComponent<PlayerController>();
            kid.fallPivot = fall; kid.holdPoint = hold; kid.dragPoint = drag;
            kid.ghostMaterial = MT("PlaceGhost", new Color(0.3f, 0.85f, 0.95f, 0.45f), 0.2f);
            var act = go.AddComponent<KidActionAnimator>();
            act.player = kid;
            kid.actions = act;
        }

        static void Mom()
        {
            var root = new GameObject("Mom");
            root.transform.position = V(8.6f, 0f, -2.6f);
            var vis = new GameObject("Visual").transform; vis.SetParent(root.transform, false);
            var skin = M("Skin", C("FFD2B0"), 0.3f); var dress = M("MomDress", C("A777E3"), 0.2f); var hair = M("MomHair", C("F2852E"), 0.3f);
            L(PrimitiveType.Capsule, "LegL", vis, V(-0.1f, 0.15f, 0), V(0.12f, 0.12f, 0.12f), skin);
            L(PrimitiveType.Capsule, "LegR", vis, V(0.1f, 0.15f, 0), V(0.12f, 0.12f, 0.12f), skin);
            L(PrimitiveType.Cylinder, "Skirt", vis, V(0, 0.55f, 0), V(0.66f, 0.33f, 0.6f), dress);
            L(PrimitiveType.Cube, "Hem", vis, V(0, 0.27f, 0), V(0.66f, 0.05f, 0.6f), M("Hem", C("FFE07A")));
            L(PrimitiveType.Capsule, "Torso", vis, V(0, 1.08f, 0), V(0.48f, 0.3f, 0.4f), dress);
            L(PrimitiveType.Capsule, "ArmL", vis, V(-0.3f, 1.05f, 0.02f), V(0.12f, 0.24f, 0.12f), dress, false, V(0, 0, -12));
            L(PrimitiveType.Capsule, "ArmR", vis, V(0.3f, 1.05f, 0.02f), V(0.12f, 0.24f, 0.12f), dress, false, V(0, 0, 12));
            L(PrimitiveType.Cube, "GroceryBag", vis, V(0.44f, 0.62f, 0.1f), V(0.3f, 0.36f, 0.22f), M("PaperBag", C("C99A62")));
            L(PrimitiveType.Sphere, "Greens", vis, V(0.38f, 0.86f, 0.08f), V(0.13f, 0.15f, 0.13f), M("Leaf", C("3FAF4F")));
            L(PrimitiveType.Cube, "Handbag", vis, V(-0.42f, 0.78f, 0.05f), V(0.18f, 0.16f, 0.1f), M("Handbag", C("FFD447")));
            var head = new GameObject("Head").transform; head.SetParent(vis, false); head.localPosition = V(0, 1.42f, 0);
            L(PrimitiveType.Sphere, "Face", head, V(0, 0.12f, 0), Vector3.one * 0.38f, skin);
            L(PrimitiveType.Sphere, "HairCap", head, V(0, 0.2f, -0.04f), V(0.42f, 0.34f, 0.42f), hair);
            L(PrimitiveType.Sphere, "Bun", head, V(0, 0.4f, -0.1f), Vector3.one * 0.24f, hair);
            var gl = M("Glasses", C("E0413C"), 0.6f);
            L(PrimitiveType.Cylinder, "LensL", head, V(-0.08f, 0.13f, 0.18f), V(0.11f, 0.01f, 0.11f), gl, false, V(90, 0, 0));
            L(PrimitiveType.Cylinder, "LensR", head, V(0.08f, 0.13f, 0.18f), V(0.11f, 0.01f, 0.11f), gl, false, V(90, 0, 0));
            L(PrimitiveType.Sphere, "EyeL", head, V(-0.08f, 0.13f, 0.172f), V(0.04f, 0.05f, 0.02f), M("Eye", C("1A1416"), 0.8f));
            L(PrimitiveType.Sphere, "EyeR", head, V(0.08f, 0.13f, 0.172f), V(0.04f, 0.05f, 0.02f), M("Eye", C("1A1416"), 0.8f));
            L(PrimitiveType.Sphere, "Mouth", head, V(0, 0.03f, 0.175f), V(0.07f, 0.025f, 0.02f), M("Mouth", C("B5424A")));
            mom = root.AddComponent<MomController>();
            mom.visual = vis; mom.head = head;
            var rg = G("MomRoute", gSystems);
            mom.route = new[]
            {
                IP(rg, "Entrance", V(8.6f, 0, 1.3f), 4.0f, true, false, "I'm hooome~! Sweetie?"),
                IP(rg, "Living room", V(5.0f, 0, 2.9f), 4.5f, true, true, "Were you watching TV all day?"),
                IP(rg, "Hallway", V(5.0f, 0, 7.0f), 2.6f, true, false, null),
                IP(rg, "BedDoorOut", V(3.0f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Bedroom", V(3.0f, 0, 9.8f), 3.8f, true, true, "Let me see your room..."),
                IP(rg, "BedDoorBack", V(3.0f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Hall middle", V(9.5f, 0, 7.0f), 2.6f, true, false, null),
                IP(rg, "Bathroom", V(9.5f, 0, 9.6f), 3.6f, true, true, "I need to wash my hands..."),
                IP(rg, "BathBack", V(9.5f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Hall east", V(16.0f, 0, 7.0f), 2.6f, true, false, null),
                IP(rg, "Kitchen", V(16.0f, 0, 10.2f), 4.2f, true, true, "Let me put the groceries away..."),
                IP(rg, "Stove", V(14.6f, 0, 12.3f), 2.5f, true, false, "Now, how did the curry turn out?"),
                IP(rg, "KitchenOut", V(15.0f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Laundry", V(15.0f, 0, 4.0f), 2.8f, true, true, "Let me start a load of laundry..."),
                IP(rg, "LaundryOut", V(15.0f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Dining", V(19.5f, 0, 4.6f), 3.0f, true, false, null),
                IP(rg, "DiningOut", V(19.5f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Hall far east", V(24.5f, 0, 7.0f), 2.4f, true, false, null),
                IP(rg, "Game room", V(26.0f, 0, 10.6f), 4.4f, true, true, "What's this room doing like THIS?"),
                IP(rg, "GameOut", V(24.5f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Garage", V(24.0f, 0, 3.4f), 3.6f, true, true, "Let me put the bags in the garage..."),
                IP(rg, "GarageOut", V(23.9f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Back to hall", V(5.0f, 0, 7.0f), 0f, false, false, null),
                IP(rg, "Sofa", V(4.6f, 0, 2.6f), 3.0f, true, false, "Ahh~ finally, time to sit down..."),
            };
        }

        static MomController.InspectPoint IP(Transform parent, string n, Vector3 p, float r, bool inspect, bool hides, string line) =>
            new MomController.InspectPoint { name = n, point = G(n, parent, p), radius = r, inspect = inspect, checksHidingSpots = hides, arriveLine = line };

        // =====================================================================  SYSTEMS
        static void Systems()
        {
            // flood sim
            var fsGo = new GameObject("FloodSim"); fsGo.transform.SetParent(gSystems, false);
            var fs = fsGo.AddComponent<FloodSim>();
            fs.origin = V2(-0.5f, -0.5f); fs.cell = 0.2f; fs.width = 156; fs.height = 76;
            fs.walls = new List<Vector4>(blockRects);
            fs.groundRects = new List<Vector4>(groundRects); fs.groundOffsets = new List<float>(groundOffs);
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(plane.GetComponent<Collider>());
            plane.name = "WaterSurface"; plane.transform.SetParent(fsGo.transform, false);
            plane.transform.position = V(-0.5f + 156 * 0.2f / 2, 0.006f, -0.5f + 76 * 0.2f / 2);
            plane.transform.rotation = Quaternion.Euler(90, 0, 0);
            plane.transform.localScale = V(156 * 0.2f, 76 * 0.2f, 1);
            var pr = plane.GetComponent<MeshRenderer>(); pr.sharedMaterial = puddleMat; pr.shadowCastingMode = ShadowCastingMode.Off;
            fs.surface = pr;
            splashFx = MakeSplash(fsGo.transform);
            fs.splashFx = splashFx;

            // smoke sim
            var smGo = new GameObject("SmokeSim"); smGo.transform.SetParent(gSystems, false);
            var sm = smGo.AddComponent<SmokeSim>();
            sm.origin = V2(-0.5f, -0.5f); sm.cell = 0.5f; sm.width = 63; sm.height = 31; sm.walls = new List<Vector4>(blockRects);
            sm.smokeFx = MakeSmokePS(smGo.transform);

            // hood vent (weak) + windows registered automatically
            // game manager + UI
            var gmGo = new GameObject("GameManager"); gmGo.transform.SetParent(gSystems, false);
            var ui = gmGo.AddComponent<MamaUI>();
            ui.roundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            ui.circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var gm = gmGo.AddComponent<GameManager>();
            gm.player = kid; gm.mom = mom; gm.tap = tap; gm.mainValve = valve; gm.stove = stove; gm.pot = pot; gm.alarm = alarm;
            gm.frontDoor = frontDoor; gm.ui = ui; gm.livingRoomCarpet = carpetT; gm.totalTime = 540f;
            gm.floodSeeds = new[] { V(9.5f, 0, 11.6f), V(8.6f, 0, 9.4f), V(10.8f, 0, 12.4f), V(9.5f, 0, 7.2f) };
            gm.floodSeedRadius = 1.6f; gm.floodSeedDepth = 0.0042f; gm.startHeat = 15f; gm.startSmoke = 3f;

            // camera
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 50f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            var acd = camGo.AddComponent<UniversalAdditionalCameraData>();
            acd.renderPostProcessing = true; acd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = kid.transform; tpc.yaw = 0f; tpc.pitch = 50f; tpc.distance = 7f;
            Quaternion rot = Quaternion.Euler(tpc.pitch, tpc.yaw, 0f);
            camGo.transform.SetPositionAndRotation(kid.transform.position + tpc.offset - rot * Vector3.forward * tpc.distance, rot);
            gm.cam = tpc;
        }

        static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = C("FFE6C7"); RenderSettings.ambientEquatorColor = C("E8C9A6"); RenderSettings.ambientGroundColor = C("8A7058");
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.012f; RenderSettings.fogColor = C("8C8279");
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            sun.color = C("FFE9CF"); sun.intensity = 1.35f; sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.65f;
            RenderSettings.sun = sun;
            PointLight("LivingLight", V(4f, 2.6f, 3f), C("FFD9A8"), 9f, 1.4f, gRooms);
            PointLight("EntryLight", V(10f, 2.6f, 3f), C("FFD9A8"), 7f, 1.0f, gRooms);
            PointLight("HallLight", V(5f, 2.6f, 7f), C("FFE2B8"), 7f, 0.9f, gRooms);
            PointLight("HallLight2", V(17f, 2.6f, 7f), C("FFE2B8"), 7f, 0.9f, gRooms);

            string vpPath = Root + "/MamaVolumeProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(vpPath) != null) AssetDatabase.DeleteAsset(vpPath);
            var prof = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(prof, vpPath);
            var bloom = prof.Add<Bloom>(true); bloom.intensity.Override(0.45f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.6f);
            var tm = prof.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            var ca = prof.Add<ColorAdjustments>(true); ca.saturation.Override(14f); ca.contrast.Override(6f); ca.postExposure.Override(0.1f);
            var vg = prof.Add<Vignette>(true); vg.intensity.Override(0.22f); vg.smoothness.Override(0.45f);
            foreach (var comp in prof.components) AssetDatabase.AddObjectToAsset(comp, prof);
            EditorUtility.SetDirty(prof);
            var vol = new GameObject("Global Volume").AddComponent<Volume>();
            vol.isGlobal = true; vol.sharedProfile = prof;
        }

        // =====================================================================  FX
        static Material ParticleMat(string name, bool additive)
        {
            string path = MatDir + "/" + name + ".mat";
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            m.SetTexture("_BaseMap", texSoft);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive) m.EnableKeyword("_BLENDMODE_ADD"); else m.DisableKeyword("_BLENDMODE_ADD");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static ParticleSystem NewPS(string name, Transform parent, Vector3 pos, Material mat, int max)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = max; main.playOnAwake = true; main.loop = true;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }

        static FireSource MakeFire(string label, Vector3 pos, FireType type, float radius, float maxSize, Transform parent, float light)
        {
            var go = new GameObject(label); go.transform.SetParent(parent, true); go.transform.position = pos;
            var f = go.AddComponent<FireSource>();
            f.label = label; f.type = type; f.radius = radius; f.maxFlameSize = maxSize;
            var flames = NewPS("Flames", go.transform, pos, ParticleMat("FX_Flame", true), 300);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.2f, 1.6f, 0.5f, 1f), new Color(2.4f, 0.9f, 0.2f, 1f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = -0.2f;
            var em = flames.emission; em.rateOverTime = 0f;
            var sh = flames.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = radius; sh.rotation = V(-90, 0, 0);
            var col = flames.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(C("FFF2A8"), 0f), new GradientColorKey(C("FF9A2E"), 0.35f), new GradientColorKey(C("E0412A"), 0.75f), new GradientColorKey(C("5A1E10"), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = flames.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 0.2f));
            var noise = flames.noise; noise.enabled = true; noise.strength = 0.35f; noise.frequency = 1.4f;
            flames.GetComponent<ParticleSystemRenderer>().sortingFudge = -200f;
            flames.Play();
            f.flames = flames;

            var embers = NewPS("Embers", go.transform, pos, ParticleMat("FX_Flame", true), 100);
            var em2 = embers.main;
            em2.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f); em2.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            em2.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f); em2.startColor = new ParticleSystem.MinMaxGradient(C("FFB347"), C("FF5A1F"));
            em2.gravityModifier = -0.05f;
            var ee = embers.emission; ee.rateOverTime = 0f;
            var es = embers.shape; es.shapeType = ParticleSystemShapeType.Cone; es.angle = 25f; es.radius = radius; es.rotation = V(-90, 0, 0);
            var en = embers.noise; en.enabled = true; en.strength = 0.8f;
            embers.GetComponent<ParticleSystemRenderer>().sortingFudge = -200f;
            embers.Play();
            f.embers = embers;

            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(go.transform, false); l.transform.position = pos + V(0, 0.4f, 0);
            l.type = LightType.Point; l.color = C("FF8A3A"); l.range = 5f; l.intensity = 0f; l.enabled = false;
            f.glow = l; f.baseLight = 6f * light;
            return f;
        }

        static ParticleSystem MakeSmokePS(Transform parent)
        {
            var ps = NewPS("SmokeParticles", parent, Vector3.zero, ParticleMat("FX_Smoke", false), 2500);
            var main = ps.main; main.startLifetime = 3f; main.startSpeed = 0f; main.startSize = 1.8f;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.7f, 1, 1.3f));
            var rol = ps.rotationOverLifetime; rol.enabled = true; rol.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            ps.Play();
            return ps;
        }

        static ParticleSystem MakeSmokeColumn(Transform parent, Vector3 pos)
        {
            var ps = NewPS("SmokeColumn", parent, pos, ParticleMat("FX_Smoke", false), 400);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.23f, 0.22f, 0.4f), new Color(0.4f, 0.38f, 0.36f, 0.3f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.12f; sh.rotation = V(-90, 0, 0);
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 4.5f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.5f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.6f;
            var vol = ps.velocityOverLifetime; vol.enabled = true; vol.space = ParticleSystemSimulationSpace.World; vol.z = new ParticleSystem.MinMaxCurve(-0.35f, -0.15f); vol.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); vol.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            ps.Play();
            return ps;
        }

        static ParticleSystem MakeSplash(Transform parent)
        {
            var ps = NewPS("Splashes", parent, Vector3.zero, ParticleMat("FX_Water", false), 400);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f); main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f, 0.9f), new Color(0.55f, 0.78f, 1f, 0.8f));
            main.gravityModifier = 2.2f;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.15f; sh.rotation = V(-90, 0, 0);
            ps.Play();
            return ps;
        }

        static ParticleSystem MakeSpray(Transform nozzle)
        {
            var go = new GameObject("Spray"); go.transform.SetParent(nozzle, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 800;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f); main.startColor = new Color(1f, 1f, 1f, 0.7f);
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = 0.02f;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.4f, 1, 4f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0f, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = ParticleMat("FX_Smoke", false); r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        // =====================================================================  HELPERS
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Vector2 V2(float x, float y) => new Vector2(x, y);
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static Transform G(string n, Transform parent = null, Vector3 pos = default)
        {
            var t = new GameObject(n).transform;
            if (parent) t.SetParent(parent, false);
            t.position = pos;
            return t;
        }

        static void PointLight(string n, Vector3 p, Color c, float range, float intensity, Transform parent)
        {
            var l = new GameObject(n).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = p; l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            if (parent == gRooms) l.gameObject.AddComponent<PoweredLight>();
        }

        static Material M(string name, Color c, float smooth = 0.15f, float metal = 0f)
        {
            if (mats.TryGetValue(name, out var m)) return m;
            string path = MatDir + "/" + name + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); } else m.shader = lit;
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            m.SetTexture("_BaseMap", null);
            m.SetFloat("_Surface", 0f); m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = -1; m.SetFloat("_ZWrite", 1f);
            m.SetFloat("_SrcBlend", 1f); m.SetFloat("_DstBlend", 0f); m.SetOverrideTag("RenderType", "Opaque");
            m.SetShaderPassEnabled("DepthOnly", true); m.SetShaderPassEnabled("ShadowCaster", true);
            m.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        static Material ME(string name, Color c, Color emission)
        {
            var m = M(name, c, 0.3f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        static Material MT(string name, Color c, float smooth)
        {
            var m = M(name, c, smooth);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", 1f); m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false); m.SetShaderPassEnabled("ShadowCaster", false);
            return m;
        }

        static Material MTex(string name, Texture2D t, Vector2 tiling, float smooth)
        {
            var m = M(name, Color.white, smooth);
            m.SetTexture("_BaseMap", t); m.SetTextureScale("_BaseMap", tiling);
            if (t) texFix.Add(new KeyValuePair<Material, string>(m, t.name));
            return m;
        }

        static GameObject W(PrimitiveType t, string n, Transform parent, Vector3 pos, Vector3 scale, Material m, bool col, Vector3 euler)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = n;
            if (parent) g.transform.SetParent(parent, false);
            g.transform.position = pos; g.transform.rotation = Quaternion.Euler(euler); g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static GameObject Cube(string n, Transform p, Vector3 pos, Vector3 size, Material m, bool col = false, Vector3 euler = default) => W(PrimitiveType.Cube, n, p, pos, size, m, col, euler);
        static GameObject Cyl(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = false, Vector3 euler = default) => W(PrimitiveType.Cylinder, n, p, pos, scale, m, col, euler);
        static GameObject Sph(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = false, Vector3 euler = default) => W(PrimitiveType.Sphere, n, p, pos, scale, m, col, euler);
        static GameObject Cap(string n, Transform p, Vector3 pos, Vector3 scale, Material m, bool col = false, Vector3 euler = default) => W(PrimitiveType.Capsule, n, p, pos, scale, m, col, euler);

        static GameObject L(PrimitiveType t, string n, Transform parent, Vector3 lpos, Vector3 scale, Material m, bool col = false, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = n; g.transform.SetParent(parent, false);
            g.transform.localPosition = lpos; g.transform.localRotation = Quaternion.Euler(euler); g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static void BoxCol(GameObject go, Vector3 center, Vector3 size) { var b = go.AddComponent<BoxCollider>(); b.center = center; b.size = size; }

        static T Prop<T>(string name, Vector3 pos, float mass, string display) where T : Pickup
        {
            var root = new GameObject(name);
            root.transform.SetParent(gProps, false);
            root.transform.position = pos;
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = mass; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var c = root.AddComponent<T>();
            c.displayName = display; c.interactRadius = 1.4f;
            return c;
        }

        static void AtHome(MessyProp p, Vector3 euler = default)
        {
            p.transform.rotation = Quaternion.Euler(euler);
            p.homePosition = p.transform.position; p.homeRotation = p.transform.rotation; p.homeSet = true;
        }

        static void SetHome(MessyProp p, Vector3 homePos, Vector3 homeEuler, Vector3 startPos, Vector3 startEuler)
        {
            p.homePosition = homePos; p.homeRotation = Quaternion.Euler(homeEuler); p.homeSet = true;
            p.transform.SetPositionAndRotation(startPos, Quaternion.Euler(startEuler));
        }

        static void Frame(Transform p, Vector3 c, float w, float h, Vector3 n, Color col)
        {
            var m = M("Frame_" + ColorUtility.ToHtmlStringRGB(col), col, 0.3f);
            Vector3 along = Vector3.Cross(Vector3.up, n).normalized;
            Vector3 aAbs = new Vector3(Mathf.Abs(along.x), 0, Mathf.Abs(along.z)), nAbs = new Vector3(Mathf.Abs(n.x), 0, Mathf.Abs(n.z));
            const float t = 0.06f;
            Cube("Frame", p, c + Vector3.up * (h * 0.5f), aAbs * (w + t) + Vector3.up * t + nAbs * 0.04f, m);
            Cube("Frame", p, c - Vector3.up * (h * 0.5f), aAbs * (w + t) + Vector3.up * t + nAbs * 0.04f, m);
            Cube("Frame", p, c + along * (w * 0.5f), aAbs * t + Vector3.up * h + nAbs * 0.04f, m);
            Cube("Frame", p, c - along * (w * 0.5f), aAbs * t + Vector3.up * h + nAbs * 0.04f, m);
            Cube("Picture", p, c - n * 0.005f, aAbs * w + Vector3.up * h + nAbs * 0.01f, M("PictureBG", C("FFF8E8")));
            Sph("Doodle", p, c - n * 0.012f, aAbs * w * 0.4f + Vector3.up * h * 0.4f + nAbs * 0.01f, M("Doodle_" + ColorUtility.ToHtmlStringRGB(col), Color.Lerp(col, Color.white, 0.4f)));
        }

        static void Clock(Transform parent, Vector3 c, Vector3 n)
        {
            Vector3 e = n == Vector3.right ? V(0, 0, 90) : V(90, 0, 0);
            Cyl("ClockRim", parent, c, V(0.45f, 0.02f, 0.45f), M("ClockRim", C("FF7AA8")), false, e);
            Cyl("ClockFace", parent, c + n * 0.012f, V(0.38f, 0.01f, 0.38f), M("ClockFace", C("FFFDF4")), false, e);
            Cube("Hand", parent, c + n * 0.025f + Vector3.up * 0.06f, n == Vector3.right ? V(0.01f, 0.13f, 0.015f) : V(0.015f, 0.13f, 0.01f), M("Black", C("1A1A1F"), 0.8f));
        }

        static void Plant(Transform p, Vector3 basePos, float s, Color potCol)
        {
            var pm = M("PlantPot_" + ColorUtility.ToHtmlStringRGB(potCol), potCol, 0.3f);
            var leaf = M("Leaf", C("3FAF4F"), 0.3f); var leaf2 = M("Leaf2", C("6CC63A"), 0.3f);
            Cyl("Pot", p, basePos + Vector3.up * 0.1f * s, V(0.2f, 0.1f, 0.2f) * s, pm, s > 1f);
            Cyl("PotRim", p, basePos + Vector3.up * 0.2f * s, V(0.23f, 0.02f, 0.23f) * s, pm);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                Vector3 off = Quaternion.Euler(0, a, 0) * Vector3.forward * 0.05f * s;
                Sph("Leaf", p, basePos + Vector3.up * 0.34f * s + off, V(0.07f, 0.22f, 0.05f) * s, i % 2 == 0 ? leaf : leaf2, false, new Vector3(20f, a, 0f));
            }
        }

        // ---------------------------------------------------------------- textures
        static void Textures()
        {
            texSoft = MakeTex("SoftParticle", 99);
        }

        static Texture2D MakeTex(string name, int kind)
        {
            string path = TexDir + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++) px[y * s + x] = Pixel(kind, x, y, s);
            t.SetPixels(px); t.Apply(true);
            t.wrapMode = kind == 99 ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4;
            t.name = name;
            AssetDatabase.CreateAsset(t, path);
            return t;
        }

        static Color Stripe(int x, Color a, Color b) => (x / 16) % 2 == 0 ? a : b;

        static Color Pixel(int kind, int x, int y, int s)
        {
            switch (kind)
            {
                case 0: { bool a = ((x / 64) + (y / 64)) % 2 == 0; Color c = a ? new Color(1f, 0.89f, 0.5f) : new Color(1f, 0.97f, 0.84f); int gx = x % 64, gy = y % 64; if (gx < 2 || gy < 2 || gx > 61 || gy > 61) c *= 0.92f; c.a = 1; return c; }
                case 1:
                case 2:
                    {
                        int plank = y / 32; float[] tones = { 0f, 0.05f, -0.04f, 0.03f };
                        Color b = kind == 1 ? new Color(0.78f, 0.52f, 0.32f) : new Color(0.9f, 0.72f, 0.5f);
                        float n = Mathf.PerlinNoise(x * 0.05f + plank * 10f, y * 0.3f) * 0.08f;
                        Color c = b * (1f + tones[plank] + n - 0.04f);
                        if (y % 32 < 2) c = b * 0.62f;
                        int seam = (plank * 47 + 20) % 128; if (Mathf.Abs(x - seam) < 1) c = b * 0.66f;
                        c.a = 1; return c;
                    }
                case 4: { Color c = new Color(0.78f, 0.8f, 0.82f); if (x % 64 < 2 || y % 64 < 2) c *= 0.8f; c.a = 1; return c; }
                case 5: { bool a = ((x / 32) + (y / 32)) % 2 == 0; Color c = a ? new Color(0.97f, 0.98f, 0.98f) : new Color(0.62f, 0.86f, 0.88f); if (x % 32 < 1 || y % 32 < 1) c *= 0.85f; c.a = 1; return c; }
                case 13: { int tx = x / 64, ty = y / 64; float v = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.06f + ((tx * 7 + ty * 3) % 5) * 0.012f; Color c = new Color(0.84f - v, 0.74f - v, 0.6f - v); if (x % 64 < 2 || y % 64 < 2) c = new Color(0.52f, 0.47f, 0.42f); c.a = 1; return c; }
                case 6: return Stripe(x, new Color(0.62f, 0.88f, 0.78f), new Color(0.69f, 0.92f, 0.83f));
                case 7: return Stripe(x, new Color(1f, 0.92f, 0.62f), new Color(1f, 0.95f, 0.72f));
                case 8: return Stripe(x, new Color(0.8f, 0.74f, 0.95f), new Color(0.86f, 0.8f, 0.98f));
                case 9: { Color c = y < 64 ? (((x / 21) + (y / 21)) % 2 == 0 ? new Color(0.75f, 0.92f, 0.95f) : new Color(0.9f, 0.97f, 0.98f)) : new Color(0.96f, 0.98f, 0.99f); if (y < 64 && (x % 21 == 0 || y % 21 == 0)) c *= 0.88f; if (y >= 64 && y < 68) c = new Color(0.4f, 0.75f, 0.8f); c.a = 1; return c; }
                case 10: return Stripe(x, new Color(1f, 0.66f, 0.28f), new Color(1f, 0.73f, 0.37f));
                case 11: return Stripe(x, new Color(0.7f, 0.86f, 1f), new Color(0.78f, 0.9f, 1f));
                case 12: return Stripe(x, new Color(1f, 0.8f, 0.7f), new Color(1f, 0.85f, 0.76f));
                case 99: { float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f; float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f; float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a); return new Color(1, 1, 1, a); }
            }
            return Color.magenta;
        }
    }
}
