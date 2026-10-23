using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MamasComing.Style;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.Style.Editor
{
    /// <summary>"Bright Comic" art pass (docs/MAMA_ART_STYLE.md). Visual only: materials, textures, sky, volume, renderer features.
    /// Never moves geometry or touches colliders/gameplay. Safe to re-run after a level rebuild.</summary>
    public static class MamaToonStyleTool
    {
        const string Root = "Assets/MamasComing/Art/Style";
        const string TexDir = Root + "/Textures";
        const string MatDir = Root + "/Materials";
        const string KitchenScene = "Assets/MamasComing/Scenes/MamasKitchen.unity";
        const string WitchScene = "Assets/MamasComing/WitchTest/Scenes/AppleWitch_Test.unity";
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";

        static Texture2D wood, tile, wallpaper, fabric, plaster, brick, paint;

        [MenuItem("MamasComing/Style/Apply Bright Comic Look (both levels)")]
        public static void ApplyBothLevels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode first.");
            string back = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var log = new StringBuilder();
            foreach (var path in new[] { KitchenScene, WitchScene })
            {
                EditorSceneManager.OpenScene(path);
                log.AppendLine(ApplyActiveScene());
            }
            if (!string.IsNullOrEmpty(back) && File.Exists(back)) EditorSceneManager.OpenScene(back);
            Debug.Log("[MamaToonStyle] " + log);
        }

        /// <summary>Command-line entry: -executeMethod MamasComing.Style.Editor.MamaToonStyleTool.Batch (applies, audits layering, captures, quits).</summary>
        public static void Batch()
        {
            int code = 0;
            try
            {
                var log = new StringBuilder();
                foreach (var path in new[] { KitchenScene, WitchScene })
                {
                    EditorSceneManager.OpenScene(path);
                    log.AppendLine(ApplyActiveScene());
                    log.AppendLine(MamaLayerAudit.AuditActiveScene("docs/evidence/style", fixThinOverlaps: true));
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                }
                log.AppendLine(Capture("docs/evidence/style", "bright"));
                Directory.CreateDirectory("docs/evidence/style");
                File.WriteAllText("docs/evidence/style/batch.log", log.ToString());
                Debug.Log("[MamaToonStyle] batch\n" + log);
            }
            catch (Exception e) { Debug.LogException(e); code = 1; }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        [MenuItem("MamasComing/Style/Apply Bright Comic Look (open scene)")]
        public static void ApplyOpenSceneMenu() => Debug.Log("[MamaToonStyle] " + ApplyActiveScene());

        public static string ApplyActiveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            bool witch = scene.path == WitchScene;
            EnsureAssets();
            var colliders = ColliderSignature();
            int converted = ConvertMaterials(scene, witch);
            var profile = SetupVolume(witch);
            SetupSky(witch);
            if (colliders != ColliderSignature()) throw new InvalidOperationException("Collider signature changed - aborting before save.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return scene.name + ": " + converted + " materials on ToyToon, volume=" + AssetDatabase.GetAssetPath(profile);
        }

        [MenuItem("MamasComing/Style/Revert open scene materials to URP Lit")]
        public static void RevertOpenScene()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit"); int n = 0;
            foreach (var m in SceneMaterials(EditorSceneManager.GetActiveScene()))
                if (m.shader.name == "MamasComing/ToyToon") { m.shader = lit; EditorUtility.SetDirty(m); n++; }
            AssetDatabase.SaveAssets();
            Debug.Log("[MamaToonStyle] reverted " + n + " materials to URP/Lit");
        }

        // ---------------------------------------------------------------- shared assets

        public static void EnsureAssets()
        {
            Directory.CreateDirectory(TexDir); Directory.CreateDirectory(MatDir);
            wood = Tex("TEX_ComicWoodPlanks", Wood, true, 1);
            tile = Tex("TEX_ComicTile", Tile, true, 2);
            wallpaper = Tex("TEX_ComicWallpaper", Wallpaper, true, 3);
            fabric = Tex("TEX_ComicFabric", Fabric, true, 4);
            plaster = Tex("TEX_ComicPlaster", Plaster, true, 5);
            brick = Tex("TEX_ComicBrick", Brick, true, 6);
            paint = TexRGB("TEX_ComicPaint", PaintGrime, false);
            EnsureRendererFeatures();
        }

        static void EnsureRendererFeatures()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!data) throw new InvalidOperationException("Missing " + RendererPath);
            var style = data.rendererFeatures.OfType<MamaToonStyleFeature>().FirstOrDefault();
            if (!style) style = AddFeature<MamaToonStyleFeature>(data, "MamaToonStyle");
            style.paintTexture = paint; style.SetActive(true); EditorUtility.SetDirty(style);

            var inkMat = Mat("MAT_ComicInk", "MamasComing/ComicInk");
            // reuse the orphaned "DreamComic" full-screen slot if present, otherwise add one
            var ink = data.rendererFeatures.OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(f => f.name == "MamaComicInk" || f.name == "DreamComic" || f.passMaterial == inkMat || f.passMaterial == null);
            if (!ink) ink = AddFeature<FullScreenPassRendererFeature>(data, "MamaComicInk");
            ink.name = "MamaComicInk";
            ink.passMaterial = inkMat; ink.passIndex = 0;
            ink.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            ink.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color;
            ink.fetchColorBuffer = true;
            ink.SetActive(true);
            EditorUtility.SetDirty(ink); EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        static T AddFeature<T>(UniversalRendererData data, string name) where T : ScriptableRendererFeature
        {
            var f = ScriptableObject.CreateInstance<T>(); f.name = name;
            AssetDatabase.AddObjectToAsset(f, data);
            data.rendererFeatures.Add(f);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long id);
            var so = new SerializedObject(data);
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            return f;
        }

        static Material Mat(string name, string shader)
        {
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find(shader)) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else if (m.shader.name != shader) m.shader = Shader.Find(shader);
            return m;
        }

        // ---------------------------------------------------------------- materials

        static IEnumerable<Material> SceneMaterials(UnityEngine.SceneManagement.Scene scene)
        {
            var seen = new HashSet<Material>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (r.gameObject.scene != scene || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                foreach (var m in r.sharedMaterials)
                    if (m && seen.Add(m) && AssetDatabase.Contains(m) && AssetDatabase.GetAssetPath(m).EndsWith(".mat", StringComparison.Ordinal)) yield return m;
            }
        }

        static int ConvertMaterials(UnityEngine.SceneManagement.Scene scene, bool witch)
        {
            var toon = Shader.Find("MamasComing/ToyToon");
            if (!toon) throw new InvalidOperationException("ToyToon shader did not compile.");
            int n = 0;
            foreach (var m in SceneMaterials(scene))
            {
                bool isLit = m.shader.name == "Universal Render Pipeline/Lit" || m.shader.name == "Universal Render Pipeline/Simple Lit";
                if (!(isLit || m.shader == toon)) continue;
                if (isLit && (m.GetFloat("_Surface") > 0.5f || m.renderQueue >= 2450 && !m.IsKeywordEnabled("_ALPHATEST_ON"))) continue;   // glass / water stay Lit
                if (isLit) { int q = m.renderQueue; m.shader = toon; m.renderQueue = q < 2450 ? -1 : q; }
                Style(m, witch);
                EditorUtility.SetDirty(m); n++;
            }
            return n;
        }

        static void Style(Material m, bool witch)
        {
            string n = m.name.ToLowerInvariant();
            Color c = m.GetColor("_BaseColor");
            Color.RGBToHSV(c, out float h, out float s, out float v);
            // coloured shadow: lavender base pulled toward a deeper, more saturated version of the surface hue (never grey/black)
            Color hueShadow = Color.HSVToRGB(Mathf.Repeat(h + 0.04f, 1f), Mathf.Clamp01(s * 1.25f + 0.15f), 0.72f);
            Color lav = witch ? new Color(0.46f, 0.44f, 0.78f) : new Color(0.62f, 0.57f, 0.86f);
            m.SetColor("_ToonShadow", s > 0.12f ? Color.Lerp(lav, hueShadow, 0.35f) : lav);

            bool soft = Any(n, "cushion", "pillow", "sofa", "blanket", "curtain", "carpet", "rug", "towel", "beanbag", "bed", "plush", "teddy", "bear", "skin", "hair", "cloth", "fabric");
            bool metal = m.GetFloat("_Metallic") > 0.3f || Any(n, "brass", "metal", "chrome", "steel", "pot", "pan");
            m.SetFloat("_ToonSoft", soft ? 0.22f : metal ? 0.06f : 0.1f);
            m.SetFloat("_ToonWrap", soft ? 0.35f : 0.18f);
            m.SetFloat("_ToonStep", 0.02f);
            m.SetFloat("_ToonAmbient", 0.55f);
            m.SetFloat("_DetailStrength", metal ? 0.3f : soft ? 1.2f : 1f);

            var t = m.GetTexture("_BaseMap");
            string tp = t ? AssetDatabase.GetAssetPath(t) : "";
            Texture2D assign = null; float metres = 0;
            if (tp.StartsWith("Assets/MamasComing/Art/Polish/Textures/", StringComparison.Ordinal) || tp.StartsWith(TexDir, StringComparison.Ordinal) || !t)
            {
                string tn = t ? t.name.ToLowerInvariant() : "";
                bool ceramic = Any(n, "kitchen", "bath", "laundry", "tile", "basin");
                if (tn.Contains("wood") || tn.Contains("plank") || (!t && Any(n, "wood", "plank", "shelf", "bookshelf", "table", "desk", "bench", "stool", "crate", "tvstand", "pantry", "door", "drawer")) || (!t && n.Contains("floor") && !ceramic)) { assign = wood; metres = 1.4f; }
                if (tn.Contains("tile") || (n.Contains("floor") && ceramic) || (!t && n.Contains("tile"))) { assign = tile; metres = 1.2f; }
                if (tn.Contains("wallpaper") || (!t && Any(n, "stripe", "wallk_", "walll_", "wallpaper"))) { assign = wallpaper; metres = 1.6f; }
                if (tn.Contains("fabric") || tn.Contains("toonfabric") || (!t && soft && !Any(n, "skin", "hair", "teddy", "bear"))) { assign = fabric; metres = 0.7f; }
                if (!t && assign == null && Any(n, "brick", "exterior", "outer", "facade", "chimney", "garden", "yard", "alley")) { assign = brick; metres = 2.2f; }
                if (!t && assign == null && Any(n, "wall", "ceiling", "plaster", "house", "skirting", "baseboard")) { assign = plaster; metres = 2.5f; }
                if (witch) metres *= 0.25f;   // the witch kitchen is seen from a 12 cm apple
            }
            if (assign)
            {
                m.SetTexture("_BaseMap", assign);
                m.SetTextureScale("_BaseMap", Vector2.one); m.SetTextureOffset("_BaseMap", Vector2.zero);
                m.SetFloat("_WorldUV", metres);
            }
        }

        static bool Any(string s, params string[] keys) { foreach (var k in keys) if (s.Contains(k)) return true; return false; }

        // ---------------------------------------------------------------- volume / sky

        static VolumeProfile SetupVolume(bool witch)
        {
            var volume = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v => v.isGlobal && v.gameObject.scene == EditorSceneManager.GetActiveScene());
            string path = MatDir.Replace("/Materials", "") + (witch ? "/VP_Witch_BrightComic.asset" : "/VP_Kitchen_BrightComic.asset");
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile)
            {
                // start from the level's current grade so nothing that was tuned there is lost
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
                if (volume && volume.sharedProfile)
                    foreach (var src in volume.sharedProfile.components)
                    {
                        if (!src) continue;
                        var c = UnityEngine.Object.Instantiate(src); c.name = src.GetType().Name;
                        profile.components.Add(c); AssetDatabase.AddObjectToAsset(c, profile);
                    }
            }
            if (!volume)
            {
                volume = new GameObject(witch ? "Witch Global Volume" : "Global Volume").AddComponent<Volume>();
                volume.isGlobal = true;
            }
            volume.sharedProfile = profile; volume.priority = Mathf.Max(volume.priority, 1); volume.weight = 1;
            EditorUtility.SetDirty(volume);

            // "Bright Comic" grade: the refs' ink, halftone and street-art flavour, but sunny, vivid and kid-friendly.
            // No dirty colour cast; light vignette, a hint of chromatic fringe and a gentle CRT bulge for the comic-panel feel.
            var tm = Get<Tonemapping>(profile); tm.mode.Override(TonemappingMode.Neutral);
            var ca = Get<ColorAdjustments>(profile);
            ca.postExposure.Override(witch ? 0.05f : 0.15f); ca.contrast.Override(witch ? 24f : 16f); ca.saturation.Override(witch ? 42f : 34f);
            ca.colorFilter.Override(Color.white); ca.hueShift.Override(0f);
            var lgg = Get<LiftGammaGain>(profile);
            lgg.lift.Override(new Vector4(1f, 1f, 1f, 0f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0.02f));
            lgg.gain.Override(witch ? new Vector4(1f, 1f, 1.02f, 0.04f) : new Vector4(1.03f, 1.01f, 0.97f, 0.04f));
            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(witch ? 0.9f : 1.0f); bloom.intensity.Override(witch ? 0.9f : 0.6f);
            bloom.scatter.Override(0.7f); bloom.tint.Override(witch ? new Color(0.85f, 0.8f, 1f) : new Color(1f, 0.92f, 0.78f)); bloom.highQualityFiltering.Override(true);
            var vig = Get<Vignette>(profile);
            vig.intensity.Override(witch ? 0.26f : 0.2f); vig.smoothness.Override(0.4f); vig.rounded.Override(true);
            vig.color.Override(new Color(0.05f, 0.04f, 0.1f));
            var smh = Get<ShadowsMidtonesHighlights>(profile);
            smh.shadows.Override(new Vector4(0.98f, 0.98f, 1.04f, 0f));
            smh.highlights.Override(new Vector4(1.04f, 1.02f, 0.96f, 0f));
            var wb = Get<WhiteBalance>(profile); wb.temperature.Override(witch ? -4f : 3f); wb.tint.Override(witch ? -6f : -4f);
            var chroma = Get<ChromaticAberration>(profile); chroma.intensity.Override(0.08f);
            var grain = Get<FilmGrain>(profile); grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(0.12f); grain.response.Override(0.85f);
            var lens = Get<LensDistortion>(profile); lens.intensity.Override(0.08f); lens.scale.Override(0.985f);

            var st = Get<MamaToonStyle>(profile);
            st.inkStrength.Override(1f);
            st.inkColor.Override(witch ? new Color(0.06f, 0.03f, 0.1f) : new Color(0.07f, 0.04f, 0.08f));
            st.inkKeepsColor.Override(0.12f);
            st.inkThickness.Override(witch ? 2.2f : 2.0f);
            st.depthThreshold.Override(witch ? 0.035f : 0.045f);
            st.normalThreshold.Override(0.35f);
            st.inkFadeStart.Override(witch ? 1.5f : 16f); st.inkFadeEnd.Override(witch ? 6f : 50f); st.inkFar.Override(0.45f);
            st.shadowTint.Override(witch ? new Color(0.42f, 0.4f, 0.85f) : new Color(0.58f, 0.52f, 0.95f));
            st.shadowTintAmount.Override(0.45f);
            st.celSharpness.Override(0.75f);
            st.ambientBoost.Override(witch ? 0.25f : 0.12f);
            st.shadowLift.Override(0.06f);
            st.rimColor.Override(witch ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.95f, 0.75f));
            st.rimStrength.Override(witch ? 0.5f : 0.38f);
            st.halftoneStrength.Override(0.35f);
            st.halftoneCell.Override(6f);
            st.paintGrain.Override(0.25f);
            st.paintGrainScale.Override(witch ? 0.3f : 1.8f);
            st.grime.Override(0.06f);   // a little lived-in wear, never dirty
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Get<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (p.TryGet<T>(out var c)) { c.active = true; EditorUtility.SetDirty(c); return c; }
            c = p.Add<T>(true); c.name = typeof(T).Name;
            if (AssetDatabase.Contains(p)) AssetDatabase.AddObjectToAsset(c, p);
            return c;
        }

        static void SetupSky(bool witch)
        {
            var sky = Mat(witch ? "SKY_BrightMagicNight" : "SKY_BrightComicDay", "MamasComing/ToonSky");
            if (witch)
            {
                sky.SetFloat("_FollowSun", 0); sky.SetVector("_SunDir", new Vector4(-0.45f, 0.42f, -0.8f, 0));
                sky.SetColor("_Top", new Color(0.08f, 0.06f, 0.3f)); sky.SetColor("_Mid", new Color(0.3f, 0.15f, 0.55f));
                sky.SetColor("_Horizon", new Color(0.95f, 0.45f, 0.7f)); sky.SetColor("_Ground", new Color(0.1f, 0.06f, 0.18f));
                sky.SetColor("_SunColor", new Color(1.8f, 1.7f, 1.3f)); sky.SetColor("_SunGlow", new Color(0.7f, 0.55f, 1f));
                sky.SetFloat("_SunSize", 0.0045f); sky.SetFloat("_MoonPhase", 0.65f);
                sky.SetColor("_CloudLit", new Color(0.75f, 0.6f, 1f)); sky.SetColor("_CloudShade", new Color(0.36f, 0.25f, 0.62f)); sky.SetColor("_CloudInk", new Color(0.06f, 0.03f, 0.12f));
                sky.SetFloat("_CloudCover", 0.36f); sky.SetFloat("_Stars", 1f); sky.SetFloat("_Halftone", 0.3f); sky.SetFloat("_Bands", 5f);
                var cam = Camera.main;
                if (cam && cam.clearFlags != CameraClearFlags.Skybox) { cam.clearFlags = CameraClearFlags.Skybox; EditorUtility.SetDirty(cam); }
            }
            else
            {
                sky.SetFloat("_FollowSun", 1);
                sky.SetColor("_Top", new Color(0.15f, 0.45f, 0.95f)); sky.SetColor("_Mid", new Color(0.4f, 0.72f, 1f));
                sky.SetColor("_Horizon", new Color(1f, 0.85f, 0.55f)); sky.SetColor("_Ground", new Color(0.5f, 0.42f, 0.6f));
                sky.SetColor("_SunColor", new Color(2.2f, 1.9f, 1.2f)); sky.SetColor("_SunGlow", new Color(1f, 0.72f, 0.4f));
                sky.SetFloat("_SunSize", 0.0035f); sky.SetFloat("_MoonPhase", 0f);
                sky.SetColor("_CloudLit", new Color(1f, 1f, 0.97f)); sky.SetColor("_CloudShade", new Color(0.62f, 0.7f, 0.95f)); sky.SetColor("_CloudInk", new Color(0.08f, 0.06f, 0.15f));
                sky.SetFloat("_CloudCover", 0.5f); sky.SetFloat("_Stars", 0f); sky.SetFloat("_Halftone", 0.4f); sky.SetFloat("_Bands", 6f);
            }
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            // ambient/fog follow the grade so no old pink/purple haze fights it
            RenderSettings.ambientMode = AmbientMode.Trilight;
            if (witch)
            {
                RenderSettings.ambientSkyColor = new Color(0.5f, 0.52f, 0.66f); RenderSettings.ambientEquatorColor = new Color(0.46f, 0.4f, 0.38f); RenderSettings.ambientGroundColor = new Color(0.2f, 0.16f, 0.2f);
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.85f); RenderSettings.ambientEquatorColor = new Color(0.85f, 0.72f, 0.6f); RenderSettings.ambientGroundColor = new Color(0.4f, 0.32f, 0.38f);
                if (RenderSettings.fog) RenderSettings.fogColor = new Color(0.85f, 0.82f, 0.95f);
            }
        }

        static string ColliderSignature()
        {
            var scene = EditorSceneManager.GetActiveScene();
            return string.Join("\n", UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID)
                .Where(c => c.gameObject.scene == scene)
                .Select(c => c.GetInstanceID() + "|" + c.enabled + "|" + c.bounds.center + "|" + c.bounds.size));
        }

        // ---------------------------------------------------------------- evidence shots

        [MenuItem("MamasComing/Style/Capture style evidence shots")]
        public static void CaptureMenu() => Capture("docs/evidence/style", "after");

        /// <summary>Renders fixed views of both levels into folder/{prefix}_*.png. Edit mode only; restores the camera.</summary>
        public static string Capture(string folder, string prefix)
        {
            Directory.CreateDirectory(folder);
            string back = EditorSceneManager.GetActiveScene().path;
            var written = new List<string>();
            EditorSceneManager.OpenScene(KitchenScene);
            written.Add(Shot(folder, prefix + "_kitchen_top.png", null, default));
            written.Add(Shot(folder, prefix + "_kitchen_low.png", new Vector3(4.4f, 2.6f, -3.2f), Quaternion.Euler(22f, 10f, 0f)));
            EditorSceneManager.OpenScene(WitchScene);
            written.Add(Shot(folder, prefix + "_witch_play.png", new Vector3(-0.515f, 1.235f, 1.44f), Quaternion.Euler(24f, 180f, 0f)));
            var from = new Vector3(1.3f, 1.9f, 1.5f);
            written.Add(Shot(folder, prefix + "_witch_wide.png", from, Quaternion.LookRotation(new Vector3(-0.3f, 0.7f, -0.6f) - from)));
            if (!string.IsNullOrEmpty(back) && File.Exists(back)) EditorSceneManager.OpenScene(back);
            return string.Join("\n", written);
        }

        static string Shot(string folder, string file, Vector3? pos, Quaternion rot)
        {
            var cam = Camera.main;
            var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
            if (pos.HasValue) { cam.transform.SetPositionAndRotation(pos.Value, rot); cam.fieldOfView = 60f; }
            var rt = new RenderTexture(1600, 900, 24) { antiAliasing = 4 };
            try
            {
                cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
                RenderTexture.active = null;
                string path = Path.Combine(folder, file);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return path;
            }
            finally
            {
                cam.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
                cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0;
            }
        }

        // ---------------------------------------------------------------- procedural textures (tileable, grey: the material colour tints them)

        const int Size = 1024;

        static Texture2D Tex(string name, Func<float, float, float> f, bool srgb, int grimeSeed = -1)
            => TexRGB(name, (u, v) => { float g = f(u, v) * (grimeSeed >= 0 ? GrimeMul(u, v, grimeSeed) : 1f); return new Color(g, g, g, 1); }, srgb);

        static Texture2D TexRGB(string name, Func<float, float, Color> f, bool srgb)
        {
            string path = TexDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                var px = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        px[y * Size + x] = (Color32)f((x + 0.5f) / Size, (y + 0.5f) / Size);
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                tex.SetPixels32(px); tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.sRGBTexture != srgb || imp.wrapMode != TextureWrapMode.Repeat || imp.anisoLevel != 4)
            {
                imp.sRGBTexture = srgb; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true; imp.anisoLevel = 4;
                imp.textureCompression = TextureImporterCompression.CompressedHQ; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 982451653);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        // value noise that tiles with period (px, py) cells over u,v in [0,1)
        static float VNoise(float u, float v, int px, int py, int seed)
        {
            float x = u * px, y = v * py;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int Wx(int i) => ((i % px) + px) % px; int Wy(int i) => ((i % py) + py) % py;
            float a = Hash(Wx(x0), Wy(y0), seed), b = Hash(Wx(x0 + 1), Wy(y0), seed);
            float c = Hash(Wx(x0), Wy(y0 + 1), seed), d = Hash(Wx(x0 + 1), Wy(y0 + 1), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int px, int py, int oct, int seed)
        {
            float s = 0, a = 0.5f, n = 0;
            for (int i = 0; i < oct; i++) { s += VNoise(u, v, px << i, py << i, seed + i * 17) * a; n += a; a *= 0.5f; }
            return s / n;
        }

        static float Posterize(float x, int steps, float soft = 0.15f)
        {
            float q = x * steps; float f = q - Mathf.Floor(q);
            return (Mathf.Floor(q) + Mathf.SmoothStep(0.5f - soft, 0.5f + soft, f)) / steps;
        }

        // 4 boards across, staggered butt joints, banded toon grain, a few knots
        static float Wood(float u, float v)
        {
            const int boards = 4;
            int b = Mathf.FloorToInt(u * boards);
            float bu = u * boards - b;
            float tone = 0.80f + 0.14f * Hash(b, 7, 3);
            float jointV = Hash(b, 11, 5);
            float vv = Mathf.Repeat(v - jointV, 1f);
            int seg = vv < 0.5f ? 0 : 1;
            tone += 0.05f * (Hash(b, seg, 9) - 0.5f);
            float warp = Fbm(u, v, 8, 2, 3, 21) * 2.2f;
            float grain = Mathf.Sin((bu * 9f + warp) * Mathf.PI * 2f) * 0.5f + 0.5f;
            grain = Posterize(grain, 3, 0.2f);
            float fine = Fbm(u, v, 128, 8, 2, 31);
            float val = tone * (0.9f + 0.08f * grain + 0.04f * fine);
            for (int k = 0; k < 2; k++)
            {
                float kx = (b + 0.25f + 0.5f * Hash(b, 20 + k, 1)) / boards, ky = Hash(b, 30 + k, 2);
                if (Hash(b, 40 + k, 3) < 0.55f) continue;
                float dx = (u - kx) * boards * 3.5f, dy = Mathf.Repeat(v - ky + 0.5f, 1f) - 0.5f; dy *= 14f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r < 1f) val *= 0.86f + 0.1f * Posterize(Mathf.Sin(r * 14f) * 0.5f + 0.5f, 2, 0.25f) * r;
            }
            float seam = Mathf.Min(bu, 1 - bu) * Size / boards;
            if (seam < 2.5f) val *= 0.62f + 0.12f * seam;
            float j = Mathf.Min(Mathf.Abs(vv), Mathf.Abs(vv - 0.5f), Mathf.Abs(vv - 1f)) * Size;
            if (j < 2f) val *= 0.66f;
            if (seam >= 2.5f && seam < 6f) val *= 1.04f;     // tiny bevel highlight next to the seam
            return val;
        }

        // 4x4 glazed tiles: grout, bevel light/shade, tone per tile, speckles
        static float Tile(float u, float v)
        {
            const int n = 4;
            int tx = Mathf.FloorToInt(u * n), ty = Mathf.FloorToInt(v * n);
            float fu = u * n - tx, fv = v * n - ty;
            float px = Size / (float)n;
            float g = 6f / px;
            if (fu < g || fv < g) return 0.6f + 0.06f * Fbm(u, v, 64, 64, 2, 4);
            float val = 0.9f + 0.06f * Hash(tx, ty, 13);
            float e = 9f / px;
            if (fu < g + e || fv > 1 - e) val += 0.05f;          // lit bevel
            if (fv < g + e || fu > 1 - e) val -= 0.06f;          // shaded bevel
            val += 0.03f * (Fbm(u, v, 16, 16, 3, 8) - 0.5f);
            if (Hash(Mathf.FloorToInt(u * 512), Mathf.FloorToInt(v * 512), 77) > 0.9975f) val -= 0.12f;
            return val;
        }

        // wide/narrow stripes with a pinstripe and a small diamond motif
        static float Wallpaper(float u, float v)
        {
            const int stripes = 6;
            float su = u * stripes; int si = Mathf.FloorToInt(su); float f = su - si;
            float val = f < 0.62f ? 0.97f : 0.89f;
            if (Mathf.Abs(f - 0.62f) < 0.012f || Mathf.Abs(f - 0.985f) < 0.012f) val = 0.8f;
            float cx = (f - 0.31f) / 0.31f, cy = Mathf.Repeat(v * 12f + (si % 2) * 0.5f, 1f) * 2f - 1f;
            float diamond = Mathf.Abs(cx) + Mathf.Abs(cy);
            if (f < 0.62f && diamond < 0.32f) val = diamond < 0.16f ? 0.86f : 0.92f;
            val *= 0.97f + 0.05f * Fbm(u, v, 16, 16, 3, 51);
            return val;
        }

        // basket weave with a soft knit wobble
        static float Fabric(float u, float v)
        {
            const int cells = 64;
            float x = u * cells, y = v * cells;
            int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float fx = x - cx, fy = y - cy;
            bool horiz = ((cx + cy) & 1) == 0;
            float across = horiz ? fy : fx;
            float thread = Mathf.Sin(across * Mathf.PI);
            float val = 0.82f + 0.12f * thread;
            val += 0.06f * (Fbm(u, v, 32, 32, 3, 61) - 0.5f);
            val *= 0.96f + 0.06f * Fbm(u, v, 4, 4, 3, 71);
            return val;
        }

        // soft hand-painted plaster: big blotches + short brush strokes
        static float Plaster(float u, float v)
        {
            float big = Fbm(u, v, 4, 4, 4, 81);
            float strokes = Fbm(u, v, 48, 6, 3, 91);
            return 0.9f + 0.07f * Posterize(big, 4, 0.3f) + 0.04f * (strokes - 0.5f);
        }

        // linear grain used everywhere through ToyToon (_MC_PaintTex); mean 0.5
        static float PaintGrain(float u, float v)
        {
            float a = Fbm(u, v, 6, 6, 4, 101);
            float s = Fbm(u, v, 40, 8, 3, 111);
            float d = Fbm(u, v, 8, 40, 3, 121);
            float x = 0.55f * a + 0.25f * s + 0.2f * d;
            return Mathf.Clamp01(0.5f + (Posterize(x, 5, 0.35f) - 0.5f) * 1.6f);
        }

        // r = brush grain, g = grime mask (blotchy stains + vertical drips), both linear and tileable
        static Color PaintGrime(float u, float v)
        {
            return new Color(PaintGrain(u, v), GrimeMask(u, v, 131), 0, 1);
        }

        static float GrimeMask(float u, float v, int seed)
        {
            float blot = Fbm(u, v, 5, 5, 5, seed);
            float stain = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 0.7f, blot));
            // drips: columns of streaks running down from random heights
            int cols = 48; int c = Mathf.FloorToInt(u * cols);
            float cu = u * cols - c;
            float top = Hash(c, 3, seed), len = 0.08f + 0.35f * Hash(c, 4, seed), on = Hash(c, 5, seed);
            float dv = Mathf.Repeat(top - v, 1f);
            float width = 0.18f + 0.25f * Hash(c, 6, seed);
            float drip = on > 0.55f && dv < len ? (1f - dv / len) * Mathf.Clamp01(1f - Mathf.Abs(cu - 0.5f) / width) : 0f;
            float speck = Hash(Mathf.FloorToInt(u * 256), Mathf.FloorToInt(v * 256), seed + 9) > 0.985f ? 0.6f : 0f;
            return Mathf.Clamp01(Posterize(Mathf.Max(stain, drip * 0.8f, speck), 3, 0.25f));
        }

        static float GrimeMul(float u, float v, int seed)
        {
            float g = GrimeMask(u, v, 200 + seed * 7);
            float scratch = Fbm(u, v, 96, 3, 2, 300 + seed) > 0.78f ? 0.94f : 1f;
            return Mathf.Lerp(1f, 0.93f, g) * scratch;
        }

        // chunky hand-drawn bricks with chipped corners and dark mortar
        static float Brick(float u, float v)
        {
            const int rows = 12, cols = 4;
            float y = v * rows; int r = Mathf.FloorToInt(y); float fy = y - r;
            float x = u * cols + ((r & 1) == 1 ? 0.5f : 0f); int c = Mathf.FloorToInt(x); float fx = x - c;
            c = ((c % cols) + cols) % cols;
            float wob = (Fbm(u, v, 32, 32, 2, 141) - 0.5f) * 0.06f;
            float mx = 0.045f + wob, my = 0.09f + wob;
            if (fx < mx || fx > 1 - mx || fy < my || fy > 1 - my) return 0.38f + 0.1f * Fbm(u, v, 64, 64, 2, 151);
            float val = 0.72f + 0.2f * Hash(c, r, 161);
            val *= 0.92f + 0.12f * Posterize(Fbm(u, v, 24, 24, 3, 171), 3, 0.2f);
            if (fy > 1 - my - 0.12f) val *= 0.86f;      // painted under-shadow on each brick
            if (fy < my + 0.1f) val *= 1.06f;
            return val;
        }
    }
}
