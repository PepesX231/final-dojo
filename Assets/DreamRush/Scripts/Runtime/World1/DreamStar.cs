using System.Collections;
using DreamRush.Player;
using DreamRush.Presentation;
using UnityEngine;

namespace DreamRush.World1
{
    /// <summary>Dream Star collectible (3 per chapter). Spins, bobs, bursts when collected.</summary>
    public class DreamStar : MonoBehaviour
    {
        public int chapter;
        public float radius = 1.1f;
        public static int CollectedThisRun { get; private set; }
        static readonly System.Collections.Generic.HashSet<DreamStar> Stars = new System.Collections.Generic.HashSet<DreamStar>();
        public static int TotalInLevel => Stars.Count;
        public bool Collected { get; private set; }
        PlayerMotor3D _player;
        Vector3 _home;

        public static void ResetRun() { CollectedThisRun = 0; Stars.RemoveWhere(star => star == null); }

        void Awake() { _home = transform.position; Stars.Add(this); }
        void OnDestroy() { Stars.Remove(this); }
        void Start() => _player = FindFirstObjectByType<PlayerMotor3D>();

        void Update()
        {
            transform.Rotate(0, 120f * Time.deltaTime, 0, Space.World);
            transform.position = _home + Vector3.up * Mathf.Sin(Time.time * 2.2f) * 0.18f;
            if (Collected || _player == null) return;
            if ((_player.transform.position + Vector3.up * 0.6f - transform.position).sqrMagnitude < radius * radius) Collect();
        }

        public void Collect()
        {
            if (Collected) return;
            Collected = true; CollectedThisRun++;
            VFX.Play(VFXKind.StarBurst, transform.position, 1.6f, VFX.Gold);
            VFX.Play(VFXKind.PaperBurst, transform.position, 1f);
            Audio.AudioManager.Play(Audio.Sfx.UiClick, transform.position, 1f);
            UI.UIRoot.Get().Toast($"DREAM STAR!  {CollectedThisRun} / {TotalInLevel}", 1.8f);
            gameObject.SetActive(false);
        }
    }
}
