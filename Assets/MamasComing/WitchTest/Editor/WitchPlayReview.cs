using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MamasComing.WitchTest.EditorTools
{
    // Stages locations to isolate input/physics regressions; not a human playthrough.
    public static class WitchPlayReview
    {
        static IEnumerator routine;
        static float waitLeft;
        static int lastFrame;
        static Keyboard keyboard;
        static Mouse mouse;
        static InputSettings.BackgroundBehavior background;
        static InputSettings.EditorInputBehaviorInPlayMode focus;
        static bool runBackground;
        static readonly List<string> lines = new List<string>(), errors = new List<string>();
        static int passed;
        const string Folder = "DevBridge/witch-test/";
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play the isolated witch scene first.");
            background = InputSystem.settings.backgroundBehavior; focus = InputSystem.settings.editorInputBehaviorInPlayMode;
            runBackground = Application.runInBackground;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            lines.Clear(); errors.Clear(); passed = 0;
            Application.logMessageReceivedThreaded += OnLog;
            routine = Checks(); waitLeft = 0; lastFrame = -1; EditorApplication.update += Tick;
        }
        static void Tick()
        {
            if (!EditorApplication.isPlaying) { Finish("ABORTED"); return; }
            EditorApplication.isPaused = false;
            if (Time.frameCount != lastFrame)
            {
                waitLeft -= Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime;
                lastFrame = Time.frameCount;
            }
            if (waitLeft > 0) return;
            try { if (!routine.MoveNext()) { Finish("PASSED"); return; } waitLeft = routine.Current is float t ? t : .1f; }
            catch (Exception e) { lines.Add(e.ToString()); Finish("FAILED"); }
        }
        static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); passed++; lines.Add("PASS " + message); }
        static void Key(params UnityEngine.InputSystem.Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        static void Click(bool on) => InputSystem.QueueStateEvent(mouse, new MouseState { buttons = (ushort)(on ? 1 : 0) });
        static void Shot(string name) => ScreenCapture.CaptureScreenshot(Folder + name + ".png");
        static void Aim(WitchLevel l, Vector3 point)
        {
            l.orbit.inputEnabled = false;
            for (int i = 0; i < 12; i++)
            {
                Vector3 pivot = l.motor.transform.position + Quaternion.Euler(0, l.orbit.yaw, 0) * l.orbit.shoulder;
                Vector3 boom = Quaternion.Euler(Mathf.Clamp(l.orbit.pitch, 12, 55), l.orbit.yaw, 0) * Vector3.forward;
                float length = l.orbit.distance;
                foreach (var hit in Physics.SphereCastAll(pivot, .018f, -boom, length, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(l.motor.transform) || hit.collider.GetComponentInParent<WitchProp>()) continue;
                    length = Mathf.Min(length, Mathf.Max(.12f, hit.distance - .02f));
                }
                Vector3 eye = pivot - boom * length;
                Vector3 d = point - eye;
                l.orbit.yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                l.orbit.pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            }
        }
        static void Stand(WitchLevel l, Vector3 point)
        {
            // Approach along the counter, rather than placing the apple beyond its front lip.
            l.motor.Teleport(new Vector3(point.x - .34f, .952f, point.z), Quaternion.Euler(0, 90, 0));
            Aim(l, point);
        }
        static IEnumerator Checks()
        {
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Repaint();
            var l = UnityEngine.Object.FindFirstObjectByType<WitchLevel>();
            yield return .5f;
            Assert(l && l.phase == WitchLevel.Phase.Title, "isolated scene starts at title");
            Shot("title-reviewed"); yield return .25f;
            Key(UnityEngine.InputSystem.Key.Enter); yield return .15f; Key(); yield return .3f;
            Assert(l.phase == WitchLevel.Phase.Playing && l.motor.InputEnabled, "Enter begins gameplay");
            l.orbit.inputEnabled = false; Shot("play-reviewed"); yield return .2f;
            Key(UnityEngine.InputSystem.Key.H); yield return .2f;
            Assert(GameObject.Find("SpellNotes"), "optional held H opens illustrated notes");
            Shot("notes-reviewed"); yield return .2f; Key(); yield return .2f;
            Assert(!GameObject.Find("SpellNotes"), "notes close when H released");
            Key(UnityEngine.InputSystem.Key.Escape); yield return .2f; Key(); yield return .2f;
            float remaining = l.TimeLeft; yield return .4f;
            Assert(l.Paused && Mathf.Approximately(remaining, l.TimeLeft), "pause freezes round timer");
            Key(UnityEngine.InputSystem.Key.Escape); yield return .2f; Key(); yield return .2f;
            Assert(!l.Paused, "Escape resumes");
            l.orbit.inputEnabled = false;
            var cup = l.props.First(p => p.id == "cup");
            Stand(l, cup.Center); yield return .4f;
            Assert(l.Aimed == cup, "centre-screen ray targets cup");
            Click(true); yield return .15f; Click(false); yield return .3f;
            Assert(l.Held == cup && cup.body.isKinematic, "normal left click levitates cup");
            float before = l.HoldDistance;
            InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0, 120) }); yield return .15f; Click(false); yield return .15f;
            Assert(l.HoldDistance > before, "normal wheel input increases hold distance");
            l.orbit.pitch = -10f; l.AdjustDistance(.12f); yield return 1.5f;
            Assert(cup.transform.position.y > 1.01f, "levitation rises off countertop: " + cup.transform.position);
            Shot("levitation-reviewed"); yield return .2f;
            // Separate sweep probe with a temporary physical blocker, removed before subsequent checks.
            Vector3 from = cup.body.position;
            var blocker = new GameObject("Review temporary blocker"); blocker.transform.position = cup.Center + Vector3.right * .20f;
            var box = blocker.AddComponent<BoxCollider>(); box.size = new Vector3(.025f, .7f, .7f); Physics.SyncTransforms();
            for (int i = 0; i < 10; i++) { cup.MoveHeld(from + Vector3.right * .6f, l.motor.transform, .02f); yield return .05f; }
            Assert(cup.body.position.x < blocker.transform.position.x, "swept held volume stops before solid blocker");
            UnityEngine.Object.Destroy(blocker); yield return .2f;
            // Wrong home should never increment progress, even on deliberate release.
            var wrong = l.homes.First(h => h.acceptsId == "book");
            Assert(!wrong.Accepts(cup, wrong.transform.position), "book receiver rejects cup");
            Click(true); yield return .15f; Click(false); yield return .3f;
            Assert(l.Held == null && !cup.Placed && l.TidyCount == 0, "left click outside receiver releases without auto-completion");
            foreach (var p in l.props)
            {
                p.ResetProp(); yield return .25f;
                Stand(l, p.Center); yield return .6f;
                Aim(l, p.Center); yield return .2f;
                Assert(l.Aimed == p, "centre-screen ray targets " + p.id);
                Click(true); yield return .15f; Click(false); yield return .2f;
                Assert(l.Held == p, "left click picks up " + p.id);
                // First raise above the rack/books; a low straight drag correctly collides with them.
                Vector3 overhead = p.Center + Vector3.up * .30f;
                Aim(l, overhead); yield return .2f;
                l.AdjustDistance(Vector3.Distance(l.orbit.transform.position, overhead) - l.HoldDistance);
                yield return 1.3f;
                // Stage the player's position, then let the regular held physics carry the object over the receiver.
                Vector3 landing = p.home.transform.position + Vector3.up * .055f;
                Vector3 high = landing + Vector3.up * (p.shape.center.y + .26f);
                Stand(l, high); yield return .6f;
                // A staged landing can put the apple on a book. Aim after the motor settles at that height.
                Aim(l, high); yield return .2f;
                l.AdjustDistance(Vector3.Distance(l.orbit.transform.position, high) - l.HoldDistance);
                yield return 3f;
                Aim(l, landing + Vector3.up * p.shape.center.y); yield return .2f;
                l.AdjustDistance(Vector3.Distance(l.orbit.transform.position, landing + Vector3.up * p.shape.center.y) - l.HoldDistance);
                yield return 3f;
                lines.Add(p.id + " placement actual=" + p.transform.position + " receiver=" + p.home.transform.position);
                Assert(l.ValidPlacement, "physical placement footprint reached for " + p.id + " actual=" + p.transform.position + " hold=" + l.HoldDistance + " camera=" + l.orbit.transform.position + " angles=" + l.orbit.transform.eulerAngles);
                Shot("placing-" + p.id); yield return .2f;
                Click(true); yield return .15f; Click(false); yield return .5f;
                Assert(p.Placed && !p.Held, "deliberate normal click settles " + p.id);
            }
            yield return 1f; Shot("mother-arrival-reviewed"); yield return 3f;
            Assert(l.phase == WitchLevel.Phase.Won && l.TidyCount == 3 && l.mother.gameObject.activeSelf, "three placements reveal human-scale mother and win");
            Shot("success-reviewed"); yield return .2f;
            Key(UnityEngine.InputSystem.Key.R); yield return .2f; Key(); yield return .3f;
            Assert(l.phase == WitchLevel.Phase.Playing && l.TidyCount == 0 && !l.mother.gameObject.activeSelf, "R resets all props and mother");
            l.roundSeconds = .25f; l.Restart(); yield return 4f;
            Assert(l.phase == WitchLevel.Phase.Lost, "timer expiry reveals mother and loss");
            Shot("timeout-reviewed"); yield return .2f;
            l.roundSeconds = 210; l.Restart(); l.orbit.inputEnabled = false;
            Assert(errors.Count == 0, "no game errors during review (known external connector auth failure excluded)");
        }
        static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (message.Contains("Server forcefully disconnected this plugin") && message.Contains("Authorization failed")) return;
            lock(errors) errors.Add(message + "\n" + trace);
        }
        static void Finish(string result)
        {
            EditorApplication.update -= Tick; Application.logMessageReceivedThreaded -= OnLog;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = focus; Application.runInBackground = runBackground;
            var l = UnityEngine.Object.FindFirstObjectByType<WitchLevel>(); if (l) { l.roundSeconds = 210; if (l.Paused) l.TogglePause(); l.Restart(); }
            lines.Insert(0, result + " checks=" + passed + " (staged input/physics review, not a continuous human playthrough)"); lines.AddRange(errors);
            File.WriteAllLines(Folder + "review-latest.txt", lines);
            Debug.Log("Witch review " + result + " checks=" + passed);
        }
    }
}
