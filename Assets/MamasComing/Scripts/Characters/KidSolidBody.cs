using UnityEngine;
namespace MamasComing
{
    /// Owner-side swept circle contact. Remote puppets keep their motors disabled; touching them never reads input or triggers a fall.
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(PlayerController))]
    public class KidSolidBody : MonoBehaviour
    {
        PlayerController player; CharacterController capsule; Vector3 previous; bool initialized;
        const float Separation=.84f;
        void Awake(){player=GetComponent<PlayerController>();capsule=GetComponent<CharacterController>();}
        public static Vector3 Block(Vector3 start, Vector3 end, Vector3 other, float radius)
        {
            Vector3 d=end-start;d.y=0;Vector3 a=start-other;a.y=0;
            float len=d.magnitude;
            if(a.sqrMagnitude>=radius*radius && len>.0001f)
            {
                Vector3 dir=d/len; float b=Vector3.Dot(a,dir),c=a.sqrMagnitude-radius*radius,disc=b*b-c;
                if(b<0 && disc>=0){float hit=-b-Mathf.Sqrt(disc);if(hit>=0 && hit<len){Vector3 contact=start+dir*hit;Vector3 n=contact-other;n.y=0;n.Normalize();Vector3 rest=d-dir*hit;rest-=n*Mathf.Min(0,Vector3.Dot(rest,n));end=contact+rest;}}
            }
            Vector3 flat=end-other;flat.y=0;
            if(flat.sqrMagnitude<radius*radius){if(flat.sqrMagnitude<.000001f)flat=Vector3.right;end=other+flat.normalized*radius+Vector3.up*(end.y-other.y);}
            return end;
        }
        void LateUpdate()
        {
            if(!player || !player.isLocal || !capsule || !capsule.enabled){initialized=false;return;}
            Vector3 current=transform.position;
            if(!initialized || Vector3.Distance(previous,current)>3f){previous=current;initialized=true;}
            if(player.controlsEnabled && !(NetSession.I && NetSession.I.Loading))
            {
                foreach(var other in PlayerController.AllPlayers)
                {
                    if(!other || other==player || !other.gameObject.activeInHierarchy || Mathf.Abs(current.y-other.transform.position.y)>1.25f)continue;
                    float distance=Separation * Mathf.Max(transform.lossyScale.x, other.transform.lossyScale.x);
                    Vector3 safe=Block(previous,current,other.transform.position,distance); safe.y=current.y;
                    capsule.Move(safe-current); current=transform.position;
                }
            }
            previous=transform.position;
        }
    }
}
