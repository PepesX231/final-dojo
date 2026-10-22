using System;
using System.Collections.Generic;
using System.Linq;
using DreamRush.Campaign;
using DreamRush.CameraSystem;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Look;
using DreamRush.Player;
using DreamRush.Palace;
using DreamRush.UI;
using DreamRush.World1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace DreamRush.EditorTools
{
    public static class CampaignWorldBuilder
    {
        static Material cream,ink,wood,teal,gold,rose,water,cloud,crimsonSketch;
        static Transform root;
        static StoryWorldDirector director;
        static List<StoryMechanism> nodes;
        static List<GameObject> paths;
        static Color C(int hex)=>Core.Palette.Hex(hex);
        [MenuItem("DreamRush/Build/Campaign worlds 3 to 5")]
        public static void BuildAll(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring worlds.");
            for(int world=3;world<=5;world++)Build(world);
            PatchGalaxy();AssetDatabase.SaveAssets();
            Debug.Log("[Campaign] Worlds 3, 4, 5 built and routed. Gameplay playthrough still required.");
        }
        public static string BuildThree(){Build(3);return "Toy Box Moon saved";}
        public static string BuildFour(){Build(4);return "Bubble Bath Sea saved";}
        public static string BuildFive(){Build(5);return "Midnight Teacup saved";}
        static void Materials(){
            cream=ArtKit.Mat("M_Campaign_Cream",C(0xEFE4CC),"T_PageFloor",new Vector2(2,2),.03f,spec:.03f);
            ink=ArtKit.Mat("M_Campaign_Ink",C(0x34334A),rim:.03f);
            wood=ArtKit.Mat("M_Campaign_Wood",C(0xA47D56),"T_PageEdge",new Vector2(3,1),.03f,spec:.04f);
            teal=ArtKit.Mat("M_Campaign_Teal",C(0x6AA7A5),"T_BookCloth",new Vector2(2,2),.05f);
            gold=ArtKit.Mat("M_Campaign_Gold",C(0xDAB862),rim:.05f,spec:.06f);
            rose=ArtKit.Mat("M_Campaign_Rose",C(0xD98E96),"T_FabricRose",new Vector2(2,2),.03f);
            water=ArtKit.Mat("M_Campaign_Water",C(0x5AAEC0),rim:.08f,spec:.12f);
            cloud=ArtKit.Mat("M_Campaign_Cloud",C(0xD9E3EC),rim:.03f);
            crimsonSketch=ArtKit.Mat("M_Campaign_CrimsonSketch",new Color(0.95f,0.08f,0.16f),rim:.25f,spec:.1f);
            crimsonSketch.SetColor("_Emission",new Color(1.4f,0.06f,0.14f));
            EditorUtility.SetDirty(crimsonSketch);
        }
        static Transform Group(Transform parent,string name,Vector3 at=default){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=at;return t;}
        static GameObject Shape(Transform parent,string name,Vector3 at,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube,bool solid=true){
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.position=at;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());
            else if(type==PrimitiveType.Cylinder){Object.DestroyImmediate(g.GetComponent<Collider>());g.AddComponent<MeshCollider>().sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;}
            return g;
        }
        static Transform Model(Transform parent,string path,Vector3 at,float width,float yaw=0){
            var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(src==null){Debug.LogWarning("[Campaign] missing visual "+path);return null;}
            var g=(GameObject)PrefabUtility.InstantiatePrefab(src);g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
            foreach(var col in g.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            var rr=g.GetComponentsInChildren<Renderer>();if(rr.Length>0){var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);g.transform.localScale*=width/Mathf.Max(.1f,bounds.size.x);}
            return g.transform;
        }
        static Transform Bed(Transform parent,string name,Vector3 at,float width,float yaw=0)=>Model(parent,"Assets/DreamRush/Art/Bedroom/Models/"+name+".fbx",at,width,yaw);
        static Transform Prop(Transform parent,string name,Vector3 at,float width,float yaw=0)=>Model(parent,ArtKit.PropDir+"/"+name+".fbx",at,width,yaw);
        static Transform Kit(Transform parent,string name,Vector3 at,float width,float yaw=0)=>Model(parent,"Assets/DreamRush/Art/MidnightPalace/Models/INT_"+name+".fbx",at,width,yaw);
        static Transform Island(Transform parent,string name,Vector3 at,float radius,Material top){
            var t=Group(parent,name,at);
            Shape(t,"BoundPaper",at-Vector3.up*.5f,new Vector3(radius*2,.5f,radius*2),wood,PrimitiveType.Cylinder);
            Shape(t,"WalkSurface",at+Vector3.up*.025f,new Vector3(radius*2,.025f,radius*2),top,PrimitiveType.Cylinder);
            Shape(t,"PaintedRim",at-Vector3.up*.09f,new Vector3(radius*2.025f,.045f,radius*2.025f),gold,PrimitiveType.Cylinder,false);
            return t;
        }
        static Checkpoint Checkpoint(Transform parent,Vector3 at,int order,bool first=false){
            var t=Group(parent,"PillowCheckpoint",at);var cp=t.gameObject.AddComponent<Checkpoint>();cp.order=order;cp.isStart=first;cp.radius=2.5f;
            var cushion=Prop(t,"PROP_Cushion",at+Vector3.up*.04f,1.5f);if(cushion!=null){var scale=cushion.localScale;cushion.localScale=new Vector3(scale.x,scale.y*.12f,scale.z);}return cp;
        }
        static void Star(Transform parent,Vector3 at,int n){
            var g=new GameObject("DreamStar_"+n);g.transform.SetParent(parent,false);g.transform.position=at;
            var visual=Prop(g.transform,"PROP_GalStar",at,.65f);
            if(visual==null)Shape(g.transform,"Star",at,Vector3.one*.5f,gold,PrimitiveType.Sphere,false);
            g.AddComponent<DreamStar>().chapter=n;
        }
        static StoryMechanism Mechanism(Transform parent,Vector3 at,int stage,bool rest=false,bool finale=false){
            var t=Group(parent,rest?"RestPoint":finale?"FinaleBell":"Mechanism_"+(stage+1),at);
            var m=t.gameObject.AddComponent<StoryMechanism>();m.director=director;m.stage=stage;m.finalRest=rest;m.finaleControl=finale;m.targetBeat=finale?.6f:.5f;
            m.correctTurns=stage%3+1;m.label=director.world==StoryWorldKind.BubbleBathSea?"หมุนวาล์วให้ลูกศรตรงกัน":finale?"ตอบทำนองสุดท้าย":"ตีเสียงที่ "+(stage+1);
            Shape(t,"Pedestal",at+Vector3.up*.3f,new Vector3(.9f,.6f,.9f),wood);
            var ringMat=director.world==StoryWorldKind.ToyBoxMoon?crimsonSketch:teal;
            var ring=Shape(t,"BeatRing",at+Vector3.up*.05f,new Vector3(2,.025f,2),ringMat,PrimitiveType.Cylinder,false);m.ring=ring.transform;
            if(rest){Bed(t,"RUG_Round",at,2.3f);Kit(t,"Armchair",at+Vector3.forward*.15f,1.25f,180);}
            else {
                var r=Group(t,"Rotor",at+Vector3.up*1.05f);m.rotor=r;
                Shape(r,"Disc",r.position,new Vector3(.8f,.1f,.8f),gold,PrimitiveType.Cylinder,false);
                Shape(r,"Arrow",r.position+new Vector3(0,.14f,.25f),new Vector3(.14f,.1f,.65f),director.world==StoryWorldKind.ToyBoxMoon?crimsonSketch:ink,PrimitiveType.Cube,false);
                Shape(t,"TargetArrow",at+new Vector3(Mathf.Sin(m.correctTurns*Mathf.PI/2)*.9f,.04f,Mathf.Cos(m.correctTurns*Mathf.PI/2)*.9f),new Vector3(.28f,.05f,.55f),director.world==StoryWorldKind.ToyBoxMoon?crimsonSketch:gold,PrimitiveType.Cube,false).transform.rotation=Quaternion.Euler(0,m.correctTurns*90,0);
                Label(t,(stage+1).ToString(),at+Vector3.up*1.6f,.16f);
            }
            if(!rest&&!finale)nodes.Add(m);return m;
        }
        static void Label(Transform parent,string text,Vector3 at,float size){
            var t=Group(parent,"Sign",at);t.rotation=Quaternion.Euler(0,180,0);var mesh=t.gameObject.AddComponent<TextMesh>();
            mesh.text=text;mesh.font=UIFactory.FontBold;mesh.fontSize=64;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=C(0xF5E7D3);
            t.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
        }
        static LaunchPad Pad(Transform parent,Vector3 at,Vector3 target,float apex=4){
            var g=Group(parent,"LaunchCushion",at);var pivot=Group(g,"Squash",at);
            var padMat=director!=null&&director.world==StoryWorldKind.ToyBoxMoon?crimsonSketch:rose;
            Shape(g,"StableCollider",at+Vector3.up*.12f,new Vector3(2.3f,.24f,2.3f),padMat).GetComponent<Renderer>().enabled=false;
            Shape(pivot,"Cushion",at+Vector3.up*.12f,new Vector3(2.3f,.15f,2.3f),padMat,PrimitiveType.Cylinder,false);
            var to=Group(g,"LandingTarget",target);var pad=g.gameObject.AddComponent<LaunchPad>();pad.visual=pivot;pad.target=to;pad.apexHeight=apex;pad.windup=.12f;pad.radius=1.3f;
            for(int i=1;i<6;i++){var p=Vector3.Lerp(at,target,i/6f)+Vector3.up*(Mathf.Sin(i/6f*Mathf.PI)*apex);Shape(g,"ArcGuide",p,Vector3.one*.14f,padMat,PrimitiveType.Sphere,false);}
            return pad;
        }
        static GameObject Path(string name,Vector3 from,Vector3 to,int pieces,bool raised=false){
            var g=Group(root,name);var delta=to-from;float flat=new Vector2(delta.x,delta.z).magnitude;float yaw=Quaternion.LookRotation(new Vector3(delta.x,0,delta.z)).eulerAngles.y;
            Vector3 dir=new Vector3(delta.x,0,delta.z).normalized;
            Vector3 Point(float d){var p=from+dir*d;p.y=Mathf.Lerp(from.y,to.y,Mathf.Clamp01((d-3f)/Mathf.Max(1,flat-7f)));return p;}
            for(int i=0;i<pieces;i++){
                var a=Point(flat*i/pieces);var b=Point(flat*(i+1)/pieces);var rotation=Quaternion.LookRotation(b-a);
                var p=(a+b)*.5f;var step=Shape(g,"Plank",p-rotation*Vector3.up*.2f,new Vector3(3.2f,.4f,Vector3.Distance(a,b)+.08f),i%3==0?teal:wood);
                step.transform.rotation=rotation;
                if(raised&&i%3==0)Shape(g,"Support",new Vector3(p.x,(p.y-3)*.5f,p.z),new Vector3(.25f,p.y+3,.25f),wood);
            }
            paths.Add(g.gameObject);g.gameObject.SetActive(false);return g.gameObject;
        }
        static void Environment(int world){
            RenderSettings.ambientMode=AmbientMode.Trilight;
            if(world==3){
                RenderSettings.ambientSkyColor=C(0xF6F1E6);RenderSettings.ambientEquatorColor=C(0xDDD4C4);RenderSettings.ambientGroundColor=C(0x8E8578);
            }else{
                RenderSettings.ambientSkyColor=C(0xBAC7D8);RenderSettings.ambientEquatorColor=C(0x9A9CAE);RenderSettings.ambientGroundColor=C(0x4E4558);
            }
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=340;
            var sky=world==3?World1Builder.AnimeSky("M_Sky_ToyMoon",C(0xEFECE4),C(0xF8F5EE),C(0xE2DDD2),C(0xD8D2C5),.18f,C(0xC0B9AC),0f):world==4?World1Builder.AnimeSky("M_Sky_BathSea",C(0x579AD1),C(0xE3E4CC),C(0xFFD6A4),C(0x6284A0),.42f,C(0x89A6C8),0):World1Builder.AnimeSky("M_Sky_Teacup",C(0x151C3A),C(0xB4A1AC),C(0xF4C792),C(0x3E3048),.12f,C(0x5F567B),.7f);
            RenderSettings.skybox=sky;RenderSettings.fogColor=world==3?C(0xF4F0E6):world==4?C(0xC6D8D9):C(0x74718F);
            var light=Group(root,"KeyLight").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=C(0xFFF0D5);light.intensity=world==3?1.15f:world==4?1.05f:.85f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(38,-28,0);
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.22f);bloom.threshold.Override(1.2f);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
            string pp="Assets/DreamRush/Settings/PP_CampaignWorld"+world+".asset";var old=AssetDatabase.LoadAssetAtPath<VolumeProfile>(pp);
            if(old!=null){Object.DestroyImmediate(profile);profile=old;}else{AssetDatabase.CreateAsset(profile,pp);foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);}
            var volume=Group(root,"PostFX").gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var style=Group(root,"StorybookStyle").gameObject.AddComponent<StylePack>();
            if(world==3){
                // World 3 Pencil Sketch / Hand-Drawn Ink Cross-Hatching & Screentone Art Style (with vivid crimson accents)
                style.ink=1f;style.inkColor=new Color(0.09f,0.08f,0.12f);style.inkWidth=2.1f;style.inkFar=180f;
                style.halftone=0.45f;style.halftoneColor=new Color(0.12f,0.11f,0.15f,0.75f);style.halftoneScale=5.2f;
                style.posterize=0.25f;style.saturate=0f;style.celHard=0.85f;
                style.sketch=1f;style.paperColor=new Color(0.96f,0.94f,0.90f,1f);
                style.pipRim=new Color(0.95f,0.12f,0.18f);style.pipRimStrength=0.45f;
            }else{
                style.ink=.58f;style.inkWidth=1.05f;style.halftone=.08f;style.posterize=.05f;style.saturate=-.03f;style.pipRimStrength=.18f;style.sketch=0f;
            }
        }
        static void Build(int world){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Cannot author while playing");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Materials();
            root=new GameObject("WORLD_"+world).transform;nodes=new List<StoryMechanism>();paths=new List<GameObject>();
            director=Group(root,"StoryWorldDirector").gameObject.AddComponent<StoryWorldDirector>();director.world=(StoryWorldKind)world;director.title=CampaignRoute.Titles[world-1];
            director.subtitle=world==3?"ใช้แรงส่งและทำนอง พาดวงจันทร์เข้านอน":world==4?"เปิดทางน้ำ แล้วพาเรือกระดาษกลับบ้าน":"ฟังเสียงที่ซ่อนอยู่ในความเงียบ";director.bpm=world==3?84:world==4?108:92;
            Environment(world);
            var start=Group(root,"Spawn",new Vector3(0,.15f,0));director.spawn=start;
            var level=Group(root,"DreamLevelManager").gameObject.AddComponent<DreamLevelManager>();level.killY=world==4?-2.5f:-12;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DreamRush/Prefabs/Player/PF_Player.prefab") ?? MovementGymBuilder.BuildPlayerPrefab();
            var player=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);player.transform.SetPositionAndRotation(start.position,Quaternion.identity);
            var motor=player.GetComponent<PlayerMotor3D>();level.player=motor;
            if(player.GetComponent<PlayerAbilities>()==null)player.AddComponent<PlayerAbilities>();
            if(player.GetComponent<GrindController>()==null)player.AddComponent<GrindController>();
            var camera=PlayerRigBuilder.BuildHubCamera(player);CameraTuning.ApplyFollow(camera);camera.distance=5.8f;camera.defaultPitch=17;camera.Pitch=17;
            camera.GetComponent<Camera>().farClipPlane=550;DreamLighting.ConfigureCamera(camera.GetComponent<Camera>());camera.GetComponent<Camera>().clearFlags=CameraClearFlags.Skybox;motor.CameraTransform=camera.transform;
            var section=Group(root,"WorldSection",new Vector3(0,20,65)).gameObject.AddComponent<DreamSection>();section.sectionId="WORLD "+world;section.displayName=world==3?"TOY MOON":world==4?"BATH SEA":"TEACUP";section.size=new Vector3(500,200,500);section.objective=director.subtitle;
            var stability=Group(root,"SleepStability").gameObject.AddComponent<SleepStability>();stability.baseMax=5;
            Group(root,"DreamHUD").gameObject.AddComponent<DreamHUD>();
            var context=Group(root,"SceneContext").gameObject.AddComponent<SceneContext>();context.kind=SceneKind.Dream;context.music=world==3?Audio.Music.ToyBoxMoon:world==4?Audio.Music.BubbleBathSea:Audio.Music.MidnightTeacup;
            Island(root,"Arrival",Vector3.zero,9,cream);Checkpoint(root,start.position,0,true);Bed(root,"DOOR_Arch",new Vector3(0,0,-10),4);Label(root,director.title,new Vector3(0,3,-10),.085f);
            if(world==3)Moon();else if(world==4)Sea();else Teacup();
            DetailWorld(world);
            director.mechanisms=nodes.ToArray();director.unlockedPaths=paths.ToArray();
            string path="Assets/DreamRush/Scenes/"+CampaignRoute.Scenes[world-1]+".unity";EditorSceneManager.SaveScene(scene,path);MovementGymBuilder.AddToBuildSettings(path);AssetDatabase.SaveAssets();
        }
        static void Moon(){
            Vector3 a=new Vector3(-8,3,24),b=new Vector3(8,6,50),c=new Vector3(-5,9,77),end=new Vector3(0,12,108);
            Island(root,"MusicBoxIsland",a,7,teal);Island(root,"OrbitIsland",b,7,cream);Island(root,"RocketIsland",c,7,rose);Island(root,"MoonCradle",end,10,cream);
            Pad(root,new Vector3(0,.1f,6),a+Vector3.back*2,4);
            Mechanism(root,a,0);Mechanism(root,b,1);Mechanism(root,c,2);
            var p0=Group(root,"AwakenedSpring1");Pad(p0,a+Vector3.forward*4,b+Vector3.back*3,5);paths.Add(p0.gameObject);p0.gameObject.SetActive(false);
            var p1=Group(root,"AwakenedSpring2");Pad(p1,b+Vector3.forward*4,c+Vector3.back*3,5);paths.Add(p1.gameObject);p1.gameObject.SetActive(false);
            var p2=Group(root,"AwakenedSpring3");Pad(p2,c+Vector3.forward*4,end+Vector3.back*5,5);paths.Add(p2.gameObject);p2.gameObject.SetActive(false);
            var stations=new[]{a,b,c};for(int k=0;k<3;k++){
                Checkpoint(root,stations[k]+Vector3.back*2,k+1);Bed(root,"TOY_Rocket",stations[k]+new Vector3(-4,0,1),2.4f);Prop(root,"PROP_ToyBlock",stations[k]+new Vector3(4,0,2),2.1f,k*35);
                Star(root,stations[k]+new Vector3(4.7f,1.6f,-1.5f),k+1);Star(root,stations[k]+new Vector3(-4,2.2f,2.5f),k+1);
                var orb=Island(root,"OrbitShortcut"+k,(stations[k]+(k==2?end:stations[k+1]))*.5f,2.3f,gold);
                var mover=orb.gameObject.AddComponent<MovingPlatform>();mover.orbitCenter=orb.position;mover.orbitRadius=3;mover.orbitDegPerSec=18;mover.carryRadius=2.3f;
                // Sketch world floating drafting frames & crimson laser guide rings (visual only, no colliders)
                var sketchFrame=Shape(root,"SketchFrame_"+k,stations[k]+new Vector3(0,4.8f,4.5f),new Vector3(6.2f,0.12f,0.12f),ink,PrimitiveType.Cube,false);
                sketchFrame.transform.rotation=Quaternion.Euler(0,0,k%2==0?12:-12);
                var redAccent=Shape(root,"SketchCrimsonAccent_"+k,stations[k]+new Vector3(0,0.06f,0),new Vector3(4.2f,0.02f,4.2f),crimsonSketch,PrimitiveType.Cylinder,false);
            }
            director.landmark=Bed(root,"MOON_Crescent",end+new Vector3(0,7,5),12,180);
            director.finale=Mechanism(root,end+Vector3.back*3,3,finale:true).transform;Mechanism(root,end+new Vector3(4,0,2),4,rest:true);
            Bed(root,"BED_Apple",end+new Vector3(-4,0,3),3,160);
            for(int i=0;i<12;i++){float a0=i*.7f;var p=new Vector3(Mathf.Sin(a0)*35,-8-i%3*5,15+i*8);Bed(root,"CLOUD_Puff",p,12+i%3*3);}
            Prop(root,"PLANET_Toy",new Vector3(35,-10,65),25,35);Prop(root,"PROP_GalRing",new Vector3(20,18,90),30,30);
        }
        static void Sea(){
            Shape(root,"BathOcean",new Vector3(0,-2,65),new Vector3(250,.2f,260),water,PrimitiveType.Cube,false);
            Vector3 a=new Vector3(-13,1.5f,24),b=new Vector3(12,3,49),c=new Vector3(-9,5,73),end=new Vector3(0,3,121);
            Island(root,"SoapHarbor",a,7,cream);Island(root,"FlowWorks",b,7,teal);Island(root,"ShellGarden",c,7,cream);Island(root,"LighthouseDock",end,11,wood);
            var walk=Path("ArrivalBoardwalk",new Vector3(0,0,7),a+Vector3.back*5,8,true);paths.Remove(walk);walk.SetActive(true);
            Path("FlowBridge1",a+Vector3.forward*5,b+Vector3.back*5,12,true);Path("FlowBridge2",b+Vector3.forward*5,c+Vector3.back*5,12,true);Path("BoatPier",c+Vector3.forward*5,new Vector3(0,3,84),7,true);
            Mechanism(root,a,0);Mechanism(root,b,1);Mechanism(root,c,2);
            var st=new[]{a,b,c};for(int i=0;i<3;i++){
                Checkpoint(root,st[i]+Vector3.back*2,i+1);Bed(root,"PLANT_Pot",st[i]+new Vector3(-4,0,1),1.4f);Kit(root,"StoneLantern",st[i]+new Vector3(4,0,1),1.4f);
                Star(root,st[i]+new Vector3(4,1.4f,-2),i+1);Pad(paths[i].transform,st[i]+new Vector3(-4,0,3),(i==2?end:st[i+1])+Vector3.back*3,6);
            }
            var ship=Group(root,"PaperShip",new Vector3(0,3,88));Shape(ship,"Deck",ship.position,new Vector3(7,.6f,11),cream);
            Prop(ship,"PROP_PillowShip",ship.position-Vector3.up*.8f,8);var mover=ship.gameObject.AddComponent<MovingPlatform>();mover.AutoMove=false;mover.carryRadius=0;
            director.boat=ship;director.boatDestination=new Vector3(0,3,113);
            var tower=Group(root,"Lighthouse",end+new Vector3(0,0,4));Shape(tower,"Tower",tower.position+Vector3.up*5,new Vector3(3,5,3),cream,PrimitiveType.Cylinder);
            Shape(tower,"Lantern",tower.position+Vector3.up*10.5f,new Vector3(4,1,4),gold,PrimitiveType.Cylinder,false);Bed(tower,"MOON_Crescent",tower.position+Vector3.up*13,3);
            director.landmark=tower;director.finale=Mechanism(root,end,3,rest:true).transform;
            for(int i=0;i<2;i++){
                var hazard=Group(root,"TidePulse"+i,new Vector3(0,3,97+i*8));var h=hazard.gameObject.AddComponent<StoryPulseHazard>();h.phaseOffset=i*.5f;
                h.warning=Shape(hazard,"WarningRing",hazard.position+Vector3.up*.37f,new Vector3(9,.03f,9),gold,PrimitiveType.Cylinder,false).transform;
                h.jet=Shape(hazard,"FoamJet",hazard.position+Vector3.up*2,new Vector3(1.5f,4,1.5f),cloud,PrimitiveType.Cylinder,false).transform;
            }
            for(int i=0;i<18;i++){var p=new Vector3((i%2==0?-1:1)*(14+i%4*4),-.7f,i*7);Shape(root,"FoamIsland",p,new Vector3(4+i%3,.4f,4+i%3),cloud,PrimitiveType.Sphere,false);}
            Prop(root,"PLANET_Bath",new Vector3(-42,-10,90),35,20);
        }
        static void Teacup(){
            Vector3 a=new Vector3(0,0,23),b=new Vector3(-12,3,48),c=new Vector3(12,6,76),end=new Vector3(0,8,108);
            Island(root,"FirstCup",a,7,cream);Island(root,"QuietKitchen",b,8,rose);Island(root,"BellGarden",c,8,teal);Island(root,"LastTable",end,11,cream);
            var walk=Path("TeaTowelArrival",new Vector3(0,0,7),a+Vector3.back*5,7);paths.Remove(walk);walk.SetActive(true);
            Path("TeaTowelBridge1",a+Vector3.forward*5,b+Vector3.back*5,10);Path("TeaspoonBridge2",b+Vector3.forward*5,c+Vector3.back*5,12);Path("LastRibbon",c+Vector3.forward*5,end+Vector3.back*7,11);
            var st=new[]{a,b,c};for(int i=0;i<3;i++){
                var m=Mechanism(root,st[i],i);m.targetBeat=new[]{.2f,.55f,.8f}[i];
                Checkpoint(root,st[i]+Vector3.back*2,i+1);Kit(root,"BistroTable",st[i]+new Vector3(-4,0,2),2);Kit(root,"BistroChair",st[i]+new Vector3(-4,0,.7f),1.1f,180);
                Kit(root,"Monstera",st[i]+new Vector3(4,0,2),1.5f);Star(root,st[i]+new Vector3(4.5f,1.7f,-1),i+1);Star(root,st[i]+new Vector3(-4,1.8f,2),i+1);
            }
            var clock=Kit(root,"GrandfatherClock",end+new Vector3(0,0,4),7,180);director.landmark=clock;
            director.finale=Mechanism(root,end+Vector3.back*2,3,finale:true).transform;Mechanism(root,end+new Vector3(4,0,3),4,rest:true);
            Kit(root,"CafeCounter",end+new Vector3(-6,0,3),5);Kit(root,"Pendant",end+new Vector3(-6,5,3),2);
            Prop(root,"PLANET_Kitchen",new Vector3(32,-12,85),35,30);
            for(int i=0;i<12;i++){var p=new Vector3((i%2==0?1:-1)*(22+i%3*5),-8,i*10);Kit(root,i%2==0?"BistroTable":"SideTable",p,8+i%4,35*i);}
        }
        static void DetailWorld(int world){
            string[] islands=world==3?new[]{"Arrival","MusicBoxIsland","OrbitIsland","RocketIsland","MoonCradle"}:world==4?new[]{"Arrival","SoapHarbor","FlowWorks","ShellGarden","LighthouseDock"}:new[]{"Arrival","FirstCup","QuietKitchen","BellGarden","LastTable"};
            foreach(string name in islands){
                var island=root.Find(name);if(island==null)continue;
                var floor=island.Find("WalkSurface");float radius=floor.localScale.x*.5f;var at=island.position;
                var dressing=Group(island,"IllustratedDetails",at);
                for(int k=0;k<24;k++){
                    float angle=k*Mathf.PI/12;var p=at+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(radius-.45f)+Vector3.up*.075f;
                    Shape(dressing,"BindingStitch",p,new Vector3(.10f,.025f,.42f),world==3?crimsonSketch:world==4?teal:gold,PrimitiveType.Cube,false).transform.rotation=Quaternion.Euler(0,k*15,0);
                }
                if(world==3){
                    for(int k=0;k<4;k++){
                        float angle=(45+k*90)*Mathf.Deg2Rad;var side=at+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(radius*.58f);
                        Shape(dressing,"MusicBoxFoot",side-Vector3.up*.95f,new Vector3(.7f,.9f,.7f),wood,PrimitiveType.Cube,false);
                        Shape(dressing,"BrassStud",at+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*(radius-.3f)-Vector3.up*.3f,Vector3.one*.22f,crimsonSketch,PrimitiveType.Sphere,false);
                    }
                    if(name!="Arrival"){
                        Bed(dressing,"TOY_Train",at+new Vector3(radius*.57f,.08f,2.5f),2.5f,35);
                        Bed(dressing,"SHELF_Books",at+new Vector3(-radius*.6f,0,-1),2,65);
                    }
                }else if(world==4){
                    for(int x=-2;x<=2;x++)for(int z=-2;z<=2;z++){
                        var p=at+new Vector3(x*1.6f,.06f,z*1.6f);if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(at.x,at.z))>radius-1)continue;
                        Shape(dressing,"GlazedBathTile",p,new Vector3(1.54f,.018f,1.54f),(x+z)%2==0?cream:teal,PrimitiveType.Cube,false);
                    }
                    for(int k=0;k<3;k++)Shape(dressing,"SoapBar",at+new Vector3(-radius*.63f,.22f+k*.19f,2),new Vector3(1.3f,.32f,.7f),k%2==0?rose:cream,PrimitiveType.Cube,false).transform.rotation=Quaternion.Euler(0,20+k*17,0);
                    Bed(dressing,"LAMP_Mushroom",at+new Vector3(radius*.6f,0,1),1.2f);
                }else{
                    // The table-islands read as giant ceramic cups, with their handles below the walkable lip.
                    Shape(dressing,"CeramicCupBody",at-Vector3.up*1.5f,new Vector3(radius*1.97f,1.3f,radius*1.97f),name=="QuietKitchen"?rose:cream,PrimitiveType.Cylinder,false);
                    for(int k=0;k<12;k++){
                        float a0=(-90+k*15)*Mathf.Deg2Rad,a1=(-90+(k+1)*15)*Mathf.Deg2Rad;
                        Vector3 a=at+new Vector3(radius-.3f+Mathf.Cos(a0)*3,-1.5f+Mathf.Sin(a0)*1.7f,0),b=at+new Vector3(radius-.3f+Mathf.Cos(a1)*3,-1.5f+Mathf.Sin(a1)*1.7f,0);
                        var piece=Shape(dressing,"CupHandle",(a+b)*.5f,new Vector3(.5f,.5f,Vector3.Distance(a,b)+.12f),cream,PrimitiveType.Cube,false);piece.transform.rotation=Quaternion.LookRotation(b-a);
                    }
                    Kit(dressing,"Bookcase",at+new Vector3(-radius*.6f,0,-.5f),2,55);
                    Kit(dressing,"FloorLamp",at+new Vector3(radius*.55f,0,-2),.85f,15);
                }
                foreach(var renderer in dressing.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(world==5){
                foreach(var table in root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("INT_BistroTable")).ToArray()){
                    var rr=table.GetComponentsInChildren<Renderer>();if(rr.Length==0)continue;var bounds=rr[0].bounds;foreach(var r in rr)bounds.Encapsulate(r.bounds);
                    var p=bounds.center;p.y=bounds.max.y+.03f;
                    Shape(table,"TeaSaucer",p,new Vector3(.6f,.025f,.6f),cream,PrimitiveType.Cylinder,false);
                    Shape(table,"TeaCup",p+Vector3.up*.12f,new Vector3(.3f,.12f,.3f),rose,PrimitiveType.Cylinder,false);
                    Shape(table,"TeaSurface",p+Vector3.up*.245f,new Vector3(.24f,.008f,.24f),wood,PrimitiveType.Cylinder,false);
                }
            }
        }
        [MenuItem("DreamRush/Campaign/Connect all five Galaxy worlds")]
        public static void PatchGalaxy(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before patching Galaxy");
            var scene=EditorSceneManager.OpenScene("Assets/DreamRush/Scenes/DreamGalaxy.unity");
            var galaxy=Object.FindFirstObjectByType<GalaxySelect>();
            if(galaxy==null||galaxy.planets.Count<5)throw new InvalidOperationException("Five authored planets required");
            for(int i=0;i<5;i++){
                var p=galaxy.planets[i];p.sceneName=CampaignRoute.Scenes[i];p.locked=false;p.subtitle=i<2?p.subtitle:"WORLD "+(i+1)+" · "+(i==2?"กล่อมดวงจันทร์":i==3?"พาเรือกลับบ้าน":"เสียงสุดท้ายของคืนนี้");
                if(i>=2)p.chapters=i==2?"◆ กระโดดด้วยแรงส่ง\n◆ เกาะของเล่นและวงโคจร\n◆ กล่อมดวงจันทร์ให้หลับ":i==3?"◆ วาล์วและกระแสน้ำ\n◆ ล่องเรือ / ทางลัดฟองสบู่\n◆ พาเรือถึงประภาคาร":"◆ ฟังทำนองและอ่านจังหวะ\n◆ ระฆังในสวนถ้วยชา\n◆ THE LAST KNOCK · บทส่งท้าย";
                foreach(var renderer in p.GetComponentsInChildren<Renderer>()){
                    var mats=renderer.sharedMaterials;for(int k=0;k<mats.Length;k++)if(mats[k]!=null&&mats[k].name.EndsWith("_Locked")){
                        string n=mats[k].name.Substring(0,mats[k].name.Length-7);var guid=AssetDatabase.FindAssets(n+" t:Material").FirstOrDefault(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)).name==n);if(guid!=null)mats[k]=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                    }renderer.sharedMaterials=mats;
                }EditorUtility.SetDirty(p);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("[Campaign] Galaxy routed; runtime save-based unlocks remain enforced.");
        }
    }
}
