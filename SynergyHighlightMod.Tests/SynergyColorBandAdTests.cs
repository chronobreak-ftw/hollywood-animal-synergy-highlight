using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class SynergyColorBandAdTests
    {
        [Fact]
        public void FromAdRelevance_ReturnsGreen_AtGreenThreshold() =>
            Assert.Equal(
                ColorBand.Green,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdGreenMin)
            );

        [Fact]
        public void FromAdRelevance_ReturnsGreen_AboveGreenThreshold() =>
            Assert.Equal(ColorBand.Green, SynergyColorBand.FromAdRelevance(1.0f));

        [Fact]
        public void FromAdRelevance_ReturnsGreen_JustAboveGreenMin() =>
            Assert.Equal(
                ColorBand.Green,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdGreenMin + 0.01f)
            );

        [Fact]
        public void FromAdRelevance_ReturnsYellow_AtYellowThreshold() =>
            Assert.Equal(
                ColorBand.Yellow,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdYellowMin)
            );

        [Fact]
        public void FromAdRelevance_ReturnsYellow_JustBelowGreenMin() =>
            Assert.Equal(
                ColorBand.Yellow,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdGreenMin - 0.01f)
            );

        [Fact]
        public void FromAdRelevance_ReturnsRed_AtRedThreshold() =>
            Assert.Equal(
                ColorBand.Red,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdRedMax)
            );

        [Fact]
        public void FromAdRelevance_ReturnsRed_BelowRedThreshold() =>
            Assert.Equal(ColorBand.Red, SynergyColorBand.FromAdRelevance(0.0f));

        [Fact]
        public void FromAdRelevance_ReturnsRed_JustBelowRedMax() =>
            Assert.Equal(
                ColorBand.Red,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdRedMax - 0.01f)
            );

        [Fact]
        public void FromAdRelevance_ReturnsClear_InNeutralZone() =>
            Assert.Equal(ColorBand.Clear, SynergyColorBand.FromAdRelevance(0.35f));

        [Fact]
        public void FromAdRelevance_ReturnsClear_JustAboveRedMax() =>
            Assert.Equal(
                ColorBand.Clear,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdRedMax + 0.01f)
            );

        [Fact]
        public void FromAdRelevance_ReturnsClear_JustBelowYellowMin() =>
            Assert.Equal(
                ColorBand.Clear,
                SynergyColorBand.FromAdRelevance(SynergyColorBand.AdYellowMin - 0.01f)
            );
    }
}
