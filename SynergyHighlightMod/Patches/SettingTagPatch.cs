using Data.GameObject;
using HarmonyLib;
using PP.OptimizedList;
using UI.Common.Lists.ItemView;
using UnityEngine;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(ToggleImageListItemView), "OnUpdate")]
    static class SettingTagItemView_OnUpdate_Patch
    {
        static void Postfix(ToggleImageListItemView __instance)
        {
            if (!(__instance is SettingTagItemView))
                return;

            var data = Traverse.Create(__instance).Property("Data").GetValue<ItemContainerData>();
            if (data == null)
                return;

            var tagData = data.GetData<TagData>();
            if (tagData == null)
                return;

            var genres = SynergyTracker.SelectedGenreIds;
            if (genres.Count == 0)
            {
                SynergyOverlay.Apply(__instance.gameObject, Color.clear);
                return;
            }

            float? score = SynergyDatabase.GetSynergyScore(tagData.Id, genres);
            Color color = SynergyOverlay.ScoreToColor(score, SynergyOverlay.OverlayAlphaSetting);
            SynergyOverlay.Apply(__instance.gameObject, color);
        }
    }
}
