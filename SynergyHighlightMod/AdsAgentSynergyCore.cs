using System;

namespace SynergyHighlightMod
{
    internal static class AdsAgentSynergyCore
    {
        internal const float FracBase = 0.55f;
        internal const float FracArt = 0.15f;
        internal const float FracCom = 0.30f;

        internal static readonly float[] AdsEfficiency = { 0.15f, 0.30f, 0.50f };
        private const float MaxQualityEff = 0.50f; // AdsEfficiency[2]

        internal static readonly float[][] GroupWeights = new float[][]
        {
            new[] { 0.150f, 0.050f, 0.200f }, // TM
            new[] { 0.150f, 0.050f, 0.200f }, // TF
            new[] { 0.300f, 0.400f, 0.250f }, // YM
            new[] { 0.300f, 0.300f, 0.250f }, // YF
            new[] { 0.050f, 0.100f, 0.100f }, // AM
            new[] { 0.050f, 0.100f, 0.100f }, // AF
        };

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

        internal static float GetQualityEff(int quality)
        {
            if (quality < 0)
                return 0f;
            if (quality < AdsEfficiency.Length)
                return AdsEfficiency[quality];
            return AdsEfficiency[AdsEfficiency.Length - 1];
        }

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
