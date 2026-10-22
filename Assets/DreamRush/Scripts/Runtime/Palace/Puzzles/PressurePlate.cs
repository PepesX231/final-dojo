using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17: a glowing floor plate; pressed by a crate sitting on it (or by Pip if <see cref="playerCounts"/>).</summary>
    public class PressurePlate : MonoBehaviour
    {
        public float radius = 1.3f;
        public bool playerCounts;
        public Renderer glow;
        public bool Pressed { get; private set; }
        Transform _player;

        void Start() { var p = FindFirstObjectByType<Player.PlayerMotor3D>(); _player = p != null ? p.transform : null; Paint(); }

        void Update()
        {
            bool on = false;
            foreach (var c in FindObjectsByType<PushCrate>(FindObjectsSortMode.None))
            {
                Vector3 d = c.transform.position - transform.position; d.y = 0f;
                if (d.magnitude < radius && Mathf.Abs(c.transform.position.y - transform.position.y) < 2.5f) { on = true; break; }
            }
            if (!on && playerCounts && _player != null) { Vector3 d = _player.position - transform.position; d.y = 0f; on = d.magnitude < radius && Mathf.Abs(_player.position.y - transform.position.y) < 1f; }
            if (on != Pressed) { Pressed = on; Paint(); if (on) Audio.AudioManager.Play(Audio.Sfx.UiOpen, transform.position, 0.7f); }
        }

        void Paint()
        {
            if (glow == null) return;
            var mpb = new MaterialPropertyBlock(); var c = Pressed ? new Color(0.3f, 1f, 0.6f) : new Color(1f, 0.35f, 0.4f);
            mpb.SetColor("_BaseColor", c); mpb.SetColor("_Emission", c * (Pressed ? 2.5f : 1f)); glow.SetPropertyBlock(mpb);
        }
    }
}
