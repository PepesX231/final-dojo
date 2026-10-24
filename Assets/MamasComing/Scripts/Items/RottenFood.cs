using UnityEngine;
namespace MamasComing
{
    public class RottenFood : MessyProp
    {
        public FridgeHazard fridge;
        public bool Disposed { get; private set; }
        public bool Removed { get; private set; }
        public override bool CanSettle=>false;
        public override bool IsProblem=>!Disposed && fridge && fridge.Spoiled;
        public override bool CanInteract(PlayerController p)=>!Disposed && fridge && fridge.Spoiled && (Removed || fridge.DoorOpen) && base.CanInteract(p);
        public override string GetPrompt(PlayerController p)=>MamaReadability.Text("Pick up the rotten food • take it to a bin","หยิบอาหารเน่า • นำไปทิ้งถังขยะ");
        public override void OnPickedUp(PlayerController p){Removed=true;base.OnPickedUp(p);}
        public void DisposeFood(PlayerController p){Disposed=true;if(fridge)fridge.FoodDisposed(p);}
    }
}
