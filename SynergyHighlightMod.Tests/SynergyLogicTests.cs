using System.Collections.Generic;
using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class SynergyLogicTests
    {
        private static Dictionary<string, Dictionary<string, float>> Compat(
            params (string a, string b, float score)[] pairs
        )
        {
            var d = new Dictionary<string, Dictionary<string, float>>(
                System.StringComparer.OrdinalIgnoreCase
            );
            foreach (var (a, b, score) in pairs)
            {
                if (!d.ContainsKey(a))
                    d[a] = new Dictionary<string, float>(System.StringComparer.OrdinalIgnoreCase);
                d[a][b] = score;
            }
            return d;
        }

        private static Dictionary<string, Dictionary<string, (float art, float com)>> GenrePairs(
            params (string g1, string g2, float art, float com)[] pairs
        )
        {
            var d = new Dictionary<string, Dictionary<string, (float, float)>>(
                System.StringComparer.OrdinalIgnoreCase
            );
            foreach (var (g1, g2, art, com) in pairs)
            {
                if (!d.ContainsKey(g1))
                    d[g1] = new Dictionary<string, (float, float)>(
                        System.StringComparer.OrdinalIgnoreCase
                    );
                d[g1][g2] = (art, com);
            }
            return d;
        }

        [Fact]
        public void GetCompatibility_ReturnsNeutral_WhenCompatIsNull()
        {
            float result = SynergyLogic.GetCompatibility(null, "Action", "Comedy");
            Assert.Equal(SynergyLogic.NEUTRAL, result);
        }

        [Fact]
        public void GetCompatibility_ReturnsStoredScore_ForwardLookup()
        {
            var compat = Compat(("Action", "Comedy", 4.5f));
            Assert.Equal(4.5f, SynergyLogic.GetCompatibility(compat, "Action", "Comedy"));
        }

        [Fact]
        public void GetCompatibility_ReturnsStoredScore_ReverseLookup()
        {
            var compat = Compat(("Action", "Comedy", 4.5f));
            Assert.Equal(4.5f, SynergyLogic.GetCompatibility(compat, "Comedy", "Action"));
        }

        [Fact]
        public void GetCompatibility_ReturnsNeutral_WhenPairMissing()
        {
            var compat = Compat(("Action", "Comedy", 4.5f));
            Assert.Equal(
                SynergyLogic.NEUTRAL,
                SynergyLogic.GetCompatibility(compat, "Action", "Drama")
            );
        }

        [Fact]
        public void GetCompatibility_IsCaseInsensitive()
        {
            var compat = Compat(("action", "comedy", 2f));
            Assert.Equal(2f, SynergyLogic.GetCompatibility(compat, "ACTION", "COMEDY"));
        }

        [Fact]
        public void GetSynergyScore_ReturnsNull_WhenNoGenres()
        {
            var compat = Compat(("Tag", "Action", 4f));
            Assert.Null(SynergyLogic.GetSynergyScore(compat, "Tag", new string[0]));
        }

        [Fact]
        public void GetSynergyScore_AveragesOverMultipleGenres()
        {
            var compat = Compat(("Tag", "Action", 5f), ("Tag", "Comedy", 3f));
            float? score = SynergyLogic.GetSynergyScore(
                compat,
                "Tag",
                new[] { "Action", "Comedy" }
            );
            Assert.Equal(4f, score.Value, precision: 4);
        }

        [Fact]
        public void GetSynergyScore_UsesMissingEntryAsNeutral()
        {
            var compat = Compat(("Tag", "Action", 5f));
            float? score = SynergyLogic.GetSynergyScore(
                compat,
                "Tag",
                new[] { "Action", "Unknown" }
            );
            Assert.Equal(4f, score.Value, precision: 4); // (5 + 3) / 2
        }

        [Fact]
        public void Normalize_MinMapsToZero() =>
            Assert.Equal(0f, SynergyLogic.Normalize(SynergyLogic.MIN));

        [Fact]
        public void Normalize_MaxMapsToOne() =>
            Assert.Equal(1f, SynergyLogic.Normalize(SynergyLogic.MAX));

        [Fact]
        public void Normalize_NeutralMapsMidpoint() =>
            Assert.Equal(0.5f, SynergyLogic.Normalize(SynergyLogic.NEUTRAL), precision: 4);

        [Fact]
        public void Normalize_ClampsBelowMin() => Assert.Equal(0f, SynergyLogic.Normalize(0f));

        [Fact]
        public void Normalize_ClampsAboveMax() => Assert.Equal(1f, SynergyLogic.Normalize(10f));

        [Fact]
        public void GetGenrePairSum_ReturnsZero_WhenNull()
        {
            Assert.Equal(0f, SynergyLogic.GetGenrePairSum(null, "Action", "Comedy"));
        }

        [Fact]
        public void GetGenrePairSum_SumsArtAndCom()
        {
            var gp = GenrePairs(("Action", "Comedy", 0.2f, 0.15f));
            Assert.Equal(0.35f, SynergyLogic.GetGenrePairSum(gp, "Action", "Comedy"), precision: 4);
        }

        [Fact]
        public void GetGenrePairSum_IsSymmetric()
        {
            var gp = GenrePairs(("Action", "Comedy", 0.2f, 0.15f));
            Assert.Equal(
                SynergyLogic.GetGenrePairSum(gp, "Action", "Comedy"),
                SynergyLogic.GetGenrePairSum(gp, "Comedy", "Action")
            );
        }

        [Fact]
        public void GetBestGenrePairScore_ReturnsNull_WhenNoOtherGenres()
        {
            var gp = GenrePairs(("Action", "Comedy", 0.2f, 0.1f));
            Assert.Null(SynergyLogic.GetBestGenrePairScore(gp, "Action", new[] { "Action" }));
        }

        [Fact]
        public void GetBestGenrePairScore_SkipsSameGenre()
        {
            var gp = GenrePairs(("Action", "Comedy", 0.2f, 0.1f));
            float? best = SynergyLogic.GetBestGenrePairScore(
                gp,
                "Action",
                new[] { "Action", "Comedy" }
            );
            Assert.Equal(0.30f, best.Value, precision: 4);
        }

        [Fact]
        public void GetBestGenrePairScore_ReturnsBestAmongCandidates()
        {
            var gp = GenrePairs(
                ("Action", "Comedy", 0.1f, 0.05f), // sum 0.15
                ("Action", "Drama", 0.3f, 0.10f) // sum 0.40
            );
            float? best = SynergyLogic.GetBestGenrePairScore(
                gp,
                "Action",
                new[] { "Comedy", "Drama" }
            );
            Assert.Equal(0.40f, best.Value, precision: 4);
        }
    }
}
