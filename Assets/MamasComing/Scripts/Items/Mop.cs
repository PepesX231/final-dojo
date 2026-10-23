using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Hold LMB to mop: big energetic side-to-side strokes that splash. Soaks up to its capacity, then must
    /// be wrung (bucket / sink / toilet / tub). Rushing just shoves the water ahead. Also scrubs powder.
    /// </summary>
    public class Mop : Pickup
    {
        public Transform head;
        public Renderer headRenderer;
        public float capacity = 99999f;
        public float load;
        public float soakRate = 3.2f;
        public float strokeSpeed = 14f;

        public float Phase { get; private set; }
        float toastCd, splashCd, strokeW;
        MaterialPropertyBlock mpb;
        static readonly Color DryCol = new Color(0.96f, 0.95f, 0.9f);
        static readonly Color WetCol = new Color(0.42f, 0.52f, 0.7f);

        protected override void Awake() { base.Awake(); mpb = new MaterialPropertyBlock(); }

        public void Wring() { load = 0f; MamaAudio.Play("wring", transform.position, 0.8f); }
        int lastHalf;

        void Update()
        {
            if (toastCd > 0f) toastCd -= Time.deltaTime;
            if (splashCd > 0f) splashCd -= Time.deltaTime;
            if (headRenderer)
            {
                mpb.SetColor("_BaseColor", Color.Lerp(DryCol, WetCol, load / capacity));
                headRenderer.SetPropertyBlock(mpb);
            }
        }

        public override bool UseHeld(PlayerController p, float dt)
        {
            p.Scrubbing = true;
            if (Grabbing) return true;
            strokeW = Mathf.MoveTowards(strokeW, 1f, dt * 5f);
            Phase += dt * strokeSpeed;
            float s = Mathf.Sin(Phase), c = Mathf.Cos(Phase * 2f);
            // wide figure-8 strokes: sweep across and push forward on each pass
            transform.localPosition = holdOffset + new Vector3(s * 0.48f * strokeW, Mathf.Abs(c) * 0.015f, 0.12f + c * 0.14f * strokeW);
            transform.localRotation = Quaternion.Euler(holdEuler) * Quaternion.Euler(0f, s * 28f * strokeW, -s * 10f * strokeW);

            Vector3 hp = head ? head.position : p.transform.position + p.transform.forward;
            PowderMess.CleanAt(hp, 0.55f, dt);
            int half = Mathf.FloorToInt(Phase / Mathf.PI);
            if (half != lastHalf)
            {
                lastHalf = half;
                bool wetHere = FloodSim.I && FloodSim.I.DepthAround(hp, 0.6f) > 0.0006f;
                if (wetHere) { MamaAudio.Play("squish", hp, 0.75f, Random.Range(0.85f, 1.1f)); MamaAudio.Play("swish", hp, 0.3f, Random.Range(1.2f, 1.4f)); }
                else MamaAudio.Play("swish", hp, 0.4f, Random.Range(0.85f, 1.05f));
            }
            if (!FloodSim.I) return true;

            float depth = FloodSim.I.DepthAt(hp);
            if (depth > 0.0009f && splashCd <= 0f)
            {
                splashCd = 0.09f;
                FloodSim.I.Splash(hp + p.transform.right * s * 0.2f, 1.2f);
            }

            // simple: every stroke soaks up a wide strip of water
            FloodSim.I.RemoveLiters(hp, 0.85f, soakRate * dt);
            FloodSim.I.RemoveLiters(p.transform.position, 0.6f, soakRate * 0.4f * dt);
            return true;
        }

        public override void HeldUpdate(PlayerController p)
        {
            if (!MamaInput.UseHeld && !Grabbing)
            {
                strokeW = 0f;
                transform.localPosition = Vector3.Lerp(transform.localPosition, holdOffset, Time.deltaTime * 10f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(holdEuler), Time.deltaTime * 10f);
            }
        }

        void Hint(string s)
        {
            if (toastCd > 0f) return;
            toastCd = 3f;
            if (GameManager.I) GameManager.I.Toast(s);
        }
    }
}
