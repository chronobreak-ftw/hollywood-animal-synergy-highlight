using System;
using Data.Configs;
using Data.GameObject.Movie;
using Enums;
using Model.Movies;
using UnityEngine;

namespace SynergyHighlightMod
{
    internal static class AdsAgentSynergyLogic
    {
        internal static float ComputeRelevance(
            AdsAgentConfig agent,
            MovieDataWrapper movie,
            MovieProcessor processor
        )
        {
            if (agent?.audiences == null || agent.audiences.Length == 0)
                return 0f;

            GetMovieScores(movie, out float artTotal, out float comTotal, out float baseline);

            var audiences = new (int demoIndex, int scoreTypeIndex)[agent.audiences.Length];
            for (int i = 0; i < agent.audiences.Length; i++)
            {
                audiences[i] = (
                    DemoIndex(agent.audiences[i].id),
                    ScoreTypeIndex(agent.audiences[i].scoreType)
                );
            }

            return AdsAgentSynergyCore.ComputeRelevanceCore(
                audiences,
                agent.quality,
                artTotal,
                comTotal,
                baseline
            );
        }

        private static void GetMovieScores(
            MovieDataWrapper movie,
            out float artTotal,
            out float comTotal,
            out float baseline
        )
        {
            var result =
                TryGetStageResult(movie, MovieStages.Release)
                ?? TryGetStageResult(movie, MovieStages.Postproduction)
                ?? TryGetStageResult(movie, MovieStages.Production);

            if (result == null)
                Plugin.Log?.LogWarning(
                    "[AdsAgentSynergyLogic] No stage result found for movie; "
                        + "ad relevance scores are based on max potential and may be inaccurate."
                );

            artTotal = Mathf.Clamp01(result?.ArtTotal ?? movie.MaxArtScoreTotal);
            comTotal = Mathf.Clamp01(result?.CommercialTotal ?? movie.MaxComScoreTotal);
            baseline = Mathf.Clamp01(
                result?.Baseline ?? (movie.MaxArtScoreTotal + movie.MaxComScoreTotal) * 0.5f
            );
        }

        private static MovieStageResult TryGetStageResult(MovieDataWrapper movie, MovieStages stage)
        {
            try
            {
                return movie.GetStageResult(stage);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug(
                    $"[AdsAgentSynergyLogic] GetStageResult({stage}) threw: {ex.Message}"
                );
                return null;
            }
        }

        private static int DemoIndex(AudienceGroupIds demo)
        {
            switch (demo)
            {
                case AudienceGroupIds.TM:
                    return 0;
                case AudienceGroupIds.TF:
                    return 1;
                case AudienceGroupIds.YM:
                    return 2;
                case AudienceGroupIds.YF:
                    return 3;
                case AudienceGroupIds.AM:
                    return 4;
                case AudienceGroupIds.AF:
                    return 5;
                default:
                    return -1;
            }
        }

        private static int ScoreTypeIndex(ScoreType st)
        {
            switch (st)
            {
                case ScoreType.BASE:
                    return 0;
                case ScoreType.ART:
                    return 1;
                case ScoreType.COM:
                    return 2;
                default:
                    return -1;
            }
        }
    }
}
