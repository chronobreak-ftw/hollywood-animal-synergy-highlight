using System;
using System.Collections.Generic;
using Data.GameObject;
using HarmonyLib;
using UI.Common.Lists.ItemView;
using UnityEngine;

namespace SynergyHighlightMod.Patches
{
    static class ContentTagCardOverlay
    {
        private static readonly Type ContentTagSelectorPanelType = Type.GetType(
            "UI.Common.SubPanels.ContentTagSelectorPanel, Assembly-CSharp"
        );

        private static readonly HashSet<ContentTagCardItemView> _trackedCards =
            new HashSet<ContentTagCardItemView>();

        private static bool _subscribed = false;

        private static void EnsureSubscribed()
        {
            if (!_subscribed)
            {
                SynergyTracker.OnGenresChanged += RefreshAllTrackedCards;
                _subscribed = true;
            }
        }

        private static void RefreshAllTrackedCards()
        {
            var log = BepInEx.Logging.Logger.CreateLogSource("Synergy Highlight");

            foreach (var card in new List<ContentTagCardItemView>(_trackedCards))
            {
                if (card != null)
                {
                    Apply(card);
                }
            }
        }

        private static bool IsUnderContentTagSelectorPanel(Transform t)
        {
            if (ContentTagSelectorPanelType == null)
                return false;
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.GetComponent(ContentTagSelectorPanelType) != null)
                    return true;
            }
            return false;
        }

        internal static void Apply(ContentTagCardItemView instance)
        {
            try
            {
                EnsureSubscribed();

                var tagData = Traverse.Create(instance).Property("TagData").GetValue<TagData>();

                if (tagData?.Id == null)
                {
                    SynergyOverlay.Remove(instance.gameObject);
                    _trackedCards.Remove(instance);
                    return;
                }

                _trackedCards.Add(instance);

                if (tagData.Selected && IsUnderContentTagSelectorPanel(instance.transform))
                {
                    SynergyOverlay.Remove(instance.gameObject);
                    return;
                }

                if (!instance.Interactable)
                {
                    SynergyOverlay.Apply(instance.gameObject, Color.clear);
                    return;
                }

                var genres = SynergyTracker.SelectedGenreIds;

                if (genres.Count == 0)
                {
                    SynergyOverlay.Apply(instance.gameObject, Color.clear);
                    return;
                }

                float? score = SynergyDatabase.GetSynergyScore(tagData.Id, genres);
                Color color = SynergyOverlay.ScoreToColor(
                    score,
                    SynergyOverlay.OverlayAlphaContent
                );

                SynergyOverlay.Apply(instance.gameObject, color);
            }
            catch (System.Exception ex)
            {
                var log = BepInEx.Logging.Logger.CreateLogSource("Synergy Highlight");
                log.LogError($"[ContentTagCardOverlay] Exception in Apply: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(ContentTagCardItemView), "OnUpdate")]
    static class ContentTagCardItemView_OnUpdate_Patch
    {
        static void Postfix(ContentTagCardItemView __instance) =>
            ContentTagCardOverlay.Apply(__instance);
    }

    [HarmonyPatch(typeof(ContentTagCardItemView), "OnSelectionChanged")]
    static class ContentTagCardItemView_OnSelectionChanged_Patch
    {
        static void Postfix(ContentTagCardItemView __instance) =>
            ContentTagCardOverlay.Apply(__instance);
    }
}
