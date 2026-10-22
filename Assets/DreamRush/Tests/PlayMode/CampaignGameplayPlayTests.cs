using System;
using System.Collections;
using System.IO;
using System.Linq;
using DreamRush.Campaign;
using DreamRush.Core;
using DreamRush.Flow;
using DreamRush.Interaction;
using DreamRush.Player;
using DreamRush.UI;
using DreamRush.World1;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace DreamRush.Tests
{
    /// <summary>Actual used world scenes only. Walk/launch/ride and use the real interaction system; never teleport or call win.</summary>
    public class CampaignGameplayPlayTests
    {
        SaveManager previous;string savePath;PlayerMotor3D motor;StoryWorldDirector director;
        Vector2 move;int jumpFrame=-100;Keyboard virtualKeyboard;
        bool previousMute,previousCinematic,previousEnding,previousBackground;InputSettings.BackgroundBehavior previousFocus;InputSettings.EditorInputBehaviorInPlayMode previousEditorFocus;
        [SetUp] public void Setup(){
            previousEditorFocus=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            previousBackground=Application.runInBackground;Application.runInBackground=true;previousFocus=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            previous=SaveManager.Instance;savePath=Path.Combine(Path.GetTempPath(),"dreamrush_campaign_"+Guid.NewGuid().ToString("N")+".json");
            var save=new SaveManager(savePath);save.Load();SaveManager.SetInstance(save);
            previousMute=Audio.AudioManager.Muted;Audio.AudioManager.Muted=true;
            previousCinematic=DreamCinematic.SkipForGameplayProbe;DreamCinematic.SkipForGameplayProbe=true;
            previousEnding=DreamEnding.SkipForGameplayProbe;DreamEnding.SkipForGameplayProbe=false;
            TitleScreen.Enabled=false;Time.timeScale=1;if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();if(UIRoot.Exists)UIRoot.Get().CloseAll();
        }
        [TearDown] public void Cleanup(){
            if(GameFlowManager.Exists){GameFlowManager.Get().ResetForTests();GameFlowManager.Get().SequenceSpeed=1;}
            if(UIRoot.Exists)UIRoot.Get().CloseAll();Time.timeScale=1;
            if(motor!=null)motor.CommandSource=null;if(virtualKeyboard!=null)InputSystem.RemoveDevice(virtualKeyboard);
            Audio.AudioManager.Muted=previousMute;DreamCinematic.SkipForGameplayProbe=previousCinematic;DreamEnding.SkipForGameplayProbe=previousEnding;
            InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorFocus;InputSystem.settings.backgroundBehavior=previousFocus;Application.runInBackground=previousBackground;
            SaveManager.SetInstance(previous);foreach(var p in new[]{savePath,savePath+".tmp",savePath+".bak"})if(File.Exists(p))File.Delete(p);
        }
        IEnumerator Load(string scene){
            SceneManager.LoadScene(scene);yield return null;yield return null;
            yield return TransitionOverlay.Get().Open(0);
            director=StoryWorldDirector.Current;motor=Object.FindFirstObjectByType<PlayerMotor3D>();
            Assert.IsNotNull(director);Assert.IsNotNull(motor);motor.CameraTransform=null;move=Vector2.zero;jumpFrame=-100;
            motor.CommandSource=new FuncCommandSource(()=>new PlayerCommand{Move=move,Sprint=true,JumpPressed=Time.frameCount==jumpFrame,JumpHeld=Time.frameCount-jumpFrame<12});
            GameFlowManager.Get().SequenceSpeed=20;
            yield return Until(()=>motor.IsGrounded&&!director.Busy,8,"arrival grounded");
            Capture(scene+"-arrival");
        }
        static IEnumerator Until(Func<bool> condition,float limit,string detail){
            float elapsed=0;while(!condition()){elapsed+=Time.unscaledDeltaTime;if(elapsed>limit)Assert.Fail("Gameplay timeout: "+detail+" cinematic="+DreamCinematic.Active+" panel="+(UIRoot.Exists?UIRoot.Get().TopPanel?.name:"none")+" scale="+Time.timeScale+" dt="+Time.unscaledDeltaTime);yield return null;}
        }
        IEnumerator Walk(Vector3 to,float tolerance=.65f,float limit=12){
            float t=0;
            while(true){
                var delta=to-motor.transform.position;var flat=new Vector2(delta.x,delta.z);
                if(flat.magnitude<=tolerance&&Mathf.Abs(delta.y)<1.6f){move=Vector2.zero;yield break;}
                move=flat.sqrMagnitude>.001f?flat.normalized*Mathf.Clamp01(flat.magnitude/2f):Vector2.zero;t+=Time.deltaTime;
                if(t>limit)Assert.Fail($"Walk stuck from {motor.transform.position:F2} to {to:F2}, grounded={motor.IsGrounded}, intent={motor.MoveIntent}, velocity={motor.Velocity}, enabled={motor.InputEnabled}, camera={motor.CameraTransform}, overlaps={string.Join(",",Physics.OverlapSphere(motor.transform.position+Vector3.up*.6f,.5f).Select(c=>c.name))}");
                yield return null;
            }
        }
        IEnumerator WalkPath(GameObject path){
            Assert.IsTrue(path.activeInHierarchy,"Progress must reveal the physical route");
            foreach(Transform step in path.transform)if(step.name=="Plank")yield return Walk(step.position+Vector3.up*.2f,.95f,6);
        }
        IEnumerator Use(StoryMechanism node){
            yield return Walk(node.transform.position-Vector3.forward*1.25f,.18f);
            move=Vector2.zero;var face=node.transform.position-motor.transform.position;face.y=0;motor.transform.rotation=Quaternion.LookRotation(face);
            var interaction=motor.GetComponent<InteractionSystem>();Assert.IsNotNull(interaction);
            float elapsed=0;
            while(!node.Completed&&!node.finaleControl&&!node.finalRest){
                yield return null;elapsed+=Time.deltaTime;
                Assert.That(elapsed,Is.LessThan(8),"Mechanism not reachable/readable: "+node.name);
                if(director.Busy)continue;
                if(node.director.world!=StoryWorldKind.BubbleBathSea){float distance=Mathf.Abs(Mathf.DeltaAngle(director.Beat*360,node.targetBeat*360))/360;if(distance>.12f)continue;}
                if(ReferenceEquals(interaction.Current,node))interaction.TryInteract();
                yield return new WaitForSeconds(.12f);
            }
            yield return Walk(node.transform.position+Vector3.right*2.1f,.5f);
        }
        IEnumerator RidePad(LaunchPad pad){
            int before=pad.Launches;float elapsed=0;
            while(pad.Launches==before){
                var delta=pad.transform.position-motor.transform.position;move=new Vector2(delta.x,delta.z).normalized;
                elapsed+=Time.deltaTime;if(elapsed>10)Assert.Fail("Could not naturally trigger launch cushion at "+pad.transform.position+" player="+motor.transform.position+" intent="+motor.MoveIntent+" velocity="+motor.Velocity+" camera="+motor.CameraTransform);
                yield return null;
            }
            move=Vector2.zero;yield return new WaitForSeconds(.25f);
            yield return Until(()=>motor.IsGrounded,9,"ballistic landing");
            Assert.That(Vector3.Distance(motor.transform.position,pad.target.position),Is.LessThan(5),"Natural launch should land on intended island");
        }
        IEnumerator Finale(){
            yield return Until(()=>!director.Busy,5,"finale reveal");
            var node=Object.FindObjectsByType<StoryMechanism>(FindObjectsSortMode.None).FirstOrDefault(n=>n.finaleControl);
            if(node!=null){
                yield return Walk(node.transform.position-Vector3.forward*1.25f,.18f);
                var interaction=motor.GetComponent<InteractionSystem>();float elapsed=0;
                while(!director.ReadyForRest){
                    var recovery=node.transform.position-Vector3.forward*1.25f-motor.transform.position;move=Vector2.ClampMagnitude(new Vector2(recovery.x,recovery.z)*.5f,.55f);
                    var d=node.transform.position-motor.transform.position;d.y=0;if(d.sqrMagnitude>.01f)motor.transform.rotation=Quaternion.LookRotation(d);
                    if(motor.IsGrounded&&director.Beat>.84f)jumpFrame=Time.frameCount+1;
                    if(director.Beat>.46f&&director.Beat<.68f&&ReferenceEquals(interaction.Current,node))interaction.TryInteract();
                    elapsed+=Time.deltaTime;if(elapsed>15)Assert.Fail("Finale could not be resolved: hits="+director.FinaleHits+" stage="+director.Stage+" sleep="+DreamRush.Dream.SleepStability.Current?.Value+" at="+motor.transform.position+" node="+node.transform.position+" current="+interaction.Current+" busy="+director.Busy+" beat="+director.Beat);yield return null;
                }
            }
            yield return Until(()=>!director.Busy,5,"rest reveal");Capture(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name+"-finale");
        }
        IEnumerator RestAndReturn(string scene){
            var rest=Object.FindObjectsByType<StoryMechanism>(FindObjectsSortMode.None).First(n=>n.finalRest);
            yield return Walk(rest.transform.position-Vector3.forward*1.25f,.18f);
            var facing=rest.transform.position-motor.transform.position;facing.y=0;motor.transform.rotation=Quaternion.LookRotation(facing);
            var interaction=motor.GetComponent<InteractionSystem>();yield return null;yield return null;
            Assert.That(interaction.Current,Is.SameAs(rest),"Manual rest must be selectable through normal E targeting");
            Assert.IsTrue(interaction.TryInteract());
            if(scene==CampaignRoute.Scenes[4]){
                yield return Until(()=>DreamEnding.Showing,10,"original ending appears after World 5");
                yield return new WaitForSecondsRealtime(3.2f);Capture("world5-original-ending");
                virtualKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.Enter));yield return new WaitForSecondsRealtime(.2f);
                InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState());
            }
            yield return Until(()=>SceneManager.GetActiveScene().name==GameFlowManager.HubScene&&GameFlowManager.Get().State==FlowState.Hub,12,"victory returns to bedroom (ending="+DreamEnding.Showing+", scene="+SceneManager.GetActiveScene().name+", flow="+GameFlowManager.Get().State+")");
            var p=new SaveManager(savePath).Load().progression;Assert.That(p.FindLevelRecord(scene)?.clears,Is.EqualTo(1));
            Debug.Log("[CAMPAIGN GAMEPLAY] completed by physical traversal + real interactions: "+scene);
        }
        [UnityTest] public IEnumerator ToyBoxMoon_WalkLaunchTuneAndRest_WritesClear(){
            string scene=CampaignRoute.Scenes[2];yield return Load(scene);
            var pads=Object.FindObjectsByType<LaunchPad>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(p=>p.transform.position.z).ToArray();
            Assert.That(pads.Length,Is.EqualTo(4));
            for(int i=0;i<3;i++){yield return RidePad(pads[i]);yield return Use(director.mechanisms[i]);Assert.That(director.Stage,Is.EqualTo(i+1));}
            yield return RidePad(pads[3]);yield return Finale();yield return RestAndReturn(scene);
        }
        [UnityTest] public IEnumerator BubbleBathSea_WalkTurnValvesRideBoatAndMoor_WritesClear(){
            string scene=CampaignRoute.Scenes[3];yield return Load(scene);yield return WalkPath(GameObject.Find("ArrivalBoardwalk"));
            for(int i=0;i<3;i++){
                yield return Use(director.mechanisms[i]);Assert.That(director.Stage,Is.EqualTo(i+1));
                yield return Until(()=>!director.Busy,5,"flow reveal");yield return WalkPath(director.unlockedPaths[i]);
            }
            float elapsed=0;var hazards=Object.FindObjectsByType<StoryPulseHazard>(FindObjectsSortMode.None);
            while(!director.ReadyForRest){
                var delta=director.boat.position-motor.transform.position;var flat=new Vector2(delta.x,delta.z);move=flat.magnitude>1?flat.normalized*.6f:Vector2.zero;
                if(motor.IsGrounded&&hazards.Any(h=>Vector3.Distance(h.transform.position,motor.transform.position)<7&&h.Phase>.6f&&h.Phase<.8f))jumpFrame=Time.frameCount+1;
                elapsed+=Time.deltaTime;if(elapsed>32)Assert.Fail("Boat did not carry Pip to the lighthouse");yield return null;
            }
            Capture("world4-boat-arrival");move=Vector2.zero;Assert.That(motor.transform.position.z,Is.GreaterThan(103),"Must actually ride to the destination");yield return RestAndReturn(scene);
        }
        [UnityTest] public IEnumerator MidnightTeacup_WalkRingSequenceFinaleAndOriginalCredits_ReturnsHome(){
            string scene=CampaignRoute.Scenes[4];yield return Load(scene);yield return WalkPath(GameObject.Find("TeaTowelArrival"));
            for(int i=0;i<3;i++){
                yield return Use(director.mechanisms[i]);Assert.That(director.Stage,Is.EqualTo(i+1));yield return Until(()=>!director.Busy,5,"bell reveal");yield return WalkPath(director.unlockedPaths[i]);
            }
            yield return Finale();yield return RestAndReturn(scene);Assert.That(DreamEnding.CompletedThisSession,Is.GreaterThan(0));
        }
        static void Capture(string name){
            var bridge=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DreamRush.DevBridge.DevBridge")).FirstOrDefault(t=>t!=null);
            var method=bridge?.GetMethod("CaptureCamera",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            if(method!=null)method.Invoke(null,new object[]{Path.GetFullPath("DevBridge/out/"+name+".png"),true,1280,720});
        }
        [UnityTest] public IEnumerator UsedWorldCinematic_EnterSkipsAndRestoresCameraInput(){
            yield return Load(CampaignRoute.Scenes[2]);DreamCinematic.SkipForGameplayProbe=false;
            var camera=Camera.main;float fov=camera.fieldOfView;var follow=Object.FindFirstObjectByType<CameraSystem.ThirdPersonCameraController>();bool enabled=follow.enabled;
            var bridge=motor.GetComponent<PlayerUIBridge>();bool locked=bridge.CutsceneLock;
            var p=motor.transform.position;
            director.StartCoroutine(DreamCinematic.Play(new DreamCinematic.Shot{from=p+new Vector3(8,5,-10),to=p+new Vector3(4,2,-6),lookAt=p+Vector3.up,seconds=8,fovFrom=60,fovTo=38,caption="TOY BOX MOON"}));
            yield return null;yield return null;
            Assert.IsTrue(DreamCinematic.Active);Assert.IsFalse(UIRoot.Get().Hud.gameObject.activeSelf);Assert.IsFalse(motor.InputEnabled);Assert.IsFalse(follow.enabled);Capture("world3-cinematic");
            virtualKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.Enter));yield return new WaitForSecondsRealtime(.2f);yield return null;
            InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState());yield return Until(()=>!DreamCinematic.Active,2,"cinematic skip");
            Assert.IsTrue(motor.InputEnabled);Assert.IsTrue(UIRoot.Get().Hud.gameObject.activeSelf);Assert.That(follow.enabled,Is.EqualTo(enabled));Assert.That(camera.fieldOfView,Is.EqualTo(fov).Within(.01f));Assert.That(bridge.CutsceneLock,Is.EqualTo(locked));
            motor.InputEnabled=false;
            yield return DreamCinematic.Play(new DreamCinematic.Shot{from=p+new Vector3(4,3,-6),to=p+new Vector3(3,2,-5),lookAt=p+Vector3.up,seconds=.15f});
            Assert.IsFalse(motor.InputEnabled,"Restore the prior disabled state too");motor.InputEnabled=true;
        }
        [UnityTest] public IEnumerator UsedWorldOne_FirstChapterTitleStartsAtOne(){
            SceneManager.LoadScene(CampaignRoute.Scenes[0]);yield return null;yield return null;
            motor=Object.FindFirstObjectByType<PlayerMotor3D>();yield return new WaitForSeconds(1.9f);
            var labels=Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Select(t=>t.text).ToArray();
            Assert.IsTrue(labels.Any(t=>t.StartsWith("CHAPTER 1")),"First playable chapter must display CHAPTER 1. Actual: "+string.Join(" | ",labels.Where(t=>t.Contains("CHAPTER"))));
        }
        [UnityTest] public IEnumerator UsedGalaxy_SequentialClearsUnlockAllFive_AfterReload(){
            for(int next=0;next<5;next++){
                if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();
                SceneManager.LoadScene(GameFlowManager.GalaxyScene);yield return null;yield return null;
                var galaxy=GalaxySelect.Current;
                for(int locked=next+1;locked<5;locked++){
                    galaxy.Select(galaxy.planets.FindIndex(p=>p.sceneName==CampaignRoute.Scenes[locked]),true);
                    Assert.IsFalse(galaxy.Confirm(),"Must not skip to world "+(locked+1));Assert.IsFalse(galaxy.Done);
                }
                galaxy.Select(galaxy.planets.FindIndex(p=>p.sceneName==CampaignRoute.Scenes[next]),true);
                Assert.IsTrue(galaxy.Confirm());Assert.That(galaxy.ChosenScene,Is.EqualTo(CampaignRoute.Scenes[next]));
                SaveManager.Instance.Data.progression.RecordClear(CampaignRoute.Scenes[next],100,2);SaveManager.Instance.Save();
                var reloaded=new SaveManager(savePath);reloaded.Load();SaveManager.SetInstance(reloaded);
            }
        }
        [UnityTest] public IEnumerator UsedWorldEnding_FastScrollShowsCreditsAndRestoresPresentation(){
            yield return Load(CampaignRoute.Scenes[4]);
            int completed=DreamEnding.CompletedThisSession;var camera=Camera.main;int mask=camera.cullingMask;var clear=camera.clearFlags;bool hud=UIRoot.Get().Hud.gameObject.activeSelf;
            director.StartCoroutine(DreamEnding.Play());yield return Until(()=>DreamEnding.Showing,2,"ending starts");
            virtualKeyboard=InputSystem.AddDevice<Keyboard>();InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.Space));
            float elapsed=0;bool poem=false,credits=false;
            var text=Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).First(t=>t.name=="PoemAndCredits");
            float height=text.rectTransform.sizeDelta.y;
            float limit=(height+1200)/220+10;
            while(DreamEnding.Showing){
                InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState(Key.Space));
                elapsed+=Time.unscaledDeltaTime;
                if(!poem&&elapsed>6){Capture("world5-poem-reading");poem=true;}
                if(!credits&&text.rectTransform.anchoredPosition.y>height*.72f){Capture("world5-full-credits");credits=true;}
                if(elapsed>limit)Assert.Fail("Accelerated ending did not finish; keyboard space="+virtualKeyboard.spaceKey.isPressed);
                yield return null;
            }
            InputSystem.QueueStateEvent(virtualKeyboard,new KeyboardState());
            Assert.IsTrue(credits);Assert.That(DreamEnding.CompletedThisSession,Is.EqualTo(completed+1));Assert.IsTrue(motor.InputEnabled);
            Assert.That(camera.cullingMask,Is.EqualTo(mask));Assert.That(camera.clearFlags,Is.EqualTo(clear));Assert.That(UIRoot.Get().Hud.gameObject.activeSelf,Is.EqualTo(hud));
        }
        [UnityTest] public IEnumerator UsedWorldFive_ActualArrivalFramesLandmarkAndRestoresControl(){
            DreamCinematic.SkipForGameplayProbe=false;
            SceneManager.LoadScene(CampaignRoute.Scenes[4]);yield return null;yield return null;yield return TransitionOverlay.Get().Open(0);
            motor=Object.FindFirstObjectByType<PlayerMotor3D>();
            yield return Until(()=>DreamCinematic.Active,2,"actual arrival starts");
            yield return new WaitForSecondsRealtime(1);Capture("world5-actual-arrival-cinematic");
            yield return Until(()=>!DreamCinematic.Active,7,"actual arrival completes");
            Assert.IsTrue(motor.InputEnabled);Assert.IsTrue(UIRoot.Get().Hud.gameObject.activeSelf);
            Assert.IsTrue(Object.FindFirstObjectByType<CameraSystem.ThirdPersonCameraController>().enabled);
        }
        [UnityTest] public IEnumerator UsedWorldMoon_RepeatedNaturalFalls_ZeroSleepWakesWithoutClear(){
            yield return Load(CampaignRoute.Scenes[2]);
            var stability=DreamRush.Dream.SleepStability.Current;int falls=0;float elapsed=0;
            while(stability!=null&&stability.Value>0){
                move=Vector2.right;elapsed+=Time.deltaTime;
                if(DreamRush.Dream.DreamLevelManager.Current!=null)falls=DreamRush.Dream.DreamLevelManager.Current.FallCount;
                if(elapsed>35)Assert.Fail("Natural falls did not deplete sleep");yield return null;
            }
            move=Vector2.zero;Assert.That(falls,Is.GreaterThanOrEqualTo(3));
            yield return Until(()=>SceneManager.GetActiveScene().name==GameFlowManager.HubScene&&GameFlowManager.Get().State==FlowState.Hub,8,"zero sleep wakes from actual world");
            var p=new SaveManager(savePath).Load();Assert.That(p.progression.FindLevelRecord(CampaignRoute.Scenes[2])?.clears??0,Is.EqualTo(0));Assert.That(p.diary.runsLost,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator UsedAllWorlds_ActualIntrosAndWorldOneChapterCuts_RestoreControl(){
            DreamCinematic.SkipForGameplayProbe=false;
            foreach(var scene in CampaignRoute.Scenes){
                if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();if(BossIntroCard.Current!=null)Object.Destroy(BossIntroCard.Current.gameObject);
                SceneManager.LoadScene(scene);yield return null;yield return null;yield return TransitionOverlay.Get().Open(0);
                motor=Object.FindFirstObjectByType<PlayerMotor3D>();
                yield return Until(()=>DreamCinematic.Active,3,"arrival starts: "+scene);yield return new WaitForSecondsRealtime(.9f);Capture(scene+"-actual-intro");
                yield return Until(()=>!DreamCinematic.Active,8,"arrival restores: "+scene);
                Assert.IsTrue(motor.InputEnabled,scene);Assert.IsTrue(UIRoot.Get().Hud.gameObject.activeSelf,scene);
                if(scene==CampaignRoute.Scenes[0]){
                    var chapters=ChapterDirector.Current;
                    for(int index=1;index<chapters.chapters.Count;index++){
                        yield return new WaitForSecondsRealtime(2.5f);chapters.GoTo(index,true);
                        yield return Until(()=>DreamCinematic.Active,4,"chapter cinematic "+index);yield return new WaitForSecondsRealtime(.8f);Capture("world1-chapter-"+(index+1)+"-actual-cut");
                        yield return Until(()=>!DreamCinematic.Active,5,"chapter restored "+index);yield return Until(()=>!motor.GetComponent<PlayerUIBridge>().CutsceneLock,2,"chapter interaction restored");
                        Assert.IsTrue(motor.InputEnabled);Assert.That(chapters.Index,Is.EqualTo(index));Assert.IsFalse(motor.GetComponent<PlayerUIBridge>().CutsceneLock);
                    }
                }
            }
        }
        [UnityTest] public IEnumerator UsedWorldMusic_ContextTracksFinaleAndCredits_ActuallyAdvance(){
            Audio.AudioManager.Muted=false;
            try{
                var themes=new[]{Audio.Music.ToyBoxMoon,Audio.Music.BubbleBathSea,Audio.Music.MidnightTeacup};
                for(int i=0;i<3;i++){
                    if(GameFlowManager.Exists)GameFlowManager.Get().ResetForTests();
                    yield return Load(CampaignRoute.Scenes[i+2]);
                    yield return Until(()=>Audio.AudioManager.MusicIsPlaying,3,"context music starts "+themes[i]);yield return new WaitForSecondsRealtime(.3f);
                    Assert.That(Audio.AudioManager.CurrentMusic,Is.EqualTo(themes[i]));Assert.That(Audio.AudioManager.MusicTime,Is.GreaterThan(.05f));
                    Debug.Log("[CAMPAIGN AUDIO] actual context playback "+themes[i]+" time="+Audio.AudioManager.MusicTime);
                }
                foreach(var music in new[]{Audio.Music.DreamFinale,Audio.Music.Credits}){
                    Audio.AudioManager.PlayMusic(music,.1f);yield return Until(()=>Audio.AudioManager.MusicIsPlaying,3,"music starts "+music);yield return new WaitForSecondsRealtime(.3f);
                    Assert.That(Audio.AudioManager.CurrentMusic,Is.EqualTo(music));Assert.That(Audio.AudioManager.MusicTime,Is.GreaterThan(.05f));
                    Debug.Log("[CAMPAIGN AUDIO] actual playback "+music+" time="+Audio.AudioManager.MusicTime);
                }
            }finally{Audio.AudioManager.ResetForTests();}
        }
    }
}

