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
            AutomatedBehaviorScenarioTests.TrySchedule(__instance);
        }
    }

    [HarmonyPatch(typeof(MovieScriptEditorView), "OnTabSelected")]
    static class MovieScriptEditorView_OnTabSelected_AutomatedBehaviorScenarioPatch
    {
        static void Postfix(MovieScriptEditorView __instance, string tabId)
        {
            AutomatedBehaviorScenarioTests.TrySchedule(__instance);
        }
    }
#endif
}
