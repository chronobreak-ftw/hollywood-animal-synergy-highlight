using System;
using System.Collections.Generic;

namespace SynergyHighlightMod
{
    internal static class SynergyLogic
    {
        internal const float NEUTRAL = 3f;
        internal const float MIN = 1f;
        internal const float MAX = 5f;

        internal static float GetCompatibility(
            Dictionary<string, Dictionary<string, float>> compat,
            string a,
            string b
        )
        {
            if (compat == null)
                return NEUTRAL;
            if (compat.TryGetValue(a, out var inner) && inner.TryGetValue(b, out float s))
                return s;
            if (compat.TryGetValue(b, out var inner2) && inner2.TryGetValue(a, out float s2))
                return s2;
            return NEUTRAL;
        }

        internal static float? GetSynergyScore(
            Dictionary<string, Dictionary<string, float>> compat,
            string tagId,
            IEnumerable<string> genreIds
        )
        {
            float sum = 0f;
            int count = 0;
            foreach (string g in genreIds)
            {
                sum += GetCompatibility(compat, tagId, g);
                count++;
            }
            return count == 0 ? (float?)null : sum / count;
        }

        internal static float Normalize(float score) =>
            Math.Max(0f, Math.Min(1f, (score - MIN) / (MAX - MIN)));

        internal static float GetGenrePairSum(
            Dictionary<string, Dictionary<string, (float art, float com)>> genrePairs,
            string genre1,
            string genre2
        )
        {
            (float art, float com) = GetGenrePairBonus(genrePairs, genre1, genre2);
            return art + com;
        }

        internal static float? GetBestGenrePairScore(
            Dictionary<string, Dictionary<string, (float art, float com)>> genrePairs,
            string candidateGenre,
            IEnumerable<string> selectedGenreIds
        )
        {
            float? best = null;
            foreach (string g in selectedGenreIds)
            {
                if (string.Equals(g, candidateGenre, StringComparison.OrdinalIgnoreCase))
                    continue;
                float score = GetGenrePairSum(genrePairs, candidateGenre, g);
                if (best == null || score > best.Value)
                    best = score;
            }
            return best;
        }

        private static (float art, float com) GetGenrePairBonus(
            Dictionary<string, Dictionary<string, (float art, float com)>> genrePairs,
            string g1,
            string g2
        )
        {
            if (genrePairs == null)
                return (0f, 0f);
            if (genrePairs.TryGetValue(g1, out var inner) && inner.TryGetValue(g2, out var b))
                return b;
            if (genrePairs.TryGetValue(g2, out var inner2) && inner2.TryGetValue(g1, out var b2))
                return b2;
            return (0f, 0f);
        }
    }
}
