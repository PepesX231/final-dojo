using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace MamasComing.Editor
{
    /// Targeted authoring for the party flow. Never rebuilds the kitchen or replaces its rooms.
    public static class MamaPartyFlowAuthoring
    {
        const string Root="Assets/MamasComing/";
        static Material Mat(string name,Color c)
        {
            string folder=Root+"FrontEnd/Results";Directory.CreateDirectory(folder);
            string path=folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=c;AssetDatabase.CreateAsset(m,path);}return m;
        }
        static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material mat,bool collision=true)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        public static string Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the open scene before authoring.");
            string original=SceneManager.GetActiveScene().path;
            var kitchen=EditorSceneManager.OpenScene(Root+"Scenes/MamasKitchen.unity");
            var fridge=UnityEngine.Object.FindFirstObjectByType<FridgeHazard>();
            if(!fridge.rottenFood)
            {
                var mat=Mat("Spoiled green",new Color(.40f,.48f,.12f));var bowl=Mat("Bowl cream",MamaTheme.Cream);
                var root=new GameObject("RottenFood");root.transform.SetParent(fridge.transform,false);root.transform.localPosition=new Vector3(0,1.05f,-.56f);
                var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.mass=.3f;
                Shape("Bowl",PrimitiveType.Cylinder,root.transform,Vector3.zero,new Vector3(.38f,.035f,.38f),bowl);
                for(int i=0;i<4;i++)Shape("Spoiled food",PrimitiveType.Sphere,root.transform,new Vector3((i%2-.5f)*.13f,.06f,(i/2-.5f)*.13f),new Vector3(.18f,.11f,.18f),mat,false);
                var food=root.AddComponent<RottenFood>();food.fridge=fridge;food.displayName="Rotten food";food.startKinematic=true;food.homeSet=true;food.homePosition=root.transform.position;food.homeRotation=root.transform.rotation;food.interactRadius=1.8f;food.holdOffset=Vector3.zero;
                fridge.rottenFood=food;root.SetActive(false);
            }
            // Existing fridge had an animated door over a solid cube, with stationary handles/notes.
            var body=fridge.transform.Find("Body");var bodyMat=body.GetComponent<Renderer>().sharedMaterial;
            body.localPosition=new Vector3(0,1,.37f);body.localScale=new Vector3(1,2,.11f);
            if(!fridge.transform.Find("Shell left"))
            {
                Shape("Shell left",PrimitiveType.Cube,fridge.transform,new Vector3(-.47f,1,0),new Vector3(.06f,2,.85f),bodyMat);
                Shape("Shell right",PrimitiveType.Cube,fridge.transform,new Vector3(.47f,1,0),new Vector3(.06f,2,.85f),bodyMat);
                Shape("Shell top",PrimitiveType.Cube,fridge.transform,new Vector3(0,1.97f,0),new Vector3(1,.06f,.85f),bodyMat);
                Shape("Shell bottom",PrimitiveType.Cube,fridge.transform,new Vector3(0,.03f,0),new Vector3(1,.06f,.85f),bodyMat);
                for(int i=0;i<3;i++)Shape("Shelf "+i,PrimitiveType.Cube,fridge.transform,new Vector3(0,.45f+i*.52f,-.02f),new Vector3(.89f,.035f,.7f),bodyMat);
            }
            var inside=fridge.transform.Find("Inside");inside.localPosition=new Vector3(0,1,.30f);
            foreach(var child in fridge.transform.Cast<Transform>().ToArray()) if(child.name=="Handle" || child.name=="Note" || child.name=="Split")child.SetParent(fridge.door,true);
            fridge.openAngle=100f;
            EditorUtility.SetDirty(fridge);EditorSceneManager.MarkSceneDirty(kitchen);EditorSceneManager.SaveScene(kitchen);
            CaptureTips();
            // Copy only the existing working player into the new results stage.
            var gm=UnityEngine.Object.FindFirstObjectByType<GameManager>();
            var player=UnityEngine.Object.Instantiate(gm.player.gameObject);player.name="Results local player";
            var ranking=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.MoveGameObjectToScene(player,ranking);SceneManager.SetActiveScene(ranking);
            var stage=new GameObject("After-party award corner");
            var teal=Mat("Stage teal",new Color(.16f,.40f,.38f));var wood=Mat("Stage walnut",new Color(.30f,.16f,.09f));var cream=Mat("Stage cream",MamaTheme.Cream);var coral=Mat("Stage coral",MamaTheme.Brick);var gold=Mat("Stage gold",MamaTheme.Mustard);var dark=Mat("Stage background",new Color(.06f,.15f,.16f));
            Shape("Floor",PrimitiveType.Cube,stage.transform,new Vector3(1.5f,-.25f,1),new Vector3(18,.5f,14),wood);
            Shape("Round living-room rug",PrimitiveType.Cylinder,stage.transform,new Vector3(2,.025f,.9f),new Vector3(9,.025f,6),teal);
            Shape("Backdrop",PrimitiveType.Cube,stage.transform,new Vector3(2,3.5f,5),new Vector3(17,8,.25f),dark);
            for(int i=0;i<3;i++)
            {
                Vector3 p=MamaRankingScreen.SpawnFor(i);p.y=.1f;
                Shape("Award plinth "+(i+1),PrimitiveType.Cylinder,stage.transform,p,new Vector3(1.7f,.1f,1.5f),i==0?gold:i==1?teal:coral);
                Shape("Cream plinth top "+(i+1),PrimitiveType.Cylinder,stage.transform,p+Vector3.up*.105f,new Vector3(1.62f,.018f,1.42f),cream);
            }
            // A handmade bunting line and framed family notes fit the existing paper-and-wood UI.
            for(int i=0;i<13;i++)
            {
                var flag=Shape("Party bunting "+i,PrimitiveType.Cube,stage.transform,new Vector3(-4+i*.9f,4.3f-Mathf.Sin(i/12f*Mathf.PI)*.55f,4.7f),new Vector3(.55f,.58f,.05f),i%3==0?coral:i%3==1?gold:teal,false);
                flag.transform.localRotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*12);
            }
            for(int side=0;side<2;side++)
            {
                float x=side==0?-1.8f:6.5f;
                Shape("Plant pot",PrimitiveType.Cylinder,stage.transform,new Vector3(x,.4f,2.8f),new Vector3(.75f,.4f,.75f),coral);
                for(int j=0;j<5;j++){var leaf=Shape("Leaf",PrimitiveType.Sphere,stage.transform,new Vector3(x+(j-2)*.16f,1.3f+j%2*.25f,2.8f),new Vector3(.3f,1.2f,.16f),teal,false);leaf.transform.localRotation=Quaternion.Euler(0,0,(j-2)*-18);}
            }
            var camGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));camGO.tag="MainCamera";var cam=camGO.GetComponent<Camera>();cam.transform.position=new Vector3(0,4.4f,-11.8f);cam.transform.LookAt(new Vector3(.45f,1.35f,.8f));cam.fieldOfView=43;cam.backgroundColor=new Color(.08f,.18f,.18f);cam.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Warm ceiling light",typeof(Light));var light=key.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.7f;light.color=new Color(1,.87f,.69f);light.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(40,-25,0);
            var fill=new GameObject("Soft fill",typeof(Light));var fl=fill.GetComponent<Light>();fl.type=LightType.Directional;fl.intensity=.65f;fl.color=new Color(.6f,.85f,1f);fill.transform.rotation=Quaternion.Euler(25,150,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.53f,.5f);RenderSettings.fog=false;
            var screen=new GameObject("Ranking flow").AddComponent<MamaRankingScreen>();screen.player=player.GetComponent<PlayerController>();screen.player.controlsEnabled=false;player.transform.position=MamaRankingScreen.SpawnFor(0);player.transform.rotation=Quaternion.Euler(0,180,0);
            EditorSceneManager.SaveScene(ranking,Root+"Scenes/MC_Ranking.unity");EditorSceneManager.CloseScene(ranking,true);
            var loading=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Loading • persistent tip curtain is owned by NetSession");
            var loadingCam=new GameObject("Loading camera",typeof(Camera));loadingCam.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;loadingCam.GetComponent<Camera>().backgroundColor=new Color(.085f,.15f,.16f);
            EditorSceneManager.SaveScene(loading,Root+"Scenes/MC_Loading.unity");
            foreach(string name in new[]{"MC_MainMenu","MC_Lobby"})
            {
                var s=EditorSceneManager.OpenScene(Root+"Scenes/"+name+".unity");
                foreach(var title in UnityEngine.Object.FindObjectsByType<MamaTitleScreen>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var kept=title.items.Where(x=>x&&x.action!=MamaMenuItem.Action.Coop).ToArray();
                    foreach(var it in title.items)if(it&&it.action==MamaMenuItem.Action.Coop)it.gameObject.SetActive(false);
                    if(kept.Length>1){var first=kept[0].transform.localPosition;var last=kept[kept.Length-1].transform.localPosition;for(int i=0;i<kept.Length;i++)kept[i].transform.localPosition=Vector3.Lerp(first,last,i/(float)(kept.Length-1));}
                    title.items=kept;title.startItem=kept.Length-1;EditorUtility.SetDirty(title);
                }
                foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(!text.text.Contains("ENTER start"))continue;
                    text.text="Q / E fruit  •  ESC back";var loc=text.GetComponent<MamaLocText>();if(loc){loc.en="Q / E fruit  •  ESC back";loc.th="Q / E เปลี่ยนผลไม้  •  ESC กลับ";}
                }
                EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
            }
            var builds=EditorBuildSettings.scenes.ToList();foreach(string name in new[]{"MC_Loading","MC_Ranking"})if(!builds.Any(s=>s.path.EndsWith("/"+name+".unity")))builds.Add(new EditorBuildSettingsScene(Root+"Scenes/"+name+".unity",true));EditorBuildSettings.scenes=builds.ToArray();
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();EditorSceneManager.OpenScene(original);return "Updated kitchen interactions, title/lobby, authored MC_Loading and MC_Ranking, and captured 6 tip photos.";
        }
        static void CaptureTips()
        {
            string folder=Root+"Resources/MamaTips";Directory.CreateDirectory(folder);
            var camGO=new GameObject("Tip capture",typeof(Camera));var cam=camGO.GetComponent<Camera>();cam.CopyFrom(Camera.main);cam.tag="Untagged";cam.enabled=false;cam.fieldOfView=43;
            var gm=UnityEngine.Object.FindFirstObjectByType<GameManager>();var fridge=UnityEngine.Object.FindFirstObjectByType<FridgeHazard>();
            var subjects=new[]{("fire",gm.stove.transform.position), ("water",gm.tap.transform.position),("pot",gm.pot.transform.position),("fridge",fridge.transform.position),("power",UnityEngine.Object.FindFirstObjectByType<BreakerPanel>().transform.position),("team",gm.player.transform.position)};
            var target=new RenderTexture(1024,576,24);var old=RenderTexture.active;
            foreach(var subject in subjects)
            {
                Vector3 aim=subject.Item2+Vector3.up*.5f;cam.transform.position=aim+new Vector3(2.7f,4.2f,-3.8f);cam.transform.LookAt(aim);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
                var tex=new Texture2D(1024,576,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1024,576),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+subject.Item1+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            }
            RenderTexture.active=old;cam.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camGO);
        }
    }
}
