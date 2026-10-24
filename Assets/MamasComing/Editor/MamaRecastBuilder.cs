using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace MamasComing.Editor
{
    public static class MamaRecastBuilder
    {
        const string Root="Assets/MamasComing/Art/Recast";
        static Material atlas;
        static int replacements;
        public static void ResumeReview()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Resume from Edit mode");
            if(EditorSceneManager.GetActiveScene().path!="Assets/MamasComing/Scenes/MamasKitchen.unity")
                EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity");
            Apply();MamaUxReview.StartFromEditor();
        }
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring");
            var scene=EditorSceneManager.GetActiveScene();
            if(scene.path!="Assets/MamasComing/Scenes/MamasKitchen.unity")throw new InvalidOperationException("Wrong scene");
            Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
            atlas=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/MamaPainted.mat");
            if(!atlas){atlas=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(atlas,Root+"/Materials/MamaPainted.mat");}
            atlas.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/MamaPaintedAtlas.png"));atlas.SetColor("_BaseColor",Color.white);atlas.SetFloat("_Smoothness",.22f);atlas.enableInstancing=true;
            replacements=0;
            foreach(var p in UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(p.gameObject.scene!=scene)continue;
                string model=null;
                if(p is Hamster)model="PET_Hamster";
                else if(p is Fish)model="PET_Goldfish";
                else if(p.displayName=="Teddy bear")model="PROP_PlushTeddy";
                else if(p.displayName=="Trash bag")model="PROP_TrashBag";
                else if(p.displayName=="Pizza box")model="PROP_PizzaBox";
                else if(p is Extinguisher)model="PROP_Extinguisher";
                else if(p is Bucket)model="PROP_Bucket";
                else if(p is Towel)model="PROP_FoldedTowel";
                if(model==null)continue;
                var art=Replace(p.transform,model);
                if(p.displayName=="Teddy bear")
                {
                    p.transform.rotation=Quaternion.Euler(0,180,0);
                    var collider=p.GetComponent<BoxCollider>();if(collider){collider.center=new Vector3(0,.28f,0);collider.size=new Vector3(.4f,.56f,.28f);}
                }
                if(p is Towel towel)
                {
                    towel.cloth=art.GetComponentsInChildren<Renderer>();
                    if(p.TryGetComponent<CoverObject>(out var cover))cover.tintRenderers=towel.cloth;
                }
                if(p is Hamster || p is Fish || p.displayName=="Teddy bear" || p.displayName=="Trash bag" || p.displayName=="Pizza box")
                {
                    var grip=p.GetComponent<CarryGrip>()??p.gameObject.AddComponent<CarryGrip>();
                    float half=p is Hamster?.17f:p is Fish?.1f:p.displayName=="Trash bag"?.2f:p.displayName=="Teddy bear"?.2f:.15f;
                    float y=p is Fish?0:p is Hamster?.13f:p.displayName=="Teddy bear"?.22f:.12f;
                    grip.left=new Vector3(-half,y,0);grip.right=new Vector3(half,y,0);
                    grip.carryAnchor=new Vector3(0,p is Hamster?.5f:p is Fish?.62f:.42f,p is Hamster?.8f:p is Fish?.7f:.72f);
                    grip.shoulderForward=p is Hamster?.55f:.45f;grip.shoulderInward=.17f;
                    p.holdOffset=Vector3.zero;p.holdEuler=Vector3.zero;p.grabTime=.6f;
                }
            }
            // Named decoration only: gameplay components, pivots, collision and particles survive.
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToArray())
            {
                if(!t || t.gameObject.scene!=scene || t.GetComponentInParent<Pickup>() || t.name.StartsWith("Art_Recast"))continue;
                if(t.name=="CakeBlob")Replace(t,"PROP_CakeSlice");
                else if(t.name.StartsWith("Pipe") && t.GetComponent<MeshFilter>() && t.GetComponent<MeshRenderer>())FitPipe(t);
                else if(t.name=="ArmchairCushion")FitMesh(t,"PROP_SeatCushion");
                else if(t.name=="Toolbox" && t.GetComponent<MeshRenderer>())FitMesh(t,"PROP_Toolbox");
                else if(t.name=="Vase" && t.GetComponent<MeshRenderer>())FitMesh(t,"PROP_CeramicVase");
                else if(t.name=="TrashBin" || t.name=="TrashBin2" || t.name=="Recycle")MakeBin(t);
            }
            var bike=GameObject.Find("Rooms/Garage");
            var wheel=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name=="BikeWheel");
            if(wheel)
            {
                var parent=wheel.transform.parent;
                foreach(var r in parent.GetComponentsInChildren<MeshRenderer>())if(r.name.StartsWith("Bike"))r.enabled=false;
                var root=parent.Find("RecastBicycle");if(!root){root=new GameObject("RecastBicycle").transform;root.SetParent(parent,false);root.position=new Vector3(29.1f,.02f,.4f);}
                Replace(root,"PROP_Bicycle");
            }
            var breaker=UnityEngine.Object.FindFirstObjectByType<BreakerPanel>();
            if(breaker)
            {
                var old=breaker.transform.Find("Box");if(old && old.GetComponent<Renderer>())old.GetComponent<Renderer>().enabled=false;
                var art=Add(breaker.transform,"PROP_BreakerPanel");art.transform.localRotation=Quaternion.Euler(0,90,0);
                art.transform.localScale=Vector3.one*1.15f;
            }
            // A recognisable pump under the existing heater, without adding a new mechanic.
            var heater=UnityEngine.Object.FindFirstObjectByType<WaterHeaterHazard>();
            if(heater)
            {
                var mount=heater.transform.Find("RecastPump");if(!mount){mount=new GameObject("RecastPump").transform;mount.SetParent(heater.transform,false);mount.localPosition=new Vector3(-.65f,0,-.15f);}
                Replace(mount,"PROP_WaterPump");
            }
            var lounge=GameObject.Find("Rooms/LivingRoom");
            var tvStand=lounge?lounge.transform.Find("TVStand"):null;
            if(tvStand && !lounge.transform.Find("RecastRouter"))
            {
                var router=new GameObject("RecastRouter").transform;router.SetParent(lounge.transform,false);
                var surface=tvStand.GetComponent<Renderer>().bounds;
                router.position=new Vector3(surface.center.x,surface.max.y+.01f,surface.max.z-.2f);Add(router,"PROP_Router");
            }
            ImportMother();VibrantHouse();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("DevBridge/mama-rebuild/authoring.txt","Recast replacements="+replacements+"\nOriginal source copies preserved. Existing gameplay roots/colliders retained; bin lid colliders intentionally changed to a hollow bin shell.\n");
            Debug.Log("Mama recast authored, replacements="+replacements);
        }

        static GameObject Add(Transform parent,string name)
        {
            var old=parent.Find("Art_Recast");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            string path=Root+"/Models/"+name+".fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model)throw new InvalidOperationException("Missing model "+path);
            var art=(GameObject)PrefabUtility.InstantiatePrefab(model);art.name="Art_Recast";art.transform.SetParent(parent,false);
            art.transform.localPosition=Vector3.zero;art.transform.localRotation=Quaternion.identity;art.transform.localScale=Vector3.one;
            foreach(var r in art.GetComponentsInChildren<MeshRenderer>())r.sharedMaterial=atlas;
            replacements++;return art;
        }
        static GameObject Replace(Transform parent,string name)
        {
            foreach(var r in parent.GetComponentsInChildren<MeshRenderer>(true))if(!r.transform.GetComponentInParent<PlayerController>())r.enabled=false;
            return Add(parent,name);
        }
        static void FitMesh(Transform old,string name)
        {
            // Match the existing world bounds while cancelling inherited primitive scale.
            var renderer=old.GetComponent<MeshRenderer>();if(!renderer)return;
            Bounds target=renderer.bounds;renderer.enabled=false;
            var art=Add(old,name);Vector3 inherited=old.lossyScale;
            art.transform.localScale=new Vector3(1/Mathf.Abs(inherited.x),1/Mathf.Abs(inherited.y),1/Mathf.Abs(inherited.z));
            Bounds b=Bounds(art);float factor=Mathf.Min(target.size.x/Mathf.Max(.001f,b.size.x),target.size.z/Mathf.Max(.001f,b.size.z));
            art.transform.localScale*=factor;art.transform.position+=target.center-Bounds(art).center;
        }
        static void FitPipe(Transform old)
        {
            var original=old.GetComponent<MeshFilter>().sharedMesh.bounds;
            old.GetComponent<MeshRenderer>().enabled=false;
            var art=Add(old,"PROP_PipeStraight");var mf=art.GetComponentInChildren<MeshFilter>();var b=mf.sharedMesh.bounds;
            var s=new Vector3(original.size.x/b.size.x,original.size.y/b.size.y,original.size.z/b.size.z);
            art.transform.localScale=s;art.transform.localPosition=original.center-Vector3.Scale(b.center,s);
        }
        static Bounds Bounds(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
        }
        static void MakeBin(Transform existing)
        {
            Vector3 position=existing.position;position.y=.02f;
            var parent=existing.parent;string key="RecastBin_"+existing.name;
            var already=parent.Find(key);if(already)return;
            if(existing.GetComponent<Renderer>())existing.GetComponent<Renderer>().enabled=false;
            if(existing.GetComponent<Collider>())existing.GetComponent<Collider>().enabled=false;
            var root=new GameObject(key).transform;root.SetParent(parent,false);root.position=position;
            Add(root,"PROP_OpenBin");
            var pivot=new GameObject("LidHinge").transform;pivot.SetParent(root,false);pivot.localPosition=new Vector3(0,.8f,-.31f);
            var lid=Add(pivot,"PROP_BinLid");
            var bin=root.gameObject.AddComponent<TrashBin>();bin.displayName="Trash bin";bin.doorPivot=pivot;bin.openAxis=Vector3.right;bin.openAngle=-100;
            bin.stashPoint=new GameObject("Bag destination").transform;bin.stashPoint.SetParent(root,false);bin.stashPoint.localPosition=new Vector3(0,.1f,0);
            for(int i=0;i<4;i++)
            {
                var wall=new GameObject("CollisionWall"+i).transform;wall.SetParent(root,false);var box=wall.gameObject.AddComponent<BoxCollider>();
                bool x=i<2;wall.localPosition=x?new Vector3(i==0?-.3f:.3f,.4f,0):new Vector3(0,.4f,i==2?-.3f:.3f);
                box.size=x?new Vector3(.04f,.8f,.58f):new Vector3(.58f,.8f,.04f);
            }
        }
        static void ImportMother()
        {
            var mom=UnityEngine.Object.FindFirstObjectByType<MomController>();if(!mom)return;
            var visual=mom.transform.Find("MotherAppleVisual");
            if(visual)UnityEngine.Object.DestroyImmediate(visual.gameObject);
            if(mom.visual)foreach(var r in mom.visual.GetComponentsInChildren<Renderer>())r.enabled=false;
            visual=new GameObject("MotherAppleVisual").transform;visual.SetParent(mom.transform,false);
            var presentation=mom.GetComponent<MotherApplePresentation>()??mom.gameObject.AddComponent<MotherApplePresentation>();
            foreach(string mood in new[]{"Kind","Angry"})
            {
                string path="Assets/MamasComing/Art/Family/Mother/MotherApple_"+mood+".fbx";
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!model)throw new InvalidOperationException("Mother model not imported");
                var root=new GameObject(mood);root.transform.SetParent(visual,false);
                var actor=(GameObject)PrefabUtility.InstantiatePrefab(model);actor.transform.SetParent(root.transform,false);
                var bounds=Bounds(actor);float scale=1.8f/bounds.size.y;
                Vector3 center=mom.transform.InverseTransformPoint(bounds.center), bottom=mom.transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                root.transform.localScale=Vector3.one*scale;root.transform.localPosition=-new Vector3(center.x,bottom.y,center.z)*scale;
                foreach(var r in actor.GetComponentsInChildren<Renderer>())
                {
                    var slots=r.sharedMaterials;
                    for(int i=0;i<slots.Length;i++)
                    {
                        var src=slots[i];if(!src)continue;string mp=Root+"/Materials/Mother_"+src.name.Replace("/","_")+".mat";
                        var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
                        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",src.HasProperty("_BaseColor")?src.GetColor("_BaseColor"):src.HasProperty("_Color")?src.GetColor("_Color"):Color.white);mat.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(mat,mp);}
                        slots[i]=mat;
                    }
                    r.sharedMaterials=slots;
                }
                if(mood=="Kind")presentation.kind=root;else presentation.angry=root;
            }
            presentation.angry.SetActive(false);mom.visual=visual;mom.head=null;
        }
        static Color C(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static void VibrantHouse()
        {
            foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/MamasComing/Art/Polish/Materials"}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));if(!m||!m.HasProperty("_BaseColor"))continue;
                string n=m.name;
                if(n.Contains("Cabinet")||n.Contains("CounterBody")||n.Contains("UpperDoor")||n.Contains("UpperCab"))m.SetColor("_BaseColor",C("27998D"));
                else if(n.Contains("Sofa"))m.SetColor("_BaseColor",C("D94B54"));
                else if(n.Contains("ArmCushion")||n.Contains("ChairCushion"))m.SetColor("_BaseColor",C("FFC338"));
                else if(n.Contains("Stripe")||n.Contains("WallK_"))m.SetColor("_BaseColor",C(n.Contains("Orange")||n.Contains("WallK_")?"E6A054":n.Contains("Mint")?"5CACA0":"D6AF87"));
                else if(n.Contains("WallL_"))m.SetColor("_BaseColor",C("5CACA0"));
                EditorUtility.SetDirty(m);
            }
            var volume=UnityEngine.Object.FindFirstObjectByType<Volume>();
            if(volume && volume.sharedProfile)
            {
                string path=Root+"/Mama_Recast_Grade.asset";var old=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if(old)volume.sharedProfile=old;
                else
                {
                    var profile=UnityEngine.Object.Instantiate(volume.sharedProfile);profile.name="Mama_Recast_Grade";
                    profile.components.Clear();foreach(var c in volume.sharedProfile.components)profile.components.Add(UnityEngine.Object.Instantiate(c));
                    AssetDatabase.CreateAsset(profile,path);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);volume.sharedProfile=profile;
                }
            }
            var sun=GameObject.Find("Sun")?.GetComponent<Light>();if(sun){sun.intensity=1.35f;sun.color=C("FFE5BB");sun.shadowStrength=.85f;}
        }
        public static void Inspect()
        {
            Directory.CreateDirectory("DevBridge/mama-rebuild");
            File.WriteAllText("DevBridge/mama-rebuild/scene-state.txt",EditorSceneManager.GetActiveScene().path+"\n"+"Playing="+EditorApplication.isPlaying+"\n"+"Recast roots="+UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(t=>t.name=="Art_Recast"));
        }
        public static void Screenshot()
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DreamRush.DevBridge.DevBridge",false)).First(t=>t!=null);
            var capture=type.GetMethod("CaptureCamera",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            capture.Invoke(null,new object[]{"DevBridge/mama-rebuild/game-view.png",true,1920,1080});
        }
    }
}
