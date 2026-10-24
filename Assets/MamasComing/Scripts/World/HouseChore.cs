using UnityEngine;

namespace MamasComing
{
    /// Prototype task mechanics: physical delivery, staged repairs, timing and a timed relay.
    /// Kept out of the main game until the two-storey simulations/networking have been integrated.
    public class HouseChore : Interactable
    {
        public enum Mechanic { Delivery, Sequence, Timing, Relay }
        public Mechanic mechanic;
        public string instruction;
        public Pickup requiredItem;
        public Transform deliveryPoint;
        public HouseChore prerequisite;
        public HouseChore gate;
        public Transform movingPart;
        public Renderer lamp;
        public float workSeconds = 1.5f;
        public float relaySeconds = 22f;
        public bool Complete { get; private set; }
        public int Step { get; private set; }
        public float LatchedUntil { get; private set; }
        public bool Open => Complete && mechanic != Mechanic.Relay || Time.time < LatchedUntil;
        public bool Ready => (!prerequisite || prerequisite.Complete) && (!gate || gate.Open);
        public float Needle => Mathf.PingPong(Time.time * .7f, 1f);
        public bool InTimingWindow => Needle >= .35f && Needle <= .7f;
        int lastHitBeat = -1;
        public bool CanHit => InTimingWindow && Mathf.FloorToInt(Time.time * .7f) != lastHitBeat;
        MaterialPropertyBlock block;

        public override bool CanInteract(PlayerController p) => !Complete || mechanic == Mechanic.Relay;
        public override float Priority(PlayerController p) => 0.7f;
        public override float HoldTime(PlayerController p) => Ready && mechanic == Mechanic.Sequence ? workSeconds : 0f;
        public override string GetPrompt(PlayerController p)
        {
            if (prerequisite && !prerequisite.Complete) return "First: " + prerequisite.displayName;
            if (gate && !gate.Open) return "Latch " + gate.displayName + " first ("+gate.relaySeconds+" seconds)";
            if (mechanic == Mechanic.Delivery) return "Bring " + (requiredItem ? requiredItem.displayName : "item") + " • " + instruction;
            if (mechanic == Mechanic.Sequence) return "Hold E • " + instruction + " • step "+(Step+1)+"/3";
            if (mechanic == Mechanic.Timing) return "Tap E when GREEN • " + Mathf.RoundToInt(Needle * 100f) + "%";
            return "Latch for " + relaySeconds + "s • " + instruction;
        }

        public override void Interact(PlayerController p)
        {
            if (!Ready || (Complete && mechanic != Mechanic.Relay)) return;
            var lab = FindFirstObjectByType<HousePlayground>();
            switch (mechanic)
            {
                case Mechanic.Delivery:
                    if (!requiredItem || p.Held != requiredItem) { if (lab) lab.Say("Carry the matching object here, then press E."); return; }
                    p.ReleaseHeld(); requiredItem.OnDropped(p, Vector3.zero);
                    requiredItem.Body.isKinematic = true;
                    requiredItem.transform.position = deliveryPoint ? deliveryPoint.position : transform.position + Vector3.up * .015f;
                    requiredItem.transform.SetParent(deliveryPoint ? deliveryPoint.parent : transform.parent,true);
                    requiredItem.enabled = false;
                    Complete = true; break;
                case Mechanic.Sequence:
                    Step++; Complete = Step >= 3;
                    if (movingPart) movingPart.localRotation *= Quaternion.Euler(0, 120, 0);
                    if (lab && !Complete) lab.Say("Repair " + Step + "/3 • " + instruction);
                    break;
                case Mechanic.Timing:
                    if (!InTimingWindow) { Step = 0; if (lab) lab.Say("Missed! Watch the green indicator and try again."); return; }
                    if (!CanHit) return; // one hit per sweep, not three button presses in the same green interval
                    lastHitBeat = Mathf.FloorToInt(Time.time * .7f);
                    Step++; Complete = Step >= 3;
                    if (lab && !Complete) lab.Say("Good! " + Step + "/3 hits");
                    break;
                case Mechanic.Relay:
                    LatchedUntil = Time.time + relaySeconds; Complete = true;
                    if (lab) lab.Say("Latched! Run to the other station, or let a friend do it.");
                    break;
            }
            if (Complete && lab) lab.Say("Done: " + displayName);
        }

        void Update()
        {
            if (!lamp) return;
            if (block == null) block = new MaterialPropertyBlock();
            if (movingPart && !Complete) movingPart.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 5f) * 12f, Time.time * 30f, 0);
            Color color = Complete ? new Color(.2f, .9f, .5f) : !Ready ? new Color(.4f, .45f, .5f) : mechanic == Mechanic.Timing && InTimingWindow ? Color.green : new Color(1f, .48f, .18f);
            lamp.GetPropertyBlock(block); block.SetColor("_BaseColor", color); lamp.SetPropertyBlock(block);
        }
    }
}
