using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing
{
    /// Separate, untimed house/quest design sandbox. Does not run the original flat-house flood grid.
    public class HousePlayground : MonoBehaviour
    {
        public PlayerController player;
        public ThirdPersonCamera orbit;
        public Transform upperFloor;
        public Camera planCamera;
        public HouseChore[] chores;
        public Transform roof;
        public Vector3 spawn = new Vector3(10,.06f,3), planCenter = new Vector3(13,0,10);
        Renderer[] upperRenderers;
        Renderer[] roofRenderers;
        sealed class PropVisual { public Pickup item; public Renderer[] renderers; public bool[] authoredVisibility; }
        PropVisual[] propVisuals;
        TextMesh[] labels;
        bool plan, notes, upstairs;
        bool exterior;
        Vector2 noteScroll;
        float messageUntil;
        string message;
        public bool Plan => plan;
        public bool Exterior => exterior;
        public void Say(string text) { message = text; messageUntil = Time.time + 3f; }

        void Start()
        {
            player.controlsEnabled = true; player.WakeUp();
            player.Motor.CameraTransform = orbit.transform;
            orbit.inputEnabled = true;
            upperRenderers = upperFloor.GetComponentsInChildren<Renderer>();
            roofRenderers = roof ? roof.GetComponentsInChildren<Renderer>() : new Renderer[0];
            propVisuals=FindObjectsByType<Pickup>(FindObjectsSortMode.None).Where(p=>p.gameObject.scene==gameObject.scene).Select(p=>
            {
                var rs=p.GetComponentsInChildren<Renderer>(true);
                return new PropVisual{item=p,renderers=rs,authoredVisibility=rs.Select(r=>r.enabled).ToArray()};
            }).ToArray();
            labels = FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            KidLook.Apply(player, KidLook.Selected);
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            planCamera.gameObject.SetActive(false);
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && k.f9Key.wasPressedThisFrame) SetPlan(!plan);
            if (k != null && k.f10Key.wasPressedThisFrame) SetExterior(!exterior);
            if (k != null && k.tabKey.wasPressedThisFrame) notes = !notes;
            if (k != null && k.escapeKey.wasPressedThisFrame) { if (plan||exterior) SetPlan(false); else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }
            if (!plan && !exterior && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            upstairs = player.transform.position.y > 1.7f;
            foreach (var r in upperRenderers) if (r && r.transform.IsChildOf(upperFloor)) { r.enabled = upstairs || exterior; if(exterior)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; }
            foreach (var r in roofRenderers) if(r) r.enabled = exterior;
            // Deliveries change floors and parents. Initial upstairs membership is not a valid
            // visibility source after carrying; otherwise a returned fish floats through the cutaway.
            foreach(var prop in propVisuals)
            {
                if(!prop.item)continue;
                bool visible=exterior || prop.item.IsHeld || upstairs==(prop.item.transform.position.y>2.5f);
                for(int i=0;i<prop.renderers.Length;i++)if(prop.renderers[i])prop.renderers[i].enabled=visible&&prop.authoredVisibility[i];
            }
            foreach(var label in labels)
            {
                bool upperLabel = label.transform.IsChildOf(upperFloor);
                bool onFloor = upperLabel == upstairs;
                bool isPlan = label.name.StartsWith("PlanLabel_");
                label.GetComponent<Renderer>().enabled = !exterior && onFloor && (isPlan ? plan : !plan && Vector3.Distance(label.transform.position, player.transform.position)<2.2f);
            }
            player.controlsEnabled = !plan && !exterior;
            orbit.inputEnabled = !notes;
            if (player.transform.position.y < -4f) ResetPlayer();
        }

        void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        public void ResetPlayer()
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.position = spawn; cc.enabled = true;
            player.Motor.Launch(Vector3.zero);
        }

        public void SetPlan(bool value)
        {
            exterior=false;plan = value; orbit.gameObject.SetActive(!plan); planCamera.gameObject.SetActive(plan);
            planCamera.orthographic=true;planCamera.orthographicSize=17;
            planCamera.transform.SetPositionAndRotation(planCenter+Vector3.up*40,Quaternion.Euler(90,0,0));
            Cursor.lockState = plan ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = plan;
        }

        public void SetExterior(bool value)
        {
            exterior=value;plan=false;orbit.gameObject.SetActive(!value);planCamera.gameObject.SetActive(value);
            planCamera.orthographic=false;planCamera.fieldOfView=36;planCamera.clearFlags=CameraClearFlags.SolidColor;planCamera.backgroundColor=new Color(.70f,.81f,.84f);
            planCamera.transform.position=planCenter+new Vector3(30,23,-39);planCamera.transform.LookAt(planCenter+Vector3.up*3);
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=value;
        }

        void OnGUI()
        {
            float scale = Mathf.Clamp(Screen.height / 900f, .7f, 1.7f);
            var old = GUI.matrix; GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale;
            GUI.Box(new Rect(18, 18, 415, 66), "");
            GUI.Label(new Rect(30, 23, 400, 28), "MAMA • HOUSE STUDY / " + (upstairs ? "UPSTAIRS" : "GROUND FLOOR"));
            GUI.Label(new Rect(30, 48, 400, 25), "Chores " + chores.Count(c => c && c.Complete) + "/" + chores.Length + " • TAB list • F9 plan • F10 outside");
            GUI.Box(new Rect(18, h - 86, w - 36, 70), "");
            GUI.Label(new Rect(30, h - 80, w - 50, 24), "WASD move • SHIFT run • SPACE jump • E use/hold • Q place • RMB throw • G shove");
            GUI.Label(new Rect(30, h - 57, w - 50, 24), "1 Apple / 2 Banana / 3 Orange / 4 Watermelon / 5 Wax apple • F6/F7 cycle • F8 tumble");
            if (player.Target && !plan)
            {
                GUI.Box(new Rect(w * .5f - 265, h - 156, 530, 58), "");
                GUI.Label(new Rect(w * .5f - 250, h - 150, 500, 45), "E  " + player.Target.GetPrompt(player) + (player.HoldProgress > 0 ? "  " + Mathf.RoundToInt(player.HoldProgress * 100f) + "%" : ""));
            }
            if (Time.time < messageUntil) GUI.Box(new Rect(w * .5f - 240, 96, 480, 46), message);
            if (notes)
            {
                GUI.Box(new Rect(w - 410, 100, 390, h-205), "HOUSE CHORES • "+chores.Length+" prototype challenges");
                noteScroll=GUI.BeginScrollView(new Rect(w-400,132,375,h-250),noteScroll,new Rect(0,0,350,chores.Length*30));
                for (int i = 0; i < chores.Length; i++)
                {
                    var c = chores[i]; GUI.Label(new Rect(0, i * 30, 350, 30), (c.Complete ? "[OK] " : "[  ] ") + c.displayName);
                }
                GUI.EndScrollView();
            }
            GUI.matrix = old;
        }
    }
}
