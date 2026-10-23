using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GAME ROOM: cracked fish tank + fish out of water
    public class AquariumHazard : Hazard
    {
        public Transform tankBody;
        public Vector3 tiltEuler = new Vector3(0f, 0f, 9f);
        public Transform leakPoint;
        public float leakRate = 0.45f;
        public Fish[] fish;
        public ParticleSystem drip;
        Quaternion baseRot;
        public bool Patched => Fixed;
        bool FishInHand(PlayerController p) => Patched && p && p.Held is Fish f && !f.Dead;
        public override bool CanInteract(PlayerController p) => base.CanInteract(p) || FishInHand(p);
        public override float Priority(PlayerController p) => FishInHand(p) ? 2.5f : base.Priority(p) + (p && p.Held is Fish ? 1f : 0f);
        public override string GetPrompt(PlayerController p) => FishInHand(p) ? (Loc.Thai ? "ปล่อยปลากลับลงตู้" : "Put the fish back in the tank") : base.GetPrompt(p);
        public override MiniGame.Kind MiniGameKind(PlayerController p) => FishInHand(p) ? MiniGame.Kind.None : base.MiniGameKind(p);
        public override void Interact(PlayerController p) { if (FishInHand(p)) { p.Drop(false); return; } base.Interact(p); }

        protected override void Start() { if (tankBody) baseRot = tankBody.localRotation; base.Start(); }
        protected override void OnActivated()
        {
            if (fish != null && NetGame.IsAuthority) foreach (var f in fish) if (f) f.JumpOut();
            MamaAudio.Play("slam", transform.position, 0.9f, 0.7f); MamaAudio.Play("splash", transform.position, 1f, 0.8f);
        }
        protected override void Tick(float dt)
        {
            if (FloodSim.I && leakPoint) FloodSim.I.AddLiters(leakPoint.position, leakRate * (Failed ? 2.5f : 1f) * dt, 0.3f);
        }
        protected override void VisualTick(float dt, bool running)
        {
            if (tankBody) tankBody.localRotation = Quaternion.Slerp(tankBody.localRotation, Active && !Fixed ? baseRot * Quaternion.Euler(tiltEuler) : baseRot, dt * 6f);
            if (drip) { var em = drip.emission; em.rateOverTime = running ? 30f : 0f; }
        }
    }
}
