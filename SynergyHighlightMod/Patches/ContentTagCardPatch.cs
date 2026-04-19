using Data.GameObject;
using HarmonyLib;
using UI.Common.Lists.ItemView;
using UnityEngine;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(ContentTagCardItemView), "OnUpdate")]
    static class ContentTagCardItemView_OnUpdate_Patch
    {
        static void Postfix(ContentTagCardItemView __instance)
        {
            var tagData = Traverse.Create(__instance).Property("TagData").GetValue<TagData>();

            if (tagData == null)
            {
                SynergyOverlay.Remove(__instance.gameObject);
                return;
            }

            var genres = SynergyTracker.SelectedGenreIds;

            if (genres.Count == 0)
            {
                SynergyOverlay.Apply(__instance.gameObject, Color.clear);
                return;
            }

            float? score = SynergyDatabase.GetSynergyScore(tagData.Id, genres);
            Color color = SynergyOverlay.ScoreToColor(score, SynergyOverlay.OverlayAlphaContent);
            SynergyOverlay.Apply(__instance.gameObject, color);
        }
    }
}
