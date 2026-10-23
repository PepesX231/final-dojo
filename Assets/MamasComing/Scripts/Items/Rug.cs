using UnityEngine;

namespace MamasComing
{
    /// <summary>
    /// Rug / runner. Grabbed by its nearest edge and dragged flat along the floor, trailing behind the kid
    /// like a real rug on a rope. Hides wet patches and soaks water (gets heavy and dark).
    /// </summary>
    [RequireComponent(typeof(CoverObject))]
    public class Rug : Pickup
    {
        public float floorY = 0.012f;
        CoverObject cover;
        PlayerController puller;
        Vector3 grabLocal;      // grabbed point in rug-local space (on the edge)
        float grabDist;
        float wobble;

        public override bool FollowsOnItsOwn => true;

        protected override void Awake()
        {
            base.Awake();
            cover = GetComponent<CoverObject>();
            foreach (var c in cols) if (c) c.isTrigger = true;
            Body.isKinematic = true;
        }

        public override float SpeedMultiplier => Mathf.Lerp(0.85f, 0.45f, cover ? cover.Wetness01 : 0f);

        public override string GetPrompt(PlayerController p)
        {
            string n = displayName.ToLower();
            if (p.Held != null) return "Swap for the " + n;
            return cover && cover.Wetness01 > 0.4f ? "Grab the soggy " + n + " and drag it" : "Grab the " + n + " and drag it";
        }

        public override void OnPickedUp(PlayerController p)
        {
            base.OnPickedUp(p);
            puller = p;
            // nearest point on the rug's edge to the kid
            Vector3 l = transform.InverseTransformPoint(p.transform.position);
            Vector2 h = cover ? cover.halfSize : new Vector2(1f, 0.5f);
            float cx = Mathf.Clamp(l.x, -h.x, h.x), cz = Mathf.Clamp(l.z, -h.y, h.y);
            // push to the closest edge
            float dx = h.x - Mathf.Abs(cx), dz = h.y - Mathf.Abs(cz);
            if (dx < dz) cx = Mathf.Sign(cx == 0f ? 1f : cx) * h.x; else cz = Mathf.Sign(cz == 0f ? 1f : cz) * h.y;
            grabLocal = new Vector3(cx, 0f, cz);
            grabDist = new Vector2(cx, cz).magnitude;
        }

        void LateUpdate()
        {
            if (!IsHeld || !puller) return;
            float fy = HouseLayout.FloorYAt(puller.transform.position) + floorY;   // whichever storey the puller is on
            // the hands are just behind the kid: the rug trails behind as you walk away
            Vector3 hand = puller.transform.position - puller.transform.forward * 0.25f;
            hand.y = fy;
            Vector3 center = transform.position; center.y = fy;
            Vector3 grabWorld = transform.TransformPoint(grabLocal); grabWorld.y = fy;

            Vector3 toHand = hand - center; toHand.y = 0f;
            float d = toHand.magnitude;
            if (d > 0.001f)
            {
                Vector3 dir = toHand / d;
                // rotate so the grabbed edge points at the hands (slowly, rugs are floppy)
                float wantYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - Mathf.Atan2(grabLocal.x, grabLocal.z) * Mathf.Rad2Deg;
                float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, wantYaw, 220f * Time.deltaTime * Mathf.Clamp01(puller.PlanarSpeed));
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                // rope constraint: only pulled when the hands get further than the grab distance
                grabWorld = transform.TransformPoint(grabLocal); grabWorld.y = fy;
                Vector3 slack = hand - grabWorld; slack.y = 0f;
                if (slack.magnitude > 0.05f) center += slack;
            }
            wobble += Time.deltaTime * puller.PlanarSpeed * 6f;
            transform.position = new Vector3(center.x, fy + Mathf.Abs(Mathf.Sin(wobble)) * 0.008f, center.z);
        }

        public override void OnDropped(PlayerController p, Vector3 velocity)
        {
            base.OnDropped(p, Vector3.zero);
            puller = null;
            Vector3 pos = transform.position;
            float fy = HouseLayout.FloorYAt(transform.position) + floorY;
            pos.y = fy;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            Body.isKinematic = true;
        }
    }
}
