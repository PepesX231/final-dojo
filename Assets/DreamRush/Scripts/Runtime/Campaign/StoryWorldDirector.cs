using System.Collections;
using DreamRush.Core;
using DreamRush.Dream;
using DreamRush.Flow;
using DreamRush.Player;
using DreamRush.Presentation;
using DreamRush.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DreamRush.Campaign
{
    public enum StoryWorldKind { ToyBoxMoon=3, BubbleBathSea=4, MidnightTeacup=5 }
    public sealed class StoryWorldDirector : MonoBehaviour
    {
        public StoryWorldKind world;
        public string title,subtitle;
        public Transform landmark,finale,spawn;
        public StoryMechanism[] mechanisms;
        public GameObject[] unlockedPaths;
        public Transform boat;
        public Vector3 boatDestination;
        public float bpm=84f;
        public bool playArrival=true;
        public int Stage {get;private set;}
        public int FinaleHits {get;private set;}
        public bool ReadyForRest {get;private set;}
        public bool Won {get;private set;}
        public float Clock {get;private set;}
        public float Beat => Mathf.Repeat((Audio.AudioManager.MusicIsPlaying?Audio.AudioManager.MusicTime:Clock)*bpm/60f,1f);
        public bool Busy {get;private set;}
        public static StoryWorldDirector Current {get;private set;}
        PlayerMotor3D _player;Text _beat;GameObject _hud;
        Vector3 _boatHome;bool _boatDeparted;float _boatT,_hazardCool,_lastBeat=-1,_worldBpm;
        public event System.Action<int> StageCompleted;
        void Awake(){Current=this;_worldBpm=bpm;if(boat!=null)_boatHome=boat.position;}
        IEnumerator Start()
        {
            _player=FindFirstObjectByType<PlayerMotor3D>();
            BuildHud();Refresh();yield return null;
            if(playArrival&&landmark!=null&&spawn!=null){
                Busy=true;var p=LandmarkFocus();
                yield return DreamCinematic.Play(
                    new DreamCinematic.Shot{from=p+new Vector3(-23,16,-26),to=p+new Vector3(-16,10,-19),lookAt=p,seconds=2.7f,fovFrom=62,fovTo=48,caption=title},
                    new DreamCinematic.Shot{from=spawn.position+new Vector3(5,2,-7),to=spawn.position+new Vector3(2,1.8f,-4),lookAt=spawn.position+Vector3.up,seconds=2, fovFrom=48,fovTo=54,caption=subtitle});
                Busy=false;
            }
        }
        Vector3 LandmarkFocus(){
            if(landmark==null)return finale!=null?finale.position:transform.position;
            var renderers=landmark.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return landmark.position;
            var bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);return bounds.center;
        }
        void BuildHud(){
            var rt=UIFactory.Rect("StoryWorldObjectives",UIRoot.Get().Hud);_hud=rt.gameObject;
            rt.anchorMin=rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(1,1);rt.anchoredPosition=new Vector2(-28,-30);rt.sizeDelta=new Vector2(560,74);
            var panel=UIFactory.Panel("Paper",rt,new Color(.05f,.07f,.12f,.88f));UIFactory.Stretch(panel.rectTransform);
            _beat=UIFactory.Label("Beat",rt,"",22,new Color(.5f,.91f,.86f),TextAnchor.MiddleLeft);UIFactory.Stretch(_beat.rectTransform,18);
        }
        void Update()
        {
            if(Won||Busy||DreamCinematic.Active||Time.timeScale<=0)return;
            Clock+=Time.deltaTime;_hazardCool-=Time.deltaTime;
            if(_beat!=null){
                int dots=Mathf.Clamp(Mathf.FloorToInt(Beat*8),0,7);
                _beat.text=world==StoryWorldKind.BubbleBathSea ? (Beat<.5f?"กระแสน้ำสงบ — สังเกตลูกศรที่วาล์ว":"คลื่นกำลังมา — รอจังหวะปลอดภัย") : "จังหวะ  "+new string('◆',dots+1)+new string('◇',7-dots);
            }
            if(Stage>=3&&world==StoryWorldKind.BubbleBathSea&&boat!=null){
                if(!_boatDeparted){
                    var ride=boat.GetComponent<World1.MovingPlatform>();
                    _boatDeparted=(ride!=null&&ride.PlayerOnTop)||(_player!=null&&Vector3.Distance(_player.transform.position,boatDestination)<18f);
                    if(!_boatDeparted)return;
                }
                _boatT=Mathf.Min(1f,_boatT+Time.deltaTime/18f);
                var platform=boat.GetComponent<World1.MovingPlatform>();
                if(platform!=null)platform.AutoMove=false;
                boat.position=Vector3.Lerp(_boatHome,boatDestination,Mathf.SmoothStep(0,1,_boatT));
                if(_boatT>=1f&&!ReadyForRest){ReadyForRest=true;bpm=_worldBpm;Audio.AudioManager.PlayMusic(Audio.Music.BubbleBathSea,2);Refresh();UIRoot.Get().Toast("ถึงประภาคารแล้ว — นำเรือเข้าฝั่ง",3);}
            }
            // Finale attacks are announced by the shrinking beat ring; no random contact damage.
            if(Stage>=3&&!ReadyForRest&&world!=StoryWorldKind.BubbleBathSea&&_player!=null&&finale!=null){
                if(Beat<.14f&&_lastBeat>.8f&&_hazardCool<=0&&Vector3.Distance(_player.transform.position,finale.position)<7f){
                    VFX.Play(VFXKind.LandingRing,finale.position,5f,VFX.Danger);
                    if(_player.IsGrounded){SleepStability.Current?.Damage(world==StoryWorldKind.ToyBoxMoon?"ลมของดวงจันทร์ — กระโดดข้ามวงคลื่น":"เสียงเคาะ — กระโดดข้ามแรงสั่น");_hazardCool=1.2f;}
                }
            }
            _lastBeat=Beat;
        }
        public bool Use(StoryMechanism node,PlayerMotor3D player)
        {
            if(Won||Busy||DreamCinematic.Active||SleepStability.Current!=null&&SleepStability.Current.Depleted||player==null||Vector3.Distance(player.transform.position,node.transform.position)>3.3f)return false;
            if(node.finalRest){
                if(!ReadyForRest){UIRoot.Get().Toast(world==StoryWorldKind.BubbleBathSea?"เปิดทางน้ำให้ครบ แล้วขึ้นเรือไปประภาคาร":"ทำให้เสียงสุดท้ายสงบก่อน",2);return false;}
                StartCoroutine(Rest(player,node));return true;
            }
            if(node.finaleControl){
                if(Stage<3||ReadyForRest)return false;
                bool open=node.Assisted || Beat>.38f&&Beat<.8f;
                if(!open){UIRoot.Get().Toast("รอจังหวะวงแหวนหด แล้วกดอีกครั้ง",1.5f);return false;}
                if(!node.TryCooldown(.7f))return false;
                FinaleHits++;Audio.AudioManager.Play(Audio.Sfx.BellToll,node.transform.position,.8f);
                VFX.Play(VFXKind.ImpactRing,node.transform.position,2f,VFX.Gold);
                if(landmark!=null)landmark.localScale*=.92f;
                if(FinaleHits>=3){ReadyForRest=true;StartCoroutine(RevealRest());}
                Refresh();return true;
            }
            if(node.stage!=Stage){UIRoot.Get().Toast("ฟังเสียง / ตามสัญลักษณ์ "+(Stage+1),1.5f);return false;}
            if(world==StoryWorldKind.BubbleBathSea){
                node.turns=(node.turns+1)%4;node.SetRotation();
                if(node.turns!=node.correctTurns){Audio.AudioManager.Play(Audio.Sfx.Ratchet,node.transform.position,.8f);return true;}
            } else {
                float target=world==StoryWorldKind.ToyBoxMoon?.5f:node.targetBeat;
                float distance=Mathf.Abs(Mathf.DeltaAngle(Beat*360,target*360))/360f;
                float window=SaveManager.GetOrCreate().Data.settings.reduceFlashing?.31f:.24f;
                if(!node.Assisted&&distance>window){UIRoot.Get().Toast("ฟังจังหวะก่อน… ลองใหม่ได้ ไม่มีการรีเซ็ตเส้นทาง",1.5f);Audio.AudioManager.Play(Audio.Sfx.Clank,node.transform.position,.35f);return false;}
            }
            node.Completed=true;Stage++;
            if(unlockedPaths!=null)for(int i=0;i<unlockedPaths.Length;i++)if(unlockedPaths[i]!=null)unlockedPaths[i].SetActive(i<Stage);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime,node.transform.position,.8f);VFX.Play(VFXKind.StarBurst,node.transform.position,1.6f,VFX.Gold);
            StageCompleted?.Invoke(Stage);Refresh();
            if(Stage==3)StartCoroutine(RevealFinale());return true;
        }
        IEnumerator RevealFinale(){
            Busy=true;bpm=112;Audio.AudioManager.PlayMusic(Audio.Music.DreamFinale,1.2f);var p=LandmarkFocus();
            yield return DreamCinematic.Play(new DreamCinematic.Shot{from=p+new Vector3(10,4,-14),to=p+new Vector3(6,2,-8),lookAt=p,seconds=2.5f,fovFrom=60,fovTo=40,caption=world==StoryWorldKind.BubbleBathSea?"THE SEA OPENS — พาเรือกลับบ้าน":"THE LAST NOTE — ฟัง แล้วตอบกลับ",impact=true});
            Busy=false;
        }
        IEnumerator RevealRest(){
            Busy=true;bpm=_worldBpm;Audio.AudioManager.PlayMusic(world==StoryWorldKind.ToyBoxMoon?Audio.Music.ToyBoxMoon:Audio.Music.MidnightTeacup,1.5f);yield return DreamCinematic.Play(new DreamCinematic.Shot{from=finale.position+new Vector3(7,3,-9),to=finale.position+new Vector3(3,2,-5),lookAt=finale.position+Vector3.up,seconds=2.3f,fovFrom=50,fovTo=42,caption="เงียบแล้ว… ถึงเวลาพัก"});Busy=false;
        }
        IEnumerator Rest(PlayerMotor3D player,StoryMechanism node){
            Busy=true;UIRoot.Get().Toast("พักอยู่ตรงนี้สักครู่…",2.4f);
            float held=0;while(held<2f){if(player==null||Vector3.Distance(player.transform.position,node.transform.position)>3.3f){Busy=false;yield break;}held+=Time.deltaTime;yield return null;}
            Won=true;Audio.AudioManager.Play(Audio.Sfx.Victory,node.transform.position,.8f);
            yield return DreamCinematic.Play(new DreamCinematic.Shot{from=player.transform.position+new Vector3(5,2,-6),to=player.transform.position+new Vector3(2,1.6f,-4),lookAt=player.transform.position+Vector3.up,seconds=2.2f,fovFrom=54,fovTo=38,caption=world==StoryWorldKind.ToyBoxMoon?"GOODNIGHT, LITTLE MOON":world==StoryWorldKind.BubbleBathSea?"EVERY SHIP DESERVES A SHORE":"THE NIGHT CAN REST NOW",impact=false});
            GameFlowManager.Get().EndDream(true,world==StoryWorldKind.ToyBoxMoon?"MoonLullaby":world==StoryWorldKind.BubbleBathSea?"SafeHarbor":"QuietTeacup",null);
        }
        void Refresh(){

            string action=world==StoryWorldKind.ToyBoxMoon?"เล่นทำนองกล่อมดวงจันทร์":world==StoryWorldKind.BubbleBathSea?"หมุนวาล์วตามลูกศร · เปิดทางน้ำ":"ฟังและตีระฆังตามลำดับ";
            DreamHUD.Current?.SetObjective((Stage<3?$"{action}  {Stage}/3":ReadyForRest?"เข้าหาจุดพัก · กด E แล้วพักสองวินาที":world==StoryWorldKind.BubbleBathSea?"ขึ้นเรือไปประภาคาร · หรือใช้เส้นทางฟองสบู่":$"จังหวะสุดท้าย  {FinaleHits}/3 · E เมื่อวงแหวนหด"));
        }
        void OnDestroy(){if(_hud!=null)Destroy(_hud);if(Current==this)Current=null;}
    }
}
