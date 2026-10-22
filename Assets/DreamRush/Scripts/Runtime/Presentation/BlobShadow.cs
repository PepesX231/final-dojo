using DreamRush.Core;
using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// Soft contact shadow projected on the surface directly below the target, independent of how high
    /// the target is (GDD §9.7). Shrinks/fades with height so the landing point stays readable.
    /// </summary>
    [DefaultExecutionOrder(90)]
    public class BlobShadow : MonoBehaviour
    {
        public Transform target;
        public Material material;
        public float baseSize = 0.95f;
        public float minScaleAtMaxHeight = 0.55f;
        public float maxHeight = 8f;
        [Range(0, 1)] public float maxAlpha = 0.45f;
        public float surfaceOffset = 0.02f;
        public float castStartHeight = 0.6f;

        Transform _quad;
        Material _instance;
        static Texture2D _sharedTex;

        public bool HasGround { get; private set; }
        public Vector3 GroundPoint { get; private set; }

        void Awake()
        {
            if (target == null) target = transform.parent != null ? transform.parent : transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BlobShadowQuad";
            DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            _quad = go.transform;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (material == null)
            {
                if (_sharedTex == null) _sharedTex = RuntimeMaterials.CreateRadialTexture();
                material = RuntimeMaterials.CreateUnlitTransparent(Palette.MidnightPlum, _sharedTex, "M_BlobShadow_Runtime");
            }
            _instance = new Material(material);
            r.sharedMaterial = _instance;
        }

        void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }

        void LateUpdate() => UpdateShadow();

        public void UpdateShadow()
        {
            if (target == null || _quad == null) return;
            Vector3 origin = target.position + Vector3.up * castStartHeight;
            HasGround = PhysicsUtil.RaycastIgnoring(origin, Vector3.down, maxHeight + castStartHeight, target, out RaycastHit hit);
            _quad.gameObject.SetActive(HasGround);
            if (!HasGround) return;

            GroundPoint = hit.point;
            float height = Mathf.Max(0f, target.position.y - hit.point.y);
            float t = Mathf.Clamp01(height / maxHeight);
            float size = baseSize * Mathf.Lerp(1f, minScaleAtMaxHeight, t);

            // Detach from parent rotation so the quad lies on the surface.
            _quad.SetPositionAndRotation(hit.point + hit.normal * surfaceOffset,
                Quaternion.LookRotation(-hit.normal, Vector3.ProjectOnPlane(target.forward, hit.normal).sqrMagnitude > 1e-4f
                    ? Vector3.ProjectOnPlane(target.forward, hit.normal) : Vector3.forward));
            _quad.localScale = Vector3.one * size;
            var c = Palette.MidnightPlum;
            c.a = maxAlpha * Mathf.Lerp(1f, 0.55f, t);
            _instance.SetColor("_BaseColor", c);
        }
    }
}
