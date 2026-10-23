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
        public static float CarryDepth = .45f;

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

        /// Rebuild only PF_Kid_Banana (after re-running build_banana_rig.py), leaving the other fruit prefabs untouched.
        [MenuItem("Mama's Coming/Build Banana Kid Prefab")]
        public static void BuildBanana()
        {
            var kid = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(k => k.name.StartsWith("Kid"));
            if (!kid) { Debug.LogError("[Mama's Coming] open MamasKitchen first (needs the configured scene kid)"); return; }
            Build("Banana", kid);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] PF_Kid_Banana built from " + RiggedPath("Banana"));
        }

        /// Skinned, bone-animated export from ArtSource/Blender/Scripts/build_banana_rig.py (hands drivable by KidActionAnimator).
        static string RiggedPath(string n) => "Assets/MamasComing/Art/Family/" + n + "/" + n + "_Rigged.fbx";

        static AnimationClip SetupImport(string fbx, string name, bool rigged = false)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.importBlendShapes = true;
            var src = imp.defaultClipAnimations;
            if (src.Length > 0)
            {
                var c = src[0]; c.name = name + "_Walk"; c.loopTime = true; c.loopPose = false;
                if (!rigged) c.lastFrame = Mathf.Max(c.firstFrame + 1, c.lastFrame - 1);   // frame 40 == frame 1: skip the duplicate
                // rigged: the last key repeats the first, so the full range loops without a dropped frame
                imp.clipAnimations = new[] { c };
            }
            imp.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(x => !x.name.StartsWith("__"));
        }

        static void Build(string n, PlayerController sceneKid)
        {
            bool rigged = AssetDatabase.LoadAssetAtPath<GameObject>(RiggedPath(n));
            string fbx = rigged ? RiggedPath(n) : "Assets/MamasComing/Art/Family/" + n + "/" + n + "_Walk.fbx";
            var clip = SetupImport(fbx, n, rigged);

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
            an.runtimeAnimatorController = ctrl; an.applyRootMotion = false;
            // KidActionAnimator layers on top of the animated bones each frame; culled bones would accumulate its offsets
            an.cullingMode = rigged ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;
            if (rigged) FaceForward(model.transform, holder);
            foreach (var r in model.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true; }

            // fit: feet on the ground, centred, kid-sized
            var rs = model.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float s = Height / Mathf.Max(0.01f, b.size.y);
            holder.localScale = Vector3.one * s;
            rs = model.GetComponentsInChildren<Renderer>(); b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 off = go.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            holder.position += off;

            var fa = holder.gameObject.AddComponent<FruitAnimator>(); fa.anim = an; fa.player = go.GetComponent<PlayerController>();
            // rigged: KidActionAnimator finds Body/Arm_*/Arm_*_Lower/Hand_* bones under the holder and drives the hands;
            // otherwise there is no apple rig, so it keeps the hold logic and skips the bends
            var act = go.GetComponent<KidActionAnimator>(); if (act) act.chr = holder;
            if (!go.GetComponent<KidRoughhouse>()) go.AddComponent<KidRoughhouse>();

            if (rigged && act)
            {
                act.pivot = holder.InverseTransformPoint(go.transform.position + Vector3.up * 0.12f);
                act.carryDepth = CarryDepth;   // slim banana: hug items close instead of the apple's round-belly reach
            }
            string path = Dir + "/PF_Kid_" + n + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        /// Turn the model so its shoulders run along the kid's +X (Arm_R on the right), whatever axis the FBX arrived in.
        static void FaceForward(Transform model, Transform holder)
        {
            Transform face = null, body = null, left = null, right = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Face") face = t; else if (t.name == "Body") body = t;
                else if (t.name == "Arm_L") left = t; else if (t.name == "Arm_R") right = t;
            }
            if (!left || !right) { Debug.LogWarning("[Mama's Coming] rigged model has no Arm_L/Arm_R bones; facing left as imported"); return; }
            Vector3 side = holder.InverseTransformDirection(right.position - left.position); side.y = 0f;
            Vector3 forward = Vector3.Cross(side, Vector3.up);
            model.localRotation = Quaternion.Euler(0f, Vector3.SignedAngle(forward, Vector3.forward, Vector3.up), 0f) * model.localRotation;
            if (face && body && Vector3.Dot(holder.InverseTransformDirection(face.position - body.position), Vector3.forward) <= 0f)
                Debug.LogError("[Mama's Coming] rigged model faces backwards after shoulder alignment: Arm_L/Arm_R are swapped in the export");
        }
    }
}
