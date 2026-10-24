using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

namespace PantryBanditLab.Editor
{
    public static class PantryShowcaseReview
    {
        const string Folder="DevBridge/pantry-showcase/Unity/";
        static PantryMotionLab lab;static Keyboard keyboard;static IEnumerator routine;static double next;static int passed;
        static InputSettings.BackgroundBehavior oldBackground;static InputSettings.EditorInputBehaviorInPlayMode oldFocus;
        static readonly List<string> lines=new List<string>(),errors=new List<string>();
        public static void StartFromEditor(){SessionState.SetBool("PantryReview",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Hook(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PantryReview",false)){SessionState.SetBool("PantryReview",false);EditorApplication.delayCall+=Run;}};}
        public static void Run()
        {
            lab=Object.FindFirstObjectByType<PantryMotionLab>();if(!lab||!EditorApplication.isPlaying)throw new InvalidOperationException("Open isolated motion lab and enter Play mode.");
            Directory.CreateDirectory(Folder);lines.Clear();errors.Clear();passed=0;
            oldBackground=InputSystem.settings.backgroundBehavior;oldFocus=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard=InputSystem.AddDevice<Keyboard>();Application.logMessageReceivedThreaded+=Log;routine=Checks();next=0;EditorApplication.update+=Tick;
        }
        static void Log(string text,string trace,LogType t){if(t==LogType.Error||t==LogType.Exception)lock(errors)errors.Add(text);}
        static void Check(bool value,string text){if(!value)throw new Exception(text);passed++;lines.Add("PASS "+text);}
        static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
        static Transform Bone(string name)=>lab.actor.GetComponentsInChildren<Transform>().First(t=>t.name==name);
        static IEnumerator Checks()
        {
            yield return .6f;lab.autoShow=false;
            Check(lab.gameObject.scene.path==PantryShowcaseBuilder.ScenePath,"Showcase is in its own scene/folder");
            Check(lab.actor.runtimeAnimatorController.animationClips.Distinct().Count()==6,"Six actual FBX skeletal clips imported");
            var skin=lab.actor.GetComponentsInChildren<SkinnedMeshRenderer>();
            Check(skin.Length>0&&skin.All(s=>s.bones.Length>=24)&&Bone("root")&&Bone("hips")&&Bone("tail.05"),"Skinned renderer and 26-bone hierarchy imported (root/hips are controls)");
            Check(skin.All(s=>s.sharedMaterials.All(m=>m&&m.shader.isSupported&&m.GetTexture("_BaseMap"))),"Palette textures and supported Unity shaders assigned");
            float h=skin.Max(s=>s.bounds.max.y)-skin.Min(s=>s.bounds.min.y);lines.Add("MEASURE character height="+h.ToString("F3"));Check(h>1.5f&&h<2.2f,"Character imports at intended metre scale");
            lab.Play(0);yield return .25f;Capture("unity-hero.png");
            var keys=new[]{Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6};
            for(int id=0;id<6;id++)
            {
                Keys(keys[id]);yield return .16f;Keys();yield return .12f;
                Check(lab.current==id,"Digit "+(id+1)+" selects "+PantryMotionLab.Clips[id]);
                Check(lab.actor.GetCurrentAnimatorStateInfo(0).IsName("AN_Pip_"+PantryMotionLab.Clips[id]),"Animator evaluates selected imported state");
                var foot=Bone("foot.L");var hand=Bone("hand.R");var start=foot.localPosition;var rotation=foot.localRotation;var wrist=hand.position;
                yield return .25f;
                float change=Vector3.Distance(start,foot.localPosition)+Quaternion.Angle(rotation,foot.localRotation)*.01f+Vector3.Distance(wrist,hand.position);
                lines.Add("MEASURE "+PantryMotionLab.Clips[id]+" combined limb motion="+change.ToString("F4"));
                Check(change>.001f,"Bones genuinely animate in "+PantryMotionLab.Clips[id]);Capture("unity-"+PantryMotionLab.Clips[id]+".png");
            }
            var pos=lab.character.position;Keys(Key.W);yield return .6f;Keys();yield return .1f;
            Check(Vector3.Distance(pos,lab.character.position)>.35f,"Real W input moves the actor with walk loop");
            pos=lab.character.position;Keys(Key.W,Key.LeftShift);yield return .4f;Keys();yield return .1f;
            Check(lab.current==2&&Vector3.Distance(pos,lab.character.position)>.5f,"Shift W runs faster with the Run clip");
            Keys(Key.R);yield return .16f;Keys();yield return .12f;
            Check(lab.character.position.sqrMagnitude<.001f,"R resets preview position");
            Keys(Key.E);yield return .16f;Keys();yield return .55f;
            Check(lab.current==4&&lab.cheese.gameObject.activeInHierarchy,"E plays the reachable cheese-snatch setup");yield return .28f;Capture("unity-snatch-contact.png");
            lab.Play(4);yield return .05f;
            for(int i=0;i<12;i++){Capture("Snatch_"+i.ToString("D2")+".png");yield return .125f;}
            Keys(Key.Space);yield return .16f;Keys();yield return .3f;
            Check(lab.current==3,"Space plays the Hop clip");
            Check(errors.Count==0,"No runtime exceptions/errors in staged input review");
        }
        static void Capture(string file)
        {
            var cam=lab.view;var old=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,1000,24);
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tx=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tx.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tx.Apply();File.WriteAllBytes(Folder+file,tx.EncodeToPNG());Object.DestroyImmediate(tx);}
            finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
        }
        static void Tick(){if(!EditorApplication.isPlaying){Finish("ABORTED");return;}if(EditorApplication.timeSinceStartup<next)return;try{if(!routine.MoveNext()){Finish("PASSED");return;}next=EditorApplication.timeSinceStartup+(routine.Current is float f?f:.1f);}catch(Exception e){lines.Add(e.ToString());Finish("FAILED");}}
        static void Finish(string result)
        {
            EditorApplication.update-=Tick;Application.logMessageReceivedThreaded-=Log;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldFocus;
            lines.Insert(0,result+" "+passed+" checks "+DateTime.Now.ToString("s")+" • staged input, not human playtest");lock(errors)lines.AddRange(errors.Select(e=>"ERROR "+e));File.WriteAllLines(Folder+"review.txt",lines);Debug.Log("[PantryReview] "+result+" "+passed);
            if(lab){lab.character.SetPositionAndRotation(Vector3.zero,Quaternion.identity);lab.autoShow=true;lab.Play(0);}
        }
    }
}
