using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Owner: new house-repair co-op jobs (replace the removed unicorn / dance game). Rebuilds only its own
    /// "Coop puzzles" root in MamasKitchen (and drops the old "Coop games" root). Safe to run again.
    ///  - Game room: bookshelf fell over (one holds it up, one screws it in)
    ///  - Router (game room) + TV (living room): WiFi code - one reads, one enters
    public static class MamaCoopPuzzles
    {
        const string Kitchen = "Assets/MamasComing/Scenes/MamasKitchen.unity";
        const string Dir = "Assets/MamasComing/Art/CoopPuzzles/";

        [MenuItem("Mama's Coming/Build co-op puzzles")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[CoopPuzzles] stop Play mode first"); return; }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != Kitchen) scene = EditorSceneManager.OpenScene(Kitchen, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects()) if (go.name == "Coop puzzles" || go.name == "Coop games") Object.DestroyImmediate(go);
            System.IO.Directory.CreateDirectory(Dir);
            var root = new GameObject("Coop puzzles").transform;
            Shelf(root); Wifi(root);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[CoopPuzzles] built");
        }

        // ------------------------------------------------------------------ game room bookshelf
        static void Shelf(Transform root)
        {
            var go = new GameObject("Fallen bookshelf"); go.transform.SetParent(root, false); go.transform.position = new Vector3(22.55f, 0f, 9.2f);
            var hz = go.AddComponent<ShelfHazard>(); hz.displayName = "Bookshelf"; hz.interactRadius = .1f; hz.severity = 2; hz.escalateTime = 150f; hz.appearAt = 120f;
            var wood = Mat("Shelf wood", new Color(.62f, .42f, .28f), .3f);
            var dark = Mat("Shelf back", new Color(.42f, .27f, .18f), .25f);
            Color[] bookCols = { new Color(.9f, .35f, .38f), new Color(.3f, .62f, .85f), new Color(.98f, .8f, .3f), new Color(.45f, .75f, .5f), new Color(.75f, .5f, .85f) };
            var pivot = new GameObject("Shelf pivot").transform; pivot.SetParent(go.transform, false);   // bottom front edge, tips toward +x
            var body = new GameObject("Shelf").transform; body.SetParent(pivot, false); body.localPosition = new Vector3(-.2f, 0, 0);
            Box("Back", body, dark, new Vector3(-.17f, .9f, 0), new Vector3(.04f, 1.8f, 1.4f));
            Box("Side L", body, wood, new Vector3(0, .9f, -.68f), new Vector3(.4f, 1.8f, .05f));
            Box("Side R", body, wood, new Vector3(0, .9f, .68f), new Vector3(.4f, 1.8f, .05f));
            for (int i = 0; i < 5; i++) Box("Board " + i, body, wood, new Vector3(0, .04f + i * .43f, 0), new Vector3(.4f, .05f, 1.4f));
            int n = 0;
            for (int row = 0; row < 4; row++)
            {
                float z = -.6f;
                while (z < .55f)
                {
                    float w = Random.Range(.06f, .12f), h = Random.Range(.24f, .36f);
                    Box("Book", body, Mat("Book " + (n % bookCols.Length), bookCols[n % bookCols.Length], .35f), new Vector3(-.02f, .07f + row * .43f + h * .5f, z + w * .5f), new Vector3(.28f, h, w));
                    z += w + .012f; n++;
                }
            }
            // a few books on the floor
            for (int i = 0; i < 5; i++)
            {
                var b = Box("Dropped book", go.transform, Mat("Book " + (i % bookCols.Length), bookCols[i % bookCols.Length], .35f), new Vector3(.9f + Random.Range(0f, .9f), .03f, Random.Range(-.8f, .8f)), new Vector3(.24f, .05f, .32f));
                b.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 180), 0);
                hz.showWhenActive = Append(hz.showWhenActive, b);
            }
            var hold = Station("Bookshelf - hold it up", go.transform, new Vector3(1.05f, 0, -.55f), hz, 0);
            var screw = Station("Bookshelf - screw it in", go.transform, new Vector3(.75f, 0, 1.25f), hz, 1);
            hz.stations = new[] { hold, screw };
            hz.shelfPivot = pivot;
        }

        // ------------------------------------------------------------------ WiFi: router (game room) + TV (living room)
        static void Wifi(Transform root)
        {
            var go = new GameObject("WiFi router"); go.transform.SetParent(root, false); go.transform.position = new Vector3(23.9f, 0f, 13.5f);
            var hz = go.AddComponent<WifiHazard>(); hz.displayName = "WiFi"; hz.interactRadius = .1f; hz.severity = 2; hz.escalateTime = 160f; hz.appearAt = 200f;
            var white = Mat("Router stand", new Color(.95f, .93f, .9f), .4f);
            var black = Mat("Router body", new Color(.12f, .13f, .16f), .6f);
            var led = Mat("Router led", new Color(1f, .35f, .3f), .5f, 0f, new Color(1f, .2f, .15f) * 2f);
            Box("Stand", go.transform, white, new Vector3(0, .35f, 0), new Vector3(.5f, .7f, .4f));
            Box("Router", go.transform, black, new Vector3(0, .75f, 0), new Vector3(.42f, .09f, .28f));
            Box("Antenna L", go.transform, black, new Vector3(-.16f, .95f, .1f), new Vector3(.025f, .32f, .025f));
            Box("Antenna R", go.transform, black, new Vector3(.16f, .95f, .1f), new Vector3(.025f, .32f, .025f));
            for (int i = 0; i < 4; i++) Box("Led", go.transform, led, new Vector3(-.12f + i * .08f, .76f, -.145f), new Vector3(.03f, .02f, .01f));
            Box("Code sticker", go.transform, Mat("Sticker", Color.white, .2f), new Vector3(0, .5f, -.205f), new Vector3(.3f, .16f, .005f));
            var read = Station("Router - WiFi code", go.transform, new Vector3(0, 0, -.85f), hz, 0);

            // the TV end lives in the living room (in front of the TV); a small receiver box shows the colour
            var tv = new GameObject("WiFi TV input"); tv.transform.SetParent(go.transform, false); tv.transform.position = new Vector3(1.0f, 0f, 2.6f);
            Box("Receiver", tv.transform, black, new Vector3(0, .25f, 0), new Vector3(.36f, .5f, .36f));
            var scr = new GameObject("Receiver screen", typeof(MeshRenderer), typeof(TextMesh)); scr.transform.SetParent(tv.transform, false);
            scr.transform.localPosition = new Vector3(.19f, .55f, 0); scr.transform.localRotation = Quaternion.Euler(0, -90, 0);
            var tm = scr.GetComponent<TextMesh>(); tm.font = MamaTheme.Bold; scr.GetComponent<MeshRenderer>().sharedMaterial = MamaTheme.Bold.material;
            tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center; tm.fontSize = 60; tm.characterSize = .02f; tm.text = "";
            hz.tvScreen = tm;
            var enter = Station("TV - enter the WiFi code", tv.transform, new Vector3(.75f, 0, 0), hz, 1);
            hz.stations = new[] { read, enter };
        }

        // ------------------------------------------------------------------ helpers
        static CoopStation Station(string name, Transform parent, Vector3 local, CoopPuzzle hz, int index)
        {
            var s = new GameObject(name).transform; s.SetParent(parent, false); s.localPosition = local;
            var st = s.gameObject.AddComponent<CoopStation>(); st.displayName = name; st.interactRadius = .85f; st.puzzle = hz; st.index = index;
            var ring = Box("Floor mark", s, Mat("Spot mark", new Color(1f, .55f, .8f), .2f, 0f, new Color(1f, .4f, .7f) * .6f), new Vector3(0, .012f, 0), new Vector3(.75f, .01f, .75f));
            hz.showWhenActive = Append(hz.showWhenActive, ring);
            return st;
        }
        static GameObject[] Append(GameObject[] a, GameObject g) { var l = new List<GameObject>(a ?? new GameObject[0]); l.Add(g); return l.ToArray(); }
        static GameObject Box(string name, Transform parent, Material m, Vector3 pos, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = size;
            g.GetComponent<MeshRenderer>().sharedMaterial = m; return g;
        }
        static Material Mat(string name, Color c, float smooth, float metal = 0f, Color? emission = null)
        {
            string path = Dir + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
            EditorUtility.SetDirty(m); return m;
        }
    }
}
