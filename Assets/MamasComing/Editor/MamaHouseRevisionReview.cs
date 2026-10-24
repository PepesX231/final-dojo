using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace MamasComing.Editor
{
    public static class MamaHouseRevisionReview
    {
        const string Folder="DevBridge/house-revision/";
        static IEnumerator routine;static double next;static int passed,oldFruit;static bool hadFruit;
        static Keyboard keyboard;static HousePlayground lab;
        static readonly List<string> checks=new List<string>(),errors=new List<string>();
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldFocus;
        public static void StartFromEditor(){SessionState.SetBool("MamaHomeV2Review",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Hook(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MamaHomeV2Review",false)){SessionState.SetBool("MamaHomeV2Review",false);EditorApplication.delayCall+=Run;}};}
        public static void Run()
        {
            lab=Object.FindFirstObjectByType<HousePlayground>();if(!EditorApplication.isPlaying||!lab)throw new InvalidOperationException("Open study in Play mode.");
            Directory.CreateDirectory(Folder);checks.Clear();errors.Clear();passed=0;hadFruit=PlayerPrefs.HasKey("mama.kid");oldFruit=KidLook.Selected;
            oldBackground=InputSystem.settings.backgroundBehavior;oldFocus=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();Application.logMessageReceivedThreaded+=Log;routine=Checks();next=0;EditorApplication.update+=Tick;
        }
        static void Log(string m,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)lock(errors)errors.Add(m);}
        static void Check(bool ok,string description){if(!ok)throw new Exception(description);passed++;checks.Add("PASS "+description);File.WriteAllLines(Folder+"progress.txt",checks);}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static void Teleport(Vector3 p){var cc=lab.player.GetComponent<CharacterController>();cc.enabled=false;lab.player.transform.position=p;cc.enabled=true;lab.player.Motor.Launch(Vector3.zero);Physics.SyncTransforms();}
        static Transform Foot(PlayerController pc,bool left)
        {
            string side=left?"L":"R";
            var all=pc.fallPivot.GetComponentsInChildren<Transform>();
            return all.FirstOrDefault(t=>t.name=="Foot_"+side)??all.FirstOrDefault(t=>t.name.Contains("duck foot "+side))??all.FirstOrDefault(t=>t.name=="Leg_"+side+"_Lower");
        }
        static IEnumerator Checks()
        {
            yield return .5f;var pc=lab.player;
            Check(lab.chores.Length==50,"Exactly 50 authored prototype chores in scene");
            var floors=Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None).Where(c=>c.name.StartsWith("Floor •")).ToArray();
            float area=floors.Sum(c=>c.bounds.size.x*c.bounds.size.z);checks.Add("MEASURE floor area="+area+" previous=259 ratio="+area/259f);
            Check(area>=777&&area<850,"Usable floor area is approximately three times the previous house");
            Check(lab.roof&&lab.roof.GetComponentsInChildren<Renderer>().Length>50,"Physical pitched roofs, gables, eaves and chimney authored");
            Keys(Key.F10);yield return .25f;Keys();yield return .15f;
            Check(lab.Exterior,"F10 opens complete roofed exterior");Capture("exterior.png",lab.planCamera);
            Check(lab.roof.GetComponentsInChildren<Renderer>().All(r=>r.enabled),"Exterior mode shows every roof surface");
            Keys(Key.F10);yield return .2f;Keys();yield return .12f;
            Check(!lab.Exterior&&lab.roof.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Interior cutaway hides existing roof without deleting it");
            foreach(var chair in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Chair assembly")))
            {
                var rb=chair.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).ToArray();float bottom=rb.Min(b=>b.min.y);
                Check(Mathf.Abs(bottom-chair.position.y)<.03f,"Chair legs touch their floor: "+chair.position);
            }
            foreach(var chore in lab.chores.Where(t=>t.requiredItem))
            {
                var item=chore.requiredItem;float bottom=item.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);
                var hit=Physics.RaycastAll(new Vector3(item.transform.position.x,bottom+.2f,item.transform.position.z),Vector3.down,.6f).Where(h=>!h.transform.IsChildOf(item.transform)).OrderBy(h=>h.distance).FirstOrDefault();
                Check(hit.collider&&bottom-hit.point.y<.055f&&bottom-hit.point.y>-.02f,"Pickup rests on a real surface: "+chore.displayName);
            }
            var fruitKeys=new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5};
            for(int id=0;id<5;id++)
            {
                Teleport(new Vector3(7.2f,.06f,8.7f));Keys(fruitKeys[id]);yield return .2f;Keys();yield return .2f;
                Check(KidLook.Of(pc)==id,"Fruit key selects "+KidLook.Names[id]);
                var l=Foot(pc,true);var r=Foot(pc,false);Check(l&&r,"Both foot controls found for "+KidLook.Names[id]);
                if(id>0)Check(pc.GetComponentInChildren<ReadableFruitGait>(),"Explicit foot stride layer active: "+KidLook.Names[id]);
                var motor=pc.Motor;var camera=motor.CameraTransform;motor.CameraTransform=null;
                float min=999,max=-999,liftMin=999,liftMax=-999,lean=0;Vector3 start=pc.transform.position;
                Keys(Key.W);
                for(int f=0;f<8;f++)
                {
                    yield return .12f;
                    float separation=Vector3.Dot(l.position-r.position,Vector3.forward);min=Mathf.Min(min,separation);max=Mathf.Max(max,separation);
                    float ly=l.position.y-pc.transform.position.y;liftMin=Mathf.Min(liftMin,ly);liftMax=Mathf.Max(liftMax,ly);lean=Mathf.Max(lean,Quaternion.Angle(Quaternion.identity,pc.fallPivot.localRotation));
                    var captureCam=lab.orbit.GetComponent<Camera>();var cp=captureCam.transform.position;var cr=captureCam.transform.rotation;
                    captureCam.transform.position=pc.transform.position+pc.transform.forward*2.0f+pc.transform.right*1.2f+Vector3.up*.9f;captureCam.transform.LookAt(pc.transform.position+Vector3.up*.55f);
                    Capture("walk-"+KidLook.Names[id]+"-"+f.ToString("00")+".png",captureCam,960,640);captureCam.transform.SetPositionAndRotation(cp,cr);
                }
                Keys();motor.CameraTransform=camera;
                checks.Add("MEASURE "+KidLook.Names[id]+" stride="+(max-min).ToString("F3")+" lift="+(liftMax-liftMin).ToString("F3")+" body sway="+lean.ToString("F2"));
                Check(Vector3.Distance(start,pc.transform.position)>1,"W moves "+KidLook.Names[id]);
                Check(max-min>.10f,"Feet alternate forward/back rather than slide: "+KidLook.Names[id]);
                Check(liftMax-liftMin>.025f,"Swing foot visibly lifts: "+KidLook.Names[id]);
                Check(lean>2f,"Visible whole-body sway: "+KidLook.Names[id]);
                Teleport(new Vector3(7,.06f,9.4f));pc.transform.rotation=Quaternion.LookRotation(Vector3.right);yield return .15f;
                Keys(Key.F8);yield return .1f;Keys();yield return 1f;
                Check(pc.Body.RolledDegrees>150&&pc.Body.LimbsOut<.05f,"Whole-fruit tumble retained: "+KidLook.Names[id]);yield return 1.5f;
            }
            var m=pc.Motor;var cam=m.CameraTransform;m.CameraTransform=null;m.AutoStep=false;m.InputEnabled=true;Teleport(new Vector3(2,.06f,11.5f));
            for(int i=0;i<160;i++)m.Step(1f/60,new PlayerCommand{Move=Vector2.up});
            checks.Add("MEASURE top landing="+pc.transform.position);Check(pc.transform.position.y>2.85f&&pc.transform.position.z>18,"Walk upstairs without jumping or teleporting");
            for(int i=0;i<160;i++)m.Step(1f/60,new PlayerCommand{Move=Vector2.down});
            Check(pc.transform.position.y<.3f,"Walk downstairs without falling through stair ramp");m.AutoStep=true;m.CameraTransform=cam;
            // Real E input: pick up and deliver one cushion before direct contract checks below.
            var cushion=lab.chores[3];var objectPos=cushion.requiredItem.transform.position;
            Teleport(new Vector3(objectPos.x,.06f,objectPos.z-.9f));pc.transform.rotation=Quaternion.identity;yield return .2f;
            Keys(Key.E);yield return .15f;Keys();yield return .55f;Check(pc.Held==cushion.requiredItem,"E actually picks up the cushion");
            var target=cushion.transform.position;Teleport(new Vector3(target.x,.06f,target.z+.95f));pc.transform.rotation=Quaternion.Euler(0,180,0);yield return .2f;
            Keys(Key.E);yield return .15f;Keys();yield return .2f;Check(cushion.Complete&&pc.Held==null,"E actually delivers the cushion onto the sofa");
            var pending=lab.chores.Where(t=>!t.Complete).ToList();int safety=0;
            while(pending.Count>0&&safety++<60)
            {
                bool progressed=false;
                foreach(var task in pending.ToArray())
                {
                    if(task.prerequisite&&!task.prerequisite.Complete)continue;
                    if(task.gate)task.gate.Interact(pc);
                    if(task.mechanic==HouseChore.Mechanic.Delivery){task.Interact(pc);Check(!task.Complete,"Empty hands cannot complete "+task.displayName);pc.PickUp(task.requiredItem);task.Interact(pc);}
                    else if(task.mechanic==HouseChore.Mechanic.Timing){for(int i=0;i<3;i++){while(!task.CanHit)yield return .05f;task.Interact(pc);yield return .08f;}}
                    else if(task.mechanic==HouseChore.Mechanic.Sequence){for(int i=0;i<3;i++)task.Interact(pc);}
                    else task.Interact(pc);
                    Check(task.Complete,"Completion path: "+task.displayName);
                    if(task.requiredItem)
                    {
                        float bottom=task.requiredItem.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);
                        var item=task.requiredItem;
                        var support=Physics.RaycastAll(new Vector3(item.transform.position.x,bottom+.2f,item.transform.position.z),Vector3.down,.6f).Where(h=>!h.transform.IsChildOf(item.transform)).OrderBy(h=>h.distance).FirstOrDefault();
                        Check(support.collider&&bottom-support.point.y<.06f&&bottom-support.point.y>-.02f,"Delivered object has real support: "+task.displayName);
                    }
                    pending.Remove(task);progressed=true;
                }
                Check(progressed,"Dependency graph remains solvable");
            }
            Check(lab.chores.All(t=>t.Complete),"All 50 prototype task completion paths work");
            Teleport(lab.spawn);yield return .2f;
            var upstairsDeliveries=lab.chores.Where(t=>t.requiredItem&&t.requiredItem.transform.position.y>2.5f).ToArray();
            Check(upstairsDeliveries.All(t=>t.requiredItem.GetComponentsInChildren<Renderer>().All(r=>!r.enabled)),"Returned upstairs objects are hidden from downstairs: no floating cutaway props");
            Keys(Key.F9);yield return .2f;Keys();yield return .15f;Capture("plan-ground.png",lab.planCamera);
            Teleport(new Vector3(9,3.06f,8.5f));yield return .25f;
            Check(upstairsDeliveries.All(t=>t.requiredItem.GetComponentsInChildren<Renderer>().All(r=>r.enabled)),"Returned upstairs objects become visible on their actual floor");
            Capture("plan-upper.png",lab.planCamera);
            lab.SetPlan(false);Teleport(lab.spawn);yield return .3f;Capture("entry-interior.png",lab.orbit.GetComponent<Camera>());
            Check(errors.Count==0,"No runtime errors/exceptions in revision review");
        }
        static void Capture(string name,Camera camera,int width=1440,int height=1000)
        {
            var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24);
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tx=new Texture2D(width,height,TextureFormat.RGB24,false);tx.ReadPixels(new Rect(0,0,width,height),0,0);tx.Apply();File.WriteAllBytes(Folder+name,tx.EncodeToPNG());Object.DestroyImmediate(tx);}
            finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
        }
        static void Tick(){if(!EditorApplication.isPlaying){Finish("ABORTED");return;}if(EditorApplication.timeSinceStartup<next)return;try{if(!routine.MoveNext()){Finish("PASSED");return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.1f);}catch(Exception e){checks.Add(e.ToString());Finish("FAILED");}}
        static void Finish(string result)
        {
            EditorApplication.update-=Tick;Application.logMessageReceivedThreaded-=Log;if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldFocus;KidLook.Selected=oldFruit;if(!hadFruit)PlayerPrefs.DeleteKey("mama.kid");
            if(lab&&lab.player){lab.player.Motor.AutoStep=true;lab.player.Motor.CameraTransform=lab.orbit.transform;}
            checks.Insert(0,result+" "+passed+" checks "+DateTime.Now.ToString("s")+"; staged input / contract tests, not a human or online playtest");lock(errors)checks.AddRange(errors.Select(s=>"ERROR "+s));File.WriteAllLines(Folder+"review.txt",checks);Debug.Log("[HouseV2Review] "+result+" "+passed);
        }
        public static void InspectRigs()
        {
            Directory.CreateDirectory("DevBridge/house-revision");
            var lines = new System.Collections.Generic.List<string>();
            foreach(var n in new[]{"Banana","Orange","Watermelon","WaxApple"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/PF_Kid_"+n+".prefab");
                lines.Add("FRUIT "+n);
                foreach(var t in prefab.GetComponentsInChildren<Transform>(true)) if(!t.name.Contains("InkOutline")) lines.Add(t.name+" parent="+(t.parent?t.parent.name:"")+" pos="+t.localPosition);
                foreach(var a in prefab.GetComponentsInChildren<Animator>(true))
                    foreach(var clip in a.runtimeAnimatorController.animationClips.Distinct())
                    {
                        lines.Add("CLIP "+clip.name+" duration="+clip.length);
                        foreach(var b in AnimationUtility.GetCurveBindings(clip).Take(25))lines.Add(b.path+"."+b.propertyName);
                    }
            }
            File.WriteAllLines("DevBridge/house-revision/rigs.txt",lines);
        }
        public static void InspectRoof()
        {
            var lines=new List<string>();
            foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.name.Contains("gables")))
            {
                var mesh=mf.sharedMesh;var rend=mf.GetComponent<Renderer>();lines.Add(mf.name+" vertices="+mesh.vertexCount+" normals="+string.Join(";",mesh.normals.Select(n=>n.ToString()))+" tris="+string.Join(",",mesh.triangles)+" material="+rend.sharedMaterial.name+" color="+rend.sharedMaterial.GetColor("_BaseColor"));
            }
            File.WriteAllLines(Folder+"roof-diagnostic.txt",lines);
        }
    }
}
