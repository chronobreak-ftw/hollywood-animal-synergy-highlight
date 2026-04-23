using System;
using Data.GameObject.Movie;
using HarmonyLib;
using Model.Movies;
using UI.Views.MovieEditor;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(ReleaseEditorView), "OnShow")]
    static class ReleaseEditorView_OnShow_Patch
    {
        static void Postfix(ReleaseEditorView __instance)
        {
            try
            {
                var movie = Traverse
                    .Create(__instance)
                    .Field("movieWrapper")
                    .GetValue<MovieDataWrapper>();
                var processor = Traverse
                    .Create(__instance)
                    .Field("movieProcessor")
                    .GetValue<MovieProcessor>();

                if (movie != null && processor != null)
                    AdsHighlightTracker.SetContext(movie, processor);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[ReleaseEditorViewPatch] Exception: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ReleaseEditorView), "OnHidden")]
    static class ReleaseEditorView_OnHidden_Patch
    {
        static void Postfix()
        {
            AdsHighlightTracker.Clear();
        }
    }
}
