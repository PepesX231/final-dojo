using UnityEngine;

namespace MamasComing
{
    /// <summary>Main water shut-off valve (laundry room). Turning it kills every tap in the house.</summary>
    public class MainValve : Interactable, IMess
    {
        public bool open = true;
        public Transform wheel;
        float spin;

        public override string GetPrompt(PlayerController p) => open ? "Shut the MAIN water valve" : "Hold [E]  Open the main water valve";
        public override MiniGame.Kind MiniGameKind(PlayerController p) => open ? MiniGame.Kind.Mash : MiniGame.Kind.None;
        public override string MiniGameTitle => "CRANK THE MAIN VALVE!";
        public override float HoldTime(PlayerController p) => open ? 0f : 1.5f;
        public override void HoldTick(PlayerController p, float t) { spin += Time.deltaTime * (open ? 420f : -420f); }

        void Update() { if (wheel) wheel.localRotation = Quaternion.Euler(0f, 0f, spin); }

        public override void Interact(PlayerController p)
        {
            MamaAudio.Play("creak", transform.position, 0.8f, 0.8f); MamaAudio.Play("click", transform.position, 0.8f, 0.6f);
            open = !open;
            if (GameManager.I) GameManager.I.Toast(open ? "Water is back on." : "*clunk* Main water OFF. Every tap in the house just went quiet.");
        }

        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }
        public MessKind Kind => MessKind.Hazard;
        public bool IsProblem => !open;
        public bool IsHidden => false;
        public int Severity => 1;
        public string MomLine => "Why is there no water?! Who turned off the main valve?!";
        public string Label => "Main valve";
        public Vector3 Position => transform.position;
    }
}
