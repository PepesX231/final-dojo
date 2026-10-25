using UnityEngine;

namespace MamasComing
{
    /// A problem only friends can solve together (2+ players): every CoopStation needs its own player.
    /// It only appears in online games with at least two players (QuestDirector never schedules it solo).
    public abstract class CoopPuzzle : Hazard
    {
        public CoopStation[] stations;
        [Tooltip("Hold E on the stations (true) or press E together on a beat (false).")]
        public bool holdMode = true;
        [Tooltip("Seconds into the round before it can start (scaled like other problems).")]
        public float appearAt = 150f;
        /// Dev / presentation: allow it in a solo game too.
        public static bool AllowSolo;

        protected PlayerController[] who; protected float[] seen;
        protected int Count { get { int n = 0; for (int i = 0; i < who.Length; i++) if (who[i] && Time.time - seen[i] < .2f) n++; return n; } }
        protected bool AllManned { get { if (who == null) return false; for (int i = 0; i < who.Length; i++) { if (!who[i] || Time.time - seen[i] > .2f) return false; for (int j = 0; j < i; j++) if (who[j] == who[i]) return false; } return true; } }

        protected override void Start()
        {
            coop = true;
            if (stations == null) stations = new CoopStation[0];
            who = new PlayerController[stations.Length]; seen = new float[stations.Length];
            for (int i = 0; i < stations.Length; i++) if (stations[i]) { stations[i].puzzle = this; stations[i].index = i; }
            base.Start();
        }

        public override bool CanInteract(PlayerController p) => false;   // you work the stations, not the prop itself

        protected override void Update()
        {
            base.Update();
            if (!Active && !Fixed && NetGame.IsAuthority && GameManager.I && GameManager.I.state == GameManager.State.Playing && QuestDirector.I
                && (NetGame.PlayerCount >= 2 || AllowSolo) && QuestDirector.I.Elapsed >= appearAt / Mathf.Max(.6f, QuestDirector.I.Pace) * .85f)
                Activate();
        }

        public virtual void Holding(CoopStation s, PlayerController p) { if (s.index < who.Length) { who[s.index] = p; seen[s.index] = Time.time; } }
        public virtual void Pressed(CoopStation s, PlayerController p) { if (s.index < who.Length) { who[s.index] = p; seen[s.index] = Time.time; } }
        public abstract string StationPrompt(CoopStation s, PlayerController p);
        /// Does this station want E held (true) or tapped (false)? Puzzles can mix both (one holds, one works).
        public virtual bool StationHolds(CoopStation s) => holdMode;
        /// Floating text over a station (null = the default "STAND HERE n").
        public virtual string StationLabel(CoopStation s, out Color c) { c = Color.white; return null; }
        /// Display-only mark from the network: a friend is holding this station (no game logic).
        public void Seen(CoopStation s, PlayerController p) { if (who != null && s.index < who.Length) { who[s.index] = p; seen[s.index] = Time.time; } }
        /// Is somebody else on any other station right now?
        public bool FriendOnOther(CoopStation s, PlayerController p) => who != null && OtherManned(s, p);
        public bool IsManned(int i) => who != null && i >= 0 && i < who.Length && who[i] && Time.time - seen[i] < .25f;

        /// Somebody's on the OTHER station already?
        protected bool OtherManned(CoopStation s, PlayerController p) { for (int i = 0; i < who.Length; i++) if (i != s.index && who[i] && who[i] != p && Time.time - seen[i] < .25f) return true; return false; }

        protected static void Boing(PlayerController p, Vector3 from, string en, string th, float force = 4.5f)
        {
            if (!p) return;
            Vector3 d = p.transform.position - from; d.y = 0f;
            NetGame.Hit(p, d, force, 1.2f, true, from);
            NetGame.Pop(p.transform.position + Vector3.up * 1.4f, en, th, new Color(1f, .6f, .85f), "thud", .9f, 1.2f);
        }
    }
}
