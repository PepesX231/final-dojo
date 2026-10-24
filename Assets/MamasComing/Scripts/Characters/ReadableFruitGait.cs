using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Explicit world-aligned strides for both the bone rig and the supplied object-animated fruit.
    /// Runs after imported animation but before carrying and whole-body tumble layers.
    [DefaultExecutionOrder(450)]
    public class ReadableFruitGait : MonoBehaviour
    {
        struct Part { public Transform t; public Vector3 p; public Quaternion r; public int side; public int role; }
        readonly List<Part> parts = new List<Part>();
        PlayerController player;
        Vector3 last;
        float phase, weight;
        public float StepPhase => phase;
        void Start()
        {
            player=GetComponentInParent<PlayerController>();last=player?player.transform.position:transform.position;
            foreach(var t in GetComponentsInChildren<Transform>(true))
            {
                string n=t.name;
                int side=n.Contains("_L")||n.EndsWith(" L")?-1:n.Contains("_R")||n.EndsWith(" R")?1:0;
                int role=n=="Foot_L"||n=="Foot_R"||n.Contains("duck foot")?1:n=="Leg_L"||n=="Leg_R"?2:n=="Leg_L_Lower"||n=="Leg_R_Lower"?3:n=="Arm_L"||n=="Arm_R"||n.Contains("swinging glove")?4:n=="Arm_L_Lower"||n=="Arm_R_Lower"?5:0;
                if(role>0 && side!=0)parts.Add(new Part{t=t,p=t.localPosition,r=t.localRotation,side=side,role=role});
            }
        }
        void LateUpdate()
        {
            if(!player || Time.deltaTime<=0)return;
            Vector3 delta=player.transform.position-last;delta.y=0;last=player.transform.position;
            bool canWalk=!player.Stunned && player.Motor && player.Motor.IsGrounded;
            float speed=delta.magnitude/Time.deltaTime;
            weight=Mathf.MoveTowards(weight,canWalk?Mathf.Clamp01(speed/1.1f):0,Time.deltaTime*9);
            // Preserve the gait phase when stopping/reversing; a frame-rate independent stride.
            if(delta.magnitude<1f)phase+=delta.magnitude/.95f*Mathf.PI*2;
            foreach(var p in parts)
            {
                if(!p.t||!p.t.parent)continue;
                float a=phase+(p.side>0?Mathf.PI:0);
                float forward=Mathf.Cos(a)*.19f*weight;
                float lift=Mathf.Max(0,Mathf.Sin(a))*.13f*weight;
                Vector3 shift=player.transform.forward*forward+Vector3.up*lift;
                Vector3 axis=p.t.parent.InverseTransformDirection(player.transform.right);
                float swing=Mathf.Sin(a)*32*weight;
                if(p.role<=3)
                {
                    float k=p.role==1?1:p.role==2?.25f:.65f;
                    p.t.localPosition=p.p+p.t.parent.InverseTransformVector(shift*k);
                    p.t.localRotation=Quaternion.AngleAxis(p.role==1?-Mathf.Max(0,Mathf.Sin(a)) * 16*weight:swing,axis)*p.r;
                }
                else if(!player.Held && player.HoldProgress<=0 &&
                    (!player.actions || (!player.actions.Busy && player.actions.Current == KidActionAnimator.Clip.None)))
                {
                    p.t.localPosition=p.p+p.t.parent.InverseTransformVector(-shift*.45f);
                    p.t.localRotation=Quaternion.AngleAxis(-swing*.75f,axis)*p.r;
                }
            }
        }
    }
}
