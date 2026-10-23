using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MamasComing.EditorTools
{
    /// The party aftermath: the new east wing (garage + game room) and a chain of household disasters in every room.
    public static partial class MamaLevelBuilder
    {
        static Transform gParty;
        static AquariumHazard tankHz;
        static Transform laundrySocketT;
        static Material frostingMat, confettiMat, foamMat, sodaMat;

        // =====================================================================  NEW WING
        static void PartyWing()
        {
            gParty = G("Party", gRooms);
            frostingMat = M("Frosting", C("FFD1E3"), 0.4f);
            confettiMat = M("Confetti", C("FF6FB5"), 0.3f);
            foamMat = M("SoapFoam", C("F6FBFF"), 0.6f);
            sodaMat = MT("Soda", new Color(0.45f, 0.22f, 0.08f, 0.75f), 0.85f);
            Garage();
            GameRoom();
            PointLight("HallLight3", V(26f, 2.6f, 7f), C("FFE2B8"), 7f, 0.9f, gRooms);
        }

        static void Garage()
        {
            var t = G("Garage", gRooms);
            var grey = M("GarageGrey", C("9AA3AD"), 0.3f);
            Cube("GarageDoor", t, V(26f, 1.15f, 0.12f), V(5.0f, 2.2f, 0.06f), M("GarageDoorWhite", C("EDEDE8")));
            for (int i = 0; i < 6; i++) Cube("Slat", t, V(26f, 0.2f + i * 0.38f, 0.16f), V(4.9f, 0.03f, 0.02f), grey);
            Cube("OilStain", t, V(26f, 0.004f, 2.6f), V(1.6f, 0.004f, 2.4f), M("OilStain", C("6F6A63")));

            var wood = M("BenchWood", C("B9844F"));
            Cube("Bench", t, V(29.5f, 0.85f, 2.2f), V(0.8f, 0.06f, 2.2f), wood, true);
            Cube("BenchLegs", t, V(29.5f, 0.42f, 2.2f), V(0.7f, 0.84f, 2.0f), M("BenchDark", C("8A5E36")), true);
            Cube("Pegboard", t, V(29.92f, 1.6f, 2.2f), V(0.04f, 1.0f, 2.0f), M("Pegboard", C("D9B98A")));
            Cube("Toolbox", t, V(29.5f, 1.0f, 1.5f), V(0.4f, 0.24f, 0.6f), M("ToolboxRed", C("E0413C"), 0.4f));
            var wrench = Prop<ToolItem>("Wrench", V(29.4f, 0.9f, 2.6f), 0.8f, "Wrench");
            L(PrimitiveType.Cube, "Handle", wrench.transform, V(0, 0.02f, 0), V(0.06f, 0.03f, 0.42f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            L(PrimitiveType.Cube, "Jaw", wrench.transform, V(0, 0.02f, 0.23f), V(0.14f, 0.035f, 0.08f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            BoxCol(wrench.gameObject, V(0, 0.02f, 0.05f), V(0.14f, 0.05f, 0.5f));
            wrench.kind = ToolItem.Kind.Wrench; wrench.holdOffset = V(0, 0, 0.1f); AtHome(wrench, V(0, 30, 0));
            var plunger = Prop<ToolItem>("Plunger", V(23.0f, 0f, 3.0f), 0.6f, "Plunger");
            L(PrimitiveType.Sphere, "Cup", plunger.transform, V(0, 0.07f, 0), V(0.22f, 0.12f, 0.22f), M("PlungerRed", C("C0392B"), 0.5f));
            L(PrimitiveType.Cylinder, "Stick", plunger.transform, V(0, 0.45f, 0), V(0.035f, 0.38f, 0.035f), M("Wood", C("C47A45")));
            BoxCol(plunger.gameObject, V(0, 0.4f, 0), V(0.22f, 0.8f, 0.22f));
            plunger.kind = ToolItem.Kind.Plunger; plunger.holdOffset = V(0, -0.2f, 0.12f); plunger.startKinematic = true; AtHome(plunger);

            Cyl("TrashBin", t, V(24.4f, 0.45f, 0.6f), V(0.7f, 0.45f, 0.7f), M("BinGreen", C("3F8F4F")), true);
            Cyl("TrashBin2", t, V(25.3f, 0.45f, 0.6f), V(0.7f, 0.45f, 0.7f), M("BinGreen", C("3F8F4F")), true);
            Cyl("Recycle", t, V(27.4f, 0.4f, 0.6f), V(0.6f, 0.4f, 0.6f), M("BinBlue", C("2F6FD6")), true);
            Cube("Shelf", t, V(22.35f, 1.2f, 4.4f), V(0.5f, 2.4f, 1.6f), M("ShelfMetal", C("A7ADB4"), 0.4f), true);
            Color[] pc = { C("FF6F61"), C("43CFC4"), C("FFD447"), C("7A5CD6") };
            for (int i = 0; i < 4; i++) Cyl("PaintCan", t, V(22.35f, 0.55f + (i / 2) * 0.8f, 3.9f + (i % 2) * 0.8f), V(0.25f, 0.13f, 0.25f), M("Paint" + i, pc[i]));
            Cyl("BikeWheel", t, V(28.6f, 0.35f, 0.4f), V(0.7f, 0.02f, 0.7f), M("Black", C("1A1A1F"), 0.8f), false, V(90, 0, 0));
            Cyl("BikeWheel", t, V(29.6f, 0.35f, 0.4f), V(0.7f, 0.02f, 0.7f), M("Black", C("1A1A1F"), 0.8f), false, V(90, 0, 0));
            Cube("BikeFrame", t, V(29.1f, 0.6f, 0.4f), V(0.9f, 0.05f, 0.05f), M("BikeTeal", C("43CFC4")), false, V(0, 0, 20));
            PointLight("GarageLight", V(26f, 2.6f, 3f), C("F2F6FF"), 9f, 1.2f, gRooms);
        }

        static void GameRoom()
        {
            var t = G("GameRoom", gRooms);
            Cube("Carpet", t, V(26f, 0.007f, 11f), V(5.5f, 0.012f, 4.0f), M("GameCarpet", C("2E2A55")));
            var tvM = M("TVBody", C("2E2A3A"), 0.6f);
            Cube("BigTV", t, V(22.15f, 1.3f, 11f), V(0.1f, 1.0f, 1.8f), tvM, true);
            Cube("BigTVScreen", t, V(22.21f, 1.3f, 11f), V(0.01f, 0.9f, 1.7f), ME("Screen2", C("1C2A6A"), new Color(0.2f, 0.25f, 0.8f)));
            Sph("Beanbag", t, V(24.2f, 0.3f, 10.2f), V(0.9f, 0.55f, 0.9f), M("BeanRed", C("E0413C")), true);
            Sph("Beanbag", t, V(24.4f, 0.3f, 12.0f), V(0.9f, 0.55f, 0.9f), M("BeanYellow", C("FFD447")), true);
            Cube("SnackTable", t, V(27.5f, 0.45f, 8.7f), V(1.6f, 0.05f, 0.6f), M("SnackTable", C("F2C08A")), true);
            Cube("SnackLegs", t, V(27.5f, 0.22f, 8.7f), V(1.5f, 0.44f, 0.5f), M("BenchDark", C("8A5E36")), true);
            Cyl("ChipBowl", t, V(27.0f, 0.52f, 8.7f), V(0.35f, 0.05f, 0.35f), M("BowlPink", C("FF8FB1"), 0.5f));
            Color[] bc = { C("FF6F61"), C("43CFC4"), C("FFD447"), C("7A5CD6"), C("FF8FB1") };
            for (int i = 0; i < 9; i++) { var p = V(22.8f + (i * 0.87f) % 7f, 2.35f + (i % 3) * 0.12f, 8.6f + (i * 1.7f) % 5f); Sph("Balloon", t, p, V(0.32f, 0.38f, 0.32f), M("Balloon" + (i % 5), bc[i % 5], 0.6f)); }
            for (int i = 0; i < 10; i++) Cube("Flag", t, V(22.6f + i * 0.75f, 2.5f, 13.85f), V(0.3f, 0.3f, 0.01f), M("Balloon" + (i % 5), bc[i % 5], 0.6f), false, V(0, 0, 45));
            PointLight("GameRoomLight", V(26f, 2.6f, 11f), C("E6D8FF"), 9f, 1.0f, gRooms);
        }

        // =====================================================================  DISASTERS
        static void PartyHazards()
        {
            laundrySocketT = G("LaundrySocket", gParty, V(14.2f, 0.25f, 0.6f));
            Cube("Socket", gParty, V(14.2f, 0.28f, 0.09f), V(0.12f, 0.12f, 0.02f), M("White", C("FFFFFF")));

            // ---------------------------------------------------------- GARAGE: breaker panel (power)
            var bp = G("Breaker panel", gParty, V(22.12f, 1.4f, 2.0f));
            Cube("Box", bp, V(22.12f, 1.4f, 2.0f), V(0.12f, 0.8f, 0.6f), M("PanelGrey", C("B8BEC6"), 0.5f, 0.3f));
            var lever = G("Lever", bp, V(22.2f, 1.55f, 2.0f));
            L(PrimitiveType.Cube, "Handle", lever, V(0.05f, 0, 0), V(0.1f, 0.18f, 0.06f), M("Black", C("1A1A1F"), 0.8f));
            var bl = new GameObject("PanelLight").AddComponent<Light>(); bl.transform.SetParent(bp, false); bl.transform.position = V(22.4f, 1.8f, 2.0f);
            bl.type = LightType.Point; bl.range = 1.5f; bl.intensity = 1.2f;
            Sph("LED", bp, V(22.19f, 1.78f, 2.2f), Vector3.one * 0.04f, ME("LEDGreen", C("44FF66"), new Color(0.3f, 2f, 0.4f)));
            var breaker = bp.gameObject.AddComponent<BreakerPanel>();
            breaker.displayName = "Breaker panel"; breaker.lever = lever; breaker.panelLight = bl; breaker.laundrySocket = laundrySocketT;
            breaker.interactAnchor = G("Anchor", bp, V(22.9f, 0, 2.0f)); breaker.interactRadius = 1.5f;
            breaker.questEn = "Restore the power (breaker panel)"; breaker.questTh = "กู้ไฟที่ตู้เบรกเกอร์";
            breaker.puzzleEn = "RECONNECT THE WIRES!"; breaker.puzzleTh = "ต่อสายไฟให้ถูกสี!";
            breaker.doneEn = "Power's back on!"; breaker.doneTh = "ไฟกลับมาแล้ว!";
            breaker.escalateTime = 9999f; breaker.severity = 2;

            // ---------------------------------------------------------- GARAGE: water heater (co-op valves)
            var wh = G("Water heater", gParty, V(28.9f, 0f, 5.2f));
            Cyl("Tank", wh, V(28.9f, 0.85f, 5.2f), V(0.8f, 0.85f, 0.8f), M("HeaterWhite", C("ECECE6"), 0.4f), true);
            Cyl("Top", wh, V(28.9f, 1.72f, 5.2f), V(0.82f, 0.03f, 0.82f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cyl("PipeUp", wh, V(28.9f, 2.2f, 5.2f), V(0.07f, 0.5f, 0.07f), M("Copper", C("D9814A"), 0.7f, 0.7f));
            Cyl("PipeRun", wh, V(26.0f, 2.6f, 5.85f), V(0.06f, 2.9f, 0.06f), M("Copper", C("D9814A"), 0.7f, 0.7f), false, V(0, 0, 90));
            Cyl("PipeDown", wh, V(23.1f, 1.7f, 5.85f), V(0.06f, 0.9f, 0.06f), M("Copper", C("D9814A"), 0.7f, 0.7f));
            var gauge = G("Gauge", wh, V(28.45f, 1.35f, 5.2f));
            Cyl("Dial", gauge, V(28.45f, 1.35f, 5.2f), V(0.22f, 0.02f, 0.22f), M("White", C("FFFFFF")), false, V(0, 0, 90));
            Cube("RedZone", gauge, V(28.44f, 1.42f, 5.25f), V(0.005f, 0.05f, 0.08f), M("RedPlastic", C("FF5A5A")));
            var needle = G("Needle", gauge, V(28.43f, 1.35f, 5.2f)); needle.rotation = Quaternion.Euler(0, -90, 0);
            L(PrimitiveType.Cube, "N", needle, V(0, 0.05f, 0), V(0.012f, 0.09f, 0.005f), M("Black", C("1A1A1F"), 0.8f));
            var steam = NewPS("Steam", wh, V(28.9f, 1.8f, 5.2f), ParticleMat("FX_Smoke", false), 200);
            SetupMist(steam, new Color(1f, 1f, 1f, 0.5f), 1.4f);
            var heater = wh.gameObject.AddComponent<WaterHeaterHazard>();
            heater.displayName = "Water heater"; heater.needle = needle; heater.steam = steam; heater.burstPoint = G("Burst", wh, V(28.2f, 0, 5.0f));
            heater.questEn = "Release the water-heater pressure (2 valves AT ONCE)"; heater.questTh = "ลดแรงดันเครื่องทำน้ำร้อน (หมุน 2 วาล์วพร้อมกัน)";
            heater.startEn = "The water heater gauge is creeping into the RED... (garage)"; heater.startTh = "เข็มเครื่องทำน้ำร้อนเริ่มขึ้นช่องแดง... (โรงรถ)";
            heater.failEn = "KA-BOOM! The water heater blew its valve - the garage is flooding!"; heater.failTh = "ตูม! วาล์วเครื่องทำน้ำร้อนระเบิด - โรงรถน้ำท่วม!";
            heater.doneEn = "Pssshhh... pressure released. Teamwork!"; heater.doneTh = "ฟู่ววว... แรงดันลดแล้ว ทีมเวิร์ก!";
            heater.momEn = "Why is the water heater making that noise?!"; heater.momTh = "ทำไมเครื่องทำน้ำร้อนเสียงดังแปลก ๆ?!";
            heater.momFailEn = "WHAT HAPPENED TO THE WATER HEATER?! The garage is soaked!"; heater.momFailTh = "เครื่องทำน้ำร้อนเป็นอะไร?! โรงรถเปียกหมดเลย!";
            heater.startAt = 15f; heater.escalateTime = 170f; heater.coop = true; heater.severity = 2; heater.loopClip = "hum"; heater.loopVolume = 0.18f;
            heater.interactAnchor = G("Anchor", wh, V(28.1f, 0, 5.1f));
            heater.valveA = HeaterValveAt(heater, V(28.3f, 0.95f, 4.55f), V(27.8f, 0, 4.4f), "Valve A");
            heater.valveB = HeaterValveAt(heater, V(23.1f, 1.2f, 5.75f), V(23.4f, 0, 5.1f), "Valve B");

            // ---------------------------------------------------------- KITCHEN: burst pipe (chain from the heater)
            var pipeRoot = G("Burst pipe", gParty, V(18.15f, 0.25f, 13.2f));
            Cyl("Pipe", pipeRoot, V(18.15f, 0.3f, 13.15f), V(0.06f, 0.3f, 0.06f), M("Copper", C("D9814A"), 0.7f, 0.7f));
            var spray = NewPS("Spray", pipeRoot, V(18.15f, 0.35f, 12.95f), ParticleMat("FX_Water", false), 400);
            var sm = spray.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f); sm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            sm.startColor = new Color(0.7f, 0.88f, 1f, 0.85f); sm.gravityModifier = 1.5f;
            var ssh = spray.shape; ssh.shapeType = ParticleSystemShapeType.Cone; ssh.angle = 25f; ssh.radius = 0.02f; ssh.rotation = V(0, 180, 0);
            var em0 = spray.emission; em0.rateOverTime = 0f;
            var pipe = pipeRoot.gameObject.AddComponent<SinkPipeHazard>();
            pipe.displayName = "Burst pipe"; pipe.leakPoint = G("Leak", pipeRoot, V(18.1f, 0, 12.6f)); pipe.spray = spray;
            pipe.interactAnchor = G("Anchor", pipeRoot, V(18.2f, 0, 12.6f)); pipe.interactRadius = 1.3f;
            pipe.questEn = "Fix the burst pipe under the kitchen counter (wrench)"; pipe.questTh = "ซ่อมท่อแตกใต้เคาน์เตอร์ครัว (ใช้ประแจ)";
            pipe.puzzleEn = "TIGHTEN THE BOLTS!"; pipe.puzzleTh = "ขันน็อตให้แน่น!";
            pipe.failEn = "The kitchen is flooding - and the water reached the outlets!"; pipe.failTh = "ครัวน้ำท่วม - น้ำไหลถึงปลั๊กแล้ว!";
            pipe.doneEn = "Pipe fixed! (closing the main valve would've slowed it too)"; pipe.doneTh = "ซ่อมท่อแล้ว! (ปิดวาล์วหลักก็ช่วยชะลอได้นะ)";
            pipe.momEn = "Why is the kitchen cabinet DRIPPING?!"; pipe.momTh = "ทำไมตู้ครัวมีน้ำหยด?!";
            pipe.startAt = 330f; pipe.escalateTime = 75f; pipe.severity = 2; pipe.loopClip = "water"; pipe.loopVolume = 0.3f;
            heater.pipe = pipe;

            // ---------------------------------------------------------- KITCHEN: toaster-oven party pizza
            var ov = G("Toaster oven", gParty, V(21.45f, 0.92f, 10.1f));
            Cube("Body", ov, V(21.45f, 1.08f, 10.1f), V(0.45f, 0.32f, 0.6f), M("OvenChrome", C("C9CED6"), 0.7f, 0.5f), true);
            Cube("Window", ov, V(21.21f, 1.08f, 10.05f), V(0.01f, 0.2f, 0.4f), ME("OvenOrange", C("FF9A3A"), new Color(1.6f, 0.6f, 0.15f)));
            Cyl("Dial", ov, V(21.21f, 1.08f, 10.33f), V(0.06f, 0.01f, 0.06f), M("Black", C("1A1A1F"), 0.8f), false, V(0, 0, 90));
            var ovSmoke = MakeSmokeColumn(ov, V(21.45f, 1.3f, 10.1f));
            var glow = new GameObject("OvenGlow").AddComponent<Light>(); glow.transform.SetParent(ov, false); glow.transform.position = V(21.0f, 1.1f, 10.1f);
            glow.type = LightType.Point; glow.color = C("FF8A3A"); glow.range = 1.6f; glow.enabled = false;
            var ovFire = MakeFire("Toaster oven", V(21.45f, 1.28f, 10.1f), FireType.Ordinary, 0.2f, 0.7f, ov, 0.8f);
            ovFire.growth = 0.012f; ovFire.smokeRate = 1.3f;
            foreach (var f in Object.FindObjectsByType<FireSource>(FindObjectsSortMode.None)) if (f.label == "Kitchen cabinet") ovFire.spreadTo.Add(f);
            var oven = ov.gameObject.AddComponent<OvenHazard>();
            oven.displayName = "Toaster oven"; oven.ovenFire = ovFire; oven.ovenSmoke = ovSmoke; oven.ovenGlow = glow;
            oven.interactAnchor = G("Anchor", ov, V(20.7f, 0, 10.1f)); oven.interactRadius = 1.3f;
            oven.questEn = "Turn off the toaster oven (pizza burning!)"; oven.questTh = "ปิดเตาอบ (พิซซ่าไหม้!)";
            oven.puzzleEn = "TURN THE DIAL TO OFF!"; oven.puzzleTh = "หมุนปุ่มไปที่ปิด!"; oven.puzzle = MiniGame.Kind.Timing;
            oven.startEn = "Something smells burnt... the PIZZA in the toaster oven! (kitchen)"; oven.startTh = "ได้กลิ่นไหม้... พิซซ่าในเตาอบ! (ครัว)";
            oven.failEn = "The toaster oven CAUGHT FIRE!"; oven.failTh = "เตาอบไฟลุก!";
            oven.doneEn = "Oven off. RIP pizza."; oven.doneTh = "ปิดเตาแล้ว ลาก่อนพิซซ่า";
            oven.momEn = "Why is the toaster oven still warm and smoky?!"; oven.momTh = "ทำไมเตาอบยังร้อนและมีควัน?!";
            oven.startAt = 90f; oven.escalateTime = 70f; oven.needsPower = true; oven.severity = 2; oven.loopClip = "hum"; oven.loopVolume = 0.2f;

            // ---------------------------------------------------------- KITCHEN: fridge left open
            var fr = gRooms.Find("Kitchen/Fridge");
            var hinge = G("FridgeHinge", fr, V(21.73f, 1.0f, 12.97f));
            Cube("Door", hinge, V(21.25f, 1.0f, 12.95f), V(0.96f, 1.95f, 0.05f), M("Fridge", C("F4F08A"), 0.4f));
            Cube("Inside", fr, V(21.25f, 1.0f, 13.0f), V(0.9f, 1.85f, 0.01f), ME("FridgeIn", C("EAF6FF"), new Color(0.6f, 0.7f, 0.8f)));
            var stink = NewPS("Stink", fr, V(21.25f, 1.6f, 12.8f), ParticleMat("FX_Smoke", false), 60);
            SetupMist(stink, new Color(0.55f, 0.85f, 0.3f, 0.5f), 0.6f);
            var fridge = fr.gameObject.AddComponent<FridgeHazard>();
            fridge.displayName = "Fridge"; fridge.door = hinge; fridge.stink = stink; fridge.openAngle = 80f;
            fridge.interactAnchor = G("Anchor", fr, V(21.0f, 0, 12.3f)); fridge.interactRadius = 1.3f;
            fridge.questEn = "Close the fridge (food is warming up!)"; fridge.questTh = "ปิดตู้เย็น (อาหารกำลังจะเสีย!)";
            fridge.failEn = "The fridge food SPOILED... it stinks!"; fridge.failTh = "อาหารในตู้เย็นบูดแล้ว... เหม็นมาก!";
            fridge.doneEn = "Fridge closed. Still cold!"; fridge.doneTh = "ปิดตู้เย็นแล้ว ยังเย็นอยู่!";
            fridge.momEn = "Who left the FRIDGE OPEN?!"; fridge.momTh = "ใครเปิดตู้เย็นทิ้งไว้?!";
            fridge.momFailEn = "UGH what is that SMELL?! Everything in the fridge is rotten!"; fridge.momFailTh = "อี๋ กลิ่นอะไร?! ของในตู้เย็นเน่าหมดเลย!";
            fridge.startActive = true; fridge.escalateTime = 160f; fridge.severity = 2;

            // ---------------------------------------------------------- DINING: sparklers in the cake
            var cake = G("Party cake", gParty, V(19.5f, 0.78f, 2.8f));
            Cyl("Cake", cake, V(19.5f, 0.86f, 2.8f), V(0.45f, 0.08f, 0.45f), M("CakePink", C("FF9FC2")));
            Cyl("CakeTop", cake, V(19.5f, 0.95f, 2.8f), V(0.42f, 0.01f, 0.42f), M("Frosting", C("FFD1E3"), 0.4f));
            Cube("Tablecloth", cake, V(19.5f, 0.785f, 2.8f), V(1.85f, 0.006f, 0.95f), M("PartyCloth", C("FFE45C")));
            for (int i = 0; i < 4; i++) Cyl("Sparkler", cake, V(19.38f + i * 0.08f, 1.08f, 2.8f + (i % 2) * 0.06f), V(0.01f, 0.14f, 0.01f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            var sparks = NewPS("Sparks", cake, V(19.5f, 1.22f, 2.8f), ParticleMat("FX_Flame", true), 300);
            var spm = sparks.main; spm.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); spm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            spm.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f); spm.startColor = new ParticleSystem.MinMaxGradient(new Color(3f, 2.5f, 1.2f), new Color(3f, 1.4f, 0.4f)); spm.gravityModifier = 0.6f;
            var sps = sparks.shape; sps.shapeType = ParticleSystemShapeType.Sphere; sps.radius = 0.08f;
            var spe = sparks.emission; spe.rateOverTime = 0f;
            var cloth = MakeFire("Tablecloth", V(19.5f, 0.85f, 2.8f), FireType.Ordinary, 0.6f, 1.0f, cake, 1.0f);
            cloth.growth = 0.012f; cloth.smokeRate = 1.2f;
            var spark = cake.gameObject.AddComponent<SparklerHazard>();
            spark.displayName = "Sparklers"; spark.sparks = sparks; spark.clothFire = cloth;
            spark.interactAnchor = G("Anchor", cake, V(19.5f, 0, 1.4f)); spark.interactRadius = 1.6f;
            spark.questEn = "Blow out the sparklers on the cake!"; spark.questTh = "เป่าดอกไม้ไฟบนเค้กให้ดับ!";
            spark.puzzle = MiniGame.Kind.Blow; spark.puzzleEn = "BLOW!! BLOW!!"; spark.puzzleTh = "เป่า!! เป่า!!";
            spark.startEn = "Someone lit the cake SPARKLERS... they're dripping onto the tablecloth! (dining)"; spark.startTh = "ใครจุดดอกไม้ไฟบนเค้ก... สะเก็ดไฟหยดลงผ้าปูโต๊ะ! (ห้องกินข้าว)";
            spark.failEn = "The TABLECLOTH caught fire!"; spark.failTh = "ผ้าปูโต๊ะติดไฟ!";
            spark.doneEn = "Phew... sparklers out."; spark.doneTh = "เฮ้อ... ดับแล้ว";
            spark.momEn = "Why is there a half-burnt cake on my dining table?"; spark.momTh = "ทำไมมีเค้กไหม้ครึ่ง ๆ บนโต๊ะกินข้าว?";
            spark.startAt = 160f; spark.escalateTime = 40f; spark.severity = 1; spark.loopClip = "sparkle"; spark.loopVolume = 0.3f;

            // ---------------------------------------------------------- GAME ROOM: fish tank
            var aq = G("Fish tank", gParty, V(29.25f, 0f, 9.6f));
            Cube("Stand", aq, V(29.25f, 0.4f, 9.6f), V(0.6f, 0.8f, 1.3f), M("BenchDark", C("8A5E36")), true);
            var body = G("TankBody", aq, V(29.25f, 0.8f, 9.6f));
            Cube("Glass", body, V(29.25f, 1.1f, 9.6f), V(0.55f, 0.6f, 1.25f), MT("TankGlass", new Color(0.75f, 0.95f, 1f, 0.25f), 0.95f));
            Cube("Water", body, V(29.25f, 1.05f, 9.6f), V(0.5f, 0.45f, 1.2f), MT("TankWater", new Color(0.35f, 0.7f, 0.95f, 0.45f), 0.95f));
            Cube("Gravel", body, V(29.25f, 0.84f, 9.6f), V(0.5f, 0.06f, 1.2f), M("Gravel", C("D9B27C")));
            Cube("Crack", body, V(28.97f, 1.0f, 9.5f), V(0.005f, 0.3f, 0.04f), M("Black", C("1A1A1F"), 0.8f), false, V(25, 0, 0));
            var drip = NewPS("Drip", body, V(28.95f, 0.9f, 9.5f), ParticleMat("FX_Water", false), 200);
            var dm = drip.main; dm.startLifetime = 0.5f; dm.startSpeed = 0.6f; dm.startSize = 0.05f; dm.gravityModifier = 1.8f; dm.startColor = new Color(0.7f, 0.88f, 1f, 0.8f);
            var dsh = drip.shape; dsh.shapeType = ParticleSystemShapeType.Cone; dsh.angle = 10f; dsh.rotation = V(0, -90, 0);
            var de = drip.emission; de.rateOverTime = 0f;
            tankHz = aq.gameObject.AddComponent<AquariumHazard>();
            tankHz.displayName = "Fish tank"; tankHz.tankBody = body; tankHz.leakPoint = G("Leak", aq, V(28.7f, 0, 9.5f)); tankHz.drip = drip;
            tankHz.interactAnchor = G("Anchor", aq, V(28.4f, 0, 9.6f)); tankHz.interactRadius = 1.4f;
            tankHz.questEn = "Patch the cracked fish tank!"; tankHz.questTh = "อุดรอยร้าวตู้ปลา!";
            tankHz.puzzle = MiniGame.Kind.Tape; tankHz.puzzleEn = "TAPE THE CRACK!"; tankHz.puzzleTh = "แปะเทปปิดรอยร้าว!";
            tankHz.failEn = "The fish tank is pouring out! The game room is flooding!"; tankHz.failTh = "น้ำในตู้ปลาไหลทะลักแล้ว! ห้องเล่นเกมน้ำท่วม!";
            tankHz.doneEn = "Tank patched! Put the fish back in!"; tankHz.doneTh = "อุดตู้ปลาแล้ว! เอาปลาใส่คืนได้!";
            tankHz.momEn = "Why is the fish tank cracked and taped?!"; tankHz.momTh = "ทำไมตู้ปลาร้าวแล้วแปะเทปไว้?!";
            tankHz.startAt = 280f; tankHz.escalateTime = 90f; tankHz.severity = 2; tankHz.loopClip = "water"; tankHz.loopVolume = 0.15f;
            var fishList = new List<Fish>();
            Color[] fc = { C("FF8A2E"), C("FFB347"), C("FF6F61") };
            for (int i = 0; i < 3; i++)
            {
                var home = G("FishHome" + i, body, V(29.25f, 1.05f, 9.2f + i * 0.4f));
                var f = Prop<Fish>("Goldfish", home.position, 0.15f, "Goldfish");
                var fb = L(PrimitiveType.Sphere, "Body", f.transform, V(0, 0, 0), V(0.08f, 0.07f, 0.14f), M("Fish" + i, fc[i], 0.6f));
                L(PrimitiveType.Cube, "Tail", f.transform, V(0, 0, -0.09f), V(0.01f, 0.07f, 0.05f), M("Fish" + i, fc[i], 0.6f), false, V(45, 0, 0));
                BoxCol(f.gameObject, Vector3.zero, V(0.09f, 0.08f, 0.16f));
                f.transform.SetParent(home, true); f.startKinematic = true; f.tank = tankHz; f.homeInTank = home; f.body = fb.GetComponent<Renderer>();
                f.tool = ToolType.Prop; f.holdOffset = V(0, 0.05f, 0.05f);
                fishList.Add(f);
            }
            tankHz.fish = fishList.ToArray();

            // ---------------------------------------------------------- GAME ROOM: speakers / DJ desk
            var dj = G("DJ desk", gParty, V(26f, 0f, 13.35f));
            Cube("Desk", dj, V(26f, 0.45f, 13.35f), V(1.6f, 0.9f, 0.6f), M("DJBlack", C("232330")), true);
            Cube("Mixer", dj, V(26f, 0.93f, 13.3f), V(0.6f, 0.05f, 0.35f), M("MixerGrey", C("5A5F6A"), 0.5f));
            Cube("Laptop", dj, V(25.4f, 0.93f, 13.3f), V(0.35f, 0.02f, 0.25f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            for (int i = 0; i < 4; i++) Cube("Fader", dj, V(25.82f + i * 0.12f, 0.97f, 13.3f), V(0.03f, 0.03f, 0.08f), ME("FaderGlow", C("FF4FD8"), new Color(2f, 0.4f, 1.6f)));
            var cones = new List<Transform>();
            foreach (var sx in new[] { 22.7f, 29.3f })
            {
                Cube("Speaker", dj, V(sx, 0.75f, 13.35f), V(0.7f, 1.5f, 0.6f), M("Speaker", C("3A3A44")), true);
                foreach (var y in new[] { 0.45f, 1.1f })
                {
                    var cone = G("Cone", dj, V(sx, y, 13.03f));
                    L(PrimitiveType.Cylinder, "C", cone, Vector3.zero, V(y < 0.6f ? 0.5f : 0.3f, 0.02f, y < 0.6f ? 0.5f : 0.3f), M("ConeBlack", C("15151A")), false, V(90, 0, 0));
                    cones.Add(cone);
                }
            }
            var ball = G("DiscoBall", dj, V(26f, 2.45f, 11f));
            L(PrimitiveType.Sphere, "Ball", ball, Vector3.zero, Vector3.one * 0.35f, M("Mirror", C("D8EEF5"), 0.95f, 0.8f));
            var discos = new List<Light>();
            Color[] dcs = { C("FF4FD8"), C("4FD8FF"), C("FFE04F") };
            for (int i = 0; i < 3; i++)
            {
                var dl = new GameObject("Disco" + i).AddComponent<Light>(); dl.transform.SetParent(dj, false);
                dl.transform.position = V(23.5f + i * 2.5f, 2.5f, 10.5f); dl.type = LightType.Point; dl.range = 4.5f; dl.intensity = 2.2f; dl.color = dcs[i];
                discos.Add(dl);
            }
            var spk = dj.gameObject.AddComponent<SpeakerHazard>();
            spk.displayName = "DJ mixer"; spk.cones = cones.ToArray(); spk.disco = discos.ToArray(); spk.discoBall = ball; spk.tank = tankHz;
            spk.interactAnchor = G("Anchor", dj, V(26f, 0, 12.55f)); spk.interactRadius = 1.4f;
            spk.questEn = "Turn the party music DOWN (neighbours!)"; spk.questTh = "ลดเสียงเพลงปาร์ตี้ (ข้างบ้านรำคาญ!)";
            spk.puzzle = MiniGame.Kind.Simon; spk.puzzleEn = "SHUT DOWN THE MIXER (repeat the lights)"; spk.puzzleTh = "ปิดมิกเซอร์ (กดตามไฟ)";
            spk.failEn = "The speakers OVERLOADED the circuit!"; spk.failTh = "ลำโพงดึงไฟเกิน!";
            spk.doneEn = "Silence... sweet silence."; spk.doneTh = "เงียบแล้ว... สบายหู";
            spk.momEn = "Why are there DJ speakers in my house?!"; spk.momTh = "ทำไมมีลำโพงดีเจในบ้าน?!";
            spk.startActive = true; spk.escalateTime = 120f; spk.needsPower = true; spk.severity = 2; spk.loopClip = "party"; spk.loopVolume = 0.32f;

            // ---------------------------------------------------------- LAUNDRY: soap-foam washer
            var wa = gRooms.Find("Laundry/Washer");
            var drum = G("Drum", wa, V(13.75f, 0.45f, 0.8f));
            L(PrimitiveType.Cube, "Clothes", drum, V(0.08f, 0, 0), V(0.15f, 0.08f, 0.02f), M("Shirt1", C("FF6F61")));
            L(PrimitiveType.Cube, "Clothes2", drum, V(-0.08f, 0.05f, 0), V(0.12f, 0.1f, 0.02f), M("Shirt2", C("6CC63A")));
            var foamPile = G("FoamPile", wa, V(13.75f, 0.9f, 0.9f));
            for (int i = 0; i < 5; i++) L(PrimitiveType.Sphere, "Foam", foamPile, V((i - 2) * 0.12f, (i % 2) * 0.08f, 0.05f), Vector3.one * (0.22f + (i % 3) * 0.05f), foamMat);
            var washer = wa.gameObject.AddComponent<WasherHazard>();
            washer.displayName = "Washing machine"; washer.foamOrigin = G("FoamOrigin", wa, V(13.75f, 0, 1.0f)); washer.foamMat = foamMat; washer.drum = drum;
            washer.showWhenActive = new[] { foamPile.gameObject };
            washer.interactAnchor = G("HzAnchor", wa, V(13.25f, 0, 1.25f)); washer.interactRadius = 1.2f;
            washer.questEn = "STOP the washing machine (soap foam!)"; washer.questTh = "หยุดเครื่องซักผ้า (ฟองสบู่ล้น!)";
            washer.puzzle = MiniGame.Kind.Simon; washer.puzzleEn = "PRESS THE STOP SEQUENCE"; washer.puzzleTh = "กดปุ่มหยุดตามลำดับ";
            washer.startEn = "Someone put DISH SOAP in the washer... FOAM is pouring out! (laundry)"; washer.startTh = "ใครใส่น้ำยาล้างจานในเครื่องซักผ้า... ฟองทะลักออกมา! (ห้องซักผ้า)";
            washer.failEn = "Foam everywhere... and it's getting near the socket!"; washer.failTh = "ฟองเต็มห้อง... ใกล้ปลั๊กไฟแล้ว!";
            washer.doneEn = "Washer stopped! Now mop that foam."; washer.doneTh = "หยุดเครื่องแล้ว! ถูฟองด้วย";
            washer.momEn = "Why is the washer FULL of bubbles?!"; washer.momTh = "ทำไมเครื่องซักผ้ามีแต่ฟอง?!";
            washer.startAt = 130f; washer.escalateTime = 90f; washer.needsPower = true; washer.severity = 1; washer.loopClip = "whir"; washer.loopVolume = 0.28f;

            // ---------------------------------------------------------- BATHROOM: clogged toilet
            var to = gRooms.Find("Bathroom/Toilet");
            var toHz = G("ToiletClog", to, V(11.4f, 0.45f, 12.9f));
            var over = G("Overflow", toHz, V(11.4f, 0.46f, 12.9f));
            L(PrimitiveType.Cylinder, "Water", over, Vector3.zero, V(0.44f, 0.01f, 0.38f), MT("TubWater", new Color(0.45f, 0.72f, 0.95f, 0.75f), 0.95f));
            var toilet = toHz.gameObject.AddComponent<ToiletHazard>();
            toilet.displayName = "Toilet"; toilet.overflowPoint = G("Spill", toHz, V(10.9f, 0, 12.9f)); toilet.overflowFx = over.gameObject;
            toilet.interactAnchor = G("Anchor", toHz, V(10.85f, 0, 12.35f)); toilet.interactRadius = 1.1f;
            toilet.questEn = "Unclog the overflowing toilet (plunger!)"; toilet.questTh = "ทะลวงชักโครกที่ล้น (ใช้ที่ปั๊ม!)";
            toilet.puzzle = MiniGame.Kind.Plunger; toilet.puzzleEn = "PLUNGE IT!"; toilet.puzzleTh = "ปั๊ม!";
            toilet.startEn = "*FLUSH* ...someone clogged the toilet and it's OVERFLOWING! (bathroom)"; toilet.startTh = "*ซู่* ...ส้วมตันแล้วน้ำล้น! (ห้องน้ำ)";
            toilet.failEn = "The toilet is gushing - ewww!!"; toilet.failTh = "น้ำส้วมทะลัก - อี๋!!";
            toilet.doneEn = "*SHLOOP* Unclogged!"; toilet.doneTh = "*ปุ๊ก* หายตันแล้ว!";
            toilet.momEn = "Why is the toilet... wet... everywhere?!"; toilet.momTh = "ทำไมรอบชักโครก... เปียกไปหมด?!";
            toilet.startAt = 210f; toilet.escalateTime = 100f; toilet.severity = 2; toilet.loopClip = "water"; toilet.loopVolume = 0.15f;

            // ---------------------------------------------------------- BEDROOM: iron on Mom's dress
            var ib = G("Ironing board", gParty, V(3.6f, 0f, 9.3f));
            Cube("Board", ib, V(3.6f, 0.85f, 9.3f), V(1.3f, 0.04f, 0.38f), M("BoardCover", C("BFE8F0")), true);
            Cube("LegA", ib, V(3.3f, 0.42f, 9.3f), V(0.04f, 0.85f, 0.3f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f), true, V(0, 0, 25));
            Cube("LegB", ib, V(3.9f, 0.42f, 9.3f), V(0.04f, 0.85f, 0.3f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f), true, V(0, 0, -25));
            var dress = Cube("Mom's dress", ib, V(3.5f, 0.88f, 9.3f), V(0.9f, 0.015f, 0.34f), M("DressPurple", C("BF73F2")));
            Cube("Iron", ib, V(3.75f, 0.94f, 9.3f), V(0.26f, 0.1f, 0.14f), M("IronBlue", C("4A7BE0"), 0.5f));
            Cube("IronHandle", ib, V(3.75f, 1.02f, 9.3f), V(0.18f, 0.03f, 0.04f), M("Black", C("1A1A1F"), 0.8f));
            var iLed = Sph("IronLED", ib, V(3.66f, 1.0f, 9.37f), Vector3.one * 0.025f, ME("LED", C("FF2222"), new Color(3f, 0.1f, 0.1f)));
            Cube("Cord", ib, V(3.9f, 0.45f, 9.55f), V(0.02f, 0.9f, 0.02f), M("Black", C("1A1A1F"), 0.8f));
            var isteam = NewPS("Steam", ib, V(3.75f, 1.0f, 9.3f), ParticleMat("FX_Smoke", false), 120);
            SetupMist(isteam, new Color(1f, 1f, 1f, 0.45f), 0.5f);
            var bfire = MakeFire("Ironing board", V(3.6f, 0.9f, 9.3f), FireType.Ordinary, 0.35f, 0.8f, ib, 0.8f); bfire.growth = 0.014f;
            var iron = ib.gameObject.AddComponent<IronHazard>();
            iron.displayName = "Iron"; iron.boardFire = bfire; iron.steam = isteam; iron.dress = dress.GetComponent<Renderer>(); iron.ironLight = iLed.transform;
            iron.interactAnchor = G("Anchor", ib, V(3.9f, 0, 9.9f)); iron.interactRadius = 1.3f;
            iron.questEn = "Unplug the iron - it's burning Mom's dress!"; iron.questTh = "ถอดปลั๊กเตารีด - มันกำลังไหม้ชุดแม่!";
            iron.puzzle = MiniGame.Kind.Tug; iron.puzzleEn = "PULL THE PLUG!"; iron.puzzleTh = "ดึงปลั๊ก!";
            iron.startEn = "Is that... burning fabric? The IRON is on Mom's dress! (bedroom)"; iron.startTh = "กลิ่นผ้าไหม้... เตารีดวางทับชุดแม่! (ห้องนอน)";
            iron.failEn = "Mom's dress is SCORCHED - and the ironing board caught fire!"; iron.failTh = "ชุดแม่ไหม้เป็นรู - แล้วโต๊ะรีดผ้าก็ติดไฟ!";
            iron.doneEn = "Iron unplugged!"; iron.doneTh = "ถอดปลั๊กเตารีดแล้ว!";
            iron.momEn = "Why was my iron out? And is that my GOOD dress?!"; iron.momTh = "ทำไมเตารีดออกมาวาง? แล้วนั่นชุดดีของแม่ใช่ไหม?!";
            iron.momFailEn = "MY SILK DRESS!!! THERE'S AN IRON-SHAPED HOLE IN IT!!!"; iron.momFailTh = "ชุดผ้าไหมของแม่!!! เป็นรูรูปเตารีดเลย!!!";
            iron.startAt = 50f; iron.escalateTime = 75f; iron.needsPower = true; iron.severity = 2; iron.loopClip = "sizzle"; iron.loopVolume = 0.12f;

            // ---------------------------------------------------------- BEDROOM: hamster on the loose
            var cageRoot = G("Hamster cage", gParty, V(0.6f, 0f, 8.7f));
            Cube("CageBase", cageRoot, V(0.6f, 0.04f, 8.7f), V(0.7f, 0.08f, 0.5f), M("CageBlue", C("4FA3E0")), true);
            for (int i = 0; i < 6; i++) Cyl("Bar", cageRoot, V(0.27f + i * 0.13f, 0.25f, 8.47f), V(0.01f, 0.2f, 0.01f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cube("CageTop", cageRoot, V(0.6f, 0.46f, 8.7f), V(0.7f, 0.02f, 0.5f), M("Chrome", C("D5DAE0"), 0.9f, 0.9f));
            Cyl("Wheel", cageRoot, V(0.85f, 0.2f, 8.8f), V(0.25f, 0.02f, 0.25f), M("YellowPlastic", C("FFD447")), false, V(90, 0, 0));
            var ham = Prop<Hamster>("Hamster", V(3.0f, 0.02f, 10.2f), 0.1f, "Hamster");
            L(PrimitiveType.Sphere, "Body", ham.transform, V(0, 0.06f, 0), V(0.13f, 0.1f, 0.17f), M("Hamster", C("F2B36B"), 0.2f));
            L(PrimitiveType.Sphere, "Belly", ham.transform, V(0, 0.045f, 0.02f), V(0.1f, 0.06f, 0.12f), M("HamsterBelly", C("FFF1D6")));
            L(PrimitiveType.Sphere, "EarL", ham.transform, V(-0.04f, 0.11f, 0.05f), Vector3.one * 0.03f, M("HamsterEar", C("FF9FB1")));
            L(PrimitiveType.Sphere, "EarR", ham.transform, V(0.04f, 0.11f, 0.05f), Vector3.one * 0.03f, M("HamsterEar", C("FF9FB1")));
            BoxCol(ham.gameObject, V(0, 0.06f, 0), V(0.14f, 0.12f, 0.18f));
            ham.homePosition = V(0.6f, 0.1f, 8.75f); ham.homeRotation = Quaternion.identity; ham.homeSet = true; ham.snapDistance = 1.2f;
            ham.holdOffset = V(0, 0.1f, 0.05f);
            ham.nodes = new[]
            {
                V(3f, 0, 10.5f), V(1.6f, 0, 12.2f), V(3f, 0, 8.6f),
                V(3f, 0, 7f), V(5f, 0, 7f), V(9.5f, 0, 7f), V(15f, 0, 7f), V(19.5f, 0, 7f), V(24f, 0, 7f), V(28.5f, 0, 7f),
                V(5f, 0, 5.2f), V(2f, 0, 1.5f), V(9f, 0, 2f), V(11.5f, 0, 4.2f),
                V(9.5f, 0, 8.6f), V(9.6f, 0, 10.3f),
                V(16f, 0, 8.6f), V(14f, 0, 10.5f), V(19f, 0, 11.6f),
                V(15f, 0, 5.4f), V(15.2f, 0, 3.2f),
                V(19.5f, 0, 5.4f), V(21.2f, 0, 4.5f),
                V(23.9f, 0, 5.3f), V(25.5f, 0, 2.6f), V(28.3f, 0, 3.5f),
                V(24.5f, 0, 8.7f), V(26.5f, 0, 10.6f), V(28.2f, 0, 12.3f),
            };
            ham.links = new[]
            {
                new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(2,3), new Vector2Int(3,4), new Vector2Int(4,5), new Vector2Int(5,6), new Vector2Int(6,7), new Vector2Int(7,8), new Vector2Int(8,9),
                new Vector2Int(4,10), new Vector2Int(10,11), new Vector2Int(10,12), new Vector2Int(12,13), new Vector2Int(11,12),
                new Vector2Int(5,14), new Vector2Int(14,15), new Vector2Int(6,16), new Vector2Int(16,17), new Vector2Int(16,18),
                new Vector2Int(6,19), new Vector2Int(19,20), new Vector2Int(7,21), new Vector2Int(21,22),
                new Vector2Int(8,23), new Vector2Int(23,24), new Vector2Int(24,25), new Vector2Int(8,26), new Vector2Int(26,27), new Vector2Int(27,28),
            };
            ham.momLine = "Why is the HAMSTER running loose in my house?!";

            // ---------------------------------------------------------- LIVING ROOM: cake on the ceiling fan
            var cf = G("Ceiling fan", gParty, V(4.0f, 2.55f, 3.0f));
            Cyl("Rod", cf, V(4.0f, 2.68f, 3.0f), V(0.04f, 0.12f, 0.04f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
            var blades = G("Blades", cf, V(4.0f, 2.52f, 3.0f));
            L(PrimitiveType.Cylinder, "Hub", blades, Vector3.zero, V(0.22f, 0.05f, 0.22f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
            for (int i = 0; i < 4; i++) L(PrimitiveType.Cube, "Blade", blades, Quaternion.Euler(0, i * 90, 0) * V(0.45f, 0, 0), V(0.75f, 0.02f, 0.16f), M("FanWood", C("C98B5A")), false, V(0, i * 90, 0));
            var blob = G("CakeBlob", blades, V(4.65f, 2.55f, 3.0f));
            L(PrimitiveType.Sphere, "Cake", blob, Vector3.zero, V(0.25f, 0.12f, 0.25f), M("Frosting", C("FFD1E3"), 0.4f));
            var sw = G("Fan switch", gParty, V(6.45f, 1.2f, 5.9f));
            Cube("Plate", sw, V(6.45f, 1.2f, 5.92f), V(0.1f, 0.14f, 0.01f), M("White", C("FFFFFF")));
            var tvs = Cube("TVSplat", gParty, V(0.43f, 1.1f, 3.0f), V(0.02f, 0.45f, 0.7f), M("Frosting", C("FFD1E3"), 0.4f)); tvs.SetActive(false);
            var fan = cf.gameObject.AddComponent<CeilingFanHazard>();
            fan.displayName = "Ceiling fan switch"; fan.blades = blades; fan.cakeBlob = blob; fan.frostingMat = frostingMat; fan.tvSplat = tvs.transform; fan.area = new Rect(1f, 0.6f, 10f, 5f);
            fan.interactAnchor = G("Anchor", sw, V(6.45f, 0, 5.3f)); fan.interactRadius = 1.2f;
            fan.questEn = "Switch off the ceiling fan (cake is flying!)"; fan.questTh = "ปิดพัดลมเพดาน (เค้กปลิวว่อน!)";
            fan.failEn = "SPLAT! The whole cake flew off - right onto the TV!"; fan.failTh = "แปะ! เค้กทั้งก้อนปลิวไปติดจอทีวี!";
            fan.doneEn = "Fan off. Now wipe up the frosting."; fan.doneTh = "ปิดพัดลมแล้ว เช็ดครีมด้วย";
            fan.momEn = "Is that CAKE on my ceiling fan?!"; fan.momTh = "นั่นเค้กติดพัดลมเพดานเหรอ?!";
            fan.momFailEn = "WHY IS THERE CAKE ON THE TV?!"; fan.momFailTh = "ทำไมมีเค้กติดจอทีวี?!";
            fan.startActive = true; fan.escalateTime = 80f; fan.needsPower = true; fan.severity = 1; fan.loopClip = "whir"; fan.loopVolume = 0.2f;

            // ---------------------------------------------------------- LIVING ROOM: broken vase
            var vz = G("Broken vase", gParty, V(7.3f, 0.01f, 1.0f));
            var shards = G("Shards", vz, V(7.3f, 0.01f, 1.0f));
            for (int i = 0; i < 6; i++) L(PrimitiveType.Cube, "Shard", shards, V(Mathf.Sin(i * 1.7f) * 0.25f, 0.01f, Mathf.Cos(i * 2.3f) * 0.2f), V(0.08f, 0.015f, 0.06f), M("Vase", C("43CFC4"), 0.6f), false, V(0, i * 40, 0));
            var glued = Prop<MessyProp>("Glued vase", V(7.3f, 0.05f, 1.1f), 0.5f, "Glued vase");
            L(PrimitiveType.Cylinder, "Vase", glued.transform, V(0, 0.15f, 0), V(0.16f, 0.15f, 0.16f), M("Vase", C("43CFC4"), 0.6f));
            L(PrimitiveType.Cube, "GlueLine", glued.transform, V(0.08f, 0.15f, 0), V(0.005f, 0.25f, 0.02f), M("Glue", C("FFFBEA")), false, V(0, 0, 20));
            BoxCol(glued.gameObject, V(0, 0.15f, 0), V(0.16f, 0.3f, 0.16f));
            SetHome(glued, V(7.0f, 0.78f, 0.25f), Vector3.zero, V(7.3f, 0.05f, 1.1f), Vector3.zero);
            glued.momLine = "Is that... GLUE on my vase?!";
            glued.gameObject.SetActive(false);
            var vase = vz.gameObject.AddComponent<VaseHazard>();
            vase.displayName = "Broken vase"; vase.shards = shards.gameObject; vase.gluedVase = glued;
            vase.interactAnchor = G("Anchor", vz, V(7.3f, 0, 1.6f)); vase.interactRadius = 1.3f;
            vase.questEn = "Glue Mom's broken vase back together"; vase.questTh = "ต่อแจกันที่แตกของแม่";
            vase.puzzle = MiniGame.Kind.Glue; vase.puzzleEn = "FIND THE PIECE THAT FITS"; vase.puzzleTh = "หาชิ้นที่พอดี";
            vase.doneEn = "Good as new! (almost). Put it back on the entry table."; vase.doneTh = "เหมือนใหม่! (เกือบ) เอาไปวางคืนบนโต๊ะทางเข้า";
            vase.startActive = true; vase.escalateTime = 9999f; vase.severity = 3;

            // ---------------------------------------------------------- LIVING ROOM: sofa flipped
            var sofaT = gRooms.Find("LivingRoom/Sofa");
            var sf = G("Flipped sofa", gParty, V(5.6f, 0, 3.0f));
            var sofa = sf.gameObject.AddComponent<SofaHazard>();
            sofa.displayName = "Sofa"; sofa.sofa = sofaT; sofa.interactAnchor = G("Anchor", sf, V(4.6f, 0, 3.0f)); sofa.interactRadius = 2.0f;
            sofa.questEn = "Flip the sofa back (lift it together!)"; sofa.questTh = "พลิกโซฟากลับ (ช่วยกันยก!)";
            sofa.doneEn = "Heave-ho! Sofa's back."; sofa.doneTh = "ฮึบ! โซฟาเข้าที่แล้ว";
            sofa.momEn = "Why is my sofa UPSIDE DOWN?!"; sofa.momTh = "ทำไมโซฟาคว่ำ?!";
            sofa.startActive = true; sofa.escalateTime = 9999f; sofa.coop = true; sofa.severity = 2;

            // ---------------------------------------------------------- party garbage (goes to the garage bins)
            Vector3[] bagStart = { V(9.5f, 0f, 3.8f), V(20.5f, 0f, 4.8f), V(27.8f, 0f, 11.8f) };
            for (int i = 0; i < bagStart.Length; i++)
            {
                var bag = Prop<MessyProp>("Trash bag", bagStart[i], 3f, "Trash bag");
                L(PrimitiveType.Sphere, "Bag", bag.transform, V(0, 0.3f, 0), V(0.55f, 0.6f, 0.5f), M("TrashBag", C("1E1F24"), 0.75f));
                L(PrimitiveType.Sphere, "Knot", bag.transform, V(0, 0.62f, 0), V(0.12f, 0.12f, 0.12f), M("TrashBag", C("1E1F24"), 0.75f));
                BoxCol(bag.gameObject, V(0, 0.3f, 0), V(0.5f, 0.6f, 0.45f));
                bag.carrySpeedMultiplier = 0.6f; bag.holdOffset = V(0, -0.3f, 0.2f);
                SetHome(bag, V(24.0f + i * 0.55f, 0.02f, 1.5f), V(0, i * 50, 0), bagStart[i], V(0, i * 70, 0));
                bag.momLine = "Why are there TRASH BAGS all over my house?!";
            }
            Vector3[] cupStart = { V(3.2f, 0.45f, 2.6f), V(11.0f, 0.02f, 1.2f), V(24.3f, 0.02f, 11.0f), V(27.3f, 0.5f, 8.6f), V(18.8f, 0.8f, 2.5f) };
            for (int i = 0; i < cupStart.Length; i++)
            {
                var cup = Prop<MessyProp>("Party cup", cupStart[i], 0.1f, "Party cup");
                L(PrimitiveType.Cylinder, "Cup", cup.transform, V(0, 0.06f, 0), V(0.08f, 0.06f, 0.08f), M("RedCup", C("E53935"), 0.5f));
                BoxCol(cup.gameObject, V(0, 0.06f, 0), V(0.09f, 0.12f, 0.09f));
                SetHome(cup, V(27.25f + (i % 3) * 0.12f, 0.82f + (i / 3) * 0.1f, 0.5f + (i % 2) * 0.12f), Vector3.zero, cupStart[i], V(i % 2 == 0 ? 90 : 0, 0, 0));
                cup.momLine = "Why are there plastic CUPS everywhere?! Was there a PARTY?!";
            }
            Vector3[] boxStart = { V(1.6f, 0.02f, 4.2f), V(25.2f, 0.02f, 9.6f) };
            for (int i = 0; i < boxStart.Length; i++)
            {
                var box = Prop<MessyProp>("Pizza box", boxStart[i], 0.4f, "Pizza box");
                L(PrimitiveType.Cube, "Box", box.transform, V(0, 0.025f, 0), V(0.42f, 0.05f, 0.42f), M("PizzaBox", C("D9B27C")));
                L(PrimitiveType.Cube, "Logo", box.transform, V(0, 0.052f, 0), V(0.18f, 0.002f, 0.18f), M("RedPlastic", C("FF5A5A")));
                BoxCol(box.gameObject, V(0, 0.025f, 0), V(0.42f, 0.05f, 0.42f));
                SetHome(box, V(25.3f, 0.95f + i * 0.06f, 0.6f), V(0, 10 + i * 30, 0), boxStart[i], V(0, i * 35, 0));
                box.momLine = "Pizza boxes?! You said you'd eat the CURRY!";
            }
            var rnd = new System.Random(11);
            for (int i = 0; i < 9; i++)
            {
                var p = i < 5 ? V(1f + (float)rnd.NextDouble() * 10f, 0.013f, 0.8f + (float)rnd.NextDouble() * 4.6f) : V(23f + (float)rnd.NextDouble() * 6f, 0.013f, 9f + (float)rnd.NextDouble() * 4f);
                SplatAt(SplatKind.Confetti, p, 0.6f, ConfettiMat(i));
            }
            SplatAt(SplatKind.Soda, V(25.6f, 0.014f, 9.8f), 0.9f, sodaMat);
            SplatAt(SplatKind.Soda, V(27.6f, 0.014f, 11.5f), 0.7f, sodaMat);
            SplatAt(SplatKind.Soda, V(8.2f, 0.014f, 7.1f), 0.7f, sodaMat);
        }

        static Material ConfettiMat(int i)
        {
            string[] cols = { "FF6FB5", "43CFC4", "FFD447", "7A5CD6", "6CC63A" };
            return M("Confetti" + (i % 5), C(cols[i % 5]), 0.3f);
        }

        static void SplatAt(SplatKind k, Vector3 p, float size, Material m)
        {
            var g = Cyl(k + " splat", gParty, p, V(size, 0.003f, size * 0.8f), m, false, V(0, p.x * 37f, 0));
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var s = g.AddComponent<SplatMess>(); s.kind = k; s.sticky = k == SplatKind.Soda;
        }

        static HeaterValve HeaterValveAt(WaterHeaterHazard heater, Vector3 pos, Vector3 anchor, string name)
        {
            var v = G(name, heater.transform, pos);
            var wheel = G("Wheel", v, pos);
            L(PrimitiveType.Cylinder, "Rim", wheel, Vector3.zero, V(0.24f, 0.02f, 0.24f), M("ValveRed", C("E53935"), 0.4f), false, V(90, 0, 0));
            L(PrimitiveType.Cube, "Spoke", wheel, Vector3.zero, V(0.22f, 0.03f, 0.03f), M("ValveRed", C("E53935"), 0.4f));
            L(PrimitiveType.Cube, "Spoke", wheel, Vector3.zero, V(0.03f, 0.22f, 0.03f), M("ValveRed", C("E53935"), 0.4f));
            Cube("Tag", v, pos + V(0, 0.2f, 0), V(0.12f, 0.08f, 0.01f), M("TagYellow", C("FFD447")));
            var hv = v.gameObject.AddComponent<HeaterValve>();
            hv.heater = heater; hv.wheel = wheel; hv.displayName = name; hv.interactAnchor = G("Anchor", v, anchor); hv.interactRadius = 1.2f;
            return hv;
        }

        static void SetupMist(ParticleSystem ps, Color col, float size)
        {
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size); main.startColor = col;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 15f; sh.radius = 0.08f; sh.rotation = V(-90, 0, 0);
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 2.5f));
            var c = ps.colorOverLifetime; c.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0f, 0), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1) });
            c.color = g;
            ps.Play();
        }
    }
}
