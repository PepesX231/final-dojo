using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Place where smoke escapes (window, front door). While open it sucks nearby smoke out of the
    /// house and you SEE it: grey plumes stream out through the opening into the garden.
    /// </summary>
    public class SmokeVent : MonoBehaviour
    {
        public float radius = 1.2f;
        [Tooltip("Fraction of smoke removed per second around the vent.")]
        public float rate = 1.4f;
        public Window window;
        public Door door;
        public bool alwaysOpen;
        [Tooltip("Direction pointing out of the house through this opening.")]
        public Vector3 outward = Vector3.forward;
        public float openingWidth = 1.2f;

        public bool IsOpen => alwaysOpen || (window && window.open) || (door && door.open);

        bool wasOpen;
        float acc;

        void Start() { if (SmokeSim.I) SmokeSim.I.Register(this); }

        void Update()
        {
            var sim = SmokeSim.I;
            if (!sim || !sim.smokeFx) return;
            bool open = IsOpen;
            float c = 0f;
            if (open)
            {
                // sample the smoke a little inside the room
                c = Mathf.Max(sim.At(transform.position), sim.At(transform.position - outward * 0.8f));
                if (!wasOpen && c > 0.03f) Plume(Mathf.RoundToInt(10 + c * 40f), c, 1.6f);   // big whoosh when you open it
                acc += Mathf.Clamp(c * 30f, 0f, 35f) * Time.deltaTime;
                int n = (int)acc; acc -= n;
                if (n > 0) Plume(n, c, 1f);
            }
            wasOpen = open;
        }

        void Plume(int n, float c, float speedMul)
        {
            var fx = SmokeSim.I.smokeFx;
            Vector3 side = Vector3.Cross(Vector3.up, outward).normalized;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = transform.position - outward * Random.Range(0.1f, 0.7f) + side * Random.Range(-openingWidth * 0.45f, openingWidth * 0.45f);
                p.y = Random.Range(1.15f, 2.1f);
                var ep = new ParticleSystem.EmitParams
                {
                    position = p,
                    velocity = (outward * Random.Range(1.1f, 2.0f) + Vector3.up * Random.Range(0.15f, 0.5f) + side * Random.Range(-0.2f, 0.2f)) * speedMul,
                    startSize = Random.Range(0.5f, 1.1f),
                    startLifetime = Random.Range(2.2f, 3.2f),
                    startColor = new Color(0.36f, 0.34f, 0.33f, Mathf.Clamp(0.25f + c * 0.35f, 0.25f, 0.65f)),
                    rotation = Random.Range(0f, 360f)
                };
                fx.Emit(ep, 1);
            }
        }
    }
}
