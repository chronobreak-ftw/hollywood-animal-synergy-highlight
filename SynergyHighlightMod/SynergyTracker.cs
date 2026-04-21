using System.Collections.Generic;

namespace SynergyHighlightMod
{
    public static class SynergyTracker
    {
        private static readonly HashSet<string> _genres = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase
        );

        public static IReadOnlyCollection<string> SelectedGenreIds => _genres;

        public static event System.Action OnGenresChanged;

        public static void SetGenre(string genreId, bool selected)
        {
            bool changed = selected ? _genres.Add(genreId) : _genres.Remove(genreId);
            if (changed)
            {
                OnGenresChanged?.Invoke();
            }
        }

        public static void Clear()
        {
            _genres.Clear();
        }
    }
}
