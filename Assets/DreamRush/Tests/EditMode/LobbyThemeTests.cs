using System.Linq;
using DreamRush.Hub;
using NUnit.Framework;
using UnityEngine;

namespace DreamRush.Tests
{
    /// <summary>The lobby's look follows the real clock (docs/LOBBY_DESIGN.md §3).</summary>
    public class LobbyThemeTests
    {
        [TearDown] public void TearDown() => LobbyTheme.OverrideId = "";

        [Test] // LOB-01 every hour maps to the right mood
        public void ForHour_MapsTheFourMoods()
        {
            Assert.That(LobbyTheme.ForHour(0), Is.SameAs(LobbyTheme.Midnight));
            Assert.That(LobbyTheme.ForHour(4), Is.SameAs(LobbyTheme.Midnight));
            Assert.That(LobbyTheme.ForHour(5), Is.SameAs(LobbyTheme.Morning));
            Assert.That(LobbyTheme.ForHour(10), Is.SameAs(LobbyTheme.Morning));
            Assert.That(LobbyTheme.ForHour(11), Is.SameAs(LobbyTheme.Afternoon));
            Assert.That(LobbyTheme.ForHour(16), Is.SameAs(LobbyTheme.Afternoon));
            Assert.That(LobbyTheme.ForHour(17), Is.SameAs(LobbyTheme.Sunset));
            Assert.That(LobbyTheme.ForHour(19), Is.SameAs(LobbyTheme.Sunset));
            Assert.That(LobbyTheme.ForHour(20), Is.SameAs(LobbyTheme.Midnight));
            Assert.That(LobbyTheme.ForHour(23), Is.SameAs(LobbyTheme.Midnight));
            Assert.That(LobbyTheme.ForHour(-1), Is.SameAs(LobbyTheme.Midnight));
            for (int h = 0; h < 24; h++) Assert.IsNotNull(LobbyTheme.ForHour(h));
        }

        [Test] // LOB-02 four genuinely different palettes (never the same red/black), readable ink on primary
        public void Themes_AreDistinctAndReadable()
        {
            var all = LobbyTheme.All;
            Assert.That(all.Select(t => t.id).Distinct().Count(), Is.EqualTo(4));
            for (int i = 0; i < all.Length; i++)
                for (int j = i + 1; j < all.Length; j++)
                {
                    float d = Mathf.Abs(all[i].primary.r - all[j].primary.r) + Mathf.Abs(all[i].primary.g - all[j].primary.g) + Mathf.Abs(all[i].primary.b - all[j].primary.b);
                    Assert.That(d, Is.GreaterThan(0.4f), $"{all[i].id} vs {all[j].id} primaries too similar");
                }
            foreach (var t in all)
            {
                Assert.That(Contrast(t.primary, t.ink), Is.GreaterThan(4.5f), t.id + " ink on primary");
                Assert.That(Contrast(t.paper, t.ink), Is.GreaterThan(7f), t.id + " ink on paper");
            }
            Assert.That(all.Count(t => t.night), Is.EqualTo(1));
        }

        [Test] // LOB-03 dev override wins over the clock, "" goes back to the clock
        public void Override_TakesPrecedence()
        {
            LobbyTheme.OverrideId = "sunset";
            Assert.That(LobbyTheme.Current, Is.SameAs(LobbyTheme.Sunset));
            LobbyTheme.OverrideId = "";
            Assert.That(LobbyTheme.Current, Is.SameAs(LobbyTheme.ForHour(System.DateTime.Now.Hour)));
        }

        static float Lum(Color c)
        {
            float f(float x) => x <= 0.03928f ? x / 12.92f : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * f(c.r) + 0.7152f * f(c.g) + 0.0722f * f(c.b);
        }
        static float Contrast(Color a, Color b) { float la = Lum(a), lb = Lum(b); return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f); }
    }
}
