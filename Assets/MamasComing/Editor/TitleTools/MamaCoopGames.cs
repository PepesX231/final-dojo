using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Adds the co-op-only party problems to MamasKitchen (replaces only its own "Coop games" root):
    ///  - Garage: giant inflatable unicorn on Mom's parking spot (squeeze both sides together)
    ///  - Game room: frozen dance game (both pads, E on the beat together)
    public static class MamaCoopGames
    {
        const string Kitchen = "Assets/MamasComing/Scenes/MamasKitchen.unity";
        const string Dir = "Assets/MamasComing/Art/CoopGames/";

        /// Owner: the unicorn + dance-off were not fun -> taken out of the house (code kept dormant).
        [MenuItem("Mama's Coming/Remove co-op games")]
        public static void Remove()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            var scene = EditorSceneManager.OpenScene(Kitchen, OpenSceneMode.Single);
            int n = 0;
            foreach (var go in scene.GetRootGameObjects()) if (go.name == "Coop games") { Object.DestroyImmediate(go); n++; }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[CoopGames] removed " + n);
        }

        [MenuItem("Mama's Coming/Rebuild co-op games (old)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[CoopGames] stop Play mode first"); return; }
            var scene = EditorSceneManager.OpenScene(Kitchen, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects()) if (go.name == "Coop games") Object.DestroyImmediate(go);
            Directory.CreateDirectory(Dir);
            meshN = 0; var root = new GameObject("Coop games").transform;
            Unicorn(root); Dance(root);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[CoopGames] built");
        }

        // ------------------------------------------------------------------ garage unicorn
        static void Unicorn(Transform root)
        {
            var go = new GameObject("Giant unicorn"); go.transform.SetParent(root, false); go.transform.position = new Vector3(26f, 0f, 2.5f); go.transform.rotation = Quaternion.Euler(0, 20, 0);
            var hz = go.AddComponent<UnicornHazard>(); hz.displayName = "Giant unicorn"; hz.interactRadius = .1f; hz.severity = 2; hz.escalateTime = 140f; hz.appearAt = 110f;
            var white = Mat("Unicorn vinyl", new Color(1f, .96f, .98f), .75f); var pink = Mat("Unicorn pink", new Color(1f, .62f, .82f), .7f); var gold = Mat("Unicorn horn", new Color(1f, .8f, .3f), .8f, .6f);
            var blue = Mat("Unicorn mane blue", new Color(.55f, .82f, 1f), .7f); var lilac = Mat("Unicorn mane lilac", new Color(.8f, .65f, 1f), .7f); var ink = Mat("Unicorn eye", new Color(.12f, .08f, .1f), .9f);
            var body = new GameObject("Body").transform; body.SetParent(go.transform, false); body.localPosition = new Vector3(0, .42f, 0);
            Part("Ring", body, MamaRankingStage.Torus(.95f, .42f, 48, 20), white, Vector3.zero, Quaternion.identity, Vector3.one);
            for (int i = 0; i < 6; i++) { float a = i / 6f * Mathf.PI * 2 + .4f; Part("Spot", body, MamaRankingStage.Sphere(16, 10), pink, new Vector3(Mathf.Cos(a) * .95f, .28f, Mathf.Sin(a) * .95f), Quaternion.identity, new Vector3(.32f, .12f, .32f)); }
            var head = new GameObject("Head").transform; head.SetParent(go.transform, false); head.localPosition = new Vector3(1.05f, .55f, 0);
            Part("Neck", head, MamaRankingStage.Lathe(new List<Vector2> { new Vector2(0, 0), new Vector2(.34f, 0), new Vector2(.3f, .8f), new Vector2(.26f, 1.1f), new Vector2(0, 1.15f) }, 32), white, Vector3.zero, Quaternion.Euler(0, 0, -12), Vector3.one);
            Part("Skull", head, MamaRankingStage.Sphere(32, 20), white, new Vector3(.2f, 1.2f, 0), Quaternion.identity, new Vector3(.9f, .72f, .7f));
            Part("Snout", head, MamaRankingStage.Sphere(24, 16), pink, new Vector3(.58f, 1.1f, 0), Quaternion.identity, new Vector3(.42f, .36f, .44f));
            Part("Horn", head, MamaRankingStage.Lathe(new List<Vector2> { new Vector2(.09f, 0), new Vector2(0, .5f) }, 20), gold, new Vector3(.25f, 1.52f, 0), Quaternion.Euler(0, 0, -20), Vector3.one);
            Part("Eye L", head, MamaRankingStage.Sphere(16, 10), ink, new Vector3(.4f, 1.3f, .22f), Quaternion.identity, Vector3.one * .1f);
            Part("Eye R", head, MamaRankingStage.Sphere(16, 10), ink, new Vector3(.4f, 1.3f, -.22f), Quaternion.identity, Vector3.one * .1f);
            for (int i = 0; i < 6; i++) Part("Mane", head, MamaRankingStage.Sphere(16, 10), i % 2 == 0 ? blue : lilac, new Vector3(-.08f - i * .05f, 1.45f - i * .16f, 0), Quaternion.identity, Vector3.one * (.24f - i * .015f));
            hz.body = body; hz.head = head;
            // two glowing "sit here" spots on opposite sides of the ring (no valves: it's a pool float you squash flat)
            var spot = Mat("Unicorn sit spot", new Color(1f, .85f, .95f), .2f, 0f, new Color(1f, .45f, .8f) * 1.2f);
            var st = new List<CoopStation>();
            for (int i = 0; i < 2; i++)
            {
                var s = new GameObject(i == 0 ? "Sit spot A" : "Sit spot B"); s.transform.SetParent(go.transform, false); s.transform.localPosition = new Vector3(-.15f, .05f, i == 0 ? 1.75f : -1.75f);
                Part("Ring", s.transform, MamaRankingStage.Torus(.32f, .035f, 32, 8), spot, Vector3.zero, Quaternion.identity, Vector3.one);
                Part("Butt arrow", s.transform, MamaRankingStage.Lathe(new List<Vector2> { new Vector2(.12f, .55f), new Vector2(0f, .3f) }, 4), spot, Vector3.zero, Quaternion.identity, Vector3.one);
                var cs = s.AddComponent<CoopStation>(); cs.displayName = "Pool unicorn"; cs.interactRadius = 1.2f; st.Add(cs);
            }
            hz.stations = st.ToArray();
            var col = go.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, .45f, 0); col.radius = 1.3f; col.height = .9f; col.direction = 0;
        }

        // ------------------------------------------------------------------ game room dance-off
        static void Dance(Transform root)
        {
            var go = new GameObject("Frozen dance game"); go.transform.SetParent(root, false); go.transform.position = new Vector3(23.1f, 0f, 11f);
            var hz = go.AddComponent<DanceOffHazard>(); hz.displayName = "Dance game"; hz.interactRadius = .1f; hz.severity = 2; hz.escalateTime = 140f; hz.appearAt = 170f;
            var st = new List<CoopStation>(); var glow = new List<Renderer>();
            for (int i = 0; i < 2; i++)
            {
                var pad = GameObject.CreatePrimitive(PrimitiveType.Cube); pad.name = i == 0 ? "Dance pad A" : "Dance pad B"; pad.transform.SetParent(go.transform, false);
                pad.transform.localPosition = new Vector3(.95f, .03f, i == 0 ? -.62f : .62f); pad.transform.localScale = new Vector3(.95f, .06f, .95f);
                Object.DestroyImmediate(pad.GetComponent<Collider>());
                var m = Mat("Dance pad " + (i == 0 ? "A" : "B"), new Color(.25f, .2f, .35f), .5f, 0f, new Color(.2f, .1f, .2f)); pad.GetComponent<Renderer>().sharedMaterial = m; glow.Add(pad.GetComponent<Renderer>());
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube); arrow.name = "Arrow"; arrow.transform.SetParent(pad.transform, false); arrow.transform.localPosition = new Vector3(0, .6f, 0); arrow.transform.localScale = new Vector3(.5f, .2f, .14f); arrow.transform.localRotation = Quaternion.Euler(0, 45, 0);
                Object.DestroyImmediate(arrow.GetComponent<Collider>()); arrow.GetComponent<Renderer>().sharedMaterial = Mat("Dance arrow", new Color(1f, .95f, .85f), .3f, 0f, new Color(.6f, .55f, .5f));
                var cs = pad.AddComponent<CoopStation>(); cs.displayName = "Dance pad"; cs.interactRadius = .95f; st.Add(cs);
            }
            hz.stations = st.ToArray(); hz.padGlow = glow.ToArray();
            var lgo = new GameObject("Beat light", typeof(Light)); lgo.transform.SetParent(go.transform, false); lgo.transform.localPosition = new Vector3(.6f, 2.3f, 0);
            var l = lgo.GetComponent<Light>(); l.type = LightType.Point; l.range = 5f; l.intensity = 0f; l.enabled = false; hz.beatLight = l;
            var tgo = new GameObject("Screen text", typeof(MeshRenderer), typeof(TextMesh)); tgo.transform.SetParent(go.transform, false);
            tgo.transform.localPosition = new Vector3(-.75f, 1.35f, 0); tgo.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var tm = tgo.GetComponent<TextMesh>(); tm.font = MamaTheme.Display; tm.fontSize = 90; tm.characterSize = .03f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = new Color(1f, .9f, .4f); tm.text = "";
            if (tm.font) tgo.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            hz.screenText = tm;
        }

        // ------------------------------------------------------------------ helpers
        static Material Mat(string name, Color c, float smooth, float metal = 0f, Color? emission = null)
        {
            string path = Dir + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); }
            EditorUtility.SetDirty(m); return m;
        }
        static int meshN;
        static GameObject Part(string name, Transform parent, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = scale;
            string path = Dir + "Mesh_" + parent.name.Replace(" ", "") + "_" + name.Replace(" ", "") + "_" + (meshN++) + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = mat; return go;
        }
    }
}
