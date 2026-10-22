using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>M17: all plates pressed at once → the reward fires (and stays solved).</summary>
    public class PlateGroup : MonoBehaviour
    {
        public List<PressurePlate> plates = new List<PressurePlate>();
        public PuzzleReward reward;
        public bool Solved { get; private set; }
        public int PressedCount { get; private set; }

        void Update()
        {
            if (Solved) return;
            int n = 0; foreach (var p in plates) if (p != null && p.Pressed) n++;
            PressedCount = n;
            if (n == plates.Count && n > 0) { Solved = true; if (reward != null) reward.Solve(); }
        }
    }
}
