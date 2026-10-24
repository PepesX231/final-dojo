using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.Editor
{
    /// Restages MC_Ranking as a bright "party podium" results set: cyclorama, tiered poufs, sun-burst, bubbles,
    /// confetti, cheering fruits, warm three-point lighting and its own post-processing profile.
    /// Keeps the scene's "Results local player" and "Ranking flow" objects. Safe to run again (it replaces only its own root).
    public static class MamaRankingStage
    {
        const string Scene = "Assets/MamasComing/Scenes/MC_Ranking.unity";
        const string Dir = "Assets/MamasComing/FrontEnd/Results/";
        const string Root = "Ranking stage (party)";
        static Vector3[] Podium => MamaRankingScreen.Podium;
        static readonly float[] PodiumR = { 1.25f, 1.0f, 1.0f };

        [MenuItem("Mama's Coming/Rebuild Ranking stage")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[RankingStage] stop Play mode first"); return; }
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
            {
                string n = go.name;
                if (n == Root || n == "After-party award corner" || n == "Warm ceiling light" || n == "Soft fill" || n == "Main Camera" || n == "Ranking volume" || n == "Ranking lights")
                    Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory(Dir);
            var root = new GameObject(Root).transform;

            // ------------------------------------------------------------ materials
            var wall = Mat("RK Cyclorama peach", new Color(.86f, .58f, .47f), .12f);
            var burst = Mat("RK Burst light", new Color(.93f, .68f, .55f), .05f, 0f, false, new Color(.12f, .07f, .04f));
            var gold = Mat("RK Pouf mustard", new Color(.93f, .7f, .3f), .3f);
            var teal = Mat("RK Pouf teal", new Color(.27f, .55f, .54f), .3f);
            var coral = Mat("RK Pouf coral", new Color(.84f, .43f, .4f), .3f);
            var cream = Mat("RK Piping cream", new Color(1f, .95f, .84f), .45f);
            var ink = Mat("RK Ink brown", new Color(.36f, .2f, .12f), .2f);
            var bubble = Mat("RK Soap bubble", new Color(.75f, .93f, 1f, .28f), .95f, 0f, true, new Color(.05f, .1f, .14f));
            var rug = Mat("RK Rug cream", new Color(.9f, .8f, .68f), .05f);

            // ------------------------------------------------------------ set: seamless cyclorama + floor collider
            var cyc = Obj("Cyclorama", root, Cyclorama(), wall);
            var floor = new GameObject("Floor collider", typeof(BoxCollider)); floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0, -.25f, -1f); floor.transform.localScale = new Vector3(30, .5f, 16);
            var back = new GameObject("Back wall collider", typeof(BoxCollider)); back.transform.SetParent(root, false);
            back.transform.localPosition = new Vector3(0, 3, 3.6f); back.transform.localScale = new Vector3(30, 6, .5f);
            var burstGo = Obj("Sun burst", root, Burst(18, 9.5f), burst); burstGo.transform.localPosition = new Vector3(0, 3.6f, 4.55f); burstGo.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            var spin = burstGo.AddComponent<MamaFloaty>(); spin.bob = 0; spin.sway = 0; spin.spin = 4f;
            var rugGo = Obj("Round rug", root, Lathe(new List<Vector2> { new Vector2(0, .02f), new Vector2(4.6f, .02f), new Vector2(4.7f, 0f) }, 64), rug);
            rugGo.transform.localPosition = new Vector3(0, 0, .2f); rugGo.transform.localScale = new Vector3(1.25f, 1, .75f);

            // ------------------------------------------------------------ podium poufs (1 centre, 2 left, 3 right)
            Material[] mats = { gold, teal, coral };
            for (int i = 0; i < 3; i++)
            {
                float h = Podium[i].y, r = PodiumR[i];
                var p = Obj("Pouf " + (i + 1), root, Pouf(r, h, .14f), mats[i]);
                p.transform.localPosition = new Vector3(Podium[i].x, 0, Podium[i].z);
                var mc = p.AddComponent<MeshCollider>(); mc.sharedMesh = p.GetComponent<MeshFilter>().sharedMesh; mc.convex = true;
                var pipe = Obj("Piping", p.transform, Torus(r - .06f, .045f, 64, 10), cream); pipe.transform.localPosition = new Vector3(0, h - .07f, 0);
                var pipe2 = Obj("Piping low", p.transform, Torus(r - .02f, .04f, 64, 10), cream); pipe2.transform.localPosition = new Vector3(0, .09f, 0);
                // buttons on top (tufted cushion)
                for (int k = 0; k < 5; k++)
                {
                    float a = k / 5f * Mathf.PI * 2 + .3f; var b = Obj("Tuft", p.transform, Lathe(new List<Vector2> { new Vector2(0, .025f), new Vector2(.06f, .015f), new Vector2(.07f, 0) }, 16), cream);
                    b.transform.localPosition = new Vector3(Mathf.Cos(a) * r * .55f, h, Mathf.Sin(a) * r * .55f);
                }
                Number(p.transform, (i + 1).ToString(), new Vector3(0, h * .5f + .02f, -r - .01f), i == 0 ? .75f : .6f);
            }

            // ------------------------------------------------------------ floating soap bubbles + confetti
            var bubbles = new GameObject("Soap bubbles").transform; bubbles.SetParent(root, false);
            Vector3[] bp = { new Vector3(-5.2f, 2.6f, 2.2f), new Vector3(-3.6f, 4.1f, 3.2f), new Vector3(4.4f, 3.3f, 2.6f), new Vector3(5.6f, 1.6f, 1.4f), new Vector3(2.9f, 4.7f, 3.6f), new Vector3(-6.2f, 1.2f, 1f), new Vector3(6.6f, 4.4f, 3.4f) };
            float[] bs = { .7f, .45f, .6f, .35f, .5f, .3f, .55f };
            for (int i = 0; i < bp.Length; i++)
            {
                var s = Obj("Bubble", bubbles, Sphere(24, 16), bubble); s.transform.localPosition = bp[i]; s.transform.localScale = Vector3.one * bs[i] * 2;
                var f = s.AddComponent<MamaFloaty>(); f.bob = .18f; f.bobSpeed = .25f + i * .03f; f.sway = 0; f.spin = 10;
            }
            Confetti(root);

            // ------------------------------------------------------------ cheering fruits (hidden at runtime if a player uses that fruit)
            var crowd = new GameObject("Cheering fruits").transform; crowd.SetParent(root, false);
            Vector3[] cp = { new Vector3(-4.3f, 0, 1.5f), new Vector3(4.3f, 0, 1.6f), new Vector3(-5.6f, 0, 2.5f), new Vector3(5.6f, 0, 2.6f), new Vector3(-6.6f, 0, 3.4f) };
            MamaPosedKid.Pose[] cpose = { MamaPosedKid.Pose.Wave, MamaPosedKid.Pose.Hop, MamaPosedKid.Pose.FingerGuns, MamaPosedKid.Pose.Flex, MamaPosedKid.Pose.HandsOnHips };
            int[] order = { 1, 2, 3, 4, 0 };
            for (int i = 0; i < 5; i++)
            {
                var k = new GameObject("Cheer " + KidLook.Names[order[i]]); k.transform.SetParent(crowd, false);
                k.transform.localPosition = cp[i]; k.transform.localRotation = Quaternion.Euler(0, 180 + (cp[i].x > 0 ? 18 : cp[i].x < 0 ? -18 : 0), 0);
                var pk = k.AddComponent<MamaPosedKid>(); pk.kid = order[i]; pk.pose = cpose[i]; pk.phase = i * .37f; pk.energy = 1.2f; pk.normalizeHeight = 1.55f; pk.fitToSeat = true; pk.backClear = 5f;
                pk.Rebuild();
            }

            // ------------------------------------------------------------ lights
            var lights = new GameObject("Ranking lights").transform;
            var key = L("Key (warm sun)", lights, LightType.Directional, new Color(1f, .88f, .76f), .95f, Quaternion.Euler(42, -28, 0)); key.shadows = LightShadows.Soft; key.shadowStrength = .65f;
            var rim = L("Rim (pink back light)", lights, LightType.Directional, new Color(1f, .62f, .75f), .7f, Quaternion.Euler(20, 160, 0));
            // stage beams: soft additive cones from above onto each podium (concert look)
            var beamMat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "RK Beam.mat");
            if (!beamMat) { beamMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(beamMat, Dir + "RK Beam.mat"); }
            beamMat.SetFloat("_Surface", 1); beamMat.SetFloat("_Blend", 2); beamMat.SetFloat("_Cull", 0); beamMat.SetFloat("_ZWrite", 0);
            beamMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); beamMat.SetFloat("_DstBlend", (float)BlendMode.One);
            beamMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); beamMat.renderQueue = (int)RenderQueue.Transparent + 10; beamMat.SetOverrideTag("RenderType", "Transparent");
            beamMat.SetColor("_BaseColor", new Color(1f, .86f, .66f, .07f)); EditorUtility.SetDirty(beamMat);
            for (int i = 0; i < 3; i++)
            {
                Vector3 target = new Vector3(Podium[i].x, Podium[i].y, Podium[i].z);
                Vector3 apex = new Vector3(Podium[i].x * 1.6f, 8.5f, Podium[i].z - 2.5f);
                var cone = Obj("Beam " + (i + 1), root, Lathe(new List<Vector2> { new Vector2(.12f, 0f), new Vector2(i == 0 ? 1.35f : 1.05f, -1f) }, 40), beamMat);
                cone.transform.position = apex; cone.transform.rotation = Quaternion.FromToRotation(Vector3.down, (target - apex).normalized);
                cone.transform.localScale = new Vector3(1, (target - apex).magnitude, 1);
                cone.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var sp = L("Beam light " + (i + 1), lights, LightType.Spot, new Color(1f, .9f, .75f), i == 0 ? 16f : 9f, Quaternion.LookRotation(target - apex));
                sp.transform.position = apex; sp.range = 14; sp.spotAngle = i == 0 ? 26 : 22; sp.innerSpotAngle = 10; sp.shadows = i == 0 ? LightShadows.Soft : LightShadows.None;
            }
            var fill = L("Fill (sky bounce)", lights, LightType.Point, new Color(.75f, .88f, 1f), 3.5f, Quaternion.identity); fill.transform.position = new Vector3(-4, 3, -4); fill.range = 14;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.86f, .78f, .8f) * .9f;
            RenderSettings.ambientEquatorColor = new Color(.85f, .6f, .52f) * .8f;
            RenderSettings.ambientGroundColor = new Color(.55f, .32f, .26f) * .9f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = new Color(.8f, .55f, .47f); RenderSettings.fogStartDistance = 14; RenderSettings.fogEndDistance = 40;
            RenderSettings.skybox = null;

            // ------------------------------------------------------------ camera + post
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>(); cam.fieldOfView = 33; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.7f, .45f, .38f);
            cam.transform.position = new Vector3(-2.35f, 2.35f, -9.6f); cam.transform.LookAt(new Vector3(-1.45f, 1.45f, 0.4f));
            cam.nearClipPlane = .1f; cam.farClipPlane = 60;
            var data = camGo.GetComponent<UniversalAdditionalCameraData>(); if (!data) data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; data.renderShadows = true;
            var volGo = new GameObject("Ranking volume", typeof(Volume)); var vol = volGo.GetComponent<Volume>(); vol.isGlobal = true; vol.priority = 5; vol.sharedProfile = Profile();

            // portraits for the scoreboard rows
            Portraits(cam);

            var flow = Object.FindFirstObjectByType<MamaRankingScreen>();
            if (flow) { flow.crowd = crowd; EditorUtility.SetDirty(flow); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[RankingStage] rebuilt " + Scene);
        }

        // ================================================================== helpers
        static Material Mat(string name, Color c, float smooth, float metal = 0f, bool transparent = false, Color? emission = null)
        {
            string path = Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", metal);
            if (transparent)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_ZWrite", 0); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
            }
            if (emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            EditorUtility.SetDirty(m); return m;
        }

        static GameObject Obj(string name, Transform parent, Mesh mesh, Material mat)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false);
            string path = Dir + "Meshes/" + parent.name + " - " + name + ".asset";
            Directory.CreateDirectory(Dir + "Meshes");
            path = path.Replace("•", "").Replace("(", "").Replace(")", "");
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = mat; return go;
        }

        static Light L(string name, Transform parent, LightType type, Color c, float intensity, Quaternion rot)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.SetParent(parent, false); go.transform.rotation = rot;
            var l = go.GetComponent<Light>(); l.type = type; l.color = c; l.intensity = intensity; return l;
        }

        static void Number(Transform parent, string s, Vector3 pos, float size)
        {
            var font = MamaTheme.Display; if (!font) return;
            for (int layer = 0; layer < 3; layer++)
            {
                var go = new GameObject(layer == 0 ? "Number " + s : "Number depth", typeof(MeshRenderer), typeof(TextMesh)); go.transform.SetParent(parent, false);
                go.transform.localPosition = pos + new Vector3(layer * .012f, -layer * .012f, layer * .015f);
                var tm = go.GetComponent<TextMesh>(); tm.text = s; tm.font = font; tm.fontSize = 120; tm.characterSize = size * .05f; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
                tm.color = layer == 0 ? new Color(1f, .97f, .9f) : new Color(.35f, .18f, .1f);
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }

        static VolumeProfile Profile()
        {
            string path = Dir + "VP_Ranking.asset";
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (p) AssetDatabase.DeleteAsset(path);
            p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, path);
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.Neutral);
            var bloom = p.Add<Bloom>(true); bloom.intensity.Override(.4f); bloom.threshold.Override(1.0f); bloom.scatter.Override(.65f); bloom.tint.Override(new Color(1f, .9f, .8f));
            var ca = p.Add<ColorAdjustments>(true); ca.postExposure.Override(.05f); ca.contrast.Override(18f); ca.saturation.Override(-4f); ca.colorFilter.Override(new Color(1f, .97f, .95f));
            var vg = p.Add<Vignette>(true); vg.intensity.Override(.34f); vg.smoothness.Override(.45f); vg.color.Override(new Color(.35f, .12f, .05f));
            var lgg = p.Add<LiftGammaGain>(true); lgg.lift.Override(new Vector4(1f, .98f, .96f, .02f));
            foreach (var c in p.components) AssetDatabase.AddObjectToAsset(c, p);
            EditorUtility.SetDirty(p); return p;
        }

        internal static void Confetti(Transform root)
        {
            var go = new GameObject("Confetti", typeof(ParticleSystem)); go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0, 7.5f, 0); go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 7f); main.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .8f);
            main.startSize3D = true; main.startSizeX = new ParticleSystem.MinMaxCurve(.07f, .13f); main.startSizeY = new ParticleSystem.MinMaxCurve(.12f, .2f); main.startSizeZ = 1f;
            main.startRotation3D = true; main.startRotationX = new ParticleSystem.MinMaxCurve(0, 6.28f); main.startRotationY = new ParticleSystem.MinMaxCurve(0, 6.28f); main.startRotationZ = new ParticleSystem.MinMaxCurve(0, 6.28f);
            main.gravityModifier = .06f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 500; main.useUnscaledTime = true; main.prewarm = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(new Color(1f, .78f, .25f), 0), new GradientColorKey(new Color(.25f, .75f, .72f), .33f), new GradientColorKey(new Color(.97f, .42f, .45f), .66f), new GradientColorKey(new Color(1f, 1f, 1f), 1f) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission; em.rateOverTime = 45;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(16, 6, .1f);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true; rot.x = new ParticleSystem.MinMaxCurve(-4, 4); rot.y = new ParticleSystem.MinMaxCurve(-4, 4); rot.z = new ParticleSystem.MinMaxCurve(-3, 3);
            var noise = ps.noise; noise.enabled = true; noise.strength = .35f; noise.frequency = .4f;
            var col = ps.colorOverLifetime; col.enabled = true; var fade = new Gradient(); fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .08f), new GradientAlphaKey(1, .85f), new GradientAlphaKey(0, 1) }); col.color = fade;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            var m = AssetDatabase.LoadAssetAtPath<Material>(Dir + "RK Confetti.mat");
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit")); AssetDatabase.CreateAsset(m, Dir + "RK Confetti.mat"); }
            m.SetFloat("_Cull", 0); m.SetColor("_BaseColor", Color.white); EditorUtility.SetDirty(m);
            r.sharedMaterial = m; r.alignment = ParticleSystemRenderSpace.Local; r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ meshes
        static Mesh Cyclorama()
        {
            var prof = new List<Vector2>(); // (z, y)
            prof.Add(new Vector2(-14, 0)); prof.Add(new Vector2(-4, 0)); prof.Add(new Vector2(1.5f, 0));
            float R = 3.2f; for (int i = 1; i <= 16; i++) { float a = -Mathf.PI / 2 + i / 16f * Mathf.PI / 2; prof.Add(new Vector2(1.5f + R * Mathf.Cos(a), R + R * Mathf.Sin(a))); }
            prof.Add(new Vector2(1.5f + R, 18));
            int nx = 2; float x0 = -22, x1 = 22;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < prof.Count; i++) for (int j = 0; j < nx; j++) v.Add(new Vector3(Mathf.Lerp(x0, x1, j / (float)(nx - 1)), prof[i].y, prof[i].x));
            for (int i = 0; i < prof.Count - 1; i++) for (int j = 0; j < nx - 1; j++)
                {
                    int a = i * nx + j, b = (i + 1) * nx + j, c = i * nx + j + 1, d = (i + 1) * nx + j + 1;
                    t.AddRange(new[] { a, b, c, c, b, d });
                }
            var m = new Mesh { name = "Cyclorama" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        /// Revolves a (radius, height) profile around Y.
        internal static Mesh Lathe(List<Vector2> prof, int seg)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < prof.Count; i++) for (int s = 0; s <= seg; s++) { float a = s / (float)seg * Mathf.PI * 2; v.Add(new Vector3(Mathf.Cos(a) * prof[i].x, prof[i].y, Mathf.Sin(a) * prof[i].x)); }
            int w = seg + 1;
            for (int i = 0; i < prof.Count - 1; i++) for (int s = 0; s < seg; s++)
                {
                    int a = i * w + s, b = (i + 1) * w + s, c = i * w + s + 1, d = (i + 1) * w + s + 1;
                    t.AddRange(new[] { a, c, b, b, c, d });
                }
            var m = new Mesh { name = "Lathe" }; m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        /// A plump cushion/pouf: rounded top and bottom edges, slightly bulging sides. Profile goes top-centre -> bottom-centre.
        static Mesh Pouf(float r, float h, float bevel)
        {
            var p = new List<Vector2> { new Vector2(0, h) };
            p.Add(new Vector2(r * .6f, h + .015f));
            for (int i = 0; i <= 6; i++) { float a = Mathf.PI / 2 - i / 6f * Mathf.PI / 2; p.Add(new Vector2(r - bevel + Mathf.Cos(a) * bevel, h - bevel + Mathf.Sin(a) * bevel)); }
            for (int i = 1; i < 6; i++) { float u = i / 6f; p.Add(new Vector2(r + Mathf.Sin(u * Mathf.PI) * .04f, Mathf.Lerp(h - bevel, bevel, u))); }
            for (int i = 0; i <= 6; i++) { float a = 0 - i / 6f * Mathf.PI / 2; p.Add(new Vector2(r - bevel + Mathf.Cos(a) * bevel, bevel + Mathf.Sin(a) * bevel)); }
            p.Add(new Vector2(0, 0));
            return Lathe(p, 56);
        }

        internal static Mesh Torus(float R, float r, int seg, int side)
        {
            var p = new List<Vector2>(); for (int i = 0; i <= side; i++) { float a = i / (float)side * Mathf.PI * 2; p.Add(new Vector2(R + Mathf.Cos(a) * r, Mathf.Sin(a) * r)); }
            return Lathe(p, seg);
        }

        internal static Mesh Sphere(int seg, int rings)
        {
            var p = new List<Vector2>(); for (int i = 0; i <= rings; i++) { float a = Mathf.PI / 2 - i / (float)rings * Mathf.PI; p.Add(new Vector2(Mathf.Cos(a) * .5f, Mathf.Sin(a) * .5f)); }
            return Lathe(p, seg);
        }

        /// Flat sun-burst (alternating wedges) in the XZ plane, facing +Y.
        static Mesh Burst(int rays, float radius)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < rays; i++)
            {
                float a0 = i / (float)rays * Mathf.PI * 2, a1 = (i + .5f) / rays * Mathf.PI * 2;
                int b = v.Count; v.Add(Vector3.zero); v.Add(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius); v.Add(new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius);
                t.AddRange(new[] { b, b + 2, b + 1 });
            }
            var m = new Mesh { name = "Burst" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); return m;
        }

        // ------------------------------------------------------------------ fruit portraits for the scoreboard
        static void Portraits(Camera sceneCam)
        {
            string folder = "Assets/MamasComing/Resources/MamaUI/Portraits"; Directory.CreateDirectory(folder);
            var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); var prev = RenderTexture.active;
            var camGo = new GameObject("Portrait cam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.fieldOfView = 24; cam.targetTexture = rt; cam.enabled = false;
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>(); camData.renderPostProcessing = false;
            for (int k = 0; k < KidLook.Count; k++)
            {
                var go = new GameObject("Portrait kid"); go.transform.position = new Vector3(40, 0, 40); go.transform.rotation = Quaternion.Euler(0, 180, 0);
                var pk = go.AddComponent<MamaPosedKid>(); pk.kid = k; pk.pose = MamaPosedKid.Pose.Hero; pk.Rebuild();
                if (!pk.Model) { Object.DestroyImmediate(go); continue; }
                foreach (var smr in pk.Model.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
                var b = MamaPosedKid.TightBounds(pk.Model);
                Debug.Log("[RankingStage] portrait " + k + " bounds " + b.center.ToString("F2") + " size " + b.size.ToString("F2") + " renderers " + pk.Model.GetComponentsInChildren<Renderer>().Length);
                Vector3 focus = new Vector3(b.center.x, b.max.y - b.size.y * .32f, b.center.z);
                float dist = b.size.y * .62f / Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
                cam.transform.position = focus + new Vector3(-.12f, .06f, -1f).normalized * dist; cam.transform.LookAt(focus);
                cam.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); tex.Apply();
                File.WriteAllBytes(folder + "/fruit_" + k + ".png", tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                Object.DestroyImmediate(go);
            }
            RenderTexture.active = prev; cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(camGo);
            AssetDatabase.Refresh();
        }
    }
}
