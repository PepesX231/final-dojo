using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing
{
    /// <summary>
    /// Skill checks for shutting things off, drawn as the real object (tap wheel / gas knob).
    /// Valve: crank the stiff wheel by alternating [A] and [D]; water pressure keeps turning it back,
    ///        pressing the same key twice slips.
    /// Gas:   the knob pointer swings faster and faster - press [E] exactly as it passes OFF, 3 times.
    /// </summary>
    public class MiniGame : MonoBehaviour
    {
        public static MiniGame I { get; private set; }
        public enum Kind { None, Timing, Mash, Wires, Simon, Plunger, Bolts, Tug, Blow, Glue, Tape }   // Timing = gas knob, Mash = valve wheel (names kept for compatibility)

        public MamaUI ui;
        [Header("Valve")]
        public float degPerPress = 30f;
        public float slipDeg = 25f;
        public float pressureDegPerSec = 30f;
        public float tapGoal = 540f;
        public float mainValveGoal = 720f;
        [Header("Gas knob")]
        public float swingAmp = 150f;
        public float startSpeed = 1.25f;
        public float speedPerHit = 0.3f;
        public float offWindow = 17f;
        public int hitsNeeded = 3;

        public bool Active { get; private set; }
        public Kind Current { get; private set; }
        public Interactable Target { get; private set; }

        PlayerController player;
        string title;
        float angle, goal, phase, speed, flash, startFrame;
        int hits, lastKey;   // lastKey: -1 = A, 1 = D, 0 = none
        bool flashGood;

        void Awake() { I = this; if (!ui) ui = GetComponent<MamaUI>(); }
        void OnDestroy() { if (I == this) I = null; }

        public void Begin(Kind k, Interactable target, PlayerController p, string label)
        {
            if (k == Kind.None || Active) return;
            Active = true; Current = k; Target = target; player = p; title = label;
            angle = 0f; hits = 0; lastKey = 0; flash = 0f; phase = Random.Range(0f, 6.28f); speed = startSpeed;
            goal = target is MainValve ? mainValveGoal : tapGoal;
            startFrame = Time.frameCount;
            if ((int)k > (int)Kind.Mash) { Puzzles.Ensure(); Puzzles.I.Begin(k, label); return; }
            MamaAudio.Play(k == Kind.Timing ? "click" : "creak", target.transform.position, 0.7f);
        }

        public void Cancel()
        {
            if ((int)Current > (int)Kind.Mash && Puzzles.I) Puzzles.I.End();
            Active = false; Current = Kind.None;
            if (ui) ui.SetMini2(false, null, Kind.None, 0, 0, 0, 0, 0, 0, false);
        }

        void Update()
        {
            if (!Active) return;
            float dt = Time.deltaTime;
            if (!player || !Target || player.Stunned || (GameManager.I && !GameManager.I.Live)) { Cancel(); return; }
            Vector3 d = Target.InteractPoint - player.transform.position; d.y = 0f;
            if (d.magnitude > Target.interactRadius + 1f) { Cancel(); return; }
            if (MamaInput.DropDown || MamaInput.EscDown) { Cancel(); return; }
            bool fresh = Time.frameCount > startFrame;
            if (flash > 0f) flash -= dt;
            if ((int)Current > (int)Kind.Mash)
            {
                int res = Puzzles.I ? Puzzles.I.Tick(dt, fresh) : 1;
                if (player.actions) player.actions.Hold(KidActionAnimator.Clip.Twist);
                if (res > 0) Succeed();
                else if (res < 0) { var tg = Target; var pl = player; Cancel(); MamaAudio.Play("bad", null, 0.8f); if (GameManager.I) GameManager.I.KidSay(Loc.Thai ? "ไม่ทัน! ลองใหม่!" : "Argh! Try again!"); if (tg is BreakerPanel && pl) pl.KnockBack(tg.transform.position, 2.5f); }
                return;
            }
            var k = Keyboard.current; var g = Gamepad.current;

            if (Current == Kind.Mash)
            {
                bool a = fresh && ((k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame)) || (g != null && g.leftShoulder.wasPressedThisFrame));
                bool dd = fresh && ((k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame)) || (g != null && g.rightShoulder.wasPressedThisFrame));
                int key = a ? -1 : (dd ? 1 : 0);
                if (key != 0)
                {
                    if (key != lastKey) { angle += degPerPress; flash = 0.12f; flashGood = true; MamaAudio.Play("squeak", Target.transform.position, 0.45f, 0.85f + angle / goal * 0.5f); }
                    else { angle = Mathf.Max(0f, angle - slipDeg); flash = 0.25f; flashGood = false; MamaAudio.Play("bad", null, 0.5f); if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.2f); }
                    lastKey = key;
                }
                angle = Mathf.Max(0f, angle - pressureDegPerSec * dt * (angle > 0f ? 1f : 0f));
                if (angle >= goal) { Succeed(); return; }
                if (ui) ui.SetMini2(true, title, Current, angle, goal, 0, 0, lastKey == -1 ? 1 : -1, flash, flashGood);
            }
            else
            {
                phase += speed * dt;
                float ptr = Mathf.Sin(phase) * swingAmp;     // 0 deg = OFF (top)
                bool press = fresh && MamaInput.InteractDown;
                if (press)
                {
                    if (Mathf.Abs(ptr) <= offWindow)
                    {
                        hits++; speed += speedPerHit; flash = 0.25f; flashGood = true;
                        MamaAudio.Play("click", Target.transform.position, 0.9f, 1f + hits * 0.15f);
                        phase += Random.Range(0.8f, 2.2f);   // jump so it isn't the same rhythm
                    }
                    else
                    {
                        hits = Mathf.Max(0, hits - 1); flash = 0.35f; flashGood = false;
                        MamaAudio.Play("whoosh", Target.transform.position, 0.6f, 1.4f);
                        if (GameManager.I && GameManager.I.cam) GameManager.I.cam.Shake(0.3f);
                    }
                }
                if (hits >= hitsNeeded) { Succeed(); return; }
                if (ui) ui.SetMini2(true, title, Current, ptr, offWindow, hits, hitsNeeded, 0, flash, flashGood);
            }

            if (player.actions) player.actions.Hold(Current == Kind.Timing ? KidActionAnimator.Clip.Twist : KidActionAnimator.Clip.Crank);
        }

        void Succeed()
        {
            var t = Target; var p = player;
            Cancel();
            MamaAudio.Play("good", null, 0.8f);
            if (t && p) NetGame.Interact(t, p);
        }
    }
}
