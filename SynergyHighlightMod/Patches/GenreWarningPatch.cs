using System.Collections.Generic;
using System.Linq;
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

        internal static void UpdateWarning(MovieScriptEditorView instance)
        {
            var sliderMB = Traverse.Create(instance).Field("genreSlider").GetValue<MonoBehaviour>();
            if (sliderMB == null)
                return;

            var movieWrapper = Traverse.Create(instance).Field("movieWrapper").GetValue<object>();
            if (movieWrapper == null)
                return;

            var genres = Traverse.Create(movieWrapper).Property("Genres").GetValue<List<TagData>>();
            if (genres == null || genres.Count < 2)
            {
                SynergyOverlay.ApplyBorder(sliderMB.gameObject, Color.clear);
                return;
            }

            var fractions = genres.Select(g => g.Fraction).OrderByDescending(f => f).ToList();
            float topSum = fractions[0] + fractions[1];
            float topSecondary = fractions[1];

            bool bonusLost = topSum < PAIR_SUM_MIN || topSecondary < PAIR_FRAC_MIN;
            var warningColor = bonusLost ? new Color(1f, 0.15f, 0.15f) : Color.clear;
            SynergyOverlay.ApplyBorder(sliderMB.gameObject, warningColor);
        }
    }
}
