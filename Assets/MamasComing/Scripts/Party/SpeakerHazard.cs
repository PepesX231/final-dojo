using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- GAME ROOM: party speakers blasting
    public class SpeakerHazard : Hazard
    {
        public Transform[] cones;
        public Light[] disco;
        public Transform discoBall;
        public AquariumHazard tank;
        public float bassToTank = 45f;
        bool shook;
        protected override void Tick(float dt)
        {
            if (!shook && tank && ActiveTime >= bassToTank / Mathf.Max(0.5f, QuestDirector.I ? QuestDirector.I.Pace : 1f))
            {
                shook = true;
                tank.Activate("BOOM-BOOM-BOOM... the bass shook the FISH TANK off its stand!", "ตึ้ม ตึ้ม ตึ้ม... เสียงเบสสั่นจนตู้ปลาร่วงจากขาตั้ง!");
            }
        }
        protected override void VisualTick(float dt, bool running)
        {
            float beat = running ? Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI * 2f)), 8f) : 0f;
            if (cones != null) foreach (var c in cones) if (c) c.localScale = Vector3.one * (1f + beat * 0.18f);
            if (disco != null) for (int i = 0; i < disco.Length; i++) if (disco[i])
                    { disco[i].enabled = running; disco[i].color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.3f + i * 0.33f, 1f), 0.8f, 1f); }
            if (discoBall && running) discoBall.Rotate(0f, 90f * dt, 0f, Space.World);
        }
        protected override void OnFailed()
        {
            if (!NetGame.IsAuthority) return;
            PowerGrid.Trip("The speakers overloaded the circuit!", "ลำโพงดึงไฟเกินจนเบรกเกอร์ตัด!");
            Failed = false; Progress = 0.4f;   // music comes back when power does
        }
        protected override void OnFixed(PlayerController p) { if (QuestDirector.I) QuestDirector.I.Neighbor -= 15f; }
    }
}
