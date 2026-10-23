using UnityEngine;

namespace MamasComing
{
    /// <summary>Kitchen sink: scrub the pot here, fill buckets, wet towels. Needs the main valve open.</summary>
    public class KitchenSink : WaterDump
    {
        public GameObject stream;
        float streamT;

        public static KitchenSink Nearest(Vector3 p, float r)
        {
            KitchenSink best = null; float bd = r;
            foreach (var d in AllDumps)
            {
                if (!(d is KitchenSink k)) continue;
                Vector3 v = k.InteractPoint - p; v.y = 0f;
                if (v.magnitude < bd) { bd = v.magnitude; best = k; }
            }
            return best;
        }

        public new void Splash(float dt)
        {
            streamT = 0.2f;
            base.Splash(dt);
        }

        void Update()
        {
            if (streamT > 0f) streamT -= Time.deltaTime;
            if (stream) stream.SetActive(streamT > 0f && HasWater);
        }
    }
}
