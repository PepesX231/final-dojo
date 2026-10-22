using DreamRush.DreamLogic;
using DreamRush.Interaction;
using DreamRush.Player;
using UnityEngine;
namespace DreamRush.Campaign
{
    /// <summary>A physical, manually operated world mechanism. Status effects assist timing, never grant a clear.</summary>
    public sealed class StoryMechanism : Interactable, IEffectReceiver
    {
        public StoryWorldDirector director;
        public int stage,correctTurns=1,turns;
        public float targetBeat=.5f;
        public bool finaleControl,finalRest;
        public string label="PLAY THE NOTE";
        public Transform rotor,ring;
        public bool Completed {get;set;}
        public bool Assisted => Time.time<_assistedUntil;
        float _assistedUntil,_cooldown;
        Vector3 _ringScale;
        public DreamProperty Properties=>DreamProperty.Freezeable|DreamProperty.Rootable|DreamProperty.Pullable|DreamProperty.SocialResponsive|DreamProperty.Distractible;
        public Transform Transform=>transform;
        public override string Prompt=>Completed?"ทำนองนี้ครบแล้ว":finalRest?"พัก / เข้าฝั่ง":label;
        public override bool CanInteract(InteractionContext context)=>base.CanInteract(context)&&!Completed&&director!=null&&!director.Won&&!director.Busy;
        protected override void OnInteract(InteractionContext context){if(CanInteract(context))director.Use(this,context.Motor);}
        protected override void OnEnable(){base.OnEnable();EffectReceiverRegistry.Register(this);}
        protected override void OnDisable(){base.OnDisable();EffectReceiverRegistry.Unregister(this);}
        void Start(){if(director==null)director=StoryWorldDirector.Current;if(ring!=null)_ringScale=ring.localScale;SetRotation();}
        void Update(){
            if(director==null)return;
            if(ring!=null){float pulse=Completed?1f:Mathf.Lerp(.7f,1.45f,Mathf.Abs(Mathf.DeltaAngle(director.Beat*360,targetBeat*360))/180f);ring.localScale=new Vector3(_ringScale.x*pulse,_ringScale.y,_ringScale.z*pulse);}
            if(rotor!=null&&director.world!=StoryWorldKind.BubbleBathSea&&!Assisted&&!Completed)rotor.localRotation=Quaternion.Euler(0,Mathf.Sin(director.Clock*2f+stage)*35,0);
        }
        public void SetRotation(){if(rotor!=null&&director!=null&&director.world==StoryWorldKind.BubbleBathSea)rotor.localRotation=Quaternion.Euler(0,turns*90,0);}
        public bool TryCooldown(float seconds){if(Time.time<_cooldown)return false;_cooldown=Time.time+seconds;return true;}
        public bool CanReceive(EffectContext c)=>!Completed&&(Properties&EffectContext.RequiredProperty(c.Action))!=0;
        public void ApplyEffect(EffectContext c){
            if(!CanReceive(c))return;
            _assistedUntil=Time.time+Mathf.Max(2.5f,c.Duration);
            if(c.Action==EffectAction.Pull&&director!=null&&director.world==StoryWorldKind.BubbleBathSea){turns=(correctTurns+3)%4;SetRotation();}
            Presentation.VFX.Play(Presentation.VFXKind.PaperBurst,transform.position+Vector3.up,1.2f);
            UI.UIRoot.Get().Toast("กลไกชะลอแล้ว — ใช้จังหวะนี้ด้วยตัวเอง",2);
        }
    }
}
