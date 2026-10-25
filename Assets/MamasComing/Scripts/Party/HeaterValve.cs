using UnityEngine;

namespace MamasComing
{
    /// Something a co-op label / the network can mark as "a friend is working this right now".
    public interface ICoopMark { void MarkHeld(PlayerController p); }

    /// One of the two water-heater valves. Owner: it could be done alone (turn one, run to the other).
    /// Now BOTH valves have to be held at the same time by TWO different kids until the pressure drops.
    public class HeaterValve : Interactable, ICoopMark
    {
        public WaterHeaterHazard heater;
        public Transform wheel;
        [System.NonSerialized] public float heldAt = -99f;
        [System.NonSerialized] public PlayerController heldBy;
        float spin;
        TextMesh label;

        public bool Held => heldBy && Time.time - heldAt < .25f;
        HeaterValve Other => !heater ? null : heater.valveA == this ? heater.valveB : heater.valveA;
        public bool FriendOnOther(PlayerController p) { var o = Other; return o && o.Held && o.heldBy != p; }

        public void MarkHeld(PlayerController p) { heldBy = p; heldAt = Time.time; }
        public override bool CanInteract(PlayerController p) => heater && heater.Urgent && p && !p.Held;
        public override float Priority(PlayerController p) => 0.7f;
        public override string GetPrompt(PlayerController p) =>
            FriendOnOther(p) ? (Loc.Thai ? "หมุนค้างไว้! เพื่อนหมุนอีกตัวอยู่" : "Keep turning! Your friend has the other valve")
                             : (Loc.Thai ? "กด E ค้าง หมุนวาล์ว - ต้องให้เพื่อนหมุนอีกตัวพร้อมกัน!" : "Hold E: turn valve - a friend must hold the OTHER one too!");
        public override float HoldTime(PlayerController p) => 9999f;   // a hold that only ends when you let go
        public override void HoldTick(PlayerController p, float t) => MarkHeld(p);
        public override void Interact(PlayerController p) { }

        // owner: no floating signs everywhere - the label only shows for the kid who is using this spot
        bool Mine() { var me = GameManager.I ? GameManager.I.player : null; return me && me.Target == this; }
        void LateUpdate()
        {
            bool show = heater && heater.Urgent && Mine();
            if (Held && heater && heater.Turning) spin += Time.deltaTime * 300f;
            else if (Held) spin += Mathf.Sin(Time.time * 30f) * 2f;   // stuck: it only jiggles alone
            if (wheel) wheel.localRotation = Quaternion.Euler(0, 0, spin);

            if (show && !label)
            {
                var go = new GameObject("Valve label", typeof(MeshRenderer), typeof(TextMesh)); go.transform.SetParent(transform, false);
                label = go.GetComponent<TextMesh>(); label.font = MamaTheme.Bold; label.GetComponent<MeshRenderer>().sharedMaterial = MamaTheme.Bold.material;
                label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.fontSize = 64; label.characterSize = .025f;
            }
            if (!label) return;
            if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
            if (!show) return;
            string n = heater.valveA == this ? "1" : "2";
            bool both = Held && Other && Other.Held && Other.heldBy != heldBy;
            label.text = both ? (Loc.Thai ? "หมุนพร้อมกัน! " + Mathf.RoundToInt(heater.Turn * 100) + "%" : "TOGETHER! " + Mathf.RoundToInt(heater.Turn * 100) + "%")
                       : Held ? (Loc.Thai ? "OK! วาล์ว " + n + "\nรอเพื่อนอีกวาล์ว" : "OK! valve " + n + "\nwaiting for a friend")
                       : (Loc.Thai ? "วาล์ว " + n + "\n(กด E ค้าง)" : "VALVE " + n + "\n(hold E)");
            label.color = both ? new Color(.45f, 1f, .55f) : Held ? new Color(1f, .9f, .4f) : new Color(1f, .5f, .8f);
            label.transform.position = transform.position + Vector3.up * (1.5f + Mathf.Sin(Time.time * 3f) * .06f);
            var cam = Camera.main; if (cam) label.transform.rotation = cam.transform.rotation;
        }
    }
}
