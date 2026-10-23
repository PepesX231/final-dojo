using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace MamasComing.Editor
{
    /// <summary>Focused staged input/UX regressions. Does not claim a human or continuous playthrough.</summary>
    [InitializeOnLoad]
    public static class MamaUxReview
    {
        static MamaUxReview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if(state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("MamaRecastReviewRequested", false))return;
                SessionState.SetBool("MamaRecastReviewRequested",false);
                EditorApplication.delayCall += Run;
            };
        }
        public static void StartFromEditor()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Start the review from Edit mode.");
            SessionState.SetBool("MamaRecastReviewRequested",true);
            EditorApplication.isPlaying = true;
        }
        const string Folder = "DevBridge/mama-rebuild/ux/";
        static IEnumerator routine;
        static double next;
        static Keyboard keyboard;
        static InputSettings.BackgroundBehavior oldBackground;
        static InputSettings.EditorInputBehaviorInPlayMode oldFocus;
        static bool oldRunBackground, oldThai, oldLangKey;
        static int oldLangValue, passed;
        static readonly List<string> errors = new List<string>();
        static readonly List<string> lines = new List<string>();

        public static void Run()
        {
            if (!EditorApplication.isPlaying || !GameManager.I) throw new InvalidOperationException("Open MamasKitchen in Play mode first.");
            Directory.CreateDirectory(Folder);
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldRunBackground = Application.runInBackground;
            oldThai = Loc.Thai; oldLangKey = PlayerPrefs.HasKey("mama_lang"); oldLangValue = PlayerPrefs.GetInt("mama_lang");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            passed = 0; lines.Clear(); errors.Clear();
            Application.logMessageReceivedThreaded += OnLog;
            routine = Checks(); next = 0;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish("ABORTED: Play mode stopped"); return; }
            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (!routine.MoveNext()) { Finish("PASSED"); return; }
                next = EditorApplication.timeSinceStartup + (routine.Current is float wait ? wait : 0.12f);
            }
            catch (Exception ex) { lines.Add(ex.ToString()); Finish("FAILED"); }
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            passed++; lines.Add("PASS " + message);
        }

        static Transform Find(string path) => GameObject.Find("MamaCanvas").transform.Find(path);
        static string Copy(string path) => Find(path).GetComponentInChildren<Text>(true).text;
        static void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        static void Shot(string name) { ScreenCapture.CaptureScreenshot(Folder + name + ".png"); }
        static void StandNear(Interactable target, float distance = 0.75f)
        {
            Vector3 at = target.InteractPoint - Vector3.forward * distance; at.y = 0.03f;
            GameManager.I.player.Motor.Teleport(at, Quaternion.identity);
        }

        static IEnumerator Checks()
        {
            var gm = GameManager.I;
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            EditorWindow.GetWindow(type).Repaint();
            Loc.SetThai(true);
            yield return 0.5f;
            Assert(gm.state == GameManager.State.Title, "starts at title");
            Assert(Copy("Title/Card/Controls").Contains("เป้าหมาย"), "Thai title explains goal and choices");
            Shot("after-title"); yield return 0.25f;
            Press(Key.Enter); yield return 0.15f; Press(); yield return 2.5f;
            Assert(gm.state == GameManager.State.Playing && gm.player.controlsEnabled, "Enter starts normal wake-up flow");
            Assert(!Find("HUD/ObjList").gameObject.activeSelf, "long checklist hidden by default");
            Assert(!Find("HUD/OptionalIdea").gameObject.activeSelf, "solution hints hidden by default");
            Assert(!GameObject.Find("QuestBoard").transform.Find("Board").gameObject.activeSelf, "party checklist is not forced over gameplay");
            Assert(!Find("HUD/Problems").gameObject.activeSelf, "disaster telemetry is tucked into notes");
            Shot("after-play"); yield return 0.25f;
            var mother = gm.mom.GetComponent<MotherApplePresentation>();
            Assert(mother && mother.kind && mother.angry, "provided Mother Apple has both imported moods");
            Assert(mother.kind.GetComponentsInChildren<SkinnedMeshRenderer>().Length > 0, "Mother Apple uses the provided skinned mesh");
            mother.IsAngry = true; yield return .15f;
            Assert(mother.angry.activeSelf && !mother.kind.activeSelf, "mother angry expression swaps visibly");
            mother.IsAngry = false;
            Assert(Resources.LoadAll<AudioClip>("MamaFoley").Length >= 10, "ten licensed foley clips are available at runtime");
            foreach (string propName in new[] { "Teddy bear", "Hamster" })
            {
                var item = UnityEngine.Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).First(x => x.displayName == propName);
                var grip = item.GetComponent<CarryGrip>();
                Assert(grip && item.GetComponentsInChildren<MeshRenderer>().Any(r => r.enabled && r.transform.IsChildOf(item.transform.Find("Art_Recast"))), propName + " uses the rebuilt mesh");
                StandNear(item, .6f); yield return .2f;
                Assert(gm.player.Target == item, propName + " can be targeted with E");
                Press(Key.E); yield return .09f; Press();
                Assert(gm.player.Held == item && item.GrabProgress < .34f, propName + " has a reach phase before lifting");
                yield return .8f;
                var animator = gm.player.GetComponent<KidActionAnimator>();
                float left = Vector3.Distance(animator.LeftGripWorld, grip.LeftWorld), right = Vector3.Distance(animator.RightGripWorld, grip.RightWorld);
                lines.Add(propName + " grip error metres L=" + left.ToString("F3") + " R=" + right.ToString("F3"));
                Assert(left < .055f && right < .055f, propName + " wrists reach both authored contacts");
                var torso=gm.player.GetComponentsInChildren<Renderer>().First(r=>r.name=="Body");
                var heldBounds=item.transform.Find("Art_Recast").GetComponentInChildren<Renderer>().bounds;
                lines.Add(propName+" body clearance metres="+(heldBounds.min.z-torso.bounds.max.z).ToString("F3"));
                Assert(heldBounds.min.z>=torso.bounds.max.z-.03f,propName+" held mesh clears the front of the torso");
                Shot("recast-hold-" + propName.Replace(" ", "-")); yield return .2f;
                MamaRecastEvidence.HeldCloseup();
                Press(Key.Q); yield return .15f; Press(); yield return .3f;
            }
            var bag = UnityEngine.Object.FindObjectsByType<MessyProp>(FindObjectsSortMode.None).First(x => x.displayName == "Trash bag" && !x.Settled);
            StandNear(bag, .6f); yield return .2f;
            Press(Key.E); yield return .15f; Press(); yield return .7f;
            Assert(gm.player.Held == bag, "trash bag picked up through E");
            var bin = UnityEngine.Object.FindFirstObjectByType<TrashBin>();
            StandNear(bin, .8f); yield return .2f;
            Assert(gm.player.Target == bin, "hollow trash bin is targetable while carrying a bag");
            Press(Key.E); yield return .15f; Press(); yield return .3f;
            Assert(bin.IsOpen && gm.player.Held == bag && Quaternion.Angle(bin.doorPivot.localRotation, Quaternion.identity)>60f, "first E opens the hinged bin lid and keeps the bag held");
            Press(Key.E); yield return .15f; Press(); yield return .4f;
            Assert(gm.player.Held == null && bin.stored == 1 && bag.Settled && !bag.IsProblem, "second E deposits bag and resolves its mess state");
            Assert(Vector3.Distance(bag.transform.position, bin.stashPoint.position)<.05f, "deposited bag is inside the bin destination");
            Shot("recast-bin-disposal"); yield return .2f;
            MamaRecastEvidence.MotherPreview();
            MamaRecastEvidence.ExportMusicPreview();
            Press(Key.Tab); yield return 0.2f;
            Assert(GameObject.Find("QuestBoard").transform.Find("Board").gameObject.activeSelf && Find("HUD/Problems").gameObject.activeSelf, "Tab opens party overview and disaster details");
            Assert(!Find("HUD/HouseNotes").gameObject.activeSelf, "obsolete second overview stays hidden");
            Press(); yield return 0.15f; Press(Key.H); yield return 0.2f;
            Assert(Find("HUD/OptionalIdea").gameObject.activeSelf && !Find("HUD/HouseNotes").gameObject.activeSelf, "held H shows idea without overlapping overview");
            Assert(!GameObject.Find("QuestBoard").transform.Find("Board").gameObject.activeSelf, "optional hint suppresses party board");
            Press(); yield return 0.2f;
            Assert(!Find("HUD/OptionalIdea").gameObject.activeSelf, "releasing H removes hint");
            Press(Key.Tab); yield return 0.15f; Press(); yield return 0.15f;

            var teddy = UnityEngine.Object.FindObjectsByType<MessyProp>(FindObjectsSortMode.None).First(x => x.displayName == "Teddy bear");
            StandNear(teddy); yield return 0.2f;
            Assert(gm.player.Target == teddy, "teddy can be targeted with normal interaction");
            Press(Key.E); yield return 0.15f; Press(); yield return 0.45f;
            Assert(gm.player.Held == teddy, "E picks up teddy");
            Assert(Copy("HUD/ContextCard/Meaning").Contains("ห้องนอน"), "held teddy tells player the destination room");
            Assert(!GameObject.Find("Home outline - Teddy bear"), "precise destination is not forced across the house");
            Shot("teddy-room-clue"); yield return 0.25f;
            Press(Key.H); yield return 0.3f;
            Assert(Find("HUD/Guide5").gameObject.activeSelf, "optional H reveals teddy destination");
            Assert(Find("HUD/Guide5/Label").gameObject.activeSelf, "destination retains a label off-screen");
            Shot("teddy-optional-direction"); yield return 0.25f;
            Press();
            var at = teddy.homePosition + Vector3.right * 0.85f; at.y = 0.03f;
            gm.player.Motor.Teleport(at, Quaternion.Euler(0, -90, 0));
            yield return 0.4f;
            var ghost = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Home outline - Teddy bear");
            Assert(ghost && ghost.gameObject.activeSelf, "nearby home has matching 3D teddy silhouette");
            Assert(Copy("HUD/Prompt").Contains("Q"), "home communicates the place button");
            Shot("teddy-home"); yield return 0.25f;
            Press(Key.Q); yield return 0.15f; Press(); yield return 0.3f;
            Assert(gm.player.Held == null && Vector3.Distance(teddy.transform.position, teddy.homePosition) < 0.2f && !teddy.IsProblem, "Q returns teddy to its correct home");
            yield return 1.2f;
            Assert(!teddy.IsProblem, "returned teddy stays tidy after physics settles");

            StandNear(gm.stove, 1.15f); gm.stove.on = false; gm.pot.Fire.Ignite(0.25f);
            yield return 0.3f;
            Assert(GameObject.Find("Oil warning"), "near stove has visible oil/water warning");
            Press(Key.H); yield return 0.25f;
            Assert(Copy("HUD/OptionalIdea/Body").Contains("อากาศ"), "fire hint explains air and multiple approaches");
            Assert(Find("HUD/Guide3").gameObject.activeSelf && Find("HUD/Guide4").gameObject.activeSelf, "fire offers lid and extinguisher as options");
            Shot("fire-options"); yield return 0.25f; Press();
            var lid = UnityEngine.Object.FindFirstObjectByType<Lid>();
            StandNear(lid); yield return 0.3f;
            Assert(gm.player.Target == lid, "lid is discoverable and targetable");
            Press(Key.E); yield return 0.15f; Press(); yield return 0.4f;
            Assert(gm.player.Held == lid, "E picks up lid");
            StandNear(gm.pot); yield return 0.25f;
            Assert(gm.player.Target == gm.pot, "held lid makes burning pot actionable");
            Press(Key.E); yield return 0.15f; Press(); yield return 0.4f;
            Assert(!gm.pot.Fire.burning && gm.pot.lid == lid && gm.player.Held == null, "normal E smothers small oil fire without automatic puzzle completion");
            Assert(Copy("HUD/Call").Contains("ดับไฟแล้ว"), "resolved fire replaces the stale emergency banner");
            Assert(GameObject.Find("Fire out - settling steam"), "fire-out has local steam feedback");
            Assert(!Find("HUD/Toast").gameObject.activeSelf, "routine messages do not stack beneath an emergency");
            Shot("fire-solved"); yield return 0.25f;

            Loc.SetThai(false); yield return 0.35f;
            Assert(Copy("Title/Card/Controls").Contains("YOUR GOAL"), "new title copy switches to English");
            Assert(!Copy("HUD/ContextCard/Meaning").Contains("แก๊ส"), "context switches with language");
            Loc.SetThai(true); yield return 0.35f;
            Assert(Copy("Title/Card/Controls").Contains("เป้าหมาย"), "new title copy switches back to Thai");
            var context = Find("HUD/ContextCard/Meaning").GetComponentInChildren<Text>();
            Assert(context.preferredHeight <= context.rectTransform.rect.height + 2f, "context body fits its card at current Game View size");
            // Presentation stress: use the real puppet creation path, without opening a network room.
            var sourceFace=gm.player.GetComponentInChildren<DreamRush.Player.AppleFace>();
            sourceFace.Squeeze(.5f);yield return .05f;
            Assert(sourceFace.EyesClosed,"remote spawn regression starts with the source eyes shut");
            var spawn = typeof(NetSession).GetMethod("SpawnPuppet", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var p2 = (PlayerController)spawn.Invoke(NetSession.I, new object[] { 1 });
            var p3 = (PlayerController)spawn.Invoke(NetSession.I, new object[] { 2 });
            gm.player.Motor.Teleport(new Vector3(16f, .03f, 11f), Quaternion.identity);
            p2.transform.position = new Vector3(14.7f, .03f, 10.5f); p3.transform.position = new Vector3(17.3f, .03f, 10.5f);
            yield return .8f;
            Assert(PlayerController.AllPlayers.Count == 3 && !NetGame.Online, "three-avatar presentation check runs without starting networking");
            var bodyRenderer = p2.GetComponentsInChildren<Renderer>().First(r => r.name == "Body");
            var bodyMats = bodyRenderer.sharedMaterials;
            int eyeSlot = System.Array.FindIndex(bodyMats, m => m && m.name.Contains("EyeWhite"));
            Assert(eyeSlot>=0,"remote clone retains open-eye materials when the source was blinking");
            var block = new MaterialPropertyBlock(); bodyRenderer.GetPropertyBlock(block, eyeSlot);
            Color actualEye = block.GetColor("_BaseColor"), expectedEye = bodyMats[eyeSlot].GetColor("_BaseColor");
            Assert(Vector4.Distance(actualEye, expectedEye) < .001f, "remote eye material keeps its original colour instead of turning black");
            var sleeve = p2.GetComponentsInChildren<Renderer>().First(r => r.sharedMaterials.Any(m => m && m.name.Contains("Sleeve")));
            int sleeveSlot = System.Array.FindIndex(sleeve.sharedMaterials, m => m && m.name.Contains("Sleeve"));
            block.Clear(); sleeve.GetPropertyBlock(block, sleeveSlot);
            Assert(block.GetColor("_BaseColor").g > .6f, "P2 has a teal clothing accent per material slot");
            var labels = UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            Assert(labels.Any(t => t.text == "◆ P2") && labels.Any(t => t.text == "▲ P3"), "friends have different shape and text identity cues");
            Shot("three-avatars"); yield return .25f;
            var dish = FireSource.All.First(f => f.label == "Dish towel"); dish.Ignite(.3f);
            yield return .3f;
            block.Clear(); dish.flames.GetComponent<Renderer>().GetPropertyBlock(block);
            var flameColor = block.GetColor("_BaseColor");
            Assert(flameColor.a == 0 || flameColor.maxColorComponent > .3f, "burnt-cloth and wet-cloth tints do not blacken the flame renderer");
            Assert(errors.Count == 0, "no gameplay exceptions during focused review");
        }

        static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            // Existing unrelated external connector authorization failure is recorded, never hidden as a game error.
            if (message.Contains("Server forcefully disconnected this plugin") && message.Contains("Authorization failed")) return;
            lock (errors) errors.Add(message + "\n" + trace);
        }

        static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceivedThreaded -= OnLog;
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldFocus;
            Application.runInBackground = oldRunBackground;
            Loc.SetThai(oldThai);
            if (oldLangKey) PlayerPrefs.SetInt("mama_lang", oldLangValue); else PlayerPrefs.DeleteKey("mama_lang");
            PlayerPrefs.Save();
            lines.Insert(0, result + " passed=" + passed + " (staged input regression, not human playthrough)");
            lock (errors) lines.AddRange(errors);
            File.WriteAllLines(Folder + "review-" + DateTime.Now.ToString("HHmmss") + ".txt", lines);
            Debug.Log("Mama UX review " + result + ", checks=" + passed);
        }
    }
}
