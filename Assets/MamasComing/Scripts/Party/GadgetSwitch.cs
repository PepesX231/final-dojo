using UnityEngine;

namespace MamasComing
{
    /// One of several buttons/levers a GadgetHazard needs pressed at (nearly) the same time.
    public class GadgetSwitch : Interactable
    {
        public GadgetHazard owner;
        public Transform knob;
        float pressedAt = -99f;
        public bool PressedRecently(float window) => Time.time - pressedAt <= window;
        public override bool CanInteract(PlayerController p) => owner && owner.Urgent && !PressedRecently(owner.partnerWindow);
        public override string GetPrompt(PlayerController p) => Loc.Thai ? "กด E ค้าง (พร้อมกับเพื่อนอีกฝั่ง!)" : "Hold E (at the same time as the OTHER one!)";
        public override float HoldTime(PlayerController p) => 0.8f;
        public override void HoldTick(PlayerController p, float t) { if (knob) knob.localPosition = new Vector3(0f, -0.04f * t, 0f); }
        public override void Interact(PlayerController p)
        {
            pressedAt = Time.time;
            MamaAudio.Play("click", transform.position, 0.8f, 1.1f);
            if (owner) owner.PartnerPressed(this, p);
        }
    }
}
