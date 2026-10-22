using System.Collections.Generic;
using DreamRush.Look;
using DreamRush.Palace;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M17 WALK-IN BUILDINGS (user: "I don't want press-and-warp — I want to just walk in, and see inside from close
    /// up"). Every city building's podium is a real hollow room behind an open doorway and big shop windows, furnished
    /// for its type, sized to the building: Dreamer's Flat, Watchmaker (shop), Moonlight Cafe (rest + save), Shadow
    /// Dojo (wave trial, big buildings), Clock Library (hour compass), Time Vault (two-storey gear climb → chest).
    /// Rooms are dressed (resident + minutes) the first time Pip walks in (InteriorDirector, no loading).
    /// </summary>
    public static partial class MidnightPalaceBuilder
    {
        const string BedDir = "Assets/DreamRush/Art/Bedroom/Models";
        static int _doorN, _cacheN;
        static readonly List<Vector3> _doorOutsides = new List<Vector3>();
        static readonly List<InteriorRoom> _rooms = new List<InteriorRoom>();
        static bool NearDoor(Vector3 p, float r) { foreach (var o in _doorOutsides) if ((o - p).sqrMagnitude < r * r) return true; return false; }
        static readonly RoomType[] SmallRooms = { RoomType.Flat, RoomType.Cafe, RoomType.Flat, RoomType.Watchmaker, RoomType.Flat, RoomType.Library, RoomType.Flat, RoomType.Cafe };
        static readonly RoomType[] BigRooms = { RoomType.Dojo, RoomType.Vault, RoomType.Library, RoomType.Watchmaker };
        static int _smallN, _bigN;
        static readonly Dictionary<RoomType, string[]> Signs = new Dictionary<RoomType, string[]>
        {
            [RoomType.Watchmaker] = new[] { "GEARHART & SONS", "TICK-TOCK REPAIRS", "THE SPRING SHOP" },
            [RoomType.Cafe] = new[] { "MOONLIGHT CAFE", "CAFE 11:59", "THE SLEEPY BEAN" },
            [RoomType.Dojo] = new[] { "SHADOW DOJO", "DOJO OF THE SECOND HAND" },
            [RoomType.Library] = new[] { "CLOCK LIBRARY", "HALL OF HOURS" },
            [RoomType.Vault] = new[] { "TIME VAULT", "OLD MINUTE BANK", "LOCKED HOUR VAULT" },
        };

        static RoomType NextRoomType(bool big) => big ? BigRooms[_bigN++ % BigRooms.Length] : SmallRooms[_smallN++ % SmallRooms.Length];
        static float PodiumHeight(RoomType t) => t == RoomType.Vault ? 11.5f : t == RoomType.Dojo ? 7.5f : Rf(5.6f, 6.4f);

        // ================================================================== the doorway (no door to press: an open, lit archway)
        static BuildingDoor Door(Transform parent, Vector3 c, float w, float d, Vector3 n, RoomType type)
        {
            float half = Mathf.Abs(n.x) > 0.5f ? w / 2f : d / 2f;
            var rot = Quaternion.LookRotation(n); float yaw = rot.eulerAngles.y;
            Vector3 face = c + n * half; Vector3 t = Vector3.Cross(Vector3.up, n);
            var g = new GameObject("Door_" + type).transform; g.SetParent(parent, false); g.SetPositionAndRotation(face, rot);
            var mat = M["Door" + type];
            for (int s = -1; s <= 1; s += 2) Box(g, "Jamb", face + t * s * 1.7f + n * 0.15f + Vector3.up * 1.9f, new Vector3(0.35f, 3.8f, 0.5f), M["Billboard"], true, yaw);
            Box(g, "Lintel", face + n * 0.15f + Vector3.up * 3.95f, new Vector3(3.8f, 0.4f, 0.5f), M["Billboard"], false, yaw);
            var glow = Box(g, "ArchNeon", face + n * 0.42f + Vector3.up * 3.78f, new Vector3(3.2f, 0.14f, 0.08f), mat, false, yaw);
            for (int s = -1; s <= 1; s += 2) Box(g, "ArchNeonSide", face + t * s * 1.5f + n * 0.42f + Vector3.up * 1.9f, new Vector3(0.12f, 3.7f, 0.08f), mat, false, yaw);
            Box(g, "Awning", face + n * 0.9f + Vector3.up * 4.4f, new Vector3(4.2f, 0.22f, 1.8f), mat, false, yaw);
            Box(g, "Mat", face + n * 0.9f + Vector3.up * 0.03f, new Vector3(2.6f, 0.04f, 1.6f), mat, false, yaw);
            if (type != RoomType.Flat)
            {
                var icon = Box(g, "Beacon", face + n * 1.2f + Vector3.up * 6f, new Vector3(1.2f, 1.2f, 0.18f), mat, false);
                icon.transform.rotation = rot * Quaternion.Euler(0, 0, 45);
            }
            var entry = new GameObject("Entry").transform; entry.SetParent(g, false); entry.SetPositionAndRotation(face + n * 0.9f + Vector3.up * 1f, rot);
            var door = entry.gameObject.AddComponent<BuildingDoor>();
            door.id = $"B{_doorN:000}"; door.type = type; door.seed = R.Next(1, 999999); door.glow = glow.GetComponent<Renderer>();
            door.sign = Signs.TryGetValue(type, out var names) ? names[R.Next(names.Length)] : "";
            var o = new GameObject("Outside").transform; o.SetParent(g, false); o.SetPositionAndRotation(face + n * 2.4f + Vector3.up * 0.15f, rot);
            door.outside = o; _doorOutsides.Add(o.position);
            if (R.NextDouble() < 0.16)
            {
                Vector3 tt = t * (R.NextDouble() < 0.5 ? 3.4f : -3.4f);
                var vg = new GameObject("VendingMachine").transform; vg.SetParent(g, false); vg.SetPositionAndRotation(face + tt + n * 0.75f, rot);
                var m = Place(vg, "VENDING", vg.position, yaw);
                if (m != null)
                {
                    var b = Bounds(m); var bc = vg.gameObject.AddComponent<BoxCollider>();
                    bc.center = vg.InverseTransformPoint(b.center); bc.size = Mathf.Abs(n.x) > 0.5f ? new Vector3(b.size.z, b.size.y, b.size.x) : b.size;
                }
                vg.gameObject.AddComponent<VendingMachine>().key = "vend:" + door.id;
            }
            _doorN++;
            return door;
        }

        static void Cache(Transform parent, Vector3 at, int count)
        {
            var go = new GameObject("MinuteCache"); go.transform.SetParent(parent, false); go.transform.position = at;
            var mc = go.AddComponent<MinuteCache>(); mc.key = "cache" + _cacheN++; mc.count = count;
        }

        static Bounds Bounds(Transform t)
        {
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(t.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        // ================================================================== director + resident template
        static InteriorDirector Interiors(Transform root, Transform templates, GameObject tick, GameObject brute, GameObject roller, GameObject cuckoo)
        {
            var dir = new GameObject("InteriorDirector").AddComponent<InteriorDirector>();
            var npc = new GameObject("NPC_Clockfolk_Template"); npc.transform.SetParent(templates, false);
            var model = Place(npc.transform, "NPC_Clockfolk", Vector3.zero);
            if (model != null) DreamLook.AddOutline(model.gameObject, 1.4f);
            npc.AddComponent<ClockfolkLook>(); npc.AddComponent<NpcTalker>();
            npc.SetActive(false);
            dir.npcTemplate = npc;
            dir.rooms = new List<InteriorRoom>(_rooms);
            foreach (var r in _rooms)
            {
                var trial = r.GetComponentInChildren<DojoTrial>(true);
                if (trial != null) { trial.tick = tick; trial.brute = brute; trial.roller = roller; trial.cuckoo = cuckoo; }
            }
            return dir;
        }

        // ================================================================== room-local helpers (+Z = into the building, door at −Z)
        static Vector3 L(InteriorRoom r, float x, float y, float z) => r.transform.TransformPoint(new Vector3(x, y, z));
        static float RY(InteriorRoom r, float y = 0f) => r.transform.eulerAngles.y + y;

        static GameObject RBox(InteriorRoom r, string n, Vector3 local, Vector3 size, Material m, bool col = true, bool spot = false)
        {
            var go = Box(r.transform, n, r.transform.TransformPoint(local), size, m, col, RY(r));
            if (spot) Spot(r, r.transform.TransformPoint(local + Vector3.up * size.y / 2f));
            return go;
        }

        static GameObject RPrim(InteriorRoom r, PrimitiveType type, string n, Vector3 local, Vector3 scale, Vector3 euler, Material m, bool col = false)
        {
            var go = GameObject.CreatePrimitive(type); go.name = n; if (!col) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(r.transform, false);
            go.transform.SetPositionAndRotation(r.transform.TransformPoint(local), r.transform.rotation * Quaternion.Euler(euler));
            go.transform.localScale = scale; go.GetComponent<MeshRenderer>().sharedMaterial = m;
            return go;
        }

        static Transform Furn(InteriorRoom room, string name, Vector3 local, float yaw, float scale = 1f, bool spot = false, bool solid = true)
        {
            string dir = System.IO.File.Exists($"{BedDir}/{name}.fbx") ? BedDir : ModelDir;
            var t = Place(room.transform, name, room.transform.TransformPoint(local), RY(room, yaw), scale, false, dir);
            if (t == null) return null;
            DreamLook.AddOutline(t.gameObject, 1.4f);
            var b = Bounds(t);
            if (solid)
            {
                var col = new GameObject(name + "_Col"); col.transform.SetParent(room.transform, false); col.transform.position = b.center;
                col.AddComponent<BoxCollider>().size = b.size;
            }
            if (spot) Spot(room, new Vector3(b.center.x, b.max.y, b.center.z));
            return t;
        }

        static void Spot(InteriorRoom room, Vector3 world)
        {
            var s = new GameObject("Spot").transform; s.SetParent(room.transform, false); s.position = world; room.spots.Add(s);
        }

        // ================================================================== the hollow podium + its room
        static readonly Dictionary<RoomType, (int wall, int floor, Color light)> RoomLook = new Dictionary<RoomType, (int, int, Color)>
        {
            [RoomType.Flat] = (0x7A6AA8, 0x8A5A40, new Color(1f, 0.85f, 0.7f)),
            [RoomType.Watchmaker] = (0x2E5A4E, 0x5A3A2A, new Color(1f, 0.88f, 0.6f)),
            [RoomType.Cafe] = (0xC86A90, 0xB98E74, new Color(1f, 0.8f, 0.8f)),
            [RoomType.Dojo] = (0x6A2230, 0xC9B27A, new Color(1f, 0.75f, 0.6f)),
            [RoomType.Library] = (0x2A3870, 0x6A4030, new Color(0.8f, 0.88f, 1f)),
            [RoomType.Vault] = (0x40326E, 0x2A2250, new Color(0.85f, 0.75f, 1f)),
        };

        static InteriorRoom WalkIn(Transform g, Vector3 c, float w, float d, float pod, Vector3 n, BuildingDoor door, RoomType type)
        {
            bool alongZ = Mathf.Abs(n.z) > 0.5f;
            float lw = alongZ ? w : d, ld = alongZ ? d : w;
            // M21: big lots made 29 x 29 m hangars. The room is capped at 20 x 16 m against the street front; the rest of
            // the podium stays solid (front filler wall + the full roof slab), so rooms read as rooms, not warehouses.
            float pw = lw, pd = ld; lw = Mathf.Min(lw, 20f); ld = Mathf.Min(ld, 16f);
            float shift = (pd - ld) / 2f;
            var go = new GameObject("Room_" + type); go.transform.SetParent(g, false);
            go.transform.SetPositionAndRotation(c + n * shift + Vector3.up * 0.02f, Quaternion.LookRotation(-n));
            var r = go.AddComponent<InteriorRoom>(); r.type = type; r.door = door;
            const float T = 0.4f;
            var look = RoomLook[type];
            var wm = Mat("M_MP_IntWall" + type, H(look.wall), rim: 0.1f); var fm = Mat("M_MP_IntFloor" + type, H(look.floor), rim: 0.05f, spec: 0.2f);
            var accent = M["Door" + type];
            r.ambient = look.light;
            // floor, ceiling (= the slab the tower stands on), back + side walls
            RBox(r, "Floor", new Vector3(0, 0.02f, 0), new Vector3(lw - 2 * T, 0.04f, ld - 2 * T), fm, false);
            RBox(r, "Ceiling", new Vector3(0, pod - T / 2f, shift), new Vector3(pw, T, pd), wm);
            RBox(r, "WallBack", new Vector3(0, pod / 2f, ld / 2f - T / 2f), new Vector3(lw, pod, T), wm);
            RBox(r, "WallL", new Vector3(-lw / 2f + T / 2f, pod / 2f, 0), new Vector3(T, pod, ld), wm);
            RBox(r, "WallR", new Vector3(lw / 2f - T / 2f, pod / 2f, 0), new Vector3(T, pod, ld), wm);
            // front: a doorway (3 m) and big open shop windows, so the street sees inside
            float z = -ld / 2f + T / 2f; const float doorW = 3.0f, doorH = 3.6f, sill = 0.9f; float top = Mathf.Min(pod - T, 4.2f);
            float sideW = (lw - doorW) / 2f;
            for (int s = -1; s <= 1; s += 2)
            {
                float cx = s * (doorW / 2f + sideW / 2f);
                RBox(r, "Sill", new Vector3(cx, sill / 2f, z), new Vector3(sideW, sill, T), M["Podium"]);
                int panes = Mathf.Max(1, Mathf.RoundToInt(sideW / 3f));
                for (int k = 0; k <= panes; k++) RBox(r, "Mullion", new Vector3(s * (doorW / 2f + k * sideW / panes), (sill + top) / 2f, z), new Vector3(0.2f, top - sill, T), M["M_Kit_MP_Dark"]);
                RBox(r, "WindowGlow", new Vector3(cx, sill + 0.06f, z - 0.05f), new Vector3(sideW, 0.08f, 0.5f), accent, false);
            }
            RBox(r, "Head", new Vector3(0, (top + pod) / 2f, z), new Vector3(pw, pod - top, T), M["Podium"]);
            if (pw > lw + 0.1f) for (int s = -1; s <= 1; s += 2)   // solid front either side of a capped room
                RBox(r, "FrontFill", new Vector3(s * (lw / 2f + (pw - lw) / 4f), top / 2f, z), new Vector3((pw - lw) / 2f, top, T), M["Podium"]);
            if (top < doorH + 0.1f) { }
            RBox(r, "DoorHead", new Vector3(0, (doorH + top) / 2f, z), new Vector3(doorW, Mathf.Max(0.05f, top - doorH), T), M["Podium"]);
            // interior trim + light
            RBox(r, "Trim", new Vector3(0, pod - T - 0.3f, ld / 2f - T - 0.05f), new Vector3(lw - 2 * T, 0.12f, 0.1f), accent, false);
            var l = new GameObject("RoomLight").AddComponent<Light>(); l.transform.SetParent(go.transform, false);
            l.transform.position = L(r, 0, pod - 1.2f, 0); l.type = LightType.Point; l.range = Mathf.Max(lw, ld) * 0.9f; l.intensity = 2.4f; l.color = look.light; l.shadows = LightShadows.None;
            var spawn = new GameObject("Spawn").transform; spawn.SetParent(go.transform, false); spawn.SetPositionAndRotation(L(r, 0, 0.1f, -ld / 2f + 1.8f), go.transform.rotation); r.spawn = spawn;
            var npcSpot = new GameObject("NpcSpot").transform; npcSpot.SetParent(go.transform, false); npcSpot.SetPositionAndRotation(L(r, 1.4f, 0, 0.6f), go.transform.rotation * Quaternion.Euler(0, 200f, 0)); r.npcSpot = npcSpot;
            var dyn = new GameObject("Dynamic").transform; dyn.SetParent(go.transform, false); r.dynamicRoot = dyn;
            var rc = go.transform.position - Vector3.up * 0.02f;
            r.area = new Bounds(rc + Vector3.up * pod / 2f, alongZ ? new Vector3(lw - 2 * T - 0.2f, pod, ld - 2 * T - 0.2f) : new Vector3(ld - 2 * T - 0.2f, pod, lw - 2 * T - 0.2f));
            float iw = lw - 2 * T, id = ld - 2 * T;
            switch (type)
            {
                case RoomType.Flat: FurnishFlat(r, iw, id); break;
                case RoomType.Watchmaker: FurnishShop(r, iw, id, pod); break;
                case RoomType.Cafe: FurnishCafe(r, iw, id); break;
                case RoomType.Dojo: FurnishDojo(r, iw, id, pod); break;
                case RoomType.Library: FurnishLibrary(r, iw, id); break;
                case RoomType.Vault: FurnishVault(r, iw, id, pod); break;
            }
            // a few paper customers inside, so a glance through the window shows life
            int guests = type == RoomType.Cafe ? 3 : type == RoomType.Watchmaker || type == RoomType.Library ? 2 : type == RoomType.Flat ? 0 : 1;
            for (int k = 0; k < guests; k++)
            {
                var p = Ped(go.transform, L(r, Rf(-iw / 3f, iw / 3f), 0.05f, Rf(-id / 4f, id / 6f)), _doorN * 31 + k);
                p.rotation = go.transform.rotation * Quaternion.Euler(0, Rf(-60, 60), 0);
            }
            _rooms.Add(r);
            return r;
        }

        // ================================================================== M21 furnishing: detailed stylized kit (build_interior_kit.py)
        // Rooms are composed in zones (sleep / work / lounge, counter / tables / booth, stacks / reading nook...) and keep
        // a clear lane from the doorway (x in ±1.6 for z < 0) so walking in is never blocked.
        static Transform K(InteriorRoom r, string name, float x, float z, float yaw, float scale = 1f, bool spot = false, bool solid = true, float y = 0f)
            => Furn(r, name, new Vector3(x, y, z), yaw, scale, spot, solid);

        static void Pendants(InteriorRoom r, float h, params Vector2[] at)
        {
            foreach (var p in at) K(r, "INT_Pendant", p.x, p.y, 0, 1f, false, false, h - 0.45f);
        }

        static void Lounge(InteriorRoom r, float x, float z, float yaw, bool sofa = false)
        {
            // rug + seat(s) + side table + floor lamp, facing yaw
            var q = Quaternion.Euler(0, yaw, 0);
            Vector3 P(float lx, float lz) => new Vector3(x, 0, z) + q * new Vector3(lx, 0, lz);
            var c = P(0, 0); K(r, "INT_Rug", c.x, c.z, yaw, 0.8f, false, false);
            if (sofa) { var s1 = P(0, 0.9f); K(r, "INT_Sofa", s1.x, s1.z, yaw + 180f, 1f, true); }
            else for (int s = -1; s <= 1; s += 2) { var a = P(s * 0.75f, 0.6f); K(r, "INT_Armchair", a.x, a.z, yaw + 180f + s * 18f, 1f, s > 0); }
            var t = P(sofa ? 1.45f : 0f, sofa ? 0.9f : 0.95f); K(r, "INT_SideTable", t.x, t.z, yaw, 1f, true);
            var l = P(sofa ? -1.4f : -1.35f, 1.0f); K(r, "INT_FloorLamp", l.x, l.z, yaw, 1f, false, true);
        }

        static void FurnishFlat(InteriorRoom r, float w, float d)
        {
            float h = 5.6f;
            K(r, "INT_Bed", -w / 2 + 1.3f, d / 2 - 1.4f, 180f, 1f, true);
            K(r, "INT_Nightstand", -w / 2 + 2.6f, d / 2 - 0.35f, 180f, 1f, true);
            K(r, "INT_Wardrobe", w / 2 - 1.0f, d / 2 - 0.45f, 180f);
            K(r, "INT_Bookcase", w / 2 - 0.3f, 0.6f, -90f, 0.9f, true);
            K(r, "INT_Desk", -w / 2 + 0.55f, -0.4f, 90f, 1f, true);
            Lounge(r, w / 2 - 2.6f, -d / 2 + 2.4f, 200f);
            K(r, "INT_Monstera", -w / 2 + 0.6f, -d / 2 + 0.8f, 30f, 1.1f);
            K(r, "INT_Monstera", w / 2 - 0.6f, d / 2 - 1.6f, 120f, 0.9f);
            K(r, "INT_Rug", -0.6f, 0.9f, 10f, 0.9f, false, false);
            Pendants(r, h, new Vector2(0, 0.5f));
            Spot(r, L(r, 0.6f, 0f, 1.2f));
        }

        static void FurnishShop(InteriorRoom r, float w, float d, float h)
        {
            var wood = M["M_Kit_MP_IntWood"]; var gold = M["M_Kit_MP_Gold"];
            // the watchmaker's counter (glass cases) with the big dial on the back wall
            K(r, "INT_DisplayCase", -0.75f, d / 2 - 2.6f, 180f, 1f, true);
            K(r, "INT_DisplayCase", 0.75f, d / 2 - 2.6f, 180f, 1f, true);
            r.npcSpot.SetPositionAndRotation(L(r, 0, 0, d / 2 - 1.4f), r.transform.rotation * Quaternion.Euler(0, 180, 0));
            RPrim(r, PrimitiveType.Cylinder, "BigDial", new Vector3(0, Mathf.Min(h - 1.6f, 3.6f), d / 2 - 0.3f), new Vector3(2.6f, 0.1f, 2.6f), new Vector3(90, 0, 0), gold);
            RPrim(r, PrimitiveType.Cylinder, "BigDialFace", new Vector3(0, Mathf.Min(h - 1.6f, 3.6f), d / 2 - 0.36f), new Vector3(2.2f, 0.1f, 2.2f), new Vector3(90, 0, 0), M["M_Kit_MP_Face"]);
            // benches along the back wall, grandfather clocks along the sides, display islands in the middle (big shops)
            for (int s = -1; s <= 1; s += 2)
            {
                K(r, "INT_Workbench", s * (Mathf.Min(w / 2 - 1.4f, 4.2f)), d / 2 - 0.5f, 180f, 1f, true);
                int clocks = Mathf.Max(2, Mathf.FloorToInt((d - 4f) / 1.6f));
                for (int k = 0; k < clocks; k++) K(r, "INT_GrandfatherClock", s * (w / 2 - 0.35f), -d / 2 + 2.4f + k * (d - 5f) / Mathf.Max(1, clocks - 1), s < 0 ? 90f : -90f, 1f, k == 0);
                if (w > 18f) for (int k = 0; k < 3; k++) K(r, "INT_DisplayCase", s * w / 4f, -d / 2 + 4f + k * 3.2f, 90f, 1f, k == 1);
            }
            var gear = Furn(r, "GEAR_Platform", new Vector3(w / 2 - 2.6f, 0.2f, -d / 2 + 2.6f), 0, 0.3f, true);
            if (gear != null) gear.gameObject.AddComponent<ClockSpin>().axis = Vector3.up;
            K(r, "INT_Monstera", -w / 2 + 0.8f, -d / 2 + 1f, 0, 1.2f);
            K(r, "INT_Rug", 0, -0.5f, 0, w > 18f ? 1.3f : 0.9f, false, false);
            Pendants(r, h, new Vector2(-2.5f, d / 2 - 2.6f), new Vector2(2.5f, d / 2 - 2.6f), new Vector2(0, 0));
        }

        static void FurnishCafe(InteriorRoom r, float w, float d)
        {
            float h = 5.6f;
            float cw = Mathf.Min(1f, (w - 3f) / 3.4f);
            K(r, "INT_CafeCounter", 0, d / 2 - 1.6f, 180f, cw, true);
            r.npcSpot.SetPositionAndRotation(L(r, 0, 0, d / 2 - 0.7f), r.transform.rotation * Quaternion.Euler(0, 180, 0));
            Furn(r, "MOON_Crescent", new Vector3(w / 2 - 2.4f, 3.0f, d / 2 - 0.3f), 180f, 1.0f, false, false);
            // two rows of bistro tables either side of the entry lane
            int cols = Mathf.Clamp(Mathf.FloorToInt((w / 2 - 2f) / 2.4f), 1, 3);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < cols; i++)
                    for (int j = 0; j < 2; j++)
                    {
                        float x = s * (2.4f + i * 2.4f), z = -d / 2 + 2.6f + j * 2.6f;
                        if (z > d / 2 - 3.4f) continue;
                        var t = K(r, "INT_BistroTable", x, z, (i + j) * 30f, 1f, false);
                        if (t != null) Spot(r, t.position + Vector3.up * 0.8f);
                        K(r, "INT_BistroChair", x - 0.62f, z, 90f, 1f, false, false);
                        K(r, "INT_BistroChair", x + 0.62f, z, -90f, 1f, false, false);
                    }
            // a booth sofa against one side wall, plants in the corners, pendants over the tables
            K(r, "INT_Sofa", -w / 2 + 0.6f, d / 2 - 3.6f, 90f, 1f, true);
            K(r, "INT_SideTable", -w / 2 + 0.6f, d / 2 - 2.2f, 0f, 1f, true);
            K(r, "INT_Monstera", w / 2 - 0.7f, -d / 2 + 0.9f, 0, 1.2f);
            K(r, "INT_Monstera", w / 2 - 0.7f, d / 2 - 0.8f, 60, 1.0f);
            K(r, "INT_FloorLamp", w / 2 - 0.6f, 0f, 0f);
            var pend = new List<Vector2>();
            for (int s = -1; s <= 1; s += 2) for (int i = 0; i < cols; i++) pend.Add(new Vector2(s * (2.4f + i * 2.4f), -d / 2 + 3.9f));
            Pendants(r, h, pend.ToArray());
        }

        static void FurnishDojo(InteriorRoom r, float w, float d, float h)
        {
            float rad = Mathf.Min(w, d) * 0.36f;
            RPrim(r, PrimitiveType.Cylinder, "Arena", new Vector3(0, 0.05f, 0.5f), new Vector3(rad * 2f, 0.02f, rad * 2f), Vector3.zero, M["M_Kit_MP_Wood"]);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                RPrim(r, PrimitiveType.Cube, "RingNeon", new Vector3(Mathf.Sin(a) * rad, 0.08f, 0.5f + Mathf.Cos(a) * rad), new Vector3(rad * 0.27f, 0.08f, 0.2f), new Vector3(0, a * Mathf.Rad2Deg + 90f, 0), M["DoorDojo"]);
            }
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    RBox(r, "Pillar", new Vector3(sx * (w / 2 - 1.2f), h / 2, sz * (d / 2 - 1.2f)), new Vector3(1f, h, 1f), M["M_Kit_MP_Wood"]);
            var gongLocal = new Vector3(0, 0, d / 2 - 1.4f);
            var gong = new GameObject("Gong").transform; gong.SetParent(r.transform, false); gong.SetPositionAndRotation(r.transform.TransformPoint(gongLocal), r.transform.rotation);
            for (int s = -1; s <= 1; s += 2) RBox(r, "Post", gongLocal + new Vector3(s * 1.6f, 1.7f, 0), new Vector3(0.3f, 3.4f, 0.3f), M["M_Kit_MP_Wood"]);
            RBox(r, "Beam", gongLocal + new Vector3(0, 3.4f, 0), new Vector3(3.8f, 0.3f, 0.35f), M["M_Kit_MP_Red"], false);
            RPrim(r, PrimitiveType.Cylinder, "Disc", gongLocal + new Vector3(0, 1.9f, 0), new Vector3(2.2f, 0.08f, 2.2f), new Vector3(90, 0, 0), M["M_Kit_MP_Gold"]).transform.SetParent(gong, true);
            var trial = gong.gameObject.AddComponent<DojoTrial>();
            var center = new GameObject("ArenaCenter").transform; center.SetParent(r.transform, false); center.position = L(r, 0, 0, 0.5f);
            trial.arenaCenter = center;
            r.npcSpot.SetPositionAndRotation(L(r, -4f, 0, d / 2 - 2.2f), r.transform.rotation * Quaternion.Euler(0, 160, 0));
            Spot(r, L(r, w / 2 - 2.6f, 0, d / 2 - 2.6f)); Spot(r, L(r, -w / 2 + 2.6f, 0, -d / 2 + 2.6f));
            // M21 dressing outside the fighting ring: tatami walkways, practice dummies, glove racks, stone lanterns
            for (int s = -1; s <= 1; s += 2)
            {
                for (float z = -d / 2 + 1.5f; z < d / 2 - 1.5f; z += 1.0f) K(r, "INT_Tatami", s * (w / 2 - 2.2f), z, 90f, 1f, false, false, 0.05f);
                K(r, "INT_TrainingDummy", s * (w / 2 - 1.1f), -d / 4f, s < 0 ? 90f : -90f, 1f, true);
                K(r, "INT_TrainingDummy", s * (w / 2 - 1.1f), d / 6f, s < 0 ? 90f : -90f, 1f, false);
                K(r, "INT_GloveRack", s * (w / 2 - 0.35f), 0.2f, s < 0 ? 90f : -90f, 1f, true);
                K(r, "INT_StoneLantern", s * (w / 2 - 1.2f), -d / 2 + 1.2f, 0f, 1.1f);
                K(r, "INT_StoneLantern", s * 2.6f, d / 2 - 1.2f, 180f, 1.1f);
            }
        }

        static void FurnishLibrary(InteriorRoom r, float w, float d)
        {
            float h = 6.4f;
            // wall-to-wall stacks on the back and side walls
            int n = Mathf.Max(2, Mathf.FloorToInt((w - 1f) / 2.2f));
            for (int i = 0; i < n; i++) K(r, "INT_Bookcase", -w / 2 + 1.3f + i * (w - 2.6f) / Mathf.Max(1, n - 1), d / 2 - 0.3f, 180f, 1f, i % 3 == 0);
            int sn = Mathf.Max(1, Mathf.FloorToInt((d - 6f) / 2.2f));
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < sn; i++) K(r, "INT_Bookcase", s * (w / 2 - 0.3f), -d / 2 + 4f + i * 2.2f, s < 0 ? 90f : -90f, 1f, i == 0 && s > 0);
            // big libraries: freestanding back-to-back stacks make aisles left and right of the central lane
            if (w > 18f)
                for (int s = -1; s <= 1; s += 2)
                    for (int row = 0; row < 3; row++)
                    {
                        float x = s * (w / 4f + 0.6f), z = -d / 2 + 6f + row * 3.4f;
                        if (z > d / 2 - 3.5f) continue;
                        for (int k = -1; k <= 1; k += 2) K(r, "INT_Bookcase", x + k * 1.05f, z - 0.24f, 180f, 1f, row == 1 && k > 0);
                        for (int k = -1; k <= 1; k += 2) K(r, "INT_Bookcase", x + k * 1.05f, z + 0.24f, 0f, 1f, false, false);
                    }
            RBox(r, "MapTable", new Vector3(0, 0.5f, 0.3f), new Vector3(3.6f, 1f, 2.2f), M["M_Kit_MP_IntWood"]);
            RBox(r, "Map", new Vector3(0, 1.03f, 0.3f), new Vector3(3.2f, 0.05f, 1.8f), M["Glass"], false);
            RBox(r, "MapTower", new Vector3(0, 1.3f, 0.3f), new Vector3(0.2f, 0.5f, 0.2f), M["M_Kit_MP_Gold"], false);
            Spot(r, L(r, 1.4f, 1.05f, 0.3f));
            // reading nooks in the front corners
            Lounge(r, -w / 2 + 2.6f, -d / 2 + 2.6f, 160f);
            Lounge(r, w / 2 - 2.8f, -d / 2 + 2.4f, 200f, true);
            K(r, "INT_Monstera", -w / 2 + 0.7f, d / 2 - 1.2f, 0, 1.2f);
            Pendants(r, h, new Vector2(-1.2f, 0.3f), new Vector2(1.2f, 0.3f));
            r.npcSpot.SetPositionAndRotation(L(r, -2.6f, 0, 1.8f), r.transform.rotation * Quaternion.Euler(0, 160, 0));
        }

        static void FurnishVault(InteriorRoom r, float w, float d, float h)
        {
            // two storeys: spinning gear steps up to a mezzanine with the chest
            float rad = Mathf.Min(w, d) * 0.28f; int steps = 5; float rise = (h - 3.4f) / (steps + 1);
            for (int i = 0; i < steps; i++)
            {
                float a = (220f - i * 34f) * Mathf.Deg2Rad;
                var gear = Furn(r, "GEAR_Platform", new Vector3(Mathf.Sin(a) * rad, 1.0f + i * rise, Mathf.Cos(a) * rad), 0, 0.62f, false, false);
                if (gear != null)
                {
                    foreach (var mf in gear.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                    var mp = gear.gameObject.AddComponent<World1.MovingPlatform>(); mp.spinDegPerSec = (i % 2 == 0 ? 1 : -1) * (14f + i * 3f); mp.carryRadius = 2f;
                }
            }
            float ly = 1.0f + steps * rise;
            RBox(r, "Mezzanine", new Vector3(0, ly - 0.3f, d / 2 - 2.2f), new Vector3(w - 1f, 0.6f, 4.4f), M["M_Kit_MP_Stone"]);
            RBox(r, "MezzNeon", new Vector3(0, ly + 0.02f, d / 2 - 4.4f), new Vector3(w - 1f, 0.06f, 0.14f), M["DoorVault"], false);
            var chest = new GameObject("VaultChest").transform; chest.SetParent(r.transform, false); chest.SetPositionAndRotation(L(r, 0, ly, d / 2 - 1.5f), r.transform.rotation);
            Box(chest, "ChestBase", chest.position + Vector3.up * 0.4f, new Vector3(1.6f, 0.8f, 1.0f), M["M_Kit_MP_Feather"], true, RY(r));
            var lid = new GameObject("Lid").transform; lid.SetParent(chest, false); lid.SetPositionAndRotation(chest.position + Vector3.up * 0.8f + chest.forward * 0.5f, chest.rotation);
            Box(lid, "LidMesh", chest.position + Vector3.up * 0.95f, new Vector3(1.66f, 0.3f, 1.06f), M["M_Kit_MP_Feather"], false, RY(r));
            Box(lid, "Lock", chest.position + Vector3.up * 0.82f - chest.forward * 0.55f, new Vector3(0.25f, 0.3f, 0.08f), M["M_Kit_MP_Gold"], false, RY(r));
            chest.gameObject.AddComponent<VaultChest>().lid = lid;
            r.npcSpot.SetPositionAndRotation(L(r, -w / 2 + 2f, 0, -d / 2 + 2.6f), r.transform.rotation * Quaternion.Euler(0, 140, 0));
            // M21: a real vault — round safe doors in the side walls, gold spilling on the mezzanine and the floor
            for (int s = -1; s <= 1; s += 2)
            {
                K(r, "INT_SafeDoor", s * (w / 2 - 0.15f), -d / 2 + 4.5f, s < 0 ? 90f : -90f, 1.2f, false, false);
                K(r, "INT_GoldPile", s * 1.6f, d / 2 - 1.4f, s * 40f, 0.9f, false, false, ly);
                K(r, "INT_GoldPile", s * (w / 2 - 1.6f), d / 2 - 6.2f, s * 70f, 1.2f, true);
            }
            K(r, "INT_DisplayCase", 0, -d / 2 + 4.2f, 0f, 1f, true);
        }
    }
}
