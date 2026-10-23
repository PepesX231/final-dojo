using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Powder fire extinguisher. Hold LMB to spray: knocks down fires in a cone, but leaves white powder
    /// everywhere (evidence! mop it up). Limited charge.
    /// </summary>
    public class Extinguisher : Pickup, IMess
    {
        public float charge = 1f;
        public float duration = 11f;
        public float power = 0.55f;
        public float range = 3.2f;
        public float angle = 30f;
        public Transform nozzle;
        public ParticleSystem spray;
        public Material powderMat;
        public bool used;

        float powderT, toastCd;

        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        public override string GetPrompt(PlayerController p) =>
            "Grab the fire extinguisher" + (used ? " (" + Mathf.RoundToInt(charge * 100f) + "%)" : "");

        public override bool UseHeld(PlayerController p, float dt)
        {
            if (charge <= 0f) { if (Time.time > toastCd) { toastCd = Time.time + 3f; if (GameManager.I) GameManager.I.Toast("*pfft* ...it's empty!"); } return false; }
            used = true;
            MamaAudio.Hiss(transform.position);
            charge = Mathf.Max(0f, charge - dt / duration);
            if (spray) spray.Emit(Mathf.CeilToInt(220f * dt));
            Vector3 o = nozzle ? nozzle.position : p.transform.position + Vector3.up;
            Vector3 f = p.transform.forward;
            foreach (var fire in FireSource.All)
            {
                if (!fire || !fire.burning) continue;
                Vector3 d = fire.transform.position - o; float dist = d.magnitude;
                Vector3 flat = d; flat.y = 0f;
                if (dist > range || Vector3.Angle(f, flat) > angle) continue;
                fire.Extinguish(power * dt * (1.2f - dist / range));
            }
            if (SmokeSim.I) SmokeSim.I.Add(o + f * 1.5f, 0.05f * dt);
            powderT += dt;
            if (powderT > 0.35f)
            {
                powderT = 0f;
                Vector3 at = p.transform.position + f * Random.Range(1.2f, 2.6f) + p.transform.right * Random.Range(-0.6f, 0.6f);
                PowderMess.Spawn(at, powderMat);
            }
            return true;
        }

        public MessKind Kind => MessKind.Evidence;
        public bool IsProblem => used && !IsHeld && !HideZone.IsHidden(transform.position);
        public bool IsHidden => false;
        public int Severity => 2;
        public string MomLine => "Why has the fire extinguisher been USED?!";
        public string Label => "Used extinguisher";
        public Vector3 Position => transform.position;
    }
}
