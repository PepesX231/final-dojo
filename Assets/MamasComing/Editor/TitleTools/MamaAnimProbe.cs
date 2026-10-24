using UnityEngine;

namespace MamasComing.Editor
{
    /// Play-mode helpers for checking the fruit animation from DevBridge (no keyboard needed).
    public static class MamaAnimProbe
    {
        class Runner : MonoBehaviour
        {
            public PlayerController pc; public float t, dur = 3f, speed = 2.6f, slow = 0.36f; public bool punch, fired;
            void Update()
            {
                if (!pc) { Destroy(gameObject); return; }
                t += Time.deltaTime;
                if (punch) { if (!fired && pc.actions) { fired = true; pc.actions.Play(KidActionAnimator.Clip.Punch, slow); } if (t > slow + 0.5f) Destroy(gameObject); return; }
                var d = pc.transform.forward * speed * Time.deltaTime;
                if (pc.Motor) pc.Motor.Teleport(pc.transform.position + d, pc.transform.rotation); else pc.transform.position += d;
                if (t > dur) Destroy(gameObject);
            }
        }

        static PlayerController Kid() { var gm = GameManager.I; return gm ? gm.player : Object.FindFirstObjectByType<PlayerController>(); }

        public static void SetFruit(string id)
        {
            var pc = Kid(); if (!pc) return;
            int k = int.Parse(id); KidLook.Selected = k; KidLook.Apply(pc, k); pc.WakeUp();
            pc.transform.SetPositionAndRotation(new Vector3(12.2f, 0.02f, 3.0f), Quaternion.Euler(0, 270, 0));
            if (pc.Motor) pc.Motor.Teleport(pc.transform.position, pc.transform.rotation);
            Debug.Log("[AnimProbe] fruit " + k);
        }

        class FakeInput : DreamRush.Player.IPlayerCommandSource
        {
            public Vector2 move; public bool sprint;
            public DreamRush.Player.PlayerCommand ReadCommand() => new DreamRush.Player.PlayerCommand { Move = move, Sprint = sprint };
        }
        static DreamRush.Player.IPlayerCommandSource savedSource; static Transform savedCam;

        /// Drives the kid with fake stick input: "x,y,sprint" in world space (e.g. "0,1,0"); "stop" restores.
        public static void Drive(string arg)
        {
            var pc = Kid(); if (!pc || !pc.Motor) return;
            var m = pc.Motor;
            if (arg == "stop") { if (savedSource != null) m.CommandSource = savedSource; m.CameraTransform = savedCam; savedSource = null; return; }
            var a = arg.Split(',');
            if (savedSource == null) { savedSource = m.CommandSource; savedCam = m.CameraTransform; }
            m.CameraTransform = null; m.InputEnabled = true;
            m.CommandSource = new FakeInput { move = new Vector2(float.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture)), sprint = a.Length > 2 && a[2] == "1" };
            pc.controlsEnabled = true; pc.wetRunFallChance = 0f; pc.wetWalkFallChance = 0f; // probe only: no random slips mid-capture
        }

