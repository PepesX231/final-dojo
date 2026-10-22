using System.Collections.Generic;
using DreamRush.Palace;
using NUnit.Framework;

namespace DreamRush.Tests
{
    /// <summary>M17: the neon-circuit power trace (pure logic).</summary>
    public class CircuitFlowTests
    {
        // N=1 E=2 S=4 W=8; row 0 = south; source enters row 0 from the west, sink exits row 2 to the east
        static readonly int[] Solved = { 9, 5, 3, 6, 9, 10, 12, 6, 10 };

        [Test] public void SolvedLayout_ReachesTheSign()
        {
            var on = new HashSet<int>();
            Assert.IsTrue(CircuitPuzzle.Flow(3, 3, Solved, 0, 2, on));
            CollectionAssert.IsSupersetOf(on, new[] { 0, 3, 4, 7, 8 });
        }

        [Test] public void OneTileTurned_BreaksThePath()
        {
            var m = (int[])Solved.Clone(); m[4] = 6;   // the middle corner faces the wrong way
            Assert.IsFalse(CircuitPuzzle.Flow(3, 3, m, 0, 2, new HashSet<int>()));
        }

        [Test] public void NoWestInput_NoPower()
        {
            var m = (int[])Solved.Clone(); m[0] = 5;
            var on = new HashSet<int>();
            Assert.IsFalse(CircuitPuzzle.Flow(3, 3, m, 0, 2, on));
            Assert.That(on.Count, Is.EqualTo(0));
        }
    }
}
