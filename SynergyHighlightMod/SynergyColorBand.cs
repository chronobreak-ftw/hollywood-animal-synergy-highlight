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
        // Tag-genre compatibility thresholds (score range 1-5)
        internal const float ScoreGreenMin = 4.0f;
        internal const float ScoreYellowMin = 3.5f;
        internal const float ScoreRedMax = 2.5f;

        // Genre-pair bonus sum thresholds
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
    }
}
