using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace MamasComing.Editor
{
    public static class MamaHomeV2Builder
    {
        public const float Storey=3f, WalkableArea=808f, PreviousArea=259f;
        const string Art="Assets/MamasComing/Art/Recast/Models/", Mats="Assets/MamasComing/Art/HouseStudy/";
        static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        static Transform low,up,roof;static readonly List<HouseChore> tasks=new List<HouseChore>();
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
            var old=EditorSceneManager.GetActiveScene();
            if(old.isDirty)throw new InvalidOperationException("Save current scene edits first.");
            System.IO.Directory.CreateDirectory(Mats);AssetDatabase.Refresh();mats.Clear();tasks.Clear();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            low=Group("GROUND • furnished family home");up=Group("UPPER • bedrooms / study / workshop");roof=Group("ROOF • complete exterior shell");
            Rooms();Walls();Stairs();Furnish();Quests();SnapSupportedItems();Roofs();
            var player=MakePlayer();
            var camGo=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(ThirdPersonCamera));camGo.tag="MainCamera";
            camGo.GetComponent<Camera>().nearClipPlane=.05f;camGo.GetComponent<Camera>().farClipPlane=160;
            var orbit=camGo.GetComponent<ThirdPersonCamera>();orbit.target=player.transform;orbit.distance=5.5f;orbit.maxDistance=9;orbit.minDistance=3.4f;orbit.pitch=43;orbit.yaw=15;orbit.baseFog=0;
            var plan=new GameObject("House overview camera",typeof(Camera)).GetComponent<Camera>();plan.orthographic=true;plan.orthographicSize=17;plan.farClipPlane=180;plan.gameObject.SetActive(false);
            var lab=new GameObject("House study • untimed playable prototype").AddComponent<HousePlayground>();
            lab.player=player;lab.orbit=orbit;lab.planCamera=plan;lab.upperFloor=up;lab.roof=roof;lab.spawn=new Vector3(10,.06f,3);lab.planCenter=new Vector3(13,0,10);lab.chores=tasks.ToArray();
            var sun=new GameObject("Afternoon sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.15f;sun.color=Hex("FFE5BC");sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(48,-30,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("B9D7EB");RenderSettings.ambientEquatorColor=Hex("AAB7BA");RenderSettings.ambientGroundColor=Hex("63576B");RenderSettings.fog=false;
            Box("Garden",null,new Vector3(13,-.3f,10),new Vector3(40,.3f,34),"Grass","548362");
            Box("Front path",null,new Vector3(10,-.06f,-4),new Vector3(2,.08f,5),"Stone","D4BD94");
            for(int i=0;i<7;i++){Sphere("Hedge",null,new Vector3(-1.2f,.5f,i*3),new Vector3(1.4f,1,1.7f),"Hedge","32674C");Sphere("Hedge",null,new Vector3(28,.5f,i*3),new Vector3(1.4f,1,1.7f),"Hedge","32674C");}
            EditorSceneManager.SaveScene(scene,MamaHouseStudyBuilder.ScenePath);AssetDatabase.SaveAssets();
            var view=SceneView.lastActiveSceneView;if(view)view.LookAt(new Vector3(13,2,10),Quaternion.Euler(30,-30,0),33);
            Debug.Log("[HouseV2] Saved roofed home, 808 square units / 3.12x previous floor area, 50 authored prototype chores.");
        }
        static void Rooms()
        {
            Floor(low,"LIVING",0,0,8,8,0,"D4A16C");Floor(low,"ENTRY",8,0,12,8,0,"E6C595");Floor(low,"DINING",12,0,18,8,0,"CF9B66");
            Floor(low,"FAMILY HALL",0,8,18,11,0,"DCAF7F");Floor(low,"STAIR HALL",0,11,4,20,0,"DCAF7F");Floor(low,"PLAY ROOM",4,11,11,20,0,"C49E7D");Floor(low,"GARAGE",11,11,18,20,0,"A5AFB2");
            Floor(low,"KITCHEN",18,6,26,14,0,"79BDB3");Floor(low,"UTILITY",18,14,26,20,0,"9CB6AA");
            Floor(up,"KIDS BEDROOM",0,0,7,7,3,"C794AE");Floor(up,"BATHROOM",7,0,12,7,3,"80BEB8");Floor(up,"MAMA BEDROOM",12,0,18,7,3,"D5B788");Floor(up,"UPSTAIRS HALL",0,7,18,10,3,"DCAF7F");
            Floor(up,"LOWER LANDING",0,10,4,12,3,"DCAF7F");Floor(up,"TOP LANDING",0,18,4,20,3,"DCAF7F");Floor(up,"GALLERY",4,10,6,20,3,"DCAF7F");
            Floor(up,"STUDY",6,10,12,20,3,"C5AA86");Floor(up,"HOBBY ROOM",12,10,18,20,3,"B6A1BB");
            Box("Porch deck",low,new Vector3(10,-.07f,-1.1f),new Vector3(5,.14f,2.2f),"Wood","A66A43");
        }
        static void Walls()
        {
            // Exterior walls have real window/door openings, layered frames, baseboards and eaves.
            Exterior(low,0,0,8,0,0,true);DoorWall(low,8,0,12,0,10,0,"E8CFAB");Exterior(low,12,0,18,0,0,true);
            Exterior(low,0,0,0,20,0,true);Exterior(low,0,20,26,20,0,true);Exterior(low,18,0,18,6,0,true);Exterior(low,18,6,26,6,0,true);Exterior(low,26,6,26,20,0,true);
            Exterior(up,0,0,18,0,3,true);Exterior(up,0,0,0,20,3,true);Exterior(up,0,20,18,20,3,true);Exterior(up,18,0,18,20,3,true);
            DoorWall(low,8,0,8,8,4.8f,0,"E8CFAB");DoorWall(low,12,0,12,8,4.8f,0,"E8CFAB");
            DoorWall(low,0,8,8,8,5.8f,0,"70AAA0");DoorWall(low,8,8,12,8,10,0,"E8CFAB");DoorWall(low,12,8,18,8,15,0,"E8CFAB");
            DoorWall(low,4,11,11,11,7.2f,0,"A5A2C4");DoorWall(low,11,11,18,11,15.5f,0,"B9C4BA");
            DoorWall(low,4,11,4,20,18.5f,0,"A5A2C4");DoorWall(low,11,11,11,20,16.5f,0,"A5A2C4");
            DoorWall(low,18,6,18,14,9.5f,0,"70AAA0");DoorWall(low,18,14,18,20,17,0,"B9C4BA");DoorWall(low,18,14,26,14,22,0,"B9C4BA");
            Wall(up,7,0,7,7,3,"C99FB5");Wall(up,12,0,12,7,3,"8DBFB8");
            DoorWall(up,0,7,7,7,4.5f,3,"C99FB5");DoorWall(up,7,7,12,7,9.5f,3,"8DBFB8");DoorWall(up,12,7,18,7,15,3,"DEC494");
            DoorWall(up,6,10,12,10,9,3,"D6B284");DoorWall(up,12,10,18,10,15.5f,3,"B5A2C9");DoorWall(up,6,10,6,20,18.5f,3,"D6B284");Wall(up,12,10,12,20,3,"B5A2C9");
            // Open gallery railing around the stair void. The landing has a clear 1.8m route.
            Rail(up,new Vector3(4,3,15),6,true);Rail(up,new Vector3(2,3,12),4,false);
            for(int i=0;i<2;i++)Box("Porch column",low,new Vector3(7.8f+i*4.4f,1.25f,-1.9f),new Vector3(.16f,2.5f,.16f),"Cream","F9EACD");
            var door=Box("Front door • open",low,new Vector3(8.98f,1.05f,.75f),new Vector3(.12f,2.1f,1.65f),"Teal","277D79");
            Sphere("Brass door knob",door.transform,new Vector3(9.08f,1,.95f),Vector3.one*.09f,"Brass","D8AA4D");
        }
        static void Stairs()
        {
            for(int i=0;i<20;i++)Box("Stair tread "+i,low,new Vector3(2,(i+1)*.15f-.06f,12.15f+i*.3f),new Vector3(1.9f,.12f,.3f),"Wood","A66A43",false);
            var ramp=Box("Walkable stair ramp",low,new Vector3(2,1.44f,15.0f),new Vector3(1.9f,.12f,6.7082f),"Wood","A66A43");ramp.transform.rotation=Quaternion.Euler(-26.565f,0,0);ramp.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<11;i++)Box("Stair banister",low,new Vector3(.95f,.8f+i*.3f,12+i*.6f),new Vector3(.07f,.85f,.07f),"Cream","F9EACD");
            var rail=Box("Stair sloping handrail",low,new Vector3(.95f,2.25f,15),new Vector3(.09f,.09f,6.71f),"Cream","F9EACD",false);rail.transform.rotation=Quaternion.Euler(-26.565f,0,0);
        }
        static void Furnish()
        {
            // Each assembly owns its local coordinates. Seats face a table and stay off the walls.
            Rug(low,new Vector3(4,0,3.6f),new Vector2(4.5f,4),"B66254");
            var sofa=Group("Sofa assembly",low);sofa.position=new Vector3(1.5f,0,3.5f);Box("Seat",sofa,sofa.position+Vector3.up*.44f,new Vector3(1.3f,.4f,3.2f),"Coral","D86E5C");Box("Back",sofa,sofa.position+new Vector3(-.66f,.83f,0),new Vector3(.25f,.9f,3.3f),"Coral","D86E5C");
            foreach(float z in new[]{-1.6f,1.6f})Box("Arm",sofa,sofa.position+new Vector3(0,.68f,z),new Vector3(1.45f,.4f,.22f),"Coral","D86E5C");
            Table(low,"Coffee table",new Vector3(4.1f,0,3.6f),new Vector2(1.6f,1.1f),.55f);
            Cabinet(low,"TV console",new Vector3(4.1f,0,.65f),new Vector2(2.6f,.65f),.7f);Box("TV screen",low,new Vector3(4.1f,1.14f,.57f),new Vector3(1.65f,.85f,.11f),"Ink","263646");
            Table(low,"Side table",new Vector3(1.3f,0,6.3f),new Vector2(.75f,.75f),.7f);Lamp(low,new Vector3(1.3f,.75f,6.3f));
            Table(low,"Dining table",new Vector3(15,0,3.5f),new Vector2(2.2f,3.1f),.86f);
            foreach(float z in new[]{2.5f,4.5f}){Chair(low,new Vector3(13.35f,0,z),90);Chair(low,new Vector3(16.65f,0,z),-90);}
            Cabinet(low,"Entry shoe bench",new Vector3(10,0,7.1f),new Vector2(2.4f,.65f),.52f);
            Rug(low,new Vector3(10,0,2.5f),new Vector2(2.4f,3.2f),"BFA06E");
            for(int i=0;i<6;i++)Cabinet(low,"Kitchen cabinet",new Vector3(25.35f,0,7.2f+i*1.05f),new Vector2(.9f,1f),.91f);
            for(int i=0;i<4;i++)Cabinet(low,"Kitchen return",new Vector3(19.2f+i*1.05f,0,6.65f),new Vector2(1f,.9f),.91f);
            Box("Fridge",low,new Vector3(19,.95f,12.8f),new Vector3(1,1.9f,1),"Cream","F9EACD");Box("Fridge handle",low,new Vector3(19.38f,1.2f,12.26f),new Vector3(.07f,.55f,.07f),"Brass","D8AA4D",false);
            Box("Stove",low,new Vector3(25.3f,1,10.3f),new Vector3(.8f,.07f,1),"Ink","263646");for(int i=0;i<4;i++)Sphere("Burner",low,new Vector3(25.1f+i%2*.35f,1.06f,10.1f+i/2*.4f),new Vector3(.25f,.025f,.25f),"Ink","263646");
            Table(low,"Kitchen island",new Vector3(22,0,10.5f),new Vector2(1.4f,2.6f),.9f);
            for(int i=0;i<2;i++){Box(i==0?"Washer":"Dryer",low,new Vector3(24.1f+i*1.15f,.54f,19.3f),new Vector3(1.05f,1.08f,.9f),"Cream","F9EACD");Sphere("Porthole",low,new Vector3(24.1f+i*1.15f,.54f,18.82f),new Vector3(.68f,.68f,.06f),"Teal","277D79");}
            Table(low,"Laundry folding bench",new Vector3(20.5f,0,19),new Vector2(2,.9f),.85f);
            Table(low,"Garage workbench",new Vector3(16.8f,0,18),new Vector2(.85f,2.8f),.85f);Model("PROP_Bicycle",low,new Vector3(12.3f,0,18.4f),1.2f);
            Rug(low,new Vector3(7.3f,0,15.5f),new Vector2(4.8f,5),"A080B5");Table(low,"Game table",new Vector3(7.3f,0,15.5f),new Vector2(1.8f,1.8f),.68f);Chair(low,new Vector3(5.9f,0,15.5f),90);Chair(low,new Vector3(8.7f,0,15.5f),-90);
            Bookcase(low,new Vector3(5.2f,0,19.45f),3.8f);
            Bed(up,new Vector3(2.2f,3,2.6f),"Kids bed");Bed(up,new Vector3(14.7f,3,2.6f),"Mama bed");
            Cabinet(up,"Kids wardrobe",new Vector3(6.3f,3,2.7f),new Vector2(.9f,2.6f),1.9f);Cabinet(up,"Mama wardrobe",new Vector3(17.3f,3,3),new Vector2(.9f,3),1.9f);
            Table(up,"Bedside table",new Vector3(4.3f,3,1.3f),new Vector2(.7f,.7f),.6f);Lamp(up,new Vector3(4.3f,3.65f,1.3f));
            Box("Bath tub",up,new Vector3(9,3.38f,1.3f),new Vector3(2.6f,.75f,1.2f),"Cream","F9EACD");Box("Bath water",up,new Vector3(9,3.77f,1.3f),new Vector3(2.25f,.025f,.9f),"Water","68B9CB",false);
            Cabinet(up,"Bathroom vanity",new Vector3(11.3f,3,4.8f),new Vector2(.9f,2.2f),.9f);Sphere("Toilet",up,new Vector3(8,3.35f,5.4f),new Vector3(.7f,.7f,.9f),"Cream","F9EACD");
            Table(up,"Study desk",new Vector3(9,3,19.1f),new Vector2(3.3f,1),.83f);Chair(up,new Vector3(9,3,17.8f),0);Bookcase(up,new Vector3(6.5f,3,14),2.8f);
            Table(up,"Craft workbench",new Vector3(15,3,19.1f),new Vector2(3.3f,1),.83f);Chair(up,new Vector3(15,3,17.7f),0);Cabinet(up,"Craft cabinet",new Vector3(17.3f,3,13),new Vector2(.9f,3),1.1f);
            for(int i=0;i<3;i++){Box("Family photo frame",low,new Vector3(1.1f+i*1.1f,1.5f,7.86f),new Vector3(.6f,.65f,.05f),"Brass","D8AA4D",false);Sphere("Family portrait",low,new Vector3(1.1f+i*1.1f,1.5f,7.82f),new Vector3(.35f,.42f,.04f),"Coral","D86E5C");}
            // The cake is visibly mounted to an actual fan stand, never a loose hovering model.
            Box("Fan base",up,new Vector3(15,3.08f,13),new Vector3(.8f,.16f,.8f),"Teal","277D79");Cylinder("Fan column",up,new Vector3(15,3.63f,13),new Vector3(.09f,.5f,.09f),"Brass","D8AA4D");
            for(int i=0;i<3;i++){var blade=Box("Fan blade",up,new Vector3(15,4.17f,13),new Vector3(1.6f,.05f,.2f),"Teal","277D79",false);blade.transform.rotation=Quaternion.Euler(0,i*60,0);}
            Model("PROP_CakeSlice",up,new Vector3(15,4.2f,13),.22f);
            // Calm landing has no objectives: a clear landmark and a place to see the route.
            Table(up,"Gallery console",new Vector3(5,3,11),new Vector2(.7f,1.3f),.7f);Model("PROP_CeramicVase",up,new Vector3(5,3.75f,11),.38f);
        }

        static void Quests()
        {
            var water=Repair("Latch main water",low,new Vector3(23,.75f,17),HouseChore.Mechanic.Relay,"Valve open for 35 seconds");water.relaySeconds=35;Model("PROP_WaterPump",low,new Vector3(23,0,17),.65f);
            var pipe=Repair("Seal kitchen pipe",low,new Vector3(24.7f,.5f,8),HouseChore.Mechanic.Sequence,"Tighten / seal / test");pipe.gate=water;pipe.movingPart=Model("PROP_PipeElbow",low,new Vector3(24.7f,.15f,8),.4f);
            var router=Repair("Restore router signal",low,new Vector3(10,.62f,7.1f),HouseChore.Mechanic.Timing,"Three stable pulses");router.prerequisite=pipe;Model("PROP_Router",low,new Vector3(10,.58f,7.1f),.2f);
            Deliver("Return lounge cushion","PROP_SeatCushion",low,new Vector3(6,.02f,6),.28f,low,new Vector3(1.6f,.66f,3));
            var trash=Deliver("Bag rubbish in utility","PROP_TrashBag",low,new Vector3(16,.02f,6),.5f,low,new Vector3(19.2f,.2f,17));Model("PROP_OpenBin",low,new Vector3(19.2f,0,17),.8f);
            Deliver("Save leftover pizza","PROP_PizzaBox",low,new Vector3(4.1f,.61f,3.6f),.15f,low,new Vector3(15,.92f,3.5f));
            Deliver("Bring teddy to bed","PROP_PlushTeddy",low,new Vector3(7,.02f,12.5f),.55f,up,new Vector3(2.2f,3.82f,2.6f));
            Deliver("Return escaped fish","PET_Goldfish",low,new Vector3(21,.02f,12.5f),.18f,up,new Vector3(9,3.72f,1.3f));
            var washer=Repair("Latch washer drum",low,new Vector3(24.1f,1.15f,19.3f),HouseChore.Mechanic.Relay,"Bring a towel before the drum restarts");washer.relaySeconds=35;
            var towel=Deliver("Load missing bath towel","PROP_FoldedTowel",up,new Vector3(11.3f,3.96f,4.8f),.12f,low,new Vector3(24.1f,.55f,18.8f));towel.gate=washer;
            Repair("Balance cake fan",up,new Vector3(15,3.9f,13),HouseChore.Mechanic.Timing,"Time three separate sweeps");
            Deliver("Bring bike repair tools","PROP_Toolbox",low,new Vector3(20.5f,.91f,19),.3f,low,new Vector3(12.5f,.12f,18.5f));

            // A catalog of 50 authored household beats: distinct endpoints/tools, no stacked task kiosks.
            Deliver("Bring controller to game table","PROP_Router",low,new Vector3(4.8f,.02f,9.5f),.14f,low,new Vector3(7.3f,.74f,15.5f));
            Deliver("Return picture to entry bench","PROP_PizzaBox",up,new Vector3(5.2f,.02f+3,8.5f),.1f,low,new Vector3(10.7f,.58f,7.1f));
            Deliver("Put spare cushion on kids bed","PROP_SeatCushion",low,new Vector3(7.8f,.02f,18.5f),.25f,up,new Vector3(2.1f,3.82f,3.2f));
            Deliver("Return Mama's cushion","PROP_SeatCushion",up,new Vector3(7,.02f+3,15),.25f,up,new Vector3(14.7f,3.82f,3.1f));
            Deliver("Store craft tools","PROP_Toolbox",low,new Vector3(16.8f,.91f,18.5f),.27f,up,new Vector3(15,3.89f,19.1f));
            Deliver("Take spare pipe to garage","PROP_PipeStraight",low,new Vector3(21.5f,.02f,16),.35f,low,new Vector3(16.8f,.91f,17.3f));
            Deliver("Put vase on bedside table","PROP_CeramicVase",up,new Vector3(16,.02f+3,8.5f),.3f,up,new Vector3(4.3f,3.66f,1.3f));
            Deliver("Sort second rubbish bag","PROP_TrashBag",low,new Vector3(6,.02f,17.7f),.45f,low,new Vector3(20.2f,.2f,17));Model("PROP_OpenBin",low,new Vector3(20.2f,0,17),.8f);
            Deliver("Return kitchen towel","PROP_FoldedTowel",low,new Vector3(15.7f,.02f,9.5f),.1f,low,new Vector3(22,.96f,10.5f));
            Deliver("Carry picnic leftovers","PROP_PizzaBox",low,new Vector3(9.7f,.02f,-1),.14f,low,new Vector3(15,.92f,4.3f));
            Deliver("Rescue plush from garage","PROP_PlushTeddy",low,new Vector3(13,.02f,13),.42f,low,new Vector3(5.2f,1.7f,19.45f));
            Deliver("Return hobby-room bucket","PROP_Bucket",up,new Vector3(16,.02f+3,12),.38f,low,new Vector3(21,.05f,19));
            Deliver("Put pump on garage bench","PROP_WaterPump",low,new Vector3(15,.02f,12),.45f,low,new Vector3(16.8f,.91f,18.3f));
            Deliver("Move birthday cake to table","PROP_CakeSlice",up,new Vector3(9,3.89f,19.1f),.18f,low,new Vector3(15,.92f,2.8f));
            Deliver("Carry extinguisher downstairs","PROP_Extinguisher",up,new Vector3(5,.02f+3,16.5f),.48f,low,new Vector3(20,.05f,13));

            var tv=Repair("Retune noisy television",low,new Vector3(4.1f,1.1f,.7f),HouseChore.Mechanic.Timing,"Three quiet channels");
            Repair("Tighten dining chair",low,new Vector3(16.65f,.5f,2.5f),HouseChore.Mechanic.Sequence,"Brace / tighten / check");
            Repair("Level the game table",low,new Vector3(7.3f,.75f,15.5f),HouseChore.Mechanic.Timing,"Find the level mark");
            Repair("Adjust garage brake cable",low,new Vector3(12.4f,.6f,18.5f),HouseChore.Mechanic.Sequence,"Loosen / align / tighten");
            Repair("Prime utility pump",low,new Vector3(23,.7f,16),HouseChore.Mechanic.Timing,"Catch the pressure pulses").prerequisite=pipe;
            Repair("Reset oven timer",low,new Vector3(25.3f,1.1f,10.3f),HouseChore.Mechanic.Timing,"Stop the beeping");
            Repair("Secure fridge handle",low,new Vector3(19.3f,1.1f,12.5f),HouseChore.Mechanic.Sequence,"Align / fasten / close");
            Repair("Unblock bath drain",up,new Vector3(9,3.8f,1.3f),HouseChore.Mechanic.Sequence,"Clear / plunge / flush");
            Repair("Tune study radio",up,new Vector3(9,3.95f,19),HouseChore.Mechanic.Timing,"Find a clean signal").prerequisite=router;
            Repair("Fix craft stool",up,new Vector3(15,3.5f,17.7f),HouseChore.Mechanic.Sequence,"Brace / fasten / test");
            Repair("Tighten kids bed frame",up,new Vector3(2.2f,3.4f,2),HouseChore.Mechanic.Sequence,"Lift / tighten / lower");
            Repair("Quiet Mama's wardrobe",up,new Vector3(17,4,3),HouseChore.Mechanic.Timing,"Catch the loose latch");
            Repair("Unstick bathroom drawer",up,new Vector3(11.2f,3.8f,4.2f),HouseChore.Mechanic.Sequence,"Pull / align / slide");
            Repair("Balance dryer load",low,new Vector3(25.25f,1.15f,19.3f),HouseChore.Mechanic.Timing,"Reduce the rattle").prerequisite=washer;
            Repair("Repair porch hinge",low,new Vector3(8.98f,1,.9f),HouseChore.Mechanic.Sequence,"Support / tighten / test");
            Repair("Synchronise living lamp",low,new Vector3(1.3f,1.05f,6.3f),HouseChore.Mechanic.Timing,"Stop flickering").prerequisite=tv;
            var workshop=Repair("Latch hobby clamp",up,new Vector3(15.9f,3.94f,19.1f),HouseChore.Mechanic.Relay,"Run for the toolbox");workshop.relaySeconds=35;
            var craft=Repair("Secure hobby shelf",up,new Vector3(17.3f,4.2f,13),HouseChore.Mechanic.Sequence,"Brace / tighten / test");craft.gate=workshop;
            var sink=Repair("Latch kitchen supply",low,new Vector3(25.3f,1,12.5f),HouseChore.Mechanic.Relay,"Run upstairs to the bath");sink.relaySeconds=45;
            Repair("Seal bathroom tap",up,new Vector3(11.3f,3.95f,5.5f),HouseChore.Mechanic.Sequence,"Fit / tighten / pressure test").gate=sink;
            Repair("Sort study cable plugs",up,new Vector3(8,3.94f,19.1f),HouseChore.Mechanic.Sequence,"Unplug / untangle / reconnect");
            Repair("Tune game-room speaker",low,new Vector3(5.2f,1.65f,19.45f),HouseChore.Mechanic.Timing,"Three clean beats").prerequisite=router;
            Repair("Final family-room sound check",low,new Vector3(6.8f,.8f,.65f),HouseChore.Mechanic.Timing,"Confirm the room is quiet").prerequisite=tv;
            if(tasks.Count!=50)throw new InvalidOperationException("Expected exactly 50 authored tasks, got "+tasks.Count);
        }
        static void Roofs()
        {
            // Main gable spans the full two-storey body; separate pitched roof covers the lower wing.
            PitchedRoof(roof,"Main gabled roof",-.45f,-.45f,18.45f,20.45f,6.05f,2.5f);
            PitchedRoof(roof,"Kitchen utility roof",17.8f,5.6f,26.45f,20.45f,3.03f,1.3f);
            PitchedRoof(roof,"Porch canopy",7.4f,-2.4f,12.6f,.2f,2.62f,.5f);
            Box("Main ceiling",roof,new Vector3(9,5.93f,10),new Vector3(18,.12f,20),"Ceiling","F3DFC0");Box("Wing ceiling",roof,new Vector3(22,2.93f,13),new Vector3(8,.12f,14),"Ceiling","F3DFC0");
            Box("Chimney",roof,new Vector3(14.2f,7.4f,15.5f),new Vector3(1.1f,2.3f,1),"Brick","A05F4E");Box("Chimney cap",roof,new Vector3(14.2f,8.6f,15.5f),new Vector3(1.3f,.18f,1.2f),"Cream","F9EACD");
        }
        static void SnapSupportedItems()
        {
            Physics.SyncTransforms();
            foreach(var task in tasks)
            {
                if(!task.requiredItem)continue;
                var item=task.requiredItem;
                var source=Physics.RaycastAll(item.transform.position+Vector3.up*.35f,Vector3.down,1.5f,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>!h.transform.IsChildOf(item.transform)).OrderBy(h=>h.distance).FirstOrDefault();
                if(source.collider)item.transform.position=new Vector3(item.transform.position.x,source.point.y+.015f,item.transform.position.z);
                if(task.deliveryPoint)
                {
                    var target=Physics.RaycastAll(task.deliveryPoint.position+Vector3.up*.3f,Vector3.down,1.2f,~0,QueryTriggerInteraction.Ignore)
                        .Where(h=>!h.transform.IsChildOf(item.transform)).OrderBy(h=>h.distance).FirstOrDefault();
                    if(target.collider)task.deliveryPoint.position=new Vector3(task.deliveryPoint.position.x,target.point.y+.015f,task.deliveryPoint.position.z);
                }
            }
        }
        static void PitchedRoof(Transform par,string name,float x0,float z0,float x1,float z1,float eave,float rise)
        {
            float mid=(x0+x1)*.5f,half=(x1-x0)*.5f,length=Mathf.Sqrt(half*half+rise*rise),angle=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
            foreach(int side in new[]{-1,1})
            {
                var g=Box(name+(side<0?" left":" right"),par,new Vector3(mid+side*half*.5f,eave+rise*.5f,(z0+z1)*.5f),new Vector3(length,.16f,z1-z0),"RoofSlate","3B5964");g.transform.rotation=Quaternion.Euler(0,0,-side*angle);
                for(float z=z0;z<=z1;z+=.65f){var seam=Box("Roof standing seam",par,new Vector3(mid+side*half*.5f,eave+rise*.5f+.1f,z),new Vector3(length,.035f,.035f),"RoofEdge","527783",false);seam.transform.rotation=g.transform.rotation;}
            }
            // Filled triangular gables are custom mesh assets, not an open triangle under the roof.
            var mesh=new Mesh{name=name+" gable"};mesh.vertices=new[]{new Vector3(x0,eave,z0),new Vector3(x1,eave,z0),new Vector3(mid,eave+rise,z0),new Vector3(x0,eave,z1),new Vector3(mid,eave+rise,z1),new Vector3(x1,eave,z1)};mesh.triangles=new[]{0,2,1,3,5,4};mesh.RecalculateNormals();
            mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.forward,Vector3.forward,Vector3.forward};mesh.RecalculateBounds();
            string path=Mats+name.Replace(" ","_")+"_gable.asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior){prior.Clear();prior.vertices=mesh.vertices;prior.triangles=mesh.triangles;prior.normals=mesh.normals;prior.RecalculateBounds();EditorUtility.SetDirty(prior);Object.DestroyImmediate(mesh);mesh=prior;}else AssetDatabase.CreateAsset(mesh,path);
            var front=Group(name+" closed gables",par).gameObject;front.AddComponent<MeshFilter>().sharedMesh=mesh;front.AddComponent<MeshRenderer>().sharedMaterial=Mat("Cream","F9EACD");
        }
        static PlayerController MakePlayer()
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MamasComing/Prefabs/Characters/PF_AppleKid.prefab"));PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name="Playable fruit • 1–5";go.transform.position=new Vector3(10,.06f,3);
            var baked=go.transform.Find("BlobShadow/BlobShadowQuad");if(baked)Object.DestroyImmediate(baked.gameObject);
            var fall=Group("FallPivot",go.transform);go.transform.Find("Visual").SetParent(fall,false);
            var pc=go.AddComponent<PlayerController>();pc.fallPivot=fall;pc.holdPoint=Group("HoldPoint",go.transform);pc.holdPoint.localPosition=new Vector3(0,.62f,.5f);pc.dragPoint=Group("DragPoint",go.transform);pc.dragPoint.localPosition=new Vector3(0,0,.95f);
            pc.actions=go.AddComponent<KidActionAnimator>();pc.actions.player=pc;go.GetComponent<DreamRush.Player.PlayerMotor3D>().Config=AssetDatabase.LoadAssetAtPath<DreamRush.Player.PlayerMotorConfig>("Assets/MamasComing/KidMotorConfig.asset");go.AddComponent<KidBody>().player=pc;go.AddComponent<KidRoughhouse>();return pc;
        }
        static HouseChore Repair(string title,Transform par,Vector3 at,HouseChore.Mechanic kind,string instruction)
        {
            var g=Group((tasks.Count+1).ToString("00")+" "+title,par);g.position=at;
            var t=g.gameObject.AddComponent<HouseChore>();t.displayName=g.name;t.mechanic=kind;t.instruction=instruction;t.interactRadius=1.65f;
            t.lamp=Sphere("Small interaction indicator",g,at+Vector3.up*.12f,Vector3.one*.07f,"Brass","D8AA4D").GetComponent<Renderer>();tasks.Add(t);return t;
        }
        static HouseChore Deliver(string title,string model,Transform sourceParent,Vector3 from,float h,Transform targetParent,Vector3 to)
        {
            var g=Group("Carry • "+title,sourceParent);g.position=from;Model(model,g,from,h);
            var rb=g.gameObject.AddComponent<Rigidbody>();rb.mass=.7f;rb.isKinematic=true;
            var c=g.gameObject.AddComponent<BoxCollider>();c.center=Vector3.up*h*.5f;c.size=new Vector3(Mathf.Max(.2f,h*.7f),h,Mathf.Max(.2f,h*.6f));
            var p=g.gameObject.AddComponent<Pickup>();p.displayName=title;p.startKinematic=true;p.grabTime=.5f;p.holdOffset=new Vector3(0,-.08f,.1f);
            var t=Repair(title,targetParent,to,HouseChore.Mechanic.Delivery,"Find the matching home spot");t.requiredItem=p;t.deliveryPoint=Group("Supported delivery point",targetParent);t.deliveryPoint.position=to;return t;
        }
        static void Floor(Transform par,string name,float x0,float z0,float x1,float z1,float y,string color)
        {
            Box("Floor • "+name,par,new Vector3((x0+x1)*.5f,y-.1f,(z0+z1)*.5f),new Vector3(x1-x0,.2f,z1-z0),"Floor"+color,color);
            for(float x=x0+.75f;x<x1;x+=.75f)Box("Plank seam",par,new Vector3(x,y+.001f,(z0+z1)*.5f),new Vector3(.008f,.002f,z1-z0),"Seam","B18B68",false);
            var g=new GameObject("PlanLabel_"+name,typeof(TextMesh));g.transform.SetParent(par);g.transform.position=new Vector3((x0+x1)*.5f,y+.05f,(z0+z1)*.5f);g.transform.rotation=Quaternion.Euler(90,0,0);var t=g.GetComponent<TextMesh>();t.text=name;t.fontSize=60;t.characterSize=.09f;t.anchor=TextAnchor.MiddleCenter;t.color=Hex("253947");g.GetComponent<Renderer>().enabled=false;
        }
        static void Wall(Transform par,float x0,float z0,float x1,float z1,float y,string color)
        {
            var size=new Vector3(Mathf.Max(.18f,Mathf.Abs(x1-x0)),2.85f,Mathf.Max(.18f,Mathf.Abs(z1-z0)));
            var g=Box("Wall",par,new Vector3((x0+x1)*.5f,y+1.425f,(z0+z1)*.5f),size,"Wall"+color,color);Cut(g,x0!=x1?Vector3.forward:Vector3.right,Mathf.Max(size.x,size.z));
            Box("Skirting",g.transform,new Vector3((x0+x1)*.5f,y+.08f,(z0+z1)*.5f),new Vector3(size.x+.04f,.16f,size.z+.04f),"Cream","F9EACD",false);
        }
        static void DoorWall(Transform par,float x0,float z0,float x1,float z1,float door,float y,string color)
        {
            bool x=x0!=x1;float a=door-.95f,b=door+.95f;
            if(x){Wall(par,x0,z0,a,z1,y,color);Wall(par,b,z0,x1,z1,y,color);}else{Wall(par,x0,z0,x1,a,y,color);Wall(par,x0,b,x1,z1,y,color);}
            Box("Door lintel",par,new Vector3(x?door:x0,y+2.52f,x?z0:door),x?new Vector3(1.9f,.66f,.18f):new Vector3(.18f,.66f,1.9f),"Cream","F9EACD");
        }
        static void Exterior(Transform par,float x0,float z0,float x1,float z1,float y,bool windows)
        {
            bool x=x0!=x1;float a=x?x0:z0,b=x?x1:z1;float length=b-a;
            if(!windows||length<3){Wall(par,x0,z0,x1,z1,y,"E3CBA6");return;}
            int count=Mathf.Max(1,Mathf.FloorToInt(length/4f));float cursor=a;
            for(int i=0;i<count;i++)
            {
                float center=a+length*(i+.5f)/count,left=center-.75f,right=center+.75f;
                if(x)Wall(par,cursor,z0,left,z0,y,"E3CBA6");else Wall(par,x0,cursor,x0,left,y,"E3CBA6");
                var baseP=new Vector3(x?center:x0,y,x?z0:center);
                var lowPanel=Box("Under window",par,baseP+Vector3.up*.43f,x?new Vector3(1.5f,.86f,.18f):new Vector3(.18f,.86f,1.5f),"WallE3CBA6","E3CBA6");Cut(lowPanel,x?Vector3.forward:Vector3.right,1.5f);
                var head=Box("Above window",par,baseP+Vector3.up*2.5f,x?new Vector3(1.5f,.7f,.18f):new Vector3(.18f,.7f,1.5f),"WallE3CBA6","E3CBA6");Cut(head,x?Vector3.forward:Vector3.right,1.5f);
                var frame=Group("Window frame",par);frame.position=baseP;
                foreach(float h in new[]{.9f,2.1f})Box("Window sill",frame,baseP+Vector3.up*h,x?new Vector3(1.65f,.1f,.28f):new Vector3(.28f,.1f,1.65f),"Cream","F9EACD",false);
                foreach(float offset in new[]{-.77f,0,.77f})Box("Window mullion",frame,baseP+Vector3.up*1.5f+(x?Vector3.right:Vector3.forward)*offset,x?new Vector3(.07f,1.2f,.16f):new Vector3(.16f,1.2f,.07f),"Cream","F9EACD",false);
                // Intentionally clear openings: no opaque blue wall masquerading as glass.
                cursor=right;
            }
            if(x)Wall(par,cursor,z0,b,z0,y,"E3CBA6");else Wall(par,x0,cursor,x0,b,y,"E3CBA6");
        }
        static void Cut(GameObject g,Vector3 normal,float length){var c=g.AddComponent<CutawayWall>();c.normal=normal;c.halfExtent=new Vector2(length*.5f,1.5f);}
        static void Rail(Transform par,Vector3 p,float len,bool z){Box("Gallery handrail",par,p+Vector3.up*.9f,z?new Vector3(.1f,.1f,len):new Vector3(len,.1f,.1f),"Cream","F9EACD");for(float i=-len*.5f;i<=len*.5f;i+=.45f)Box("Gallery baluster",par,p+Vector3.up*.45f+(z?Vector3.forward:Vector3.right)*i,new Vector3(.07f,.9f,.07f),"Cream","F9EACD");}
        static void Table(Transform par,string name,Vector3 p,Vector2 size,float h){var root=Group(name,par);root.position=p;Box("Table top",root,p+Vector3.up*h,new Vector3(size.x,.12f,size.y),"Wood","A66A43");for(int i=0;i<4;i++)Box("Table leg",root,p+new Vector3((i%2-.5f)*(size.x-.2f),(h-.06f)*.5f,(i/2-.5f)*(size.y-.2f)),new Vector3(.1f,h-.06f,.1f),"Wood","A66A43");}
        static void Chair(Transform par,Vector3 p,float yaw)
        {
            var root=Group("Chair assembly • faces table",par);root.position=p;
            Box("Seat",root,p+Vector3.up*.45f,new Vector3(.58f,.12f,.58f),"Teal","277D79");Sphere("Seat pad",root,p+Vector3.up*.53f,new Vector3(.53f,.12f,.53f),"Gold","DAB764");Box("Back",root,p+new Vector3(0,.85f,-.26f),new Vector3(.58f,.7f,.1f),"Teal","277D79");
            for(int i=0;i<4;i++)Box("Chair leg",root,p+new Vector3((i%2-.5f)*.46f,.2f,(i/2-.5f)*.46f),new Vector3(.075f,.4f,.075f),"Wood","A66A43");root.rotation=Quaternion.Euler(0,yaw,0);
        }
        static void Cabinet(Transform par,string name,Vector3 p,Vector2 size,float h){Box(name,par,p+Vector3.up*h*.5f,new Vector3(size.x,h,size.y),"Teal","277D79");Box(name+" top",par,p+Vector3.up*(h+.035f),new Vector3(size.x+.06f,.07f,size.y+.06f),"Cream","F9EACD");}
        static void Bed(Transform par,Vector3 p,string name){Box(name+" frame",par,p+Vector3.up*.3f,new Vector3(2.2f,.6f,3.3f),"Wood","A66A43");Sphere(name+" mattress",par,p+Vector3.up*.66f,new Vector3(2.15f,.3f,3.2f),"Cream","F9EACD").AddComponent<BoxCollider>();Box(name+" quilt",par,p+new Vector3(0,.81f,.55f),new Vector3(2.16f,.12f,2),"Lilac","A382B7");Sphere(name+" pillow",par,p+new Vector3(0,.87f,-1.05f),new Vector3(1.6f,.26f,.7f),"Gold","DAB764");Box(name+" headboard",par,p+new Vector3(0,.8f,-1.7f),new Vector3(2.3f,1.5f,.13f),"Wood","A66A43");}
        static void Rug(Transform par,Vector3 p,Vector2 size,string color){Box("Rug",par,p+Vector3.up*.015f,new Vector3(size.x,.03f,size.y),"Rug"+color,color,false);}
        static void Lamp(Transform par,Vector3 p){Cylinder("Lamp stem",par,p+Vector3.up*.2f,new Vector3(.035f,.2f,.035f),"Brass","D8AA4D");Sphere("Lamp shade",par,p+Vector3.up*.46f,new Vector3(.45f,.4f,.45f),"Gold","DAB764");var l=new GameObject("Warm practical",typeof(Light)).GetComponent<Light>();l.transform.SetParent(par);l.transform.position=p+Vector3.up*.35f;l.type=LightType.Point;l.color=Hex("FFD09B");l.range=3.5f;l.intensity=.7f;}
        static void Bookcase(Transform par,Vector3 p,float width){Box("Bookcase back",par,p+Vector3.up*.9f,new Vector3(width,1.8f,.12f),"Wood","A66A43");for(int j=0;j<3;j++){Box("Book shelf",par,p+new Vector3(0,.1f+j*.65f,-.2f),new Vector3(width,.08f,.5f),"Wood","A66A43");for(int i=0;i<5;i++)Box("Book",par,p+new Vector3((i-2)*.32f,.35f+j*.65f,-.2f),new Vector3(.13f,.45f,.28f),i%2==0?"Gold":"Coral",i%2==0?"DAB764":"D86E5C",false);}}
        static Transform Model(string model,Transform par,Vector3 p,float h){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+model+".fbx");if(!asset)throw new InvalidOperationException("Missing "+model);var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);go.transform.SetParent(par);go.transform.position=p;var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);go.transform.localScale*=h/Mathf.Max(.01f,b.size.y);b=rs[0].bounds;foreach(var r in rs){b.Encapsulate(r.bounds);r.sharedMaterials=Enumerable.Repeat(AssetDatabase.LoadAssetAtPath<Material>("Assets/MamasComing/Art/Recast/Materials/MamaPainted.mat"),r.sharedMaterials.Length).ToArray();}go.transform.position+=p-new Vector3(b.center.x,b.min.y,b.center.z);return go.transform;}
        static Transform Group(string name,Transform par=null){var t=new GameObject(name).transform;t.SetParent(par,false);return t;}
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static Material Mat(string name,string color){if(mats.TryGetValue(name,out var m))return m;string path=Mats+name+".mat";m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",Hex(color));m.SetFloat("_Smoothness",.25f);mats[name]=m;EditorUtility.SetDirty(m);return m;}
        static GameObject Primitive(PrimitiveType type,string name,Transform par,Vector3 p,Vector3 size,string mat,string color,bool collider){var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(par);g.transform.position=p;var s=par?par.lossyScale:Vector3.one;g.transform.localScale=new Vector3(size.x/s.x,size.y/s.y,size.z/s.z);g.GetComponent<Renderer>().sharedMaterial=Mat(mat,color);if(!collider)Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static GameObject Box(string n,Transform p,Vector3 at,Vector3 size,string m,string c,bool collision=true)=>Primitive(PrimitiveType.Cube,n,p,at,size,m,c,collision);
        static GameObject Sphere(string n,Transform p,Vector3 at,Vector3 size,string m,string c)=>Primitive(PrimitiveType.Sphere,n,p,at,size,m,c,false);
        static GameObject Cylinder(string n,Transform p,Vector3 at,Vector3 size,string m,string c)=>Primitive(PrimitiveType.Cylinder,n,p,at,size,m,c,false);
    }
}
