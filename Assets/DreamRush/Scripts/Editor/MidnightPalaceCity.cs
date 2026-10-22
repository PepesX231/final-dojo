using System.Collections.Generic;
using DreamRush.Look;
using DreamRush.Palace;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// M16 — the city rebuilt after the user's Night City references ("everywhere I look it's the same building; the
    /// special objects are nothing; you stacked the same box into a wall"). Lessons taken (no assets/brands copied):
    ///  • SILHOUETTE VARIETY: six tower archetypes (setback tiers, cantilever top, painted megablock with colour bands,
    ///    round glass tower, balcony residential, twin towers + skybridge), landmark skyscrapers, mixed materials.
    ///  • STREET LEVEL: every podium is a row of lit shops with awnings + signs; tall ad screens down the facades.
    ///  • LAYERS ABOVE THE STREET: a monorail loop with a train, two elevated highways, skybridges, sky traffic lanes.
    ///  • LIFE: pedestrians on every sidewalk, cars on the streets/highways, kiosks, palms, lamps, traffic lights,
    ///    yellow crossings, steam vents, giant holograms.
    ///  • THE EDGE: a ring road + barrier on the bay, water to the horizon, a hazy distant skyline (no box wall).
    /// </summary>
    public static partial class MidnightPalaceBuilder
    {
        struct Bld { public Vector3 c; public float w, d, h; }
        static List<Bld> _blds;
        static List<(Vector3 c, float size, int district)> _blockList;
        static readonly List<GameObject> _static = new List<GameObject>();
        static readonly float[] StreetLines = { -120f, -70f, -20f, 30f, 80f, 130f };
        static Color[] Tints => new[] { H(0xE0823A), H(0x5DB89C), H(0x2F8FA0), H(0xE6D8BC), H(0xC9574F), H(0x8C7BD6) };
        static readonly Color[] CarColors = { new Color(0.95f, 0.3f, 0.45f), new Color(1f, 0.82f, 0.2f), new Color(0.2f, 0.75f, 0.95f), new Color(0.92f, 0.92f, 0.95f), new Color(0.15f, 0.15f, 0.2f), new Color(0.55f, 0.35f, 0.95f), new Color(1f, 0.5f, 0.15f) };
        static readonly Color[] PedColors = { new Color(0.45f, 0.6f, 1f), new Color(1f, 0.55f, 0.7f), new Color(0.5f, 0.85f, 0.6f), new Color(1f, 0.75f, 0.3f), new Color(0.7f, 0.5f, 1f), new Color(0.35f, 0.85f, 0.9f), new Color(0.95f, 0.95f, 0.95f), new Color(0.3f, 0.3f, 0.4f) };

        // ================================================================== materials (called from Materials())
        static void CityMaterials(Dictionary<string, Material> m)
        {
            m["Glass"] = Mat("M_MP_Glass2", Color.white, "T_MP_Glass", 40f, "T_MP_Glass_E", Color.white * 1.0f, rim: 0.35f, spec: 0.7f);
            m["Concrete"] = Mat("M_MP_Concrete", Color.white, "T_MP_Concrete", 34f, "T_MP_Concrete_E", Color.white * 0.95f, rim: 0.08f);
            m["Balcony"] = Mat("M_MP_Balcony", Color.white, "T_MP_Balcony", 22f, "T_MP_Balcony_E", Color.white * 0.95f, rim: 0.08f);
            m["Industrial"] = Mat("M_MP_Industrial", Color.white, "T_MP_Industrial", 18f, "T_MP_Industrial_E", Color.white * 0.9f, rim: 0.08f);
            var tints = Tints;
            for (int i = 0; i < tints.Length; i++)
            {
                m["Panel" + i] = Mat($"M_MP_Panel{i}", tints[i], "T_MP_Panel", 24f, "T_MP_Panel_E", Color.white * 0.95f, rim: 0.06f);
                m["Paint" + i] = Mat($"M_MP_Paint{i}", tints[i] * 0.95f, rim: 0.12f);
            }
            m["Store"] = Mat("M_MP_Store", new Color(0.7f, 0.7f, 0.75f), "T_MP_Storefront", 0f, "T_MP_Storefront_E", Color.white * 0.7f, rim: 0f);
            m["Podium"] = Mat("M_MP_Podium", H(0x2C2836), rim: 0.1f);
            m["Bridge"] = Mat("M_MP_Bridge", H(0x9FD8FF), "T_MP_Glass", 12f, "T_MP_Glass_E", Color.white * 2f, rim: 0.5f, spec: 0.8f);
            m["Concrete2"] = Mat("M_MP_Deck", H(0x6E6A72), "T_MP_Sidewalk", 6f, rim: 0.05f);
            m["CrossY"] = Mat("M_MP_CrossY", Color.white, "T_MP_CrossYellow", 0f, rim: 0f, spec: 0.1f);
            m["Water"] = Mat("M_MP_Bay", H(0x060B18), rim: 0.05f, spec: 1f, emission: H(0x02040A));
            for (int i = 0; i < 8; i++)
            {
                m["AdV" + i] = Mat($"M_MP_AdV{i}", Color.white, $"T_MP_AdV{i}", 0f, $"T_MP_AdV{i}", Color.white * 0.95f, rim: 0f);
                m["AdH" + i] = Mat($"M_MP_AdH{i}", Color.white, $"T_MP_AdH{i}", 0f, $"T_MP_AdH{i}", Color.white * 0.95f, rim: 0f);
            }
            // city-life kit (build_city_life.py)
            m["M_Kit_MP_CarPaint"] = Mat("M_Kit_MP_CarPaint", Color.white, rim: 0.4f, spec: 0.8f);
            m["M_Kit_MP_CarGlass"] = Mat("M_Kit_MP_CarGlass", H(0x24405E), rim: 0.7f, spec: 1f, emission: H(0x0C1830));
            m["M_Kit_MP_HeadLight"] = Mat("M_Kit_MP_HeadLight", H(0xFFF6DD), rim: 0f, emission: H(0xFFF2CC) * 1.6f);
            m["M_Kit_MP_TailLight"] = Mat("M_Kit_MP_TailLight", H(0xFF2040), rim: 0f, emission: H(0xFF1030) * 2f);
            m["M_Kit_MP_Trunk"] = Mat("M_Kit_MP_Trunk", H(0x7A5A40), rim: 0.1f);
            m["M_Kit_MP_Leaf"] = Mat("M_Kit_MP_Leaf", H(0x3E9A52), rim: 0.3f);
            m["M_Kit_MP_Screen"] = Mat("M_Kit_MP_Screen", H(0x7FF0FF), rim: 0f, emission: H(0x30D8FF) * 2.2f);
            m["M_Kit_MP_LightRed"] = Mat("M_Kit_MP_LightRed", H(0xFF3030), rim: 0f, emission: H(0xFF1A1A) * 3f);
            m["M_Kit_MP_LightGreen"] = Mat("M_Kit_MP_LightGreen", H(0x40FF90), rim: 0f, emission: H(0x20FF80) * 3f);
            m["M_Kit_MP_Train"] = Mat("M_Kit_MP_Train", H(0x1FAF9F), rim: 0.3f, spec: 0.5f);
        }

        static Material HoloMat()
        {
            const string path = "Assets/DreamRush/Art/MidnightPalace/M_MP_Holo.mat";
            var sh = Shader.Find("DreamRush/DreamParticle");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.One); m.SetFloat("_Cull", 0f);
            m.SetColor("_BaseColor", new Color(0.3f, 0.9f, 1f, 0.5f)); m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================== helpers
        static GameObject SBox(Transform p, string n, Vector3 c, Vector3 s, Material m, bool col = true, float yaw = 0f)
        {
            var go = Box(p, n, c, s, m, col, yaw); _static.Add(go); return go;
        }

        static GameObject SQuad(Transform p, string n, Vector3 c, Vector2 s, Vector3 normal, Material m)
        {
            var go = Quad(p, n, c, s, Quaternion.LookRotation(-normal), m); _static.Add(go); return go;
        }

        static string RandomFacade(int district)
        {
            double r = R.NextDouble();
            switch (district)
            {
                case 0: return r < 0.4 ? "Glass" : r < 0.7 ? "Concrete" : r < 0.85 ? "Industrial" : "Balcony";     // Neon Alley: dense, mixed
                case 1: return r < 0.45 ? "Balcony" : r < 0.75 ? "Panel" + R.Next(6) : "Concrete";                 // Candy Rooftops: painted + homes
                case 2: return r < 0.4 ? "Industrial" : r < 0.7 ? "Concrete" : "Panel" + R.Next(6);               // Clockwork Canal: works
                default: return r < 0.55 ? "Glass" : r < 0.8 ? "Concrete" : "Panel" + R.Next(6);                  // Moonlit Plaza: corporate glass
            }
        }

        static Vector3 SideN(int s) => s == 0 ? Vector3.forward : s == 1 ? Vector3.back : s == 2 ? Vector3.right : Vector3.left;

        // ================================================================== blocks
        static void CityV2(Transform root)
        {
            var g = Group(root, "City");
            _blds = new List<Bld>(); _blockList = new List<(Vector3, float, int)>(); _static.Clear(); _doorOutsides.Clear(); _rooms.Clear(); _smallN = 0; _bigN = 0;
            for (float bx = -Half + 25; bx <= Half - 25; bx += Pitch)
                for (float bz = -Half + 25; bz <= Half - 25; bz += Pitch)
                {
                    var c = new Vector3(bx, 0, bz);
                    if (Reserved(c)) continue;
                    BlockV2(g, c, Pitch - Street - 2f);
                }
        }

        static void BlockV2(Transform parent, Vector3 c, float size)
        {
            int di = DistrictOf(c); var d = Districts[di];
            _blockList.Add((c, size, di));
            var blk = Group(parent, "Block");
            int n = R.NextDouble() < 0.3 ? 1 : 2;
            float lot = size / n;
            var mine = new List<Bld>();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    var lc = c + new Vector3((i - (n - 1) * 0.5f) * lot, 0, (j - (n - 1) * 0.5f) * lot);
                    float w = lot - Rf(5f, 7f), dpt = lot - Rf(5f, 7f);   // wider sidewalks: room for people, kiosks, palms
                    float h = Rf(d.hMin, d.hMax) * (n == 1 ? 1.7f : 1.15f);
                    if (R.NextDouble() < 0.14) h = Rf(60f, 105f);                         // landmark skyscrapers break the skyline
                    Vector3 outw = lc - c;
                    if (n == 1) outw = SideN(R.Next(4));
                    else outw = R.NextDouble() < 0.5 ? new Vector3(Mathf.Sign(outw.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(outw.z));
                    Tower(blk, lc, w, dpt, h, di, outw, NextRoomType(n == 1));
                    var b = new Bld { c = lc, w = w, d = dpt, h = h }; mine.Add(b); _blds.Add(b);
                }
            if (n == 2)
            {
                // skybridges over the alleys between neighbours
                for (int a = 0; a < mine.Count; a++)
                    for (int b = a + 1; b < mine.Count; b++)
                    {
                        var A = mine[a]; var B = mine[b];
                        bool adjX = Mathf.Abs(A.c.z - B.c.z) < 1f, adjZ = Mathf.Abs(A.c.x - B.c.x) < 1f;
                        if ((!adjX && !adjZ) || R.NextDouble() > 0.45) continue;
                        float y = Mathf.Min(A.h, B.h) * Rf(0.45f, 0.75f); if (y < 9f) continue;
                        Vector3 mid = (A.c + B.c) * 0.5f + Vector3.up * y;
                        float gap = adjX ? Mathf.Abs(A.c.x - B.c.x) : Mathf.Abs(A.c.z - B.c.z);
                        var s = adjX ? new Vector3(gap, 3.2f, 4f) : new Vector3(4f, 3.2f, gap);
                        SBox(blk, "Skybridge", mid, s, M["Bridge"]);
                        SBox(blk, "SkybridgeFloor", mid + Vector3.down * 1.75f, s + new Vector3(0.6f, -2.9f, 0.6f), M["Podium"]);
                        SBox(blk, "SkybridgeLight", mid + Vector3.down * 1.95f, adjX ? new Vector3(gap, 0.08f, 0.3f) : new Vector3(0.3f, 0.08f, gap), M["Neon" + d.neonA], false);
                    }
                if (R.NextDouble() < 0.45) Cache(blk, c + Vector3.up * 0.6f, 2);
                if (R.NextDouble() < 0.55) Steam(blk, c + Vector3.up * 0.1f, 1.4f, 7f);   // alley steam
            }
        }

        static void Steam(Transform p, Vector3 at, float width, float height)
        {
            var go = new GameObject("Steam"); go.transform.SetParent(p, false); go.transform.position = at;
            var s = go.AddComponent<SteamVent>(); s.width = width; s.height = height; s.rate = 5f + width * 2f;
        }

        // ================================================================== towers
        static void Tower(Transform parent, Vector3 c, float w, float d, float h, int di, Vector3 door, RoomType roomType)
        {
            var dd = Districts[di];
            float pod = PodiumHeight(roomType); h = Mathf.Max(h, pod + 8f);
            var g = new GameObject("Tower").transform; g.SetParent(parent, false); g.position = c;
            // ---- podium: a row of lit shops on every street face, awnings, signs
            // M17: no solid podium — the ground floor is a real walk-in room (built after the doorway below)
            for (int s = 0; s < 4; s++)
            {
                Vector3 n = SideN(s);
                bool doorSide = Vector3.Dot(n, door) > 0.9f; float face = s < 2 ? w : d; float half = s < 2 ? d / 2 : w / 2;
                Vector3 t = Vector3.Cross(Vector3.up, n);
                int shops = Mathf.Max(1, Mathf.RoundToInt(face / 15f));
                float sw = face / shops;
                for (int k = 0; k < shops && !doorSide; k++)
                {
                    Vector3 sc = c + n * (half + 0.04f) + t * ((k - (shops - 1) * 0.5f) * sw) + Vector3.up * ((pod - 0.9f) / 2f);
                    // M20 (user: "why does every building have the same bookshop window?"): the blind sides are a plain
                    // ground-floor wall with a frosted window band — only the door side shows the real room inside
                    SQuad(g, "PodiumWall", sc, new Vector2(sw - 0.3f, pod - 0.9f), n, M["Podium"]);
                    SQuad(g, "WindowBand", sc + n * 0.02f + Vector3.up * (pod * 0.12f), new Vector2(sw - 1.6f, Mathf.Min(1.6f, pod * 0.28f)), n, M["Glass"]);
                }
                var nm = M["Neon" + (R.NextDouble() < 0.5 ? dd.neonA : dd.neonB)];
                SBox(g, "Awning", c + n * (half + 0.7f) + Vector3.up * (pod - 0.55f), s < 2 ? new Vector3(w + 0.2f, 0.18f, 1.4f) : new Vector3(1.4f, 0.18f, d + 0.2f), M["Roof"], false);
                SBox(g, "AwningNeon", c + n * (half + 1.42f) + Vector3.up * (pod - 0.55f), s < 2 ? new Vector3(w + 0.2f, 0.1f, 0.1f) : new Vector3(0.1f, 0.1f, d + 0.2f), nm, false);
                if (R.NextDouble() < 0.7)
                {
                    bool vertical = R.NextDouble() < 0.35;
                    var size = vertical ? new Vector2(1.5f, 3.6f) : new Vector2(4.6f, 1.7f);
                    Vector3 at = c + n * (half + 0.08f) + t * Rf(-face / 3f, face / 3f) + Vector3.up * (pod + (vertical ? 2.4f : 1.2f));
                    var q = Quad(g, "Sign", at, size, Quaternion.LookRotation(-n) * (vertical ? Quaternion.Euler(0, 0, 90) : Quaternion.identity), M["Sign" + R.Next(12)]);
                    if (vertical) q.transform.localScale = new Vector3(size.y, size.x, 1);
                    _static.Add(q);
                }
            }
            // ---- the mass above: one of six archetypes
            float top = h; float topW = w, topD = d; Vector3 topC = c;
            int arch = R.Next(6);
            if (h > 55f && arch == 4) arch = 0;
            string fac = RandomFacade(di);
            switch (arch)
            {
                case 0: // setback tiers with lit ledges
                {
                    int tiers = R.Next(2, 5); float y = pod; float tw = w * Rf(0.85f, 0.97f), td = d * Rf(0.85f, 0.97f); Vector3 tc = c;
                    for (int i = 0; i < tiers; i++)
                    {
                        float th = (h - pod) / tiers * Rf(0.8f, 1.2f); if (i == tiers - 1) th = Mathf.Max(4f, h - y);
                        string f = i == 0 ? fac : RandomFacade(di);
                        SBox(g, "Tier", tc + Vector3.up * (y + th / 2), new Vector3(tw, th, td), M[f]);
                        y += th;
                        SBox(g, "Ledge", tc + Vector3.up * (y + 0.2f), new Vector3(tw + 0.8f, 0.4f, td + 0.8f), M["Roof"]);
                        if (R.NextDouble() < 0.6) SBox(g, "LedgeNeon", tc + Vector3.up * (y + 0.05f), new Vector3(tw + 0.85f, 0.1f, td + 0.85f), M["Neon" + dd.neonA], false);
                        if (i < tiers - 1) { tw *= Rf(0.68f, 0.85f); td *= Rf(0.68f, 0.85f); tc += new Vector3(Rf(-1.5f, 1.5f), 0, Rf(-1.5f, 1.5f)); }
                    }
                    top = y + 0.4f; topW = tw; topD = td; topC = tc;
                    break;
                }
                case 1: // slim shaft + wide cantilevered crown with lit underside
                {
                    float sw = w * 0.55f, sd = d * 0.6f, crown = Mathf.Clamp(h * 0.22f, 5f, 16f);
                    float shaft = h - crown;
                    SBox(g, "Shaft", c + Vector3.up * (pod + (shaft - pod) / 2), new Vector3(sw, shaft - pod, sd), M[fac]);
                    Vector3 off = new Vector3(R.NextDouble() < 0.5 ? Rf(-w * 0.2f, w * 0.2f) : 0f, 0, Rf(-d * 0.15f, d * 0.15f));
                    float cw = w * 1.08f, cd = d * 1.02f;
                    SBox(g, "Crown", c + off + Vector3.up * (shaft + crown / 2), new Vector3(cw, crown, cd), M[RandomFacade(di)]);
                    SBox(g, "CrownBelly", c + off + Vector3.up * (shaft - 0.05f), new Vector3(cw - 0.6f, 0.12f, cd - 0.6f), M["M_Kit_MP_LampGlow"], false);
                    for (int k = 0; k < 2; k++) SBox(g, "Strut", c + off * 0.5f + new Vector3((k * 2 - 1) * sw * 0.45f, shaft - 3f, 0), new Vector3(0.5f, 6f, 0.5f), M["M_Kit_MP_Dark"], false);
                    top = h; topW = cw; topD = cd; topC = c + off;
                    break;
                }
                case 2: // painted megablock: tinted panels + contrasting painted bands (orange/green like the ref)
                {
                    int ti = R.Next(6); int bi = (ti + 1 + R.Next(5)) % 6;
                    float bw = w * 0.96f, bd = d * 0.96f;
                    SBox(g, "Mega", c + Vector3.up * (pod + (h - pod) / 2), new Vector3(bw, h - pod, bd), M["Panel" + ti]);
                    int bands = R.Next(2, 4);
                    for (int k = 0; k < bands; k++)
                    {
                        float x = ((k + 0.5f) / bands - 0.5f) * bw;
                        SBox(g, "Band", c + new Vector3(x, pod + (h - pod) / 2, bd / 2 + 0.25f), new Vector3(Rf(1.2f, 2.4f), h - pod, 0.5f), M["Paint" + bi]);
                        SBox(g, "Band", c + new Vector3(x, pod + (h - pod) / 2, -bd / 2 - 0.25f), new Vector3(Rf(1.2f, 2.4f), h - pod, 0.5f), M["Paint" + bi]);
                    }
                    for (float y = pod + 10f; y < h - 3f; y += Rf(10f, 16f))
                        SBox(g, "Belt", c + Vector3.up * y, new Vector3(bw + 0.6f, 1.2f, bd + 0.6f), M["Paint" + bi]);
                    if (R.NextDouble() < 0.5)   // a box module bolted to the side (the ref's chunky add-ons)
                    {
                        float my = Rf(pod + 4f, Mathf.Max(pod + 5f, h - 8f));
                        SBox(g, "Module", c + new Vector3(bw / 2 + 2f, my, Rf(-bd / 4, bd / 4)), new Vector3(4f, 6f, bd * 0.4f), M["Paint" + ti]);
                    }
                    top = h; topW = bw; topD = bd;
                    break;
                }
                case 3: // round glass tower with ring ledges
                {
                    float r = Mathf.Min(w, d) * 0.45f;
                    var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cyl.name = "Round"; cyl.transform.SetParent(g, false);
                    cyl.transform.position = c + Vector3.up * (pod + (h - pod) / 2); cyl.transform.localScale = new Vector3(r * 2, (h - pod) / 2, r * 2);
                    cyl.GetComponent<MeshRenderer>().sharedMaterial = M[R.NextDouble() < 0.7 ? "Glass" : "Concrete"]; _static.Add(cyl);
                    for (float y = pod + 8f; y < h; y += Rf(8f, 14f))
                    {
                        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder); ring.name = "Ring"; ring.transform.SetParent(g, false);
                        ring.transform.position = c + Vector3.up * y; ring.transform.localScale = new Vector3(r * 2 + 1f, 0.2f, r * 2 + 1f);
                        ring.GetComponent<MeshRenderer>().sharedMaterial = R.NextDouble() < 0.5 ? M["Roof"] : M["Neon" + dd.neonB]; _static.Add(ring);
                    }
                    top = h; topW = topD = r * 1.4f;
                    break;
                }
                case 4: // residential slab with real balconies on the street faces + roof garden
                {
                    float bw = w * 0.9f, bd = d * 0.85f;
                    SBox(g, "Homes", c + Vector3.up * (pod + (h - pod) / 2), new Vector3(bw, h - pod, bd), M["Balcony"]);
                    for (float y = pod + 2.8f; y < h - 1f; y += 3.2f)
                        for (int sgn = -1; sgn <= 1; sgn += 2)
                            SBox(g, "Balcony", c + new Vector3(0, y, sgn * (bd / 2 + 0.5f)), new Vector3(bw * Rf(0.6f, 0.95f), 0.18f, 1f), M["M_Kit_MP_Mask"], false);
                    for (int k = 0; k < 3; k++) Place(g, "PALM_Tree", c + new Vector3(Rf(-bw / 3, bw / 3), h + 0.5f, Rf(-bd / 3, bd / 3)), Rf(0, 360), Rf(0.35f, 0.55f));
                    top = h; topW = bw; topD = bd;
                    break;
                }
                default: // twin towers joined by a skybridge
                {
                    float tw = w * 0.38f, td = d * 0.8f, h2 = h * Rf(0.7f, 0.9f);
                    string f2 = RandomFacade(di);
                    SBox(g, "TwinA", c + new Vector3(-w * 0.26f, pod + (h - pod) / 2, 0), new Vector3(tw, h - pod, td), M[fac]);
                    SBox(g, "TwinB", c + new Vector3(w * 0.26f, pod + (h2 - pod) / 2, 0), new Vector3(tw, h2 - pod, td), M[f2]);
                    float by = Mathf.Max(pod + 6f, h2 * 0.6f);
                    SBox(g, "TwinBridge", c + Vector3.up * by, new Vector3(w * 0.2f + 0.4f, 3f, 4f), M["Bridge"]);
                    if (h2 > 30f) SBox(g, "TwinBridge2", c + Vector3.up * (h2 - 6f), new Vector3(w * 0.2f + 0.4f, 2.6f, 3.4f), M["Bridge"]);
                    top = h; topW = tw; topD = td; topC = c + new Vector3(-w * 0.26f, 0, 0);
                    break;
                }
            }
            // ---- tall ad screens down a facade (the ref's vertical billboards)
            if (h > 22f && R.NextDouble() < 0.6)
            {
                int s = R.Next(4); Vector3 n = SideN(s);
                float half = (s < 2 ? Mathf.Min(d, topD) : Mathf.Min(w, topW)) / 2f;
                float len = Mathf.Min((h - pod) * 0.6f, 30f);
                Vector3 at = c + n * (half + 0.5f) + Vector3.up * (h - len / 2f - 3f);
                SQuad(g, "AdV", at, new Vector2(len / 4f, len), n, M["AdV" + R.Next(8)]);
                SBox(g, "AdFrame", at - n * 0.25f, s < 2 ? new Vector3(len / 4f + 0.4f, len + 0.4f, 0.4f) : new Vector3(0.4f, len + 0.4f, len / 4f + 0.4f), M["M_Kit_MP_Dark"], false);
            }
            if (R.NextDouble() < 0.28)
            {
                int s = R.Next(4); Vector3 n = SideN(s); float half = (s < 2 ? d : w) / 2f;
                Vector3 at = c + n * (half + 0.12f) + Vector3.up * (pod + 4.5f);
                SQuad(g, "AdH", at, new Vector2(8f, 4f), n, M["AdH" + R.Next(8)]);
            }
            // ---- roof
            Vector3 roof = topC + Vector3.up * top;
            SBox(g, "RoofCap", roof + Vector3.up * 0.2f, new Vector3(topW + 0.4f, 0.4f, topD + 0.4f), M["Roof"]);
            double roll = R.NextDouble();
            if (roll < 0.3) Place(g, "WATER_Tank", roof + new Vector3(Rf(-topW / 4, topW / 4), 0.4f, Rf(-topD / 4, topD / 4)), Rf(0, 360));
            else if (roll < 0.6) for (int k = 0; k < 3; k++) Place(g, "AC_Unit", roof + new Vector3(Rf(-topW / 3, topW / 3), 0.4f, Rf(-topD / 3, topD / 3)), Rf(0, 360), 1.3f);
            if (R.NextDouble() < 0.45)
            {
                int ants = R.Next(1, 4);
                for (int k = 0; k < ants; k++)
                {
                    float ah = Rf(4f, h > 55f ? 18f : 9f); Vector3 ap = roof + new Vector3(Rf(-topW / 3, topW / 3), ah / 2, Rf(-topD / 3, topD / 3));
                    SBox(g, "Antenna", ap, new Vector3(0.25f, ah, 0.25f), M["M_Kit_MP_Dark"], false);
                    SBox(g, "Beacon", ap + Vector3.up * (ah / 2 + 0.2f), Vector3.one * 0.45f, M["M_Kit_MP_LightRed"], false);
                }
            }
            if (R.NextDouble() < 0.15) Billboard(g, roof + Vector3.up * 0.4f, topW, topD);
            if (R.NextDouble() < 0.18) Steam(g, roof + new Vector3(Rf(-topW / 3, topW / 3), 0.5f, Rf(-topD / 3, topD / 3)), 1.6f, 9f);
            if (R.NextDouble() < 0.22) Cache(g, roof + new Vector3(Rf(-topW / 4, topW / 4), 1f, Rf(-topD / 4, topD / 4)), 3);
            // CLOCKWORK CANAL signature: giant turning gears bolted onto the walls
            if (di == 2 && h > 18f && R.NextDouble() < 0.45)
            {
                int s = R.Next(4); Vector3 n = SideN(s); float half = (s < 2 ? d : w) / 2f;
                var gear = Place(g, "GEAR_Platform", c + n * (half + 0.4f) + Vector3.up * Rf(pod + 6f, h - 4f), 0f, Rf(2.2f, 3.6f));
                if (gear != null) { gear.rotation = Quaternion.FromToRotation(Vector3.up, n); var cs = gear.gameObject.AddComponent<ClockSpin>(); cs.axis = Vector3.up; cs.degPerSec = (R.NextDouble() < 0.5 ? 1 : -1) * Rf(6f, 14f); }
            }
            var bdoor = Door(g, c, w, d, door, roomType);
            WalkIn(g, c, w, d, pod, door, bdoor, roomType);
        }

        // ================================================================== district signatures
        static void Signatures(Transform root)
        {
            var g = Group(root, "Signatures");
            // CANDY ROOFTOPS: a neon ferris wheel (turning, gondolas stay level)
            {
                Vector3 c = new Vector3(105, 0, -95); float R0 = 26f, Hc = 32f;
                var wheel = new GameObject("FerrisWheel").transform; wheel.SetParent(g, false); wheel.position = c + Vector3.up * Hc;
                var ring = K_Torus(wheel, "Rim", R0, 0.5f, M["Neon0"]); var ring2 = K_Torus(wheel, "Rim2", R0 - 2f, 0.3f, M["Neon2"]);
                for (int i = 0; i < 16; i++)
                {
                    float a = i / 16f * 360f;
                    var spoke = Box(wheel, "Spoke", wheel.position, new Vector3(0.25f, R0 * 2f, 0.25f), M["M_Kit_MP_Metal"], false);
                    spoke.transform.rotation = Quaternion.Euler(0, 0, a); spoke.transform.position = wheel.position;
                    var pivot = new GameObject("GondolaPivot").transform; pivot.SetParent(wheel, false);
                    pivot.localPosition = Quaternion.Euler(0, 0, a) * Vector3.up * R0;
                    var gond = Box(pivot, "Gondola", pivot.position + Vector3.down * 1.6f, new Vector3(2.2f, 2.4f, 2.2f), M["Paint" + (i % 6)], false);
                    Box(pivot, "GondolaWin", pivot.position + Vector3.down * 1.4f, new Vector3(2.3f, 1f, 1.6f), M["M_Kit_MP_Window"], false);
                    var cs = pivot.gameObject.AddComponent<ClockSpin>(); cs.axis = Vector3.forward; cs.degPerSec = 7f;   // counter-rotate: stays level
                }
                var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder); hub.name = "Hub"; hub.transform.SetParent(wheel, false); hub.transform.localRotation = Quaternion.Euler(90, 0, 0); hub.transform.localScale = new Vector3(3, 1.2f, 3);
                hub.GetComponent<MeshRenderer>().sharedMaterial = M["M_Kit_MP_Gold"];
                var spin = wheel.gameObject.AddComponent<ClockSpin>(); spin.axis = Vector3.forward; spin.degPerSec = -7f;
                for (int s = -1; s <= 1; s += 2)
                {
                    var leg = SBox(g, "Leg", c + new Vector3(s * 9f, Hc / 2f, 0), new Vector3(1.2f, Hc + 2f, 1.2f), M["M_Kit_MP_Metal"]);
                    leg.transform.rotation = Quaternion.Euler(0, 0, -s * 15f);
                }
                SBox(g, "WheelBase", c + Vector3.up * 1f, new Vector3(30f, 2f, 10f), M["Podium"]);
                SQuad(g, "WheelSign", c + new Vector3(0, 3.2f, -5.05f), new Vector2(14f, 3.5f), Vector3.back, M["AdH" + 7]);
            }
            // M21: the cables/lanterns/banners strung across the streets are gone (user: floating cables all over the map)
            // gates over the four grand avenues (entrances to the tower plaza)
            for (int k = 0; k < 4; k++)
            {
                var rot = Quaternion.Euler(0, k * 90f, 0); Vector3 c = rot * new Vector3(0, 0, -52f); Vector3 side = rot * Vector3.right;
                for (int s = -1; s <= 1; s += 2)
                {
                    SBox(g, "GatePillar", c + side * s * 12f + Vector3.up * 7f, new Vector3(1.6f, 14f, 1.6f), M["M_Kit_MP_Red"], true);
                    SBox(g, "GatePillarNeon", c + side * s * 12f + Vector3.up * 7f, new Vector3(1.7f, 14f, 0.2f), M["Neon" + (k % 2)], false, k * 90f);
                }
                SBox(g, "GateBeam", c + Vector3.up * 14.5f, new Vector3(28f, 1.4f, 1.8f), M["M_Kit_MP_Red"], false, k * 90f);
                SBox(g, "GateBeam2", c + Vector3.up * 12.6f, new Vector3(26f, 0.6f, 1f), M["M_Kit_MP_Dark"], false, k * 90f);
                var sign = Quad(g, "GateSign", c + Vector3.up * 16.6f + rot * Vector3.back * 0.95f, new Vector2(12f, 3f), rot, M["AdH" + (k * 2 % 8)]); _static.Add(sign);
                var sign2 = Quad(g, "GateSign", c + Vector3.up * 16.6f + rot * Vector3.forward * 0.95f, new Vector2(12f, 3f), rot * Quaternion.Euler(0, 180, 0), M["AdH" + ((k * 2 + 1) % 8)]); _static.Add(sign2);
                SBox(g, "GateSignFrame", c + Vector3.up * 16.6f, new Vector3(12.6f, 3.6f, 1.6f), M["M_Kit_MP_Dark"], false, k * 90f);
            }
        }

        static GameObject K_Torus(Transform parent, string name, float R0, float r, Material m)
        {
            // a ring of short boxes (no torus primitive in Unity)
            var holder = new GameObject(name); holder.transform.SetParent(parent, false);
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f; float seg = 2f * Mathf.PI * R0 / 48f + 0.05f;
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.DestroyImmediate(b.GetComponent<Collider>()); b.name = "Seg";
                b.transform.SetParent(holder.transform, false);
                b.transform.localPosition = new Vector3(Mathf.Cos(a) * R0, Mathf.Sin(a) * R0, 0); b.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
                b.transform.localScale = new Vector3(r, seg, r); b.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            return holder;
        }

        // ================================================================== M21 street dressing (user: "the roads are empty")
        static void StreetDressing(Transform props, Transform cars)
        {
            var placed = new List<Vector3>();
            bool Free(Vector3 p, float r) { foreach (var q in placed) if ((q - p).sqrMagnitude < r * r) return false; return !NearDoor(p, 4.5f); }
            string[] kit = { "ST_Planter", "ST_Planter", "ST_Planter", "ST_Hydrant", "ST_NewsBox", "ST_BikeRack", "ST_Bollard", "ST_VendorCart" };
            int parked = 0, items = 0;
            foreach (var (c, size, di) in _blockList)
            {
                for (int s = 0; s < 4; s++)
                {
                    Vector3 nrm = SideN(s); Vector3 t = Vector3.Cross(Vector3.up, nrm);
                    float yawRoad = Quaternion.LookRotation(nrm).eulerAngles.y;
                    // sidewalk furniture on the curb band, between the lamps and away from doors
                    bool stop = false;
                    for (float u = -size / 2f + 3f; u <= size / 2f - 3f; u += 3f)
                    {
                        Vector3 p = c + nrm * (size / 2f + 0.2f) + t * (u + Rf(-0.4f, 0.4f));
                        if (!Free(p, 2.6f)) continue;
                        double roll = R.NextDouble();
                        if (!stop && roll < 0.08 && Mathf.Abs(u) < size / 2f - 6f)
                        {
                            Vector3 bp = c + nrm * (size / 2f - 0.6f) + t * u;
                            if (Free(bp, 4f)) { var b = Place(props, "ST_BusStop", bp, yawRoad); if (b != null) { AddBoxCollider(b); placed.Add(bp); stop = true; items++; } }
                            continue;
                        }
                        if (roll > 0.55) continue;
                        string n = kit[R.Next(kit.Length)];
                        var tr = Place(props, n, p, yawRoad + (n == "ST_VendorCart" || n == "ST_BikeRack" ? 90f : 0f));
                        if (tr == null) continue;
                        if (n != "ST_Bollard" && n != "ST_Hydrant") AddBoxCollider(tr);
                        placed.Add(p); items++;
                    }
                    // parked cars in the curb lane (outside the moving lanes at ±3 m), clear of the crossings
                    for (float u = -size / 2f + 6f; u <= size / 2f - 6f; u += Rf(6.2f, 8f))
                    {
                        if (R.NextDouble() > 0.45) continue;
                        Vector3 p = c + nrm * (size / 2f + 2f) + t * u + Vector3.up * 0.35f;
                        if (InCanal(p)) continue;
                        var car = Place(cars, "CAR_Hover", p, Quaternion.LookRotation(t * (R.NextDouble() < 0.5 ? 1 : -1)).eulerAngles.y, 0.92f);
                        if (car == null) continue;
                        var tint = car.gameObject.AddComponent<RandomTint>(); tint.keyword = "CarPaint"; tint.palette = CarColors; tint.seed = R.Next(1, 99999);
                        AddBoxCollider(car); _static.Add(car.gameObject); parked++;
                    }
                }
            }
            // the grand avenues: flower-bed islands down the middle with benches, so the wide strips have a park feel
            for (int k = 0; k < 4; k++)
            {
                var rot = Quaternion.Euler(0, k * 90f, 0); Vector3 along = rot * Vector3.forward, perp = rot * Vector3.right;
                for (float dd = 66f; dd < Half - 10f; dd += 28f)
                {
                    Vector3 mid = along * dd + (k % 2 == 0 ? new Vector3(5f, 0, 0) : new Vector3(0, 0, 5f));
                    if (InCanal(mid)) continue;
                    Place(props, "ST_FlowerBed", mid, Quaternion.LookRotation(along).eulerAngles.y + 90f);
                    for (int s = -1; s <= 1; s += 2) Place(props, "BENCH_Bin", mid + perp * (s * 2.2f), Quaternion.LookRotation(perp * s).eulerAngles.y);
                    items += 3;
                }
            }
            Debug.Log($"[DreamRush] Palace street dressing: {items} sidewalk/avenue props, {parked} parked cars");
        }

        static void AddBoxCollider(Transform t)
        {
            var b = new Bounds(t.position, Vector3.zero); bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (!any) return;
            var bc = t.gameObject.AddComponent<BoxCollider>();
            bc.center = t.InverseTransformPoint(b.center);
            var sz = t.InverseTransformVector(b.size); bc.size = new Vector3(Mathf.Abs(sz.x), Mathf.Abs(sz.y), Mathf.Abs(sz.z));
        }

        // ================================================================== street life
        static void StreetLife(Transform root)
        {
            var g = Group(root, "StreetLife");
            var cars = Group(g, "Traffic"); var peds = Group(g, "Pedestrians"); var props = Group(g, "Props");
            // yellow crossings + traffic lights at every intersection
            foreach (float x in StreetLines)
                foreach (float z in StreetLines)
                {
                    if (InCanal(new Vector3(x, 0, z))) continue;   // M20: no crossings on the canal
                    float o = Street / 2f + 1.8f;
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var q = Quad(props, "Crossing", new Vector3(x, 0.05f, z + s * o), new Vector2(Street, 3.2f), Quaternion.Euler(90, 0, 0), M["CrossY"]); _static.Add(q);
                        var q2 = Quad(props, "Crossing", new Vector3(x + s * o, 0.055f, z), new Vector2(Street, 3.2f), Quaternion.Euler(90, 90, 0), M["CrossY"]); _static.Add(q2);
                    }
                    if (Mathf.Abs(x) < 46 && Mathf.Abs(z) < 46) continue;
                    if (R.NextDouble() < 0.6)
                    {
                        float cx = Street / 2f + 0.8f;
                        Place(props, "TRAFFIC_Light", new Vector3(x + cx, 0, z + cx), -90f);
                        Place(props, "TRAFFIC_Light", new Vector3(x - cx, 0, z - cx), 90f);
                    }
                }
            // per block: sidewalk loop of pedestrians, lamps, a kiosk, benches, steam
            int pedN = 0;
            foreach (var (c, size, di) in _blockList)
            {
                float e = size / 2f - 1.8f;
                var loop = new List<Vector3> { c + new Vector3(-e, 0, -e), c + new Vector3(e, 0, -e), c + new Vector3(e, 0, e), c + new Vector3(-e, 0, e) };
                int n = di == 0 ? 7 : 5;
                for (int k = 0; k < n; k++)
                {
                    var path = new List<Vector3>(loop); if (k % 2 == 1) path.Reverse();
                    float lane = k % 2 == 0 ? 0f : 0.8f;
                    for (int i = 0; i < path.Count; i++) path[i] += (path[i] - c).normalized * lane;
                    Walker(peds, path, Rf(1.0f, 1.6f), Rf(0, 4f * size), pedN++);
                }
                for (int s = 0; s < 4; s++)
                {
                    Vector3 nrm = SideN(s); Vector3 t = Vector3.Cross(Vector3.up, nrm);
                    for (float u = -size / 2f + 4f; u <= size / 2f - 4f; u += 16f)
                        Place(props, "STREET_Lamp", c + nrm * (size / 2f + 0.6f) + t * u, Quaternion.LookRotation(-nrm).eulerAngles.y + 90f, 1.1f);
                    if (di == 1 || di == 3 || R.NextDouble() < 0.3)
                        Place(props, "PALM_Tree", c + nrm * (size / 2f + 0.5f) + t * Rf(-size / 3f, size / 3f), Rf(0, 360), Rf(0.8f, 1.1f));
                }
                if (R.NextDouble() < 0.6)
                {
                    int s = R.Next(4); Vector3 nrm = SideN(s); Vector3 t = Vector3.Cross(Vector3.up, nrm);
                    Vector3 kp = c + nrm * (size / 2f + 0.2f) + t * Rf(-size / 4f, size / 4f);
                    var k = NearDoor(kp, 6f) ? null : Place(props, "KIOSK_Food", kp, Quaternion.LookRotation(t).eulerAngles.y);
                    if (k != null)
                    {
                        var tint = k.gameObject.AddComponent<RandomTint>(); tint.keyword = "CarPaint"; tint.palette = CarColors; tint.seed = R.Next(1, 99999);
                        foreach (var mf in k.GetComponentsInChildren<MeshFilter>()) { var bc = mf.gameObject.AddComponent<BoxCollider>(); }
                        // a queue of dreamers at the counter
                        for (int q = 0; q < R.Next(1, 4); q++)
                        {
                            var p = Ped(peds, k.position + t * (2.2f + q * 0.9f) + nrm * Rf(-0.3f, 0.3f), pedN++);
                            p.rotation = Quaternion.LookRotation(-t);
                        }
                    }
                }
                if (R.NextDouble() < 0.5)
                {
                    int s = R.Next(4); Vector3 nrm = SideN(s);
                    Place(props, "BENCH_Bin", c + nrm * (size / 2f + 0.4f) + Vector3.Cross(Vector3.up, nrm) * Rf(-size / 3f, size / 3f), Quaternion.LookRotation(-nrm).eulerAngles.y);
                }
                if (R.NextDouble() < 0.25) Steam(props, c + SideN(R.Next(4)) * (size / 2f + 4.5f) + Vector3.up * 0.05f, 1.0f, 4f);   // manhole
            }
            StreetDressing(props, cars);
            // palms + lamps along the four grand avenues, crowds on the tower plaza
            for (int k = 0; k < 4; k++)
            {
                var rot = Quaternion.Euler(0, k * 90f, 0);
                for (float dd = 52f; dd < Half; dd += 14f)
                {
                    // M20: the avenue strip is the reserved block column between the roads at -20 and +30 (centre 5, open -14..24);
                    // palms at ±15 from 0 stood in the road
                    Vector3 along = rot * Vector3.forward, perp = rot * Vector3.right;
                    Vector3 mid = along * dd + (k % 2 == 0 ? new Vector3(5f, 0, 0) : new Vector3(0, 0, 5f));
                    for (int s = -1; s <= 1; s += 2) Place(props, "PALM_Tree", mid + perp * (s * 13.5f), Rf(0, 360), Rf(0.9f, 1.2f));
                }
                var line = new List<Vector3> { rot * new Vector3(-10, 0, 48), rot * new Vector3(-10, 0, Half - 6), rot * new Vector3(10, 0, Half - 6), rot * new Vector3(10, 0, 48) };
                var back = new List<Vector3>(line); back.Reverse();
                for (int i = 0; i < 6; i++) Walker(peds, i % 2 == 0 ? line : back, Rf(1f, 1.5f), Rf(0, 300f), pedN++);
            }
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f; var p = Ped(peds, new Vector3(Mathf.Sin(a) * 38f, 0, Mathf.Cos(a) * 38f), pedN++);
                p.rotation = Quaternion.LookRotation(-p.position);   // everyone looks up at the clock tower
            }
            // street traffic: two lanes per street (skipping lines that cross the tower plaza / plazas / rooftop run)
            float[] xLines = { -70f, 80f }, zLines = { -120f, -70f, 80f, 130f };
            int carN = 0;
            foreach (float x in xLines) for (int s = -1; s <= 1; s += 2) Lane(cars, new Vector3(x + s * 3f, 0.55f, -s * (Half + 10)), new Vector3(x + s * 3f, 0.55f, s * (Half + 10)), 3, ref carN);
            foreach (float z in zLines) for (int s = -1; s <= 1; s += 2) Lane(cars, new Vector3(s * (Half + 10), 0.55f, z + s * 3f), new Vector3(-s * (Half + 10), 0.55f, z + s * 3f), 3, ref carN);
            // sky traffic: three rings of flying cars
            float[] ys = { 38f, 54f, 72f }; float[] rs = { 115f, 95f, 135f };
            for (int i = 0; i < 3; i++)
            {
                float r = rs[i]; var ring = new List<Vector3> { new Vector3(-r, ys[i], -r), new Vector3(r, ys[i], -r), new Vector3(r, ys[i], r), new Vector3(-r, ys[i], r) };
                if (i % 2 == 1) ring.Reverse();
                for (int k = 0; k < 9; k++) Car(cars, ring, true, Rf(18f, 26f), k * r * 8f / 9f, carN++);
            }
        }

        // ================================================================== M17 paper-cutout residents (2.5D)
        static readonly string[] PeopleNames = { "Clockfolk0", "Clockfolk1", "Clockfolk2", "Sheep", "Pillow", "Cat", "MoonKid", "Teacup", "Bear", "Owl", "Candle", "Alarm", "Bunny", "Star", "Mushroom", "Robot" };
        static readonly float[] PeopleHeights = { 1.75f, 1.75f, 1.75f, 1.8f, 1.85f, 1.85f, 1.25f, 1.8f, 1.95f, 1.7f, 1.7f, 1.85f, 1.6f, 1.7f, 1.25f, 1.8f };
        static Material[] _paper;

        static Material PaperMat(int i)
        {
            _paper ??= new Material[PeopleNames.Length];
            if (_paper[i] != null) return _paper[i];
            string tp = $"Assets/DreamRush/Art/MidnightPalace/People/PPL_{PeopleNames[i]}.png";
            var imp = AssetImporter.GetAtPath(tp) as TextureImporter;
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Clamp)) { imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
            var m = ArtKit.Mat("M_MP_Paper_" + PeopleNames[i], Color.white, null, null, 0f, alphaClip: true, doubleSided: true, rampSoft: 0.5f);
            m.SetTexture("_BaseMap", tex); m.SetFloat("_BaseMapStrength", 1f); m.SetFloat("_Cutoff", 0.5f);
            m.SetTexture("_EmissionMap", tex); m.SetFloat("_EmissionMapStrength", 1f); m.SetColor("_Emission", Color.white * 0.32f);   // paper stays readable in the dusk
            EditorUtility.SetDirty(m);
            return _paper[i] = m;
        }

        static Transform Ped(Transform parent, Vector3 at, int seed)
        {
            int i = Mathf.Abs(seed * 7919 + 13) % PeopleNames.Length; float hgt = PeopleHeights[i] * Rf(0.92f, 1.06f);
            var go = new GameObject("Paper_" + PeopleNames[i]); go.transform.SetParent(parent, false); go.transform.position = at;
            var card = GameObject.CreatePrimitive(PrimitiveType.Quad); card.name = "Card"; Object.DestroyImmediate(card.GetComponent<Collider>());
            card.transform.SetParent(go.transform, false); card.transform.localPosition = Vector3.up * hgt * 0.5f; card.transform.localScale = new Vector3(hgt * 0.5f, hgt, 1f);
            var r = card.GetComponent<MeshRenderer>(); r.sharedMaterial = PaperMat(i); r.shadowCastingMode = ShadowCastingMode.On;
            var pp = go.AddComponent<PaperPerson>(); pp.card = card.transform; pp.height = hgt;
            return go.transform;
        }

        static void Walker(Transform parent, List<Vector3> path, float speed, float offset, int seed)
        {
            var t = Ped(parent, path[0], seed);
            var pm = t.gameObject.AddComponent<PathMover>(); pm.points = new List<Vector3>(path); pm.closed = true; pm.speed = speed; pm.offset = offset;
        }

        static void Lane(Transform parent, Vector3 a, Vector3 b, int count, ref int carN)
        {
            float len = Vector3.Distance(a, b);
            for (int k = 0; k < count; k++) Car(parent, new List<Vector3> { a, b }, false, Rf(10f, 16f), k * len / count + Rf(0, 20f), carN++).yieldToPlayer = true;
        }

        static PathMover Car(Transform parent, List<Vector3> path, bool closed, float speed, float offset, int seed)
        {
            var go = new GameObject("Car"); go.transform.SetParent(parent, false); go.transform.position = path[0];
            Place(go.transform, "CAR_Hover", path[0], 0f);
            var tint = go.AddComponent<RandomTint>(); tint.keyword = "CarPaint"; tint.palette = CarColors; tint.seed = seed * 104729 + 7;
            var pm = go.AddComponent<PathMover>(); pm.points = path; pm.closed = closed; pm.speed = speed; pm.offset = offset;
            return pm;
        }

        // ================================================================== layers above the street
        static List<Vector3> RoundedRect(float x0, float z0, float x1, float z1, float y, float r)
        {
            var pts = new List<Vector3>();
            Vector2[] cs = { new Vector2(x0 + r, z0 + r), new Vector2(x1 - r, z0 + r), new Vector2(x1 - r, z1 - r), new Vector2(x0 + r, z1 - r) };
            float[] start = { 180f, 270f, 0f, 90f };
            for (int i = 0; i < 4; i++)
                for (int k = 0; k <= 4; k++)
                {
                    float a = (start[i] + k * 22.5f) * Mathf.Deg2Rad;
                    pts.Add(new Vector3(cs[i].x + Mathf.Cos(a) * r, y, cs[i].y + Mathf.Sin(a) * r));
                }
            return pts;
        }

        static void Monorail(Transform root)
        {
            var g = Group(root, "Monorail");
            const float Y = 17f;
            var loop = RoundedRect(-70f, -70f, 80f, 80f, Y, 14f);
            float acc = 0f;
            for (int i = 0; i < loop.Count; i++)
            {
                Vector3 a = loop[i], b = loop[(i + 1) % loop.Count]; Vector3 mid = (a + b) * 0.5f; float len = Vector3.Distance(a, b);
                var beam = SBox(g, "Beam", mid + Vector3.down * 0.7f, new Vector3(1.8f, 1.2f, len + 0.3f), M["Concrete2"]);
                beam.transform.rotation = Quaternion.LookRotation(b - a);
                var glow = SBox(g, "BeamGlow", mid + Vector3.down * 1.32f, new Vector3(0.3f, 0.06f, len), M["Neon1"], false);
                glow.transform.rotation = Quaternion.LookRotation(b - a);
                for (float s = 0; s < len; s += 2f)
                {
                    acc += 2f; if (acc < 24f) continue; acc = 0f;
                    Vector3 p = Vector3.Lerp(a, b, s / len);
                    if (Mathf.Abs(p.x) < 46 && Mathf.Abs(p.z) < 46) continue;
                    // pillars stand on the curb (not in the road Pip runs down), a cantilever arm carries the beam
                    Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized); if (Vector3.Dot(side, new Vector3(5, 0, 5) - p) < 0) side = -side;
                    Vector3 foot = p + side * 6.3f; foot.y = 0f;
                    SBox(g, "Pillar", foot + Vector3.up * (Y - 1.3f) / 2f, new Vector3(1.0f, Y - 1.3f, 1.0f), M["Concrete2"]);
                    var arm = SBox(g, "PillarArm", (foot + new Vector3(p.x, 0, p.z)) * 0.5f + Vector3.up * (Y - 1.7f), new Vector3(0.8f, 0.8f, 6.8f), M["Concrete2"], false);
                    arm.transform.rotation = Quaternion.LookRotation(side);
                }
            }
            var cars = new List<Transform>();
            for (int k = 0; k < 4; k++)
            {
                var car = new GameObject("TrainCar"); car.transform.SetParent(g, false); car.transform.position = loop[0];   // model local = 0 (M16 bug: offset by loop[0])
                Place(car.transform, "MONO_Car", loop[0], 0f);
                cars.Add(car.transform);
            }
            RidableTrain(g, loop, cars);
        }

        static void Highway(Transform root, Vector3 a, Vector3 b, float y, string name, int carsPerLane)
        {
            var g = Group(root, name);
            Vector3 dir = (b - a).normalized; float len = Vector3.Distance(a, b); Vector3 mid = (a + b) * 0.5f + Vector3.up * y;
            var rot = Quaternion.LookRotation(dir); Vector3 side = Vector3.Cross(Vector3.up, dir);
            var deck = SBox(g, "Deck", mid, new Vector3(11f, 0.9f, len), M["Concrete2"]); deck.transform.rotation = rot;
            var road = Quad(g, "DeckRoad", mid + Vector3.up * 0.46f, new Vector2(10f, len), rot * Quaternion.Euler(90, 0, 0), M["RoadX"]); _static.Add(road);
            for (int s = -1; s <= 1; s += 2)
            {
                var wall = SBox(g, "Barrier", mid + side * s * 5.3f + Vector3.up * 0.95f, new Vector3(0.4f, 1.0f, len), M["Concrete2"]); wall.transform.rotation = rot;
                var neon = SBox(g, "BarrierNeon", mid + side * s * 5.52f + Vector3.up * 1.2f, new Vector3(0.06f, 0.12f, len), M["Neon" + (s < 0 ? 0 : 1)], false); neon.transform.rotation = rot;
            }
            for (float t = 12f; t < len; t += 26f)
            {
                Vector3 p = a + dir * t;
                if (new Vector2(p.x + 115, p.z).magnitude < 30f) continue;         // keep the ambush plaza clear
                if (Mathf.Abs(p.x) < 46 && Mathf.Abs(p.z) < 46) continue;
                for (int s = -1; s <= 1; s += 2) SBox(g, "Pillar", p + side * s * 4.6f + Vector3.up * (y - 0.45f) / 2f, new Vector3(1.6f, y - 0.45f, 1.6f), M["Concrete2"]);
                var cap = SBox(g, "Cap", p + Vector3.up * (y - 0.7f), new Vector3(9f, 0.6f, 2f), M["Concrete2"], false); cap.transform.rotation = rot;
            }
            int carN = 0;
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 a2 = a + side * s * 2.5f + Vector3.up * (y + 0.95f), b2 = b + side * s * 2.5f + Vector3.up * (y + 0.95f);
                if (s > 0) { var tmp = a2; a2 = b2; b2 = tmp; }
                for (int k = 0; k < carsPerLane; k++) Car(g, new List<Vector3> { a2, b2 }, false, Rf(16f, 24f), k * len / carsPerLane + Rf(0, 10f), 900 + carN++);
            }
        }

        static void Holograms(Transform root)
        {
            var g = Group(root, "Holograms"); var mat = HoloMat();
            (string model, Vector3 at, float scale, Color tint, float spin)[] holos =
            {
                ("SKY_Whale", new Vector3(118, 62, -70), 3.2f, new Color(0.3f, 0.95f, 1f, 0.45f), 8f),
                ("PED_Dreamer", new Vector3(0, 24, -118), 14f, new Color(1f, 0.35f, 0.8f, 0.5f), 18f),
                ("CAR_Hover", new Vector3(-118, 46, -64), 5f, new Color(1f, 0.85f, 0.3f, 0.45f), 25f),
                ("NPC_Clockfolk", new Vector3(-30, 40, 118), 12f, new Color(0.5f, 1f, 0.7f, 0.45f), -14f),
            };
            foreach (var h in holos)
            {
                var root2 = new GameObject("Hologram_" + h.model); root2.transform.SetParent(g, false); root2.transform.position = h.at;
                var m = Place(root2.transform, h.model, h.at, 0f, h.scale);
                if (m == null) continue;
                foreach (var r in m.GetComponentsInChildren<Renderer>()) { var mats = r.sharedMaterials; for (int i = 0; i < mats.Length; i++) mats[i] = mat; r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.Off; }
                var holo = root2.AddComponent<Hologram>(); holo.tint = h.tint; holo.spin = h.spin;
                // projector beam from below
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.DestroyImmediate(beam.GetComponent<Collider>());
                beam.name = "ProjectorBeam"; beam.transform.SetParent(g, false); beam.transform.position = new Vector3(h.at.x, h.at.y / 2f, h.at.z); beam.transform.localScale = new Vector3(1.4f, h.at.y / 2f, 1.4f);
                beam.GetComponent<MeshRenderer>().sharedMaterial = mat; beam.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // ================================================================== the edge: ring road on the bay + water + far skyline
        static void EdgeV2(Transform root)
        {
            var g = Group(root, "CityEdge");
            float E = Half + 16f;
            for (int side = 0; side < 4; side++)
            {
                var rot = Quaternion.Euler(0, side * 90f, 0); float yaw = side * 90f;
                var rr = Quad(g, "RingRoad", rot * new Vector3(0, 0.04f, Half + 8f), new Vector2(2f * E, 11f), rot * Quaternion.Euler(90, 0, 0), M["RoadX"]); _static.Add(rr);
                SBox(g, "SeaWall", rot * new Vector3(0, 0.55f, E - 1.2f), new Vector3(2f * E + 2f, 1.1f, 0.6f), M["Concrete2"], true, yaw);
                SBox(g, "SeaWallNeon", rot * new Vector3(0, 1.15f, E - 1.2f), new Vector3(2f * E + 2f, 0.1f, 0.65f), M["Neon1"], false, yaw);
                SBox(g, "Quay", rot * new Vector3(0, -3f, E + 1.5f), new Vector3(2f * E + 8f, 6f, 5f), M["Concrete2"], false, yaw);
                for (float u = -E + 8f; u < E; u += 20f) Place(g, "STREET_Lamp", rot * new Vector3(u, 0, E - 2.4f), yaw + 90f, 1.2f);
                var wall = new GameObject("EdgeWall"); wall.transform.SetParent(g, false);
                wall.transform.SetPositionAndRotation(rot * new Vector3(0, 190f, E - 0.5f), rot);
                wall.AddComponent<BoxCollider>().size = new Vector3(2f * E + 30f, 400f, 1f);
            }
            // ring traffic
            int carN = 500;
            var outer = new List<Vector3> { new Vector3(-Half - 10.5f, 0.55f, -Half - 10.5f), new Vector3(Half + 10.5f, 0.55f, -Half - 10.5f), new Vector3(Half + 10.5f, 0.55f, Half + 10.5f), new Vector3(-Half - 10.5f, 0.55f, Half + 10.5f) };
            var inner = new List<Vector3> { new Vector3(-Half - 5.5f, 0.55f, -Half - 5.5f), new Vector3(-Half - 5.5f, 0.55f, Half + 5.5f), new Vector3(Half + 5.5f, 0.55f, Half + 5.5f), new Vector3(Half + 5.5f, 0.55f, -Half - 5.5f) };
            for (int k = 0; k < 8; k++) { Car(g, outer, true, Rf(14f, 20f), k * 180f, carN++).yieldToPlayer = true; Car(g, inner, true, Rf(14f, 20f), k * 180f + 60f, carN++).yieldToPlayer = true; }
            // the bay
            var water = Quad(g, "Bay", new Vector3(0, -1.2f, 0), new Vector2(3000f, 3000f), Quaternion.Euler(90, 0, 0), M["Water"]); _static.Add(water);
            // the bay bridge: a lit suspension bridge running out to the far city (traffic on it, lights on the cables)
            {
                var br = Group(g, "BayBridge"); const float Y = 11f; float x0 = Half + 14f, x1 = 900f; float zc = -30f;
                SBox(br, "Abutment", new Vector3(Half + 24f, (Y - 1f) / 2f - 1f, zc), new Vector3(8f, Y + 1f, 18f), M["Concrete2"], false);
                SQuad(br, "AbutmentAd", new Vector3(Half + 19.9f, Y * 0.55f, zc), new Vector2(14f, 7f), Vector3.left, M["AdH3"]);
                SBox(br, "Deck", new Vector3((x0 + x1) / 2f, Y, zc), new Vector3(x1 - x0, 1.2f, 14f), M["Concrete2"], false);
                SBox(br, "DeckNeonN", new Vector3((x0 + x1) / 2f, Y - 0.7f, zc + 7.05f), new Vector3(x1 - x0, 0.15f, 0.1f), M["Neon1"], false);
                SBox(br, "DeckNeonS", new Vector3((x0 + x1) / 2f, Y - 0.7f, zc - 7.05f), new Vector3(x1 - x0, 0.15f, 0.1f), M["Neon0"], false);
                float[] towers = { 330f, 620f };
                foreach (float tx in towers)
                {
                    for (int s = -1; s <= 1; s += 2) SBox(br, "Tower", new Vector3(tx, 38f, zc + s * 7.5f), new Vector3(3f, 80f, 3f), M["M_Kit_MP_Red"], false);
                    SBox(br, "TowerBeam", new Vector3(tx, 74f, zc), new Vector3(3f, 3f, 18f), M["M_Kit_MP_Red"], false);
                    SBox(br, "TowerBeacon", new Vector3(tx, 79f, zc), Vector3.one * 1.6f, M["M_Kit_MP_LightRed"], false);
                }
                // main cables as chains of short lit segments (catenary between the towers and down to the deck ends)
                float[] xs = { x0, towers[0], towers[1], x1 };
                for (int seg = 0; seg < 3; seg++)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        float a0 = xs[seg], a1 = xs[seg + 1]; int n = 26;
                        for (int k = 0; k < n; k++)
                        {
                            float t0 = k / (float)n, t1 = (k + 1) / (float)n;
                            float Hgt(float t) { float lo = Y + 2f; float hiA = seg == 0 ? Y + 1f : 74f, hiB = seg == 2 ? Y + 1f : 74f; float lin = Mathf.Lerp(hiA, hiB, t); return Mathf.Max(lo, lin - Mathf.Sin(t * Mathf.PI) * (seg == 1 ? 58f : 20f)); }
                            Vector3 p0 = new Vector3(Mathf.Lerp(a0, a1, t0), Hgt(t0), zc + s * 7.5f), p1 = new Vector3(Mathf.Lerp(a0, a1, t1), Hgt(t1), zc + s * 7.5f);
                            var cab = SBox(br, "Cable", (p0 + p1) * 0.5f, new Vector3(0.35f, 0.35f, Vector3.Distance(p0, p1) + 0.1f), M["M_Kit_MP_LampGlow"], false);
                            cab.transform.rotation = Quaternion.LookRotation(p1 - p0);
                            if (k % 2 == 0) SBox(br, "Hanger", new Vector3(p0.x, (p0.y + Y) / 2f, p0.z), new Vector3(0.1f, Mathf.Max(0.2f, p0.y - Y), 0.1f), M["M_Kit_MP_Dark"], false);
                        }
                    }
                for (float x = x0 + 10f; x < 700f; x += 30f)
                    for (int s = -1; s <= 1; s += 2) { var pl = SBox(br, "Pier", new Vector3(x, (Y - 0.6f) / 2f - 1f, zc), new Vector3(4f, Y + 1f, 10f), M["Concrete2"], false); if (s > 0) Object.DestroyImmediate(pl); }
                int bc = 700;
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 a = new Vector3(x0, Y + 0.65f, zc + s * 3f), b = new Vector3(x1, Y + 0.65f, zc + s * 3f);
                    if (s > 0) { var t = a; a = b; b = t; }
                    for (int k = 0; k < 6; k++) Car(br, new List<Vector3> { a, b }, false, Rf(18f, 26f), k * 120f, bc++);
                }
                for (int i = 0; i < 24; i++)   // buoys / boat lights on the water
                {
                    var bo = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.DestroyImmediate(bo.GetComponent<Collider>()); bo.name = "BayLight";
                    bo.transform.SetParent(g, false); float a = Rf(0, Mathf.PI * 2f), rr = Rf(Half + 40f, 420f);
                    bo.transform.position = new Vector3(Mathf.Sin(a) * rr, -0.9f, Mathf.Cos(a) * rr); bo.transform.localScale = Vector3.one * 0.9f;
                    bo.GetComponent<MeshRenderer>().sharedMaterial = R.NextDouble() < 0.5 ? M["M_Kit_MP_LightRed"] : M["M_Kit_MP_LampGlow"]; _static.Add(bo);
                }
            }
            // distant skyline: varied hazy giants with lit windows, ad screens and blinking tops (unreachable)
            var sky = Group(g, "FarSkyline");
            string[] facs = { "Glass", "Concrete", "Industrial", "Panel0", "Panel2", "Panel3", "Balcony" };
            // far districts: dense clusters across the water (the bridge leads to the east one), open horizon between them
            Vector3[] clusters = { new Vector3(840, 0, -30), new Vector3(-620, 0, 520), new Vector3(260, 0, -820), new Vector3(-780, 0, -380), new Vector3(560, 0, 680), new Vector3(-120, 0, 900) };
            foreach (var cc in clusters)
            for (int i = 0; i < 13; i++)
            {
                Vector3 c = cc + new Vector3(Rf(-130f, 130f), 0, Rf(-130f, 130f));
                if (Mathf.Abs(c.z + 30f) < 12f && c.x < 900f) continue;      // keep the bridge deck clear
                float w = Rf(30f, 70f), d = Rf(28f, 60f), h = Rf(70f, 330f) * (i < 2 ? 1.3f : 1f);
                int tiers = R.Next(1, 4); float y = -2f; float tw = w, td = d; float yaw = Rf(0, 90);
                for (int t = 0; t < tiers; t++)
                {
                    float th = t == tiers - 1 ? h - y : (h / tiers) * Rf(0.8f, 1.2f);
                    SBox(sky, "Giant", c + Vector3.up * (y + th / 2f), new Vector3(tw, th, td), M[facs[R.Next(facs.Length)]], false, yaw);
                    y += th; tw *= Rf(0.6f, 0.85f); td *= Rf(0.6f, 0.85f);
                }
                if (R.NextDouble() < 0.4)
                {
                    Vector3 n = -c.normalized; float len = Mathf.Min(h * 0.5f, 90f);
                    SQuad(sky, "GiantAd", c + n * (Mathf.Max(w, d) * 0.75f) + Vector3.up * (h * 0.55f), new Vector2(len / 4f, len), n, M["AdV" + R.Next(8)]);
                }
                SBox(sky, "Beacon", c + Vector3.up * (h + 1f), Vector3.one * 1.5f, M["M_Kit_MP_LightRed"], false);
            }
        }

        /// <summary>
        /// M20 layout clean-up (user: "trees in the road, bugs everywhere"): after everything is placed, delete street props
        /// that ended up on a road surface or inside a building / wall, and pedestrians standing in the road.
        /// </summary>
        static void Tidy(Transform root)
        {
            Physics.SyncTransforms();
            string[] props = { "PALM_Tree", "STREET_Lamp", "BENCH_Bin", "KIOSK_Food", "ST_" };
            int road = 0, inside = 0;
            foreach (var t in new List<Transform>(root.GetComponentsInChildren<Transform>(true)))
            {
                if (t == null) continue;
                bool prop = false; foreach (var n in props) if (t.name.StartsWith(n)) { prop = true; break; }
                bool ped = t.GetComponent<DreamRush.Palace.PaperPerson>() != null && t.GetComponent<DreamRush.Palace.PathMover>() == null;
                if (!prop && !ped) continue;
                Vector3 p = t.position;
                if (p.y > 1.5f) continue;   // rooftop / elevated decor
                if (OnRoad(p, prop && t.name.StartsWith("KIOSK") ? 0.8f : -0.3f)) { Object.DestroyImmediate(t.gameObject); road++; continue; }
                if (prop && InsideSolid(p, t)) { Object.DestroyImmediate(t.gameObject); inside++; }
            }
            Debug.Log($"[DreamRush] Palace tidy: removed {road} props/people on roads, {inside} inside buildings");
        }

        static bool OnRoad(Vector3 p, float margin)
        {
            if (Mathf.Abs(p.x) > Half || Mathf.Abs(p.z) > Half) return false;
            if (Mathf.Abs(p.x) < 46f && Mathf.Abs(p.z) < 46f) return false;               // tower plaza paves over the grid
            if (new Vector2(p.x + 115f, p.z).magnitude < 30f) return false;                 // fountain plaza
            if (InCanal(p)) return false;
            foreach (float L in StreetLines)
                if (Mathf.Abs(p.x - L) < Street / 2f + margin || Mathf.Abs(p.z - L) < Street / 2f + margin) return true;
            return false;
        }

        static bool InsideSolid(Vector3 p, Transform self)
        {
            foreach (var c in Physics.OverlapBox(p + Vector3.up * 1.4f, new Vector3(0.35f, 1f, 0.35f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(self)) continue;
                if (c.bounds.size.y < 2.5f) continue;   // curbs, benches, low planters
                return true;
            }
            return false;
        }

        static void MarkStatic()
        {
            foreach (var go in _static)
                if (go != null) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            _static.Clear();
        }
    }
}
