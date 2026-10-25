using System;
using System.Collections.Generic;
using UnityEngine;
namespace MamasComing
{
    [Serializable] public class MamaScore { public int slot, fruit, score, tasks; public List<string> completed = new List<string>(); }
    [Serializable] public class MamaJobLog { public int slot, points; public string task; }
    [Serializable] public class MamaRoundData
    {
        public bool ended, won; public string title, body, stats, reason;
        /// Outcome for the score summary (always shown, win or lose): 0 perfect, 1 almost clean, 2 suspicious, 3 busted, 4 game over (fire trucks...).
        public int grade, fixedN, foundN, hiddenN; public List<string> foundTop = new List<string>();
        public Vector3 cause; public List<MamaScore> players = new List<MamaScore>();
        public int[] votes = { 0, 0, 0 };
        /// Every credited job in the order it happened (for the results bar-graph replay).
        public List<MamaJobLog> log = new List<MamaJobLog>();
    }
    public static class MamaRound
    {
        public static MamaRoundData Data = new MamaRoundData();
        static readonly HashSet<int> credited = new HashSet<int>();
        public static void Begin() { Data = new MamaRoundData(); credited.Clear(); CapturePlayers(); }
        public static void ClearVotes() { Data.votes = new int[3]; }
        public static void CapturePlayers()
        {
            for (int s = 0; s < 3; s++)
            {
                if (NetGame.Online && NetSession.I && !NetSession.I.SlotTaken(s)) continue;
                if (!NetGame.Online && s > 0) continue;
                var row = Data.players.Find(p => p.slot == s);
                if (row == null) { row = new MamaScore { slot = s }; Data.players.Add(row); }
                row.fruit = NetSession.I ? NetSession.I.FruitFor(s) : KidLook.Selected;
            }
        }
        public static void Award(PlayerController player, UnityEngine.Object source, string task, int points)
        {
            if (!NetGame.IsAuthority || !player || !source || Data.ended || !GameManager.I || !GameManager.I.Live || !credited.Add(source.GetInstanceID())) return;
            CapturePlayers(); int slot = NetSession.I ? NetSession.I.SlotFor(player) : 0;
            var row = Data.players.Find(p => p.slot == slot); if (row == null) return;
            row.score += points; row.tasks++; row.completed.Add(task);
            Data.log.Add(new MamaJobLog { slot = slot, points = points, task = task });
        }
        public static void Finish(bool won, string title, string body, string stats, Vector3 cause, string reason)
        {
            CapturePlayers(); Data.ended = true; Data.won = won; Data.title = title; Data.body = body; Data.stats = stats; Data.cause = cause; Data.reason = reason; ClearVotes();
        }
        public static List<MamaScore> Ranked()
        {
            var rows = new List<MamaScore>(Data.players);
            rows.Sort((a,b) => a.score != b.score ? b.score.CompareTo(a.score) : a.slot.CompareTo(b.slot)); return rows;
        }
        // Strict majority, recalculated against connected players. A tie waits for a changed vote.
        public static int WinningVote(int[] votes, IList<int> connected)
        {
            int needed = connected.Count / 2 + 1;
            for (int choice = 1; choice <= 2; choice++) { int count = 0; foreach (int s in connected) if (s >= 0 && s < votes.Length && votes[s] == choice) count++; if (count >= needed) return choice; }
            return 0;
        }
    }
}
