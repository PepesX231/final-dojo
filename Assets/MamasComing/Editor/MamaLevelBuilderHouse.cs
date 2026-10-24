using System.Collections.Generic;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// The fruit-scale two-storey shell: ground walls (the old plan x2), the staircase, the upper floor
    /// (kid's bedroom, Mom's room, party loft, landing, upstairs hall with a drop-down laundry chute) and a flat roof.
    /// Everything here is authored in final world metres (raw mode), not in the old plan units.
    public static partial class MamaLevelBuilder
    {
        static float baseY;   // floor height of the walls being built
        static Transform gUpper, gRoof;
        // stair: runs along the north wall of the staircase room, rising towards +x
        const float StairX0 = 1.6f, StairX1 = 12.6f, StairZ0 = 24.6f, StairZ1 = 27.6f;
        static readonly Rect StairVoid = new Rect(1.4f, 24.3f, StairX1 - 1.4f, 27.85f - 24.3f);
        static readonly Rect ChuteHole = new Rect(26.2f, 12.15f, 3.0f, 3.7f);   // wall to wall: no way to sidestep it

        // =====================================================================  GROUND SHELL
        static void Shell()
        {
            raw = true; baseY = 0f;
            Cube("FloorCollider", gHouse, V(30f, -0.1f, 14f), V(60.8f, 0.2f, 28.8f), M("Hidden", C("777777")), true).GetComponent<Renderer>().enabled = false;
            Cube("Foundation", gHouse, V(30f, -0.32f, 14f), V(60.6f, 0.6f, 28.6f), M("Foundation", C("C9B9A6")));

            Floor("Living", 0, 0, 26, 12, Tex("WoodWarm", 1), V2(13, 6));
            Floor("Laundry", 26, 0, 34, 12, Tex("TileGrey", 4), V2(8 / 1.2f, 12 / 1.2f));
            Floor("Dining", 34, 0, 44, 12, Tex("WoodWarm", 1), V2(5, 6));
            Floor("Hallway", 0, 12, 60, 16, Tex("WoodLight", 2), V2(30, 2));
            Floor("Garage", 44, 0, 60, 12, Tex("TileGrey", 4), V2(16 / 1.8f, 12 / 1.8f));
            Floor("GameRoom", 44, 16, 60, 28, Tex("WoodWarm", 1), V2(8, 6));
            Floor("Staircase", 0, 16, 14, 28, Tex("WoodLight", 2), V2(7, 6));
            Floor("Bathroom", 14, 16, 24, 28, Tex("TileBathWarm", 13), V2(10, 12));
            Floor("Kitchen", 24, 16, 44, 28, Tex("Checker", 0), V2(10 / 1.2f, 6 / 1.2f));

            Material wLiving = Tex("StripeMint", 6), wHall = Tex("StripeButter", 7), wStair = Tex("StripeLilac", 8),
                wBath = Tex("WallTile", 9), wKit = Tex("StripeOrange", 10), wLaun = Tex("StripeSky", 11),
                wDin = Tex("StripePeach", 12), wOut = M("Siding", C("FFF1D6")),
                wGar = M("GarageWall", C("D8D2C6")), wGame = M("GameRoomWall", C("5B4B8A"));

            // exterior
            WallZ(0f, 0f, 12f, null, wOut, wLiving);
            WallZ(0f, 12f, 16f, null, wOut, wHall);
            WallZ(0f, 16f, 28f, new[] { V2(18.5f, 21.5f) }, wOut, wStair, true);
            WallZ(60f, 0f, 12f, null, wGar, wOut);
            WallZ(60f, 12f, 16f, null, wHall, wOut);
            WallZ(60f, 16f, 28f, new[] { V2(20f, 23f) }, wGame, wOut, true);
            WallX(0f, 0f, 26f, new[] { V2(4f, 7f), V2(16f, 18.4f) }, wOut, wLiving, new[] { true, false });
            WallX(0f, 26f, 34f, null, wOut, wLaun);
            WallX(0f, 34f, 44f, new[] { V2(38f, 41f) }, wOut, wDin, new[] { true });
            WallX(0f, 44f, 60f, null, wOut, wGar);
            WallX(28f, 0f, 14f, null, wStair, wOut);
            WallX(28f, 14f, 24f, new[] { V2(20.6f, 22.6f) }, wBath, wOut, new[] { true });
            WallX(28f, 24f, 44f, new[] { V2(32.6f, 35.4f) }, wKit, wOut, new[] { true });
            WallX(28f, 44f, 60f, new[] { V2(51f, 54f) }, wGame, wOut, new[] { true });

            // interior
            WallX(12f, 0f, 26f, new[] { V2(8f, 12f) }, wLiving, wHall);
            WallX(12f, 26f, 34f, new[] { V2(28.8f, 31.2f) }, wLaun, wHall);
            WallX(12f, 34f, 44f, new[] { V2(37f, 41f) }, wDin, wHall);
            WallX(12f, 44f, 60f, new[] { V2(46.4f, 49.2f) }, wGar, wHall);
            WallX(16f, 0f, 14f, new[] { V2(2f, 12f) }, wHall, wStair);   // wide arch: the staircase is the house's hub
            WallX(16f, 14f, 24f, new[] { V2(17.8f, 20.2f) }, wHall, wBath);
            WallX(16f, 24f, 44f, new[] { V2(30f, 34f) }, wHall, wKit);
            WallX(16f, 44f, 60f, new[] { V2(47f, 51f) }, wHall, wGame);
            WallZ(14f, 16f, 28f, null, wStair, wBath);
            WallZ(24f, 16f, 28f, null, wBath, wKit);
            WallZ(26f, 0f, 12f, null, wLiving, wLaun);
            WallZ(34f, 0f, 12f, null, wLaun, wDin);
            WallZ(44f, 0f, 12f, null, wDin, wGar);
            WallZ(44f, 16f, 28f, null, wKit, wGame);

            // thresholds & sunken bathroom (water has to pile up before it pours out)
            GroundOff(14.2f, 16.2f, 23.8f, 27.8f, -0.0005f);
            GroundOff(17.8f, 15.8f, 20.2f, 16.2f, 0.001f);
            GroundOff(16.8f, 20.0f, 20.8f, 24.0f, -0.0015f);
            GroundOff(17.8f, 21.0f, 19.8f, 23.0f, -0.003f);
            Cube("BathThreshold", gHouse, V(19f, 0.008f, 16f), V(2.4f, 0.016f, 0.36f), M("Marble", C("F2EEE6"), 0.6f));

            InteriorDoor(V(17.8f, 0, 16f), 2.4f, true, C("BFE8F0"));
            InteriorDoor(V(28.8f, 0, 12f), 2.4f, false, C("CFE3FF"));

            // front door
            var door = G("FrontDoor", gHouse, V(16f, 0f, 0f));
            var piv = G("Pivot", door, V(16f, 0f, 0f));
            Cube("Slab", piv, V(17.2f, 2.1f, 0f), V(2.36f, 4.2f, 0.12f), M("DoorTeal", C("3FB7B0")), true);
            Cube("PanelTop", piv, V(17.2f, 3.1f, 0.08f), V(1.6f, 1.2f, 0.03f), M("DoorPanel", C("5ED1C9")));
            Cube("PanelBottom", piv, V(17.2f, 1.1f, 0.08f), V(1.6f, 1.2f, 0.03f), M("DoorPanel", C("5ED1C9")));
            Sph("Knob", piv, V(18.1f, 2.0f, 0.12f), Vector3.one * 0.16f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            frontDoor = door.gameObject.AddComponent<Door>();
            frontDoor.pivot = piv; frontDoor.openAngle = 100f;
            Vent(V(17.2f, 2f, 0.6f), 1.8f, 0.9f, null, frontDoor, Vector3.back, 2.2f);

            Staircase();
            raw = false;
        }

        // =====================================================================  STAIRCASE (ground floor -> landing)
        static void Staircase()
        {
            var t = G("Staircase", gRooms);
            var tread = M("StairTread", C("E8B27A")); var riser = M("StairRiser", C("FFF4E0")); var rail = M("Banister", C("8A4B2A"));
            float run = StairX1 - StairX0, rise = UpY, len = Mathf.Sqrt(run * run + rise * rise), ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            float cz = (StairZ0 + StairZ1) / 2f, w = StairZ1 - StairZ0;
            // smooth controller ramp under the treads
            var ramp = Cube("StairRamp", t, V((StairX0 + StairX1) / 2f, rise / 2f - 0.1f / Mathf.Cos(ang * Mathf.Deg2Rad), cz), V(len, 0.2f, w), M("Hidden", C("777777")), true, V(0, 0, ang));
            ramp.GetComponent<Renderer>().enabled = false;
            const int n = 22; float sx = run / n;
            for (int i = 0; i < n; i++)
            {
                float top = rise * (i + 1) / n;
                Cube("Step", t, V(StairX0 + sx * (i + 0.5f), top / 2f, cz), V(sx, top, w), riser);
                Cube("Tread", t, V(StairX0 + sx * (i + 0.5f), top + 0.02f, cz), V(sx + 0.04f, 0.05f, w + 0.06f), tread);
            }
            // the open side is closed with a stringer so nobody walks into the solid steps
            var str = Cube("Stringer", t, V((StairX0 + 1f + StairX1) / 2f, rise / 2f, StairZ0 - 0.08f), V(run - 1f, rise, 0.12f), M("Hidden", C("777777")), true);
            str.GetComponent<Renderer>().enabled = false;
            Cube("StringerBoard", t, V((StairX0 + StairX1) / 2f, rise / 2f - 0.15f, StairZ0 - 0.06f), V(len, 0.35f, 0.1f), rail, false, V(0, 0, ang));
            for (int i = 1; i < 11; i++)
            {
                float x = StairX0 + run * i / 11f, y = rise * i / 11f;
                Cyl("Baluster", t, V(x, y + 0.6f, StairZ0 - 0.06f), V(0.08f, 0.6f, 0.08f), riser);
            }
            Cube("Handrail", t, V((StairX0 + StairX1) / 2f, rise / 2f + 1.25f, StairZ0 - 0.06f), V(len, 0.1f, 0.14f), rail, false, V(0, 0, ang));
            Cyl("NewelPost", t, V(StairX0 + 0.1f, 0.75f, StairZ0 - 0.06f), V(0.22f, 0.75f, 0.22f), rail, true);
            Sph("NewelCap", t, V(StairX0 + 0.1f, 1.6f, StairZ0 - 0.06f), Vector3.one * 0.32f, M("Brass", C("F2C14E"), 0.7f, 0.6f));

            // staircase hall dressing (it's a hub: kept open, things only along the walls)
            Cube("ShoeBench", t, V(13f, 0.5f, 20f), V(1.2f, 1.0f, 3.0f), M("PanelLight", C("FFB46E")), true);
            Cube("BenchCushion", t, V(13f, 1.05f, 20f), V(1.1f, 0.12f, 2.8f), M("ArmCushion", C("FFE07A")));
            Plant(t, V(1.2f, 0, 17.2f), 3.6f, C("4FA3E0"));
            Frame(t, V(0.1f, 3.6f, 22.5f), 1.6f, 1.2f, Vector3.right, C("FF7AA8"));
            Cube("Rug", t, V(7f, 0.01f, 20.5f), V(7f, 0.016f, 4f), M("RugPurple", C("B39DDB")));
            PointLight("StairLight", V(7f, 5.2f, 21f), C("FFE2B8"), 14f, 1.2f, gRooms);
        }

        // =====================================================================  UPPER FLOOR
        static void UpperFloor()
        {
            raw = true;
            gUpper = G("UpperFloor", gHouse);
            var wood = Tex("WoodLight", 2);
            // floor slab with the stair void and the laundry-chute hole cut out
            SlabWithHoles(new Rect(0f, 0f, HouseLayout.UpperMaxX, HouseLayout.UpperMaxZ), new[] { StairVoid, ChuteHole }, WallH, UpY, wood);
            Floor("BedroomUp", 0.15f, 0.15f, 13.85f, 11.85f, Tex("WoodLight", 2), V2(7, 6), UpY);
            Floor("MomsRoom", 14.15f, 0.15f, 29.85f, 11.85f, Tex("WoodWarm", 1), V2(8, 6), UpY);
            Floor("PartyLoft", 14.15f, 16.15f, 29.85f, 27.85f, Tex("Checker", 0), V2(16 / 2f, 12 / 2f), UpY);

            baseY = UpY;
            Material wOut = M("Siding", C("FFF1D6")), wBed = Tex("StripeLilac", 8), wMom = Tex("StripePeach", 12),
                wHall = Tex("StripeButter", 7), wLoft = M("LoftWall", C("3E2F6B")), wLand = Tex("StripeMint", 6);
            WallX(0f, 0f, 14f, new[] { V2(3.2f, 6f) }, wOut, wBed, new[] { true });
            WallX(0f, 14f, 30f, new[] { V2(19f, 23f) }, wOut, wMom, new[] { true });
            WallX(28f, 0f, 14f, new[] { V2(3.5f, 6.5f) }, wLand, wOut, new[] { true });
            WallX(28f, 14f, 30f, new[] { V2(20f, 24f) }, wLoft, wOut, new[] { true });
            WallZ(0f, 0f, 12f, new[] { V2(8.2f, 10.4f) }, wOut, wBed, true);   // clear of the bedroom wardrobe
            WallZ(0f, 12f, 16f, null, wOut, wHall);
            WallZ(0f, 16f, 28f, null, wOut, wLand);
            WallZ(30f, 0f, 12f, new[] { V2(4f, 7f) }, wMom, wOut, true);
            WallZ(30f, 12f, 16f, null, wHall, wOut);
            WallZ(30f, 16f, 28f, new[] { V2(20f, 23f) }, wLoft, wOut, true);
            WallX(12f, 0f, 14f, new[] { V2(9.6f, 12f) }, wBed, wHall);   // clear of the ironing board
            WallX(12f, 14f, 30f, new[] { V2(21f, 23.4f) }, wMom, wHall);
            WallX(16f, 14f, 30f, new[] { V2(18f, 20.4f) }, wHall, wLoft);
            WallZ(14f, 0f, 12f, null, wBed, wMom);
            WallZ(14f, 16f, 28f, null, wLand, wLoft);
            baseY = 0f;

            Railings();
            LaundryChute();
            UpstairsRooms();
            Roof();
            raw = false;
        }

        static void Railings()
        {
            var t = G("LandingRail", gUpper);
            var rail = M("Banister", C("8A4B2A")); var post = M("StairRiser", C("FFF4E0"));
            void Run(Vector3 a, Vector3 b)
            {
                Vector3 d = b - a; float l = d.magnitude; Vector3 c = (a + b) / 2f;
                bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
                Vector3 size = alongX ? V(l, 1.3f, 0.12f) : V(0.12f, 1.3f, l);
                Cube("RailBlock", t, c + V(0, 0.65f, 0), size, M("Hidden", C("777777")), true).GetComponent<Renderer>().enabled = false;
                Cube("Rail", t, c + V(0, 1.3f, 0), alongX ? V(l, 0.1f, 0.16f) : V(0.16f, 0.1f, l), rail);
                int k = Mathf.Max(2, Mathf.RoundToInt(l / 0.55f));
                for (int i = 0; i <= k; i++) Cyl("Baluster", t, Vector3.Lerp(a, b, i / (float)k) + V(0, 0.65f, 0), V(0.07f, 0.65f, 0.07f), post);
            }
            Run(V(StairVoid.xMin, UpY, StairVoid.yMin), V(StairVoid.xMax, UpY, StairVoid.yMin));
            Run(V(StairVoid.xMin, UpY, StairVoid.yMin), V(StairVoid.xMin, UpY, StairVoid.yMax));
        }

        /// One-way shortcut: a hatch in the upstairs hall drops you into the ground-floor hallway.
        static void LaundryChute()
        {
            var t = G("LaundryChute", gUpper);
            var rim = M("ChuteRim", C("FF6F61")); var hazard = M("HazardStripe", C("FFD447"), 0.2f);
            Rect h = ChuteHole;
            Cube("RimE", t, V(h.xMax + 0.1f, UpY + 0.2f, h.center.y), V(0.2f, 0.4f, h.height), rim, true);
            Cube("Lip", t, V(h.xMin - 0.25f, UpY + 0.008f, h.center.y), V(0.5f, 0.016f, h.height), hazard);
            Cube("Sign", t, V(h.xMax + 0.15f, UpY + 1.4f, h.center.y), V(0.06f, 0.7f, 1.6f), M("TagYellow", C("FFD447")));
            Label3D(t, "SHORTCUT  v", V(h.xMax + 0.11f, UpY + 1.4f, h.center.y), Quaternion.Euler(0, -90, 0), 0.08f, C("1A1A1F"));
            // soft landing downstairs
            Cube("CrashMat", gRooms, V(h.center.x, 0.06f, h.center.y), V(h.width + 0.6f, 0.12f, h.height - 0.3f), M("CrashMat", C("43CFC4")), true);
        }

        static void UpstairsRooms()
        {
            // ---------------------------------------------------------- upstairs hall + landing
            var hall = G("UpstairsHall", gRooms);
            Cube("HallRunner", hall, V(13f, UpY + 0.01f, 14f), V(20f, 0.016f, 1.6f), M("RunnerRed", C("C8553D")));
            Frame(hall, V(5f, UpY + 3.2f, 15.8f), 1.4f, 1.0f, Vector3.back, C("6EC6FF"));
            Frame(hall, V(16f, UpY + 3.2f, 12.2f), 1.2f, 1.4f, Vector3.forward, C("FFC93C"));
            Plant(hall, V(0.9f, UpY, 12.9f), 3.2f, C("E0623B"));
            PointLight("UpHallLight", V(10f, UpY + 4.8f, 14f), C("FFE2B8"), 14f, 1.1f, gRooms);
            PointLight("UpHallLight2", V(24f, UpY + 4.8f, 14f), C("FFE2B8"), 12f, 1.0f, gRooms);
            Cube("ReadingBench", hall, V(7f, UpY + 0.5f, 22.6f), V(3.2f, 1.0f, 1.2f), M("PanelLight", C("FFB46E")), true);
            Plant(hall, V(12.8f, UpY, 17f), 2.8f, C("4FA3E0"));
            PointLight("LandingLight", V(7f, UpY + 4.8f, 20f), C("FFE2C4"), 14f, 1.1f, gRooms);

            // ---------------------------------------------------------- Mom's room (x14..30, z0..12)
            var mr = G("MomsRoom", gRooms);
            var frame = M("MomBedWood", C("A0623A"));
            Cube("BedFrame", mr, V(22f, UpY + 0.45f, 2.6f), V(5.2f, 0.6f, 4.8f), frame, true);
            Cube("Mattress", mr, V(22f, UpY + 0.95f, 2.7f), V(5.0f, 0.4f, 4.6f), M("Sheet", C("FFFFFF")), true);
            Cube("Quilt", mr, V(22f, UpY + 1.2f, 3.3f), V(5.1f, 0.14f, 3.2f), M("MomQuilt", C("D96BA0")), true);
            Cube("Headboard", mr, V(22f, UpY + 1.6f, 0.25f), V(5.6f, 2.4f, 0.2f), M("MomHeadboard", C("7A3E7A")), true);
            for (int i = 0; i < 2; i++) Cube("Pillow", mr, V(20.8f + i * 2.4f, UpY + 1.35f, 0.9f), V(1.8f, 0.3f, 0.8f), M("Pillow", C("FFFFFF")));
            Cube("Vanity", mr, V(29.2f, UpY + 0.8f, 9.5f), V(1.2f, 1.6f, 2.8f), M("VanityWhite", C("FFF1D6")), true);
            Cube("VanityMirror", mr, V(29.75f, UpY + 2.6f, 9.5f), V(0.06f, 1.8f, 1.8f), M("Mirror", C("D8EEF5"), 0.95f, 0.5f));
            Cube("Wardrobe", mr, V(15f, UpY + 2.2f, 5f), V(1.6f, 4.4f, 5f), M("Wardrobe", C("FFF1D6")), true);
            Cube("WardrobeDoorL", mr, V(15.82f, UpY + 2.2f, 3.8f), V(0.04f, 4.2f, 2.3f), M("WardrobeDoor", C("B8E1F2")));
            Cube("WardrobeDoorR", mr, V(15.82f, UpY + 2.2f, 6.2f), V(0.04f, 4.2f, 2.3f), M("WardrobeDoor", C("B8E1F2")));
            Cyl("Rug", mr, V(22f, UpY + 0.01f, 7.6f), V(6f, 0.008f, 4f), M("RugCream", C("FFE6C7")));
            Plant(mr, V(28.8f, UpY, 0.9f), 3.0f, C("43CFC4"));
            Frame(mr, V(25.6f, UpY + 3.4f, 0.12f), 1.6f, 1.1f, Vector3.forward, C("A777E3"));
            PointLight("MomsRoomLight", V(22f, UpY + 4.8f, 6f), C("FFD9C4"), 15f, 1.2f, gRooms);

            // ---------------------------------------------------------- party loft dressing (x14..30, z16..28; the DJ rig comes from the game room)
            var loft = G("PartyLoft", gRooms);
            Sph("Beanbag", loft, V(16.6f, UpY + 0.6f, 19f), V(1.8f, 1.1f, 1.8f), M("BeanRed", C("E0413C")), true);
            Sph("Beanbag", loft, V(16.6f, UpY + 0.6f, 22f), V(1.8f, 1.1f, 1.8f), M("BeanYellow", C("FFD447")), true);
            Cube("NeonSign", loft, V(14.2f, UpY + 3.6f, 22f), V(0.06f, 1.0f, 4.0f), ME("Neon", C("FF4FD8"), new Color(2.4f, 0.4f, 2f)));
            Label3D(loft, "PARTY!!", V(14.25f, UpY + 3.6f, 22f), Quaternion.Euler(0, 90, 0), 0.22f, C("FFFFFF"));
            for (int x = 0; x < 4; x++) for (int z = 0; z < 3; z++)
                    Cube("DanceTile", loft, V(19.5f + x * 1.4f, UpY + 0.012f, 18.6f + z * 1.4f), V(1.3f, 0.016f, 1.3f), ME("DanceTile" + ((x + z) % 3), (x + z) % 3 == 0 ? C("4FD8FF") : (x + z) % 3 == 1 ? C("FF4FD8") : C("FFE04F"), new Color(0.6f, 0.6f, 0.6f)));
            PointLight("LoftLight", V(22f, UpY + 4.8f, 22f), C("E6D8FF"), 15f, 0.9f, gRooms);
        }

        /// Flat roof terrace with a parapet over both wings. Hidden in play (the camera looks into the rooms).
        static void Roof()
        {
            gRoof = G("Roof", gHouse);
            var roof = M("RoofTerrace", C("B5524A")); var cap = M("Parapet", C("FFF1D6"));
            float topUp = UpY + WallH;
            Cube("UpperRoof", gRoof, V(15f, topUp + 0.15f, 14f), V(30.6f, 0.3f, 28.6f), roof);
            Cube("LowRoof", gRoof, V(45.15f, WallH + 0.15f, 14f), V(29.7f, 0.3f, 28.6f), roof);   // stops at the upper floor's wall
            foreach (var (c, s) in new[] { (V(15f, topUp + 0.6f, 0f), V(30.6f, 0.6f, 0.3f)), (V(15f, topUp + 0.6f, 28f), V(30.6f, 0.6f, 0.3f)), (V(0f, topUp + 0.6f, 14f), V(0.3f, 0.6f, 28.6f)), (V(30f, topUp + 0.6f, 14f), V(0.3f, 0.6f, 28.6f)),
                                         (V(45f, WallH + 0.6f, 0f), V(30.6f, 0.6f, 0.3f)), (V(45f, WallH + 0.6f, 28f), V(30.6f, 0.6f, 0.3f)), (V(60f, WallH + 0.6f, 14f), V(0.3f, 0.6f, 28.6f)) })
                Cube("Parapet", gRoof, c, s, cap);
            Cube("Chimney", gRoof, V(5f, topUp + 1.8f, 24f), V(1.6f, 3f, 1.6f), M("Brick", C("B5524A")));
            Cube("Skylight", gRoof, V(45f, WallH + 0.32f, 22f), V(4f, 0.06f, 3f), MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f));
        }

        /// The old plan's prop heights don't land exactly on the 2x furniture: drop every loose pickup onto whatever is
        /// under it (counter, table, shelf, floor) so nothing hovers. Fish stay in their tank water.
        static void SettlePickups()
        {
            Physics.SyncTransforms();
            foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            {
                if (p is Fish || p is Extinguisher) continue;   // fish swim; the extinguisher hangs on its bracket
                var rs = System.Array.FindAll(p.GetComponentsInChildren<Renderer>(), r => r.enabled); if (rs.Length == 0) continue;   // art passes keep the old meshes disabled
                Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var own = p.GetComponentsInChildren<Collider>();
                float best = float.MaxValue;
                foreach (var h in Physics.RaycastAll(new Vector3(b.center.x, b.min.y + 0.05f, b.center.z), Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    if (System.Array.IndexOf(own, h.collider) < 0 && !h.collider.GetComponentInParent<Pickup>()) best = Mathf.Min(best, b.min.y - h.point.y);
                if (best == float.MaxValue || best < 0.002f || best > 1.1f) continue;
                var mp = p as MessyProp;
                bool atHome = mp && mp.homeSet && (mp.homePosition - p.transform.position).sqrMagnitude < 1e-4f;
                p.transform.position += Vector3.down * (best - 0.002f);
                if (atHome) mp.homePosition = p.transform.position;
            }
            Physics.SyncTransforms();
        }

        // =====================================================================  helpers (raw world units)
        static void SlabWithHoles(Rect area, Rect[] holes, float y0, float y1, Material m)
        {
            var xs = new List<float> { area.xMin, area.xMax }; var zs = new List<float> { area.yMin, area.yMax };
            foreach (var h in holes) { xs.Add(h.xMin); xs.Add(h.xMax); zs.Add(h.yMin); zs.Add(h.yMax); }
            xs.Sort(); zs.Sort();
            for (int i = 0; i + 1 < xs.Count; i++)
                for (int j = 0; j + 1 < zs.Count; j++)
                {
                    float a = xs[i], b = xs[i + 1], c = zs[j], d = zs[j + 1];
                    if (b - a < 0.01f || d - c < 0.01f) continue;
                    var mid = new Vector2((a + b) / 2f, (c + d) / 2f);
                    bool hole = false; foreach (var h in holes) if (h.Contains(mid)) hole = true;
                    if (hole) continue;
                    Cube("Slab", gUpper, V((a + b) / 2f, (y0 + y1) / 2f, (c + d) / 2f), V(b - a, y1 - y0, d - c), m, true);
                }
        }

        static Material Tex(string n, int kind) => MTex(n, MakeTex(n, kind), kind >= 6 ? new Vector2(5f, 1f) : Vector2.one, kind == 5 || kind == 0 ? 0.45f : 0.25f);

        static void Floor(string n, float x0, float z0, float x1, float z1, Material m, Vector2 tiling, float y = 0f)
        {
            var mm = M("Floor_" + n, Color.white, m.GetFloat("_Smoothness"));
            var src = m.GetTexture("_BaseMap");
            mm.SetTexture("_BaseMap", src);
            texFix.Add(new KeyValuePair<Material, string>(mm, src ? src.name : m.name));
            mm.SetTextureScale("_BaseMap", tiling);
            Cube("Floor_" + n, y > 0f ? gUpper : gRooms, V((x0 + x1) / 2, y - 0.01f + (y > 0f ? 0.012f : 0f), (z0 + z1) / 2), V(x1 - x0, 0.02f, z1 - z0), mm);
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
                        WallPieceX(z, a, b, 0f, SillH, matA, matB, true);
                        WallPieceX(z, a, b, HeadH, WallH, matA, matB, false);
                        WindowX(z, a, b);
                    }
                    else
                    {
                        WallPieceX(z, a, b, DoorH, WallH, matA, matB, false);
                        Trim(V((a + b) / 2, baseY + DoorH + 0.083f, z), V(b - a + 0.32f, 0.16f, WallT + 0.08f));
                        Trim(V(a - 0.08f, baseY + DoorH / 2 + 0.003f, z), V(0.16f, DoorH, WallT + 0.08f));
                        Trim(V(b + 0.08f, baseY + DoorH / 2 + 0.003f, z), V(0.16f, DoorH, WallT + 0.08f));
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
                        WallPieceZ(x, g.x, g.y, 0f, SillH, matA, matB, true);
                        WallPieceZ(x, g.x, g.y, HeadH, WallH, matA, matB, false);
                        WindowZ(x, g.x, g.y);
                    }
                    else WallPieceZ(x, g.x, g.y, DoorH, WallH, matA, matB, false);
                }
        }

        const float SillH = 0.95f * S, HeadH = 2.25f * S;

        static void WallPieceX(float z, float a, float b, float y0, float y1, Material ma, Material mb, bool blocks)
        {
            var grp = G("Wall", baseY > 0f ? gUpper : gHouse, V((a + b) / 2, baseY + WallH / 2, z));
            var cw = grp.gameObject.AddComponent<CutawayWall>();
            cw.normal = Vector3.forward; cw.halfExtent = new Vector2((b - a) / 2, WallH / 2);
            float h = y1 - y0, cy = baseY + (y0 + y1) / 2;
            Cube("SideS", grp, V((a + b) / 2, cy, z - WallT / 4), V(b - a, h, WallT / 2), ma, true);
            Cube("SideN", grp, V((a + b) / 2, cy, z + WallT / 4), V(b - a, h, WallT / 2), mb, true);
            if (blocks && y0 < 0.01f)
            {
                if (baseY < 0.01f) blockRects.Add(new Vector4(a, z - WallT / 2, b, z + WallT / 2));
                Cube("Base", grp, V((a + b) / 2, baseY + 0.123f, z), V(b - a - 0.006f, 0.24f, WallT + 0.06f), M("Baseboard", C("FFFFFF")));   // 3 mm up: no shared plane with the wall
            }
            if (y1 >= WallH - 0.01f) Cube("Top", grp, V((a + b) / 2, baseY + WallH - 0.045f, z), V(b - a - 0.006f, 0.1f, WallT + 0.08f), M("TopTrim", C("FFF4E0")));
        }

        static void WallPieceZ(float x, float a, float b, float y0, float y1, Material ma, Material mb, bool blocks)
        {
            var grp = G("Wall", baseY > 0f ? gUpper : gHouse, V(x, baseY + WallH / 2, (a + b) / 2));
            var cw = grp.gameObject.AddComponent<CutawayWall>();
            cw.normal = Vector3.right; cw.halfExtent = new Vector2((b - a) / 2, WallH / 2);
            float h = y1 - y0, cy = baseY + (y0 + y1) / 2;
            Cube("SideW", grp, V(x - WallT / 4, cy, (a + b) / 2), V(WallT / 2, h, b - a), ma, true);
            Cube("SideE", grp, V(x + WallT / 4, cy, (a + b) / 2), V(WallT / 2, h, b - a), mb, true);
            if (blocks && y0 < 0.01f)
            {
                if (baseY < 0.01f) blockRects.Add(new Vector4(x - WallT / 2, a, x + WallT / 2, b));
                Cube("Base", grp, V(x, baseY + 0.123f, (a + b) / 2), V(WallT + 0.06f, 0.24f, b - a - 0.006f), M("Baseboard", C("FFFFFF")));
            }
            if (y1 >= WallH - 0.01f) Cube("Top", grp, V(x, baseY + WallH - 0.045f, (a + b) / 2), V(WallT + 0.08f, 0.1f, b - a - 0.006f), M("TopTrim", C("FFF4E0")));
        }

        static void Trim(Vector3 p, Vector3 s) => Cube("Trim", baseY > 0f ? gUpper : gHouse, p, s, M("Trim", C("FFFFFF")));

        static void WindowX(float z, float a, float b)
        {
            float mid = baseY + (SillH + HeadH) / 2f;
            var root = G("Window", baseY > 0f ? gUpper : gHouse, V((a + b) / 2, mid, z));
            var frame = M("WindowFrame", C("8FD3F5"));
            var glass = MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f);
            float w = b - a, cx = (a + b) / 2, hh = HeadH - SillH;
            float side = z > 14f ? -0.24f : 0.24f;   // curtains hang on the room side
            Cube("Sill", root, V(cx, baseY + SillH + 0.04f, z), V(w + 0.4f, 0.12f, WallT + 0.36f), frame, true);
            Cube("Head", root, V(cx, baseY + HeadH - 0.04f, z), V(w + 0.2f, 0.12f, WallT + 0.12f), frame);
            Cube("L", root, V(a + 0.06f, mid, z), V(0.12f, hh, WallT + 0.12f), frame);
            Cube("R", root, V(b - 0.06f, mid, z), V(0.12f, hh, WallT + 0.12f), frame);
            Cube("UpperGlass", root, V(cx, mid + hh / 4f, z + 0.06f), V(w - 0.12f, hh / 2f - 0.1f, 0.03f), glass);
            var sash = G("Sash", root, V(cx, mid - hh / 4f, z - 0.06f));
            Cube("Glass", sash, V(cx, mid - hh / 4f, z - 0.06f), V(w - 0.16f, hh / 2f - 0.1f, 0.03f), glass);
            Cube("Bar", sash, V(cx, mid, z - 0.06f), V(w - 0.12f, 0.1f, 0.1f), frame);
            Cube("Bar", sash, V(cx, baseY + SillH + 0.1f, z - 0.06f), V(w - 0.12f, 0.1f, 0.1f), frame);
            var curtain = M("Curtain", C("FFB3C1"));
            Cube("CurtainL", root, V(a - 0.2f, mid - 0.1f, z + side), V(0.5f, hh + 0.4f, 0.08f), curtain);
            Cube("CurtainR", root, V(b + 0.2f, mid - 0.1f, z + side), V(0.5f, hh + 0.4f, 0.08f), curtain);
            Cyl("Rod", root, V(cx, baseY + HeadH + 0.25f, z + side), V(0.06f, (w + 1.4f) / 2, 0.06f), M("Brass", C("F2C14E"), 0.7f, 0.6f), false, V(0, 0, 90));
            var win = root.gameObject.AddComponent<Window>();
            win.displayName = "Window"; win.sash = sash; win.closedLocal = sash.localPosition; win.openLocal = sash.localPosition + V(0, hh / 2f, 0);
            win.interactRadius = 1.9f;
            win.interactAnchor = G("Anchor", root, V(cx, baseY, z + (z > 14f ? -1.2f : 1.2f)));
            if (baseY < 0.01f)   // only ground-floor windows vent the (ground-floor) smoke grid
                Vent(V(cx, 2f, z + (z > 14f ? -0.6f : 0.6f)), 1.8f, 0.7f, win, null, z > 14f ? Vector3.forward : Vector3.back, w);
            if (Mathf.Abs(z - 28f) < 0.01f && cx > 30f && cx < 37f && baseY < 0.01f) kitchenWinN = win;
        }

        static void WindowZ(float x, float a, float b)
        {
            float mid = baseY + (SillH + HeadH) / 2f;
            var root = G("Window", baseY > 0f ? gUpper : gHouse, V(x, mid, (a + b) / 2));
            var frame = M("WindowFrame", C("8FD3F5"));
            var glass = MT("Glass", new Color(0.75f, 0.92f, 1f, 0.22f), 0.95f);
            float w = b - a, cz = (a + b) / 2, hh = HeadH - SillH;
            float inward = x < 1f ? 1f : -1f;   // the room side
            Cube("Sill", root, V(x, baseY + SillH + 0.04f, cz), V(WallT + 0.36f, 0.12f, w + 0.4f), frame, true);
            Cube("Head", root, V(x, baseY + HeadH - 0.04f, cz), V(WallT + 0.12f, 0.12f, w + 0.2f), frame);
            Cube("UpperGlass", root, V(x - inward * 0.06f, mid + hh / 4f, cz), V(0.03f, hh / 2f - 0.1f, w - 0.12f), glass);
            var sash = G("Sash", root, V(x + inward * 0.06f, mid - hh / 4f, cz));
            Cube("Glass", sash, V(x + inward * 0.06f, mid - hh / 4f, cz), V(0.03f, hh / 2f - 0.1f, w - 0.16f), glass);
            Cube("Bar", sash, V(x + inward * 0.06f, mid, cz), V(0.1f, 0.1f, w - 0.12f), frame);
            var win = root.gameObject.AddComponent<Window>();
            win.displayName = "Window"; win.sash = sash; win.closedLocal = sash.localPosition; win.openLocal = sash.localPosition + V(0, hh / 2f, 0);
            win.interactRadius = 1.9f;
            win.interactAnchor = G("Anchor", root, V(x + inward * 1.8f, baseY, cz));
            if (baseY < 0.01f) Vent(V(x + inward * 0.6f, 2f, cz), 1.8f, 0.7f, win, null, V(-inward, 0, 0), w);
            if (baseY < 0.01f && x > 59f) kitchenWinE = win;
        }

        static void InteriorDoor(Vector3 hinge, float w, bool alongX, Color col)
        {
            var g = G("InteriorDoor", gHouse, hinge);
            var m = M("Door_" + ColorUtility.ToHtmlStringRGB(col), col);
            float h = DoorH - 0.2f;
            if (alongX)
            {
                // swung fully open, resting flat against the wall on the +Z side
                Cube("Slab", g, hinge + V(-w / 2, h / 2, 0.22f), V(w - 0.1f, h, 0.1f), m, true);
                Sph("Knob", g, hinge + V(-w + 0.24f, h * 0.45f, 0.32f), Vector3.one * 0.14f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            }
            else
            {
                Cube("Slab", g, hinge + V(-w / 2, h / 2, -0.22f), V(w - 0.1f, h, 0.1f), m, true);
                Sph("Knob", g, hinge + V(-w + 0.24f, h * 0.45f, -0.32f), Vector3.one * 0.14f, M("Brass", C("F2C14E"), 0.7f, 0.6f));
            }
        }

        static void Vent(Vector3 p, float r, float rate, Window w, Door d, Vector3 outward, float width)
        {
            var v = G("SmokeVent", gSystems, p).gameObject.AddComponent<SmokeVent>();
            v.radius = Mathf.Max(1.2f, r); v.rate = Mathf.Max(1.4f, rate); v.window = w; v.door = d; v.outward = outward; v.openingWidth = width;
        }
    }
}
