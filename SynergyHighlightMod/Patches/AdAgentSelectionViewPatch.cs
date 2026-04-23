using System;
using HarmonyLib;
using UI.Common.Lists.ItemView;
using UI.Views;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(AdAgentSelectionView), "OnGuiShown")]
    static class AdAgentSelectionView_OnGuiShown_Patch
    {
        private static readonly System.Reflection.MethodInfo _onUpdateMethod =
            typeof(AdsAgentItemView).GetMethod(
                "OnUpdate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

        static void Postfix(AdAgentSelectionView __instance)
        {
            if (AdsHighlightTracker.CurrentMovie == null)
                return;

            try
            {
                foreach (var view in __instance.GetComponentsInChildren<AdsAgentItemView>(true))
                {
                    _onUpdateMethod?.Invoke(view, new object[] { false });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[AdAgentSelectionViewPatch] Exception: {ex}");
            }
        }
    }
}
