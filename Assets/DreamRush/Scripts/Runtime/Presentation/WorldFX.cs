using System.Collections;
using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// Short-lived paper effect sprite: pops in with overshoot, optionally follows a target, fully faces the
    /// camera (full billboarding is reserved for effects, §5.8), then shrinks away. Small & readable (§20.5).
    /// </summary>
    public class WorldFX : MonoBehaviour
    {
        Transform _follow;
        Vector3 _offset, _start;
        float _life, _size, _bob;

        public static WorldFX Spawn(Material mat, Vector3 position, float size = 0.8f, float life = 1f, Transform follow = null, float bob = 0.15f)
        {
            if (mat == null) return null;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "FX_" + mat.name;
            DestroyImmediate(q.GetComponent<Collider>());
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var fx = q.AddComponent<WorldFX>();
            fx._follow = follow;
            fx._offset = follow != null ? position - follow.position : Vector3.zero;
            fx._life = life; fx._size = size; fx._bob = bob;
            q.transform.position = position;
            fx._start = position;
            q.transform.localScale = Vector3.zero;
            fx.StartCoroutine(fx.Run());
            return fx;
        }

        IEnumerator Run()
        {
            float t = 0f;
            while (t < _life)
            {
                t += Time.deltaTime;
                float k = t / _life;
                float pop = k < 0.15f ? EaseOutBack(k / 0.15f) : (k > 0.8f ? 1f - (k - 0.8f) / 0.2f : 1f);
                transform.localScale = Vector3.one * (_size * Mathf.Max(0f, pop));
                Vector3 basePos = _follow != null ? _follow.position + _offset : _start;
                transform.position = basePos + Vector3.up * (Mathf.Sin(t * 8f) * _bob * 0.3f);
                var cam = Camera.main;
                if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
                yield return null;
            }
            Destroy(gameObject);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.7f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }
    }
}
