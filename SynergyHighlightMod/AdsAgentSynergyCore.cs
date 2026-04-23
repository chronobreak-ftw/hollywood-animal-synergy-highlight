using System;

namespace SynergyHighlightMod
{
    /// <summary>
    /// Pure-math core of the ad-agency scoring formula.
    ///
    /// All inputs are plain C# primitives — no Unity, BepInEx, or game-assembly types —
    /// so this file can be compiled and tested by the unit-test project without those deps.
    ///
    /// Demo indices (matches GroupWeights array order):
    ///   0 = TM  1 = TF  2 = YM  3 = YF  4 = AM  5 = AF
    ///
    /// ScoreType indices:
    ///   0 = BASE  1 = ART  2 = COM
    ///
    /// Formula source: MovieProcessor.GetPotentialAudiencesForMovie (line 4350).
    /// Constants from: Data/Configs/GameVariables.json and Data/Configs/AudienceGroups.json.
    /// </summary>
    internal static class AdsAgentSynergyCore
    {
        // ── Constants from Data/Configs/GameVariables.json ──────────────────────────
        // "base_audience_fraction": "0.55"
        // "art_audience_fraction":  "0.15"
        // "com_audience_fraction":  "0.30"
        internal const float FracBase = 0.55f;
        internal const float FracArt = 0.15f;
        internal const float FracCom = 0.30f;

        // "ads_efficiency": "0.15;0.30;0.50"  (quality index 0 / 1 / 2)
        internal static readonly float[] AdsEfficiency = { 0.15f, 0.30f, 0.50f };
        private const float MaxQualityEff = 0.50f; // AdsEfficiency[2]

        // ── Constants from Data/Configs/AudienceGroups.json ─────────────────────────
        // Each row: [baseWeight, artWeight, commercialWeight]
        // Row order: TM=0, TF=1, YM=2, YF=3, AM=4, AF=5
        internal static readonly float[][] GroupWeights = new float[][]
        {
            new[] { 0.150f, 0.050f, 0.200f }, // TM
            new[] { 0.150f, 0.050f, 0.200f }, // TF
            new[] { 0.300f, 0.400f, 0.250f }, // YM
            new[] { 0.300f, 0.300f, 0.250f }, // YF
            new[] { 0.050f, 0.100f, 0.100f }, // AM
            new[] { 0.050f, 0.100f, 0.100f }, // AF
        };

        // ── Public API (internal so test project can call these directly) ────────────

        /// <summary>
        /// Movie-dependent part of the game's audience formula for one (demo, scoreType) pair:
        ///   AudienceFraction[scoreType] × audienceGroupWeight[demo][scoreType] × movieScore
        /// </summary>
        internal static float SubgroupRawScore(
            int demoIndex,
            int scoreTypeIndex,
            float artTotal,
            float comTotal,
            float baseline
        )
        {
            if (demoIndex < 0 || demoIndex >= GroupWeights.Length)
                return 0f;
            float[] w = GroupWeights[demoIndex];
            switch (scoreTypeIndex)
            {
                case 0:
                    return FracBase * w[0] * baseline;
                case 1:
                    return FracArt * w[1] * artTotal;
                case 2:
                    return FracCom * w[2] * comTotal;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Highest subgroupRawScore × maxQualityEfficiency for this movie, used as the
        /// normalisation ceiling so ComputeRelevanceCore returns a 0–1 ratio.
        /// </summary>
        internal static float GetMaxSubgroupBenefit(float artTotal, float comTotal, float baseline)
        {
            float max = 0f;
            foreach (float[] w in GroupWeights)
            {
                float b = FracBase * w[0] * baseline;
                float a = FracArt * w[1] * artTotal;
                float c = FracCom * w[2] * comTotal;
                float best = Math.Max(b, Math.Max(a, c));
                if (best > max)
                    max = best;
            }
            return max * MaxQualityEff;
        }

        /// <summary>Quality index → efficiency multiplier.</summary>
        internal static float GetQualityEff(int quality)
        {
            if (quality >= 0 && quality < AdsEfficiency.Length)
                return AdsEfficiency[quality];
            return AdsEfficiency[AdsEfficiency.Length - 1];
        }

        /// <summary>
        /// Core relevance computation: takes only primitives, no game types.
        /// audiences is an array of (demoIndex, scoreTypeIndex) pairs.
        /// Returns 0–1; higher = better fit for the movie.
        /// </summary>
        internal static float ComputeRelevanceCore(
            (int demoIndex, int scoreTypeIndex)[] audiences,
            int quality,
            float artTotal,
            float comTotal,
            float baseline
        )
        {
            if (audiences == null || audiences.Length == 0)
                return 0f;

            float maxPossible = GetMaxSubgroupBenefit(artTotal, comTotal, baseline);
            if (maxPossible <= 0f)
                return 0f;

            float qualityEff = GetQualityEff(quality);
            float total = 0f;

            foreach (var pair in audiences)
            {
                float raw = SubgroupRawScore(
                    pair.demoIndex,
                    pair.scoreTypeIndex,
                    artTotal,
                    comTotal,
                    baseline
                );
                total += qualityEff * raw / maxPossible;
            }

            return total / audiences.Length;
        }
    }
}
