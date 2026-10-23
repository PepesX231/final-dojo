using UnityEngine;

namespace MamasComing
{
    /// <summary>Gas stove. Left on, the forgotten curry chars, smokes, then the oil ignites.</summary>
    public class Stove : Interactable, IMess
    {
        public bool on = true;
        public Transform burner;
        public Transform knob;
        public GameObject flame;
        public CookPot pot;
        [Tooltip("Seconds of heating (from game start) before the pot oil ignites.")]
        public float igniteAfter = 45f;
        public float heat;
        public ParticleSystem smokeColumn;


        public float IgnitesIn => on && pot && !pot.Fire.burning && !pot.Fire.EverBurned && PotOnBurner ? Mathf.Max(0f, igniteAfter - heat) : -1f;
        bool PotOnBurner => pot && !pot.IsHeld && Vector3.Distance(pot.transform.position, burner.position) < 0.45f;

        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            if (flame) flame.SetActive(on);
            if (knob) knob.localRotation = Quaternion.Slerp(knob.localRotation, Quaternion.Euler(0f, 0f, on ? -90f : 0f), dt * 10f);
            if (smokeColumn && pot)
            {
                var em = smokeColumn.emission;
                bool burning = pot.Fire.burning;
                float k = burning ? 10f + 25f * pot.Fire.intensity : (on && PotOnBurner ? 3f + 10f * Mathf.Clamp01(heat / igniteAfter) : (pot.burn > 0.5f ? 1.5f : 0f));
                em.rateOverTime = k;
                smokeColumn.transform.position = pot.transform.position + Vector3.up * 0.9f;
            }
            bool sim = GameManager.I == null || GameManager.I.SimRunning;
            if (!sim || !pot) return;

            bool onBurner = PotOnBurner;
            if (on && onBurner)
            {
                heat += dt;
                pot.burn = Mathf.Min(1f, pot.burn + 0.02f * dt);
                if (!pot.Fire.burning && !pot.Fire.EverBurned)
                {
                    // smouldering curry: smoke gets heavier the closer it is to igniting
                    float k = Mathf.Clamp01(heat / igniteAfter);
                    if (SmokeSim.I) SmokeSim.I.Add(burner.position, (0.1f + 0.5f * k) * dt);
                    if (heat >= igniteAfter) pot.Fire.Ignite(0.25f);
                }
            }
            pot.Fire.extraGrowth = on && onBurner ? 0.008f : (pot.Fire.type == FireType.Grease ? -0.004f : 0f);
        }

        public override string GetPrompt(PlayerController p) => on ? "Turn OFF the stove" : "Turn the stove back on";
        public override MiniGame.Kind MiniGameKind(PlayerController p) => on ? MiniGame.Kind.Timing : MiniGame.Kind.None;
        public override string MiniGameTitle => "TURN OFF THE GAS!";

        public override void Interact(PlayerController p)
        {
            MamaAudio.Play("click", transform.position, 0.9f, 0.8f);
            on = !on;
            if (GameManager.I) GameManager.I.KidSay(on ? "Why did I do that?!" : (pot && pot.Fire.burning ? "Gas off! But it's still burning!" : "Gas off. Phew!"));
        }

        public MessKind Kind => MessKind.Hazard;
        public bool IsProblem => on;
        public bool IsHidden => false;
        public int Severity => 3;
        public string MomLine => "You left the STOVE ON?! You could've burned the house down!";
        public string Label => "Stove";
        public Vector3 Position => transform.position;
    }
}
