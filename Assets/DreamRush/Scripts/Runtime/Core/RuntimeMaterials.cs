using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRush.Core
{
    /// <summary>
    /// Creates URP materials from code. Used by graybox builders (which save them as assets) and as a
    /// runtime fallback. Keeps material setup in one place so the look stays consistent.
    /// </summary>
    public static class RuntimeMaterials
    {
        public const string LitShader = "Universal Render Pipeline/Lit";
        public const string UnlitShader = "Universal Render Pipeline/Unlit";

        public static Material CreateLit(Color color, float smoothness = 0.15f, string name = null)
        {
            var shader = Shader.Find(LitShader) ?? Shader.Find("Standard");
            var m = new Material(shader) { name = name ?? "M_Lit" };
            m.SetColor("_BaseColor", color);
            m.color = color;
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
            return m;
        }

        public static Material CreateUnlitTransparent(Color color, Texture texture = null, string name = null)
        {
            var shader = Shader.Find(UnlitShader) ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = name ?? "M_UnlitTransparent" };
            m.SetFloat("_Surface", 1f); // transparent
            m.SetFloat("_Blend", 0f);   // alpha
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetColor("_BaseColor", color);
            if (texture != null) m.SetTexture("_BaseMap", texture);
            return m;
        }

        /// <summary>Lit, alpha-clipped, double-sided material for paper cutouts — they receive the same light as 3D (GDD §5.7).</summary>
        public static Material CreateCutout(Texture texture, string name = null)
        {
            var m = CreateLit(Color.white, 0.05f, name ?? "M_Cutout");
            m.SetTexture("_BaseMap", texture);
            m.mainTexture = texture;
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.doubleSidedGI = true;
            m.renderQueue = (int)RenderQueue.AlphaTest;
            return m;
        }

        /// <summary>Unlit alpha-clipped material for effect sprites (always readable, unaffected by scene light).</summary>
        public static Material CreateUnlitCutout(Texture texture, string name = null)
        {
            var shader = Shader.Find(UnlitShader) ?? Shader.Find("Unlit/Transparent Cutout");
            var m = new Material(shader) { name = name ?? "M_FX" };
            m.SetTexture("_BaseMap", texture);
            m.mainTexture = texture;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.renderQueue = (int)RenderQueue.AlphaTest + 50;
            return m;
        }

        /// <summary>Soft radial blob texture (white, alpha falloff) for shadows / landing markers.</summary>
        public static Texture2D CreateRadialTexture(int size = 64, float hardness = 0.35f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "T_RadialBlob",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c; // 0 centre .. 1 edge
                float a = 1f - Mathf.SmoothStep(hardness, 1f, d);
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
