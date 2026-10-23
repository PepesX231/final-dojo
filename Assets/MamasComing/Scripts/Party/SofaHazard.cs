using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- LIVING ROOM: sofa flipped over (heavy: faster with a friend)
    public class SofaHazard : Hazard
    {
        public Transform sofa;
        public Vector3 flippedEuler = new Vector3(0, 0, 80f);
        public Vector3 flippedOffset = new Vector3(0.3f, 0.35f, 0);
        public float soloTime = 6f, duoTime = 1.4f;
        float lift; readonly HashSet<PlayerController> lifters = new HashSet<PlayerController>(); int lastCount;
        Vector3 basePos; Quaternion baseRot;
        protected override void Start() { if (sofa) { basePos = sofa.position; baseRot = sofa.rotation; } base.Start(); }
        public override float HoldTime(PlayerController p) => soloTime;
        public override void HoldTick(PlayerController p, float t) { lifters.Add(p); }
        public override string GetPrompt(PlayerController p) => lastCount >= 2 ? T("LIFT TOGETHER!", "ยกพร้อมกัน!") : T("Hold E: lift the sofa (much faster with a friend)", "กด E ค้าง ยกโซฟา (ช่วยกันยกจะเร็วกว่ามาก)");
        protected override void Update()
        {
            base.Update();
            lastCount = lifters.Count;
            if (Urgent && Sim && lastCount >= 2) { lift += Time.deltaTime / duoTime; if (lift >= 1f) Fix(null); }
            else if (lastCount == 0) lift = Mathf.Max(0f, lift - Time.deltaTime);
            lifters.Clear();
            if (sofa)
            {
                float k = Fixed ? 0f : (Active ? 1f - Mathf.Clamp01(lift) * 0.4f : 0f);
                sofa.SetPositionAndRotation(Vector3.Lerp(sofa.position, basePos + flippedOffset * k, Time.deltaTime * 8f), Quaternion.Slerp(sofa.rotation, baseRot * Quaternion.Euler(flippedEuler * k), Time.deltaTime * 8f));
            }
        }
        protected override void OnFixed(PlayerController p) { MamaAudio.Play("thud", transform.position, 1f, 0.7f); }
    }
}
