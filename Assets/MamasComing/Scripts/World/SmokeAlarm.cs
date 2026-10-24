using UnityEngine;

namespace MamasComing
{
    /// <summary>Ceiling smoke detector. Shrieks when smoke reaches it. Too high to reach without a mop/broom.</summary>
    public class SmokeAlarm : Interactable, IMess
    {
        public float threshold = 0.32f;
        public Renderer led;
        public Light ledLight;
        public AudioSource beep;

        public bool Beeping { get; private set; }
        bool ledWas;
        public bool BatteryOut { get; private set; }
        float hush, blink, beepT;
        MaterialPropertyBlock mpb;

        void Awake() { mpb = new MaterialPropertyBlock(); }
        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        public float Smoke => SmokeSim.I ? SmokeSim.I.At(transform.position) : 0f;

        void Update()
        {
            float dt = Time.deltaTime;
            if (beep) beep.mute = MamaSettings.SfxVol <= 0f;
            if (hush > 0f) hush -= dt;
            bool was = Beeping;
            Beeping = !BatteryOut && hush <= 0f && Smoke > threshold && (GameManager.I == null || GameManager.I.Live);
            if (Beeping && !was && GameManager.I) GameManager.I.OnAlarm();

            blink += dt;
            bool on = Beeping ? (blink % 0.5f < 0.25f) : (!BatteryOut && blink % 3f < 0.1f);
            if (Beeping && on && !ledWas) MamaAudio.Play("beep", transform.position, 0.45f);
            ledWas = on;
            if (led) { mpb.SetColor("_BaseColor", on ? Color.red : new Color(0.3f, 0.05f, 0.05f)); mpb.SetColor("_EmissionColor", on ? new Color(4f, 0.2f, 0.2f) : Color.black); led.SetPropertyBlock(mpb); }
            if (ledLight) ledLight.enabled = on && Beeping;

            if (Beeping && beep)
            {
                beepT -= dt;
                if (beepT <= 0f) { beepT = 0.5f; beep.Play(); }
            }
        }

        bool HasPoker(PlayerController p) => p.Held && (p.Held.tool == ToolType.Mop || p.Held.tool == ToolType.Squeegee);

        public override bool CanInteract(PlayerController p) => !BatteryOut && (HasPoker(p) || Beeping);
        public override string GetPrompt(PlayerController p)
        {
            if (!HasPoker(p)) return "The smoke alarm is too high... hold a mop or squeegee to reach it";
            return Beeping ? "Hold [E]  Jab the HUSH button with the handle" : "Hold [E]  Knock the battery out (sneaky...)";
        }
        public override float HoldTime(PlayerController p) => HasPoker(p) ? (Beeping ? 1.2f : 2.5f) : 0f;
        public override void Interact(PlayerController p)
        {
            if (!HasPoker(p)) { if (GameManager.I) GameManager.I.Toast("Can't reach! Need something long - the mop?"); return; }
            if (Beeping) { hush = 25f; if (GameManager.I) GameManager.I.Toast("Hushed for 25 seconds... clear that smoke!"); }
            else { BatteryOut = true; if (GameManager.I) GameManager.I.Toast("*clack* Battery's out. It can't snitch now... but Mom might notice."); }
        }

        public MessKind Kind => MessKind.Hazard;
        public bool IsProblem => Beeping || BatteryOut;
        public bool IsHidden => false;
        public int Severity => Beeping ? 3 : 1;
        public string MomLine => Beeping ? "THE SMOKE ALARM IS GOING OFF?!" : "Why is the smoke alarm hanging open? Where's the battery?!";
        public string Label => "Smoke alarm";
        public Vector3 Position => transform.position;
    }
}
