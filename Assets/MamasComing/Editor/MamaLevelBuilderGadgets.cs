using UnityEngine;

namespace MamasComing.EditorTools
{
    /// 22 more party-aftermath problems for the fruit-scale house (the original 28 stay as they were), so a round
    /// has ~50 things that can go wrong. Each one is a GadgetHazard with its own twist:
    /// tools that are also needed elsewhere (lid, wrench, mop, towel, bucket, chair, plunger), problems that can only
    /// be fixed with the power OFF, two-friend switches, runaway gadgets to corner, and different consequences.
    /// Authored in final world metres.
    public static partial class MamaLevelBuilder
    {
        static Transform gGadgets;

        static void Gadgets()
        {
            raw = true;
            gGadgets = G("Gadgets", gParty);
            float U = UpY;
            var slime = M("Slime", C("7BE04A"), 0.85f);
            var chrome = M("Chrome", C("D5DAE0"), 0.9f, 0.9f); var dark = M("Black", C("1A1A1F"), 0.8f);

            // ================================================================= GROUND FLOOR
            // 1. LIVING: Dusty the robot vacuum ran off with a party popper - spraying confetti wherever it goes
            {
                var root = G("Robot vacuum", gGadgets, V(17f, 0f, 6f));
                var body = G("Dusty", root, V(17f, 0f, 6f));
                Cyl("Shell", body, V(17f, 0.14f, 6f), V(0.9f, 0.12f, 0.9f), M("VacuumTeal", C("2EC4B6"), 0.6f));
                Cyl("Lid", body, V(17f, 0.28f, 6f), V(0.7f, 0.03f, 0.7f), M("VacuumTop", C("FFFFFF"), 0.7f));
                Sph("Eye", body, V(17f, 0.3f, 6.32f), V(0.16f, 0.1f, 0.06f), ME("VacuumEye", C("FF4FD8"), new Color(2.5f, 0.3f, 2f)));
                Cyl("Popper", body, V(17f, 0.5f, 5.9f), V(0.12f, 0.22f, 0.12f), M("Popper", C("FFD447")), false, V(-25, 0, 0));
                var g = Gz(root, "Robot vacuum", V(17f, 0, 6f), "Catch Dusty the robot vacuum and reboot it", "จับหุ่นดูดฝุ่นแล้วรีบูต",
                    "Dusty the robot vacuum is zooming around shooting CONFETTI! (living room)", "หุ่นดูดฝุ่นวิ่งพ่นกระดาษสีทั่วบ้าน! (ห้องนั่งเล่น)",
                    "Dusty jammed open the confetti cannon!", "หุ่นดูดฝุ่นยิงกระดาษสีไม่หยุด!", "Dusty is asleep. Good robot.", "หุ่นหลับแล้ว เด็กดี",
                    "Why is the vacuum covered in confetti?", "ทำไมหุ่นดูดฝุ่นมีกระดาษสีเต็มเลย?", 0f, 120f, 1);
                g.startActive = true; g.roamer = body; g.roamSpeed = 1.6f; g.fleeSpeed = 3.6f;
                g.roamPath = new[] { V(17f, 0, 6f), V(21f, 0, 9.5f), V(14f, 0, 10.5f), V(14f, 0, 14f), V(24f, 0, 14f), V(21f, 0, 8f), V(17f, 0, 3f), V(9f, 0, 1.6f) };
                g.puzzle = MiniGame.Kind.Simon; g.puzzleEn = "REBOOT DUSTY (repeat the lights)"; g.puzzleTh = "รีบูตหุ่น (กดตามไฟ)";
                g.splatKind = SplatKind.Confetti; g.splatMat = confettiMat; g.splatEvery = 3.5f; g.splatRadius = 0.6f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "whir"; g.loopVolume = 0.2f; g.interactRadius = 1.6f;
            }
            // 2. LIVING: the lava lamp has been on since the party and is starting to boil
            {
                var root = G("Lava lamp", gGadgets, V(6.4f, 0f, 11f));
                Cube("SideTable", root, V(6.4f, 0.6f, 11f), V(1.0f, 1.2f, 1.0f), M("LightWood", C("E8B27A")), true);
                Cyl("Base", root, V(6.4f, 1.35f, 11f), V(0.36f, 0.15f, 0.36f), chrome);
                var glass = G("Glass", root, V(6.4f, 1.5f, 11f));
                Cyl("Lava", glass, V(6.4f, 1.95f, 11f), V(0.3f, 0.45f, 0.3f), ME("LavaGlow", C("FF5A3C"), new Color(2.4f, 0.6f, 0.2f)));
                Sph("Blob", glass, V(6.4f, 2.2f, 11f), V(0.18f, 0.24f, 0.18f), ME("LavaBlob", C("FFD447"), new Color(2.2f, 1.4f, 0.2f)));
                var fire = MakeFire("Lava lamp", V(6.4f, 1.6f, 11f), FireType.Ordinary, 0.3f, 0.8f, root, 0.8f);
                var g = Gz(root, "Lava lamp", V(6.4f, 0, 9.6f), "Cool down the boiling lava lamp", "ทำให้โคมลาวาเย็นลง",
                    "The lava lamp is BUBBLING like a volcano... (living room)", "โคมลาวาเดือดปุด ๆ เหมือนภูเขาไฟ... (ห้องนั่งเล่น)",
                    "The lava lamp POPPED and set the side table on fire!", "โคมลาวาแตก ไฟลุกโต๊ะข้าง!", "Phew. Calm lava.", "เฮ้อ ลาวาสงบแล้ว",
                    "Why does it smell like hot wax in here?", "ทำไมมีกลิ่นเทียนร้อน ๆ?", 75f, 80f, 2);
                g.puzzle = MiniGame.Kind.Timing; g.puzzleEn = "TURN THE DIMMER TO LOW"; g.puzzleTh = "หมุนไฟให้อ่อนลง";
                g.wobbler = glass; g.wobble = 4f; g.needsPower = true; g.onFail = GadgetHazard.Consequence.Fire; g.failFire = fire; g.loopClip = "sizzle"; g.loopVolume = 0.12f;
            }
            // 3. LIVING: karaoke machine stuck on max with the mic howling
            {
                var root = G("Karaoke machine", gGadgets, V(2.6f, 0f, 9.6f));
                Cube("Cabinet", root, V(2.6f, 0.7f, 9.6f), V(1.2f, 1.4f, 1.0f), M("KaraokePink", C("FF4F9A"), 0.5f), true);
                Cyl("Woofer", root, V(2.6f, 0.65f, 10.12f), V(0.8f, 0.04f, 0.8f), dark, false, V(90, 0, 0));
                var mic = G("Mic", root, V(3.6f, 0f, 10.2f));
                Cyl("Stand", mic, V(3.6f, 0.9f, 10.2f), V(0.06f, 0.9f, 0.06f), chrome, true);
                Sph("Head", mic, V(3.6f, 1.9f, 10.2f), Vector3.one * 0.26f, M("MicGrille", C("BFC4CC"), 0.7f, 0.5f));
                var bl = new GameObject("Strobe").AddComponent<Light>(); bl.transform.SetParent(root, false); bl.transform.position = V(2.6f, 2.2f, 9.6f);
                bl.type = LightType.Point; bl.color = C("FF4FD8"); bl.range = 6f; bl.intensity = 2.5f;
                var g = Gz(root, "Karaoke machine", V(3.4f, 0, 8.2f), "Mute the howling karaoke machine", "ปิดเสียงคาราโอเกะที่หอนไม่หยุด",
                    null, null, "The neighbours are SINGING ALONG... and calling Mom!", "ข้างบ้านร้องตาม... แล้วโทรหาแม่!", "Silence. Beautiful silence.", "เงียบแล้ว เพราะจัง",
                    "Who was singing on my karaoke machine?!", "ใครร้องคาราโอเกะของแม่?!", 0f, 140f, 2);
                g.startActive = true; g.needsPower = true; g.neighbourPerSec = 0.6f; g.onFail = GadgetHazard.Consequence.Neighbour;
                g.puzzle = MiniGame.Kind.Simon; g.puzzleEn = "FIND THE MUTE COMBO"; g.puzzleTh = "กดรหัสปิดเสียง";
                g.wobbler = mic; g.wobble = 5f; g.blink = bl; g.loopClip = "party"; g.loopVolume = 0.22f;
            }
            // 4. KITCHEN: the fridge ice-maker won't stop - an ice avalanche melting into a puddle
            {
                var root = G("Ice maker", gGadgets, V(40.6f, 0f, 26.6f));
                Cube("Dispenser", root, V(40.6f, 2.2f, 26.4f), V(0.9f, 1.1f, 0.5f), M("IceMaker", C("BFE8F0"), 0.7f), true);
                var chute = G("Chute", root, V(40.6f, 1.7f, 26.1f));
                Cube("Spout", chute, V(40.6f, 1.7f, 26.05f), V(0.4f, 0.2f, 0.3f), chrome);
                for (int i = 0; i < 6; i++) Cube("Ice", root, V(40.2f + (i % 3) * 0.35f, 0.12f, 25.4f - (i / 3) * 0.4f), Vector3.one * 0.24f, MT("Ice", new Color(0.85f, 0.95f, 1f, 0.7f), 0.95f), false, V(i * 17, i * 31, 0));
                var fx = Burst("IceCubes", root, V(40.6f, 1.6f, 26f), new Color(0.85f, 0.95f, 1f, 0.9f), 0.12f, 2f, 2.5f, 1.4f);
                var g = Gz(root, "Ice maker", V(40.6f, 0, 24.6f), "Jam the runaway ice maker (wrench!)", "หยุดเครื่องทำน้ำแข็ง (ใช้ประแจ!)",
                    "The ice maker is pouring out an ICE AVALANCHE! (kitchen)", "เครื่องทำน้ำแข็งพ่นน้ำแข็งถล่มทลาย! (ครัว)",
                    "The ice melted into a LAKE!", "น้ำแข็งละลายเป็นทะเลสาบ!", "Ice maker jammed. Brrr.", "หยุดแล้ว หนาวเลย",
                    "Why is my kitchen floor freezing and wet?!", "ทำไมพื้นครัวทั้งเย็นทั้งเปียก?!", 150f, 100f, 2);
                g.need = GadgetHazard.Need.Wrench; g.needEn = "Need the WRENCH (garage)... or whoever has it!"; g.needTh = "ต้องใช้ประแจ (โรงรถ)... หรือใครถืออยู่!";
                g.puzzle = MiniGame.Kind.Bolts; g.puzzleEn = "TIGHTEN THE ICE CHUTE"; g.puzzleTh = "ขันรูน้ำแข็งให้แน่น";
                g.fx = fx; g.fxRate = 18f; g.waterPerSec = 0.35f; g.waterPoint = G("Melt", root, V(40.6f, 0, 25.2f));
                g.needsPower = true; g.onFail = GadgetHazard.Consequence.Water; g.failWater = 60f; g.loopClip = "whir"; g.loopVolume = 0.15f;
            }
            // 5. KITCHEN: a smoothie blender running with no lid
            {
                var root = G("Blender", gGadgets, V(43f, 1.8f, 23.4f));
                Cube("Motor", root, V(43f, 1.97f, 23.4f), V(0.5f, 0.34f, 0.5f), M("BlenderRed", C("E53935"), 0.5f), true);
                var jar = G("Jar", root, V(43f, 2.15f, 23.4f));
                Cyl("Jug", jar, V(43f, 2.55f, 23.4f), V(0.42f, 0.4f, 0.42f), MT("BlenderJug", new Color(0.8f, 0.95f, 1f, 0.35f), 0.9f));
                Cyl("Smoothie", jar, V(43f, 2.4f, 23.4f), V(0.38f, 0.25f, 0.38f), M("Smoothie", C("FF7AA8")));
                var fx = Burst("Spray", root, V(43f, 2.9f, 23.4f), new Color(1f, 0.55f, 0.7f, 1f), 0.08f, 3f, 1.2f, 1.6f);
                var g = Gz(root, "Blender", V(41.4f, 0, 23.4f), "Put a LID on the blender (and switch it off)", "เอาฝาปิดเครื่องปั่น (แล้วปิดสวิตช์)",
                    "The blender is spraying SMOOTHIE everywhere! (kitchen)", "เครื่องปั่นพ่นสมูทตี้เต็มครัว! (ครัว)",
                    "Smoothie on the CEILING!", "สมูทตี้ติดเพดาน!", "Lid on. Sticky, but quiet.", "ปิดฝาแล้ว เหนียวแต่เงียบ",
                    "Why is everything pink and sticky?!", "ทำไมทุกอย่างสีชมพูแล้วเหนียว?!", 120f, 70f, 1);
                g.need = GadgetHazard.Need.Lid; g.needEn = "Need a LID! (the pot lid... if the curry isn't on fire)"; g.needTh = "ต้องใช้ฝา! (ฝาหม้อ... ถ้าแกงไม่ไฟลุกอยู่)";
                g.holdSolo = 1.2f; g.fx = fx; g.fxRate = 40f; g.spinner = jar; g.spinSpeed = 900f; g.needsPower = true;
                g.splatKind = SplatKind.Frosting; g.splatMat = M("SmoothieSplat", C("FF7AA8"), 0.4f); g.splatEvery = 3f; g.splatRadius = 2.2f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "whir"; g.loopVolume = 0.25f;
            }
            // 6. KITCHEN: a party spoon jammed in the garbage disposal
            {
                var root = G("Garbage disposal", gGadgets, V(34f, 1.6f, 27f));
                var spoon = G("Spoon", root, V(34f, 1.75f, 27f));
                Cube("Handle", spoon, V(34f, 1.95f, 27f), V(0.06f, 0.5f, 0.04f), chrome, false, V(0, 0, 15));
                var fx = Burst("Grind", root, V(34f, 1.8f, 27f), new Color(1f, 0.9f, 0.5f, 1f), 0.04f, 2f, 1f, 0.6f);
                var g = Gz(root, "Garbage disposal", V(34f, 0, 25.4f), "Pull the spoon out of the garbage disposal", "ดึงช้อนออกจากเครื่องบดเศษอาหาร",
                    "GRRRRZZT! A spoon is stuck in the garbage disposal! (kitchen)", "แกร๊ก! ช้อนติดในเครื่องบดเศษอาหาร! (ครัว)",
                    "The disposal overheated and blew the fuse!", "เครื่องบดร้อนจัดจนฟิวส์ขาด!", "Spoon rescued. Bent, but rescued.", "ได้ช้อนคืนแล้ว งอนิดหน่อย",
                    "Why is my good spoon bent in half?", "ทำไมช้อนดีของแม่งอเป็นครึ่ง?", 240f, 75f, 1);
                g.puzzle = MiniGame.Kind.Tug; g.puzzleEn = "YANK THE SPOON!"; g.puzzleTh = "ดึงช้อน!"; g.needsPower = true;
                g.wobbler = spoon; g.wobble = 12f; g.fx = fx; g.fxRate = 25f; g.onFail = GadgetHazard.Consequence.TripPower; g.loopClip = "hum"; g.loopVolume = 0.25f;
            }
            // 7. GARAGE: the garage door opener is possessed - two buttons, held TOGETHER
            {
                var root = G("Garage door opener", gGadgets, V(52f, 0f, 0.6f));
                var panel = G("DoorPanel", root, V(52f, 2.5f, 0.45f));
                Cube("RollingDoor", panel, V(52f, 2.5f, 0.45f), V(9.6f, 4.4f, 0.12f), M("GarageDoorWhite", C("EDEDE8")), true);
                for (int i = 0; i < 6; i++) Cube("Slat", panel, V(52f, 0.6f + i * 0.75f, 0.53f), V(9.5f, 0.06f, 0.04f), M("GarageGrey", C("9AA3AD"), 0.3f));
                Cube("Motor", root, V(52f, 5.2f, 3f), V(1f, 0.5f, 1.2f), M("PanelGrey", C("B8BEC6"), 0.5f, 0.3f));
                var bl = new GameObject("Warning").AddComponent<Light>(); bl.transform.SetParent(root, false); bl.transform.position = V(52f, 4.9f, 3f);
                bl.type = LightType.Point; bl.color = C("FFB000"); bl.range = 7f; bl.intensity = 3f;
                var g = Gz(root, "Garage door", V(52f, 0, 3f), "Stop the haunted garage door (BOTH buttons together)", "หยุดประตูโรงรถผีสิง (กด 2 ปุ่มพร้อมกัน)",
                    "The garage door is going UP-DOWN-UP-DOWN by itself! (garage)", "ประตูโรงรถขึ้นลงเองไม่หยุด! (โรงรถ)",
                    "CLANG CLANG CLANG - the whole street can hear it!", "โครม ๆ ๆ - ได้ยินทั้งซอย!", "Door stopped. Spooky.", "ประตูหยุดแล้ว น่าขนลุก",
                    "Why is the garage door motor so HOT?", "ทำไมมอเตอร์ประตูโรงรถร้อนจัง?", 200f, 110f, 2);
                g.coop = true; g.needsPower = true; g.wobbler = panel; g.wobble = 2f; g.blink = bl; g.neighbourPerSec = 0.35f; g.onFail = GadgetHazard.Consequence.Neighbour;
                g.partners = new[] { Switch(g, root, V(44.35f, 1.6f, 10.4f), V(45.6f, 0, 10.4f), true), Switch(g, root, V(59.65f, 1.6f, 6.5f), V(58.4f, 0, 6.5f), true) };
                g.partnerWindow = 3f; g.loopClip = "hum"; g.loopVolume = 0.3f;
            }
            // 8. HALLWAY: the grandfather clock is chiming THIRTEEN - Mom will think she's late
            {
                var root = G("Grandfather clock", gGadgets, V(22.4f, 0f, 15.4f));
                Cube("Case", root, V(22.4f, 2.0f, 15.4f), V(1.2f, 4.0f, 0.8f), M("ClockWood", C("7A4A2A")), true);
                Cyl("Face", root, V(22.4f, 3.3f, 14.98f), V(0.9f, 0.03f, 0.9f), M("ClockFace", C("FFFDF4")), false, V(90, 0, 0));
                var pend = G("Pendulum", root, V(22.4f, 2.6f, 14.97f));
                Cyl("Rod", pend, V(22.4f, 1.9f, 14.97f), V(0.04f, 0.7f, 0.04f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
                Cyl("Bob", pend, V(22.4f, 1.2f, 14.96f), V(0.36f, 0.04f, 0.36f), M("Brass", C("F2C14E"), 0.7f, 0.6f), false, V(90, 0, 0));
                var g = Gz(root, "Grandfather clock", V(22.4f, 0, 13.8f), "Reset the clock before it chimes 13", "ตั้งนาฬิกาก่อนตีครบ 13 ที",
                    "BONG... BONG... the grandfather clock is chiming way too many times! (hallway)", "เหง่ง... เหง่ง... นาฬิกาตีเกินจำนวน! (ทางเดิน)",
                    "BONG #13! Mom thinks she's LATE and is rushing home!", "เหง่งที่ 13! แม่นึกว่าสาย รีบกลับบ้าน!", "Tick tock, all good.", "ติ๊กต่อก เรียบร้อย",
                    "Why is the clock wrong?", "ทำไมนาฬิกาเดินผิด?", 300f, 90f, 1);
                g.puzzle = MiniGame.Kind.Timing; g.puzzleEn = "STOP THE HAND AT 5"; g.puzzleTh = "หยุดเข็มที่เลข 5";
                g.wobbler = pend; g.wobble = 18f; g.onFail = GadgetHazard.Consequence.CutTime; g.cutSeconds = 45f;
            }
            // 9. LAUNDRY: someone put a box of crayons in the dryer
            {
                var root = G("Crayon dryer", gGadgets, V(29.2f, 0f, 0.9f));
                var drum = G("Drum", root, V(29.2f, 0.9f, 1.66f));
                for (int i = 0; i < 5; i++) L(PrimitiveType.Cube, "Crayon", drum, V(Mathf.Sin(i * 1.3f) * 0.25f, Mathf.Cos(i * 1.3f) * 0.25f, 0f), V(0.06f, 0.24f, 0.06f), M("Crayon" + i, new[] { C("FF6F61"), C("43CFC4"), C("FFD447"), C("7A5CD6"), C("6CC63A") }[i]), false, V(0, 0, i * 40));
                var fx = Burst("Rainbow", root, V(29.2f, 1.9f, 1.4f), new Color(1f, 0.6f, 1f, 1f), 0.1f, 1.2f, 0.4f, 2.4f);
                var g = Gz(root, "Dryer", V(29.2f, 0, 2.6f), "Stop the dryer melting crayons", "หยุดเครื่องอบผ้าที่หลอมสีเทียน",
                    "The dryer is melting CRAYONS - rainbow smoke! (laundry)", "เครื่องอบผ้าหลอมสีเทียน ควันสีรุ้ง! (ห้องซักผ้า)",
                    "Rainbow wax all over the laundry room!", "ขี้ผึ้งสีรุ้งเลอะทั้งห้อง!", "Dryer off. The towels are... colourful.", "ปิดแล้ว ผ้าขนหนูกลายเป็นหลากสี",
                    "Why are all my towels RAINBOW?!", "ทำไมผ้าขนหนูแม่กลายเป็นสีรุ้ง?!", 180f, 90f, 2);
                g.puzzle = MiniGame.Kind.Simon; g.puzzleEn = "CANCEL THE PROGRAM"; g.puzzleTh = "ยกเลิกโปรแกรม"; g.needsPower = true;
                g.spinner = drum; g.spinAxis = Vector3.forward; g.spinSpeed = 300f; g.fx = fx; g.fxRate = 12f; g.smokePerSec = 0.25f;
                g.splatKind = SplatKind.Confetti; g.splatMat = M("RainbowWax", C("C77DFF"), 0.5f); g.splatEvery = 6f; g.splatRadius = 1.8f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "whir"; g.loopVolume = 0.2f;
            }
            // 10. DINING: Mango the parrot learned a new word: "PARTY!"
            {
                var root = G("Parrot cage", gGadgets, V(42.2f, 0f, 10.6f));
                Cyl("Stand", root, V(42.2f, 1.1f, 10.6f), V(0.1f, 1.1f, 0.1f), M("Brass", C("F2C14E"), 0.7f, 0.6f), true);
                Cyl("CageBase", root, V(42.2f, 2.25f, 10.6f), V(1.1f, 0.06f, 1.1f), M("Brass", C("F2C14E"), 0.7f, 0.6f));
                for (int i = 0; i < 10; i++) { float a = i * 36f * Mathf.Deg2Rad; Cyl("Bar", root, V(42.2f + Mathf.Sin(a) * 0.5f, 2.85f, 10.6f + Mathf.Cos(a) * 0.5f), V(0.03f, 0.6f, 0.03f), M("Brass", C("F2C14E"), 0.7f, 0.6f)); }
                Sph("Dome", root, V(42.2f, 3.45f, 10.6f), V(1.05f, 0.4f, 1.05f), MT("CageDome", new Color(1f, 0.9f, 0.5f, 0.2f), 0.6f));
                var bird = G("Mango", root, V(42.2f, 2.6f, 10.6f));
                Sph("Body", bird, V(42.2f, 2.65f, 10.6f), V(0.3f, 0.42f, 0.3f), M("ParrotGreen", C("3FBF4F")));
                Sph("Head", bird, V(42.2f, 2.98f, 10.55f), Vector3.one * 0.24f, M("ParrotYellow", C("FFD447")));
                Cube("Beak", bird, V(42.2f, 2.96f, 10.38f), V(0.08f, 0.1f, 0.1f), M("ParrotBeak", C("FF7A3A")));
                var cover = Cube("Cover", root, V(42.2f, 2.9f, 10.6f), V(1.3f, 1.4f, 1.3f), M("CageCover", C("7A5CD6")));
                cover.SetActive(false);
                var g = Gz(root, "Parrot", V(40.8f, 0, 10.6f), "Cover Mango's cage (towel!) before Mom hears", "เอาผ้าคลุมกรงนกแก้ว (ผ้าขนหนู!) ก่อนแม่ได้ยิน",
                    null, null, "Mango is screaming PARTY! PARTY! - Mom will hear it the second she walks in!", "นกแก้วตะโกน ปาร์ตี้! ปาร์ตี้! - แม่ได้ยินแน่!",
                    "Shhh... night night, Mango.", "ชู่ว... ฝันดีนะ", "\"PAAARTY! SQUAWK! PARTY PARTY!\"... Mango, WHO TAUGHT YOU THAT?!", "\"ปาร์ตี้! แคว้ก! ปาร์ตี้!\"... ใครสอนนกพูดแบบนี้?!", 0f, 200f, 3);
                g.startActive = true; g.need = GadgetHazard.Need.Towel; g.needEn = "Need a TOWEL to cover the cage"; g.needTh = "ต้องใช้ผ้าขนหนูคลุมกรง";
                g.holdSolo = 1.0f; g.wobbler = bird; g.wobble = 14f; g.showWhenFixed = new[] { cover }; g.loopClip = "squeakhi"; g.loopVolume = 0.12f;
            }
            // 11. GAME ROOM: the popcorn machine won't stop popping
            {
                var root = G("Popcorn machine", gGadgets, V(52f, 0f, 26.6f));
                Cube("Cart", root, V(52f, 0.8f, 26.6f), V(1.6f, 1.6f, 1.0f), M("PopcornRed", C("D62828"), 0.4f), true);
                Cube("Kettle", root, V(52f, 2.3f, 26.6f), V(1.3f, 1.4f, 0.9f), MT("PopcornGlass", new Color(1f, 0.97f, 0.85f, 0.35f), 0.9f));
                Cube("Roof", root, V(52f, 3.1f, 26.6f), V(1.5f, 0.2f, 1.1f), M("PopcornRed", C("D62828"), 0.4f));
                var pile = G("Pile", root, V(52f, 1.8f, 26.6f));
                for (int i = 0; i < 9; i++) Sph("Kernel", pile, V(51.6f + (i % 3) * 0.4f, 1.75f + (i / 3) * 0.2f, 26.4f + (i % 2) * 0.3f), Vector3.one * 0.2f, M("Popcorn", C("FFF4C2")));
                var fx = Burst("Popping", root, V(52f, 2.6f, 26.2f), new Color(1f, 0.97f, 0.8f, 1f), 0.12f, 3f, 1.5f, 1.2f);
                var fire = MakeFire("Popcorn machine", V(52f, 2.4f, 26.6f), FireType.Grease, 0.35f, 0.9f, root, 1f);
                var g = Gz(root, "Popcorn machine", V(52f, 0, 25.2f), "Shut the popcorn machine before it burns", "ปิดเครื่องทำป๊อปคอร์นก่อนไหม้",
                    "POP POP POP - the popcorn machine is going crazy! (game room)", "ป๊อก ป๊อก ป๊อก - เครื่องป๊อปคอร์นคลั่ง! (ห้องเล่นเกม)",
                    "The popcorn is BURNING!", "ป๊อปคอร์นไหม้!", "Popcorn secured. Snack time later.", "เรียบร้อย ไว้กินทีหลัง",
                    "Why are there popcorn kernels in my carpet?", "ทำไมมีป๊อปคอร์นในพรม?", 160f, 80f, 2);
                g.puzzle = MiniGame.Kind.Mash; g.puzzleEn = "SLAM THE LID DOWN!"; g.puzzleTh = "กดฝาลงแรง ๆ!"; g.needsPower = true;
                g.fx = fx; g.fxRate = 30f; g.wobbler = pile; g.wobble = 6f; g.smokePerSec = 0.1f;
                g.splatKind = SplatKind.Confetti; g.splatMat = M("PopcornSplat", C("FFF4C2"), 0.3f); g.splatEvery = 4f; g.splatRadius = 2.5f;
                g.onFail = GadgetHazard.Consequence.Fire; g.failFire = fire; g.loopClip = "sizzle"; g.loopVolume = 0.18f;
            }

            // ================================================================= UPPER FLOOR
            // 12. BEDROOM: the glow-in-the-dark slime escaped its jar and is crawling around
            {
                var root = G("Slime jar", gGadgets, V(4f, U, 7.6f));
                Cyl("Jar", root, V(4f, U + 0.35f, 7.6f), V(0.5f, 0.35f, 0.5f), MT("JarGlass", new Color(0.8f, 1f, 0.8f, 0.3f), 0.9f));
                Cube("JarLid", root, V(4.5f, U + 0.05f, 8.1f), V(0.5f, 0.06f, 0.5f), M("JarLid", C("FF6F61")), false, V(0, 30, 0));
                var blob = G("Gloop", root, V(4f, U, 7.6f));
                Sph("Body", blob, V(4f, U + 0.22f, 7.6f), V(0.7f, 0.45f, 0.7f), slime);
                Sph("EyeL", blob, V(3.9f, U + 0.4f, 7.88f), Vector3.one * 0.1f, M("Eye", C("1A1416"), 0.8f));
                Sph("EyeR", blob, V(4.1f, U + 0.4f, 7.88f), Vector3.one * 0.1f, M("Eye", C("1A1416"), 0.8f));
                var gl = new GameObject("Glow").AddComponent<Light>(); gl.transform.SetParent(blob, false); gl.transform.position = V(4f, U + 0.8f, 7.6f);
                gl.type = LightType.Point; gl.color = C("7BE04A"); gl.range = 3f; gl.intensity = 1.6f;
                var g = Gz(root, "Escaped slime", V(4f, U, 7.6f), "Scoop up Gloop the slime (bucket!)", "ตักสไลม์กลับ (ใช้ถัง!)",
                    "Gloop the glow slime ESCAPED its jar! (bedroom)", "สไลม์เรืองแสงหนีออกจากขวด! (ห้องนอน)",
                    "Gloop slimed the whole upstairs!", "สไลม์เลอะทั้งชั้นบน!", "Gloop is home. Good slime.", "สไลม์กลับบ้านแล้ว",
                    "Why is there GREEN GOO on the stairs?!", "ทำไมมีเมือกเขียวบนบันได?!", 230f, 120f, 2);
                g.need = GadgetHazard.Need.Bucket; g.needEn = "Need a BUCKET to scoop it (laundry room)"; g.needTh = "ต้องใช้ถังตัก (ห้องซักผ้า)";
                g.roamer = blob; g.roamSpeed = 0.9f; g.fleeSpeed = 1.9f; g.fleeRadius = 2.4f;
                g.roamPath = new[] { V(4f, U, 7.6f), V(10.4f, U, 10.4f), V(10.8f, U, 14f), V(4f, U, 14f), V(10.8f, U, 14f), V(13f, U, 14f), V(10.4f, U, 10.4f), V(9.5f, U, 6.5f) };
                g.splatKind = SplatKind.Soda; g.splatMat = M("SlimeSplat", C("7BE04A"), 0.85f); g.splatEvery = 5f; g.splatRadius = 0.3f; g.onFail = GadgetHazard.Consequence.Splats;
                g.wobbler = blob; g.wobble = 5f; g.interactRadius = 1.7f; g.loopClip = "squeakhi"; g.loopVolume = 0.05f;
            }
            // 13. BEDROOM: the bubble machine is filling the room
            {
                var root = G("Bubble machine", gGadgets, V(10.4f, U, 8.4f));
                Cube("Body", root, V(10.4f, U + 0.35f, 8.4f), V(0.9f, 0.7f, 0.7f), M("BubblePurple", C("A777E3"), 0.5f), true);
                var wheel = G("Wand wheel", root, V(10.4f, U + 0.75f, 8.05f));
                for (int i = 0; i < 6; i++) L(PrimitiveType.Cylinder, "Wand", wheel, Quaternion.Euler(0, 0, i * 60) * V(0, 0.3f, 0), V(0.16f, 0.01f, 0.16f), M("Wand", C("FF8FB1")), false, V(90, 0, 0));
                var fx = Burst("Bubbles", root, V(10.4f, U + 0.8f, 7.9f), new Color(0.8f, 0.95f, 1f, 0.5f), 0.3f, 1.2f, -0.05f, 4f);
                var g = Gz(root, "Bubble machine", V(10.4f, U, 6.9f), "Unplug the bubble machine", "ถอดปลั๊กเครื่องเป่าฟอง",
                    "The bubble machine is filling the bedroom with BUBBLES! (bedroom)", "เครื่องเป่าฟองทำห้องนอนเต็มไปด้วยฟอง! (ห้องนอน)",
                    "Soapy bubbles popped all over the floor!", "ฟองแตกเลอะพื้นเต็มเลย!", "Unplugged. Pop.", "ถอดปลั๊กแล้ว ป๊อป",
                    "Why is the floor all soapy up here?", "ทำไมพื้นข้างบนลื่นเป็นฟองสบู่?", 100f, 90f, 1);
                g.puzzle = MiniGame.Kind.Tug; g.puzzleEn = "PULL THE PLUG!"; g.puzzleTh = "ดึงปลั๊ก!"; g.needsPower = true;
                g.spinner = wheel; g.spinAxis = Vector3.forward; g.spinSpeed = 200f; g.fx = fx; g.fxRate = 14f;
                g.splatKind = SplatKind.Foam; g.splatMat = foamMat; g.splatEvery = 7f; g.splatRadius = 2.5f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "whir"; g.loopVolume = 0.1f;
            }
            // 14. BEDROOM: the pillow fort is collapsing - hold it up
            {
                var root = G("Pillow fort", gGadgets, V(9.6f, U, 2.6f));
                var fort = G("Fort", root, V(9.6f, U, 2.6f));
                var pc = new[] { C("FFB3C9"), C("BFE8F0"), C("FFE07A"), C("B39DDB") };
                for (int i = 0; i < 6; i++) Cube("Pillow", fort, V(8.8f + (i % 3) * 0.8f, U + 0.3f + (i / 3) * 0.55f, 2.6f), V(0.8f, 0.5f, 0.9f), M("FortPillow" + (i % 4), pc[i % 4]), i < 3, V(0, 0, (i - 2) * 6));
                Cube("Blanket", fort, V(9.6f, U + 1.35f, 2.6f), V(2.8f, 0.06f, 1.4f), M("Blanket", C("7A5CD6")), false, V(4, 0, 0));
                var g = Gz(root, "Pillow fort", V(9.6f, U, 4.0f), "Hold up the collapsing pillow fort (hold E)", "ประคองป้อมหมอนที่กำลังถล่ม (กด E ค้าง)",
                    "The pillow fort is WOBBLING... (bedroom)", "ป้อมหมอนโยกเยก... (ห้องนอน)",
                    "The pillow fort COLLAPSED - Mom will see the mess!", "ป้อมหมอนพัง - แม่เห็นแน่!", "Fort secured. Very cosy.", "ป้อมมั่นคงแล้ว อุ่นดี",
                    "Why are all my pillows on the floor?!", "ทำไมหมอนแม่อยู่บนพื้นหมด?!", 270f, 80f, 1);
                g.holdSolo = 3.5f; g.wobbler = fort; g.wobble = 5f; g.onFail = GadgetHazard.Consequence.None;
            }
            // 15. UPSTAIRS HALL: the smart speaker recorded the whole party and wants to send it to Mom
            {
                var root = G("Smart speaker", gGadgets, V(10f, U, 12.4f));
                Cube("Shelf", root, V(10f, U + 1.2f, 12.3f), V(1.4f, 0.08f, 0.5f), M("LightWood", C("E8B27A")), true);
                Cyl("Speaker", root, V(10f, U + 1.55f, 12.3f), V(0.4f, 0.3f, 0.4f), M("SpeakerFabric", C("3A3A44")));
                var ring = G("Ring", root, V(10f, U + 1.86f, 12.3f));
                L(PrimitiveType.Cylinder, "LedRing", ring, Vector3.zero, V(0.42f, 0.02f, 0.42f), ME("AssistantBlue", C("4FD8FF"), new Color(0.3f, 1.6f, 2.4f)));
                var bl = new GameObject("Led").AddComponent<Light>(); bl.transform.SetParent(root, false); bl.transform.position = V(10f, U + 2.1f, 12.6f);
                bl.type = LightType.Point; bl.color = C("4FD8FF"); bl.range = 3f; bl.intensity = 2f;
                var g = Gz(root, "Smart speaker", V(10f, U, 13.6f), "Delete the party recording on the smart speaker", "ลบเสียงปาร์ตี้ในลำโพงอัจฉริยะ",
                    "\"Recording... Sending 'PARTY.mp3' to MOM in 3 minutes!\" (upstairs hall)", "\"กำลังอัด... จะส่ง 'PARTY.mp3' ให้แม่ใน 3 นาที!\" (ทางเดินชั้นบน)",
                    "\"Message sent to Mom.\" She's coming home EARLY!", "\"ส่งข้อความหาแม่แล้ว\" แม่กลับเร็วขึ้น!", "Recording deleted. No witnesses.", "ลบแล้ว ไม่มีพยาน",
                    "Hey assistant, play the last recording... hmm, it's empty?", "ผู้ช่วย เปิดเสียงล่าสุด... อ้าว ว่าง?", 260f, 180f, 2);
                g.puzzle = MiniGame.Kind.Wires; g.puzzleEn = "REWIRE THE DELETE BUTTON"; g.puzzleTh = "ต่อสายปุ่มลบ"; g.needsPower = true;
                g.spinner = ring; g.spinSpeed = 120f; g.blink = bl; g.onFail = GadgetHazard.Consequence.CutTime; g.cutSeconds = 40f;
            }
            // 16. MOM'S ROOM: a glitter bomb went off on Mom's vanity
            {
                var root = G("Glitter bomb", gGadgets, V(26.6f, U, 9.6f));
                Cyl("Tube", root, V(26.6f, U + 0.15f, 9.6f), V(0.3f, 0.15f, 0.3f), M("GlitterTube", C("FFD447"), 0.8f, 0.6f), false, V(0, 0, 80));
                var fx = Burst("Glitter", root, V(26.6f, U + 0.4f, 9.6f), new Color(1f, 0.85f, 1f, 1f), 0.05f, 1.6f, 0.6f, 1.6f);
                var g = Gz(root, "Glitter bomb", V(25.4f, U, 9.6f), "Mop up the glitter on Mom's floor (mop!)", "ถูกากเพชรบนพื้นห้องแม่ (ใช้ไม้ถู!)",
                    "A GLITTER BOMB went off in Mom's room! (Mom's room)", "ระเบิดกากเพชรในห้องแม่! (ห้องนอนแม่)",
                    "Glitter tracked all through the upstairs!", "กากเพชรติดเท้าไปทั่วชั้นบน!", "Glitter... mostly gone. It's forever, really.", "กากเพชร... หายเกือบหมด (จริง ๆ อยู่ตลอดไป)",
                    "Why does my room SPARKLE?!", "ทำไมห้องแม่วิบวับ?!", 170f, 100f, 2);
                g.need = GadgetHazard.Need.Mop; g.needEn = "Need the MOP (laundry room)"; g.needTh = "ต้องใช้ไม้ถูพื้น (ห้องซักผ้า)";
                g.holdSolo = 2f; g.fx = fx; g.fxRate = 20f;
                g.splatKind = SplatKind.Confetti; g.splatMat = M("Glitter", C("FFC4F0"), 0.95f, 0.6f); g.splatEvery = 6f; g.splatRadius = 2.2f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "sparkle"; g.loopVolume = 0.1f;
            }
            // 17. MOM'S ROOM: the treadmill is running at full speed with the cat toy on it - two people to stop it
            {
                var root = G("Treadmill", gGadgets, V(18.4f, U, 9.4f));
                Cube("Deck", root, V(18.4f, U + 0.25f, 9.4f), V(1.6f, 0.4f, 3.6f), M("TreadGrey", C("5A5F6A"), 0.4f), true);
                var belt = G("Belt", root, V(18.4f, U + 0.47f, 9.4f));
                for (int i = 0; i < 6; i++) L(PrimitiveType.Cube, "Stripe", belt, V(0, 0, -1.5f + i * 0.6f), V(1.3f, 0.02f, 0.12f), M("BeltStripe", C("FFD447")));
                Cube("Console", root, V(18.4f, U + 1.6f, 11.1f), V(1.4f, 0.5f, 0.3f), M("TreadConsole", C("232330")));
                Cyl("RailL", root, V(17.6f, U + 0.95f, 10.5f), V(0.08f, 0.7f, 0.08f), chrome);
                Cyl("RailR", root, V(19.2f, U + 0.95f, 10.5f), V(0.08f, 0.7f, 0.08f), chrome);
                Sph("CatToy", root, V(18.4f, U + 0.65f, 9f), Vector3.one * 0.3f, M("CatToy", C("FF6FB5")));
                var g = Gz(root, "Treadmill", V(18.4f, U, 7f), "Stop the turbo treadmill (one holds the belt, one hits STOP)", "หยุดลู่วิ่งเทอร์โบ (คนหนึ่งจับสายพาน อีกคนกดหยุด)",
                    "The treadmill is on TURBO and launching the cat toy around! (Mom's room)", "ลู่วิ่งเปิดโหมดเทอร์โบ ดีดของเล่นแมวไปทั่ว! (ห้องนอนแม่)",
                    "The treadmill motor fried the circuit!", "มอเตอร์ลู่วิ่งทำไฟตก!", "Treadmill stopped. Workout over.", "หยุดแล้ว เลิกออกกำลัง",
                    "Who used my treadmill?!", "ใครแอบใช้ลู่วิ่งแม่?!", 140f, 110f, 2);
                g.coop = true; g.needsPower = true; g.wobbler = belt; g.wobble = 3f;
                g.partners = new[] { Switch(g, root, V(18.4f, U + 1.9f, 11.1f), V(18.4f, U, 12.0f), false), Switch(g, root, V(16.8f, U + 0.6f, 8.2f), V(16.2f, U, 8.2f), false) };
                g.partnerWindow = 3f; g.onFail = GadgetHazard.Consequence.TripPower; g.loopClip = "whir"; g.loopVolume = 0.25f;
            }
            // 18. MOM'S ROOM: party streamers tangled round the chandelier - drag a CHAIR up to reach them
            {
                var root = G("Streamers", gGadgets, V(22f, U, 7.6f));
                Sph("Chandelier", root, V(22f, U + 4.6f, 7.6f), V(1.2f, 0.5f, 1.2f), ME("Chandelier", C("FFF1C9"), new Color(1.4f, 1.1f, 0.7f)));
                var sway = G("Streamers", root, V(22f, U + 4.4f, 7.6f));
                var sc = new[] { C("FF6FB5"), C("43CFC4"), C("FFD447"), C("7A5CD6") };
                for (int i = 0; i < 8; i++) L(PrimitiveType.Cube, "Streamer", sway, Quaternion.Euler(0, i * 45, 0) * V(0.5f, -1.0f, 0f), V(0.08f, 2.0f, 0.02f), M("Streamer" + (i % 4), sc[i % 4]), false, V(0, i * 45, 10));
                var g = Gz(root, "Streamers", V(22f, U, 7.6f), "Pull down the streamers (stand on a CHAIR)", "ดึงสายรุ้งลง (ต้องยืนบนเก้าอี้)",
                    null, null, null, null, "Streamers down. Nice reach!", "ดึงลงหมดแล้ว เอื้อมเก่ง!",
                    "Why is my chandelier dressed for a PARTY?!", "ทำไมโคมไฟแม่แต่งตัวไปปาร์ตี้?!", 0f, 9999f, 2);
                g.startActive = true; g.need = GadgetHazard.Need.Chair; g.needEn = "Too high! Bring a CHAIR (dining room / kitchen)"; g.needTh = "สูงไป! เอาเก้าอี้มา (ห้องกินข้าว / ครัว)";
                g.holdSolo = 1.6f; g.wobbler = sway; g.wobble = 6f; g.showWhenActive = new[] { sway.gameObject }; g.interactRadius = 2.2f;
            }
            // 19. PARTY LOFT: someone aimed the telescope straight into the neighbours' window
            {
                var root = G("Telescope", gGadgets, V(27.6f, U, 18.4f));
                for (int i = 0; i < 3; i++) Cyl("Leg", root, V(27.6f + Mathf.Sin(i * 2.09f) * 0.35f, U + 0.6f, 18.4f + Mathf.Cos(i * 2.09f) * 0.35f), V(0.06f, 0.65f, 0.06f), M("WoodDark", C("8A4B2A")), false, V(Mathf.Cos(i * 2.09f) * 15f, 0, -Mathf.Sin(i * 2.09f) * 15f));
                var tube = G("Tube", root, V(27.6f, U + 1.3f, 18.4f));
                Cyl("Barrel", tube, V(27.8f, U + 1.5f, 18.4f), V(0.3f, 0.8f, 0.3f), M("TelescopeBrass", C("D4A23A"), 0.8f, 0.7f), false, V(0, 0, -70));
                var g = Gz(root, "Telescope", V(26.4f, U, 18.4f), "Point the telescope back at the stars", "หันกล้องดูดาวกลับไปที่ดาว",
                    "The telescope is pointed right into the NEIGHBOURS' window! (party loft)", "กล้องดูดาวส่องเข้าบ้านข้าง ๆ! (ลอฟต์ปาร์ตี้)",
                    "The neighbours SAW the telescope... and they're calling Mom!", "ข้างบ้านเห็นกล้อง... กำลังโทรหาแม่!", "Stars only. Very scientific.", "ส่องดาวอย่างเดียว วิทยาศาสตร์มาก",
                    "Why is my telescope in the loft?", "ทำไมกล้องดูดาวมาอยู่ที่ลอฟต์?", 110f, 120f, 1);
                g.puzzle = MiniGame.Kind.Timing; g.puzzleEn = "AIM AT THE STAR"; g.puzzleTh = "เล็งไปที่ดาว";
                g.wobbler = tube; g.wobble = 2f; g.neighbourPerSec = 0.3f; g.onFail = GadgetHazard.Consequence.Neighbour;
            }
            // 20. PARTY LOFT: the disco foam cannon - only safe to touch with the POWER OFF (someone runs to the breaker)
            {
                var root = G("Foam cannon", gGadgets, V(27.6f, U, 23f));
                Cube("Base", root, V(27.6f, U + 0.4f, 23f), V(1.2f, 0.8f, 1.2f), M("CannonBlack", C("232330"), 0.5f), true);
                var barrel = G("Barrel", root, V(27.6f, U + 1.1f, 23f));
                Cyl("Barrel", barrel, V(27f, U + 1.3f, 23f), V(0.5f, 0.7f, 0.5f), M("CannonPink", C("FF4FD8"), 0.6f), false, V(0, 0, 60));
                var fx = Burst("Foam", root, V(26.5f, U + 1.7f, 23f), new Color(0.97f, 0.99f, 1f, 0.9f), 0.5f, 2.5f, 0.4f, 2f);
                var bl = new GameObject("Danger").AddComponent<Light>(); bl.transform.SetParent(root, false); bl.transform.position = V(27.6f, U + 2.2f, 23f);
                bl.type = LightType.Point; bl.color = C("FF2222"); bl.range = 4f; bl.intensity = 2.5f;
                Label3D(root, "DANGER: 10,000 VOLTS OF FUN", V(27.6f, U + 0.55f, 22.38f), Quaternion.Euler(0, 180, 0), 0.06f, C("FFD447"));
                var g = Gz(root, "Foam cannon", V(26f, U, 23f), "Switch off the foam cannon (cut the power FIRST)", "ปิดปืนโฟม (ต้องตัดไฟก่อน!)",
                    "The disco FOAM CANNON turned on by itself! (party loft)", "ปืนโฟมดิสโก้เปิดเอง! (ลอฟต์ปาร์ตี้)",
                    "The loft is a FOAM PARTY now!", "ลอฟต์กลายเป็นปาร์ตี้โฟมแล้ว!", "Cannon off. Turn the power back on!", "ปิดแล้ว อย่าลืมเปิดไฟคืน!",
                    "Why is there FOAM in the loft?!", "ทำไมมีโฟมเต็มลอฟต์?!", 60f, 120f, 2);
                g.coop = true; g.needsPower = true; g.fixOnlyWithoutPower = true; g.holdSolo = 1.5f;
                g.wobbler = barrel; g.wobble = 4f; g.fx = fx; g.fxRate = 20f; g.blink = bl;
                g.splatKind = SplatKind.Foam; g.splatMat = foamMat; g.splatEvery = 2.5f; g.splatRadius = 3f; g.onFail = GadgetHazard.Consequence.Splats;
                g.loopClip = "whir"; g.loopVolume = 0.25f;
            }
            // 21. LANDING: the hoverboard has been charging all night and is smoking
            {
                var root = G("Hoverboard", gGadgets, V(3.2f, U, 18.6f));
                var board = G("Board", root, V(3.2f, U, 18.6f));
                Cube("Deck", board, V(3.2f, U + 0.2f, 18.6f), V(1.4f, 0.16f, 0.45f), M("HoverTeal", C("43CFC4"), 0.6f));
                Cyl("WheelL", board, V(2.45f, U + 0.2f, 18.6f), V(0.42f, 0.1f, 0.42f), dark, false, V(0, 0, 90));
                Cyl("WheelR", board, V(3.95f, U + 0.2f, 18.6f), V(0.42f, 0.1f, 0.42f), dark, false, V(0, 0, 90));
                Cube("Charger", root, V(3.2f, U + 0.08f, 17.6f), V(0.3f, 0.16f, 0.2f), dark);
                var smoke = MakeSmokeColumn(root, V(3.2f, U + 0.4f, 18.6f));
                var fire = MakeFire("Hoverboard", V(3.2f, U + 0.35f, 18.6f), FireType.Ordinary, 0.3f, 0.8f, root, 0.9f);
                var g = Gz(root, "Hoverboard", V(3.2f, U, 19.9f), "Unplug the smoking hoverboard", "ถอดปลั๊กโฮเวอร์บอร์ดที่มีควัน",
                    "The hoverboard charger is SMOKING! (landing)", "ที่ชาร์จโฮเวอร์บอร์ดมีควัน! (ชานพักบันได)",
                    "The hoverboard CAUGHT FIRE!", "โฮเวอร์บอร์ดไฟลุก!", "Unplugged. Hoverboard lives.", "ถอดแล้ว รอดตาย",
                    "Why does the landing smell like burnt plastic?", "ทำไมชานพักบันไดมีกลิ่นพลาสติกไหม้?", 190f, 80f, 2);
                g.puzzle = MiniGame.Kind.Tug; g.puzzleEn = "YANK THE CHARGER!"; g.puzzleTh = "กระชากที่ชาร์จ!"; g.needsPower = true;
                g.fx = smoke; g.fxRate = 12f; g.wobbler = board; g.wobble = 2f; g.onFail = GadgetHazard.Consequence.Fire; g.failFire = fire; g.loopClip = "sizzle"; g.loopVolume = 0.1f;
            }
            // 22. UPSTAIRS HALL: a beanbag is jammed in the laundry-chute shortcut
            {
                var root = G("Chute jam", gGadgets, V(ChuteHole.center.x, U, ChuteHole.center.y));
                var bag = G("Beanbag", root, V(ChuteHole.center.x, U, ChuteHole.center.y));
                Sph("Bag", bag, V(ChuteHole.center.x, U + 0.2f, ChuteHole.center.y), V(3.0f, 0.9f, 3.6f), M("BeanYellow", C("FFD447")), true);
                var g = Gz(root, "Chute jam", V(ChuteHole.xMin - 1.2f, U, ChuteHole.center.y), "Unjam the laundry-chute shortcut (plunger!)", "ทะลวงช่องลัดที่ตัน (ใช้ที่ปั๊ม!)",
                    "Someone stuffed a BEANBAG down the laundry chute - the shortcut is blocked! (upstairs hall)", "ใครยัดบีนแบ็กลงช่องลัด - ทางลัดตัน! (ทางเดินชั้นบน)",
                    null, null, "*SHLOOP* Shortcut open!", "*ปุ๊ก* ทางลัดเปิดแล้ว!",
                    "Why is my beanbag in the laundry chute?", "ทำไมบีนแบ็กอยู่ในช่องผ้า?", 210f, 9999f, 1);
                g.need = GadgetHazard.Need.Plunger; g.needEn = "Need the PLUNGER (garage)"; g.needTh = "ต้องใช้ที่ปั๊ม (โรงรถ)";
                g.puzzle = MiniGame.Kind.Plunger; g.puzzleEn = "PLUNGE IT!"; g.puzzleTh = "ปั๊ม!";
                g.showWhenActive = new[] { bag.gameObject }; g.interactRadius = 1.8f;
            }
            raw = false;
        }

        // ---------------------------------------------------------------- gadget helpers (raw world units)
        static GadgetHazard Gz(Transform root, string name, Vector3 anchor, string qEn, string qTh, string sEn, string sTh, string fEn, string fTh,
            string dEn, string dTh, string mEn, string mTh, float startAt, float escalate, int severity)
        {
            var g = root.gameObject.AddComponent<GadgetHazard>();
            g.displayName = name; g.interactAnchor = G("Anchor", root, anchor); g.interactRadius = 1.5f;
            g.questEn = qEn; g.questTh = qTh; g.startEn = sEn; g.startTh = sTh; g.failEn = fEn; g.failTh = fTh;
            g.doneEn = dEn; g.doneTh = dTh; g.momEn = mEn; g.momTh = mTh;
            g.startAt = startAt > 0f ? startAt : -1f; g.escalateTime = escalate; g.severity = severity;
            return g;
        }

        static GadgetSwitch Switch(GadgetHazard owner, Transform root, Vector3 pos, Vector3 anchor, bool wall)
        {
            var s = G("Switch", root, pos);
            Cube("Plate", s, pos, wall ? V(0.12f, 0.5f, 0.4f) : V(0.4f, 0.12f, 0.4f), M("SwitchPlate", C("FFD447")));
            var knob = G("Button", s, pos);
            L(PrimitiveType.Cylinder, "Cap", knob, Vector3.zero, V(0.24f, 0.06f, 0.24f), ME("SwitchRed", C("FF3B3B"), new Color(1.6f, 0.1f, 0.1f)), false, wall ? V(0, 0, 90) : Vector3.zero);
            if (anchor.y < 1f) Floor2D(root, new Vector2(anchor.x, anchor.z), V2(1.0f, 1.0f), "HazardStripe", C("FFD447"));
            else Cube("Mark", root, anchor + V(0, 0.01f, 0), V(1.0f, 0.016f, 1.0f), M("HazardStripe", C("FFD447"), 0.2f));
            var sw = s.gameObject.AddComponent<GadgetSwitch>();
            sw.owner = owner; sw.knob = knob; sw.displayName = owner.displayName + " button"; sw.interactAnchor = G("Anchor", s, anchor); sw.interactRadius = 1.4f;
            return sw;
        }

        /// A simple world-space particle burst (bubbles, ice, popcorn, foam...), emission driven by its gadget.
        static ParticleSystem Burst(string name, Transform parent, Vector3 pos, Color col, float size, float speed, float gravity, float life)
        {
            var ps = NewPS(name, parent, pos, ParticleMat("FX_Smoke", false), 300);
            var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life); main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size); main.startColor = col; main.gravityModifier = gravity;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30f; sh.radius = 0.1f; sh.rotation = V(-90, 0, 0);
            ps.Play();
            return ps;
        }
    }
}
