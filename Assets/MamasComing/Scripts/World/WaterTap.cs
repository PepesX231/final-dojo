using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Bathtub tap left running. The handle is old and snaps off when you try it,
    /// so you must shut the main valve, or pull the plug so the tub stops overflowing.
    /// </summary>
    public class WaterTap : Interactable, IMess
    {
        [Header("Flow")]
        public bool running = true;
        public float litersPerSecond = 1.6f;
        public MainValve mainValve;
        public bool handleBreaks = false;
        public bool handleBroken;

        [Header("Tub")]
        public float tubCapacity = 160f;
        public float tubLevel = 160f;
        public bool plugIn = true;
        public float plugDrainRate = 2.2f;
        public Transform tubWater;
        public float tubWaterEmptyY, tubWaterFullY;
        public Transform[] overflowPoints;
        public GameObject stream, overflowSheet;
        public Transform handle;
        public GameObject brokenHandle;

        public bool Flowing => running && (!mainValve || mainValve.open);
        public bool Overflowing => tubLevel >= tubCapacity - 0.01f && Flowing && plugIn;

        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        void Update()
        {
            float dt = Time.deltaTime;
            bool sim = GameManager.I == null || GameManager.I.SimRunning;
            if (stream) stream.SetActive(Flowing);
            if (overflowSheet) overflowSheet.SetActive(Overflowing);
            if (brokenHandle) brokenHandle.SetActive(handleBroken);
            if (handle) handle.gameObject.SetActive(!handleBroken);

            if (sim)
            {
                float inflow = Flowing ? litersPerSecond * dt : 0f;
                float outflow = plugIn ? 0f : plugDrainRate * dt;
                tubLevel = Mathf.Max(0f, tubLevel + inflow - outflow);
                if (tubLevel > tubCapacity && FloodSim.I && overflowPoints != null && overflowPoints.Length > 0)
                {
                    float spill = tubLevel - tubCapacity;
                    tubLevel = tubCapacity;
                    float each = spill / overflowPoints.Length;
                    foreach (var o in overflowPoints) if (o) FloodSim.I.AddLiters(o.position, each, 0.2f);
                }
            }
            if (tubWater)
            {
                var lp = tubWater.localPosition;
                lp.y = Mathf.Lerp(tubWaterEmptyY, tubWaterFullY, tubLevel / tubCapacity);
                tubWater.localPosition = lp;
                tubWater.gameObject.SetActive(tubLevel > 2f);
            }
        }

        public override string GetPrompt(PlayerController p)
        {
            if (handleBroken)
                return plugIn ? "Hold [E]  Reach into the tub and pull the plug" : "Push the plug back in";
            return running ? "Close the bathtub tap" : "Turn on the tap";
        }

        public override float HoldTime(PlayerController p) => handleBroken && plugIn ? 1.5f : 0f;
        public override MiniGame.Kind MiniGameKind(PlayerController p) => running && !handleBroken ? MiniGame.Kind.Mash : MiniGame.Kind.None;
        public override string MiniGameTitle => "CLOSE THE TAP! It's stuck!";

        public override void Interact(PlayerController p)
        {
            if (handleBroken)
            {
                plugIn = !plugIn;
                MamaAudio.Play("gurgle", transform.position, 0.9f);
                if (GameManager.I) GameManager.I.Toast(plugIn ? "Plug back in. The tub will fill again..." :
                    "*SHLOOORP* Plug pulled! The tub drains... but the tap is STILL running.");
                return;
            }
            if (running && handleBreaks)
            {
                handleBroken = true;
                if (GameManager.I) GameManager.I.Toast("*CRACK*  The old handle SNAPPED OFF! Shut the MAIN VALVE (laundry) or pull the plug!");
                return;
            }
            running = !running;
            MamaAudio.Play("squeak", transform.position, 0.8f);
            if (GameManager.I) GameManager.I.KidSay(running ? "Oops..." : "Phew! Water's off!");
        }

        public MessKind Kind => MessKind.Hazard;
        public bool IsProblem => Flowing || handleBroken;
        public bool IsHidden => false;
        public int Severity => Flowing ? 3 : 2;
        public string MomLine => Flowing ? "Is that the BATHTUB running?! Why is it still running?!" : "WHO BROKE THE BATHTUB TAP?!";
        public string Label => "Bathtub tap";
        public Vector3 Position => transform.position;
    }
}
