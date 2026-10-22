using DreamRush.Combat;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// An expanding ground shock ring (Bell Brute's pound): a glowing band that rolls out to <see cref="maxRadius"/>.
    /// Hurts Pip once if the band passes under him while he is low (≤ 0.7 m above the ground) — jump it.
    /// </summary>
    public class ShockRing : MonoBehaviour
    {
        public float maxRadius = 8f, duration = 0.75f, band = 0.7f, clearance = 0.7f;
        public string reason = "a shock ring";
        IPlayerDamageReceiver _recv; Transform _player; LineRenderer _lr; float _t; bool _hit;
        const int Seg = 56;

        public static ShockRing Spawn(Vector3 at, float radius, float duration, string reason, IPlayerDamageReceiver recv, Transform player)
        {
            var go = new GameObject("ShockRing"); go.transform.position = at;
            var r = go.AddComponent<ShockRing>(); r.maxRadius = radius; r.duration = duration; r.reason = reason; r._recv = recv; r._player = player;
            r._lr = go.AddComponent<LineRenderer>(); r._lr.loop = true; r._lr.positionCount = Seg; r._lr.useWorldSpace = true;
            r._lr.widthMultiplier = 0.45f;
            var lib = VFXLibrary.Instance; if (lib != null && lib.glowAdd != null) r._lr.sharedMaterial = lib.glowAdd;
            r._lr.startColor = r._lr.endColor = new Color(1f, 0.35f, 0.3f, 1f);
            r._lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / duration), r = Mathf.Lerp(0.6f, maxRadius, 1f - (1f - k) * (1f - k));
            Vector3 c = transform.position;
            for (int i = 0; i < Seg; i++) { float a = i / (float)Seg * Mathf.PI * 2f; _lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0.15f, Mathf.Sin(a) * r)); }
            _lr.widthMultiplier = Mathf.Lerp(0.6f, 0.2f, k);
            var col = new Color(1f, 0.35f + k * 0.3f, 0.3f, 1f - k * 0.7f); _lr.startColor = _lr.endColor = col;
            if (!_hit && _player != null)
            {
                Vector3 d = _player.position - c; float flat = new Vector2(d.x, d.z).magnitude;
                if (Mathf.Abs(flat - r) < band && d.y < clearance && d.y > -1.2f) { _hit = true; _recv?.TryHurt(reason, c, 7f); }
            }
            if (_t >= duration) Destroy(gameObject);
        }
    }
}
