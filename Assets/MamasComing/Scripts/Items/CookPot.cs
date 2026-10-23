using UnityEngine;

namespace MamasComing
{
    /// <summary>The curry pot. Grease fire source, then the evidence. Lid / wet towel smother it.</summary>
    public class CookPot : Pickup, IMess
    {
        [Range(0, 1)] public float burn = 0.45f;
        public FireSource fire;
        public Renderer[] renderers;
        public GameObject curry;
        public Transform lidSocket;
        public Color cleanColor = new Color(0.78f, 0.8f, 0.86f);
        public Color burntColor = new Color(0.1f, 0.07f, 0.05f);

        public FireSource Fire => fire;
        public Lid lid;
        MaterialPropertyBlock mpb;
        bool cleanedToast;

        protected override void Awake() { base.Awake(); mpb = new MaterialPropertyBlock(); }
        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        void Update()
        {
            mpb.SetColor("_BaseColor", Color.Lerp(cleanColor, burntColor, burn));
            if (renderers != null) foreach (var r in renderers) if (r) r.SetPropertyBlock(mpb);
            if (curry) curry.SetActive(burn > 0.2f);
            if (fire && fire.burning) burn = 1f;
        }

        public override bool CanInteract(PlayerController p)
        {
            if (fire && fire.burning) return p.Held is Lid || p.Held is Towel || p.Held is Bucket;
            return base.CanInteract(p) || p.Held is Lid;
        }

        public override string GetPrompt(PlayerController p)
        {
            if (fire && fire.burning)
            {
                if (p.Held is Lid) return "SLAM the lid on the burning pot!";
                if (p.Held is Towel t) return t.IsWet ? "Throw the WET towel over the flames" : "Throw the dry towel over the flames (risky!)";
                if (p.Held is Bucket) return "Hmm... throw water on a grease fire? (RMB)";
            }
            if (p.Held is Lid && !lid) return "Put the lid on the pot";
            return base.GetPrompt(p);
        }

        public override void Interact(PlayerController p)
        {
            if (fire && fire.burning)
            {
                if (p.Held is Lid l)
                {
                    if (fire.Smother(0.85f)) { AttachLid(p, l); if (GameManager.I) GameManager.I.OnSmart("Lid on = no oxygen. Textbook!"); }
                    else if (GameManager.I) GameManager.I.Toast("The flames are too big for the lid! Use the extinguisher!");
                    return;
                }
                if (p.Held is Towel t)
                {
                    if (t.IsWet && fire.Smother(0.6f)) { t.Scorch(); p.Drop(false); return; }
                    // dry towel (or too-big fire): towel catches fire
                    var tw = p.ReleaseHeld() as Towel;
                    tw.transform.position = transform.position + Vector3.up * 0.3f;
                    tw.OnDropped(p, Vector3.zero);
                    tw.CatchFire();
                    if (GameManager.I) GameManager.I.Toast("The towel caught fire too!! Bad idea!");
                    return;
                }
                return;
            }
            if (p.Held is Lid l2 && !lid) { AttachLid(p, l2); return; }
            base.Interact(p);
        }

        void AttachLid(PlayerController p, Lid l)
        {
            p.ReleaseHeld();
            lid = l;
            l.AttachTo(lidSocket ? lidSocket : transform);
        }

        public override void OnPickedUp(PlayerController p)
        {
            base.OnPickedUp(p);
            if (lid) lid.Body.isKinematic = true;
        }

        public override bool UseHeld(PlayerController p, float dt)
        {
            var sink = KitchenSink.Nearest(p.transform.position, 1.6f);
            if (!sink) { Hint("Scrub it at the KITCHEN SINK (hold LMB there)."); return false; }
            if (!sink.HasWater) { Hint("No water! The main valve is off..."); return false; }
            burn = Mathf.Max(0f, burn - 0.1f * dt);
            p.Scrubbing = true;
            sink.Splash(dt);
            if (burn <= 0.2f && !cleanedToast) { cleanedToast = true; if (GameManager.I) GameManager.I.OnSmart("The pot looks (almost) brand new!"); }
            return true;
        }

        float hintCd;
        void Hint(string s) { if (Time.time < hintCd) return; hintCd = Time.time + 3f; if (GameManager.I) GameManager.I.Toast(s); }

        // IMess
        public MessKind Kind => MessKind.Evidence;
        public bool IsProblem => burn > 0.2f;
        public bool IsHidden => !IsHeld && HideZone.IsHidden(transform.position);
        public int Severity => 2;
        public string MomLine => "Is that my good POT?! Why is it completely BLACK?!";
        public string Label => "Burnt pot";
        public Vector3 Position => transform.position;
    }
}
