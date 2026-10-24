using UnityEngine;

namespace MamasComing
{
    /// One "handle" of a co-op puzzle. Every station must be worked by a DIFFERENT player at the same time.
    public class CoopStation : Interactable
    {
        public CoopPuzzle puzzle;
        public int index;
        public override bool CanInteract(PlayerController p) => puzzle && puzzle.Urgent && p && !p.Held;
        public override float Priority(PlayerController p) => 0.7f;
        public override string GetPrompt(PlayerController p) => puzzle ? puzzle.StationPrompt(this, p) : "";
        public override float HoldTime(PlayerController p) => puzzle && puzzle.holdMode ? 9999f : 0f;
        public override void HoldTick(PlayerController p, float t) { if (puzzle) puzzle.Holding(this, p); }
        public override void Interact(PlayerController p) { if (puzzle) puzzle.Pressed(this, p); }
    }
}
