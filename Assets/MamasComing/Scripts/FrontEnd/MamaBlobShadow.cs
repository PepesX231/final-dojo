using UnityEngine;

namespace MamasComing
{
    /// Soft dark contact shadow on whatever is under a character, so standing kids read as standing
    /// (on the ranking podium the low camera made them look like they were floating).
    public class MamaBlobShadow : MonoBehaviour
    {
        public float size = .95f, strength = .55f, maxHeight = 2.5f;
        SpriteRenderer sr; static Sprite blob;

        void Start()
        {
            var go = new GameObject("Blob shadow"); go.transform.SetParent(null, false);
            sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Blob(); sr.sortingOrder = -10;
            sr.color = new Color(.16f, .06f, .08f, strength);
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; sr.receiveShadows = false;
        }
        void OnDestroy() { if (sr) Destroy(sr.gameObject); }
        void OnDisable() { if (sr) sr.enabled = false; }

        void LateUpdate()
        {
            if (!sr) return;
            var from = transform.position + Vector3.up * .6f;
            if (Physics.Raycast(from, Vector3.down, out var hit, maxHeight + .6f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform))
            {
                float h = Mathf.Max(0f, transform.position.y - hit.point.y);
                float k = Mathf.Clamp01(1f - h / maxHeight);
                sr.enabled = true;
                sr.transform.SetPositionAndRotation(hit.point + hit.normal * .012f, Quaternion.LookRotation(-hit.normal) );
                sr.transform.localScale = Vector3.one * size * Mathf.Lerp(.6f, 1f, k);
                var c = sr.color; c.a = strength * k; sr.color = c;
            }
            else sr.enabled = false;
        }

        static Sprite Blob()
        {
            if (blob) return blob;
            const int N = 64; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(N * .5f, N * .5f)) / (N * .5f);
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            blob = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(.5f, .5f), N);
            return blob;
        }
    }
}
