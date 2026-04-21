using Enums;
using HarmonyLib;
using UI.Views;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(MovieScriptEditorView), "OnTabSelected")]
    static class MovieScriptEditorView_OnTabSelected_SynergyPatch
    {
        static void Postfix(MovieScriptEditorView __instance, string tabId)
        {
            var t = Traverse.Create(__instance);
            if (t.Field("tagsByType").GetValue() == null)
                return;

            if (tabId == TagTypes.Setting.ToString().ToUpper())
            {
                ForceUpdateViews(t.Field("settingsList").GetValue<object>());
            }
            else if (tabId == TagTypes.Content.ToString().ToUpper())
            {
                ForceUpdateViews(t.Field("contentSupportingCharactersList").GetValue<object>());
                ForceUpdateViews(t.Field("contentThemesAndEventsList").GetValue<object>());

                var display = t.Field("settingItemViewDisplay").GetValue<object>();
                if (display != null)
                    Traverse
                        .Create(display)
                        .Method("UpdateView", new object[] { false })
                        .GetValue();
            }
        }

        private static void ForceUpdateViews(object list)
        {
            if (list == null)
                return;
            Traverse.Create(list).Method("UpdateViews", new object[] { false }).GetValue();
        }
    }
}
