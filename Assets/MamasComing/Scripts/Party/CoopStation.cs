using UnityEngine;

namespace MamasComing
{
    /// One "handle" of a co-op puzzle. Every station must be worked by a DIFFERENT player at the same time.
    public class CoopStation : Interactable, ICoopMark
    {
        public CoopPuzzle puzzle;
        public int index;
        public override bool CanInteract(PlayerController p) => puzzle && puzzle.Urgent && p && !p.Held;
        public override float Priority(PlayerController p) => 0.7f;
        public override string GetPrompt(PlayerController p) => puzzle ? puzzle.StationPrompt(this, p) : "";
        public override float HoldTime(PlayerController p) => puzzle && puzzle.StationHolds(this) ? 9999f : 0f;
        public void MarkHeld(PlayerController p) { if (puzzle) puzzle.Seen(this, p); }
        public override void HoldTick(PlayerController p, float t) { if (puzzle) puzzle.Holding(this, p); }
        public override void Interact(PlayerController p) { if (puzzle) puzzle.Pressed(this, p); }

        // floating marker over the spot: which spot this is, and whether someone is on it yet
        TextMesh label;
        // owner: no floating signs everywhere - the label only shows for the kid who is using this spot
        bool Mine() { var me = GameManager.I ? GameManager.I.player : null; return me && me.Target == this; }
        void LateUpdate()
        {
            bool show = puzzle && puzzle.Urgent && Mine();
            if (show && !label)
            {
                var go = new GameObject("Station label", typeof(MeshRenderer), typeof(TextMesh)); go.transform.SetParent(transform, false);
                label = go.GetComponent<TextMesh>(); label.font = MamaTheme.Bold; label.GetComponent<MeshRenderer>().sharedMaterial = MamaTheme.Bold.material;
                label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.fontSize = 64; label.characterSize = .025f;
            }
            if (!label) return;
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (!show) return;
            bool on = puzzle.IsManned(index);
            string spot = (index + 1).ToString();
            string custom = puzzle.StationLabel(this, out var cc);
            if (custom != null) { label.text = custom; label.color = cc; }
            else
            {
                label.text = on ? "OK! " + spot : (Loc.Thai ? "ยืนตรงนี้ " + spot + "\n(กด E)" : "STAND HERE " + spot + "\n(E)");
                label.color = on ? new Color(.45f, 1f, .55f) : new Color(1f, .5f, .8f);
            }
            var cam = Camera.main;
            label.transform.position = transform.position + Vector3.up * (1.35f + Mathf.Sin(Time.time * 3f + index) * .06f);
            if (cam) label.transform.rotation = cam.transform.rotation;
        }
    }
}
