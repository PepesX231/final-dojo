using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.Editor
{
    /// Small polish on the white void title menu: a brighter, dreamier white glow, soft pastel light orbs so
    /// the white isn't empty, a few floating things pulled into the empty bottom, and the logo banner moved
    /// to the bottom-middle. Everything else in the scene is left as it is. Safe to run more than once.
    public static class MamaVoidPolish
    {
        const string Dir = "Assets/MamasComing/FrontEnd/Void";
        static string MenuPath => "Assets/MamasComing/Scenes/" + MamaLaunch.MenuScene + ".unity";

        [MenuItem("Mama's Coming/Front End/Polish white void menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[VoidPolish] stop Play mode first"); return; }
            var sc = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var fe = Object.FindFirstObjectByType<MamaFrontEnd>();
            if (!fe || !fe.title) { Debug.LogError("[VoidPolish] no title in " + MenuPath); return; }
            var sh = fe.title.shot.transform;

            Glow();
            Orbs(sh);
            Props(sh);
            MoveLogo(fe.title, sh);

            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc); AssetDatabase.SaveAssets();
            Debug.Log("[VoidPolish] done");
        }

        static Vector3 Screen(Transform sh, float u, float v, float d)
        {
            float fov = 46f; var s = sh.GetComponent<MamaShot>(); if (s) fov = s.fov;
            float th = Mathf.Tan(fov * .5f * Mathf.Deg2Rad), tw = th * 16f / 9f;
            return sh.position + sh.forward * d + sh.right * (u * tw * d) + sh.up * (v * th * d);
        }

        /// Whiter and softer: hotter sky dome + a wider, stronger bloom (the "haze" glow).
        static void Glow()
        {
            var sky = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/M_VoidSky.mat");
            if (sky) { sky.SetColor("_BaseColor", new Color(2.5f, 2.45f, 2.5f)); EditorUtility.SetDirty(sky); }
            var p = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(Dir + "/VP_Void.asset");
            if (p)
            {
                if (p.TryGet(out UnityEngine.Rendering.Universal.Bloom bl)) { bl.intensity.Override(1.35f); bl.threshold.Override(.92f); bl.scatter.Override(.86f); bl.tint.Override(new Color(1f, .96f, .98f)); }
                if (p.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments ca)) { ca.postExposure.Override(.36f); ca.contrast.Override(16f); ca.saturation.Override(14f); }
                if (p.TryGet(out UnityEngine.Rendering.Universal.Vignette vg)) { vg.intensity.Override(.08f); vg.color.Override(new Color(.9f, .82f, .95f)); }
                EditorUtility.SetDirty(p);
            }
            RenderSettings.fogColor = new Color(.99f, .98f, 1f);
            var mg = GameObject.Find("Light motes");
            if (mg) { var ps = mg.GetComponent<ParticleSystem>(); var m = ps.main; m.startSize = new ParticleSystem.MinMaxCurve(.04f, .14f); var em = ps.emission; em.rateOverTime = 70; m.maxParticles = 700; }
        }

        /// Big soft pastel "bokeh" balls of light drifting slowly in the distance - fills the white without clutter.
        static void Orbs(Transform sh)
        {
            var old = GameObject.Find("Soft light orbs"); if (old) Object.DestroyImmediate(old);
            var g = new GameObject("Soft light orbs", typeof(ParticleSystem));
            g.transform.position = sh.position + sh.forward * 12f; g.transform.rotation = Quaternion.LookRotation(sh.forward);
            var ps = g.GetComponent<ParticleSystem>(); var main = ps.main;
            main.loop = true; main.prewarm = true; main.startLifetime = 14f; main.startSpeed = .05f; main.maxParticles = 60; main.useUnscaledTime = true;
            main.startSize = new ParticleSystem.MinMaxCurve(.6f, 2.4f);
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, .62f, .8f), 0f), new GradientColorKey(new Color(.6f, .78f, 1f), .5f), new GradientColorKey(new Color(1f, .85f, .5f), 1f) },
                         new[] { new GradientAlphaKey(.5f, 0f), new GradientAlphaKey(.5f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            var em = ps.emission; em.rateOverTime = 4f;
            var shp = ps.shape; shp.shapeType = ParticleSystemShapeType.Box; shp.scale = new Vector3(26f, 12f, 10f);
            var nz = ps.noise; nz.enabled = true; nz.strength = .12f; nz.frequency = .1f;
            var col = ps.colorOverLifetime; col.enabled = true; var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .25f), new GradientAlphaKey(1, .75f), new GradientAlphaKey(0, 1) });
            col.color = fade;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "/M_GlowOrb.mat");
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(mat, Dir + "/M_GlowOrb.mat"); }
            mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0); mat.SetFloat("_ZWrite", 0);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.renderQueue = 3000; mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetColor("_BaseColor", new Color(1.3f, 1.3f, 1.3f, 1f));
            var dp = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat"); if (dp && dp.mainTexture) mat.SetTexture("_BaseMap", dp.mainTexture);
            EditorUtility.SetDirty(mat);
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mat; r.sortingFudge = 50f;
        }

        /// Pull a few small floating household things into the empty lower part / corners of the shot.
        static void Props(Transform sh)
        {
            var drift = GameObject.Find("Drifting props"); if (!drift) return;
            var list = new List<Transform>();
            foreach (Transform t in drift.transform)
            {
                if (t.name.Contains("behind sofa")) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                if (Mathf.Max(bb.size.x, bb.size.y, bb.size.z) < 1.1f) list.Add(t);
            }
            (float u, float v, float d)[] spots = { (-.05f, -.92f, 7f), (.95f, -.85f, 8f), (-.78f, -.92f, 9f), (.42f, -.9f, 10f), (-.92f, .78f, 9f), (.05f, .85f, 11f) };
            for (int i = 0; i < Mathf.Min(spots.Length, list.Count); i++)
            {
                list[i].position = Screen(sh, spots[i].u, spots[i].v, spots[i].d);
                var f = list[i].GetComponent<MamaFloaty>(); if (f) { f.bob = .22f; f.drift = new Vector3(.25f, .15f, .25f); }
            }
        }

        /// The paper logo banner, unchanged, just hung somewhere else: floating low in the middle of the shot.
        static void MoveLogo(MamaTitleScreen title, Transform sh)
        {
            var logo = title.transform.Find("Logo banner (3D)"); if (!logo) return;
            logo.position = Screen(sh, .2f, -.66f, 5f);
            logo.rotation = Quaternion.LookRotation(logo.position - sh.position, sh.up) * Quaternion.Euler(0, 0, -3f);
            logo.localScale = Vector3.one * (2.9f / 2300f * .8f);
            foreach (Transform c in logo) if (c.name == "String") c.gameObject.SetActive(false);   // no strings: it floats like everything else
            var f = logo.GetComponent<MamaFloaty>(); if (!f) f = logo.gameObject.AddComponent<MamaFloaty>();
            f.bob = .06f; f.bobSpeed = .2f; f.sway = 1.5f;
        }
    }
}
