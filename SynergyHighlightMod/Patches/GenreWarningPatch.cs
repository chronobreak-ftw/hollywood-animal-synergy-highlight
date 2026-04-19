using System.Collections.Generic;
using System.Linq;
using Data.Configs;
using Data.GameObject;
using HarmonyLib;
using UI.Views;
using UnityEngine;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(MovieScriptEditorView), "OnGenreTagFractionUpdated")]
    static class MovieScriptEditorView_OnGenreTagFractionUpdated_WarningPatch
    {
        static void Postfix(MovieScriptEditorView __instance)
        {
            var queue = Traverse
                .Create(__instance)
                .Field("genreAdditionAndRemovalQueue")
                .GetValue<System.Collections.ICollection>();
            if (queue != null && queue.Count > 0)
                return;
            GenreWarningHelper.UpdateWarning(__instance);
        }
    }

    [HarmonyPatch(typeof(MovieScriptEditorView), "CheckGenreQueue")]
    static class MovieScriptEditorView_CheckGenreQueue_WarningPatch
    {
        static void Postfix(MovieScriptEditorView __instance)
        {
            var queue = Traverse
                .Create(__instance)
                .Field("genreAdditionAndRemovalQueue")
                .GetValue<System.Collections.ICollection>();
            if (queue == null || queue.Count > 0)
                return;
            GenreWarningHelper.UpdateWarning(__instance);
        }
    }

    static class GenreWarningHelper
    {
        private const float PAIR_SUM_MIN = 0.70f;
        private const float PAIR_FRAC_MIN = 0.35f;

        private const float PAIR_SYNERGY_GREEN_MIN = 0.35f;

        private static readonly Color OutlineRed = new Color(1f, 0.15f, 0.15f);
        private static readonly Color OutlineGreen = new Color(0.10f, 0.90f, 0.20f);

        private static bool AllSelectedGenrePairsAreFullSynergy(List<TagData> genres)
        {
            if (genres == null || genres.Count < 2)
                return false;
            for (int i = 0; i < genres.Count; i++)
            {
                string a = genres[i]?.Id;
                if (string.IsNullOrEmpty(a))
                    return false;
                for (int j = i + 1; j < genres.Count; j++)
                {
                    string b = genres[j]?.Id;
                    if (string.IsNullOrEmpty(b))
                        return false;
                    float sum = SynergyDatabase.GetGenrePairSum(a, b);
                    if (sum < PAIR_SYNERGY_GREEN_MIN)
                        return false;
                }
            }
            return true;
        }

        internal static void UpdateWarning(MovieScriptEditorView instance)
        {
            var sliderMB = Traverse.Create(instance).Field("genreSlider").GetValue<MonoBehaviour>();
            if (sliderMB == null)
                return;

            var movieWrapper = Traverse.Create(instance).Field("movieWrapper").GetValue<object>();
            if (movieWrapper == null)
                return;

            var gameVariables = Traverse
                .Create(instance)
                .Field("gameVariables")
                .GetValue<GameVariables>();

            var genres = Traverse.Create(movieWrapper).Property("Genres").GetValue<List<TagData>>();
            if (genres == null || genres.Count == 0)
            {
                SynergyOverlay.ApplyBorder(sliderMB.gameObject, Color.clear);
                return;
            }

            if (genres.Count == 1)
            {
                float unpairedMin = gameVariables != null ? gameVariables.GenresUnpairedMin : 0.5f;
                bool fullUnpairedBonus = genres[0]?.Fraction + 0.0001f >= unpairedMin;
                SynergyOverlay.ApplyBorder(
                    sliderMB.gameObject,
                    fullUnpairedBonus ? OutlineGreen : Color.clear
                );
                return;
            }

            var fractions = genres.Select(g => g.Fraction).OrderByDescending(f => f).ToList();
            float topSum = fractions[0] + fractions[1];
            float topSecondary = fractions[1];

            bool bonusLost = topSum < PAIR_SUM_MIN || topSecondary < PAIR_FRAC_MIN;
            Color border;
            if (bonusLost)
                border = OutlineRed;
            else if (AllSelectedGenrePairsAreFullSynergy(genres))
                border = OutlineGreen;
            else
                border = Color.clear;

            SynergyOverlay.ApplyBorder(sliderMB.gameObject, border);
        }
    }
}