        public static void Walk(string seconds)
        {
            var pc = Kid(); if (!pc) return;
            var r = new GameObject("AnimProbe").AddComponent<Runner>(); r.pc = pc; r.dur = float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void Punch() => PunchSlow("0.36");
        public static void PunchSlow(string seconds)
        {
            var pc = Kid(); if (!pc) return;
            var r = new GameObject("AnimProbe").AddComponent<Runner>(); r.pc = pc; r.punch = true; r.slow = float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static void StartRound() { var gm = GameManager.I; if (!gm) return; gm.state = GameManager.State.Playing; gm.player.controlsEnabled = true; gm.player.WakeUp(); gm.ui.ShowTitle(false); }
        public static void ToggleNotes() { var gm = GameManager.I; var r = gm ? gm.GetComponent<MamaReadability>() : null; if (r) r.ToggleNotes(); }
        /// "Bucket|Chair": give the kid the first pickup whose name contains the first word, then stand next to the second.
        public static void HoldNear(string arg)
        {
            var pc = Kid(); if (!pc) return; var a = arg.Split('|');
            Pickup item = null; foreach (var p in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None)) if (p.name.Contains(a[0]) || p.displayName.Contains(a[0])) { item = p; break; }
            if (item) { if (pc.Held) pc.Drop(false); pc.PickUp(item); }
            Transform near = null; foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) if (t.name.Contains(a[1]) && t != item?.transform) { near = t; break; }
            if (near) { var pos = near.position + Vector3.right * .9f; pos.y = pc.transform.position.y; pc.Motor.Teleport(pos, Quaternion.LookRotation(near.position - pos, Vector3.up)); }
            Debug.Log("[AnimProbe] holding " + (pc.Held ? pc.Held.name : "nothing") + " near " + (near ? near.name : "?") + " target=" + (pc.Target ? pc.Target.name : "none"));
        }
        public static void LogCabinets() { var pc = Kid(); foreach (var c in Object.FindObjectsByType<Cabinet>(FindObjectsSortMode.None)) Debug.Log("[AnimProbe] cabinet " + c.name + " open=" + c.IsOpen + " d=" + Vector3.Distance(pc.transform.position, c.InteractPoint).ToString("F2") + " door=" + (c.doorPivot ? "y" : "n")); }
        public static void LogTarget() { var pc = Kid(); if (pc) Debug.Log("[AnimProbe] held=" + (pc.Held ? pc.Held.name : "-") + " target=" + (pc.Target ? pc.Target.name + " / " + pc.Target.GetPrompt(pc) : "none")); }
        static PlayerController dummy;
        /// Offline test partner: a copy of the kid that just stands there (no input).
        public static void SpawnDummy()
        {
            var pc = Kid(); if (!pc) return;
            var go = Object.Instantiate(pc.gameObject, pc.transform.position + pc.transform.forward * 1.0f, pc.transform.rotation * Quaternion.Euler(0, 180, 0));
            go.name = "Dummy kid"; dummy = go.GetComponent<PlayerController>(); dummy.isLocal = false;
            if (dummy.Motor) { dummy.Motor.InputEnabled = false; dummy.Motor.CameraTransform = null; }
            foreach (var cam in go.GetComponentsInChildren<Camera>()) Object.Destroy(cam.gameObject);
            foreach (var al in go.GetComponentsInChildren<AudioListener>()) Object.Destroy(al);
            KidLook.Apply(dummy, 2); dummy.WakeUp();
        }
        public static void PunchDummy() { var pc = Kid(); if (dummy) NetGame.Hit(dummy, dummy.transform.position - pc.transform.position, 3f, 2.5f, true, pc.transform.position); }
        public static void GrabDummy() { var pc = Kid(); if (dummy) NetGame.Hit(dummy, pc.transform.forward, KidCarry.GrabCode, 0f, false, pc.transform.position); }
        public static void ThrowDummy() { var pc = Kid(); if (dummy) NetGame.Hit(dummy, pc.transform.forward * 9f, KidCarry.ThrowCode, 0f, false, pc.transform.position); }
        public static void GoTo(string xz) { var pc = Kid(); var a = xz.Split(','); var p = new Vector3(float.Parse(a[0], System.Globalization.CultureInfo.InvariantCulture), pc.transform.position.y, float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture)); pc.Motor.Teleport(p, pc.transform.rotation); }
        public static void ListRoom(string idx)
        {
            int r = int.Parse(idx);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.parent && (t.parent.name == "Rooms" || t.parent.name == "Props" || t.parent.name == "Party" || t.parent.parent && t.parent.parent.name == "Rooms") && GameManager.RoomOf(t.position) == r)
                    Debug.Log("[AnimProbe] room" + r + " " + t.parent.name + "/" + t.name + " " + t.position.ToString("F1"));
        }
        public static void ActivateCoop() { CoopPuzzle.AllowSolo = true; foreach (var c in Object.FindObjectsByType<CoopPuzzle>(FindObjectsSortMode.None)) c.Activate(); }
        /// "unicorn|both" / "unicorn|solo" / "dance|both": the kid (and the dummy) work the stations for a few seconds.
        public static void CoopTest(string arg) { var go = new GameObject("CoopTest"); var r = go.AddComponent<CoopRunner>(); var a = arg.Split('|'); r.which = a[0]; r.both = a.Length < 2 || a[1] == "both"; }
        class CoopRunner : MonoBehaviour
        {
            public string which; public bool both; float t;
            CoopPuzzle pz;
            void Start()
            {
                foreach (var c in Object.FindObjectsByType<CoopPuzzle>(FindObjectsSortMode.None)) if (which == "dance" ? c is DanceOffHazard : c is UnicornHazard) pz = c;
                var pc = Kid(); if (!pz || !pc) { Destroy(gameObject); return; }
                Place(pc, pz.stations[0]); if (dummy && both) Place(dummy, pz.stations[1]);
            }
            static void Place(PlayerController p, CoopStation s) { var pos = s.transform.position; pos.y = p.transform.position.y; var dir = (s.transform.parent.position - pos); dir.y = 0; p.Motor.Teleport(pos - dir.normalized * .3f, Quaternion.LookRotation(dir.sqrMagnitude > .01f ? dir : Vector3.forward)); }
            void Update()
            {
                t += Time.deltaTime; if (!pz) { Destroy(gameObject); return; }
                var pc = Kid();
                if (pz.holdMode) { pz.Holding(pz.stations[0], pc); if (both && dummy) pz.Holding(pz.stations[1], dummy); }
                else
                {
                    float beat = 60f / ((DanceOffHazard)pz).bpm; float ph = Time.time % beat;
                    if (ph < Time.deltaTime) { pz.Pressed(pz.stations[0], pc); if (both && dummy) pz.Pressed(pz.stations[1], dummy); }
                }
                if (t > 7f || pz.Fixed) { Debug.Log("[AnimProbe] coop " + which + " fixed=" + pz.Fixed); Destroy(gameObject); }
            }
        }
        public static void LogSofa() { var g = GameObject.Find("Floating sofa (character select)"); if (!g) return; foreach (var r in g.GetComponentsInChildren<Renderer>()) if (!r.GetComponentInParent<MamaPosedKid>()) Debug.Log("[AnimProbe] sofa part " + r.transform.parent.name + "/" + r.name + " c=" + r.bounds.center.ToString("F1")); }
        public static void LogKids() { foreach (var k in Object.FindObjectsByType<MamaPosedKid>(FindObjectsSortMode.None)) Debug.Log("[AnimProbe] kid " + k.name + " pos=" + k.transform.position.ToString("F2") + " rot=" + k.transform.eulerAngles.ToString("F0") + " floaty=" + (k.GetComponent<MamaFloaty>() != null) + " parent=" + k.transform.parent.name); }
        public static void ResetFruit() { KidLook.Selected = KidLook.Apple; }
        public static void ListIsland()
        {
            var isl = GameObject.Find("Floating island (living room furniture)"); if (!isl) return;
            foreach (var r in isl.GetComponentsInChildren<Renderer>()) { var t = r.transform; string path = t.name; var p = t.parent; for (int i = 0; i < 3 && p; i++) { path = p.name + "/" + path; p = p.parent; } Debug.Log("[AnimProbe] isl " + path + " c=" + r.bounds.center.ToString("F1") + " s=" + r.bounds.size.ToString("F1")); }
        }
        public static void LogFeet()
        {
            var pc = Kid(); if (!pc) return; var rig = pc.GetComponentInChildren<DreamRush.Player.AppleProceduralAnimator>()?.transform; if (!rig) return;
            Debug.Log("[AnimProbe] grounded=" + (pc.Motor ? pc.Motor.IsGrounded.ToString() : "-"));
            foreach (var t in pc.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Foot_") || t.name.StartsWith("Leg_") || t.name.StartsWith("Arm_"))
                {
                    var q = Quaternion.Inverse(rig.rotation) * t.rotation;
                    Debug.Log("[AnimProbe] " + t.name + " lp=" + rig.InverseTransformPoint(t.position).ToString("F3") + " rel=" + q.eulerAngles.ToString("F0") + " fwd=" + (q * Vector3.forward).ToString("F2") + " up=" + (q * Vector3.up).ToString("F2") + " right=" + (q * Vector3.right).ToString("F2"));
                }
        }

        public static void LogRig()
        {
            var pc = Kid(); if (!pc) return;
            Debug.Log("[AnimProbe] pc pos=" + pc.transform.position.ToString("F2") + " stun=" + pc.Stunned + " ctl=" + pc.controlsEnabled + " sprint=" + pc.Sprinting + " input=" + (pc.Motor ? pc.Motor.InputEnabled.ToString() : "-") + " vel=" + (pc.Motor ? pc.Motor.PlanarVelocity.ToString("F2") : "-"));
            foreach (var a in pc.GetComponentsInChildren<DreamRush.Player.AppleProceduralAnimator>(true))
                Debug.Log("[AnimProbe] apa " + a.transform.parent.name + "/" + a.name + " en=" + a.isActiveAndEnabled + " motor=" + (a.motor ? "ok" : "null") + " pc.anim=" + ((Object)pc.anim == a) + " worldRot=" + a.transform.eulerAngles);
            foreach (var t in pc.GetComponentsInChildren<Transform>())
                if (t.name == "Body" || t.name.StartsWith("Arm_") || t.name.StartsWith("Leg_")) Debug.Log("[AnimProbe]  " + t.name + " p=" + t.position.ToString("F2") + " r=" + t.localEulerAngles.ToString("F0"));
        }

        /// Puts a close camera on the kid (side view) for screenshots.
        public static void CloseCam(string side)
        {
            var pc = Kid(); var cam = Camera.main; if (!pc || !cam) return;
            var tpc = cam.GetComponent<ThirdPersonCamera>(); if (tpc) tpc.enabled = false;
            Vector3 dir = side == "front" ? pc.transform.forward : pc.transform.right;
            var runner = cam.gameObject.GetComponent<Follow>(); if (!runner) runner = cam.gameObject.AddComponent<Follow>();
            runner.pc = pc; runner.dir = dir; runner.side = side != "front";
        }
        class Follow : MonoBehaviour
        {
            public PlayerController pc; public Vector3 dir; public bool side = true;
            Vector3 last; void LateUpdate() { if (!pc) return; var c = pc.transform.position + Vector3.up * 0.55f;
                var v = pc.Motor ? pc.Motor.PlanarVelocity : Vector3.zero; if (v.sqrMagnitude > 0.04f) last = v.normalized; if (last == Vector3.zero) last = pc.transform.forward;
                var d = side ? Vector3.Cross(Vector3.up, last).normalized : last; transform.position = c + d * 2.0f + Vector3.up * 0.15f; transform.LookAt(c); GetComponent<Camera>().fieldOfView = 40f; }
        }
    }
}
