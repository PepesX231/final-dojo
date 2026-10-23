using System.Linq;
using DreamRush.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MamasComing.WitchTest
{
    [DefaultExecutionOrder(110)]
    public class WitchLevel : MonoBehaviour
    {
        public enum Phase { Title, Playing, Arrival, Won, Lost }
        public Phase phase;
        public PlayerMotor3D motor;
        public PlayerInputReader input;
        public TinyWitchCamera orbit;
        public WitchHUD hud;
        public Transform wandPivot, wandTip, mother;
        public Material magicMaterial;
        public WitchProp[] props;
        public WitchHome[] homes;
        public float roundSeconds = 210f;
        public float TimeLeft { get; private set; }
        public float HoldDistance { get; private set; } = .72f;
        float HoldMinimum => Mathf.Clamp(Vector3.Distance(orbit.transform.position, motor.transform.position + Vector3.up * .06f) + .10f, .22f, .60f);
        public WitchProp Held { get; private set; }
        public WitchProp Aimed { get; private set; }
        public bool Paused { get; private set; }
        public int TidyCount => props.Count(x => x && x.Placed);
        public bool ValidPlacement => Held && Held.home && Held.home.Accepts(Held, Held.transform.position);
        public string Feedback { get; private set; }
        float feedbackUntil, arrivalT, completeT, pulse;
        bool arrivalSuccess;
        Vector3 spawn, motherStart, heldGoal;
        Quaternion spawnRotation;
        LineRenderer tether;
        Light tipLight;
        Transform arm;
        AudioSource audioSource;
        AudioClip liftSound, placeSound, wrongSound;

        void Start()
        {
            spawn = motor.transform.position; spawnRotation = motor.transform.rotation;
            motherStart = mother.position; mother.gameObject.SetActive(false);
            TimeLeft = roundSeconds;
            hud.Build(this);
            BuildMagic();
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.volume = .25f;
            liftSound = Tone("wand lift", 420, 720, .22f); placeSound = Tone("gentle placement", 650, 980, .3f); wrongSound = Tone("soft release", 330, 240, .16f);
            arm = motor.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == "Arm_R");
            ApplyControl(false);
        }

        void BuildMagic()
        {
            var line = new GameObject("Wand levitation ribbon"); line.transform.SetParent(transform);
            tether = line.AddComponent<LineRenderer>(); tether.sharedMaterial = magicMaterial;
            tether.positionCount = 25; tether.widthMultiplier = .0022f; tether.numCapVertices = 4;
            tether.startColor = new Color(1f, .72f, .42f, .75f); tether.endColor = new Color(.70f, 1f, .81f, .35f);
            tether.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tether.enabled = false;
            var lightGO = new GameObject("Wand bud glow"); lightGO.transform.SetParent(wandTip, false);
            tipLight = lightGO.AddComponent<Light>(); tipLight.type = LightType.Point; tipLight.color = new Color(1f, .65f, .36f); tipLight.range = .18f; tipLight.intensity = .0015f; tipLight.shadows = LightShadows.None;
        }

        void Update()
        {
            var k = Keyboard.current; var mouse = Mouse.current;
            if (phase == Phase.Title)
            {
                if (k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) Begin();
                return;
            }
            if (phase == Phase.Won || phase == Phase.Lost)
            {
                if (k != null && k.rKey.wasPressedThisFrame) Restart();
                return;
            }
            if (phase == Phase.Arrival)
            {
                arrivalT += Time.deltaTime;
                mother.position = Vector3.Lerp(motherStart, motherStart + Vector3.forward * .55f, Mathf.SmoothStep(0, 1, arrivalT / 3f));
                if (arrivalT >= 3f) { phase = arrivalSuccess ? Phase.Won : Phase.Lost; hud.ShowEnd(arrivalSuccess); ApplyControl(false); }
                return;
            }
            if (k != null && k.escapeKey.wasPressedThisFrame) TogglePause();
            if (Paused) return;
            TimeLeft = Mathf.Max(0, TimeLeft - Time.deltaTime);
            if (TimeLeft <= 0) { Arrive(false); return; }
            if (motor.transform.position.y < .35f)
            {
                motor.Teleport(spawn, spawnRotation); TimeLeft = Mathf.Max(0, TimeLeft - 8);
                Tell("ตกโต๊ะ! กลับมาลองใหม่ เสียเวลา 8 วินาที");
            }
            UpdateAim();
            bool click = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);
            if (click) { if (Held) ReleaseHeld(true); else if (Aimed) Lift(Aimed); }
            if (Held)
            {
                float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0;
                if (Mathf.Abs(scroll) > .01f) AdjustDistance(Mathf.Sign(scroll) * .045f);
                if (k != null)
                {
                    float turn = (k.eKey.isPressed ? 1 : 0) - (k.qKey.isPressed ? 1 : 0);
                    Held.Turn(turn * 70f * Time.deltaTime);
                }
                if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (k != null && k.xKey.wasPressedThisFrame)) ReleaseHeld(false);
            }
            foreach (var prop in props)
                if (prop && !prop.Held && !prop.Placed && prop.transform.position.y < -.3f) { prop.ResetProp(); Tell("ของตกพื้น ถูกส่งกลับมาที่โต๊ะให้ลองใหม่"); }
            completeT = TidyCount == props.Length ? completeT + Time.deltaTime : 0;
            if (completeT > .8f) Arrive(true);
            bool notes = k != null && (k.tabKey.isPressed || k.hKey.isPressed);
            hud.ShowNotes(notes);
        }

        void FixedUpdate()
        {
            if (phase != Phase.Playing || Paused || !Held) return;
            Ray aim = orbit.GetComponent<Camera>().ViewportPointToRay(new Vector3(.5f, .5f));
            heldGoal = aim.GetPoint(HoldDistance) - Held.transform.TransformVector(Held.shape.center);
            // Grounded objects hover a little until the player deliberately releases them.
            Held.MoveHeld(heldGoal, motor.transform, Time.fixedDeltaTime);
        }

        void UpdateAim()
        {
            Aimed = null;
            Ray ray = orbit.GetComponent<Camera>().ViewportPointToRay(new Vector3(.5f, .5f));
            foreach (var hit in Physics.SphereCastAll(ray, .012f, 1.25f, ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance))
            {
                if (hit.collider.transform.IsChildOf(motor.transform)) continue;
                var prop = hit.collider.GetComponentInParent<WitchProp>();
                if (prop && !prop.Held && !prop.Settling) Aimed = prop;
                break; // environmental geometry correctly blocks objects behind it
            }
        }

        public bool Lift(WitchProp prop)
        {
            if (!prop || Held || phase != Phase.Playing || Paused || Vector3.Distance(motor.transform.position, prop.Center) > 1.4f) return false;
            Held = prop; prop.Lift();
            HoldDistance = Mathf.Clamp(Vector3.Distance(orbit.transform.position, prop.Center), HoldMinimum, 1.35f);
            motor.MoveSpeedScale = prop.id == "book" ? .72f : .88f;
            audioSource.PlayOneShot(liftSound); Tell("หมุนล้อเมาส์เพื่อขยับใกล้–ไกล แล้วคลิกเพื่อวาง");
            return true;
        }

        public void AdjustDistance(float delta) { if (Held) HoldDistance = Mathf.Clamp(HoldDistance + delta, HoldMinimum, 1.35f); }

        public bool ReleaseHeld(bool place)
        {
            if (!Held) return false;
            var prop = Held; Held = null;
            bool correct = prop.Release(place); motor.MoveSpeedScale = 1;
            audioSource.PlayOneShot(correct ? placeSound : wrongSound);
            Tell(correct ? prop.label + " เข้าที่แล้ว" : "วางไว้ตรงนี้ก่อน เล็งแล้วหยิบใหม่ได้");
            return correct;
        }

        void LateUpdate()
        {
            pulse += Time.deltaTime;
            bool lifting = Held && phase == Phase.Playing;
            if (arm && lifting) arm.localRotation *= Quaternion.Euler(-48, 0, -12);
            if (wandPivot) wandPivot.localRotation = Quaternion.Slerp(wandPivot.localRotation, Quaternion.Euler(lifting ? -50 : -8, 0, -22), 1f - Mathf.Exp(-10f * Time.deltaTime));
            if (tipLight) tipLight.intensity = lifting ? .007f + Mathf.Sin(pulse * 5) * .001f : .0015f;
            if (tether)
            {
                tether.enabled = lifting;
                if (lifting)
                {
                    Vector3 a = wandTip.position, b = Held.Center;
                    Vector3 right = Vector3.Cross((b - a).normalized, Vector3.up).normalized;
                    for (int i = 0; i < 25; i++)
                    {
                        float t = i / 24f;
                        Vector3 wiggle = (right * Mathf.Sin(t * 16f - pulse * 5) + Vector3.up * Mathf.Cos(t * 16f - pulse * 5)) * (.008f * Mathf.Sin(t * Mathf.PI));
                        tether.SetPosition(i, Vector3.Lerp(a, b, t) + wiggle);
                    }
                }
            }
            bool notes = Keyboard.current != null && (Keyboard.current.hKey.isPressed || Keyboard.current.tabKey.isPressed);
            foreach (var home in homes) home.Show(phase == Phase.Playing && ((Held && Held.home == home) || notes), Held && home.Accepts(Held, Held.transform.position));
            if (hud) hud.Refresh();
        }

        public void Begin() { phase = Phase.Playing; TimeLeft = roundSeconds; hud.ShowTitle(false); ApplyControl(true); Tell("แม่กำลังกลับ... ลองเล็งของรกแล้วคลิก"); }

        void Arrive(bool success)
        {
            if (Held) ReleaseHeld(false);
            phase = Phase.Arrival; arrivalSuccess = success; arrivalT = 0;
            mother.gameObject.SetActive(true); mother.position = motherStart;
            // Stage the short arrival shot at the clear front lip, away from the tall window sill.
            motor.Teleport(new Vector3(motherStart.x, spawn.y, spawn.z - .06f), Quaternion.Euler(0, 180, 0));
            orbit.yaw = 180; orbit.pitch = -14; orbit.distance = .42f;
            ApplyControl(false); hud.ShowNotes(false);
            Tell(success ? "แม่กลับมาแล้ว... ทุกอย่างเข้าที่!" : "แม่กลับมาแล้ว... งานยังไม่เสร็จ!");
        }

        public void TogglePause()
        {
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1;
            ApplyControl(!Paused); hud.ShowPause(Paused);
        }

        void ApplyControl(bool enabled)
        {
            motor.InputEnabled = enabled; orbit.inputEnabled = enabled;
            input.GameplayBlocked = !enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !enabled;
        }

        public void Restart()
        {
            Time.timeScale = 1; Paused = false;
            if (Held) ReleaseHeld(false);
            foreach (var prop in props) prop.ResetProp();
            mother.gameObject.SetActive(false); mother.position = motherStart;
            motor.Teleport(spawn, spawnRotation); orbit.yaw = 75; orbit.pitch = 18; orbit.distance = .48f;
            completeT = 0; phase = Phase.Playing; TimeLeft = roundSeconds;
            hud.ClearOverlays(); ApplyControl(true);
        }

        public void Tell(string message) { Feedback = message; feedbackUntil = Time.unscaledTime + 3.2f; }
        public string CurrentFeedback => Time.unscaledTime < feedbackUntil ? Feedback : "";

        static AudioClip Tone(string name, float from, float to, float duration)
        {
            int rate = 22050; var data = new float[Mathf.CeilToInt(rate * duration)]; float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)data.Length; phase += Mathf.Lerp(from, to, t) / rate * Mathf.PI * 2;
                data[i] = (Mathf.Sin(phase) + .22f * Mathf.Sin(phase * 2)) * Mathf.Sin(t * Mathf.PI) * .18f;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            if (liftSound) Destroy(liftSound); if (placeSound) Destroy(placeSound); if (wrongSound) Destroy(wrongSound);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
