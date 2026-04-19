using HarmonyLib;
using UI.Views;

namespace SynergyHighlightMod.Patches
{
#if DEBUG
    [HarmonyPatch(typeof(MovieScriptEditorView), "OnShow")]
    static class MovieScriptEditorView_OnShow_AutomatedBehaviorScenarioPatch
    {
        static void Postfix(MovieScriptEditorView __instance)
        {
            Plugin.Log?.LogInfo(
                "[SynergyHighlight][AutoScenario] MovieScriptEditorView.OnShow observed; scheduling scenario runner."
            );
            AutomatedBehaviorScenarioTests.TrySchedule(__instance);
        }
    }

    [HarmonyPatch(typeof(MovieScriptEditorView), "OnTabSelected")]
    static class MovieScriptEditorView_OnTabSelected_AutomatedBehaviorScenarioPatch
    {
        static void Postfix(MovieScriptEditorView __instance, string tabId)
        {
            Plugin.Log?.LogInfo(
                $"[SynergyHighlight][AutoScenario] OnTabSelected('{tabId}') observed; retrying scenario scheduling."
            );
            AutomatedBehaviorScenarioTests.TrySchedule(__instance);
        }
    }
#endif
}
