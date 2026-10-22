using DreamRush.DreamLogic;
using DreamRush.Player;
using UnityEngine;
namespace DreamRush.Campaign
{
    /// <summary>Readable water/echo pulse: warning first, then one short ground-level burst. Jumping or a status avoids it.</summary>
    public sealed class StoryPulseHazard : MonoBehaviour,IEffectReceiver
    {
        public float period=4f,phaseOffset,radius=4.8f,height=4f;
        public Transform warning,jet;
        PlayerMotor3D _player;float _pausedUntil,_nextDamage;
        public float Phase {get;private set;}
        public DreamProperty Properties=>DreamProperty.Freezeable|DreamProperty.Rootable|DreamProperty.Distractible;
        public Transform Transform=>transform;
        void Start(){_player=FindFirstObjectByType<PlayerMotor3D>();}
        void OnEnable()=>EffectReceiverRegistry.Register(this);
        void OnDisable()=>EffectReceiverRegistry.Unregister(this);
        void Update(){
            if(DreamCinematic.Active||Time.timeScale<=0)return;
            bool paused=Time.time<_pausedUntil;
            Phase=paused?0:Mathf.Repeat(Time.time/Mathf.Max(1,period)+phaseOffset,1);
            if(warning!=null)warning.localScale=new Vector3(radius*2*Mathf.Lerp(.6f,1,Phase),.03f,radius*2*Mathf.Lerp(.6f,1,Phase));
            bool burst=Phase>.8f;
            if(jet!=null){jet.gameObject.SetActive(burst);jet.localScale=new Vector3(1.5f,height*Mathf.Sin(Mathf.InverseLerp(.8f,1,Phase)*Mathf.PI),1.5f);}
            if(!burst||_player==null||Time.time<_nextDamage)return;
            var delta=_player.transform.position-transform.position;
            if(new Vector2(delta.x,delta.z).sqrMagnitude<radius*radius&&Mathf.Abs(delta.y)<2&&_player.IsGrounded){
                Dream.SleepStability.Current?.Damage("คลื่นกำลังผ่าน — สังเกตสัญญาณแล้วกระโดดข้าม");_nextDamage=Time.time+period*.6f;
            }
        }
        public bool CanReceive(EffectContext c)=>(Properties&EffectContext.RequiredProperty(c.Action))!=0;
        public void ApplyEffect(EffectContext c){if(CanReceive(c))_pausedUntil=Time.time+Mathf.Max(3,c.Duration);}
    }
}
