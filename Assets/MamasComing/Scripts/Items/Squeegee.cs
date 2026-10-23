using UnityEngine;

namespace MamasComing
{
    /// <summary>Floor squeegee: hold LMB and walk to sweep a wave of water ahead of you (toward a drain!).</summary>
    public class Squeegee : Pickup
    {
        public Transform blade;
        public float pushRate = 3.2f;
        float toastCd;

        public override bool UseHeld(PlayerController p, float dt)
        {
            if (!FloodSim.I) return false;
            p.Scrubbing = true;
            Vector3 bp = blade ? blade.position : p.transform.position + p.transform.forward * 0.8f;
            float moved = FloodSim.I.Push(bp, p.transform.forward, 0.5f, pushRate * dt, 0.55f);
            if (moved > 0.02f && Time.time > toastCd)
            {
                toastCd = Time.time + 8f;
                if (GameManager.I) GameManager.I.Toast("Swoosh! Herd the water toward the bathroom drain.");
            }
            return true;
        }
    }
}
