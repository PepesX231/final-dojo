using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace PantryBanditLab.Editor
{
    public static class PantryShowcaseBuilder
    {
        const string Root="Assets/Showcases/PantryBandit";
        public const string ScenePath=Root+"/Scenes/PantryMotionLab.unity";
        [MenuItem("Showcases/Pantry Bandit/Build Isolated Motion Lab")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save current scene edits first.");
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            foreach(string name in new[]{"Pip","Cheese","MilkCarton"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Models/"+name+".fbx");importer.globalScale=1;importer.importAnimation=name=="Pip";
                if(name=="Pip")
                {
                    importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                    var clips=importer.defaultClipAnimations;
                    foreach(var c in clips){string n=PantryMotionLab.Clips.FirstOrDefault(k=>c.name.Contains("AN_Pip_"+k));if(n!=null)c.name="AN_Pip_"+n;c.loopTime=n=="Idle"||n=="Walk"||n=="Run"||n=="Cheer";}
                    importer.clipAnimations=clips;
                }
                importer.SaveAndReimport();
            }
            var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Pip_Palette.png");
            foreach(string n in new[]{"Soft","Fabric","Boot","Eye","Metal"})
            {
                var m=Mat("Pip_"+n,Color.white);m.SetTexture("_BaseMap",palette);m.SetFloat("_Smoothness",n=="Eye"?.82f:n=="Fabric"?.18f:n=="Metal"?.7f:.4f);m.SetFloat("_Metallic",n=="Metal"?.6f:0);EditorUtility.SetDirty(m);
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var actor=Import("Pip",Vector3.zero);actor.name="Pip • original rigged mouse";
            var animator=actor.GetComponentInChildren<Animator>();if(!animator)animator=actor.AddComponent<Animator>();
            string controllerPath=Root+"/PipMotion.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;foreach(var child in machine.states)machine.RemoveState(child.state);
            var animations=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/Pip.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            foreach(string kind in PantryMotionLab.Clips)
            {
                var clip=animations.FirstOrDefault(c=>c.name=="AN_Pip_"+kind);if(!clip)throw new InvalidOperationException("Missing imported action "+kind+"; found "+string.Join(",",animations.Select(c=>c.name)));
                var state=machine.AddState("AN_Pip_"+kind);state.motion=clip;state.writeDefaultValues=true;if(kind=="Idle")machine.defaultState=state;
            }
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var smr in actor.GetComponentsInChildren<SkinnedMeshRenderer>())smr.updateWhenOffscreen=true;
            PrefabUtility.SaveAsPrefabAsset(actor,Root+"/Prefabs/Pip.prefab");
            var navy=Mat("Studio_Navy",Hex("173746"));var gold=Mat("Studio_Brass",Hex("C8A56C"));var paper=Mat("Studio_Cream",Hex("F3E5CC"));
            Cylinder("Main motion plinth",new Vector3(0,-.10f,0),new Vector3(4.5f,.2f,4.5f),navy);
            Cylinder("Brass rim",new Vector3(0,-.19f,0),new Vector3(4.58f,.035f,4.58f),gold);
            Box("Backdrop floor",new Vector3(0,-.27f,0),new Vector3(500,.1f,500),navy);
            Cylinder("Cheese display",new Vector3(-1.25f,.12f,.1f),new Vector3(.9f,.24f,.9f),gold);
            var cheeseDisplay=Import("Cheese",new Vector3(-1.25f,.24f,.1f));SitOn(cheeseDisplay,.24f);
            var cheese=Import("Cheese",new Vector3(0,.32f,.5f));cheese.transform.localScale=Vector3.one*1.45f;SitOn(cheese,.32f);
            Cylinder("Snatch practice platter",new Vector3(0,.16f,.5f),new Vector3(.55f,.32f,.55f),gold);
            var actionPlinth=GameObject.Find("Snatch practice platter");
            Cylinder("Carton display",new Vector3(1.25f,.12f,.2f),new Vector3(.9f,.24f,.9f),gold);
            var carton=Import("MilkCarton",new Vector3(1.25f,.24f,.2f));SitOn(carton,.24f);
            Label("P I P",new Vector3(-1.25f,1.65f,.4f),.2f,paper);
            Label("THE PANTRY BANDIT",new Vector3(-1.25f,1.44f,.4f),.035f,paper);
            Label("ORIGINAL / 01",new Vector3(1.25f,1.1f,.4f),.06f,paper);
            Label("MODEL + MOTION STUDY",new Vector3(1.25f,.98f,.4f),.03f,paper);
            var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.tag="MainCamera";cam.nearClipPlane=.03f;cam.farClipPlane=500;cam.orthographic=true;cam.orthographicSize=1.9f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Hex("173746");cam.transform.position=new Vector3(1.8f,2.3f,5.8f);cam.transform.LookAt(new Vector3(0,.85f,0));
            var sun=new GameObject("Warm studio key",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.5f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.45f;sun.color=Hex("FFE3BF");sun.transform.rotation=Quaternion.Euler(38,-35,0);
            Light("Cool rim",new Vector3(-2,3,-2),Hex("9DDFDE"),2,7);Light("Portrait fill",new Vector3(2,2,3),Hex("FFF0D3"),1,7);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("A9C4D1");RenderSettings.ambientEquatorColor=Hex("718C97");RenderSettings.ambientGroundColor=Hex("344B5E");RenderSettings.fog=false;
            var lab=new GameObject("Pantry Motion Lab • isolated showcase").AddComponent<PantryMotionLab>();lab.actor=animator;lab.character=actor.transform;lab.cheese=cheese.transform;lab.view=cam;lab.actionPlinth=actionPlinth;
            lab.handLeft=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="hand.L");lab.handRight=actor.GetComponentsInChildren<Transform>().First(t=>t.name=="hand.R");
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(0,.8f,0),Quaternion.Euler(12,195,0),3.7f);
            Debug.Log("[PantryLab] Saved isolated scene with original Pip, cheese, milk carton and six imported skeletal clips.");
        }
        static GameObject Import(string name,Vector3 pos)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx"));g.transform.position=pos;
            foreach(var r in g.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Pip_"+(m&&m.name.Contains("Fabric")?"Fabric":m&&m.name.Contains("Eye")?"Eye":m&&m.name.Contains("Metal")?"Metal":m&&m.name.Contains("Boot")?"Boot":"Soft")+".mat")).ToArray();return g;
        }
        static void SitOn(GameObject g,float y){var rs=g.GetComponentsInChildren<Renderer>();float bottom=rs.Min(r=>r.bounds.min.y);g.transform.position+=Vector3.up*(y-bottom+.005f);}
        static Material Mat(string name,Color color){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.3f);return m;}
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static GameObject Box(string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
        static void Cylinder(string name,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.position=p;g.transform.localScale=new Vector3(s.x,s.y*.5f,s.z);g.GetComponent<Renderer>().sharedMaterial=m;}
        static void Light(string name,Vector3 p,Color c,float intensity,float range){var l=new GameObject(name,typeof(UnityEngine.Light)).GetComponent<UnityEngine.Light>();l.type=LightType.Point;l.transform.position=p;l.color=c;l.intensity=intensity;l.range=range;}
        static void Label(string text,Vector3 p,float size,Material m){var g=new GameObject(text,typeof(TextMesh));g.transform.position=p;g.transform.rotation=Quaternion.Euler(0,180,0);var t=g.GetComponent<TextMesh>();t.text=text;t.characterSize=size*.14f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.color=m.GetColor("_BaseColor");}
    }
}
