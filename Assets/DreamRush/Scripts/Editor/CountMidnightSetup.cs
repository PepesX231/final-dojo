using System.Collections.Generic;
using DreamRush.Combat;
using DreamRush.Look;
using UnityEditor;
using UnityEngine;

namespace DreamRush.EditorTools
{
    /// <summary>
    /// COUNT MIDNIGHT model (build_count_midnight.py) → DreamToon materials + the part hierarchy the boss code animates:
    /// Model/{Body, Pendulum, Cape, Head/{HourHand, MinuteHand}, Arm_L/Blade_Minute, Arm_R/Blade_Hour}.
    /// </summary>
    public static class CountMidnightSetup
    {
        const string Fbx = "Assets/DreamRush/Art/Characters/CountMidnight/CM_CountMidnight.fbx";

        static Dictionary<string, Material> Mats()
        {
            Material M(string n, Color c, float rim = 0.25f, float spec = 0f, Color? em = null)
            {
                var m = ArtKit.Mat(n, c, rim: rim, spec: spec, rampSoft: 0.12f);
                m.SetColor("_Emission", em ?? Color.black); EditorUtility.SetDirty(m); return m;
            }
            return new Dictionary<string, Material>
            {
                ["M_CM_Coat"] = M("M_CM_Coat", Core.Palette.Hex(0x232766), 0.35f),
                ["M_CM_Ink"] = M("M_CM_Ink", Core.Palette.Hex(0x120E1C), 0.3f, 0.4f),
                ["M_CM_Gold"] = M("M_CM_Gold", Core.Palette.Hex(0xFFC64E), 0.3f, 0.7f),
                ["M_CM_Glass"] = M("M_CM_Glass", Core.Palette.Hex(0x22386E), 0.6f, 0.8f, Core.Palette.Hex(0x101C40)),
                ["M_CM_White"] = M("M_CM_White", Core.Palette.Hex(0xFAF6EE), 0.2f),
                ["M_CM_Face"] = M("M_CM_Face", Core.Palette.Hex(0xFFF3D6), 0.15f, 0f, Core.Palette.Hex(0x2A2416)),
                ["M_CM_Eye"] = M("M_CM_Eye", Core.Palette.Hex(0xFF2E9A), 0f, 0f, Core.Palette.Hex(0xFF2E9A) * 2.6f),
                ["M_CM_Red"] = M("M_CM_Red", Core.Palette.Hex(0xE0213F), 0.2f, 0.3f),
                ["M_CM_Glow"] = M("M_CM_Glow", Core.Palette.Hex(0x9FF6FF), 0f, 0f, Core.Palette.Hex(0x5FE8FF) * 2.2f),
                ["M_CM_BladeH"] = M("M_CM_BladeH", Core.Palette.Hex(0xF2C14E), 0.3f, 0.8f),
                ["M_CM_BladeM"] = M("M_CM_BladeM", Core.Palette.Hex(0xDCE6F8), 0.3f, 0.8f),
                ["M_CM_Cape"] = M("M_CM_Cape", Core.Palette.Hex(0x4B2A8C), 0.5f),
            };
        }

        /// <summary>Instantiates the model under <paramref name="boss"/> and wires the CountMidnight references.</summary>
        public static bool Attach(CountMidnight boss)
        {
            var mi = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (mi == null) { Debug.LogWarning("[CM] model missing, graybox body will be used"); return false; }
            mi.bakeAxisConversion = false; mi.importAnimation = false; mi.importCameras = false; mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var kv in Mats()) mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            mi.SaveAndReimport();
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var model = new GameObject("Model").transform;
            model.SetParent(boss.transform, false);
            Transform T(string n) { var t = inst.transform.Find(n); if (t == null) Debug.LogWarning("[CM] missing part " + n); return t; }
            var body = T("CM_Body"); var pend = T("CM_Pendulum"); var headT = T("CM_Head"); var hh = T("CM_HourHand"); var mh = T("CM_MinuteHand");
            var aL = T("CM_Arm_L"); var aR = T("CM_Arm_R"); var bH = T("CM_Blade_Hour"); var bM = T("CM_Blade_Minute"); var cape = T("CM_Cape");
            // children first (everything still sits at the FBX origin), then the parts under the boss
            if (hh != null) hh.SetParent(headT, true); if (mh != null) mh.SetParent(headT, true);
            if (bH != null) bH.SetParent(aR, true); if (bM != null) bM.SetParent(aL, true);
            foreach (var t in new[] { body, pend, headT, aL, aR, cape }) if (t != null) t.SetParent(model, false);
            if (pend != null) pend.name = "Pendulum";
            Object.DestroyImmediate(inst);
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            DreamLook.AddOutline(model.gameObject, 2.2f);
            boss.model = model; boss.head = headT; boss.armL = aL; boss.armR = aR; boss.bladeHour = bH; boss.bladeMinute = bM;
            boss.hourHand = hh; boss.minuteHand = mh;
            EditorUtility.SetDirty(boss);
            return true;
        }
    }
}
