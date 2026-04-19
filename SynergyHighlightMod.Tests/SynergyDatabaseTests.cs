using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class SynergyDatabaseTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public SynergyDatabaseTests()
        {
            _tempDir = Path.Combine(
                Path.GetTempPath(),
                "SynergyHighlightTest_" + Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(Path.Combine(_tempDir, "Data", "Configs"));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch { }
        }

        private void WriteCompat(string json) =>
            File.WriteAllText(
                Path.Combine(_tempDir, "Data", "Configs", "TagCompatibilityData.json"),
                json
            );

        private void WriteGenrePairs(string json) =>
            File.WriteAllText(Path.Combine(_tempDir, "Data", "Configs", "GenrePairs.json"), json);

        private void Load() => SynergyDatabase.Load(_tempDir, _errors.Add, _warnings.Add);

        // ---- IsLoaded ----

        [Fact]
        public void Load_SetsIsLoaded_WhenBothFilesValid()
        {
            WriteCompat(@"{""TAG_A"":{""GENRE_1"":""4.5""}}");
            WriteGenrePairs(@"{""GENRE_1"":{""GENRE_2"":{""Item1"":""0.2"",""Item2"":""0.15""}}}");

            Load();

            Assert.True(SynergyDatabase.IsLoaded);
            Assert.Empty(_errors);
        }

        [Fact]
        public void Load_SetsIsLoaded_False_WhenCompatFileMissing()
        {
            WriteGenrePairs(@"{}");

            Load();

            Assert.False(SynergyDatabase.IsLoaded);
            Assert.NotEmpty(_errors);
        }

        [Fact]
        public void Load_SetsIsLoaded_False_WhenGenrePairsFileMissing()
        {
            WriteCompat(@"{}");

            Load();

            Assert.False(SynergyDatabase.IsLoaded);
            Assert.NotEmpty(_errors);
        }

        [Fact]
        public void Load_ResetsState_OnSubsequentCall()
        {
            WriteCompat(@"{""TAG_A"":{""GENRE_1"":""4.5""}}");
            WriteGenrePairs(@"{}");
            Load();
            Assert.True(SynergyDatabase.IsLoaded);

            // Second load with missing compat
            File.Delete(Path.Combine(_tempDir, "Data", "Configs", "TagCompatibilityData.json"));
            _errors.Clear();
            Load();

            Assert.False(SynergyDatabase.IsLoaded);
        }

        // ---- GetCompatibility ----

        [Fact]
        public void Load_ParsesCompatibilityScore()
        {
            WriteCompat(@"{""TAG_A"":{""GENRE_1"":""4.5""}}");
            WriteGenrePairs(@"{}");
            Load();

            Assert.Equal(4.5f, SynergyDatabase.GetCompatibility("TAG_A", "GENRE_1"));
        }

        [Fact]
        public void Load_CompatibilityIsBidirectional()
        {
            WriteCompat(@"{""TAG_A"":{""GENRE_1"":""2.0""}}");
            WriteGenrePairs(@"{}");
            Load();

            Assert.Equal(2.0f, SynergyDatabase.GetCompatibility("GENRE_1", "TAG_A"));
        }

        [Fact]
        public void Load_CompatibilityIsCaseInsensitive()
        {
            WriteCompat(@"{""tag_a"":{""genre_1"":""3.0""}}");
            WriteGenrePairs(@"{}");
            Load();

            Assert.Equal(3.0f, SynergyDatabase.GetCompatibility("TAG_A", "GENRE_1"));
        }

        [Fact]
        public void Load_CompatibilityReturnsNeutral_ForMissingPair()
        {
            WriteCompat(@"{""TAG_A"":{""GENRE_1"":""4.0""}}");
            WriteGenrePairs(@"{}");
            Load();

            Assert.Equal(
                SynergyLogic.NEUTRAL,
                SynergyDatabase.GetCompatibility("TAG_A", "GENRE_X")
            );
        }

        // ---- GetGenrePairSum ----

        [Fact]
        public void Load_ParsesGenrePairSum()
        {
            WriteCompat(@"{}");
            WriteGenrePairs(@"{""GENRE_1"":{""GENRE_2"":{""Item1"":""0.2"",""Item2"":""0.15""}}}");
            Load();

            Assert.Equal(0.35f, SynergyDatabase.GetGenrePairSum("GENRE_1", "GENRE_2"), 4);
        }

        [Fact]
        public void Load_GenrePairSumIsSymmetric()
        {
            WriteCompat(@"{}");
            WriteGenrePairs(@"{""GENRE_1"":{""GENRE_2"":{""Item1"":""0.2"",""Item2"":""0.15""}}}");
            Load();

            Assert.Equal(
                SynergyDatabase.GetGenrePairSum("GENRE_1", "GENRE_2"),
                SynergyDatabase.GetGenrePairSum("GENRE_2", "GENRE_1")
            );
        }

        // ---- Asymmetry warning ----

        [Fact]
        public void Load_LogsWarning_WhenGenrePairsAreAsymmetric()
        {
            WriteCompat(@"{}");
            WriteGenrePairs(
                @"{
                    ""GENRE_1"":{""GENRE_2"":{""Item1"":""0.2"",""Item2"":""0.15""}},
                    ""GENRE_2"":{""GENRE_1"":{""Item1"":""0.3"",""Item2"":""0.10""}}
                }"
            );
            Load();

            Assert.NotEmpty(_warnings);
        }

        [Fact]
        public void Load_NoWarning_WhenGenrePairsAreSymmetric()
        {
            WriteCompat(@"{}");
            WriteGenrePairs(
                @"{
                    ""GENRE_1"":{""GENRE_2"":{""Item1"":""0.2"",""Item2"":""0.15""}},
                    ""GENRE_2"":{""GENRE_1"":{""Item1"":""0.2"",""Item2"":""0.15""}}
                }"
            );
            Load();

            Assert.Empty(_warnings);
        }
    }
}
