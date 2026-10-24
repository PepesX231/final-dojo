using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// Builds Scenes/MamaSculptShowcase.unity: the five sculpted kids on a bright stage, cycling their moves with comic VFX.
    public static class MamaSculptShowcase
    {
        const string ScenePath = "Assets/MamasComing/Art/Sculpted/Showcase/MamaSculptShowcase.unity";

        [MenuItem("Mama's Coming/Sculpted Kids/Build Showcase Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("[Mama's Coming] stop play mode before building the showcase"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var toon = Shader.Find("MamasComing/ToyToon") ?? Shader.Find("Universal Render Pipeline/Lit");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Stage"; floor.transform.localScale = new Vector3(2, 1, 1.2f);
            var fm = new Material(toon) { name = "StageMint" }; fm.SetColor("_BaseColor", new Color(.55f, .9f, .78f));
            floor.GetComponent<Renderer>().sharedMaterial = fm;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Backdrop"; wall.transform.position = new Vector3(0, 2.5f, 3.2f); wall.transform.localScale = new Vector3(20, 6, 1);
            var wm = new Material(toon) { name = "BackdropButter" }; wm.SetColor("_BaseColor", new Color(1f, .87f, .45f));
            wall.GetComponent<Renderer>().sharedMaterial = wm;

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.25f; sun.color = new Color(1f, .96f, .88f); sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.62f, .6f, .72f);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera"; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(1f, .9f, .6f);
            cam.fieldOfView = 32; cam.transform.position = new Vector3(0, 1.55f, -5.6f); cam.transform.rotation = Quaternion.Euler(9, 0, 0);
            cam.gameObject.AddComponent<AudioListener>();

            var dir = new GameObject("Showcase").AddComponent<SculptShowcaseDirector>();
            var names = MamaSculptKids.Names;
            dir.kids = new Animator[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Art/Sculpted/" + names[i] + "/" + names[i] + "_Sculpt.fbx");
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/MamasComing/Prefabs/Characters/Sculpted/" + names[i] + "_Sculpt.controller");
                if (!fbx) { Debug.LogError("[Mama's Coming] missing sculpted " + names[i]); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                go.name = "CHR_" + names[i] + "_Sculpt";
                var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                an.runtimeAnimatorController = ctrl;
                go.transform.position = new Vector3((i - 2) * 1.35f, 0, 0);
                go.transform.rotation = Quaternion.Euler(0, 180f + (i - 2) * -8f, 0);   // face the camera
                dir.kids[i] = an;
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Mama's Coming] sculpt showcase written to " + ScenePath);
        }
    }
}
