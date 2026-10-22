using System.Collections.Generic;
using UnityEngine;

namespace DreamRush.Palace
{
    /// <summary>
    /// M17 NEON CIRCUIT: a grid of rotatable tiles between a GENERATOR (west edge of a row) and a SIGN (east edge of a
    /// row). Power flows through matching tube ends; when it reaches the sign the reward fires. Pure logic in
    /// <see cref="Flow"/> so it can be unit-tested.
    /// </summary>
    public class CircuitPuzzle : MonoBehaviour
    {
        public int cols = 3, rows = 3;
        public List<CircuitTile> tiles = new List<CircuitTile>();   // row-major, row 0 = south
        public int sourceRow, sinkRow;
        public Color color = new Color(0.2f, 0.95f, 1f);
        public PuzzleReward reward;
        public Renderer sinkLight;
        public bool Solved { get; private set; }

        void Start() => Trace();

        /// <summary>Returns the powered tile set (indices) and whether the sink is reached. N=1 E=2 S=4 W=8.</summary>
        public static bool Flow(int cols, int rows, int[] masks, int sourceRow, int sinkRow, HashSet<int> powered)
        {
            var q = new Queue<int>();
            int start = sourceRow * cols;
            if ((masks[start] & 8) == 0) return false;      // the generator feeds the west end of its row
            q.Enqueue(start); powered.Add(start);
            bool reached = false;
            while (q.Count > 0)
            {
                int i = q.Dequeue(); int r = i / cols, c = i % cols; int m = masks[i];
                if (c == cols - 1 && r == sinkRow && (m & 2) != 0) reached = true;
                (int dr, int dc, int bit, int opp)[] dirs = { (1, 0, 1, 4), (0, 1, 2, 8), (-1, 0, 4, 1), (0, -1, 8, 2) };
                foreach (var d in dirs)
                {
                    if ((m & d.bit) == 0) continue;
                    int nr = r + d.dr, nc = c + d.dc; if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                    int j = nr * cols + nc; if (powered.Contains(j) || (masks[j] & d.opp) == 0) continue;
                    powered.Add(j); q.Enqueue(j);
                }
            }
            return reached;
        }

        public void Trace()
        {
            if (tiles.Count != cols * rows) return;
            var masks = new int[tiles.Count]; for (int i = 0; i < masks.Length; i++) masks[i] = tiles[i].Mask;
            var on = new HashSet<int>();
            bool done = Flow(cols, rows, masks, sourceRow, sinkRow, on);
            for (int i = 0; i < tiles.Count; i++) tiles[i].SetPowered(on.Contains(i), color);
            if (sinkLight != null) { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Emission", done ? color * 3f : Color.black); sinkLight.SetPropertyBlock(mpb); }
            if (done && !Solved)
            {
                Solved = true;
                foreach (var t in tiles) t.interactable = false;
                if (reward != null) reward.Solve();
            }
        }
    }
}
