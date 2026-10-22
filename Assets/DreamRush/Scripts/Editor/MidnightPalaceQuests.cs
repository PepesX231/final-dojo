using System.Collections.Generic;
using DreamRush.Palace;
using DreamRush.World1;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M17 — "level 2 has no content; if it's semi-open-world, pave the way well; add puzzles and traversal tricks".
    /// Design notes (research: Nintendo's "gravity" landmarks + breadcrumbs for open spaces, Mario Odyssey's dense
    /// vignettes on a linear-able spine): every district gets ONE signature verb, the main spine is breadcrumbed, and
    /// every beat is visible from the last one.
    ///  ① SOUTH  — BLACKOUT: a neon-circuit floor puzzle powers the giant gate sign → lift to the monorail station.
    ///  ② TRAIN  — ride the monorail roof (stops at two stations) for fast travel and a moving viewpoint.
    ///  ③ EAST   — rooftop run with clock-hand fans; ring the STOP-THE-CLOCK bells to freeze them.
    ///  ④ NORTH  — the gear tower: pull the REWIND lever on top and race up the stairs before time runs out.
    ///  ⑤ WEST   — the fountain is sealed: punch three gift crates onto the plates, then survive the ambush.
    ///  + optional east-avenue circuit (vault of minutes), light beacons over unsolved goals, arrows on the road.
    /// </summary>
    public static partial class MidnightPalaceBuilder
    {
        static Material _arrowMat, _beamMat;

        static Material ArrowMat()
        {
            if (_arrowMat != null) return _arrowMat;
            string tp = "Assets/DreamRush/Art/MidnightPalace/Textures/T_MP_Arrow.png";
            var imp = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp)) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport(); }
            var m = ArtKit.Mat("M_MP_Arrow", H(0x7FF6FF), null, null, 0f, alphaClip: true, doubleSided: true);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); m.SetFloat("_BaseMapStrength", 1f); m.SetColor("_Emission", H(0x30E0FF) * 1.6f);
            EditorUtility.SetDirty(m); return _arrowMat = m;
        }

        static Material BeamMat(Color c, string name)
        {
            string path = $"Assets/DreamRush/Art/MidnightPalace/M_MP_Beam_{name}.mat";
            var sh = Shader.Find("DreamRush/DreamParticle");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh; m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One); m.SetFloat("_Cull", 0f); m.SetFloat("_NearFade", 1f);
            m.SetColor("_BaseColor", c); m.renderQueue = 3000; EditorUtility.SetDirty(m); return m;
        }

        static GameObject Beacon(Transform parent, Vector3 at, Color c, string name, StolenHour hour = null, PuzzleReward puzzle = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Beacon_" + name; go.transform.SetParent(parent, false); go.transform.position = at + Vector3.up * 2f; go.transform.localScale = new Vector3(0.65f, 2f, 0.65f);
            var r = go.GetComponent<MeshRenderer>(); r.sharedMaterial = BeamMat(c, name); r.shadowCastingMode = ShadowCastingMode.Off;
            var b = go.AddComponent<HourBeacon>(); b.hour = hour; b.puzzle = puzzle;
            return go;
        }

        /// <summary>Glowing chevrons on the ground every ~9 m along a path (the breadcrumb spine).</summary>
        static void Arrows(Transform parent, params Vector3[] pts)
        {
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1]; Vector3 dir = (b - a); dir.y = 0; float len = dir.magnitude; dir /= len;
                for (float t = 4f; t < len - 2f; t += 9f)
                {
                    var q = Quad(parent, "Arrow", a + dir * t + Vector3.up * 0.06f, new Vector2(2.4f, 2.4f), Quaternion.LookRotation(Vector3.down, dir), ArrowMat());
                    q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off; _static.Add(q);
                }
            }
        }

        static PuzzleReward Reward(Transform parent, Vector3 at, string id, string toast, int minutes)
        {
            var go = new GameObject("Reward_" + id); go.transform.SetParent(parent, false); go.transform.position = at;
            var r = go.AddComponent<PuzzleReward>(); r.id = id; r.toast = toast; r.minutes = minutes; return r;
        }

        // ================================================================== NEON CIRCUIT
        /// <summary>
        /// Builds a cols×rows tile floor at <paramref name="c"/> (rows along +z, cols along +x). <paramref name="solution"/>
        /// gives each tile's base mask and its solved turn; tiles start scrambled (deterministic).
        /// </summary>
        static CircuitPuzzle Circuit(Transform parent, Vector3 c, int cols, int rows, int sourceRow, int sinkRow, (int mask, int turn)[] solution, PuzzleReward reward, Color col, string name)
        {
            var g = Group(parent, "Circuit_" + name);
            var pz = g.gameObject.AddComponent<CircuitPuzzle>(); pz.cols = cols; pz.rows = rows; pz.sourceRow = sourceRow; pz.sinkRow = sinkRow; pz.reward = reward; pz.color = col;
            var tubeMat = Mat("M_MP_CircuitTube_" + name, col, rim: 0f, emission: col * 0.2f);
            const float S = 3.2f;
            Vector3 origin = c - new Vector3((cols - 1) * S / 2f, 0, (rows - 1) * S / 2f);
            SBox(g, "CircuitBase", c + Vector3.up * 0.1f, new Vector3(cols * S + 1.2f, 0.2f, rows * S + 1.2f), M["M_Kit_MP_Dark"]);
            for (int rr = 0; rr < rows; rr++)
                for (int cc = 0; cc < cols; cc++)
                {
                    int i = rr * cols + cc; var sol = solution[i];
                    var tile = new GameObject($"Tile{i}"); tile.transform.SetParent(g, false); tile.transform.position = origin + new Vector3(cc * S, 0.25f, rr * S);
                    var plate = Box(tile.transform, "Plate", tile.transform.position + Vector3.up * 0.1f, new Vector3(S - 0.25f, 0.2f, S - 0.25f), M["Podium"]);
                    var vis = new GameObject("Visual").transform; vis.SetParent(tile.transform, false); vis.localPosition = Vector3.up * 0.25f;
                    var tubes = new List<Renderer>();
                    var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(hub.GetComponent<Collider>()); hub.transform.SetParent(vis, false);
                    hub.transform.localScale = new Vector3(0.7f, 0.08f, 0.7f); hub.GetComponent<MeshRenderer>().sharedMaterial = tubeMat; tubes.Add(hub.GetComponent<Renderer>());
                    for (int bit = 0; bit < 4; bit++)
                    {
                        if ((sol.mask & (1 << bit)) == 0) continue;
                        Vector3 dir = bit == 0 ? Vector3.forward : bit == 1 ? Vector3.right : bit == 2 ? Vector3.back : Vector3.left;
                        var t = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(t.GetComponent<Collider>()); t.transform.SetParent(vis, false);
                        t.transform.localPosition = dir * S * 0.25f; t.transform.localScale = bit % 2 == 0 ? new Vector3(0.36f, 0.16f, S * 0.5f) : new Vector3(S * 0.5f, 0.16f, 0.36f);
                        t.GetComponent<MeshRenderer>().sharedMaterial = tubeMat; tubes.Add(t.GetComponent<Renderer>());
                    }
                    var ct = tile.AddComponent<CircuitTile>(); ct.baseMask = sol.mask; ct.visual = vis; ct.tubes = tubes.ToArray(); ct.puzzle = pz;
                    int scramble = (sol.turn + 1 + ((i * 7 + name.Length) % 3)) % 4;    // never starts solved
                    ct.turns = scramble;
                    pz.tiles.Add(ct);
                }
            // generator (west) + sign post (east)
            Vector3 gen = origin + new Vector3(-S * 0.95f, 0, sourceRow * S);
            SBox(g, "Generator", gen + Vector3.up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), M["Industrial"]);
            SBox(g, "GeneratorGlow", gen + new Vector3(0.82f, 1.5f, 0), new Vector3(0.1f, 0.8f, 0.8f), M["M_Kit_MP_LightGreen"], false);
            Vector3 sink = origin + new Vector3((cols - 1) * S + S * 0.95f, 0, sinkRow * S);
            SBox(g, "SinkPost", sink + Vector3.up * 1.5f, new Vector3(1.2f, 3f, 1.2f), M["M_Kit_MP_Dark"]);
            var lamp = Box(g, "SinkLamp", sink + Vector3.up * 3.3f, Vector3.one * 0.9f, Mat("M_MP_SinkLamp_" + name, col, rim: 0f), false);
            pz.sinkLight = lamp.GetComponent<Renderer>();
            var hint = Quad(g, "Hint", c + new Vector3(0, 2.6f, -rows * S / 2f - 1.6f), new Vector2(4.6f, 1.7f), Quaternion.identity, M["Sign" + 7]); _static.Add(hint);
            return pz;
        }

        static readonly (int, int)[] Sol3x3 =
        {   // row 0 (source row): power enters west → corner up; rows wind to the sink on row 2 east (N=1 E=2 S=4 W=8)
            (9, 0), (5, 0), (3, 0),       // row 0: W→N corner (from the generator), decoys
            (6, 0), (9, 0), (10, 0),      // row 1: S→E corner, W→N corner, decoy
            (12, 0), (6, 0), (10, 0),     // row 2: decoy, S→E corner, W→E straight → the sign
        };

        // ================================================================== the quest content
        static void Quests(Transform root, PalaceDirector pd, List<StolenHour> hours)
        {
            var g = Group(root, "Quests");
            // ---------- ① SOUTH: blackout circuit → the gate sign + a lift to the south monorail station
            var blackout = Reward(g, new Vector3(0, 1, -98), "blackout", "ไฟกลับมาแล้ว! ลิฟต์ไปสถานีรถไฟลอยฟ้าเปิดแล้ว", 6);
            var lift = new GameObject("StationLift_South"); lift.transform.SetParent(g, false); lift.transform.position = new Vector3(9, 0.05f, -82);
            var liftVis = Box(lift.transform, "LiftPad", lift.transform.position + Vector3.up * 0.1f, new Vector3(3f, 0.2f, 3f), M["Neon1"], false).transform;
            var liftTgt = new GameObject("Target").transform; liftTgt.SetParent(lift.transform, false); liftTgt.position = new Vector3(9, 20.4f, -65.4f);
            var lp = lift.AddComponent<LaunchPad>(); lp.target = liftTgt; lp.apexHeight = 24f; lp.visual = liftVis; lp.radius = 1.5f;
            var stepsSouth = Group(g, "NeonSteps_South");
            for (int i = 0; i < 4; i++) Box(stepsSouth, "NeonBillboardStep", new Vector3(-14 + i * 3.5f, 4f + i * 3.6f, -80 + i * 3.4f), new Vector3(4f, 0.5f, 3f), M["AdH" + (i % 8)]);
            blackout.show = new[] { lift, stepsSouth.gameObject };
            var circuitS = Circuit(g, new Vector3(0, 0, -98), 3, 3, 0, 2, Sol3x3, blackout, H(0x30F0FF), "South");
            // ---------- ② MONORAIL stations (south: unlocked by ①; north: always open)
            StationPlatform(g, new Vector3(0, 0, -65.4f), Vector3.back, "South");
            StationPlatform(g, new Vector3(0, 0, 75.4f), Vector3.forward, "North");
            var liftN = new GameObject("StationLift_North"); liftN.transform.SetParent(g, false); liftN.transform.position = new Vector3(-9, 0.05f, 60);
            var liftNVis = Box(liftN.transform, "LiftPad", liftN.transform.position + Vector3.up * 0.1f, new Vector3(3f, 0.2f, 3f), M["Neon1"], false).transform;
            var liftNTgt = new GameObject("Target").transform; liftNTgt.SetParent(liftN.transform, false); liftNTgt.position = new Vector3(-9, 20.4f, 75.4f);
            var lpN = liftN.AddComponent<LaunchPad>(); lpN.target = liftNTgt; lpN.apexHeight = 24f; lpN.visual = liftNVis; lpN.radius = 1.5f;
            // ---------- ③ EAST rooftop run: clock-hand fans + stop-the-clock bells
            Hazard(g, new Vector3(112, 13.5f, -14), 5.4f, 75f);
            Hazard(g, new Vector3(148, 21.5f, 14), 5.4f, -90f);
            Hazard(g, new Vector3(130, 25.5f, 30), 5.2f, 100f);
            Bell(g, new Vector3(88, 9.5f, -30), 40f, 8f);
            Bell(g, new Vector3(128, 17.5f, -6), 40f, 8f);
            // ---------- ④ NORTH gear tower: rewind stairs from the pillar top up to the hour (moved to y 36)
            var rw = Group(g, "RewindStairs");
            var bridge = rw.gameObject.AddComponent<RewindBridge>(); bridge.hold = 12f;
            for (int i = 0; i < 6; i++)
            {
                float a = (i * 60f) * Mathf.Deg2Rad; Vector3 p = new Vector3(Mathf.Sin(a) * 4.2f, 27.6f + i * 1.6f, 120 + Mathf.Cos(a) * 4.2f);
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cylinder); slab.name = "ClockSlab"; slab.transform.SetParent(rw, false);
                slab.transform.position = p; slab.transform.localScale = new Vector3(2.6f, 0.2f, 2.6f); slab.GetComponent<MeshRenderer>().sharedMaterial = M["M_Kit_MP_Face"];
                bridge.pieces.Add(slab.transform);
            }
            SBox(g, "HourPerch", new Vector3(0, 36f, 120), new Vector3(4f, 0.5f, 4f), M["M_Kit_MP_Gold"]);
            var lever = new GameObject("RewindLever"); lever.transform.SetParent(g, false); lever.transform.position = new Vector3(1.2f, 26.2f, 118.6f);
            Box(lever.transform, "LeverBase", lever.transform.position + Vector3.up * 0.5f, new Vector3(0.8f, 1f, 0.8f), M["M_Kit_MP_Brass"]);
            var handle = new GameObject("Handle").transform; handle.SetParent(lever.transform, false); handle.position = lever.transform.position + Vector3.up * 1.4f;
            Box(handle, "Key", handle.position, new Vector3(1.6f, 0.25f, 0.25f), M["M_Kit_MP_Gold"], false); Box(handle, "Key2", handle.position, new Vector3(0.25f, 1.2f, 0.25f), M["M_Kit_MP_Gold"], false);
            var rl = lever.AddComponent<RewindLever>(); rl.bridge = bridge; rl.handle = handle;
            // ---------- ⑤ WEST fountain seal: three crates onto three plates
            Vector3 fc = new Vector3(-115, 0, 0);
            var seal = Reward(g, fc + Vector3.up, "fountain", "ผนึกน้ำพุคลายแล้ว… เงากำลังมา!", 4);
            var sealFx = Group(g, "FountainSeal");
            for (int i = 0; i < 16; i++) { float a = i / 16f * Mathf.PI * 2f; Box(sealFx, "SealBar", fc + new Vector3(Mathf.Sin(a) * 5f, 1.4f, Mathf.Cos(a) * 5f), new Vector3(0.18f, 2.8f, 0.18f), M["Neon0"], false); }
            seal.hide = new[] { sealFx.gameObject };
            var plates = new GameObject("PlateGroup").AddComponent<PlateGroup>(); plates.transform.SetParent(g, false); plates.reward = seal;
            Vector3[] plateAt = { fc + new Vector3(0, 0, 15), fc + new Vector3(13, 0, -7.5f), fc + new Vector3(-13, 0, -7.5f) };
            Vector3[] crateOff = { new Vector3(6, 0, 0), new Vector3(0, 0, 6), new Vector3(0, 0, -6) };
            for (int i = 0; i < 3; i++)
            {
                var pl = Box(g, "Plate", plateAt[i] + Vector3.up * 0.05f, new Vector3(2.8f, 0.1f, 2.8f), Mat("M_MP_Plate", H(0xFF5A66), rim: 0f, emission: H(0xFF3040)), false);
                var pp = pl.AddComponent<PressurePlate>(); pp.glow = pl.GetComponent<Renderer>(); plates.plates.Add(pp);
                var crate = Box(g, "GiftCrate", plateAt[i] + crateOff[i] + Vector3.up * 1.2f, new Vector3(2.4f, 2.4f, 2.4f), M["Paint" + (i + 1)]);
                Box(crate.transform, "Ribbon", crate.transform.position, new Vector3(2.45f, 2.45f, 0.4f), M["M_Kit_MP_Red"], false).transform.localScale = new Vector3(1.02f, 1.02f, 0.17f);
                crate.AddComponent<PushCrate>().cellSize = 3f;
            }
            var amb = root.GetComponentInChildren<AmbushChallenge>(); if (amb != null) amb.requires = plates;
            // ---------- optional: east-avenue circuit → a vault of minutes on a rising platform
            var bonus = Reward(g, new Vector3(62, 1, 8), "eastbonus", "วงจรติด! แท่นลับลอยขึ้นมา", 0);
            var bonusPlat = Group(g, "BonusPlatform");
            SBox(bonusPlat, "Plinth", new Vector3(62, 1.5f, 18), new Vector3(4f, 3f, 4f), M["Paint1"]);
            var bonusCache = new GameObject("BonusMinutes"); bonusCache.transform.SetParent(bonusPlat, false); bonusCache.transform.position = new Vector3(62, 3.8f, 18);
            var mc = bonusCache.AddComponent<MinuteCache>(); mc.key = "bonusEast"; mc.count = 10; mc.radius = 1.4f;
            bonus.show = new[] { bonusPlat.gameObject };
            Circuit(g, new Vector3(62, 0, 6), 3, 3, 0, 2, Sol3x3, bonus, H(0xFFD040), "East");
            // ---------- wayfinding: beacons + arrows (spawn → blackout → plaza → each district)
            Beacon(g, new Vector3(0, 0, -98), H(0xFFD040), "Blackout", puzzle: blackout);
            Color[] hc = { H(0x40E8FF), H(0xA070FF), H(0xFF5AA0) };
            for (int i = 0; i < hours.Count && i < 3; i++) Beacon(g, hours[i].transform.position, hc[i], "Hour" + i, hour: hours[i]);
            var arrows = Group(g, "Arrows");
            Arrows(arrows, new Vector3(0, 0, -146), new Vector3(0, 0, -104));
            Arrows(arrows, new Vector3(0, 0, -90), new Vector3(0, 0, -52), new Vector3(0, 0, -20), new Vector3(80, 0, -20), new Vector3(84, 0, -33));
            Arrows(arrows, new Vector3(-25, 0, 0), new Vector3(-88, 0, 0));
            Arrows(arrows, new Vector3(0, 0, 46), new Vector3(0, 0, 98));
            pd.blackout = blackout; pd.fountain = seal;
        }

        static void StationPlatform(Transform parent, Vector3 c, Vector3 towardTrack, string name)
        {
            var g = Group(parent, "Station_" + name);
            float y = 19.9f;
            SBox(g, "Platform", new Vector3(c.x, y - 0.3f, c.z), new Vector3(30f, 0.6f, 6f), M["Concrete2"]);
            SBox(g, "Edge", new Vector3(c.x, y + 0.02f, c.z + towardTrack.z * 2.85f), new Vector3(30f, 0.06f, 0.3f), M["M_Kit_MP_LightGreen"], false);
            SBox(g, "Rail", new Vector3(c.x, y + 0.55f, c.z - towardTrack.z * 2.9f), new Vector3(30f, 1.1f, 0.2f), M["M_Kit_MP_Metal"]);
            for (int s = -1; s <= 1; s += 2)
            {
                SBox(g, "Column", new Vector3(c.x + s * 13f, (y - 0.6f) / 2f, c.z), new Vector3(1f, y - 0.6f, 1f), M["Concrete2"]);
                SBox(g, "RoofPost", new Vector3(c.x + s * 13f, y + 1.8f, c.z - towardTrack.z * 2.6f), new Vector3(0.3f, 3.6f, 0.3f), M["M_Kit_MP_Dark"], false);
            }
            SBox(g, "Roof", new Vector3(c.x, y + 3.7f, c.z - towardTrack.z * 1.2f), new Vector3(30f, 0.3f, 4f), M["Roof"], false);
            SQuad(g, "StationSign", new Vector3(c.x, y + 3.0f, c.z - towardTrack.z * 3.0f), new Vector2(8f, 1.6f), -towardTrack, M["Sign" + 3]);
            for (int k = 0; k < 4; k++)
            {
                var p = Ped(g, new Vector3(c.x - 9f + k * 6f, y, c.z - towardTrack.z * 1.5f), 400 + k);
                p.rotation = Quaternion.LookRotation(towardTrack);
            }
        }

        static void Hazard(Transform parent, Vector3 at, float len, float speed)
        {
            var g = new GameObject("ClockHandFan"); g.transform.SetParent(parent, false); g.transform.position = at;
            Box(g.transform, "Hub", at + Vector3.up * 0.8f, new Vector3(0.9f, 1.6f, 0.9f), M["M_Kit_MP_Brass"]);
            var arm = new GameObject("Arm").transform; arm.SetParent(g.transform, false); arm.position = at + Vector3.up * 1.1f;
            Box(arm, "HandA", arm.position + Vector3.forward * len * 0.5f, new Vector3(0.7f, 0.35f, len), M["M_Kit_MP_Red"], false);
            Box(arm, "HandB", arm.position - Vector3.forward * len * 0.5f, new Vector3(0.7f, 0.35f, len), M["M_Kit_MP_Red"], false);
            Box(arm, "TipA", arm.position + Vector3.forward * len, Vector3.one * 0.6f, M["M_Kit_MP_LightRed"], false);
            Box(arm, "TipB", arm.position - Vector3.forward * len, Vector3.one * 0.6f, M["M_Kit_MP_LightRed"], false);
            var h = g.AddComponent<SpinHazard>(); h.arm = arm; h.armLength = len; h.degPerSec = speed;
        }

        static void Bell(Transform parent, Vector3 at, float radius, float seconds)
        {
            var g = new GameObject("FreezeBell"); g.transform.SetParent(parent, false); g.transform.position = at;
            for (int s = -1; s <= 1; s += 2) Box(g.transform, "Post", at + new Vector3(s * 0.9f, 1.3f, 0), new Vector3(0.25f, 2.6f, 0.25f), M["M_Kit_MP_Dark"]);
            Box(g.transform, "Bar", at + Vector3.up * 2.6f, new Vector3(2.1f, 0.25f, 0.25f), M["M_Kit_MP_Dark"], false);
            var bell = new GameObject("Bell").transform; bell.SetParent(g.transform, false); bell.position = at + Vector3.up * 2.5f;
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cup.name = "Cup"; cup.transform.SetParent(bell, false);
            cup.transform.localPosition = Vector3.down * 0.55f; cup.transform.localScale = new Vector3(1.1f, 0.5f, 1.1f); cup.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_MP_FreezeBell", H(0x9FE8FF), rim: 0.4f, emission: H(0x40C8FF) * 0.8f);
            var fb = g.AddComponent<FreezeBell>(); fb.bell = bell; fb.radius = radius; fb.seconds = seconds;
        }

        /// <summary>The ridable monorail: replaces the free-running cars with a driver that stops at both stations.</summary>
        static void RidableTrain(Transform root, List<Vector3> loop, List<Transform> cars)
        {
            var go = new GameObject("TrainDriver"); go.transform.SetParent(root, false);
            var td = go.AddComponent<TrainDriver>(); td.points = loop; td.cars = cars; td.spacing = 12.6f; td.speed = 14f; td.dwell = 8f;
            float L(Vector3 p)
            {
                float acc = 0f, best = float.MaxValue, at = 0f;
                for (int i = 0; i < loop.Count; i++)
                {
                    Vector3 a = loop[i], b = loop[(i + 1) % loop.Count]; Vector3 ab = b - a; float len = ab.magnitude;
                    float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / (len * len)); float dd = (a + ab * t - p).sqrMagnitude;
                    if (dd < best) { best = dd; at = acc + t * len; }
                    acc += len;
                }
                return at;
            }
            float centerOffset = td.spacing * (cars.Count - 1) * 0.5f;
            td.stops = new List<float> { L(new Vector3(0, 17, -70)) + centerOffset, L(new Vector3(0, 17, 80)) + centerOffset };
            foreach (var c in cars)
            {
                var bc = c.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.6f, 0); bc.size = new Vector3(2.6f, 2.6f, 12f);
                var mp = c.gameObject.AddComponent<MovingPlatform>(); mp.AutoMove = false;
            }
        }
    }
}
