using UnityEngine;

namespace MamasComing
{
    /// <summary>Openable cabinet / hiding spot (wardrobe, washer, under the bed...). Open + holding item = stash it.</summary>
    public class Cabinet : Interactable
    {
        public bool hasDoor = true;
        public Transform doorPivot;
        public Vector3 openAxis = Vector3.up;
        public float openAngle = 100f;
        public HideZone zone;
        public Transform stashPoint;
        public string stashPhrase = "in the cabinet";

        public bool IsOpen { get; private set; }
        float cur;

        public void SetOpen(bool o) { IsOpen = o; }

        bool CanStash(PlayerController p) => p.Held != null && !p.Held.dragOnFloor;

        public override bool CanInteract(PlayerController p) => hasDoor || CanStash(p);

        public override string GetPrompt(PlayerController p)
        {
            if (CanStash(p) && (!hasDoor || IsOpen))
                return "Stash the " + p.Held.displayName.ToLower() + " " + stashPhrase;
            return (IsOpen ? "Close the " : "Open the ") + displayName.ToLower();
        }

        public override void Interact(PlayerController p)
        {
            MamaAudio.Play("door", transform.position, 0.45f, 1.5f);
            if (CanStash(p) && (!hasDoor || IsOpen))
            {
                var item = p.ReleaseHeld();
                Vector3 pos = stashPoint ? stashPoint.position : transform.position;
                pos += new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.08f, 0.08f));
                item.Stash(pos);
                if (GameManager.I) GameManager.I.Toast("Stashed the " + item.displayName.ToLower() + " " + stashPhrase + ". Out of sight...");
                return;
            }
            if (hasDoor) IsOpen = !IsOpen;
        }

        void Update()
        {
            if (!doorPivot) return;
            float t = IsOpen ? openAngle : 0f;
            cur = Mathf.MoveTowards(cur, t, 420f * Time.deltaTime);
            doorPivot.localRotation = Quaternion.AngleAxis(cur, openAxis);
        }
    }
}
