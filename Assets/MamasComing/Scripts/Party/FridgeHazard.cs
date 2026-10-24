using UnityEngine;
namespace MamasComing
{
    public class FridgeHazard : Hazard
    {
        public Transform door; public ParticleSystem stink; public float openAngle=-75f;
        public RottenFood rottenFood;
        public bool Spoiled => spoiled;
        public bool DoorOpen => doorOpen;
        bool spoiled, doorOpen; Quaternion closedRotation;
        protected override void Start() { if(door)closedRotation=door.localRotation; base.Start(); }
        protected override void OnActivated(){doorOpen=true;}
        public void Spoil()
        {
            if(spoiled || rottenFood && rottenFood.Disposed)return;
            spoiled=true;Failed=true;Fixed=false;Active=true; RefreshVisuals();
            if(rottenFood)rottenFood.gameObject.SetActive(true);
            if(QuestDirector.I)QuestDirector.I.Alarm(this,"Food spoiled! Open the fridge and take it to a bin.","อาหารเน่าแล้ว! เปิดตู้เย็นแล้วเอาไปทิ้งถังขยะ");
        }
        protected override void OnFailed(){spoiled=true;if(rottenFood)rottenFood.gameObject.SetActive(true);}
        public override bool CanInteract(PlayerController p)=>p && !p.Held && (IsProblem || doorOpen);
        public override MiniGame.Kind MiniGameKind(PlayerController p)=>MiniGame.Kind.None;
        public override string GetPrompt(PlayerController p)
        {
            if(spoiled && !doorOpen)return T("Open the fridge to remove spoiled food", "เปิดตู้เย็นเพื่อเอาอาหารเน่าออก");
            if(spoiled && rottenFood && !rottenFood.IsHeld && !rottenFood.Removed)return T("Take the rotten food to a trash bin", "หยิบอาหารเน่าไปทิ้งถังขยะ");
            return T(doorOpen?"Close the fridge":"Open the fridge",doorOpen?"ปิดตู้เย็น":"เปิดตู้เย็น");
        }
        public override void Interact(PlayerController p)
        {
            if(!p || p.Held)return;
            if(spoiled)
            {
                if(!doorOpen){doorOpen=true;MamaAudio.Play("door",transform.position,.5f);return;}
                if(rottenFood && !rottenFood.IsHeld && !rottenFood.Removed){p.PickUp(rottenFood);return;}
                doorOpen=!doorOpen; return;
            }
            doorOpen=false; Fix(p);
        }
        public void FoodDisposed(PlayerController p)
        {
            if(!spoiled)return; spoiled=false;Failed=false;doorOpen=false;Fixed=false; Fix(p);
        }
        protected override void VisualTick(float dt,bool running)
        {
            if(door)door.localRotation=Quaternion.Slerp(door.localRotation,closedRotation*Quaternion.Euler(0,doorOpen?openAngle:0,0),dt*8);
            if(stink){var em=stink.emission;em.rateOverTime=spoiled?8:0;}
            if(spoiled){questEn="Put the rotten food in a trash bin";questTh="นำอาหารเน่าไปทิ้งถังขยะ";}
        }
        public override bool IsProblem=>spoiled || Active&&!Fixed;
        public override void WriteState(System.IO.BinaryWriter w){base.WriteState(w);w.Write(spoiled);w.Write(doorOpen);}
        public override void ReadState(System.IO.BinaryReader r){base.ReadState(r);spoiled=r.ReadBoolean();doorOpen=r.ReadBoolean();}
    }
}
