using System.Collections.Generic;
using HarmonyLib;
using UI.Views;

namespace SynergyHighlightMod.Patches
{
    [HarmonyPatch(typeof(MovieScriptEditorView), "AddGenre")]
    static class MovieScriptEditorView_AddGenre_DefaultFractionPatch
    {
        private const string STEPS_KEY = "genre_new_added_steps";
        private static string _savedSteps;

        static void Prefix(MovieScriptEditorView __instance)
        {
            _savedSteps = null;

            var movieWrapper = Traverse.Create(__instance).Field("movieWrapper").GetValue<object>();
            if (movieWrapper == null)
                return;

            int currentCount =
                Traverse
                    .Create(movieWrapper)
                    .Property("Genres")
                    .GetValue<System.Collections.IList>()
                    ?.Count
                ?? 0;

            int desiredSteps;
            if (currentCount == 1)
                desiredSteps = 10; // 2nd genre → 50%
            else if (currentCount == 2)
                desiredSteps = 5; // 3rd genre → 25%
            else
                return;

            var gameVariables = Traverse
                .Create(__instance)
                .Field("gameVariables")
                .GetValue<object>();
            if (gameVariables == null)
                return;

            var data = Traverse
                .Create(gameVariables)
                .Field("data")
                .GetValue<Dictionary<string, string>>();
            if (data == null || !data.ContainsKey(STEPS_KEY))
                return;

            _savedSteps = data[STEPS_KEY];
            data[STEPS_KEY] = desiredSteps.ToString();
        }

        static void Postfix(MovieScriptEditorView __instance)
        {
            if (_savedSteps == null)
                return;

            var gameVariables = Traverse
                .Create(__instance)
                .Field("gameVariables")
                .GetValue<object>();
            if (gameVariables == null)
                return;

            var data = Traverse
                .Create(gameVariables)
                .Field("data")
                .GetValue<Dictionary<string, string>>();
            if (data != null)
                data[STEPS_KEY] = _savedSteps;

            _savedSteps = null;
        }
    }
}
