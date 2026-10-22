using DreamRush.Boss;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>
    /// FISHING ROD visual: a sagging paper-ink line from the apple's hand to the target while it is being reeled,
    /// then it snaps back. Uses a LineRenderer (unlit, dark ink) with a little catenary sag.
    /// </summary>
    public class FishingLineFX : MonoBehaviour
    {
        Transform _from, _to;
        LineRenderer _line;
        BossChairController _chair;
        float _life, _t, _release = -1f;
        const int Points = 14;
        static Material s_mat;

        public static FishingLineFX Create(Transform from, Transform to, float maxLife)
        {
            var go = new GameObject("FX_FishingLine");
            var fx = go.AddComponent<FishingLineFX>();
            fx._from = from; fx._to = to; fx._life = maxLife;
            fx._chair = to.GetComponent<BossChairController>();
            fx._line = go.AddComponent<LineRenderer>();
            fx._line.positionCount = Points;
            fx._line.widthMultiplier = 0.035f;
            fx._line.numCapVertices = 2;
            fx._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (s_mat == null) s_mat = Core.RuntimeMaterials.CreateUnlitTransparent(new Color(0.19f, 0.16f, 0.24f, 1f), null, "M_FishingLine");
            fx._line.sharedMaterial = s_mat;
            fx.UpdateLine(0f);
            return fx;
        }

        void LateUpdate()
        {
            if (_from == null || _to == null) { Destroy(gameObject); return; }
            _t += Time.deltaTime;
            bool reeling = _chair == null ? _t < 1.2f : _chair.State == ChairState.Pulled;
            if (!reeling && _release < 0f) _release = _t;
            float retract = _release < 0f ? 0f : Mathf.Clamp01((_t - _release) / 0.25f);
            UpdateLine(retract);
            if (retract >= 1f || _t > _life) Destroy(gameObject);
        }

        void UpdateLine(float retract)
        {
            Vector3 a = _from.position + Vector3.up * 0.75f + _from.right * 0.45f;   // the apple's glove
            Vector3 b = _to.position + Vector3.up * 1.0f;
            b = Vector3.Lerp(b, a, retract);
            float sag = Mathf.Lerp(0.05f, 0.45f, Mathf.Clamp01(Vector3.Distance(a, b) / 10f)) * (1f - retract);
            float wiggle = Mathf.Sin(_t * 40f) * 0.03f;
            for (int i = 0; i < Points; i++)
            {
                float u = i / (Points - 1f);
                Vector3 p = Vector3.Lerp(a, b, u);
                p.y -= Mathf.Sin(u * Mathf.PI) * sag;
                p += Vector3.Cross(b - a, Vector3.up).normalized * Mathf.Sin(u * Mathf.PI * 3f) * wiggle;
                _line.SetPosition(i, p);
            }
        }
    }
}
