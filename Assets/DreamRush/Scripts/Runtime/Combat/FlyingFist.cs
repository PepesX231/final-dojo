using UnityEngine;

namespace DreamRush.Combat
{
    /// <summary>
    /// M20 comet fist (user reference): a giant copy of Pip's mitt shoots forward trailing a fat flame stream, then pops.
    /// Pure presentation — the hit itself is still resolved by PlayerCombat.
    /// </summary>
    public class FlyingFist : MonoBehaviour
    {
        Vector3 _vel; float _t, _life; Vector3 _scale;

        public static void Launch(Transform mitt, Vector3 from, Vector3 dir, float speed = 26f, float life = 0.32f, float size = 1.8f)
        {
            if (mitt == null) return;
            var go = new GameObject("FX_FlyingFist");
            go.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir));
            var copy = Instantiate(mitt.gameObject, go.transform);
            foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0, 0);   // mitt points along the flight
            var f = go.AddComponent<FlyingFist>(); f._vel = dir.normalized * speed; f._life = life; f._scale = Vector3.one * size;
            go.transform.localScale = f._scale * 0.6f;
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.28f; tr.minVertexDistance = 0.05f; tr.numCapVertices = 6;
            tr.widthCurve = new AnimationCurve(new Keyframe(0, 1.6f), new Keyframe(0.3f, 1.2f), new Keyframe(1, 0f));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 1f, 0.8f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.25f), new GradientColorKey(new Color(1f, 0.45f, 0.08f), 0.6f), new GradientColorKey(new Color(0.85f, 0.15f, 0.05f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.35f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = g;
            tr.sharedMaterial = FlameMat();
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Presentation.VFX.Play(Presentation.VFXKind.Flare, from, 0.3f, new Color(1f, 0.7f, 0.25f));
        }

        static Material _flame;
        /// <summary>Vertex-coloured, alpha-blended flame (the additive glow material washed out to nothing in daylight).</summary>
        public static Material FlameMat()
        {
            if (_flame != null) return _flame;
            var sh = Shader.Find("Sprites/Default");
            if (sh != null) _flame = new Material(sh) { name = "M_FX_Flame", renderQueue = 3050 };
            else { var lib = Presentation.VFXLibrary.Instance; _flame = lib != null ? lib.glowAdd : null; }
            return _flame;
        }

        void Update()
        {
            float dt = Time.deltaTime; _t += dt;
            transform.position += _vel * dt;
            float k = _t / _life;
            transform.localScale = _scale * (k < 0.2f ? Mathf.Lerp(0.6f, 1.1f, k / 0.2f) : 1.1f);
            if (Mathf.Repeat(_t, 0.04f) < dt) Presentation.VFX.Play(Presentation.VFXKind.Embers, transform.position, 0.45f);
            if (_t >= _life)
            {
                Presentation.VFX.Play(Presentation.VFXKind.HitSpark, transform.position, 1.2f, new Color(1f, 0.75f, 0.3f));
                Presentation.VFX.Play(Presentation.VFXKind.Poof, transform.position, 0.8f);
                var tr = GetComponent<TrailRenderer>(); if (tr != null) { tr.transform.SetParent(null); tr.autodestruct = true; tr.emitting = false; }
                foreach (Transform c in transform) Destroy(c.gameObject);
                Destroy(this);
            }
        }
    }
}
