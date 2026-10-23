using UnityEngine;
namespace MamasComing
{
    /// <summary>A disposal destination with an animated lid, retaining Cabinet synchronization.</summary>
    public class TrashBin : Cabinet
    {
        public int capacity = 3, stored;
        bool Bag(PlayerController p) => p.Held is MessyProp && p.Held.displayName == "Trash bag";
        public override bool CanInteract(PlayerController p) => true;
        public override string GetPrompt(PlayerController p)
        {
            if(Bag(p) && stored>=capacity)return MamaReadability.Text("Bin full — try another bin", "ถังเต็มแล้ว ลองถังข้าง ๆ");
            if (Bag(p) && stored < capacity)
                return MamaReadability.Text(IsOpen ? "Put the trash bag in the bin" : "Open the bin lid", IsOpen ? "ใส่ถุงขยะลงถัง" : "เปิดฝาถังขยะ");
            return MamaReadability.Text(IsOpen ? "Close the bin lid" : "Open the bin lid", IsOpen ? "ปิดฝาถังขยะ" : "เปิดฝาถังขยะ");
        }
        public override void Interact(PlayerController p)
        {
            if(Bag(p) && stored>=capacity){if(GameManager.I)GameManager.I.ui.Confirm(MamaReadability.Text("This bin is full.","ถังนี้เต็มแล้ว"));return;}
            if (Bag(p) && IsOpen && stored < capacity)
            {
                var bag = p.ReleaseHeld() as MessyProp;
                bag.homePosition = stashPoint.position + Vector3.up * stored * .11f;
                bag.homeRotation = Quaternion.identity; bag.homeSet = true;
                // Base drop skips MessyProp's old-home auto-settle and its duplicate pop.
                bag.OnDropped(null, Vector3.zero);
                bag.Settle(); stored++;
                MamaAudio.Play("bag_drop", transform.position, .7f);
                if (GameManager.I) GameManager.I.ui.Confirm(MamaReadability.Text("Trash bag disposed of.", "ใส่ถุงขยะลงถังแล้ว"));
                return;
            }
            SetOpen(!IsOpen); MamaAudio.Play("bin_lid",transform.position,.55f);
        }
    }
}
