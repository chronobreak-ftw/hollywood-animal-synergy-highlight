using System;
using System.Collections.Generic;
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
        private static readonly HashSet<ToggleImageListItemView> _trackedCards =
            new HashSet<ToggleImageListItemView>();

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
            foreach (var card in new List<ToggleImageListItemView>(_trackedCards))
            {
                if (card != null)
                    ApplyColor(card);
            }
        }

        static void Postfix(ToggleImageListItemView __instance)
        {
            if (!(__instance is SettingTagItemView))
                return;

            EnsureSubscribed();
            ApplyColor(__instance);
        }

        private static void ApplyColor(ToggleImageListItemView instance)
        {
            var data = Traverse.Create(instance).Property("Data").GetValue<ItemContainerData>();
            if (data == null)
            {
                _trackedCards.Remove(instance);
                return;
            }

            var tagData = data.GetData<TagData>();
            if (tagData == null)
            {
                _trackedCards.Remove(instance);
                return;
            }

            _trackedCards.Add(instance);

            var genres = SynergyTracker.SelectedGenreIds;
            if (genres.Count == 0)
            {
                SynergyOverlay.Apply(instance.gameObject, Color.clear);
                return;
            }

            float? score = SynergyDatabase.GetSynergyScore(tagData.Id, genres);
            Color color = SynergyOverlay.ScoreToColor(score, SynergyOverlay.OverlayAlphaSetting);

            SynergyOverlay.Apply(instance.gameObject, color);
        }
    }
}
