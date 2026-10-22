using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M16: giant advertising hologram — slow spin, bob, scanline flicker on the additive tint.</summary>
    public class Hologram : MonoBehaviour
    {
        public float spin = 12f, bobAmp = 1.2f, bobHz = 0.15f;
        public Color tint = new Color(0.3f, 0.9f, 1f, 0.55f);
        Renderer[] _rs; Vector3 _base; MaterialPropertyBlock _mpb; float _t;

        void Start() { _rs = GetComponentsInChildren<Renderer>(); _base = transform.position; _mpb = new MaterialPropertyBlock(); _t = Random.value * 10f; }

        void Update()
        {
            _t += Time.deltaTime;
            transform.Rotate(0, spin * Time.deltaTime, 0, Space.World);
            transform.position = _base + Vector3.up * Mathf.Sin(_t * bobHz * Mathf.PI * 2f) * bobAmp;
            float flick = 0.85f + 0.15f * Mathf.Sin(_t * 23f) * Mathf.Sin(_t * 3.7f);
            if (Mathf.Repeat(_t, 4.3f) < 0.06f) flick = 0.3f;    // glitch
            var c = tint * flick; c.a = tint.a * flick;
            _mpb.SetColor("_BaseColor", c);
            foreach (var r in _rs) if (r != null) r.SetPropertyBlock(_mpb);
        }
    }
}
