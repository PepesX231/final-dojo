using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GARAGE: breaker panel (power outage)
    public class BreakerPanel : Hazard
    {
        public Transform lever;
        public Light panelLight;
        public Transform laundrySocket;
        public float socketWetDepth = 0.0035f;
        float offFor, wetCheck;
        bool spoilWarned;
        public override bool CanInteract(PlayerController p) => true;
        public override string GetPrompt(PlayerController p) => PowerGrid.On ? T("Switch the house power OFF (stops music, washer, oven, iron...)", "ตัดไฟทั้งบ้าน (เพลง เครื่องซักผ้า เตาอบ เตารีดจะหยุด)") : Quest;
        public override MiniGame.Kind MiniGameKind(PlayerController p) => PowerGrid.On ? MiniGame.Kind.None : MiniGame.Kind.Wires;
        public override float HoldTime(PlayerController p) => PowerGrid.On ? 0.8f : 0f;
        public override void Interact(PlayerController p)
        {
            if (!NetGame.IsAuthority) return;
            if (PowerGrid.On) { PowerGrid.Trip("You switched the power off.", "ตัดไฟเองแล้ว"); return; }
            if (SocketWet()) { MamaAudio.Play("zap", transform.position, 1f); if (p) p.KnockBack(transform.position, 3f); if (QuestDirector.I) QuestDirector.I.Alarm(this, "*ZAP!* It tripped again - the laundry socket is still under water!", "*แปล๊บ!* ไฟตัดอีก - ปลั๊กห้องซักผ้ายังจมน้ำอยู่!"); return; }
            Fix(p);
        }
        bool SocketWet() => FloodSim.I && laundrySocket && FloodSim.I.DepthAround(laundrySocket.position, 0.5f) > socketWetDepth;
        protected override void OnFixed(PlayerController p) { PowerGrid.Restore(); offFor = 0f; }

        protected override void Update()
        {
            if (Sim)
            {
                if (PowerGrid.On && Fixed) Rearm();
                if (!PowerGrid.On && !Active) { Active = true; Progress = 0f; RefreshVisuals(); if (QuestDirector.I) QuestDirector.I.Alarm(this, T(PowerGrid.CauseEn ?? "POWER OUT!", PowerGrid.CauseTh ?? "ไฟดับ!") + T("  Restore it at the breaker panel (garage).", "  ไปแก้ที่ตู้ไฟ (โรงรถ)"), null); }
                if (PowerGrid.On && Active && !Fixed) { Rearm(); }
                wetCheck -= Time.deltaTime;
                if (PowerGrid.On && wetCheck <= 0f) { wetCheck = 0.5f; if (SocketWet()) PowerGrid.Trip("Water reached the laundry socket! *BZZT*", "น้ำท่วมถึงปลั๊กห้องซักผ้า! *ซี่ดด*"); }
                if (!PowerGrid.On) { offFor += Time.deltaTime; if (offFor > 90f && !spoilWarned) { spoilWarned = true; var fr = FindFirstObjectByType<FridgeHazard>(); if (fr) fr.Spoil(); } }
            }
            if (lever) lever.localRotation = Quaternion.Slerp(lever.localRotation, Quaternion.Euler(PowerGrid.On ? -35f : 35f, 0, 0), Time.deltaTime * 10f);
            if (panelLight) panelLight.color = PowerGrid.On ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.2f, 0.15f);
            base.Update();
        }
        protected override float ProgressMul => 0.5f;
        public override bool IsProblem => !PowerGrid.On;
        public override string MomLine => T("Why is the POWER off?! All my clocks are blinking!", "ทำไมไฟดับ?! นาฬิกาทั้งบ้านกะพริบหมดเลย!");
    }
}
