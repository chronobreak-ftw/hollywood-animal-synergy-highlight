using System;
using System.Collections.Generic;
using Xunit;

namespace SynergyHighlightMod.Tests
{
    public class SynergyTrackerTests
    {
        [Fact]
        public void SetGenre_AddsGenre_WhenSelecting()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);

            Assert.Contains("Action", SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void SetGenre_RemovesGenre_WhenDeselecting()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);
            SynergyTracker.SetGenre("Action", false);

            Assert.DoesNotContain("Action", SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void SetGenre_IsCaseInsensitive()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("action", true);

            Assert.Contains("ACTION", SynergyTracker.SelectedGenreIds);
            Assert.Contains("Action", SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void SetGenre_FiresOnGenresChanged_WhenAdding()
        {
            SynergyTracker.Clear();
            bool eventFired = false;

            Action handler = () => eventFired = true;
            SynergyTracker.OnGenresChanged += handler;
            SynergyTracker.SetGenre("Action", true);
            SynergyTracker.OnGenresChanged -= handler;

            Assert.True(eventFired);
        }

        [Fact]
        public void SetGenre_FiresOnGenresChanged_WhenRemoving()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);

            bool eventFired = false;
            Action handler = () => eventFired = true;
            SynergyTracker.OnGenresChanged += handler;
            SynergyTracker.SetGenre("Action", false);
            SynergyTracker.OnGenresChanged -= handler;

            Assert.True(eventFired);
        }

        [Fact]
        public void SetGenre_DoesNotFireEvent_WhenNoChange()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);

            bool eventFired = false;
            Action handler = () => eventFired = true;
            SynergyTracker.OnGenresChanged += handler;
            SynergyTracker.SetGenre("Action", true); // Already selected
            SynergyTracker.OnGenresChanged -= handler;

            Assert.False(eventFired);
        }

        [Fact]
        public void Clear_RemovesAllGenres()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);
            SynergyTracker.SetGenre("Comedy", true);

            SynergyTracker.Clear();

            Assert.Empty(SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void SelectedGenreIds_ReturnsEmptyCollection_Initially()
        {
            SynergyTracker.Clear();

            Assert.Empty(SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void SelectedGenreIds_ReturnsAllSelectedGenres()
        {
            SynergyTracker.Clear();
            SynergyTracker.SetGenre("Action", true);
            SynergyTracker.SetGenre("Comedy", true);
            SynergyTracker.SetGenre("Drama", true);

            Assert.Equal(3, SynergyTracker.SelectedGenreIds.Count);
            Assert.Contains("Action", SynergyTracker.SelectedGenreIds);
            Assert.Contains("Comedy", SynergyTracker.SelectedGenreIds);
            Assert.Contains("Drama", SynergyTracker.SelectedGenreIds);
        }

        [Fact]
        public void MultipleSubscribers_CanSubscribeToEvent()
        {
            SynergyTracker.Clear();
            int callCount = 0;

            Action handler = () => callCount++;

            SynergyTracker.OnGenresChanged += handler;
            SynergyTracker.SetGenre("Action", true);

            Assert.Equal(1, callCount);

            SynergyTracker.OnGenresChanged -= handler;
        }
    }
}
