using System.Collections;
using System.Collections.Generic;
using DreamRush.UI;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 REWIND: the pieces of something broken (stairs, a bridge) float in their "fallen" pose; pull the rewind
    /// lever and time runs backwards — they fly back into place for <see cref="hold"/> seconds (a ticking clock),
    /// then crumble again. Race across before the clock runs out.
    /// </summary>
    public class RewindBridge : MonoBehaviour
    {
        public List<Transform> pieces = new List<Transform>();
        public float hold = 10f;
        public bool Built { get; private set; }
        public int Rewinds { get; private set; }
        readonly List<(Vector3 p, Quaternion r)> _built = new List<(Vector3, Quaternion)>(), _broken = new List<(Vector3, Quaternion)>();
        bool _busy;

        void Start()
        {
            var rnd = new System.Random(GetInstanceID());
            foreach (var t in pieces)
            {
                _built.Add((t.position, t.rotation));
                var off = new Vector3((float)rnd.NextDouble() * 6f - 3f, -6f - (float)rnd.NextDouble() * 6f, (float)rnd.NextDouble() * 6f - 3f);
                var rot = Quaternion.Euler((float)rnd.NextDouble() * 120f - 60f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 120f - 60f);
                _broken.Add((t.position + off, rot * t.rotation));
                t.SetPositionAndRotation(_broken[_broken.Count - 1].p, _broken[_broken.Count - 1].r);
            }
        }

        public void Rewind() { if (!_busy) StartCoroutine(Run()); }

        IEnumerator Run()
        {
            _busy = true; Rewinds++;
            Audio.AudioManager.Play(Audio.Sfx.Reflect, transform.position, 1f);
            Presentation.CameraShake.Add(0.2f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f) { Pose(Mathf.SmoothStep(0, 1, t), true); yield return null; }
            Pose(1f, true); Built = true;
            for (int s = Mathf.CeilToInt(hold); s > 0; s--)
            {
                if (UIRoot.Exists && (s <= 5 || s == Mathf.CeilToInt(hold))) UIRoot.Get().Toast($"⏱ {s}", 0.8f);
                Audio.AudioManager.Play(Audio.Sfx.UiHover, transform.position, s <= 3 ? 1f : 0.6f);
                yield return new WaitForSeconds(1f);
            }
            Built = false;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f) { Pose(1f - t * t, false); yield return null; }
            Pose(0f, false);
            _busy = false;
        }

        void Pose(float k, bool staggered)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                float ki = staggered ? Mathf.Clamp01(k * 1.6f - i * 0.6f / Mathf.Max(1, pieces.Count)) : k;
                pieces[i].SetPositionAndRotation(Vector3.LerpUnclamped(_broken[i].p, _built[i].p, ki), Quaternion.Slerp(_broken[i].r, _built[i].r, ki));
            }
        }
    }
}
