using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DreamRush.Cards
{
    /// <summary>All card definitions (lives in Resources so builds can load it).</summary>
    [CreateAssetMenu(menuName = "DreamRush/Card Library", fileName = "CardLibrary")]
    public class CardLibrary : ScriptableObject
    {
        public List<CardDefinition> starterPool = new List<CardDefinition>();
        public List<CardDefinition> dreamLaws = new List<CardDefinition>();

        public const string ResourcePath = "DreamRush/CardLibrary";
        static CardLibrary _cached;

        public static CardLibrary Load()
        {
            if (_cached == null) _cached = Resources.Load<CardLibrary>(ResourcePath);
            return _cached;
        }

        public CardDefinition Find(string id) =>
            starterPool.Concat(dreamLaws).FirstOrDefault(c => c != null && c.cardId == id);
    }

    /// <summary>
    /// Controlled random draw (GDD §17.2–17.3): three DISTINCT cards from the curated starter pool, seeded.
    /// </summary>
    public static class CardDrawManager
    {
        public static List<CardDefinition> DrawThree(IList<CardDefinition> pool, int seed)
        {
            var rng = new System.Random(seed);
            var copy = pool.Where(c => c != null).Distinct().ToList();
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy.Take(Mathf.Min(3, copy.Count)).ToList();
        }
    }
}
