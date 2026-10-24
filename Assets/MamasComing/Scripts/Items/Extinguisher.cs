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
            RenderSpray(p, dt);
            if (!NetGame.IsAuthority) return true;
            used = true;
            MamaAudio.Hiss(transform.position);
            charge = Mathf.Max(0f, charge - dt / duration);

            Vector3 o = nozzle ? nozzle.position : p.transform.position + Vector3.up;
            Vector3 f = Aim(p, o);
            foreach (var fire in FireSource.All)
            {
                if (!fire || !fire.burning) continue;
                Vector3 d = fire.transform.position - o; float dist = d.magnitude;
                if (dist > range || Vector3.Angle(f, d) > angle) continue;
                fire.Extinguish(power * dt * (1.2f - dist / range));
                if (!fire.burning) MamaRound.Award(p, fire, "Fire extinguished: " + fire.label, 150);
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

        Vector3 Aim(PlayerController p, Vector3 origin)
        {
            FireSource best=null; float distance=range;
            foreach(var fire in FireSource.All)
            {
                if(!fire || !fire.burning)continue;
                Vector3 delta=fire.transform.position-origin, flat=delta;flat.y=0;
                if(delta.magnitude<distance && Vector3.Angle(p.transform.forward,flat)<=angle){best=fire;distance=delta.magnitude;}
            }
            return best?(best.transform.position-origin).normalized:p.transform.forward;
        }
        public void RenderSpray(PlayerController p,float dt)
        {
            if(!spray || charge<=0)return;
            Vector3 origin=nozzle?nozzle.position:p.transform.position+Vector3.up;
            Vector3 direction=Aim(p,origin);
            spray.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(direction));
            var main=spray.main;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=6f;main.startLifetime=range/6f;
            var shape=spray.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.025f;shape.rotation=Vector3.zero;
            spray.Emit(Mathf.CeilToInt(160*dt));
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
