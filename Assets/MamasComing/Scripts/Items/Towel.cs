using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Bath towel. Drop it on water to soak it up (and hide the wet floor). Wet towels smother fires;
    /// dry ones catch fire. Soggy towels lying around are evidence - toss them in the washer/hamper.
    /// </summary>
    [RequireComponent(typeof(CoverObject))]
    public class Towel : MessyProp
    {
        public override bool CanSettle => false;
        public FireSource ownFire;
        public Renderer[] cloth;
        CoverObject cover;
        bool scorched;
        MaterialPropertyBlock mpb;

        public float Soaked => cover ? cover.soaked : 0f;
        public bool IsWet => Soaked > 0.6f;

        protected override void Awake()
        {
            base.Awake();
            cover = GetComponent<CoverObject>();
            mpb = new MaterialPropertyBlock();
        }

        public void Scorch() { scorched = true; }

        public void CatchFire()
        {
            if (ownFire) ownFire.Ignite(0.3f);
            scorched = true;
        }

        public override string GetPrompt(PlayerController p)
        {
            if (p.Held is Mop) return base.GetPrompt(p);
            return (Soaked > 0.3f ? "Pick up the soggy " : "Pick up the ") + displayName.ToLower();
        }

        public override bool UseHeld(PlayerController p, float dt)
        {
            // wring into a sink/tub/toilet
            var sink = WaterDump.Nearest(p.transform.position, 1.5f);
            if (sink && Soaked > 0f)
            {
                cover.soaked = Mathf.Max(0f, cover.soaked - 2f * dt);
                sink.Accept(2f * dt);
                p.Scrubbing = true;
                return true;
            }
            return false;
        }

        void LateUpdate()
        {
            if (ownFire && ownFire.burning) ownFire.transform.position = transform.position;
            if (scorched && cloth != null)
                foreach (var r in cloth) if (r && !(r is ParticleSystemRenderer)) { mpb.SetColor("_BaseColor", new Color(0.18f, 0.15f, 0.13f)); r.SetPropertyBlock(mpb); }
        }

        public override bool IsProblem => (Soaked > 0.5f || scorched) && !IsHeld && !HideZone.IsHidden(transform.position) || base.IsProblem;
        public override string MomLine => scorched ? "Why is my good towel BURNT?!" : (Soaked > 0.5f ? "Why are there soaking wet towels on the floor?!" : base.MomLine);
    }
}
