using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// A party-aftermath gadget gone wrong. One configurable hazard so the house can hold many different problems:
    /// each instance combines a requirement (a tool, the power being OFF, two friends pressing switches together, a
    /// moving target to corner), a minigame or hold, what it does while ignored (water, smoke, noise, mess) and what
    /// happens when it escalates (power trip, fire, Mom comes home earlier, another problem starts...).
    /// </summary>
    public class GadgetHazard : Hazard
    {
        public enum Need { None, Lid, Towel, Bucket, Chair, Plunger, Wrench, Extinguisher, Mop }
        public enum Consequence { None, TripPower, Fire, CutTime, Neighbour, Water, Splats, Chain }

        [Header("Requirement")]
        public Need need;
        public string needEn, needTh;
        [Tooltip("Can only be fixed while the house power is OFF (someone has to trip the breaker panel).")]
        public bool fixOnlyWithoutPower;
        [Tooltip("Co-op: every switch has to be pressed within partnerWindow seconds of each other.")]
        public GadgetSwitch[] partners;
        public float partnerWindow = 4f;
        public float holdSolo;

        [Header("Moving target (a runaway gadget)")]
        public Transform roamer;
        public Vector3[] roamPath;
        public float roamSpeed = 1.4f, fleeSpeed = 3.2f, fleeRadius = 3f;

        [Header("While it is going")]
        public Transform spinner; public Vector3 spinAxis = Vector3.up; public float spinSpeed = 360f;
        public Transform wobbler; public float wobble = 6f;
        public ParticleSystem fx; public float fxRate = 30f;
        public Light blink;
        public float waterPerSec; public Transform waterPoint;
        public float smokePerSec;
        public float neighbourPerSec;
        public SplatKind splatKind = SplatKind.Confetti; public Material splatMat; public float splatEvery; public float splatRadius = 2f;

        [Header("When it escalates")]
        public Consequence onFail;
        public FireSource failFire;
        public Hazard chainTo;
        public float cutSeconds = 30f, failWater = 40f;

        int pathIdx; float splatT; Quaternion wobbleBase; bool haveBase;

        public override Vector3 InteractPoint => roamer ? roamer.position : base.InteractPoint;
        bool HasPartners => partners != null && partners.Length > 0;

        bool Holds(PlayerController p)
        {
            if (need == Need.None) return true;
            if (!p || p.Held == null) return false;
            if (p.Held is ToolItem ti) return (need == Need.Plunger && ti.kind == ToolItem.Kind.Plunger) || (need == Need.Wrench && ti.kind == ToolItem.Kind.Wrench);
            var t = p.Held.tool;
            switch (need)
            {
                case Need.Lid: return t == ToolType.Lid;
                case Need.Towel: return t == ToolType.Towel;
                case Need.Bucket: return t == ToolType.Bucket;
                case Need.Chair: return t == ToolType.Chair;
                case Need.Extinguisher: return t == ToolType.Extinguisher;
                case Need.Mop: return t == ToolType.Mop;
            }
            return false;
        }
        bool Ready(PlayerController p) => Holds(p) && (!fixOnlyWithoutPower || !PowerGrid.On);

        public override bool CanInteract(PlayerController p) => Urgent && !HasPartners;
        public override string GetPrompt(PlayerController p)
        {
            if (!Holds(p)) return T(needEn ?? "Need a tool", needTh ?? "ต้องใช้เครื่องมือ");
            if (fixOnlyWithoutPower && PowerGrid.On) return T("Cut the power first! (breaker panel, garage)", "ต้องตัดไฟก่อน! (ตู้เบรกเกอร์ โรงรถ)");
            return Quest;
        }
        public override MiniGame.Kind MiniGameKind(PlayerController p) => Ready(p) ? puzzle : MiniGame.Kind.None;
        public override float HoldTime(PlayerController p) => Ready(p) ? holdSolo : 0f;
        public override void Interact(PlayerController p)
        {
            if (!Holds(p)) { if (GameManager.I) GameManager.I.KidSay(T(needEn ?? "I need something for this...", needTh ?? "ต้องใช้อะไรสักอย่าง...")); return; }
            if (fixOnlyWithoutPower && PowerGrid.On) { if (GameManager.I) GameManager.I.KidSay(T("It'll zap me! Someone cut the power!", "เดี๋ยวโดนไฟดูด! ใครก็ได้ตัดไฟที!")); return; }
            Fix(p);
        }

        /// Called by a partner switch on the authority.
        public void PartnerPressed(GadgetSwitch s, PlayerController p)
        {
            if (!Urgent) return;
            foreach (var o in partners) if (o && !o.PressedRecently(partnerWindow)) { if (GameManager.I) GameManager.I.KidSay(T("Now the OTHER one! Together!", "อีกอันด้วย! พร้อมกัน!")); return; }
            Fix(p);
        }

        protected override void Tick(float dt)
        {
            Vector3 at = waterPoint ? waterPoint.position : InteractPoint;
            if (waterPerSec > 0f && FloodSim.I && HouseLayout.FloorOf(at) == 0) FloodSim.I.AddLiters(at, waterPerSec * (Failed ? 2f : 1f) * dt, 0.5f);
            if (smokePerSec > 0f && SmokeSim.I) SmokeSim.I.Add(at, smokePerSec * dt);
            if (neighbourPerSec > 0f && QuestDirector.I) QuestDirector.I.Neighbor = Mathf.Min(100f, QuestDirector.I.Neighbor + neighbourPerSec * dt);
            if (splatEvery > 0f && (splatT -= dt) <= 0f)
            {
                splatT = splatEvery * Random.Range(0.7f, 1.3f);
                if (SplatMess.AllSplats.Count < 140)
                {
                    Vector2 r = Random.insideUnitCircle * splatRadius;
                    SplatMess.Spawn(splatKind, at + new Vector3(r.x, 0f, r.y), Random.Range(0.35f, 0.7f), splatMat ? splatMat : SplatMess.MatFor(splatKind));
                }
            }
            if (roamer && roamPath != null && roamPath.Length > 1) Roam(dt);
        }

        void Roam(float dt)
        {
            PlayerController near = null; float nd = fleeRadius;
            foreach (var pc in PlayerController.AllPlayers) { if (!pc) continue; float d = Vector3.Distance(pc.transform.position, roamer.position); if (d < nd) { nd = d; near = pc; } }
            Vector3 to = roamPath[pathIdx] - roamer.position; to.y = 0f;
            if (near && Vector3.Dot(to, near.transform.position - roamer.position) > 0f) { pathIdx = (pathIdx + roamPath.Length / 2) % roamPath.Length; to = roamPath[pathIdx] - roamer.position; to.y = 0f; }
            if (to.magnitude < 0.2f) { pathIdx = (pathIdx + 1) % roamPath.Length; return; }
            float sp = near ? fleeSpeed : roamSpeed;
            Vector3 step = to.normalized * Mathf.Min(to.magnitude, sp * dt);
            roamer.position += step;
            roamer.rotation = Quaternion.Slerp(roamer.rotation, Quaternion.LookRotation(to.normalized), dt * 8f);
        }

        protected override void VisualTick(float dt, bool running)
        {
            if (spinner && running) spinner.Rotate(spinAxis, spinSpeed * dt, Space.Self);
            if (wobbler)
            {
                if (!haveBase) { wobbleBase = wobbler.localRotation; haveBase = true; }
                float a = running ? Mathf.Sin(Time.time * 17f) * wobble : 0f;
                wobbler.localRotation = wobbleBase * Quaternion.Euler(a * 0.5f, 0f, a);
            }
            if (fx) { var em = fx.emission; em.rateOverTime = running ? fxRate * (Failed ? 1.8f : 1f) : 0f; }
            if (blink) blink.enabled = running && Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
        }

        protected override void OnFailed()
        {
            if (!NetGame.IsAuthority) return;
            switch (onFail)
            {
                case Consequence.TripPower: PowerGrid.Trip(failEn ?? "*BZZT* The power tripped!", failTh ?? "*ซี่ดด* ไฟตก!"); break;
                case Consequence.Fire: if (failFire) failFire.Ignite(0.35f); break;
                case Consequence.CutTime: if (GameManager.I) GameManager.I.CutTime(cutSeconds); break;
                case Consequence.Neighbour: if (QuestDirector.I) QuestDirector.I.Neighbor = Mathf.Min(100f, QuestDirector.I.Neighbor + 45f); break;
                case Consequence.Water: if (FloodSim.I) FloodSim.I.AddLiters(waterPoint ? waterPoint.position : InteractPoint, failWater, 1.2f); break;
                case Consequence.Splats:
                    for (int i = 0; i < 8; i++) { Vector2 r = Random.insideUnitCircle * splatRadius * 1.6f; SplatMess.Spawn(splatKind, InteractPoint + new Vector3(r.x, 0f, r.y), Random.Range(0.5f, 0.9f), splatMat ? splatMat : SplatMess.MatFor(splatKind)); }
                    break;
                case Consequence.Chain: if (chainTo) chainTo.Activate(); break;
            }
        }

        protected override void OnFixed(PlayerController p)
        {
            if (fx) { var em = fx.emission; em.rateOverTime = 0f; }
            if (blink) blink.enabled = false;
            MamaAudio.Play("click", InteractPoint, 0.8f, 0.9f);
        }

        public override void WriteState(System.IO.BinaryWriter w)
        {
            base.WriteState(w);
            Vector3 r = roamer ? roamer.position : Vector3.zero; w.Write(r.x); w.Write(r.y); w.Write(r.z);
        }
        public override void ReadState(System.IO.BinaryReader r)
        {
            base.ReadState(r);
            var v = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            if (roamer) roamer.position = v;
        }
    }
}
