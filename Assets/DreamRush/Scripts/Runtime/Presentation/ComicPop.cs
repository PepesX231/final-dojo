using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Presentation
{
    /// <summary>
    /// M18 punch feedback: a comic-book onomatopoeia burst (POW! BAM! WHAM! POP!) that pops at the impact point,
    /// overshoots, wobbles, drifts up and fades — the Persona/comic layer on top of the sparks. Camera-facing, pooled.
    /// </summary>
    public class ComicPop : MonoBehaviour
    {
        static readonly string[] Words = { "POW", "BAM", "WHAM", "POP" };
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static readonly Stack<ComicPop> Pool = new Stack<ComicPop>();
        public static int Spawned { get; private set; }
        MeshRenderer _r; float _t, _size, _spin; Vector3 _at; Color _c = Color.white;

        public static void Show(Vector3 at, float size = 1.6f, string word = null)
        {
            word ??= Words[Random.Range(0, Words.Length)];
            if (!Mats.TryGetValue(word, out var m) || m == null)
            {
                var tex = Resources.Load<Texture2D>("DreamRush/VFX/FX_" + word);
                if (tex == null) return;
                m = Core.RuntimeMaterials.CreateUnlitTransparent(Color.white, tex, "M_FX_" + word); m.renderQueue = 3100;
                Mats[word] = m;
            }
            ComicPop p = null;
            while (Pool.Count > 0 && p == null) p = Pool.Pop();
            if (p == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "FX_ComicPop"; Destroy(go.GetComponent<Collider>()); DontDestroyOnLoad(go);
                p = go.AddComponent<ComicPop>(); p._r = go.GetComponent<MeshRenderer>(); p._r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            p._r.sharedMaterial = m; p._at = at + Random.insideUnitSphere * 0.25f + Vector3.up * 0.6f; p._size = size; p._t = 0f; p._spin = Random.Range(-14f, 14f);
            p.gameObject.SetActive(true); Spawned++;
            p.Update();
        }

        void Update()
        {
            _t += Mathf.Max(Time.unscaledDeltaTime * 0.6f, Time.deltaTime);
            const float life = 0.55f;
            if (_t >= life) { gameObject.SetActive(false); Pool.Push(this); return; }
            float k = _t / life;
            float pop = k < 0.18f ? Mathf.Lerp(0.2f, 1.25f, k / 0.18f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.18f) / 0.15f));
            var cam = Camera.main;
            transform.position = _at + Vector3.up * k * 0.6f;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position) * Quaternion.Euler(0, 0, _spin + Mathf.Sin(_t * 40f) * 4f * (1f - k));
            transform.localScale = Vector3.one * _size * pop * (k > 0.75f ? 1f - (k - 0.75f) * 2f : 1f);
        }
    }
}
