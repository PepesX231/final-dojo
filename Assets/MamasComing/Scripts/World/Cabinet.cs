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
                MamaRound.Award(p, item, "Hidden: " + item.displayName, 25);
                if (GameManager.I) GameManager.I.Toast("Stashed the " + item.displayName.ToLower() + " " + stashPhrase + ". Out of sight...");
                return;
            }
            if (hasDoor) IsOpen = !IsOpen;
        }

        // Carrying the burnt pot near a cabinet swings it open on its own (and lights it up) so players see
        // "this is where it can go"; walking away again closes it.
        public float potInviteRadius = 2.4f;
        bool autoOpened; Light glow;
        void Start()
        {
            var go = new GameObject("Pot invite light", typeof(Light)); go.transform.SetParent(transform, false);
            go.transform.position = InteractPoint + Vector3.up * .9f;
            glow = go.GetComponent<Light>(); glow.type = LightType.Point; glow.shadows = LightShadows.None; go.SetActive(false);
        }

        void Update()
        {
            if (NetGame.IsAuthority)
            {
                bool potNear = false;
                foreach (var pc in PlayerController.AllPlayers) if (pc && pc.Held is CookPot && Vector3.Distance(pc.transform.position, InteractPoint) < potInviteRadius) { potNear = true; break; }
                if (potNear && !IsOpen) { IsOpen = true; autoOpened = true; MamaAudio.Play("creak", transform.position, .4f, 1.2f); }
                else if (!potNear && autoOpened) { IsOpen = false; autoOpened = false; }
                if (IsOpen && !potNear) autoOpened = false;
            }
            if (glow) { glow.gameObject.SetActive(IsOpen && PlayerController.AllPlayers.Exists(pc => pc && pc.Held is CookPot && Vector3.Distance(pc.transform.position, InteractPoint) < potInviteRadius)); if (glow.gameObject.activeSelf) { glow.color = new Color(1f, .85f, .45f); glow.intensity = 1.2f + Mathf.Sin(Time.time * 5f) * .4f; glow.range = 2.2f; } }
            if (!doorPivot) return;
            float t = IsOpen ? openAngle : 0f;
            cur = Mathf.MoveTowards(cur, t, 420f * Time.deltaTime);
            doorPivot.localRotation = Quaternion.AngleAxis(cur, openAxis);
        }
    }
}
