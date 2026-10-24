using System.Collections.Generic;
using System.Linq;
using MamasComing;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Turns the clay-sculpted fruit kids (ArtSource/Blender/Scripts/sculpt/kids.py -> Art/Sculpted/<n>/<n>_Sculpt.fbx)
    /// into playable kid prefabs and points the game's kid catalog at them. "Use Original Kids" switches back.
    public static class MamaSculptKids
    {
        public static readonly string[] Names = { "Apple", "Banana", "Orange", "Watermelon", "WaxApple" };
        static readonly string[] Loops = { "Idle", "Walk", "Run", "Taunt", "Cheer" };
        static readonly string[] States = { "Idle", "Walk", "Run", "Jump", "Punch", "Taunt", "Hit", "Cheer" };
        const string Art = "Assets/MamasComing/Art/Sculpted";
        const string Dir = "Assets/MamasComing/Prefabs/Characters/Sculpted";
        const string Template = "Assets/MamasComing/Prefabs/Characters/PF_Kid_Orange.prefab";
        const string Catalog = "Assets/MamasComing/Resources/MamaKidCatalog.asset";
        public const float Height = 1.08f;

        [MenuItem("Mama's Coming/Sculpted Kids/Build Prefabs And Use In Game")]
        public static void BuildAll()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var made = new GameObject[KidLook.Count];
            foreach (var n in Names) made[System.Array.IndexOf(KidLook.Names, n)] = Build(n);
            AssetDatabase.SaveAssets();
            UseSculpted(made);
        }

        [MenuItem("Mama's Coming/Sculpted Kids/Use Original Kids")]
        public static void UseOriginal()
        {
            MamaKidCatalogBuilder.Build();   // the friend's models, apple from the scene
            Debug.Log("[Mama's Coming] kid catalog back on the original fruit models");
        }

        static void UseSculpted(GameObject[] made)
        {
            var cat = AssetDatabase.LoadAssetAtPath<KidCatalog>(Catalog);
            if (!cat) { cat = ScriptableObject.CreateInstance<KidCatalog>(); AssetDatabase.CreateAsset(cat, Catalog); }
            cat.kids = made;
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mama's Coming] kid catalog now uses the sculpted kids: " + string.Join(", ", made.Select(m => m ? m.name : "-")));
        }

        static Material ToonFrom(Material src, string folder)
        {
            string path = folder + "/" + src.name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("MamasComing/ToyToon") ?? Shader.Find("Universal Render Pipeline/Lit");
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.color : Color.white;
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            bool glossy = src.name.Contains("Eye") || src.name.Contains("Peel") || src.name.Contains("Lid") || src.name.Contains("Shoe");
            m.SetFloat("_Smoothness", glossy ? .55f : .2f);
            // coloured cel shadow: a darker, cooler, more saturated version of the paint keeps it bright for kids
            if (m.HasProperty("_ToonShadow"))
            {
                Color.RGBToHSV(c, out float h, out float s, out float v);
                m.SetColor("_ToonShadow", Color.HSVToRGB(Mathf.Repeat(h + .03f, 1f), Mathf.Min(1, s * 1.1f + .1f), v * .72f));
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static (string fbx, Dictionary<string, AnimationClip> clips) Import(string n)
        {
            string fbx = Art + "/" + n + "/" + n + "_Sculpt.fbx";
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (!imp) throw new System.Exception("missing " + fbx + " (run sculpt/kids.py)");
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.importNormals = ModelImporterNormals.Import;
            imp.SaveAndReimport();

            // toon materials beside the model, remapped by name
            string matDir = Art + "/" + n + "/Materials";
            System.IO.Directory.CreateDirectory(matDir);
            foreach (var src in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>())
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), ToonFrom(src, matDir));

            var takes = imp.defaultClipAnimations;
            var list = new List<ModelImporterClipAnimation>();
            foreach (var t in takes)
            {
                string state = States.FirstOrDefault(s => t.takeName.EndsWith("_" + s) || t.takeName.EndsWith("|" + s) || t.takeName.EndsWith(s));
                if (state == null) continue;
                t.name = n + "_" + state;
                t.loopTime = Loops.Contains(state);
                list.Add(t);
            }
            imp.clipAnimations = list.ToArray();
            imp.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__"))
                .ToDictionary(c => c.name.Substring(n.Length + 1), c => c);
            return (fbx, clips);
        }

        static GameObject Build(string n)
        {
            var (fbx, clips) = Import(n);
            string ctrlPath = Dir + "/" + n + "_Sculpt.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var s in States)
            {
                var st = sm.AddState(s);
                if (clips.TryGetValue(s, out var c)) st.motion = c; else Debug.LogWarning("[Mama's Coming] " + n + " has no " + s + " clip");
                if (s == "Idle") sm.defaultState = st;
            }

            var tpl = AssetDatabase.LoadAssetAtPath<GameObject>(Template);
            var go = Object.Instantiate(tpl);
            go.name = "PF_Kid_" + n + "_Sculpt";
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var visual = go.transform.Find("FallPivot/Visual");
            for (int i = visual.childCount - 1; i >= 0; i--) if (visual.GetChild(i).name.StartsWith("CHR_")) Object.DestroyImmediate(visual.GetChild(i).gameObject);

            var holder = new GameObject("CHR_" + n + "_Sculpt").transform; holder.SetParent(visual, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            model.transform.SetParent(holder, false);
            var an = model.GetComponent<Animator>(); if (!an) an = model.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl; an.applyRootMotion = false;
            an.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // KidActionAnimator layers on the bones every frame
            FaceForward(model.transform, holder);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }

            var rs = model.GetComponentsInChildren<Renderer>(); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            holder.localScale = Vector3.one * (Height / Mathf.Max(.01f, b.size.y));
            rs = model.GetComponentsInChildren<Renderer>(); b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            holder.position += go.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);

            var mo = holder.gameObject.AddComponent<SculptKidMotion>(); mo.anim = an;
            var act = go.GetComponent<KidActionAnimator>();
            if (act)
            {
                act.chr = holder;
                act.pivot = holder.InverseTransformPoint(go.transform.position + Vector3.up * .12f);
                act.carryDepth = n == "Banana" ? .45f : n == "Watermelon" ? .62f : .55f;
            }
            if (!go.GetComponent<KidRoughhouse>()) go.AddComponent<KidRoughhouse>();
            string path = Dir + "/PF_Kid_" + n + "_Sculpt.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[Mama's Coming] built " + path + " clips=" + string.Join(",", clips.Keys));
            return saved;
        }

        /// Shoulders along the kid's +X, face toward +Z, whatever axis the FBX arrived in.
        static void FaceForward(Transform model, Transform holder)
        {
            Transform face = null, body = null, left = null, right = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Face") face = t; else if (t.name == "Body") body = t;
                else if (t.name == "Arm_L") left = t; else if (t.name == "Arm_R") right = t;
            }
            if (!left || !right) { Debug.LogWarning("[Mama's Coming] sculpted kid has no Arm_L/Arm_R"); return; }
            Vector3 side = holder.InverseTransformDirection(right.position - left.position); side.y = 0f;
            Vector3 forward = Vector3.Cross(side, Vector3.up);
            model.localRotation = Quaternion.Euler(0f, Vector3.SignedAngle(forward, Vector3.forward, Vector3.up), 0f) * model.localRotation;
            if (face && body && Vector3.Dot(holder.InverseTransformDirection(face.position - body.position), Vector3.forward) <= 0f)
                Debug.LogError("[Mama's Coming] sculpted kid faces backwards after shoulder alignment");
        }
    }
}
