using UnityEngine;

namespace MamasComing.Menu
{
    /// A fruit kid walking around the waiting lobby. The local one reads input (camera-relative), remote ones glide to their streamed pose.
    public class LobbyAvatar : MonoBehaviour
    {
        public ulong id;
        public bool isLocal;
        public AvatarLook look;
        public float PlanarSpeed { get; private set; }
        public bool Dancing { get; private set; }
        public bool Airborne { get; private set; }
        public bool Frozen;   // shop open / countdown

        CharacterController cc;
        Vector3 vel; float coyote, jumpBuffer, danceT;
        Vector3 netPos; float netYaw, netSpeed; bool netDance, netAir; float lastNet = -1f;
        TextMesh nameTag; Transform tagRoot;
        Lobby lobby;

        const float Walk = 3.6f, Run = 5.6f, Gravity = 22f, JumpV = 7.2f;

        public void Init(Lobby l, ulong pid, bool local, Cosmetic c, GameObject[] fruits, Material mat)
        {
            lobby = l; id = pid; isLocal = local;
            look = AvatarLook.Create(transform, fruits, mat, c);
            if (local)
            {
                cc = gameObject.AddComponent<CharacterController>();
                cc.height = .95f; cc.radius = .32f; cc.center = new Vector3(0, .5f, 0); cc.stepOffset = .3f; cc.skinWidth = .03f;
            }
            netPos = transform.position; netYaw = transform.eulerAngles.y;
            BuildTag();
        }

        public void SetLook(Cosmetic c, GameObject[] fruits, Material mat) { if (!look.Look.Same(c)) look.Apply(fruits, mat, c); }

        void BuildTag()
        {
            tagRoot = new GameObject("Name tag").transform; tagRoot.SetParent(transform, false);
            tagRoot.localPosition = new Vector3(0, 1.75f, 0);
            nameTag = tagRoot.gameObject.AddComponent<TextMesh>();
            nameTag.font = MenuUi.Font; nameTag.GetComponent<MeshRenderer>().sharedMaterial = MenuUi.Font.material;
            nameTag.anchor = TextAnchor.MiddleCenter; nameTag.alignment = TextAlignment.Center;
            nameTag.fontSize = 64; nameTag.characterSize = .045f; nameTag.fontStyle = FontStyle.Bold;
        }

        public void SetTag(string s, Color c) { if (nameTag) { nameTag.text = s; nameTag.color = c; } }

        public void SetNet(Vector3 p, float yaw, float speed, bool dance, bool air)
        {
            if (lastNet < 0f || (p - transform.position).sqrMagnitude > 25f) transform.position = p;   // first pose / teleport
            netPos = p; netYaw = yaw; netSpeed = speed; netDance = dance; netAir = air; lastNet = Time.time;
        }

        public void Teleport(Vector3 p, float yaw)
        {
            if (cc) cc.enabled = false;
            transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); vel = Vector3.zero;
            if (cc) cc.enabled = true;
        }

        /// Trampolines and the like.
        public void Launch(float up) { vel.y = up; coyote = 0f; Airborne = true; }

        void Update()
        {
            if (isLocal) LocalTick(); else RemoteTick();
            look.Tick(PlanarSpeed, Dancing, Airborne);
            var cam = Camera.main;
            if (tagRoot && cam) tagRoot.rotation = Quaternion.LookRotation(tagRoot.position - cam.transform.position);
        }

        void LocalTick()
        {
            float dt = Time.deltaTime;
            Vector2 input = Frozen ? Vector2.zero : MamaInput.Move;
            var cam = Camera.main ? Camera.main.transform : transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized, right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 wish = (fwd * input.y + right * input.x) * (MamaInput.Sprint ? Run : Walk);
            var planar = new Vector3(vel.x, 0, vel.z);
            planar = Vector3.MoveTowards(planar, wish, (cc.isGrounded ? 38f : 14f) * dt);
            vel.x = planar.x; vel.z = planar.z;

            var kb = UnityEngine.InputSystem.Keyboard.current; var gp = UnityEngine.InputSystem.Gamepad.current;
            bool jump = !Frozen && ((kb != null && kb.spaceKey.wasPressedThisFrame) || (gp != null && gp.buttonSouth.wasPressedThisFrame));
            bool dance = !Frozen && ((kb != null && kb.digit1Key.wasPressedThisFrame) || (gp != null && gp.dpad.up.wasPressedThisFrame));
            if (dance) { danceT = 1.6f; MenuSfx.Play(MenuSfx.Kind.Pop); }
            danceT -= dt; Dancing = danceT > 0f;

            if (cc.isGrounded) { coyote = .12f; if (vel.y < 0f) vel.y = -2f; } else coyote -= dt;
            jumpBuffer = jump ? .15f : jumpBuffer - dt;
            if (jumpBuffer > 0f && coyote > 0f) { vel.y = JumpV; jumpBuffer = 0f; coyote = 0f; }
            vel.y -= Gravity * dt;
            cc.Move(vel * dt + lobby.Carry(transform.position, cc.isGrounded, dt));
            Airborne = !cc.isGrounded;
            PlanarSpeed = new Vector3(vel.x, 0, vel.z).magnitude;
            if (PlanarSpeed > .3f && wish.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(vel.x, 0, vel.z)), 1f - Mathf.Exp(-14f * dt));
            if (transform.position.y < -10f) Teleport(lobby.SpawnPoint(0), 0f);   // fell off the world
            lobby.LocalMoved(this, planar);
        }

        void RemoteTick()
        {
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, netPos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, netYaw, 0), k);
            PlanarSpeed = netSpeed; Dancing = netDance; Airborne = netAir;
        }
    }
}
