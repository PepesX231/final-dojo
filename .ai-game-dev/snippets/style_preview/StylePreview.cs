using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Non-destructive preview of the Toybox Comic look: in-memory material clones, restored after rendering.
public static class StylePreview
{
    public static string Run(string outFile, bool witch, float px, float py, float pz, float rx, float ry, float rz, bool usePose)
    {
        var toon = Shader.Find("MamasComing/ToyToon");
        var restore = new List<(Renderer, Material[])>();
        var clones = new Dictionary<Material, Material>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer) continue;
            var mats = r.sharedMaterials; bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i]; if (!m || m.shader.name != "Universal Render Pipeline/Lit" || m.GetFloat("_Surface") > 0.5f) continue;
                if (!clones.TryGetValue(m, out var c))
                {
                    c = new Material(m) { shader = toon };
                    c.SetFloat("_ToonSoft", 0.1f); c.SetFloat("_ToonWrap", 0.2f); c.SetFloat("_ToonAmbient", 0.55f); c.SetFloat("_DetailStrength", 1f);
                    c.SetColor("_ToonShadow", witch ? new Color(0.46f, 0.44f, 0.78f) : new Color(0.62f, 0.57f, 0.86f));
                    clones[m] = c;
                }
                mats[i] = c; changed = true;
            }
            if (changed) { restore.Add((r, r.sharedMaterials)); r.sharedMaterials = mats; }
        }
        var paint = new Texture2D(256, 256, TextureFormat.R8, false);
        var px8 = new byte[256 * 256]; var rnd = new System.Random(3);
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) px8[y * 256 + x] = (byte)(118 + 20 * Mathf.PerlinNoise(x / 24f, y / 24f) + rnd.Next(0, 6));
        paint.LoadRawTextureData(px8); paint.Apply(); paint.wrapMode = TextureWrapMode.Repeat;
        Shader.SetGlobalTexture("_MC_PaintTex", paint);
        Shader.SetGlobalVector("_MC_ShadowTint", witch ? new Vector4(0.45f, 0.42f, 0.78f, 0.6f) : new Vector4(0.62f, 0.56f, 0.88f, 0.5f));
        Shader.SetGlobalVector("_MC_Cel", new Vector4(0.55f, witch ? 0.2f : 0.08f, 0.05f, 1));
        Shader.SetGlobalVector("_MC_Rim", witch ? new Vector4(0.72f, 0.82f, 1f, 0.45f) : new Vector4(1f, 0.9f, 0.78f, 0.32f));
        Shader.SetGlobalVector("_MC_Halftone", new Vector4(witch ? 0.45f : 0.35f, 5, 0.785f, 0));
        Shader.SetGlobalVector("_MC_Grain", new Vector4(0.5f, witch ? 0.25f : 1.6f, 0, 0));
        Shader.SetGlobalVector("_MC_Ink", new Vector4(0.85f, witch ? 1.5f : 1.3f, witch ? 0.04f : 0.05f, 0.4f));
        Shader.SetGlobalVector("_MC_InkColor", new Vector4(0.16f, 0.09f, 0.17f, 0.3f));
        Shader.SetGlobalVector("_MC_InkFade", witch ? new Vector4(1.2f, 5, 0.35f, 0) : new Vector4(14, 45, 0.35f, 0));

        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var fs = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().First();
        var oldMat = fs.passMaterial; var oldReq = fs.requirements;
        fs.passMaterial = new Material(Shader.Find("MamasComing/ComicInk"));
        fs.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color;
        fs.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        data.SetDirty();

        var oldSky = RenderSettings.skybox;
        var sky = new Material(Shader.Find("MamasComing/ToonSky"));
        if (witch) { sky.SetFloat("_FollowSun", 0); sky.SetVector("_SunDir", new Vector4(-0.45f, 0.42f, -0.8f, 0)); sky.SetColor("_Top", new Color(0.05f, 0.05f, 0.18f)); sky.SetColor("_Mid", new Color(0.17f, 0.12f, 0.38f)); sky.SetColor("_Horizon", new Color(0.58f, 0.32f, 0.56f)); sky.SetFloat("_Stars", 1); sky.SetFloat("_MoonPhase", 0.65f); sky.SetFloat("_CloudCover", 0.36f); sky.SetColor("_CloudLit", new Color(0.56f, 0.5f, 0.82f)); sky.SetColor("_CloudShade", new Color(0.26f, 0.2f, 0.46f)); sky.SetColor("_SunGlow", new Color(0.55f, 0.45f, 0.88f)); }
        RenderSettings.skybox = sky;

        var cam = Camera.main; var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView; var cf = cam.clearFlags;
        if (usePose) { cam.transform.SetPositionAndRotation(new Vector3(px, py, pz), Quaternion.Euler(rx, ry, rz)); cam.fieldOfView = 60; }
        cam.clearFlags = CameraClearFlags.Skybox;
        var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
        try
        {
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            RenderTexture.active = rt; var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(outFile, tex.EncodeToPNG());
        }
        finally
        {
            cam.targetTexture = null; rt.Release();
            cam.transform.SetPositionAndRotation(p0, r0); cam.fieldOfView = f0; cam.clearFlags = cf;
            foreach (var (r, m) in restore) r.sharedMaterials = m;
            RenderSettings.skybox = oldSky;
            fs.passMaterial = oldMat; fs.requirements = oldReq; data.SetDirty();
        }
        return "ok clones=" + clones.Count;
    }
}
