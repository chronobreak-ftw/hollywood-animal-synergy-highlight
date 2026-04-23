namespace SynergyHighlightMod
{
    internal enum ColorBand
    {
        Clear,
        Green,
        Yellow,
        Red,
        LimeGreen,
    }

    internal static class SynergyColorBand
    {
        internal const float ScoreGreenMin = 4.0f;
        internal const float ScoreYellowMin = 3.5f;
        internal const float ScoreRedMax = 2.5f;

        internal const float PairGreenMin = 0.35f;
        internal const float PairLimeGreenMin = 0.10f;
        internal const float PairRedMax = -0.10f;

        internal static ColorBand FromScore(float? score)
        {
            if (!score.HasValue)
                return ColorBand.Clear;
            float s = score.Value;
            if (s >= ScoreGreenMin)
                return ColorBand.Green;
            if (s >= ScoreYellowMin)
                return ColorBand.Yellow;
            if (s <= ScoreRedMax)
                return ColorBand.Red;
            return ColorBand.Clear;
        }

        internal static ColorBand FromGenrePairScore(float? pairSum)
        {
            if (!pairSum.HasValue)
                return ColorBand.Clear;
            float s = pairSum.Value;
            if (s >= PairGreenMin)
                return ColorBand.Green;
            if (s >= PairLimeGreenMin)
                return ColorBand.LimeGreen;
            if (s <= PairRedMax)
                return ColorBand.Red;
            return ColorBand.Clear;
        }

        internal const float AdGreenMin = 0.65f;
        internal const float AdYellowMin = 0.45f;
        internal const float AdRedMax = 0.25f;

        internal static ColorBand FromAdRelevance(float relevance)
        {
            if (relevance >= AdGreenMin)
                return ColorBand.Green;
            if (relevance >= AdYellowMin)
                return ColorBand.Yellow;
            if (relevance <= AdRedMax)
                return ColorBand.Red;
            return ColorBand.Clear;
        }
    }
}
