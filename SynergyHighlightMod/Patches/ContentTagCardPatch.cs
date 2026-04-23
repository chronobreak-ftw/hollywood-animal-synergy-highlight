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

        private static GameObject GetOverlayParent(ContentTagCardItemView instance)
        {
            var scaler = Traverse.Create(instance).Field("cardScaler").GetValue<Component>();
            return scaler != null ? scaler.gameObject : instance.gameObject;
        }

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
            var dead = new List<ContentTagCardItemView>();
            foreach (var card in new List<ContentTagCardItemView>(_trackedCards))
            {
                if (card == null)
                    dead.Add(card);
                else
                    Apply(card);
            }
            foreach (var d in dead)
                _trackedCards.Remove(d);
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
                var overlayParent = GetOverlayParent(instance);

                if (tagData?.Id == null)
                {
                    SynergyOverlay.Remove(overlayParent);
                    _trackedCards.Remove(instance);
                    return;
                }

                _trackedCards.Add(instance);

                if (tagData.Selected && IsUnderContentTagSelectorPanel(instance.transform))
                {
                    SynergyOverlay.Remove(overlayParent);
                    return;
                }

                if (!instance.Interactable)
                {
                    SynergyOverlay.Apply(overlayParent, Color.clear);
                    return;
                }

                var genres = SynergyTracker.SelectedGenreIds;

                if (genres.Count == 0)
                {
                    SynergyOverlay.Apply(overlayParent, Color.clear);
                    return;
                }

                float? score = SynergyDatabase.GetSynergyScore(tagData.Id, genres);
                Color color = SynergyOverlay.ScoreToColor(
                    score,
                    SynergyOverlay.OverlayAlphaContent
                );

                SynergyOverlay.Apply(overlayParent, color);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[ContentTagCardOverlay] Exception in Apply: {ex}");
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
