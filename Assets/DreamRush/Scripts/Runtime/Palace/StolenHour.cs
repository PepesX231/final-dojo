using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>A stolen hour (glowing clock-hand shard). 3 of them open the Clock Tower gate. Hidden until its challenge is done (if gated).</summary>
    public class StolenHour : MonoBehaviour
    {
        public string challenge = "";
        public bool hiddenUntilUnlocked;
        public bool Collected { get; private set; }
        Transform _player; float _t; Vector3 _base;

        void Start()
        {
            var m = FindFirstObjectByType<Player.PlayerMotor3D>(); _player = m != null ? m.transform : null;
            _base = transform.position;
            if (hiddenUntilUnlocked) SetVisible(false);
        }

        public void Reveal()
        {
            if (Collected) return;
            hiddenUntilUnlocked = false; SetVisible(true);
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up, 2f, VFX.Gold);
            Audio.AudioManager.Play(Audio.Sfx.SleepChime, transform.position, 1f);
        }

        void SetVisible(bool v) { foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = v; }

        void Update()
        {
            if (Collected || hiddenUntilUnlocked) return;
            _t += Time.deltaTime;
            transform.position = _base + Vector3.up * (1f + Mathf.Sin(_t * 2f) * 0.25f);
            transform.Rotate(0, 90f * Time.deltaTime, 0);
            if (_player != null && Vector3.Distance(_player.position + Vector3.up * 0.6f, transform.position + Vector3.up * 0.8f) < 1.6f) Collect();
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true;
            VFX.Play(VFXKind.StarBurst, transform.position + Vector3.up, 2.4f, VFX.Gold);
            VFX.Play(VFXKind.Confetti, transform.position + Vector3.up, 1.4f);
            CameraShake.Add(0.3f);
            Audio.AudioManager.Play(Audio.Sfx.Victory, transform.position, 0.7f);
            WonderDirector.Novelty("stolen hour");
            PalaceDirector.Current?.OnHourCollected(this);
            gameObject.SetActive(false);
        }
    }
}
