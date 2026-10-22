using System;
using System.Collections.Generic;
using DreamRush.Player;
using UnityEngine;

namespace DreamRush.Dream
{
    /// <summary>
    /// Per-dream level rules: checkpoints, falling into the dream void (respawn + penalty event), the current
    /// section and its objective text (GDD §13). Distance-based checks — no trigger callbacks to miss.
    /// </summary>
    public class DreamLevelManager : MonoBehaviour
    {
        public static DreamLevelManager Current { get; private set; }

        public float killY = -6f;
        [Tooltip("Open world: the most recently touched checkpoint wins, whatever its order.")]
        public bool anyOrderCheckpoints;
        public PlayerMotor3D player;

        public Checkpoint LastCheckpoint { get; private set; }
        public DreamSection CurrentSection { get; private set; }
        public int FallCount { get; private set; }

        /// <summary>Raised after a fall respawn (Sleep Stability listens, M5).</summary>
        public event Action Fell;
        public event Action<Checkpoint> CheckpointReached;
        public event Action<DreamSection> SectionChanged;

        static readonly List<Checkpoint> Checkpoints = new List<Checkpoint>();
        static readonly List<DreamSection> Sections = new List<DreamSection>();
        public static void Register(Checkpoint c) { if (!Checkpoints.Contains(c)) Checkpoints.Add(c); }
        public static void Unregister(Checkpoint c) => Checkpoints.Remove(c);
        public static void Register(DreamSection s) { if (!Sections.Contains(s)) Sections.Add(s); }
        public static void Unregister(DreamSection s) => Sections.Remove(s);

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerMotor3D>();
            Checkpoint first = null;
            foreach (var c in Checkpoints) if (c.isStart) first = c;
            LastCheckpoint = first;
        }

        void Update()
        {
            if (player == null) return;
            Vector3 p = player.transform.position;

            foreach (var c in Checkpoints)
            {
                if (c == LastCheckpoint || (!anyOrderCheckpoints && c.order <= (LastCheckpoint ? LastCheckpoint.order : int.MinValue))) continue;
                if ((p - c.transform.position).sqrMagnitude <= c.radius * c.radius)
                {
                    LastCheckpoint = c;
                    CheckpointReached?.Invoke(c);
                }
            }

            DreamSection section = null;
            foreach (var s in Sections) if (s.Contains(p) && (section == null || s.order > section.order)) section = s;
            if (section != null && section != CurrentSection)
            {
                CurrentSection = section;
                SectionChanged?.Invoke(section);
            }

            if (p.y < killY) Respawn();
        }

        /// <summary>Make <paramref name="c"/> the respawn point now (café rest, story beats).</summary>
        public void ForceCheckpoint(Checkpoint c) { if (c == null) return; LastCheckpoint = c; CheckpointReached?.Invoke(c); }

        public void Respawn()
        {
            FallCount++;
            Transform t = LastCheckpoint != null ? LastCheckpoint.transform : transform;
            player.Teleport(t.position + Vector3.up * 0.1f, Quaternion.Euler(0, t.eulerAngles.y, 0));
            Fell?.Invoke();
        }
    }
}
