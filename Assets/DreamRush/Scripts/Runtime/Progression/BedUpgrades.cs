using System;
using DreamRush.Core;

namespace DreamRush.Progression
{
    public enum BedUpgrade { CloudMattress = 0, ComfortBlanket = 2 }

    /// <summary>
    /// Permanent bed progression (GDD §21.3 first-playable scope). Pure rules: costs, caps, effects. Purchases
    /// deduct Dream Memory exactly once and are saved by the caller.
    /// </summary>
    public static class BedUpgrades
    {
        public readonly struct Info
        {
            public readonly BedUpgrade Id; public readonly string Name, Effect; public readonly int[] Costs;
            public Info(BedUpgrade id, string name, string effect, int[] costs) { Id = id; Name = name; Effect = effect; Costs = costs; }
            public int MaxLevel => Costs.Length;
        }

        public static readonly Info[] All =
        {
            new Info(BedUpgrade.CloudMattress, "CLOUD MATTRESS", "+1 max Sleep Stability per level", new[] { 20, 35, 55 }),
            new Info(BedUpgrade.ComfortBlanket, "COMFORT BLANKET", "the first bump of each dream is absorbed", new[] { 25 }),
        };

        public static Info Get(BedUpgrade id) => Array.Find(All, i => i.Id == id);

        public static int Level(ProgressionData p, BedUpgrade id) => id switch
        {
            BedUpgrade.CloudMattress => p.mattressLevel,
            _ => p.blanketLevel
        };

        static void SetLevel(ProgressionData p, BedUpgrade id, int v)
        {
            switch (id)
            {
                case BedUpgrade.CloudMattress: p.mattressLevel = v; break;
                default: p.blanketLevel = v; break;
            }
        }

        /// <summary>Cost of the next level, or -1 when maxed.</summary>
        public static int NextCost(ProgressionData p, BedUpgrade id)
        {
            var info = Get(id);
            int lv = Level(p, id);
            return info.Costs == null || lv < 0 || lv >= info.MaxLevel ? -1 : info.Costs[lv];
        }

        public static bool CanBuy(ProgressionData p, BedUpgrade id)
        {
            int c = NextCost(p, id);
            return c >= 0 && p.dreamMemory >= c;
        }

        public static bool TryBuy(ProgressionData p, BedUpgrade id)
        {
            if (p == null || !CanBuy(p, id)) return false;
            p.dreamMemory -= NextCost(p, id);
            SetLevel(p, id, Level(p, id) + 1);
            return true;
        }

        // ---- effects used by the dream
        public static int BonusStability(ProgressionData p) => Math.Max(0, Math.Min(p.mattressLevel, Get(BedUpgrade.CloudMattress).MaxLevel));
        public static int StartShields(ProgressionData p) => p.blanketLevel > 0 ? 1 : 0;
    }
}
