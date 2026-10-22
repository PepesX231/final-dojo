using DreamRush.Core;
using DreamRush.Flow;
using UnityEngine;

namespace DreamRush.UI
{
    /// <summary>Shown in the bedroom after every dream (GDD §22.3–22.4).</summary>
    public class RunSummaryPanel : ModalPanel
    {
        public static RunSummaryPanel Show(RunState run)
        {
            var p = Create<RunSummaryPanel>(run.Won ? "SWEET DREAMS" : "MOM WOKE YOU UP", new Vector2(1000, 600));
            float mins = run.ElapsedSeconds / 60f;
            string method = run.Won ? MethodTitle(run.VictoryId) : (string.IsNullOrEmpty(run.EndReason) ? "-" : run.EndReason);
            p.Paragraph(run.Won ? "Dream complete! You slept like a log." : "The dream folded away before you could finish it.", 28, TextAnchor.MiddleCenter);
            p.Paragraph($"{(run.Won ? "Solution" : "Reason")}: {method}", 28, TextAnchor.MiddleCenter);
            p.Paragraph($"Dream Memory  +{run.DreamMemoryEarned}   (total {SaveManager.GetOrCreate().Data.progression.dreamMemory})", 28, TextAnchor.MiddleCenter);
            string mastery = run.NewPersonalBest ? "  — NEW PERSONAL BEST!" : "";
            var record = SaveManager.GetOrCreate().Data.progression.FindLevelRecord(run.LevelScene);
            if (run.Won && record != null && !run.NewPersonalBest) mastery = $"  — Best: {record.bestSeconds / 60f:0.0} min";
            p.Paragraph($"Dream time: {mins:0.0} min{mastery}", 24, TextAnchor.MiddleCenter);
            if (run.LevelScene == "Dream_World1" || run.LevelScene == "Dream_Openworld") p.Paragraph($"Dream Stars: {run.DreamStars}  — replay to find the hidden routes", 24, TextAnchor.MiddleCenter);
            UIRoot.Get().Push(p);
            return p;
        }

        static string MethodTitle(string id)
        {
            foreach (var v in DreamDiaryPanel.ChairVictories) if (v.id == id) return v.title;
            return string.IsNullOrEmpty(id) ? "-" : id;
        }
    }
}
