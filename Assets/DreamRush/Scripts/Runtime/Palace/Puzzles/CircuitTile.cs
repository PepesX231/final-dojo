using DreamRush.Combat;
using DreamRush.Interaction;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 NEON CIRCUIT tile: a floor tile carrying a neon tube (straight / corner / tee). Punch it (or press E) and it
    /// turns 90°; the puzzle re-traces power from the generator every turn and lights the powered tubes.
    /// Connections are a 4-bit mask N=1 E=2 S=4 W=8 before rotation.
    /// </summary>
    public class CircuitTile : Interactable, IHittable
    {
        public int baseMask = 5;         // straight N-S
        public int turns;                // 0..3 clockwise
        public Transform visual;
        public Renderer[] tubes = { };
        public CircuitPuzzle puzzle;
        public bool Powered { get; private set; }
        float _yaw, _cool;
        static MaterialPropertyBlock _mpb;

        public int Mask { get { int m = baseMask; for (int i = 0; i < turns % 4; i++) m = ((m << 1) | (m >> 3)) & 15; return m; } }

        protected override void OnEnable() { base.OnEnable(); prompt = "ROTATE"; }
        void Start() { _yaw = turns * 90f; if (visual != null) visual.localRotation = Quaternion.Euler(0, _yaw, 0); }

        protected override void OnInteract(InteractionContext context) => Turn();
        public bool TakeHit(HitInfo hit) { if (Time.time < _cool) return false; Turn(); return true; }

        public void Turn()
        {
            if (puzzle != null && puzzle.Solved) return;
            _cool = Time.time + 0.2f;
            turns = (turns + 1) % 4;
            Audio.AudioManager.Play(Audio.Sfx.Clank, transform.position, 0.5f);
            if (puzzle != null) puzzle.Trace();
        }

        public void SetPowered(bool on, Color c)
        {
            Powered = on;
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in tubes)
            {
                if (r == null) continue;
                _mpb.Clear(); _mpb.SetColor("_Emission", on ? c * 2.4f : c * 0.12f); _mpb.SetColor("_BaseColor", on ? c : c * 0.35f); r.SetPropertyBlock(_mpb);
            }
        }

        void Update()
        {
            float target = turns * 90f;
            _yaw = Mathf.MoveTowardsAngle(_yaw, target, Time.deltaTime * 720f);
            if (visual != null) visual.localRotation = Quaternion.Euler(0, _yaw, 0);
        }
    }
}
