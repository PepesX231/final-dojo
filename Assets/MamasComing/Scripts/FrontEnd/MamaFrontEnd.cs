using UnityEngine;

namespace MamasComing
{
    /// Root of a front-end scene (MC_MainMenu or MC_Lobby): puts the scene camera on the authored shot,
    /// makes sure settings / guide / audio exist, and offers the shared scene-flow actions.
    [DefaultExecutionOrder(950)]
    public class MamaFrontEnd : MonoBehaviour
    {
        public static MamaFrontEnd I { get; private set; }
        public enum Screen { None, Title, Lobby }

        public Screen screen = Screen.Title;
        public MamaTitleScreen title;
        public MamaLobbyScreen lobby;
        [Tooltip("Seconds for the camera to settle on the shot when the scene opens.")]
        public float blendTime = 1.2f;
        [Tooltip("Camera starts this far behind the shot and glides in.")]
        public Vector3 introOffset = new Vector3(0f, 0.15f, -0.6f);

        public Screen Current { get; private set; } = Screen.None;
        public static bool Active => I && I.Current != Screen.None;

        Camera cam; MamaShot shot; float swayT;

        void Awake() { I = this; Loc.Load(); }
        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            NetSession.Ensure();
            MamaAudio.Ensure();
            MamaSettings.Ensure();
            Time.timeScale = 1f;
            cam = Camera.main;
            // the menu signs are real 3D objects: clicks reach them through physics
            if (cam && !cam.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>()) cam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            // one scene holds both: come back from a round (or while in a room) straight onto the sofa
            if (title && lobby) { screen = NetGame.Online || MamaLaunch.WantLobby ? Screen.Lobby : Screen.Title; MamaLaunch.WantLobby = false; }
            Current = screen;
            if (title) title.SetVisible(screen == Screen.Title);
            if (lobby) lobby.SetVisible(screen == Screen.Lobby);
            shot = screen == Screen.Title ? (title ? title.shot : null) : (lobby ? lobby.shot : null);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.renderMode == RenderMode.WorldSpace && !c.worldCamera) c.worldCamera = cam;
            if (cam && shot) { var s0 = shot.transform; cam.transform.SetPositionAndRotation(s0.TransformPoint(introOffset), s0.rotation); cam.fieldOfView = shot.fov; }
        }

        void LateUpdate()
        {
            if (!cam || !shot) return;
            float dt = Time.unscaledDeltaTime;
            // follow the selected item's shot (title) with a soft, slightly lazy camera
            var target = Current == Screen.Title && title ? title.CurrentShot : shot;
            if (!target) target = shot;
            float k = 1f - Mathf.Exp(-dt / Mathf.Max(0.05f, blendTime) * 3.5f);
            swayT += dt;
            var sway = Quaternion.Euler(Mathf.Sin(swayT * 0.31f) * target.sway * 0.5f, Mathf.Sin(swayT * 0.23f) * target.sway, 0f);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(cam.transform.position, target.transform.position, k), Quaternion.Slerp(cam.transform.rotation, target.transform.rotation * sway, k));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target.fov, k);
        }

        public void StartGame() => MamaLaunch.ToGame();

        /// Switch between the title signs and the character-select sofa: the camera glides over (no loading).
        public void Show(Screen s)
        {
            if (s == Current) return;
            Current = s; screen = s;
            if (title) title.SetVisible(s == Screen.Title);
            if (lobby) lobby.SetVisible(s == Screen.Lobby);
            shot = s == Screen.Title ? (title ? title.shot : shot) : (lobby ? lobby.shot : shot);
            MamaAudio.Play("whoosh", null, .5f, s == Screen.Lobby ? 1f : 1.2f);
        }

        /// Test / automation hook: select and press a menu item by action name (e.g. "Coop", "Play", "NextKid").
        public static void Press(string action)
        {
            if (!System.Enum.TryParse(action, out MamaMenuItem.Action a)) { Debug.LogWarning("[FrontEnd] cannot press " + action); return; }
            foreach (var it in FindObjectsByType<MamaMenuItem>(FindObjectsSortMode.None))
                if (it.action == a) { it.Hovered?.Invoke(it); it.Clicked?.Invoke(it); Debug.Log("[FrontEnd] pressed " + a); return; }
        }
        /// Test hook: hover (select) an item without pressing it.
        public static void Hover(string action)
        {
            if (!System.Enum.TryParse(action, out MamaMenuItem.Action a)) return;
            foreach (var it in FindObjectsByType<MamaMenuItem>(FindObjectsSortMode.None)) if (it.action == a) { it.Hovered?.Invoke(it); return; }
        }
        /// Test hook: camera numbers.
        public static void LogCamera()
        {
            var c = Camera.main; if (!c) return;
            Debug.Log("[FrontEnd] cam pos=" + c.transform.position + " rot=" + c.transform.eulerAngles + " fov=" + c.fieldOfView + " physical=" + c.usePhysicalProperties + " aspect=" + c.aspect);
        }
        public static bool OverlayOpen => MamaSettings.Open || MamaHowToPlay.Open;
    }
}
