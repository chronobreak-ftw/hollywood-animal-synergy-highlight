using System;
using Data.GameObject;
using HarmonyLib;
using PP.OptimizedList;
using UI.Common.Lists.ItemView;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(GenreTagItemView), "OnUpdate")]
    static class GenreTagItemView_OnUpdate_Patch
    {
        static void Postfix(GenreTagItemView __instance)
        {
            GenreTagViewPatchHelper.SyncAndColor(__instance);
        }
    }

    [HarmonyPatch(typeof(GenreTagItemView), "OnSelectedStateChanged")]
    static class GenreTagItemView_OnSelectedStateChanged_Patch
    {
        static void Postfix(GenreTagItemView __instance)
        {
            GenreTagViewPatchHelper.SyncAndColor(__instance);
        }
    }

    static class GenreTagViewPatchHelper
    {
        internal static void SyncAndColor(GenreTagItemView instance)
        {
            try
            {
                var data = Traverse.Create(instance).Property("Data").GetValue<ItemContainerData>();
                if (data == null)
                    return;

                var display = data.GetData<GenreTagItemView.GenreDisplayData>();
                if (display?.tag == null)
                    return;

                string genreId = display.tag.Id;
                bool isSelected = display.tag.Selected;

                SynergyTracker.SetGenre(genreId, isSelected);

                if (isSelected)
                {
                    SynergyOverlay.ApplyBorder(instance.gameObject, UnityEngine.Color.clear);
                    return;
                }

                var selected = SynergyTracker.SelectedGenreIds;
                if (selected.Count == 0)
                {
                    SynergyOverlay.ApplyBorder(instance.gameObject, UnityEngine.Color.clear);
                    return;
                }

                float? pairScore = SynergyDatabase.GetBestGenrePairScore(genreId, selected);
                UnityEngine.Color color = SynergyOverlay.GenrePairScoreToColor(
                    pairScore,
                    SynergyOverlay.OverlayAlphaGenre
                );
                SynergyOverlay.ApplyBorder(instance.gameObject, color);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[GenreTagViewPatch] Exception: {ex}");
            }
        }
    }
}
