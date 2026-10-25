using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

#if UNITY_EDITOR
namespace MamasComing.Dev
{
    /// Plays the running minigame for real, by feeding fake keyboard / mouse input into the Input System,
    /// so the whole path (input -> puzzle -> fix) is tested, not just "does the card open".
    /// exec MamasComing.Dev.MamaAutoSolve.Run   (start a minigame first)
    public class MamaAutoSolve : MonoBehaviour
    {
        public static void Run() { var go = new GameObject("AutoSolve"); go.AddComponent<MamaAutoSolve>(); }

        const System.Reflection.BindingFlags NP = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        static object Get(object o, string f) => o.GetType().GetField(f, NP)?.GetValue(o);
        static float GetF(object o, string f) => System.Convert.ToSingle(Get(o, f));

        InputSettings.EditorInputBehaviorInPlayMode prevEditor; InputSettings.BackgroundBehavior prevBg; bool changed;
        void OnDestroy() { if (InputSystem.settings != null && target) { InputSystem.settings.editorInputBehaviorInPlayMode = prevEditor; InputSystem.settings.backgroundBehavior = prevBg; } }
        float t; bool pressedLast; int lastKey = 1; Interactable target; MiniGame.Kind kind; int wiresPhase; Vector2 wireTo;

        void Start()
        {
            if (!MiniGame.I || !MiniGame.I.Active) { Debug.Log("[AutoSolve] no minigame running"); Destroy(gameObject); return; }
            // fake input must reach the game even when the Game view isn't the focused window
            prevEditor = InputSystem.settings.editorInputBehaviorInPlayMode; prevBg = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            target = MiniGame.I.Target; kind = MiniGame.I.Current;
            Debug.Log("[AutoSolve] playing " + kind + " on " + (target ? target.name : "?"));
        }

        void Key(params Key[] k) { if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(k)); }
        void Mouse(Vector2 screen, bool down)
        {
            var m = UnityEngine.InputSystem.Mouse.current; if (m == null) return;
            var s = new MouseState { position = screen }.WithButton(MouseButton.Left, down);
            InputSystem.QueueStateEvent(m, s);
        }

