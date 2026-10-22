using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Look
{
    /// <summary>
    /// The one shading model (DreamToon) + selective ink outlines (GDD §5.4–5.6). Used by builders and by
    /// runtime spawners so every object in the game shares the same look.
    /// </summary>
    public static class DreamLook
    {
        static readonly Dictionary<Material, Material> _converted = new Dictionary<Material, Material>();

        public static Shader ToonShader
        {
            get
            {
                var s = DreamLookSettings.Instance;
                return s != null && s.toon != null ? s.toon : Shader.Find("DreamRush/DreamToon");
            }
        }

        public static Material Toon(Color color, Texture texture = null, float rim = 0f, bool alphaClip = false, bool doubleSided = false, string name = null)
        {
            var sh = ToonShader;
            if (sh == null) return Core.RuntimeMaterials.CreateLit(color, 0.2f, name);
            var m = new Material(sh) { name = name ?? "M_Toon" };
            m.SetColor("_BaseColor", color);
            if (texture != null) m.SetTexture("_BaseMap", texture);
            m.SetFloat("_RimStrength", rim);
            var s = DreamLookSettings.Instance;
            if (s != null) m.SetColor("_ShadowColor", s.shadowTint);
            if (alphaClip) { m.SetFloat("_AlphaClip", 1); m.EnableKeyword("_ALPHATEST_ON"); }
            if (doubleSided) m.SetFloat("_Cull", 0);
            return m;
        }

        /// <summary>Converts a URP Lit/Unlit material to DreamToon, keeping colour and texture (cached per source).</summary>
        public static Material Convert(Material src, float rim = 0f)
        {
            if (src == null) return null;
            if (src.shader == ToonShader) return src;
            if (_converted.TryGetValue(src, out var m) && m != null) return m;
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : (src.HasProperty("_Color") ? src.color : Color.white);
            Texture t = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.mainTexture;
            bool clip = src.IsKeywordEnabled("_ALPHATEST_ON") || (src.HasProperty("_AlphaClip") && src.GetFloat("_AlphaClip") > 0.5f);
            m = Toon(c, t, rim, clip, false, src.name + "_Toon");
            if (t != null && src.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap"));
            _converted[src] = m;
            return m;
        }

        /// <summary>Restyles every renderer under <paramref name="root"/> (skips particles/lines/UI and already-toon materials).</summary>
        public static void Restyle(GameObject root, float rim = 0f)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.name == "InkOutline") continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == ToonShader) continue;
                    string sn = mats[i].shader.name;
                    if (!(sn.Contains("Lit") || sn.Contains("Simple"))) continue; // leave unlit FX/cutout sprites alone
                    mats[i] = Convert(mats[i], rim); changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Adds an ink shell to each mesh under <paramref name="root"/> (selective outline, §5.6).</summary>
        public static void AddOutline(GameObject root, float widthPx = 2.5f)
        {
            var s = DreamLookSettings.Instance;
            Material mat = s != null ? s.outlineMaterial : null;
            if (mat == null)
            {
                var sh = s != null && s.outline != null ? s.outline : Shader.Find("DreamRush/InkOutline");
                if (sh == null) return;
                mat = new Material(sh) { name = "M_InkOutline" };
            }
            // one shared ink material (asset) so prefabs/scenes serialize it; width is global (Art Bible: one line weight)
            var list = new List<MeshFilter>(root.GetComponentsInChildren<MeshFilter>(true));
            foreach (var mf in list)
            {
                if (mf.sharedMesh == null || mf.name == "InkOutline" || mf.transform.Find("InkOutline") != null) continue;
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null || !r.enabled) continue;
                var go = new GameObject("InkOutline");
                go.transform.SetParent(mf.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var or = go.AddComponent<MeshRenderer>();
                var mats = new Material[Mathf.Max(1, mf.sharedMesh.subMeshCount)];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                or.sharedMaterials = mats;
                or.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                or.receiveShadows = false;
            }
        }
    }
}
