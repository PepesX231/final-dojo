using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>Flat paper decal lying on the floor (glue puddle, landing ring). Pops in, holds, shrinks away. No collider.</summary>
    public class GroundDecal : MonoBehaviour
    {
        float _life, _size, _t;

        public static GroundDecal Spawn(Material mat, Vector3 groundPoint, float size, float life)
        {
            if (mat == null) return null;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Decal_" + mat.name;
            DestroyImmediate(q.GetComponent<Collider>());
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            q.transform.position = groundPoint + Vector3.up * 0.015f;
            q.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            q.transform.localScale = Vector3.zero;
            var d = q.AddComponent<GroundDecal>();
            d._life = life; d._size = size;
            return d;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / Mathf.Max(0.01f, _life);
            float pop = _t < 0.18f ? Mathf.SmoothStep(0f, 1.12f, _t / 0.18f) : (k > 0.85f ? Mathf.Max(0f, (1f - k) / 0.15f) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((_t - 0.18f) / 0.15f)));
            transform.localScale = Vector3.one * (_size * pop);
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
