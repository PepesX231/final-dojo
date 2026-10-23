using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    // ---------------------------------------------------------------------- BEDROOM: party guest let the hamster out
    /// Runs around the house, flees from players, chews cables (power cut) if left loose. Put it back in its cage.
    public class Hamster : MessyProp
    {
        public Vector3[] nodes;
        public Vector2Int[] links;
        public float speed = 2.1f, fleeSpeed = 4.3f;
        public float chewEvery = 70f;
        int target = -1, current;
        float chewT, squeakT, pauseT;
        public bool Loose => !Settled && !IsHeld;
        public override bool CanSettle => true;

        protected override void Awake() { base.Awake(); Body.isKinematic = true; }
        void Start() { current = Nearest(transform.position); chewT = chewEvery; }

        public override bool CanInteract(PlayerController p) => base.CanInteract(p) && Vector3.Distance(p.transform.position, transform.position) < 1.25f;
        public override string GetPrompt(PlayerController p) => Loc.Thai ? "จับหนูแฮมสเตอร์!" : "Catch the hamster!";
        public override void OnPickedUp(PlayerController p) { base.OnPickedUp(p); MamaAudio.Play("squeakhi", transform.position, 0.7f, 1.2f); if (QuestDirector.I) QuestDirector.I.Cheer("Got it! Now back in the cage (bedroom)!", "จับได้แล้ว! เอาไปคืนกรง (ห้องนอน)!"); }
        public override void OnDropped(PlayerController p, Vector3 velocity)
        {
            base.OnDropped(p, velocity);
            if (!Settled) { Body.isKinematic = true; transform.rotation = Quaternion.identity; Vector3 q = transform.position; q.y = HouseLayout.FloorYAt(q) + 0.02f; transform.position = q; current = Nearest(q); target = -1; pauseT = 1.5f; }
        }

        void Update()
        {
            if (!Loose || nodes == null || nodes.Length == 0) return;
            if (!NetGame.IsAuthority) return;
            bool sim = GameManager.I == null || GameManager.I.SimRunning;
            if (!sim) return;
            float dt = Time.deltaTime;
            squeakT -= dt; if (squeakT <= 0f) { squeakT = Random.Range(3f, 7f); MamaAudio.Play("squeakhi", transform.position, 0.35f, Random.Range(1f, 1.4f)); }
            chewT -= dt * (QuestDirector.I ? QuestDirector.I.Pace : 1f);
            if (chewT <= 0f) { chewT = chewEvery; if (PowerGrid.On) PowerGrid.Trip("The hamster CHEWED through a cable! Power's out!", "หนูแฮมสเตอร์กัดสายไฟขาด! ไฟดับ!"); }
            if (pauseT > 0f) { pauseT -= dt; return; }

            PlayerController near = null; float nd = 99f;
            foreach (var pc in PlayerController.AllPlayers) { if (!pc) continue; float d = Vector3.Distance(pc.transform.position, transform.position); if (d < nd) { nd = d; near = pc; } }
            bool flee = near && nd < 2.8f;
            if (target < 0 || (flee && Vector3.Dot(nodes[target] - transform.position, near.transform.position - transform.position) > 0f)) target = PickNext(flee ? near.transform.position : (Vector3?)null);
            Vector3 to = nodes[target] - transform.position; to.y = 0f;
            if (to.magnitude < 0.15f) { current = target; target = -1; if (!flee && Random.value < 0.3f) pauseT = Random.Range(0.5f, 1.8f); return; }
            Vector3 step = to.normalized * (flee ? fleeSpeed : speed) * dt;
            if (step.magnitude > to.magnitude) step = to;
            transform.position += step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized), dt * 12f);
            // nodes carry their floor height: on the stairs the height follows the segment
            Vector3 seg = nodes[target] - nodes[current]; seg.y = 0f;
            float along = seg.sqrMagnitude > 1e-4f ? Mathf.Clamp01(1f - (to.magnitude - step.magnitude) / seg.magnitude) : 1f;
            float floorY = Mathf.Lerp(nodes[current].y, nodes[target].y, along);
            transform.position = new Vector3(transform.position.x, floorY + 0.02f + Mathf.Abs(Mathf.Sin(Time.time * 22f)) * 0.02f, transform.position.z);
        }

        int Nearest(Vector3 p) { int b = 0; float bd = 1e9f; for (int i = 0; i < nodes.Length; i++) { float d = (nodes[i] - p).sqrMagnitude; if (d < bd) { bd = d; b = i; } } return b; }
        int PickNext(Vector3? away)
        {
            var opts = new List<int>();
            foreach (var l in links) { if (l.x == current) opts.Add(l.y); else if (l.y == current) opts.Add(l.x); }
            if (opts.Count == 0) return Nearest(transform.position);
            if (away.HasValue) { int best = opts[0]; float bd = -1f; foreach (var o in opts) { float d = (nodes[o] - away.Value).sqrMagnitude; if (d > bd) { bd = d; best = o; } } return best; }
            return opts[Random.Range(0, opts.Count)];
        }

        public override bool IsProblem => !Settled;
        public override string MomLine => Loc.Thai ? "ทำไมหนูแฮมสเตอร์ออกมาวิ่งเล่นนอกกรง?!" : "Why is the HAMSTER running loose in my house?!";
    }
}
