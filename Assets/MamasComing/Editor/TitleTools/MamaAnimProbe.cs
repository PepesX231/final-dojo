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
            int k = int.Parse(id); KidLook.Selected = k; KidLook.Apply(pc, k);
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
