using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Dollhouse cut-away for the two-storey house: while the followed kid is downstairs, everything on the upper
    /// floor (slab, walls, furniture, props lying up there) stops rendering so the ground floor stays readable.
    /// Colliders are untouched.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonCamera))]
    public class FloorCutaway : MonoBehaviour
    {
        ThirdPersonCamera cam;
        readonly List<Renderer> upperStatics = new List<Renderer>();
        readonly List<Renderer> movers = new List<Renderer>();
        readonly HashSet<Renderer> hiddenMovers = new HashSet<Renderer>();
        bool upperHidden;
        float rescanT;

        void Start()
        {
            cam = GetComponent<ThirdPersonCamera>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r || r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<PlayerController>() || r.GetComponentInParent<MomController>()) continue;
                if (r.GetComponentInParent<Pickup>()) { movers.Add(r); continue; }
                Bounds b = r.bounds;
                bool overUpperPlan = b.min.x >= -0.4f && b.max.x <= HouseLayout.UpperMaxX + 0.4f && b.min.z >= -0.4f && b.max.z <= HouseLayout.UpperMaxZ + 0.4f;
                if (r.enabled && overUpperPlan && b.min.y >= HouseLayout.WallH - 0.02f && b.min.y < HouseLayout.UpperY + HouseLayout.WallH + 1f) upperStatics.Add(r);
            }
        }

        void LateUpdate()
        {
            if (!cam || !cam.target) return;
            bool hide = HouseLayout.FloorOf(cam.target.position) == 0;
            if (hide != upperHidden)
            {
                upperHidden = hide;
                foreach (var r in upperStatics) if (r) r.enabled = !hide;
            }
            // props can be carried between floors: re-check them a few times a second
            if ((rescanT -= Time.deltaTime) > 0f) return;
            rescanT = 0.2f;
            foreach (var r in movers)
            {
                if (!r) continue;
                bool up = HouseLayout.FloorOf(r.bounds.center) == 1 && r.bounds.center.x <= HouseLayout.UpperMaxX;
                if (hide && up) { if (r.enabled) { r.enabled = false; hiddenMovers.Add(r); } }
                else if (hiddenMovers.Remove(r)) r.enabled = true;   // only ever re-show what this script hid
            }
        }
    }
}
