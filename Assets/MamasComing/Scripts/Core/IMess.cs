using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    public enum MessKind { Water, Smoke, Clutter, Evidence, Hazard }

    /// <summary>Anything in the house Mom could notice.</summary>
    public interface IMess
    {
        MessKind Kind { get; }
        /// <summary>Still a problem (whether visible or hidden).</summary>
        bool IsProblem { get; }
        /// <summary>The problem exists but is covered up / stashed ("faked").</summary>
        bool IsHidden { get; }
        int Severity { get; }
        string MomLine { get; }
        string Label { get; }
        Vector3 Position { get; }
    }

    public static class MessRegistry
    {
        public static readonly List<IMess> All = new List<IMess>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public static void Add(IMess m) { if (m != null && !All.Contains(m)) All.Add(m); }
        public static void Remove(IMess m) { All.Remove(m); }

        public static bool Alive(IMess m)
        {
            var c = m as Component;
            return c != null;
        }

        public static bool Visible(IMess m) => Alive(m) && m.IsProblem && !m.IsHidden;
    }
}
