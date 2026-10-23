using UnityEngine;

namespace MamasComing
{
    /// <summary>High third-person orbit camera with wall cut-away and smoke-thickness fog.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 0.8f, 0f);
        public float distance = 7f, minDistance = 4f, maxDistance = 11f;
        public float yaw = 0f, pitch = 48f;
        public float minPitch = 25f, maxPitch = 75f;
        public float mouseSensitivity = 0.12f;
        public float follow = 10f;
        public bool inputEnabled;
        public float baseFog = 0.012f;

        Vector3 pivot;
        float shake;

        readonly System.Collections.Generic.HashSet<Renderer> hiddenNow = new System.Collections.Generic.HashSet<Renderer>();
        readonly System.Collections.Generic.List<Renderer> scratch = new System.Collections.Generic.List<Renderer>();
        readonly RaycastHit[] occHits = new RaycastHit[24];

        Renderer[] statics;
        void CacheStatics()
        {
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<CutawayWall>() || r.GetComponentInParent<Pickup>() || r.GetComponentInParent<PlayerController>() || r.GetComponentInParent<MomController>()) continue;
                if (r.bounds.size.x > 12f || r.bounds.size.z > 12f) continue;
                list.Add(r);
            }
            statics = list.ToArray();
        }

        /// <summary>Furniture between the camera and the kid (bookshelves, wardrobes, fridge...) turns invisible.</summary>
        void HideOccluders(Vector3 cam, Vector3 look)
        {
            scratch.Clear();
            Vector3 dir = look - cam; float dist = dir.magnitude - 0.5f;
            if (dist > 0.1f)
            {
                int n = Physics.SphereCastNonAlloc(cam, 0.35f, dir.normalized, occHits, dist, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var c = occHits[i].collider;
                    if (!c || c.transform.IsChildOf(target)) continue;
                    if (c.attachedRigidbody && !c.attachedRigidbody.isKinematic) continue;
                    if (c.bounds.size.y < 0.9f && c.bounds.max.y < 1.3f) continue;   // low stuff never blocks
                    if (c.GetComponentInParent<CutawayWall>()) continue;
                    if (c.bounds.size.x > 12f || c.bounds.size.z > 12f) continue;     // floors / ground
                    var r = c.GetComponent<Renderer>();
                    if (r && !scratch.Contains(r)) scratch.Add(r);
                    Bounds bb = c.bounds; bb.Expand(0.08f);
                    if (statics == null) CacheStatics();
                    foreach (var sr in statics) if (sr && bb.Contains(sr.bounds.center) && !scratch.Contains(sr)) scratch.Add(sr);
                }
            }
            foreach (var r in hiddenNow) if (r && !scratch.Contains(r)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            hiddenNow.RemoveWhere(r => !r || !scratch.Contains(r));
            foreach (var r in scratch) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; hiddenNow.Add(r); }
        }

        public void Shake(float s) { shake = Mathf.Max(shake, s); }

        void Start() { if (target) pivot = target.position + offset; }

        void LateUpdate()
        {
            if (!target) return;
            float dt = Time.deltaTime;

            if (inputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 md = MamaInput.MouseDelta, st = MamaInput.StickLook;
                yaw += md.x * mouseSensitivity + st.x * 150f * dt;
                pitch -= md.y * mouseSensitivity * 0.7f + st.y * 100f * dt;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                float z = MamaInput.Zoom;
                if (Mathf.Abs(z) > 0.01f) distance = Mathf.Clamp(distance - Mathf.Sign(z) * 0.7f, minDistance, maxDistance);
            }

            pivot = Vector3.Lerp(pivot, target.position + offset, 1f - Mathf.Exp(-follow * dt));
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pos = pivot - rot * Vector3.forward * distance;
            if (shake > 0f) { pos += Random.insideUnitSphere * shake * 0.15f; shake = Mathf.MoveTowards(shake, 0f, dt * 2f); }
            transform.position = pos;
            transform.rotation = rot;

            Vector3 look = target.position + Vector3.up * 0.7f;
            foreach (var w in CutawayWall.All) if (w) w.Evaluate(transform.position, look);

            HideOccluders(transform.position, look);

            float s = SmokeSim.I ? SmokeSim.I.At(target.position) : 0f;
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, baseFog + Mathf.Clamp01(s / 2f) * 0.045f, dt * 2f);
        }
    }
}
