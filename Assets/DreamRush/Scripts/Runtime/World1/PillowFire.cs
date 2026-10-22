using System.Collections;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>"Pillow fire" checkpoint (Souls bonfire parody): a pillow with a little dream flame. Lights up when reached.</summary>
    public class PillowFire : MonoBehaviour
    {
        public Dream.Checkpoint checkpoint;
        public Light flame;
        public Transform flameVisual;
        bool _lit;

        void Start()
        {
            var lm = Dream.DreamLevelManager.Current;
            if (lm != null) lm.CheckpointReached += OnReached;
            SetLit(checkpoint != null && checkpoint.isStart);
        }

        void OnDestroy() { var lm = Dream.DreamLevelManager.Current; if (lm != null) lm.CheckpointReached -= OnReached; }

        void OnReached(Dream.Checkpoint c)
        {
            if (c != checkpoint || _lit) return;
            SetLit(true);
            UI.UIRoot.Get().Toast("PILLOW FIRE LIT — จุดเกิดใหม่", 1.8f);
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up * 0.8f, 1.2f, VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, transform.position, 0.8f);
        }

        void SetLit(bool on)
        {
            _lit = on;
            if (flame != null) flame.enabled = on;
            if (flameVisual != null) flameVisual.gameObject.SetActive(on);
        }

        void Update()
        {
            if (!_lit) return;
            if (flame != null) flame.intensity = 2.2f + Mathf.PerlinNoise(Time.time * 4f, 0f) * 1.2f;
            if (flameVisual != null) flameVisual.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 9f) * 0.12f, 1f) * 0.6f;
        }
    }
}
