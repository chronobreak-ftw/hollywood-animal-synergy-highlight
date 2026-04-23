using System.Collections.Generic;
using Data.GameObject.Movie;
using Enums;
using Model.Movies;

namespace SynergyHighlightMod
{
    public static class AdsHighlightTracker
    {
        public static MovieDataWrapper CurrentMovie { get; private set; }
        public static MovieProcessor CurrentMovieProcessor { get; private set; }

        private static Dictionary<AudienceGroupIds, float> _debugAudienceScores;

        public static void SetContext(MovieDataWrapper movie, MovieProcessor processor)
        {
            CurrentMovie = movie;
            CurrentMovieProcessor = processor;
        }

        public static void Clear()
        {
            CurrentMovie = null;
            CurrentMovieProcessor = null;
        }

        public static void SetDebugAudienceScores(Dictionary<AudienceGroupIds, float> scores)
        {
            _debugAudienceScores = scores;
        }

        public static void ClearDebugAudienceScores()
        {
            _debugAudienceScores = null;
        }

        public static bool TryGetDebugAudienceScores(out Dictionary<AudienceGroupIds, float> scores)
        {
            scores = _debugAudienceScores;
            return scores != null;
        }
    }
}
