using System;
using Data.Configs;
using HarmonyLib;
using Managers;
using PP.OptimizedList;
using UI.Common.Lists.ItemView;
using UI.Views;
using UnityEngine;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(AdsAgentItemView), "OnUpdate")]
    static class AdsAgentItemView_OnUpdate_Patch
    {
        static void Postfix(AdsAgentItemView __instance)
        {
            try
            {
                var movie = AdsHighlightTracker.CurrentMovie;
                var processor = AdsHighlightTracker.CurrentMovieProcessor;
                if (movie == null || processor == null)
                {
                    SynergyOverlay.ApplyBorder(__instance.gameObject, Color.clear);
                    SynergyOverlay.ApplyTint(__instance.gameObject, Color.clear);
                    return;
                }

                var data = Traverse
                    .Create(__instance)
                    .Property("Data")
                    .GetValue<ItemContainerData>();
                if (data == null)
                    return;

                var assignment = data.GetData<AdAssignment>();
                if (assignment == null)
                    return;

                if (assignment.AdsAgentCache == null)
                {
                    var dm = Traverse
                        .Create(processor)
                        .Field("dataManager")
                        .GetValue<DataManager>();
                    if (dm != null)
                        assignment.AdsAgentCache = dm.GetAdAgentByID(assignment.Id);
                }
                if (assignment.AdsAgentCache == null)
                    return;

                var agent = assignment.AdsAgentCache;

                float relevance = AdsAgentSynergyLogic.ComputeRelevance(agent, movie, processor);

                bool isInModal;
                int id = __instance.GetInstanceID();
                if (!AdsHighlightTracker.TryGetIsInModal(id, out isInModal))
                {
                    isInModal = __instance.GetComponentInParent<AdAgentSelectionView>() != null;
                    AdsHighlightTracker.SetIsInModal(id, isInModal);
                }

                if (isInModal)
                {
                    Color tint = SynergyOverlay.AdRelevanceToColor(
                        relevance,
                        SynergyOverlay.OverlayAlphaAdModal
                    );
                    SynergyOverlay.ApplyTint(__instance.gameObject, tint);
                    SynergyOverlay.ApplyBorder(__instance.gameObject, Color.clear);
                    return;
                }

                SynergyOverlay.ApplyTint(__instance.gameObject, Color.clear);
                Color color = SynergyOverlay.AdRelevanceToColor(
                    relevance,
                    SynergyOverlay.OverlayAlphaGenre
                );
                SynergyOverlay.ApplyBorder(__instance.gameObject, color);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AdsAgentItemViewPatch] Exception: {ex}");
            }
        }
    }
}
