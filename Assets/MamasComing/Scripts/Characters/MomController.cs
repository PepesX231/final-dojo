using UnityEngine;

namespace MamasComing
{
    /// <summary>Mom walks a fixed inspection route through the house and checks what she can see.</summary>
    public class MomController : MonoBehaviour
    {
        [System.Serializable]
        public class InspectPoint
        {
            public string name;
            public Transform point;
            public float radius = 3.5f;
            public bool inspect = true;
            public bool checksHidingSpots;
            [TextArea] public string arriveLine;
        }

        public InspectPoint[] route;
        public float walkSpeed = 1.6f;
        public float lookTime = 2.2f;
        public Transform visual;
        public Transform head;

        public bool Active { get; private set; }
        public bool Done { get; private set; }

        int idx;
        float wait, bobT, bonk;
        bool looking;

        public void Begin()
        {
            Active = true;
            Done = false;
            idx = 0;
            looking = false;
        }

        public void Bonk() { bonk = 1.2f; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (bonk > 0f) bonk -= dt;

            if (Active && !Done && route != null && route.Length > 0)
            {
                var ip = route[idx];
                Vector3 tgt = ip.point.position;   // route points carry their floor height: stair legs are straight slopes
                if (!looking)
                {
                    Vector3 d = tgt - transform.position;
                    float dist = d.magnitude;
                    if (dist < 0.08f)
                    {
                        looking = true;
                        wait = ip.inspect ? lookTime : 0f;
                        if (!string.IsNullOrEmpty(ip.arriveLine) && GameManager.I) GameManager.I.MomSay(ip.arriveLine);
                        if (ip.inspect && GameManager.I) GameManager.I.MomInspect(ip, this);
                    }
                    else
                    {
                        transform.position += d / dist * Mathf.Min(dist, walkSpeed * dt);
                        Vector3 face = new Vector3(d.x, 0f, d.z);
                        if (face.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 6f);
                        bobT += dt * 9f;
                        if (GameManager.I) GameManager.I.MomStep(transform.position);
                    }
                }
                else
                {
                    wait -= dt;
                    if (head) head.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 2.2f) * 45f, 0f);
                    if (wait <= 0f)
                    {
                        if (head) head.localRotation = Quaternion.identity;
                        idx++;
                        looking = false;
                        if (idx >= route.Length)
                        {
                            Done = true;
                            if (GameManager.I) GameManager.I.MomFinished();
                        }
                    }
                }
            }

            if (visual)
            {
                visual.localPosition = new Vector3(0f, (Active && !looking && !Done) ? Mathf.Abs(Mathf.Sin(bobT)) * 0.05f : 0f, 0f);
                visual.localRotation = Quaternion.Euler(bonk > 0f ? -18f * Mathf.Sin(bonk * 12f) : 0f, 0f, 0f);
            }
        }
    }
}
