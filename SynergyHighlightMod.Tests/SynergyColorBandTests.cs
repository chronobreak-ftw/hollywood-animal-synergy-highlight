using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class SynergyColorBandTests
    {
        [Fact]
        public void FromScore_ReturnsGreen_AtGreenThreshold() =>
            Assert.Equal(
                ColorBand.Green,
                SynergyColorBand.FromScore(SynergyColorBand.ScoreGreenMin)
            );

        [Fact]
        public void FromScore_ReturnsGreen_AboveGreenThreshold() =>
            Assert.Equal(ColorBand.Green, SynergyColorBand.FromScore(5.0f));

        [Fact]
        public void FromScore_ReturnsYellow_AtYellowThreshold() =>
            Assert.Equal(
                ColorBand.Yellow,
                SynergyColorBand.FromScore(SynergyColorBand.ScoreYellowMin)
            );

        [Fact]
        public void FromScore_ReturnsYellow_BelowGreenThreshold() =>
            Assert.Equal(ColorBand.Yellow, SynergyColorBand.FromScore(3.9f));

        [Fact]
        public void FromScore_ReturnsRed_AtRedThreshold() =>
            Assert.Equal(ColorBand.Red, SynergyColorBand.FromScore(SynergyColorBand.ScoreRedMax));

        [Fact]
        public void FromScore_ReturnsRed_BelowRedThreshold() =>
            Assert.Equal(ColorBand.Red, SynergyColorBand.FromScore(1.0f));

        [Fact]
        public void FromScore_ReturnsClear_InNeutralZone() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromScore(3.0f));

        [Fact]
        public void FromScore_ReturnsClear_WhenNull() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromScore(null));

        [Fact]
        public void FromScore_ReturnsClear_JustAboveRedMax() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromScore(2.51f));

        [Fact]
        public void FromScore_ReturnsClear_JustBelowYellowMin() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromScore(3.49f));

        [Fact]
        public void FromGenrePairScore_ReturnsGreen_AtGreenThreshold() =>
            Assert.Equal(
                ColorBand.Green,
                SynergyColorBand.FromGenrePairScore(SynergyColorBand.PairGreenMin)
            );

        [Fact]
        public void FromGenrePairScore_ReturnsGreen_AboveGreenThreshold() =>
            Assert.Equal(ColorBand.Green, SynergyColorBand.FromGenrePairScore(1.0f));

        [Fact]
        public void FromGenrePairScore_ReturnsLimeGreen_AtLimeGreenThreshold() =>
            Assert.Equal(
                ColorBand.LimeGreen,
                SynergyColorBand.FromGenrePairScore(SynergyColorBand.PairLimeGreenMin)
            );

        [Fact]
        public void FromGenrePairScore_ReturnsLimeGreen_BelowGreenThreshold() =>
            Assert.Equal(ColorBand.LimeGreen, SynergyColorBand.FromGenrePairScore(0.20f));

        [Fact]
        public void FromGenrePairScore_ReturnsRed_AtRedThreshold() =>
            Assert.Equal(
                ColorBand.Red,
                SynergyColorBand.FromGenrePairScore(SynergyColorBand.PairRedMax)
            );

        [Fact]
        public void FromGenrePairScore_ReturnsRed_BelowRedThreshold() =>
            Assert.Equal(ColorBand.Red, SynergyColorBand.FromGenrePairScore(-0.5f));

        [Fact]
        public void FromGenrePairScore_ReturnsClear_InNeutralZone() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromGenrePairScore(0.0f));

        [Fact]
        public void FromGenrePairScore_ReturnsClear_WhenNull() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromGenrePairScore(null));

        [Fact]
        public void FromGenrePairScore_ReturnsClear_JustBelowLimeGreenMin() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromGenrePairScore(0.09f));

        [Fact]
        public void FromGenrePairScore_ReturnsClear_JustAboveRedMax() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromGenrePairScore(-0.09f));
    }
}