        void Update()
        {
            t += Time.deltaTime;
            if (!MiniGame.I || !MiniGame.I.Active)
            {
                Key();
                var hz = target as Hazard;
                Debug.Log("[AutoSolve] " + kind + " ended after " + t.ToString("F1") + "s (" + (1f / Mathf.Max(.001f, Time.smoothDeltaTime)).ToString("F0") + " fps)  target fixed=" + (hz ? hz.Fixed.ToString() : (target ? target.GetType().Name + " (not a hazard)" : "?")));
                Destroy(gameObject); return;
            }
            if (t > 40f) { Debug.Log("[AutoSolve] " + kind + " TIMEOUT - still running after 40s"); Key(); Destroy(gameObject); return; }
            // every press is one frame down, one frame up (so wasPressedThisFrame fires)
            // a press is held for 2+ frames, then released for 2+ frames (input events landing in the same update merge)
            phaseFrames++;
            if (taps > 0 && taps <= 4 && Keyboard.current != null) foreach (var kc in Keyboard.current.allKeys) if (kc.wasPressedThisFrame) Debug.Log("[AutoSolve] seen press " + kc.keyCode + " frame " + Time.frameCount);
            if (pressedLast && kind != MiniGame.Kind.Tug) { if (phaseFrames < 2) return; pressedLast = false; phaseFrames = 0; Key(); return; }
            if (!pressedLast && phaseFrames < 2 && kind != MiniGame.Kind.Tug && kind != MiniGame.Kind.Wires) return;
            var mg = MiniGame.I; var pz = Puzzles.I;
            dbgT += Time.unscaledDeltaTime;
            if (dbgT > 2f && pz && mg) { dbgT = 0f; Debug.Log("[AutoSolve] dbg mg.angle=" + GetF(mg, "angle") + "/" + GetF(mg, "goal") + " lastKey=" + GetF(mg, "lastKey") + " b=" + GetF(pz, "b") + " c=" + GetF(pz, "c") + " a=" + GetF(pz, "a") + " pressed=" + pressedLast + " kb=" + (Keyboard.current != null ? Keyboard.current.leftArrowKey.isPressed + "/" + Keyboard.current.rightArrowKey.isPressed + "/" + Keyboard.current.upArrowKey.isPressed + "/" + Keyboard.current.downArrowKey.isPressed : "null")); }
            switch (kind)
            {
                case MiniGame.Kind.Mash:
                case MiniGame.Kind.Bolts:
                    if (t < .3f) return;
                    lastKey = -lastKey; Tap(lastKey < 0 ? UnityEngine.InputSystem.Key.A : UnityEngine.InputSystem.Key.D); break;
                case MiniGame.Kind.Timing:
                    {
                        float ptr = Mathf.Sin(GetF(mg, "phase")) * mg.swingAmp;
                        if (Mathf.Abs(ptr) < mg.offWindow * .45f) Tap(UnityEngine.InputSystem.Key.E);
                        break;
                    }
                case MiniGame.Kind.Simon:
                    {
                        if (GetF(pz, "b") < .5f) return;
                        var seq = (System.Collections.Generic.List<int>)Get(pz, "seq"); int c = (int)GetF(pz, "c");
                        if (c < seq.Count) Tap(Arrow(seq[c]));
                        break;
                    }
                case MiniGame.Kind.Plunger:
                    if (Mathf.Abs(GetF(pz, "a") - 90f) < 12f) Tap(UnityEngine.InputSystem.Key.E); break;
                case MiniGame.Kind.Tug:
                    if (GetF(pz, "a") < .57f) Key(UnityEngine.InputSystem.Key.E); else Key(); break;
                case MiniGame.Kind.Blow:
                    Tap(UnityEngine.InputSystem.Key.E); break;
                case MiniGame.Kind.Glue:
                    {
                        int ans = (int)Get(pz, "glueAns");
                        if (t < .4f) return;
                        Tap(ans == 0 ? UnityEngine.InputSystem.Key.LeftArrow : ans == 1 ? UnityEngine.InputSystem.Key.UpArrow : UnityEngine.InputSystem.Key.RightArrow);
                        t = .1f; break;
                    }
                case MiniGame.Kind.Tape:
                    {
                        var seq = (System.Collections.Generic.List<int>)Get(pz, "seq"); int step = (int)GetF(pz, "step");
                        if (step < seq.Count) Tap(Arrow(seq[step]));
                        break;
                    }
                case MiniGame.Kind.Wires: if (phaseFrames >= 3) { phaseFrames = 0; WiresStep(); } break;
            }
        }

        void Tap(Key k) { Key(k); pressedLast = true; phaseFrames = 0; if (taps++ < 4) Debug.Log("[AutoSolve] tap " + k + " frame " + Time.frameCount); }
        int taps;
        int phaseFrames; float dbgT;
        static Key Arrow(int i) => i == 0 ? UnityEngine.InputSystem.Key.LeftArrow : i == 1 ? UnityEngine.InputSystem.Key.UpArrow : i == 2 ? UnityEngine.InputSystem.Key.RightArrow : UnityEngine.InputSystem.Key.DownArrow;

        // wires: press on the next unplugged left socket, drag to the socket of the same colour, release
        void WiresStep()
        {
            var pz = Puzzles.I; var area = (RectTransform)Get(pz, "area");
            var doneL = (bool[])Get(pz, "doneL"); var rightCols = (int[])Get(pz, "rightCols");
            float[] ys = { 105, 35, -35, -105 };
            Vector2 Scr(Vector2 local) => RectTransformUtility.WorldToScreenPoint(null, area.TransformPoint(local));
            if (doneL == null) return;
            int l = System.Array.IndexOf(doneL, false); if (l < 0) return;
            int r = System.Array.IndexOf(rightCols, l);
            switch (wiresPhase)
            {
                case 0: Mouse(Scr(new Vector2(-230, ys[l])), false); wiresPhase = 1; break;
                case 1: Mouse(Scr(new Vector2(-230, ys[l])), true); wiresPhase = 2; break;
                case 2: Mouse(Scr(new Vector2(230, ys[r])), true); wiresPhase = 3; break;
                case 3: Mouse(Scr(new Vector2(230, ys[r])), false); wiresPhase = 4; break;
                default: wiresPhase = 0; break;
            }
        }
    }
}

#endif
