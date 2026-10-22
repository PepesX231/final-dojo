using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Boss
{
    /// <summary>
    /// Procedural body language for the chair (GDD §12.5, §20.4): backrest snap on notice, uneven running
    /// gait, momentum lean in turns, wobble when stumbling, bend + tremble when exhausted, struggle while sat on.
    /// Purely visual; reads <see cref="BossChairController"/> state.
    /// </summary>
    public class ChairAnimator : MonoBehaviour
    {
        public BossChairController chair;
        public FXLibrary fx;
        public Transform visual;

        Transform _eyeL, _eyeR, _pupilL, _pupilR, _browL, _browR;
        Vector3 _eyeScale, _pupilScale, _pupilRestL, _pupilRestR, _browRestPosL, _browRestPosR;
        Quaternion _browRestL, _browRestR;
        readonly Transform[] _legs = new Transform[4];          // FL, FR, BL, BR
        readonly Quaternion[] _legRest = new Quaternion[4];
        readonly float[] _swing = new float[4];
        float _lastStep;
        float _blinkTimer = 2.5f, _blinkT = -1f, _tapTimer = 3f;
        float _phase, _lean, _bank, _lastYaw;
        Vector3 _restPos;
        Quaternion _restRot;
        Vector3 _restScale;
        float _fxTimer;

        void Awake()
        {
            if (chair == null) chair = GetComponent<BossChairController>();
            if (visual == null) visual = transform.Find("Visual");
            _restPos = visual.localPosition; _restRot = visual.localRotation; _restScale = visual.localScale;
            _eyeL = Find(visual, "Eye_L"); _eyeR = Find(visual, "Eye_R");
            if (_eyeL != null) _eyeScale = _eyeL.localScale;
            _pupilL = Find(visual, "Pupil_L"); _pupilR = Find(visual, "Pupil_R");
            if (_pupilL != null) { _pupilScale = _pupilL.localScale; _pupilRestL = _pupilL.localPosition; _pupilRestR = _pupilR.localPosition; }
            _browL = Find(visual, "Brow_L"); _browR = Find(visual, "Brow_R");
            if (_browL != null)
            {
                _browRestL = _browL.localRotation; _browRestR = _browR.localRotation;
                _browRestPosL = _browL.localPosition; _browRestPosR = _browR.localPosition;
            }
            string[] legNames = { "Leg_FL", "Leg_FR", "Leg_BL", "Leg_BR" };
            for (int i = 0; i < 4; i++) { _legs[i] = Find(visual, legNames[i]); if (_legs[i] != null) _legRest[i] = _legs[i].localRotation; }
            chair.StateChanged += OnState;
            chair.Repositioned += (from, to) =>
            {
                if (fx != null) { WorldFX.Spawn(fx.poof, from + Vector3.up * 0.8f, 1.6f, 0.6f); WorldFX.Spawn(fx.poof, to + Vector3.up * 0.8f, 1.6f, 0.6f); }
            };
        }

        static Transform Find(Transform t, string n)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>()) if (c.name == n) return c;
            return null;
        }

        void OnDestroy() { if (chair != null) chair.StateChanged -= OnState; }

        void OnState(ChairState from, ChairState to)
        {
            Vector3 pos = transform.position;
            switch (to) // voice + body sounds (procedural placeholder set, see docs/AUDIO_CREDITS.md)
            {
                case ChairState.Alert: case ChairState.Reveal: Audio.AudioManager.Play(Audio.Sfx.ChairGasp, pos); break;
                case ChairState.Panic: Audio.AudioManager.Play(Audio.Sfx.ChairYelp, pos); break;
                case ChairState.Stumble: Audio.AudioManager.Play(Audio.Sfx.ChairStumble, pos); break;
                case ChairState.Exhausted: Audio.AudioManager.Play(Audio.Sfx.ChairTired, pos); break;
                case ChairState.Sitting: Audio.AudioManager.Play(Audio.Sfx.ChairCreak, pos); break;
                case ChairState.Defeated: Audio.AudioManager.Play(Audio.Sfx.Victory, pos); break;
            }
            if (fx == null) return;
            Vector3 head = transform.position + Vector3.up * 2.1f;
            switch (to)
            {
                case ChairState.Alert:
                case ChairState.Reveal:
                    WorldFX.Spawn(fx.exclaim, head, 0.9f, 0.9f, transform); break;
                case ChairState.Panic:
                    WorldFX.Spawn(fx.sweat, head + transform.right * 0.5f, 0.55f, 0.8f, transform); break;
                case ChairState.Stumble:
                    WorldFX.Spawn(fx.dizzy, head, 1.0f, chair.config.stumbleDuration, transform); break;
                case ChairState.Exhausted:
                    WorldFX.Spawn(fx.tired, head, 1.0f, chair.config.exhaustionDuration, transform); break;
                case ChairState.OfferSeat:
                    WorldFX.Spawn(fx.heart, head, 0.7f, 1.6f, transform); VFX.Play(VFXKind.StarBurst, head, 1.4f); break;
                case ChairState.Defeated:
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 o = Quaternion.Euler(0, i * 60f, 0) * Vector3.forward * 0.9f;
                        WorldFX.Spawn(fx.confetti, transform.position + Vector3.up * (1.6f + (i % 2) * 0.5f) + o, 1.1f, 1.8f);
                    if (i == 0) VFX.Play(VFXKind.Confetti, transform.position + Vector3.up * 0.5f, 1.2f);
                    }
                    break;
            }
        }

        void LateUpdate()
        {
            if (chair == null || visual == null) return;
            float dt = Time.deltaTime;
            float speed = chair.DevHold ? 5f : (chair.Locomotion != null ? chair.Locomotion.Speed : 0f); // DevHold: show the full gait in place
            float t = chair.StateTime;
            Vector3 pos = _restPos;
            Quaternion rot = _restRot;
            Vector3 scale = _restScale;
            float eye = 1f;
            // legs: x = swing (deg) per leg, splay = outward roll; brows: worry (+ = inner ends up), lift (m)
            float s0 = 0, s1 = 0, s2 = 0, s3 = 0, splay = 0f, worry = 0f, browLift = 0f, pupil = 1f; // pupil < 1 = shocked pinpoints
            bool canBlink = false;

            float yaw = transform.eulerAngles.y;
            float yawRate = dt > 0 ? Mathf.DeltaAngle(_lastYaw, yaw) / dt : 0f;
            _lastYaw = yaw;
            _bank = Mathf.Lerp(_bank, Mathf.Clamp(-yawRate / 25f, -18f, 18f) * Mathf.Clamp01(speed / 6f), 1f - Mathf.Exp(-10f * dt));

            switch (chair.State)
            {
                case ChairState.Idle:
                case ChairState.Dormant:
                    pos.y += Mathf.Sin(Time.time * 1.6f) * 0.015f;
                    canBlink = true;
                    // nervous foot tapping
                    _tapTimer -= dt;
                    if (_tapTimer < 0f) _tapTimer = 2.5f + Mathf.PerlinNoise(Time.time, 0.3f) * 2f;
                    if (_tapTimer < 0.5f) s0 = -Mathf.Abs(Mathf.Sin(_tapTimer * Mathf.PI * 4f)) * 18f;
                    break;
                case ChairState.Alert:
                case ChairState.Reveal:
                    eye = 1.45f; pupil = 0.7f;
                    browLift = 0.02f; worry = 10f;
                    splay = 8f * Mathf.Clamp01(t * 6f);                          // startled: legs stiffen out
                    rot *= Quaternion.Euler(-12f * Mathf.Clamp01(t * 6f), 0, 0); // backrest snap back
                    pos.y += Mathf.Abs(Mathf.Sin(t * 30f)) * 0.04f;
                    break;
                case ChairState.Flee:
                case ChairState.Panic:
                case ChairState.Reposition:
                    {
                        bool panic = chair.State == ChairState.Panic;
                        _phase += dt * (panic ? 26f : 19f) * Mathf.Clamp01(speed / 3f + 0.2f);
                        // uneven, clumsy gait: two different frequencies
                        pos.y += Mathf.Abs(Mathf.Sin(_phase)) * 0.12f + Mathf.Abs(Mathf.Sin(_phase * 0.5f + 1.3f)) * 0.04f;
                        rot *= Quaternion.Euler(8f + Mathf.Sin(_phase) * 6f, 0, _bank + Mathf.Sin(_phase * 0.5f) * (panic ? 9f : 5f));
                        eye = panic ? 1.5f : 1.2f;
                        // trot: diagonal pairs swing together; panic = bigger, flailing stride
                        float amp = (panic ? 34f : 24f) * Mathf.Clamp01(speed / 4f + 0.15f);
                        float sw = Mathf.Sin(_phase);
                        if (Mathf.Floor(_phase / Mathf.PI) != Mathf.Floor(_lastStep / Mathf.PI) && !chair.DevHold)
                            Audio.AudioManager.Play(Audio.Sfx.ChairStep, transform.position, panic ? 0.9f : 0.7f);
                        _lastStep = _phase;
                        s0 = sw * amp; s3 = sw * amp; s1 = -sw * amp; s2 = -sw * amp;
                        worry = panic ? 22f : 12f; browLift = panic ? 0.02f : 0.01f; pupil = panic ? 0.65f : 0.85f;
                        break;
                    }
                case ChairState.Perched:
                    // king of the hill: leaning back, smug sway, one leg tapping
                    rot *= Quaternion.Euler(-10f + Mathf.Sin(t * 2.2f) * 3f, 0, Mathf.Sin(t * 1.4f) * 6f);
                    pos.y += Mathf.Abs(Mathf.Sin(t * 2.2f)) * 0.03f;
                    eye = 1.1f; worry = -8f; canBlink = true;
                    s0 = -Mathf.Abs(Mathf.Sin(t * 7f)) * 16f;
                    break;
                case ChairState.Hesitate:
                    rot *= Quaternion.Euler(-6f, Mathf.Sin(t * 5f) * 12f, 0);
                    eye = 1.25f; worry = 14f; canBlink = true;
                    s1 = Mathf.Sin(t * 9f) * 10f;                                 // shifting its weight
                    break;
                case ChairState.Stumble:
                    rot *= Quaternion.Euler(Mathf.Sin(t * 14f) * 14f * (1f - t / 1.5f) + 18f, 0, Mathf.Cos(t * 11f) * 10f);
                    pos.y -= 0.05f;
                    eye = 0.8f; splay = 18f; worry = 18f;
                    s0 = Mathf.Sin(t * 17f) * 20f; s2 = Mathf.Cos(t * 13f) * 20f;  // legs tangled
                    break;
                case ChairState.Exhausted:
                    scale = Vector3.Scale(scale, new Vector3(1.04f, 0.9f + Mathf.Sin(t * 4f) * 0.02f, 1.04f));
                    rot *= Quaternion.Euler(10f, 0, Mathf.Sin(t * 40f) * 1.5f); // bend + tremble
                    eye = 0.6f; splay = 14f + Mathf.Sin(t * 40f) * 2f; worry = 8f; browLift = -0.03f;
                    break;
                case ChairState.Rooted:
                    rot *= Quaternion.Euler(0, 0, Mathf.Sin(t * 30f) * 4f); // straining against the glue
                    eye = 1.3f; worry = 20f; pupil = 0.8f;
                    s0 = Mathf.Max(0f, Mathf.Sin(t * 12f)) * 12f; s1 = Mathf.Max(0f, -Mathf.Sin(t * 12f)) * 12f; // tugging stuck feet
                    break;
                case ChairState.Frozen:
                    eye = 1.5f; browLift = 0.02f; splay = 6f; pupil = 0.55f;
                    break;
                case ChairState.OfferSeat:
                    rot *= Quaternion.Euler(-4f, 0, 0);
                    pos.y -= 0.04f;
                    eye = 0.9f; canBlink = true; worry = 4f; pupil = 1.1f;
                    s0 = s1 = 14f;                                                // polite little bow on the front legs
                    break;
                case ChairState.Pulled:
                    rot *= Quaternion.Euler(-16f + Mathf.Sin(t * 30f) * 4f, 0, Mathf.Sin(t * 23f) * 6f); // dug-in heels, resisting
                    eye = 1.5f; worry = 24f; pupil = 0.7f;
                    s0 = s1 = -22f; s2 = s3 = -10f;                               // heels dug in
                    break;
                case ChairState.Sitting:
                    rot *= Quaternion.Euler(Mathf.Sin(t * 22f) * 7f, 0, Mathf.Sin(t * 17f) * 9f); // dramatic struggle
                    scale = Vector3.Scale(scale, new Vector3(1f, 0.94f, 1f));
                    eye = 1.6f; worry = 26f; browLift = 0.02f; pupil = 0.6f;
                    s0 = Mathf.Sin(t * 26f) * 26f; s1 = -s0; s2 = Mathf.Cos(t * 21f) * 20f; s3 = -s2; // flailing
                    break;
                case ChairState.Defeated:
                    scale = Vector3.Scale(scale, new Vector3(1.03f, 0.95f, 1.03f));
                    eye = 0.5f; splay = 10f; worry = 4f; browLift = -0.02f;
                    break;
            }

            visual.localPosition = pos;
            visual.localRotation = rot;
            visual.localScale = scale;
            // blink (only in calm-ish states, so big reactions stay readable)
            if (canBlink)
            {
                _blinkTimer -= dt;
                if (_blinkTimer <= 0f) { _blinkT = 0f; _blinkTimer = 2.2f + Mathf.PerlinNoise(Time.time * 0.7f, 1.7f) * 2.5f; }
            }
            if (_blinkT >= 0f)
            {
                _blinkT += dt;
                eye *= Mathf.Lerp(1f, 0.08f, 1f - Mathf.Abs(_blinkT / 0.07f - 1f));
                if (_blinkT > 0.14f) _blinkT = -1f;
            }

            if (_eyeL != null)
            {
                // the face board is small: big reactions widen the eye a little and shrink the pupils instead
                Vector3 es = new Vector3(_eyeScale.x * Mathf.Lerp(1f, 1.08f, Mathf.Clamp01(eye - 1f)), _eyeScale.y * Mathf.Min(eye, 1.18f), _eyeScale.z);
                _eyeL.localScale = es; _eyeR.localScale = es;
            }
            if (_pupilL != null)
            {
                // pupils shrink with the lid and glance toward the player
                Vector3 look = Vector3.zero;
                if (chair.player != null)
                {
                    Vector3 local = transform.InverseTransformPoint(chair.player.transform.position + Vector3.up * 0.6f) - new Vector3(0, 1.55f, 0);
                    look = new Vector3(Mathf.Clamp(local.x * 0.03f, -0.035f, 0.035f), Mathf.Clamp(local.y * 0.03f, -0.03f, 0.025f), 0f);
                }
                float pe = Mathf.Min(1f, eye);
                Vector3 ps = new Vector3(_pupilScale.x * pupil, _pupilScale.y * pe * pupil, _pupilScale.z);
                _pupilL.localScale = ps; _pupilR.localScale = ps;
                _pupilL.localPosition = _pupilRestL + look; _pupilR.localPosition = _pupilRestR + look;
                bool shut = eye < 0.25f;
                _pupilL.gameObject.SetActive(!shut); _pupilR.gameObject.SetActive(!shut);
            }
            if (_browL != null)
            {
                // Brow_L sits at +X in chair space (character's left as seen from the front is the viewer's right)
                _browL.localRotation = _browRestL * Quaternion.Euler(0, 0, worry);
                _browR.localRotation = _browRestR * Quaternion.Euler(0, 0, -worry);
                _browL.localPosition = _browRestPosL + Vector3.up * browLift;
                _browR.localPosition = _browRestPosR + Vector3.up * browLift;
            }
            var swing = _swing; swing[0] = s0; swing[1] = s1; swing[2] = s2; swing[3] = s3;
            for (int i = 0; i < 4; i++)
            {
                if (_legs[i] == null) continue;
                float side = (i % 2 == 0) ? 1f : -1f;   // FL/BL are at +X
                _legs[i].localRotation = _legRest[i] * Quaternion.Euler(swing[i], 0f, splay * side);
            }
        }
    }
}
