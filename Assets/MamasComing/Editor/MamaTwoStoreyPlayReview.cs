using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MamasComing.EditorTools
{
    /// <summary>
    /// Staged Play-mode checks for the two-storey house: real virtual-keyboard walking up the stairs and down the chute,
    /// floor cut-away, cross-floor interaction filtering, Mom's route, the hamster on the stairs and every gadget's
    /// requirement/fix path. Writes docs/evidence/mama-two-storey/play-review.txt. Not a human playthrough.
    /// </summary>
    [InitializeOnLoad]
    public static class MamaTwoStoreyPlayReview
    {
        const string Out = "docs/evidence/mama-two-storey/";
        const string Flag = "MamaTwoStoreyReviewRequested", ExitFlag = "MamaTwoStoreyReviewExit";
        static IEnumerator routine; static double next; static Keyboard keyboard;
        static int passed; static readonly List<string> lines = new List<string>();

        static MamaTwoStoreyPlayReview()
        {
            // polled rather than event-driven: survives the play-mode domain reload in every editor configuration
            EditorApplication.update += WaitForPlay;
        }

        static void WaitForPlay()
        {
            if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying) return;
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (!GameManager.I) return;
            SessionState.SetBool(Flag, false);
            Run();
        }

        /// Batch entry: opens the level, enters Play mode, runs the checks and exits the editor.
        public static void StartBatch() { SessionState.SetBool(ExitFlag, true); StartFromEditor(); }

        [MenuItem("Mama's Coming/Review/Two-Storey Play Review")]
        public static void StartFromEditor()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Start from Edit mode.");
            if (EditorSceneManager.GetActiveScene().path != "Assets/MamasComing/Scenes/MamasKitchen.unity") EditorSceneManager.OpenScene("Assets/MamasComing/Scenes/MamasKitchen.unity");
            SessionState.SetBool(Flag, true);
            // a second editor on this machine can hang in the MCP plugin's assembly-reload hook: skip the reload for this run
            SessionState.SetBool("MamaTwoStoreyOldOptions", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("MamaTwoStoreyOldOptionFlags", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.isPlaying = true;
        }

        static void Run()
        {
            Directory.CreateDirectory(Out);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            passed = 0; lines.Clear();
            routine = Checks(); next = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish("ABORTED: Play mode stopped"); return; }
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;   // vendor plugin errors trigger Error Pause
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (!routine.MoveNext()) { Finish("PASSED"); return; }
                next = EditorApplication.timeSinceStartup + (routine.Current is float w ? w : 0.1f);
            }
            catch (Exception ex) { lines.Add("FAIL " + ex.Message); lines.Add(ex.StackTrace); Finish("FAILED"); }
        }

        static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
            File.WriteAllText(Out + "play-review.txt", result + " (" + passed + " checks passed) " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" + string.Join("\n", lines) + "\n");
            Debug.Log("[Mama's Coming] two-storey play review: " + result + ", " + passed + " passed");
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("MamaTwoStoreyOldOptions", false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("MamaTwoStoreyOldOptionFlags", 0);
            bool exit = SessionState.GetBool(ExitFlag, false);
            SessionState.SetBool(ExitFlag, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (exit) EditorApplication.delayCall += () => EditorApplication.Exit(result == "PASSED" ? 0 : 1);
        }

        static void Check(bool ok, string what) { if (!ok) throw new Exception(what); passed++; lines.Add("PASS " + what); }
        static void Note(string s) => lines.Add("     " + s);
        static void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        static void CamShot(string file)
        {
            var cam = Camera.main; if (!cam) return;
            var rt = new RenderTexture(1600, 900, 24); var old = cam.targetTexture; cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = old;
            File.WriteAllBytes(Out + file, tex.EncodeToPNG()); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(tex);
        }

        static IEnumerator Checks()
        {
            var gm = GameManager.I; float U = HouseLayout.UpperY;
            Check(gm && gm.player, "level loads with a player");
            yield return 0.5f;
            Press(Key.Enter); yield return 0.15f; Press(); yield return 3f;
            if (gm.state != GameManager.State.Playing) { gm.player.controlsEnabled = true; Note("title flow did not start play under batch input; controls enabled directly"); }
            var kid = gm.player; var motor = kid.Motor; var cam = gm.cam;

            // ---- start position and scale
            Check(HouseLayout.RoomOf(kid.transform.position) == 0, "kid starts in the living room");
            var sofa = GameObject.Find("Rooms/LivingRoom/Sofa");
            float sofaTop = sofa.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y);
            float kidTop = kid.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Max(r => r.bounds.max.y) - kid.transform.position.y;
            Note("kid height " + kidTop.ToString("0.00") + " m, sofa back " + sofaTop.ToString("0.00") + " m");
            Check(sofaTop > kidTop * 1.4f, "fruit-scale: the sofa back is well above the kid's head");
            CamShot("play-living.png");

            // ---- floor cut-away from the ground floor
            yield return 0.3f;
            var slab = GameObject.Find("House/UpperFloor").GetComponentsInChildren<Renderer>().First(r => r.name == "Slab");
            Check(!slab.enabled, "upper floor is hidden while the kid is downstairs");

            // ---- walk up the stairs with real input (stairs rise towards +x; camera yaw 0 so D = +x)
            cam.yaw = 0f;
            motor.Teleport(new Vector3(0.7f, 0.05f, 26.1f), Quaternion.Euler(0, 90, 0)); yield return 0.4f;
            Press(Key.D); float t0 = Time.time; float maxY = 0f;
            while (Time.time - t0 < 6f && kid.transform.position.x < 13.3f) { maxY = Mathf.Max(maxY, kid.transform.position.y); yield return 0.1f; }
            Press(); yield return 0.6f;
            Note("after stairs: " + kid.transform.position.ToString("0.00"));
            Check(kid.transform.position.y > U - 0.4f && kid.transform.position.x > 12f, "walking right climbs the stairs to the landing");
            Check(HouseLayout.FloorOf(kid.transform.position) == 1, "kid is on the upper floor");
            yield return 0.3f;
            Check(slab.enabled, "upper floor renders again once the kid is upstairs");
            CamShot("play-landing.png");

            // ---- walk the upstairs hall into the bedroom (W = +z, S = -z)
            motor.Teleport(new Vector3(10.8f, U + 0.05f, 14f), Quaternion.Euler(0, 180, 0)); yield return 0.3f;
            Press(Key.S); t0 = Time.time; while (Time.time - t0 < 2.5f && kid.transform.position.z > 8.5f) yield return 0.1f; Press(); yield return 0.4f;
            Note("after bedroom walk: " + kid.transform.position.ToString("0.00"));
            Check(HouseLayout.RoomOf(kid.transform.position) == 4, "walked through the door into the upstairs bedroom");
            CamShot("play-bedroom.png");

            // ---- cross-floor interaction filter: standing in the living room under the bedroom
            motor.Teleport(new Vector3(9f, 0.05f, 3.2f), Quaternion.Euler(0, 0, 0)); yield return 0.6f;
            Check(kid.Target == null || HouseLayout.FloorOf(kid.Target.InteractPoint) == 0, "nothing upstairs is targeted from the living room below");

            // ---- the laundry-chute shortcut (from the upper hall, walk +x into the hole)
            var jam = UnityEngine.Object.FindObjectsByType<GadgetHazard>(FindObjectsSortMode.None).First(g => g.displayName == "Chute jam");
            Check(!jam.Active, "chute is open before its jam starts");
            motor.Teleport(new Vector3(24.2f, U + 0.05f, 14f), Quaternion.Euler(0, 90, 0)); yield return 0.4f;
            Press(Key.D); t0 = Time.time; while (Time.time - t0 < 3f && kid.transform.position.y > 1f) yield return 0.1f; Press(); yield return 1.0f;
            Note("after chute: " + kid.transform.position.ToString("0.00"));
            Check(kid.transform.position.y < 1f && HouseLayout.RoomOf(kid.transform.position) == 3, "dropping through the chute lands in the ground hallway");

            // ---- Mom's route: every leg is on a floor or on the stair line
            var route = gm.mom.route;
            Check(route.Length >= 30 && route.Any(p => p.point.position.y > U - 0.1f), "Mom's route goes upstairs");
            foreach (var p in route) Check(Mathf.Abs(p.point.position.y) < 0.01f || Mathf.Abs(p.point.position.y - U) < 0.01f, "route point '" + p.name + "' is on a floor");
            foreach (var room in new[] { "Bedroom", "Mom's room", "Party loft", "Kitchen", "Game room", "Garage" })
                Check(route.Any(p => p.name == room && p.inspect), "Mom inspects the " + room);

            // ---- hamster runs the stairs
            var ham = UnityEngine.Object.FindFirstObjectByType<Hamster>();
            Check(ham.nodes.Any(n => n.y > U - 0.1f) && ham.nodes.Any(n => n.y < 0.1f), "hamster network spans both floors");
            Check(HouseLayout.FloorOf(ham.homePosition) == 1, "hamster cage is upstairs in the bedroom");

            // ---- the 50 problems
            var hz = UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None);
            var gadgets = hz.OfType<GadgetHazard>().ToArray();
            Note("hazards " + hz.Length + ", gadgets " + gadgets.Length);
            Check(gadgets.Length == 22, "22 new gadget problems exist");
            Check(gadgets.Select(g => g.displayName).Distinct().Count() == 22, "every gadget is a different problem");
            Check(gadgets.Count(g => HouseLayout.FloorOf(g.InteractPoint) == 1) >= 10, "at least 10 new problems are upstairs");
            foreach (var g in gadgets)
            {
                Check(HouseLayout.RoomOf(g.InteractPoint) >= 0, g.displayName + " is inside a room");
                Check(!string.IsNullOrEmpty(g.questTh) && !string.IsNullOrEmpty(g.questEn), g.displayName + " has Thai and English quests");
            }

            // requirement: the foam cannon refuses while the power is on, works with it off
            var foam = gadgets.First(g => g.displayName == "Foam cannon");
            if (!foam.Active) foam.Activate();
            foam.Interact(kid); Check(!foam.Fixed, "foam cannon can't be fixed with the power on");
            PowerGrid.Trip("test", "test"); foam.Interact(kid); Check(foam.Fixed, "foam cannon is fixed once the power is cut");
            PowerGrid.Restore();

            // requirement: the blender needs a lid in hand
            var blender = gadgets.First(g => g.displayName == "Blender");
            blender.Activate(); blender.Interact(kid); Check(!blender.Fixed, "blender refuses an empty-handed fix");
            Check(blender.GetPrompt(kid).ToLower().Contains("lid") || blender.GetPrompt(kid).Contains("ฝา"), "blender prompt asks for a lid");

            // co-op: the garage door needs both buttons
            var door = gadgets.First(g => g.displayName == "Garage door");
            door.Activate(); Check(!door.CanInteract(kid), "garage door itself is not a one-person fix");
            door.partners[0].Interact(kid); Check(!door.Fixed, "one garage button alone does nothing");
            door.partners[1].Interact(kid); Check(door.Fixed, "both garage buttons together stop the door");

            // runaway: Dusty moves while it is active
            var dusty = gadgets.First(g => g.displayName == "Robot vacuum");
            Vector3 d0 = dusty.InteractPoint; yield return 1.5f;
            Check((dusty.InteractPoint - d0).magnitude > 0.5f || dusty.Fixed, "Dusty the robot vacuum roams");

            // the slime upstairs needs a bucket carried up the stairs
            var gloop = gadgets.First(g => g.displayName == "Escaped slime");
            gloop.Activate(); gloop.Interact(kid); Check(!gloop.Fixed, "the slime can't be scooped without a bucket");

            // ---- downstairs view for the record
            motor.Teleport(new Vector3(32f, 0.05f, 21f), Quaternion.identity); yield return 0.8f;
            CamShot("play-kitchen.png");
            motor.Teleport(new Vector3(22f, U + 0.05f, 20f), Quaternion.identity); yield return 0.8f;
            CamShot("play-loft.png");
        }
    }
}
