using GUISystemModule;
using HarmonyLib;
using UI.Views;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(MovieScriptEditorView), "OnShow")]
    static class MovieScriptEditorView_OnShow_Patch
    {
        static void Prefix(MovieScriptEditorView __instance, GUIParams param, bool withIntro)
        {
            SynergyTracker.Clear();
        }
    }
}
