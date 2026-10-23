using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MamasComing.Menu
{
    /// The walkable 3D waiting room: everybody runs around as their dressed-up fruit, plays with the beach ball,
    /// bounces on trampolines, rides the carousel and changes clothes at the wardrobe until the host hits START.
    public class Lobby : MonoBehaviour
    {
        public GameObject[] fruitPrefabs;
        public Material baseMaterial;
        public Camera cam;
        public LobbyNet net;

        public LobbyAvatar LocalAvatar { get; private set; }
        public Rigidbody Ball { get; private set; }
        public int GoalsPink { get; private set; }
        public int GoalsTeal { get; private set; }
        readonly Dictionary<ulong, LobbyAvatar> avatars = new Dictionary<ulong, LobbyAvatar>();

        // world layout (metres)
        static readonly Vector3 ShopPos = new Vector3(-7.2f, 0, 2.6f);
        static readonly Vector3 FieldCenter = new Vector3(4.2f, 0, -2.6f);
        const float FieldHalfX = 4.2f, FieldHalfZ = 2.6f;
        static readonly Vector3[] Tramps = { new Vector3(-5.6f, 0, -4.6f), new Vector3(-3.4f, 0, -6.4f), new Vector3(-7.4f, 0, -2.4f) };
        static readonly Vector3 CarouselPos = new Vector3(1.2f, 0, 6.2f);
        const float CarouselR = 2.2f, CarouselSpeed = 55f, WorldR = 13f;
        Transform carousel, sign;
        TextMesh signText;
        readonly List<Transform> tramps = new List<Transform>();
        readonly float[] trampSquish = new float[3];

        // camera
        float camYaw = 25f, camPitch = 42f, camDist = 8.5f;
        Vector3 camFocus;
        bool shopOpen; float shopBlend;

        // ball sync (clients)
        Vector3 ballNetPos; Quaternion ballNetRot = Quaternion.identity; float kickCool, predictT;

        // UI
        RectTransform ui;
        Text codeText, statusText, playersText, levelText, levelBlurb, hintText, promptText, waitText, bigText, scoreText;
        PunchButton startBtn, levelPrev, levelNext, maxPrev, maxNext, copyBtn;
        Text maxText;
        RectTransform shopPanel, bigRt; Text[] shopValues = new Text[4];
        float bigT;

        void Start()
        {
            Loc.Load();
            AvatarLook.SetBaseMaterial(baseMaterial);
            if (!cam) cam = Camera.main;
            if (!net) net = gameObject.AddComponent<LobbyNet>();
            net.lobby = this;
            BuildWorld();
            BuildUi();
            SpawnLocal();
            camFocus = LocalAvatar.transform.position;
            OnRosterChanged();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        // ================================================================== world
        void BuildWorld()
        {
            var root = new GameObject("Lobby World").transform;
            Color cream = new Color(1f, .93f, .82f), peach = new Color(1f, .78f, .66f), mint = new Color(.62f, .9f, .8f), lilac = new Color(.78f, .7f, 1f);

            // island floor: big round toy-mat with a scalloped rim
            var floor = Solid(PrimitiveType.Cylinder, root, new Vector3(0, -.25f, 0), new Vector3(WorldR * 2, .25f, WorldR * 2), cream);
            floor.name = "Floor";
            Deco(PrimitiveType.Cylinder, root, new Vector3(0, -.6f, 0), new Vector3(WorldR * 2 + .6f, .4f, WorldR * 2 + .6f), peach);
            for (int i = 0; i < 28; i++)
            {
                float a = i * Mathf.PI * 2f / 28f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (WorldR - .1f);
                var post = Solid(PrimitiveType.Cylinder, root, p + Vector3.up * .45f, new Vector3(.5f, .45f, .5f), i % 2 == 0 ? MenuUi.Pink : MenuUi.Teal);
                post.name = "Rim";
                Deco(PrimitiveType.Sphere, root, p + Vector3.up * 1f, Vector3.one * .55f, i % 2 == 0 ? MenuUi.Lemon : Color.white);
            }
            // invisible wall so nobody tumbles off the island
            for (int i = 0; i < 36; i++)
            {
                float a = i * Mathf.PI * 2f / 36f;
                var w = new GameObject("Fence collider").transform; w.SetParent(root, false);
                w.position = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * WorldR + Vector3.up * 1.5f;
                w.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                var bc = w.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(.6f, 3f, 2.4f);
            }
            // rug stripes
            for (int i = 0; i < 6; i++) Deco(PrimitiveType.Cylinder, root, new Vector3(0, .005f + i * .001f, 0), new Vector3(9f - i * 1.4f, .005f, 9f - i * 1.4f), i % 2 == 0 ? lilac : cream);

            // ---- dress-up wardrobe
            var shop = new GameObject("Dress-up shop").transform; shop.SetParent(root, false); shop.position = ShopPos; shop.rotation = Quaternion.Euler(0, 70f, 0);
            Deco(PrimitiveType.Cylinder, shop, new Vector3(0, .01f, 1.2f), new Vector3(3.2f, .01f, 3.2f), MenuUi.Pink);
            Solid(PrimitiveType.Cube, shop, new Vector3(0, 1.3f, -.3f), new Vector3(2.6f, 2.6f, .9f), new Color(.98f, .6f, .72f));
            Deco(PrimitiveType.Cube, shop, new Vector3(-.62f, 1.25f, .16f), new Vector3(1.1f, 2.2f, .06f), new Color(1f, .82f, .88f));
            Deco(PrimitiveType.Cube, shop, new Vector3(.62f, 1.25f, .16f), new Vector3(1.1f, 2.2f, .06f), new Color(.75f, .93f, 1f)).name = "Mirror";
            Deco(PrimitiveType.Sphere, shop, new Vector3(-.15f, 1.25f, .22f), Vector3.one * .12f, MenuUi.Lemon);
            Deco(PrimitiveType.Cube, shop, new Vector3(0, 2.75f, -.3f), new Vector3(2.9f, .25f, 1.1f), MenuUi.Grape);
            for (int i = 0; i < 5; i++)
            {
                var hatPos = new Vector3(-1.1f + i * .55f, 2.98f, -.3f);
                var c = MenuFlow.Colors[i * 2 % MenuFlow.Colors.Length];
                var hc = new GameObject("Display hat", typeof(MeshFilter), typeof(MeshRenderer)); hc.transform.SetParent(shop, false);
                hc.transform.localPosition = hatPos; hc.transform.localScale = new Vector3(.3f, .38f, .3f);
                hc.GetComponent<MeshFilter>().sharedMesh = AvatarLook.Cone(); hc.GetComponent<MeshRenderer>().sharedMaterial = AvatarLook.Mat(c);
            }
            Label3D(shop, new Vector3(0, 3.5f, -.3f), MenuFlow.Txt("DRESS UP!", "ร้านแต่งตัว!"), MenuUi.Pink, .09f);

            // ---- soccer pitch with two goals
            var field = new GameObject("Ball pitch").transform; field.SetParent(root, false); field.position = FieldCenter;
            Deco(PrimitiveType.Cube, field, new Vector3(0, .012f, 0), new Vector3(FieldHalfX * 2, .01f, FieldHalfZ * 2), new Color(.55f, .85f, .5f));
            Deco(PrimitiveType.Cube, field, new Vector3(0, .02f, 0), new Vector3(.08f, .01f, FieldHalfZ * 2), Color.white);
            Deco(PrimitiveType.Cylinder, field, new Vector3(0, .018f, 0), new Vector3(1.6f, .004f, 1.6f), Color.white);
            Goal(field, -FieldHalfX, MenuUi.Pink); Goal(field, FieldHalfX, MenuUi.Teal);
            var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere); ballGo.name = "Beach ball"; ballGo.transform.SetParent(root, false);
            ballGo.transform.localScale = Vector3.one * .7f; ballGo.transform.position = FieldCenter + Vector3.up * 1.5f;
            ballGo.GetComponent<Renderer>().sharedMaterial = AvatarLook.Mat(Color.white);
            for (int i = 0; i < 3; i++)   // beach-ball stripes
            {
                var band = Deco(PrimitiveType.Sphere, ballGo.transform, Vector3.zero, new Vector3(1.01f, 1.01f, .34f), i == 0 ? MenuUi.Pink : i == 1 ? MenuUi.Lemon : MenuUi.Teal);
                band.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
            }
            var pm = new PhysicsMaterial("Bouncy ball") { bounciness = .75f, bounceCombine = PhysicsMaterialCombine.Maximum, dynamicFriction = .3f, staticFriction = .3f };
            ballGo.GetComponent<Collider>().sharedMaterial = pm;
            Ball = ballGo.AddComponent<Rigidbody>(); Ball.mass = .4f; Ball.linearDamping = .25f; Ball.angularDamping = .3f; Ball.interpolation = RigidbodyInterpolation.Interpolate;
            Ball.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (!net.IsHost) Ball.isKinematic = true;
            ballNetPos = Ball.position;
            var score = new GameObject("Score").transform; score.SetParent(field, false); score.localPosition = new Vector3(0, 2.6f, FieldHalfZ + .6f);
            Deco(PrimitiveType.Cube, score, new Vector3(0, 0, .05f), new Vector3(2.6f, 1.1f, .12f), MenuUi.Ink);
            Deco(PrimitiveType.Cylinder, score, new Vector3(-.9f, -1.3f, .1f), new Vector3(.1f, 1.3f, .1f), MenuUi.Ink);
            Deco(PrimitiveType.Cylinder, score, new Vector3(.9f, -1.3f, .1f), new Vector3(.1f, 1.3f, .1f), MenuUi.Ink);
            scoreBoard = Label3D(score, new Vector3(0, 0, -.05f), "0 : 0", Color.white, .11f);
            scoreBoard.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            // ---- trampolines
            for (int i = 0; i < Tramps.Length; i++)
            {
                var t = new GameObject("Trampoline").transform; t.SetParent(root, false); t.position = Tramps[i];
                Solid(PrimitiveType.Cylinder, t, new Vector3(0, .14f, 0), new Vector3(1.9f, .14f, 1.9f), MenuUi.Ink);
                var pad = Deco(PrimitiveType.Cylinder, t, new Vector3(0, .29f, 0), new Vector3(1.6f, .02f, 1.6f), i == 1 ? MenuUi.Lemon : MenuUi.Teal);
                tramps.Add(pad.transform);
            }
            Label3D(root, Tramps[0] + new Vector3(0, 2.4f, 0), MenuFlow.Txt("BOING!", "เด้งๆ!"), MenuUi.Teal, .07f);

            // ---- carousel
            carousel = new GameObject("Carousel").transform; carousel.SetParent(root, false); carousel.position = CarouselPos;
            Solid(PrimitiveType.Cylinder, carousel, new Vector3(0, .1f, 0), new Vector3(CarouselR * 2, .1f, CarouselR * 2), lilac);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                Deco(PrimitiveType.Cube, carousel, new Vector3(Mathf.Cos(a) * CarouselR * .55f, .205f, Mathf.Sin(a) * CarouselR * .55f), new Vector3(CarouselR * 1.05f, .01f, .12f), i % 2 == 0 ? Color.white : MenuUi.Pink).transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            }
            Solid(PrimitiveType.Cylinder, carousel, new Vector3(0, 1.3f, 0), new Vector3(.25f, 1.3f, .25f), MenuUi.Lemon);
            var roof = new GameObject("Roof", typeof(MeshFilter), typeof(MeshRenderer)); roof.transform.SetParent(carousel, false);
            roof.transform.localPosition = new Vector3(0, 2.6f, 0); roof.transform.localScale = new Vector3(CarouselR * 2.2f, 1f, CarouselR * 2.2f);
            roof.GetComponent<MeshFilter>().sharedMesh = AvatarLook.Cone(); roof.GetComponent<MeshRenderer>().sharedMaterial = AvatarLook.Mat(MenuUi.Pink);

            // ---- level sign (what we are about to play)
            sign = new GameObject("Level sign").transform; sign.SetParent(root, false); sign.position = new Vector3(-1.5f, 0, -9.6f); sign.rotation = Quaternion.Euler(0, 180f + 8f, 0);
            Solid(PrimitiveType.Cube, sign, new Vector3(0, 2.4f, 0), new Vector3(5.6f, 2.2f, .25f), MenuUi.Paper);
            Deco(PrimitiveType.Cube, sign, new Vector3(0, 2.4f, .02f), new Vector3(5.9f, 2.5f, .2f), MenuUi.Ink);
            Solid(PrimitiveType.Cylinder, sign, new Vector3(-2.2f, .7f, 0), new Vector3(.18f, .7f, .18f), MenuUi.Ink);
            Solid(PrimitiveType.Cylinder, sign, new Vector3(2.2f, .7f, 0), new Vector3(.18f, .7f, .18f), MenuUi.Ink);
            signText = Label3D(sign, new Vector3(0, 2.4f, -.14f), "", MenuUi.Ink, .075f);
            signText.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            // party balloons
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.39f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (9.5f + (i % 3));
                var b = Deco(PrimitiveType.Sphere, root, p + Vector3.up * (3.2f + i % 3 * .4f), new Vector3(.8f, .95f, .8f), MenuFlow.Colors[i % MenuFlow.Colors.Length]);
                b.AddComponent<Bobber>().phase = i;
                Deco(PrimitiveType.Cylinder, b.transform, new Vector3(0, -1.6f, 0), new Vector3(.02f, 1.1f, .02f), Color.white);
            }
        }

        TextMesh scoreBoard;

        void Goal(Transform field, float x, Color c)
        {
            float s = Mathf.Sign(x);
            var g = new GameObject("Goal").transform; g.SetParent(field, false); g.localPosition = new Vector3(x, 0, 0);
            Solid(PrimitiveType.Cylinder, g, new Vector3(0, .65f, -1.2f), new Vector3(.14f, .65f, .14f), c);
            Solid(PrimitiveType.Cylinder, g, new Vector3(0, .65f, 1.2f), new Vector3(.14f, .65f, .14f), c);
            Solid(PrimitiveType.Cube, g, new Vector3(0, 1.32f, 0), new Vector3(.16f, .16f, 2.56f), c);
            Solid(PrimitiveType.Cube, g, new Vector3(s * .7f, .65f, 0), new Vector3(.06f, 1.3f, 2.4f), new Color(1f, 1f, 1f, 1f)).name = "Net";
            Solid(PrimitiveType.Cube, g, new Vector3(s * .35f, .65f, -1.2f), new Vector3(.7f, 1.3f, .05f), Color.white);
            Solid(PrimitiveType.Cube, g, new Vector3(s * .35f, .65f, 1.2f), new Vector3(.7f, 1.3f, .05f), Color.white);
        }

        GameObject Solid(PrimitiveType t, Transform p, Vector3 pos, Vector3 scale, Color c) => AvatarLook.Prim(t, p, pos, scale, c, true);
        GameObject Deco(PrimitiveType t, Transform p, Vector3 pos, Vector3 scale, Color c)
        {
            var g = AvatarLook.Prim(t, p, pos, scale, c);
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return g;
        }

        TextMesh Label3D(Transform parent, Vector3 pos, string s, Color c, float size)
        {
            var go = new GameObject("Label"); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>(); tm.font = MenuUi.Font; go.GetComponent<MeshRenderer>().sharedMaterial = MenuUi.Font.material;
            tm.text = s; tm.color = c; tm.fontSize = 96; tm.characterSize = size; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.fontStyle = FontStyle.Bold;
            if (parent == null || parent.GetComponentInParent<Lobby>() == null) go.AddComponent<Billboard>();
            return tm;
        }

        public Vector3 SpawnPoint(int i)
        {
            float a = i * 1.2f + .4f;
            return new Vector3(Mathf.Cos(a) * 2.2f, .05f, Mathf.Sin(a) * 2.2f - 1f);
        }

        // ================================================================== avatars
        void SpawnLocal()
        {
            LocalAvatar = MakeAvatar(net.LocalId, true, MenuFlow.Mine);
            int idx = Mathf.Max(0, net.Roster.FindIndex(p => p.id == net.LocalId));
            LocalAvatar.Teleport(SpawnPoint(idx), 200f);
        }

        LobbyAvatar MakeAvatar(ulong id, bool local, Cosmetic c)
        {
            var go = new GameObject(local ? "Me" : "Friend " + id);
            go.transform.position = SpawnPoint(avatars.Count);
            var a = go.AddComponent<LobbyAvatar>();
            a.Init(this, id, local, c, fruitPrefabs, baseMaterial);
            avatars[id] = a;
            return a;
        }

        public void OnRosterChanged()
        {
            if (!LocalAvatar) return;
            var seen = new HashSet<ulong>();
            foreach (var p in net.Roster)
            {
                seen.Add(p.id);
                if (!avatars.TryGetValue(p.id, out var a) || !a) a = MakeAvatar(p.id, p.id == net.LocalId, p.look);
                else if (p.id != net.LocalId) a.SetLook(p.look, fruitPrefabs, baseMaterial);
                string me = p.id == net.LocalId ? MenuFlow.Txt(" (you)", " (คุณ)") : "";
                a.SetTag("P" + p.number + me, p.look.Tint);
            }
            if (!seen.Contains(net.LocalId)) LocalAvatar.SetTag(MenuFlow.Txt("You", "คุณ"), MenuFlow.Mine.Tint);
            foreach (var id in new List<ulong>(avatars.Keys))
                if (id != net.LocalId && !seen.Contains(id)) { if (avatars[id]) Destroy(avatars[id].gameObject); avatars.Remove(id); }
            RefreshUi();
        }

        public void OnPose(ulong id, Vector3 p, float yaw, float speed, bool dance, bool air)
        {
            if (!avatars.TryGetValue(id, out var a) || !a) return;   // roster arrives reliably; ignore poses of unknown players until then
            a.SetNet(p, yaw, speed, dance, air);
        }

        /// Carousel: carry anybody standing on it around the pole.
        public Vector3 Carry(Vector3 pos, bool grounded, float dt)
        {
            var d = pos - CarouselPos; d.y = 0;
            if (!grounded || d.magnitude > CarouselR || pos.y > .6f) return Vector3.zero;
            return Quaternion.Euler(0, CarouselSpeed * dt, 0) * d - d;
        }

        /// Called every frame for the local avatar: trampolines and ball kicks.
        public void LocalMoved(LobbyAvatar a, Vector3 planarVel)
        {
            var p = a.transform.position;
            if (!a.Airborne || a.transform.position.y < .45f)
                for (int i = 0; i < Tramps.Length; i++)
                {
                    var d = p - Tramps[i]; d.y = 0;
                    if (d.magnitude < .85f && p.y > .2f && p.y < .5f && !a.Airborne) { a.Launch(13f); trampSquish[i] = 1f; MenuSfx.Play(MenuSfx.Kind.Pop); }
                }
            kickCool -= Time.deltaTime;
            if (Ball && kickCool <= 0f)
            {
                var to = Ball.position - (p + Vector3.up * .45f);
                float reach = .35f + .45f + .12f;
                if (to.magnitude < reach && planarVel.magnitude > .4f)
                {
                    var dir = new Vector3(to.x, 0, to.z).normalized;
                    var impulse = dir * (2.2f + planarVel.magnitude * 1.1f) + Vector3.up * (a.Airborne ? 3.2f : 2.2f);
                    kickCool = .25f; net.Kick(impulse);
                }
            }
        }

        public void ApplyKick(Vector3 impulse)
        {
            if (!Ball) return;
            Ball.linearVelocity = Vector3.zero;
            Ball.AddForce(impulse, ForceMode.VelocityChange);
            Ball.AddTorque(Random.insideUnitSphere * 4f, ForceMode.VelocityChange);
            MenuSfx.Play(MenuSfx.Kind.Click);
        }
        public void PredictKick(Vector3 impulse) { predictT = .2f; ballNetPos = Ball.position + impulse * .12f; MenuSfx.Play(MenuSfx.Kind.Click); }

        public void OnBallState(Vector3 p, Quaternion q, Vector3 v, int pink, int teal)
        {
            ballNetPos = p + v * .05f; ballNetRot = q;
            if (pink != GoalsPink || teal != GoalsTeal) { bool fresh = pink + teal > GoalsPink + GoalsTeal; GoalsPink = pink; GoalsTeal = teal; if (fresh) Shout(MenuFlow.Txt("GOAL!", "โกล!"), MenuUi.Lemon); }
        }

        void UpdateBall()
        {
            if (!Ball) return;
            if (net.IsHost)
            {
                var lp = Ball.position - FieldCenter;
                if (Mathf.Abs(lp.x) > FieldHalfX + .25f && Mathf.Abs(lp.z) < 1.15f && lp.y < 1.3f)
                {
                    if (lp.x < 0) GoalsTeal++; else GoalsPink++;   // the ball went into PINK's goal -> point for TEAL
                    Shout(MenuFlow.Txt("GOAL!", "โกล!"), MenuUi.Lemon);
                    ResetBall();
                }
                if (Ball.position.y < -5f || new Vector2(Ball.position.x, Ball.position.z).magnitude > WorldR + 3f) ResetBall();
            }
            else
            {
                predictT -= Time.deltaTime;
                float k = 1f - Mathf.Exp(-(predictT > 0f ? 6f : 14f) * Time.deltaTime);
                Ball.MovePosition(Vector3.Lerp(Ball.position, ballNetPos, k));
                Ball.MoveRotation(Quaternion.Slerp(Ball.rotation, ballNetRot, k));
            }
            if (scoreBoard) scoreBoard.text = "<color=#FF5580>" + GoalsPink + "</color> : <color=#29C2B8>" + GoalsTeal + "</color>";
        }

        void ResetBall()
        {
            Ball.linearVelocity = Vector3.zero; Ball.angularVelocity = Vector3.zero;
            Ball.position = FieldCenter + Vector3.up * 2.5f; Ball.transform.position = Ball.position;
        }

        // ================================================================== frame
        void Update()
        {
            if (carousel) carousel.Rotate(0, CarouselSpeed * Time.deltaTime, 0, Space.World);
            for (int i = 0; i < tramps.Count; i++)
            {
                trampSquish[i] = Mathf.MoveTowards(trampSquish[i], 0f, Time.deltaTime * 4f);
                float s = Mathf.Sin(trampSquish[i] * Mathf.PI * 3f) * trampSquish[i] * .12f;
                tramps[i].localPosition = new Vector3(0, .29f - s, 0);
            }
            UpdateBall();
            HandleInput();
            UpdateCamera();
            UpdateHud();
        }

        void HandleInput()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            bool interact = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame);
            bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame);
            if (counting) return;
            if (shopOpen) { if (interact || back) SetShop(false); return; }
            if (interact && NearShop) SetShop(true);
            else if (back) Leave();
            var m = Mouse.current;
            if (m != null)
            {
                if (m.rightButton.isPressed) { var d = m.delta.ReadValue(); camYaw += d.x * .25f; camPitch = Mathf.Clamp(camPitch - d.y * .15f, 18f, 70f); }
                float sc = m.scroll.ReadValue().y; if (Mathf.Abs(sc) > .01f) camDist = Mathf.Clamp(camDist - Mathf.Sign(sc) * .8f, 4.5f, 14f);
            }
            if (kb != null) { if (kb.qKey.isPressed) camYaw += 90f * Time.deltaTime; }
            if (gp != null) { var r = gp.rightStick.ReadValue(); camYaw += r.x * 120f * Time.deltaTime; camPitch = Mathf.Clamp(camPitch - r.y * 60f * Time.deltaTime, 18f, 70f); }
        }

        bool NearShop => LocalAvatar && Vector3.Distance(Flat(LocalAvatar.transform.position), Flat(ShopPos + Quaternion.Euler(0, 70f, 0) * new Vector3(0, 0, 1.2f))) < 2.1f;
        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

        void UpdateCamera()
        {
            if (!cam || !LocalAvatar) return;
            shopBlend = Mathf.MoveTowards(shopBlend, shopOpen ? 1f : 0f, Time.deltaTime * 3f);
            float e = shopBlend * shopBlend * (3f - 2f * shopBlend);
            var me = LocalAvatar.transform;
            camFocus = Vector3.Lerp(camFocus, me.position + Vector3.up * .8f, 1f - Mathf.Exp(-8f * Time.deltaTime));
            var orbit = camFocus + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0, 0, -camDist);
            // shop: close-up from the front, kid shifted left of the dress-up panel
            var shopFocus = me.position + Vector3.up * .65f + me.right * .55f;
            var shopEye = me.position + me.forward * 2.6f + Vector3.up * 1.15f + me.right * .55f;
            cam.transform.position = Vector3.Lerp(orbit, shopEye, e);
            cam.transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(camFocus - orbit), Quaternion.LookRotation(shopFocus - shopEye), e);
        }

        // ================================================================== shop
        void SetShop(bool on)
        {
            shopOpen = on; LocalAvatar.Frozen = on || counting;
            shopPanel.gameObject.SetActive(on);
            MenuSfx.Play(on ? MenuSfx.Kind.Pop : MenuSfx.Kind.Click);
            if (on)
            {
                // face the camera-side of the wardrobe area so the close-up shows the outfit
                var toCam = Flat(cam.transform.position - LocalAvatar.transform.position);
                if (toCam.sqrMagnitude > .01f) LocalAvatar.transform.rotation = Quaternion.LookRotation(toCam);
            }
            RefreshShop();
        }

        void Cycle(int row, int dir)
        {
            var c = MenuFlow.Mine;
            switch (row)
            {
                case 0: c.fruit = (byte)Wrap(c.fruit + dir, MenuFlow.FruitEn.Length); break;
                case 1: c.hat = (byte)Wrap(c.hat + dir, MenuFlow.HatEn.Length); break;
                case 2: c.face = (byte)Wrap(c.face + dir, MenuFlow.FaceEn.Length); break;
                case 3: c.color = (byte)Wrap(c.color + dir, MenuFlow.Colors.Length); break;
            }
            LocalAvatar.SetLook(c, fruitPrefabs, baseMaterial);
            net.SetMyLook(c);
            RefreshShop();
        }
        static int Wrap(int v, int n) => (v % n + n) % n;

        void RefreshShop()
        {
            var c = MenuFlow.Mine;
            shopValues[0].text = Loc.Thai ? MenuFlow.FruitTh[c.fruit] : MenuFlow.FruitEn[c.fruit];
            shopValues[1].text = Loc.Thai ? MenuFlow.HatTh[c.hat] : MenuFlow.HatEn[c.hat];
            shopValues[2].text = Loc.Thai ? MenuFlow.FaceTh[c.face] : MenuFlow.FaceEn[c.face];
            shopValues[3].text = Loc.Thai ? MenuFlow.ColorTh[c.color] : MenuFlow.ColorEn[c.color];
            shopValues[3].color = Color.Lerp(c.Tint, MenuUi.Ink, .25f);
        }

        // ================================================================== start / leave
        bool counting;
        public void BeginCountdown(int level)
        {
            if (counting) return;
            counting = true;
            if (shopOpen) SetShop(false);
            if (LocalAvatar) LocalAvatar.Frozen = true;
            StartCoroutine(Countdown(level));
        }

        IEnumerator Countdown(int level)
        {
            for (int i = 3; i >= 1; i--) { Shout(i.ToString(), MenuUi.Lemon); MenuSfx.Play(MenuSfx.Kind.Pop); yield return new WaitForSecondsRealtime(.8f); }
            Shout(MenuFlow.Txt("GO!", "ลุย!"), MenuUi.Pink); MenuSfx.Play(MenuSfx.Kind.Go);
            yield return new WaitForSecondsRealtime(.6f);
            LevelLauncher.Launch(level);
        }

        public void Kicked(string why)
        {
            MenuFlow.StatusNote = why;
            Leave();
        }

        void Leave()
        {
            if (NetSession.I) { NetSession.I.enabled = true; NetSession.I.Leave(); }
            SceneManager.LoadScene(MenuFlow.MenuScene);
        }

        void Shout(string s, Color c) { bigText.text = s; bigText.color = c; bigT = 1f; }

        // ================================================================== HUD
        void BuildUi()
        {
            ui = MenuUi.Canvas("Lobby UI", 40);

            // room card (top-left)
            var card = MenuUi.Rect("Room card", ui, new Vector2(0, 1), new Vector2(28, -24), new Vector2(560, 196));
            card.localRotation = Quaternion.Euler(0, 0, 1.2f);
            MenuUi.Panel(card, MenuUi.Paper, MenuUi.Ink, 5f);
            var cap = MenuUi.Rect("cap", card, new Vector2(0, 1), new Vector2(24, -14), new Vector2(380, 40));
            MenuUi.Label(cap, MenuFlow.Txt("ROOM CODE", "รหัสห้อง"), 26, MenuUi.Pink, TextAnchor.MiddleLeft);
            var code = MenuUi.Rect("code", card, new Vector2(0, 1), new Vector2(24, -52), new Vector2(400, 76));
            codeText = MenuUi.Punchy(MenuUi.Label(code, "", 62, MenuUi.Ink, TextAnchor.MiddleLeft), new Color(1f, .8f, .85f), 3f);
            copyBtn = MenuUi.Button(card, MenuFlow.Txt("COPY", "ก็อป"), new Vector2(1, 1), new Vector2(-22, -62), new Vector2(110, 58), MenuUi.Teal,
                () => { GUIUtility.systemCopyBuffer = NetSession.I ? NetSession.I.JoinCode : ""; Shout(MenuFlow.Txt("Copied!", "ก็อปแล้ว!"), MenuUi.Teal); }, 26);
            var st = MenuUi.Rect("status", card, new Vector2(0, 0), new Vector2(24, 12), new Vector2(510, 56));
            statusText = MenuUi.Label(st, "", 22, new Color(.4f, .3f, .38f), TextAnchor.MiddleLeft, false);

            MenuUi.Button(ui, MenuFlow.Txt("LEAVE", "ออกจากห้อง"), new Vector2(0, 1), new Vector2(28, -236), new Vector2(200, 62), new Color(.55f, .45f, .55f), Leave, 26, -1.5f);

            // players (top-right)
            var pl = MenuUi.Rect("Players", ui, new Vector2(1, 1), new Vector2(-28, -24), new Vector2(420, 300));
            pl.localRotation = Quaternion.Euler(0, 0, -1f);
            MenuUi.Panel(pl, new Color(.18f, .09f, .16f, .82f), MenuUi.Ink, 4f);
            playersText = MenuUi.Label(pl, "", 30, Color.white, TextAnchor.UpperLeft, true, 26);
            playersText.rectTransform.offsetMax = new Vector2(-26, -20); playersText.supportRichText = true; playersText.lineSpacing = 1.15f;

            // level / start (bottom-right)
            var lv = MenuUi.Rect("Level card", ui, new Vector2(1, 0), new Vector2(-28, 28), new Vector2(620, 330));
            MenuUi.Panel(lv, MenuUi.Paper, MenuUi.Ink, 5f);
            var lcap = MenuUi.Rect("cap", lv, new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(560, 36));
            MenuUi.Label(lcap, MenuFlow.Txt("NEXT LEVEL", "ด่านที่จะเล่น"), 24, MenuUi.Pink);
            var ln = MenuUi.Rect("name", lv, new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(420, 62));
            levelText = MenuUi.Punchy(MenuUi.Label(ln, "", 46, Color.white), MenuUi.Ink, 3f);
            var lb = MenuUi.Rect("blurb", lv, new Vector2(.5f, 1), new Vector2(0, -112), new Vector2(560, 56));
            levelBlurb = MenuUi.Label(lb, "", 22, new Color(.35f, .25f, .32f), TextAnchor.MiddleCenter, false);
            levelPrev = MenuUi.Button(lv, "<", new Vector2(0, 1), new Vector2(22, -50), new Vector2(64, 60), MenuUi.Grape, () => net.SetRoom(net.Level - 1, net.Max), 36);
            levelNext = MenuUi.Button(lv, ">", new Vector2(1, 1), new Vector2(-22, -50), new Vector2(64, 60), MenuUi.Grape, () => net.SetRoom(net.Level + 1, net.Max), 36);
            var mx = MenuUi.Rect("max", lv, new Vector2(.5f, 1), new Vector2(0, -176), new Vector2(300, 44));
            maxText = MenuUi.Label(mx, "", 28, MenuUi.Ink);
            maxPrev = MenuUi.Button(lv, "-", new Vector2(.5f, 1), new Vector2(-190, -172), new Vector2(54, 50), MenuUi.Teal, () => net.SetRoom(net.Level, net.Max - 1), 34);
            maxNext = MenuUi.Button(lv, "+", new Vector2(.5f, 1), new Vector2(190, -172), new Vector2(54, 50), MenuUi.Teal, () => net.SetRoom(net.Level, net.Max + 1), 34);
            startBtn = MenuUi.Button(lv, MenuFlow.Txt("START!", "เริ่มเกม!"), new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(560, 92), MenuUi.Pink, () => net.StartGame(), 50, -1f);
            var wt = MenuUi.Rect("wait", lv, new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(560, 92));
            waitText = MenuUi.Label(wt, MenuFlow.Txt("Waiting for the host to start...", "รอเจ้าของห้องกดเริ่ม..."), 30, MenuUi.Grape);

            // hints + prompt (bottom-left / center)
            var hint = MenuUi.Rect("Hints", ui, new Vector2(0, 0), new Vector2(28, 24), new Vector2(980, 50));
            hintText = MenuUi.Label(hint, MenuFlow.Txt("WASD move · Space jump · Shift run · 1 dance · Right-drag camera · Esc leave",
                "WASD เดิน · Space กระโดด · Shift วิ่ง · 1 เต้น · คลิกขวาลากหมุนกล้อง · Esc ออก"), 22, Color.white, TextAnchor.MiddleLeft);
            hintText.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            var pr = MenuUi.Rect("Prompt", ui, new Vector2(.5f, 0), new Vector2(0, 170), new Vector2(520, 76), new Vector2(.5f, .5f));
            MenuUi.Panel(pr, MenuUi.Pink, MenuUi.Ink, 4f);
            promptText = MenuUi.Punchy(MenuUi.Label(pr, MenuFlow.Txt("[E]  DRESS UP!", "[E]  แต่งตัว!"), 34, Color.white), MenuUi.Ink, 2f);

            // big shout (countdown, goals)
            bigRt = MenuUi.Rect("Shout", ui, new Vector2(.5f, .5f), new Vector2(0, 120), new Vector2(900, 220));
            bigText = MenuUi.Punchy(MenuUi.Label(bigRt, "", 150, MenuUi.Lemon), MenuUi.Ink, 7f);

            // dress-up panel
            shopPanel = MenuUi.Rect("Dress-up", ui, new Vector2(1, .5f), new Vector2(-60, 40), new Vector2(620, 620));
            shopPanel.localRotation = Quaternion.Euler(0, 0, -1.5f);
            MenuUi.Panel(shopPanel, MenuUi.Paper, MenuUi.Ink, 6f);
            var title = MenuUi.Rect("title", shopPanel, new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(560, 80));
            MenuUi.Punchy(MenuUi.Label(title, MenuFlow.Txt("DRESS UP!", "แต่งตัว!"), 64, MenuUi.Pink), MenuUi.Ink, 4f);
            string[] rows = { MenuFlow.Txt("FRUIT", "ผลไม้"), MenuFlow.Txt("HAT", "หมวก"), MenuFlow.Txt("FACE", "หน้า"), MenuFlow.Txt("COLOUR", "สี") };
            for (int i = 0; i < 4; i++)
            {
                int row = i; float y = -130 - i * 100;
                var lab = MenuUi.Rect("row" + i, shopPanel, new Vector2(.5f, 1), new Vector2(0, y), new Vector2(560, 30));
                MenuUi.Label(lab, rows[i], 22, MenuUi.Grape);
                var val = MenuUi.Rect("val" + i, shopPanel, new Vector2(.5f, 1), new Vector2(0, y - 34), new Vector2(360, 54));
                MenuUi.Panel(val, Color.white, new Color(.85f, .8f, .78f), 3f);
                shopValues[i] = MenuUi.Label(val, "", 32, MenuUi.Ink);
                MenuUi.Button(shopPanel, "<", new Vector2(.5f, 1), new Vector2(-230, y - 34), new Vector2(72, 58), MenuUi.Teal, () => Cycle(row, -1), 36);
                MenuUi.Button(shopPanel, ">", new Vector2(.5f, 1), new Vector2(230, y - 34), new Vector2(72, 58), MenuUi.Teal, () => Cycle(row, 1), 36);
            }
            MenuUi.Button(shopPanel, MenuFlow.Txt("LOOKING GOOD!", "หล่อแล้ว!"), new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(420, 80), MenuUi.Pink, () => SetShop(false), 38, 1f);
            shopPanel.gameObject.SetActive(false);
        }

        void RefreshUi()
        {
            if (!playersText) return;
            var sb = new System.Text.StringBuilder();
            sb.Append(MenuFlow.Txt("PLAYERS ", "ผู้เล่น ")).Append(Mathf.Max(1, net.Roster.Count)).Append('/').Append(net.Max).Append('\n');
            int n = 0;
            foreach (var p in net.Roster)
            {
                n++;
                string hex = ColorUtility.ToHtmlStringRGB(p.look.Tint);
                string fruit = Loc.Thai ? MenuFlow.FruitTh[p.look.fruit] : MenuFlow.FruitEn[p.look.fruit];
                sb.Append("<color=#").Append(hex).Append(">●</color> P").Append(p.number).Append("  ").Append(fruit);
                if (n == 1) sb.Append(MenuFlow.Txt("  <color=#FFD030>HOST</color>", "  <color=#FFD030>เจ้าของห้อง</color>"));
                if (p.id == net.LocalId) sb.Append(MenuFlow.Txt("  (you)", "  (คุณ)"));
                sb.Append('\n');
            }
            for (int i = n; i < net.Max; i++) sb.Append("<color=#776670>○  ").Append(MenuFlow.Txt("waiting for a friend...", "รอเพื่อน...")).Append("</color>\n");
            playersText.text = sb.ToString();

            var lv = MenuFlow.Levels[net.Level];
            levelText.text = MenuFlow.LevelName(net.Level);
            levelText.GetComponent<Outline>().effectColor = MenuUi.Ink;
            levelText.color = lv.color;
            levelBlurb.text = MenuFlow.LevelBlurb(net.Level) + (lv.networked ? "" : MenuFlow.Txt("\n(each player gets their own copy for now)", "\n(ตอนนี้ด่านนี้แต่ละคนเล่นแยกจอกัน)"));
            maxText.text = MenuFlow.Txt("Max players: ", "ผู้เล่นสูงสุด: ") + net.Max;
            bool host = net.IsHost;
            foreach (var b in new[] { levelPrev, levelNext, maxPrev, maxNext }) b.gameObject.SetActive(host);
            startBtn.gameObject.SetActive(host); waitText.gameObject.SetActive(!host);
            if (signText) signText.text = MenuFlow.Txt("NEXT UP:\n", "ด่านต่อไป:\n") + MenuFlow.LevelName(net.Level);
        }

        float uiRefreshT;
        void UpdateHud()
        {
            uiRefreshT -= Time.unscaledDeltaTime; if (uiRefreshT <= 0f) { uiRefreshT = .5f; RefreshUi(); }
            var ns = NetSession.I;
            bool online = net.Online;
            codeText.text = online && ns ? (string.IsNullOrEmpty(ns.JoinCode) ? "..." : ns.JoinCode) : MenuFlow.Txt("SOLO", "เล่นคนเดียว");
            copyBtn.gameObject.SetActive(online && ns && !string.IsNullOrEmpty(ns.JoinCode));
            statusText.text = !online ? MenuFlow.Txt("Offline lobby - just you and the ball.", "ล็อบบี้ออฟไลน์ - มีแค่คุณกับลูกบอล")
                : net.IsHost ? MenuFlow.Txt("Send the code to your friends (Join Room)", "ส่งรหัสให้เพื่อนกด Join Room")
                : MenuFlow.Txt("You're in! Mess around until the host starts.", "เข้ามาแล้ว! เล่นรอจนกว่าเจ้าของห้องจะเริ่ม");
            promptText.transform.parent.gameObject.SetActive(NearShop && !shopOpen && !counting);
            startBtn.Interactable = !counting;
            bigT = Mathf.MoveTowards(bigT, 0f, Time.unscaledDeltaTime * 1.2f);
            float pop = bigT > .8f ? Mathf.Lerp(1f, 1.6f, (bigT - .8f) / .2f) : 1f;
            bigRt.localScale = Vector3.one * pop;
            var col = bigText.color; col.a = Mathf.Clamp01(bigT * 2.5f); bigText.color = col;
        }
    }

    /// Gentle float for balloons.
    public class Bobber : MonoBehaviour
    {
        public float phase; Vector3 home;
        void Start() { home = transform.localPosition; }
        void Update() { transform.localPosition = home + Vector3.up * Mathf.Sin(Time.time * 1.3f + phase) * .18f; transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * .9f + phase) * 6f); }
    }

    /// Keeps a 3D label facing the camera.
    public class Billboard : MonoBehaviour
    {
        void LateUpdate() { var c = Camera.main; if (c) transform.rotation = Quaternion.LookRotation(transform.position - c.transform.position); }
    }
}
