using System.Linq;
using MamasComing;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Builds playable fruit-kid prefabs (same rig as the scene kid / PF_Player) from the friend's Blender characters.
    public static class MamaFruitPrefabs
    {
        public static readonly string[] Names = { "Banana", "Orange", "Watermelon", "WaxApple" };
        const string Dir = "Assets/MamasComing/Prefabs/Characters";
        public const float Height = 1.08f;
        public static float Yaw = 0f;

        [MenuItem("Mama's Coming/Build Fruit Character Prefabs")]
        public static void BuildAll()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var kid = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(k => k.name.StartsWith("Kid"));
            if (!kid) { Debug.LogError("[Mama's Coming] open MamasKitchen first (needs the configured scene kid)"); return; }
            foreach (var n in Names) Build(n, kid);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] fruit prefabs built in " + Dir);
        }

        static AnimationClip SetupImport(string fbx, string name)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importBlendShapes = true;
            var src = imp.defaultClipAnimations;
            if (src.Length > 0)
            {
                var c = src[0]; c.name = name + "_Walk"; c.loopTime = true; c.loopPose = false;
                c.lastFrame = Mathf.Max(c.firstFrame + 1, c.lastFrame - 1);   // frame 40 == frame 1: skip the duplicate
                imp.clipAnimations = new[] { c };
            }
            imp.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(x => !x.name.StartsWith("__"));
        }

        static void Build(string n, PlayerController sceneKid)
        {
            string fbx = "Assets/MamasComing/Art/Family/" + n + "/" + n + "_Walk.fbx";
            var clip = SetupImport(fbx, n);

            string ctrlPath = Dir + "/" + n + "_Fruit.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle"); idle.motion = clip; idle.speed = 0f;
            var walk = sm.AddState("Walk"); walk.motion = clip;
            sm.defaultState = idle;

            var go = Object.Instantiate(sceneKid.gameObject);
            go.name = "PF_Kid_" + n;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var visual = go.transform.Find("FallPivot/Visual");
            for (int i = visual.childCount - 1; i >= 0; i--) if (visual.GetChild(i).name.StartsWith("CHR_")) Object.DestroyImmediate(visual.GetChild(i).gameObject);

            var holder = new GameObject("CHR_" + n).transform; holder.SetParent(visual, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            model.transform.SetParent(holder, false);
            model.transform.localRotation = Quaternion.Euler(0, Yaw, 0) * model.transform.localRotation;
            var an = model.GetComponent<Animator>(); if (!an) an = model.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true; }

            // fit: feet on the ground, centred, kid-sized
            var rs = model.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = Height / Mathf.Max(0.01f, b.size.y);
            holder.localScale = Vector3.one * s;
            rs = model.GetComponentsInChildren<Renderer>(); b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 off = go.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            holder.position += off;

            var fa = holder.gameObject.AddComponent<FruitAnimator>(); fa.anim = an; fa.player = go.GetComponent<PlayerController>();
            var act = go.GetComponent<KidActionAnimator>(); if (act) act.chr = holder;   // no apple rig: keep its hold logic, skip the bends
            if (!go.GetComponent<KidRoughhouse>()) go.AddComponent<KidRoughhouse>();

            string path = Dir + "/PF_Kid_" + n + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
    }
}
