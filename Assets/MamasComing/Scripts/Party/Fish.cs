using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// A goldfish that jumped out. Gets weaker out of water; save it in a bucket of water or the patched tank.
    public class Fish : Pickup, IMess
    {
        public AquariumHazard tank;
        public Transform homeInTank;
        public float breathSeconds = 75f;
        public float Breath { get; private set; } = 1f;
        public bool Out { get; private set; }
        public bool Saved { get; private set; }
        public bool Dead { get; private set; }
        public Renderer body;
        float flopT;

        protected override void OnEnable() { base.OnEnable(); MessRegistry.Add(this); }
        protected override void OnDisable() { base.OnDisable(); MessRegistry.Remove(this); }

        public void JumpOut()
        {
            if (Out || Saved) return;
            Out = true;
            Body.isKinematic = false; SetCols(true);
            transform.SetParent(null, true);
            Body.linearVelocity = new Vector3(Random.Range(-2f, 2f), 3f, Random.Range(-2.2f, -0.8f));
        }

        public override bool CanInteract(PlayerController p) => Out && !Saved && base.CanInteract(p);
        public override string GetPrompt(PlayerController p) => Dead ? (Loc.Thai ? "เก็บปลา... (ไม่ทันแล้ว)" : "Pick up the fish... (too late)") : (Loc.Thai ? "จับปลา! ต้องเอาลงน้ำ" : "Grab the fish! It needs water");

        void Update()
        {
            if (!Out || Saved || Dead) return;
            if (NetGame.IsAuthority && (GameManager.I == null || GameManager.I.SimRunning))
            {
                float pace = QuestDirector.I ? QuestDirector.I.Pace : 1f;
                Breath -= Time.deltaTime / breathSeconds * pace;
                if (Breath <= 0f) Die();
            }
            if (!IsHeld && !Dead && Body && !Body.isKinematic)
            {
                flopT -= Time.deltaTime;
                if (flopT <= 0f) { flopT = Random.Range(0.6f, 1.4f) * (0.5f + Breath); Body.AddForce(new Vector3(Random.Range(-1f, 1f), 2.2f * Breath + 0.5f, Random.Range(-1f, 1f)), ForceMode.VelocityChange); Body.AddTorque(Random.insideUnitSphere * 4f, ForceMode.VelocityChange); MamaAudio.Play("slap", transform.position, 0.35f, 1.4f); }
            }
        }

        void Die()
        {
            Dead = true;
            if (body) { var b = new MaterialPropertyBlock(); b.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.5f)); body.SetPropertyBlock(b); }
            if (QuestDirector.I) QuestDirector.I.Alarm(null, "Oh no... a goldfish didn't make it.", "ไม่นะ... ปลาทองตัวหนึ่งไม่รอดแล้ว");
        }

        public override void OnDropped(PlayerController p, Vector3 velocity)
        {
            base.OnDropped(p, velocity);
            if (Dead || !NetGame.IsAuthority) return;
            if (tank && tank.Patched && homeInTank && Vector3.Distance(Flat(transform.position), Flat(tank.transform.position)) < 1.6f) { SaveTo(homeInTank, true); return; }
            foreach (var b in FindObjectsByType<Bucket>(FindObjectsSortMode.None))
                if (b && b.liters > 1f && Vector3.Distance(Flat(transform.position), Flat(b.transform.position)) < 1.1f) { SaveTo(b.transform, false); return; }
        }

        public void SaveTo(Transform where, bool tankHome)
        {
            Saved = true; Out = false;
            Body.isKinematic = true; SetCols(false);
            transform.SetParent(where, false);
            transform.localPosition = tankHome ? Vector3.zero : new Vector3(Random.Range(-0.08f, 0.08f), 0.35f, Random.Range(-0.08f, 0.08f));
            transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            MamaAudio.Play("splash", transform.position, 0.6f, 1.5f); MamaAudio.Play("good", null, 0.6f, 1.3f);
            if (QuestDirector.I) QuestDirector.I.Cheer("Splash! The fish is safe!", "จ๋อม! ปลารอดแล้ว!");
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        public MessKind Kind => MessKind.Evidence;
        public bool IsProblem => Dead || (Out && !Saved);
        public bool IsHidden => HideZone.IsHidden(transform.position);
        public int Severity => Dead ? 3 : 2;
        public string MomLine => Dead ? (Loc.Thai ? "นั่น... ปลาทองของแม่?! ทำไมมันตาย?!" : "Is that... my GOLDFISH?! Why is it DEAD?!") : (Loc.Thai ? "ทำไมปลามาดิ้นอยู่บนพื้น?!" : "Why is a FISH flopping on my floor?!");
        public string Label => displayName;
        public Vector3 Position => transform.position;
    }
}
