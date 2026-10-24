using UnityEngine;
using UnityEngine.InputSystem;

namespace PantryBanditLab
{
    public class PantryMotionLab : MonoBehaviour
    {
        public Animator actor;
        public Transform character, cheese, handLeft, handRight;
        public GameObject actionPlinth;
        public Camera view;
        public static readonly string[] Clips={"Idle","Walk","Run","Hop","Snatch","Cheer"};
        public bool autoShow=true;
        public int current;
        float timer, actionTime, yaw=18;
        Vector3 cheeseHome,cheeseCenterLocal;Quaternion cheeseRotation;
        bool orbit;
        void Start(){cheeseHome=cheese.position;cheeseRotation=cheese.rotation;cheeseCenterLocal=cheese.InverseTransformPoint(cheese.GetComponentInChildren<Renderer>().bounds.center);Play(0);}
        public void Play(int id)
        {
            current=Mathf.Clamp(id,0,5);timer=0;actionTime=0;
            if(id==4)character.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            if(actionPlinth)actionPlinth.SetActive(id==4);
            if(cheese)cheese.gameObject.SetActive(id==4);
            actor.CrossFadeInFixedTime("AN_Pip_"+Clips[current],.12f);
        }
        void Update()
        {
            var k=Keyboard.current;float dt=Time.deltaTime;timer+=dt;actionTime+=dt;
            if(k!=null)
            {
                if(k.digit1Key.wasPressedThisFrame){autoShow=false;Play(0);}
                if(k.digit2Key.wasPressedThisFrame){autoShow=false;Play(1);}
                if(k.digit3Key.wasPressedThisFrame){autoShow=false;Play(2);}
                if(k.digit4Key.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame){autoShow=false;Play(3);}
                if(k.digit5Key.wasPressedThisFrame||k.eKey.wasPressedThisFrame){autoShow=false;Play(4);}
                if(k.digit6Key.wasPressedThisFrame){autoShow=false;Play(5);}
                if(k.tabKey.wasPressedThisFrame){autoShow=!autoShow;timer=0;if(autoShow){character.SetPositionAndRotation(Vector3.zero,Quaternion.identity);Play(0);}}
                if(k.oKey.wasPressedThisFrame)orbit=!orbit;
                Vector2 input=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                if(input.sqrMagnitude>.01f)
                {
                    autoShow=false;int want=k.leftShiftKey.isPressed?2:1;if(current!=want)Play(want);
                    Vector3 dir=new Vector3(input.x,0,input.y).normalized;
                    character.rotation=Quaternion.Slerp(character.rotation,Quaternion.LookRotation(dir),1-Mathf.Exp(-12*dt));
                    var p=character.position+dir*(want==2?1.8f:.9f)*dt;p.y=0;
                    if(new Vector2(p.x,p.z).magnitude<1.7f)character.position=p;
                }
                else if(!autoShow&&(current==1||current==2)&&timer>1.3f) { /* selected loops continue in-place for inspection */ }
                if(k.rKey.wasPressedThisFrame){character.SetPositionAndRotation(Vector3.zero,Quaternion.identity);autoShow=true;Play(0);}
            }
            if(autoShow&&timer>4f)Play((current+1)%Clips.Length);
            if(orbit)yaw+=dt*12;
            if(Mouse.current!=null&&Mouse.current.rightButton.isPressed)yaw+=Mouse.current.delta.ReadValue().x*.18f;
            view.transform.position=Quaternion.Euler(0,yaw,0)*new Vector3(0,2.3f,5.8f);view.transform.LookAt(new Vector3(0,.85f,0));
        }
        void LateUpdate()
        {
            if(current==4&&handLeft&&handRight)
            {
                float t=Mathf.Clamp01(actionTime/1.6f);
                float grasp=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.5f,t));
                float release=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,t));
                var held=(handLeft.position+handRight.position)*.5f+character.forward*.03f-character.rotation*Vector3.Scale(cheeseCenterLocal,cheese.localScale);
                cheese.position=Vector3.Lerp(cheeseHome,held,grasp*(1-release));
                cheese.rotation=Quaternion.Slerp(cheeseRotation,character.rotation,grasp*(1-release));
            }
            else cheese.SetPositionAndRotation(cheeseHome,cheeseRotation);
        }
        void OnGUI()
        {
            float s=Mathf.Clamp(Screen.height/900f,.7f,1.7f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*s);float h=Screen.height/s,w=Screen.width/s;
            GUI.Box(new Rect(22,22,280,68),"");GUI.Label(new Rect(36,29,260,24),"PIP / THE PANTRY BANDIT");
            GUI.Label(new Rect(36,54,260,25),(autoShow?"AUTO / ":"PREVIEW / ")+Clips[current].ToUpper());
            GUI.Box(new Rect(22,h-73,w-44,51),"");
            GUI.Label(new Rect(35,h-65,w-60,24),"1 Idle  2 Walk  3 Run  4 Hop  5 Snatch  6 Cheer  |  TAB auto  |  O orbit  |  RMB drag camera");
            GUI.Label(new Rect(35,h-44,w-60,23),"WASD move  /  SHIFT run  /  SPACE hop  /  E snatch  /  R reset");GUI.matrix=old;
        }
    }
}
