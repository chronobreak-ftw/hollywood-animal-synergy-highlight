using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class AdsAgentSynergyLogicTests
    {
        private const float Art = 0.70f;
        private const float Com = 0.70f;
        private const float Base = 0.27f;

        private const float MaxPossible = 0.026250f;
        private const float Tolerance = 0.0005f;

        [Fact]
        public void SubgroupRawScore_YM_COM_ReturnsExpected()
        {
            float result = AdsAgentSynergyCore.SubgroupRawScore(2, 2, Art, Com, Base);
            Assert.Equal(0.0525f, result, 4);
        }

        [Fact]
        public void SubgroupRawScore_YF_COM_ReturnsExpected()
        {
            float result = AdsAgentSynergyCore.SubgroupRawScore(3, 2, Art, Com, Base);
            Assert.Equal(0.0525f, result, 4);
        }

        [Fact]
        public void SubgroupRawScore_TM_BASE_ReturnsExpected()
        {
            float result = AdsAgentSynergyCore.SubgroupRawScore(0, 0, Art, Com, Base);
            Assert.Equal(0.022275f, result, 5);
        }

        [Fact]
        public void SubgroupRawScore_AM_ART_ReturnsExpected()
        {
            float result = AdsAgentSynergyCore.SubgroupRawScore(4, 1, Art, Com, Base);
            Assert.Equal(0.0105f, result, 4);
        }

        [Fact]
        public void SubgroupRawScore_InvalidDemo_ReturnsZero()
        {
            Assert.Equal(0f, AdsAgentSynergyCore.SubgroupRawScore(-1, 0, Art, Com, Base), 4);
            Assert.Equal(0f, AdsAgentSynergyCore.SubgroupRawScore(99, 0, Art, Com, Base), 4);
        }

        [Fact]
        public void SubgroupRawScore_InvalidScoreType_ReturnsZero()
        {
            Assert.Equal(0f, AdsAgentSynergyCore.SubgroupRawScore(0, -1, Art, Com, Base), 4);
            Assert.Equal(0f, AdsAgentSynergyCore.SubgroupRawScore(0, 99, Art, Com, Base), 4);
        }

        [Fact]
        public void GetMaxSubgroupBenefit_SicilianTigerScores_ReturnsExpected()
        {
            float result = AdsAgentSynergyCore.GetMaxSubgroupBenefit(Art, Com, Base);
            Assert.Equal(MaxPossible, result, 5);
        }

        [Fact]
        public void GetMaxSubgroupBenefit_ZeroScores_ReturnsZero()
        {
            Assert.Equal(0f, AdsAgentSynergyCore.GetMaxSubgroupBenefit(0f, 0f, 0f), 5);
        }

        [Fact]
        public void GetMaxSubgroupBenefit_PerfectScores_IsPositive()
        {
            float result = AdsAgentSynergyCore.GetMaxSubgroupBenefit(1f, 1f, 1f);
            Assert.True(result > 0f);
        }

        [Theory]
        [InlineData(0, 0.15f)]
        [InlineData(1, 0.30f)]
        [InlineData(2, 0.50f)]
        public void GetQualityEff_KnownIndex_ReturnsExpected(int quality, float expected)
        {
            Assert.Equal(expected, AdsAgentSynergyCore.GetQualityEff(quality), 4);
        }

        [Fact]
        public void GetQualityEff_OutOfRangeHigh_ReturnsCappedAtMax()
        {
            Assert.Equal(0.50f, AdsAgentSynergyCore.GetQualityEff(99), 4);
        }

        [Fact]
        public void ComputeRelevanceCore_Fc2_IsGreen()
        {
            var audiences = new[] { (1, 2), (3, 2), (5, 2) };
            float result = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 2, Art, Com, Base);
            Assert.True(
                result >= SynergyColorBand.AdGreenMin,
                $"FC2 expected Green (≥{SynergyColorBand.AdGreenMin}), got {result:F4}"
            );
        }

        [Fact]
        public void ComputeRelevanceCore_B1Radio_IsRed()
        {
            var audiences = new[] { (4, 0), (5, 0) };
            float result = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 2, Art, Com, Base);
            Assert.True(
                result <= SynergyColorBand.AdRedMax,
                $"B1RADIO expected Red (≤{SynergyColorBand.AdRedMax}), got {result:F4}"
            );
        }

        [Fact]
        public void ComputeRelevanceCore_Tyc1_IsYellow()
        {
            var audiences = new[] { (0, 2), (1, 2), (2, 2), (3, 2) };
            float result = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 1, Art, Com, Base);
            Assert.True(
                result >= SynergyColorBand.AdYellowMin && result < SynergyColorBand.AdGreenMin,
                $"TYC1 expected Yellow ({SynergyColorBand.AdYellowMin}-{SynergyColorBand.AdGreenMin}), got {result:F4}"
            );
        }

        [Fact]
        public void ComputeRelevanceCore_Artmag_IsClear()
        {
            var audiences = new[] { (2, 1), (3, 1), (4, 1), (5, 1) };
            float result = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 1, Art, Com, Base);
            Assert.True(
                result > SynergyColorBand.AdRedMax && result < SynergyColorBand.AdYellowMin,
                $"ARTMAG expected Clear ({SynergyColorBand.AdRedMax}-{SynergyColorBand.AdYellowMin}), got {result:F4}"
            );
        }

        [Fact]
        public void ComputeRelevanceCore_NullAudiences_ReturnsZero()
        {
            Assert.Equal(0f, AdsAgentSynergyCore.ComputeRelevanceCore(null, 2, Art, Com, Base), 4);
        }

        [Fact]
        public void ComputeRelevanceCore_EmptyAudiences_ReturnsZero()
        {
            Assert.Equal(
                0f,
                AdsAgentSynergyCore.ComputeRelevanceCore(new (int, int)[0], 2, Art, Com, Base),
                4
            );
        }

        [Fact]
        public void ComputeRelevanceCore_ZeroMovieScores_ReturnsZero()
        {
            var audiences = new[] { (2, 2) };
            Assert.Equal(0f, AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 2, 0f, 0f, 0f), 4);
        }

        [Fact]
        public void ComputeRelevanceCore_HigherQualityYieldsHigherRelevance()
        {
            var audiences = new[] { (2, 2) };
            float low = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 0, Art, Com, Base);
            float mid = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 1, Art, Com, Base);
            float high = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 2, Art, Com, Base);
            Assert.True(
                low < mid && mid < high,
                $"Expected quality 0 < 1 < 2 but got {low:F4} / {mid:F4} / {high:F4}"
            );
        }

        [Fact]
        public void ComputeRelevanceCore_BestSubgroupAtMaxQuality_ReachesOne()
        {
            var audiences = new[] { (2, 2) };
            float result = AdsAgentSynergyCore.ComputeRelevanceCore(audiences, 2, Art, Com, Base);
            Assert.Equal(1.0f, result, 3);
        }
    }
}
