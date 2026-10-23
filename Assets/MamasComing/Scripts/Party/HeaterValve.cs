using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// One of the two water-heater valves. Both must be cranked within a few seconds of each other.
    public class HeaterValve : Interactable
    {
        public WaterHeaterHazard heater;
        public Transform wheel;
        [System.NonSerialized] public float turnedAt = -99f;
        [System.NonSerialized] public PlayerController lastBy;
        float spin;
        public bool TurnedRecently(float w) => Time.time - turnedAt <= w;
        public override bool CanInteract(PlayerController p) => heater && heater.Urgent && !TurnedRecently(heater.window);
        public override string GetPrompt(PlayerController p) => Loc.Thai ? "กด E ค้าง หมุนวาล์ว (พร้อมกับเพื่อนอีกฝั่ง!)" : "Hold E: turn valve (same time as the OTHER valve!)";
        public override float HoldTime(PlayerController p) => 1.1f;
        public override void HoldTick(PlayerController p, float t) { spin += Time.deltaTime * 360f; if (wheel) wheel.localRotation = Quaternion.Euler(0, 0, spin); }
        public override void Interact(PlayerController p)
        {
            turnedAt = Time.time; lastBy = p;
            MamaAudio.Play("creak", transform.position, 0.8f, 0.8f);
            var other = heater.valveA == this ? heater.valveB : heater.valveA;
            if (other && !other.TurnedRecently(heater.window))
            {
                if (GameManager.I) GameManager.I.KidSay(Loc.Thai ? "อีกวาล์ว! เร็ว!!" : "The OTHER valve! Quick!!");
                StartCoroutine(SpringBack());
            }
        }
        System.Collections.IEnumerator SpringBack()
        {
            yield return new WaitForSeconds(heater.window);
            if (heater && heater.Urgent) { MamaAudio.Play("bad", transform.position, 0.6f); if (GameManager.I) GameManager.I.Toast(Loc.Thai ? "วาล์วดีดกลับ! ต้องหมุนสองตัวพร้อมกัน" : "It sprang back! Both valves must be turned together."); }
        }
    }
}
