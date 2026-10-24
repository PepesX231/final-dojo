using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MamasComing.Style
{
    /// <summary>Pushes the MamaToonStyle volume values to the ToyToon / ComicInk / ToonSky shader globals every camera.
    /// Adds no pass of its own; the ink is drawn by the Full Screen Pass feature using MamasComing/ComicInk.</summary>
    public sealed class MamaToonStyleFeature : ScriptableRendererFeature
    {
        public Texture2D paintTexture;

        static readonly int ShadowTint = Shader.PropertyToID("_MC_ShadowTint");
        static readonly int Cel = Shader.PropertyToID("_MC_Cel");
        static readonly int Rim = Shader.PropertyToID("_MC_Rim");
        static readonly int Halftone = Shader.PropertyToID("_MC_Halftone");
        static readonly int Grain = Shader.PropertyToID("_MC_Grain");
        static readonly int PaintTex = Shader.PropertyToID("_MC_PaintTex");
        static readonly int Ink = Shader.PropertyToID("_MC_Ink");
        static readonly int InkColor = Shader.PropertyToID("_MC_InkColor");
        static readonly int InkFade = Shader.PropertyToID("_MC_InkFade");

        public override void Create() { }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var s = VolumeManager.instance.stack?.GetComponent<MamaToonStyle>();
            bool on = s != null && s.active;
            // reflection/preview cameras keep the level look but skip the ink (no depth-normals there)
            bool game = renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView;
            Shader.SetGlobalTexture(PaintTex, paintTexture ? paintTexture : Texture2D.grayTexture);
            if (!on)
            {
                Shader.SetGlobalVector(ShadowTint, Vector4.zero); Shader.SetGlobalVector(Cel, Vector4.zero); Shader.SetGlobalVector(Rim, Vector4.zero);
                Shader.SetGlobalVector(Halftone, Vector4.zero); Shader.SetGlobalVector(Grain, Vector4.zero); Shader.SetGlobalVector(Ink, Vector4.zero);
                return;
            }
            Color tint = s.shadowTint.value; tint.a = s.shadowTintAmount.value;
            Shader.SetGlobalVector(ShadowTint, tint);
            Shader.SetGlobalVector(Cel, new Vector4(s.celSharpness.value, s.ambientBoost.value, s.shadowLift.value, 1));
            Color rim = s.rimColor.value; rim.a = s.rimStrength.value;
            Shader.SetGlobalVector(Rim, rim);
            Shader.SetGlobalVector(Halftone, new Vector4(s.halftoneStrength.value, s.halftoneCell.value, 0.785f, 0));
            Shader.SetGlobalVector(Grain, new Vector4(s.paintGrain.value, s.paintGrainScale.value, 0, 0));
            Shader.SetGlobalVector(Ink, new Vector4(game ? s.inkStrength.value : 0f, s.inkThickness.value, s.depthThreshold.value, s.normalThreshold.value));
            Color ink = s.inkColor.value; ink.a = s.inkKeepsColor.value;
            Shader.SetGlobalVector(InkColor, ink);
            Shader.SetGlobalVector(InkFade, new Vector4(s.inkFadeStart.value, Mathf.Max(s.inkFadeEnd.value, s.inkFadeStart.value + 0.01f), s.inkFar.value, 0));
        }
    }
}
