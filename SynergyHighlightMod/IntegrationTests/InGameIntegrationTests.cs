using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Data.GameObject;
using HarmonyLib;
using UI.Common.Lists.ItemView;
using UI.Views;
using UnityEngine;

namespace SynergyHighlightMod
{
    internal static class InGameIntegrationTests
    {
        private sealed class TestContext
        {
            private readonly ManualLogSource _log;
            private readonly bool _failFast;

            internal int Passed { get; private set; }
            internal int Failed { get; private set; }

            internal TestContext(ManualLogSource log, bool failFast)
            {
                _log = log;
                _failFast = failFast;
            }

            internal void Run(string name, Action test)
            {
                try
                {
                    test();
                    Passed++;
                    _log.LogInfo($"[SynergyHighlight][InGameTest][PASS] {name}");
                }
                catch (Exception ex)
                {
                    Failed++;
                    _log.LogError($"[SynergyHighlight][InGameTest][FAIL] {name} :: {ex}");
                    if (_failFast)
                    {
                        throw;
                    }
                }
            }
        }

        internal static void Run(Harmony harmony, ManualLogSource log, bool failFast)
        {
            if (harmony == null)
            {
                log.LogError(
                    "[SynergyHighlight][InGameTest] Cannot run tests because Harmony is null."
                );
                return;
            }

            var ctx = new TestContext(log, failFast);
            log.LogInfo("[SynergyHighlight][InGameTest] Starting runtime integration tests...");

            ctx.Run(
                "Synergy data is loaded",
                () =>
                {
                    Require(
                        SynergyDatabase.IsLoaded,
                        "SynergyDatabase.IsLoaded is false. Check StreamingAssets config files."
                    );
                }
            );

            ctx.Run("Overlay smoke test", OverlaySmokeTest);
            ctx.Run("Tracker event smoke test", TrackerSmokeTest);
            ctx.Run("Logic smoke test", LogicSmokeTest);
            ctx.Run(
                "ContentTagSelectorPanel type resolves",
                () =>
                {
                    var panelType = Type.GetType(
                        "UI.Common.SubPanels.ContentTagSelectorPanel, Assembly-CSharp"
                    );
                    Require(panelType != null, "ContentTagSelectorPanel type not found.");
                }
            );

            ctx.Run(
                "Patch exists: GenreTagItemView.OnUpdate",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(GenreTagItemView), "OnUpdate")
                    )
            );
            ctx.Run(
                "Patch exists: GenreTagItemView.OnSelectedStateChanged",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(GenreTagItemView), "OnSelectedStateChanged")
                    )
            );
            ctx.Run(
                "Patch exists: ContentTagCardItemView.OnUpdate",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(ContentTagCardItemView), "OnUpdate")
                    )
            );
            ctx.Run(
                "Patch exists: ContentTagCardItemView.OnSelectionChanged",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(ContentTagCardItemView), "OnSelectionChanged")
                    )
            );
            ctx.Run(
                "Patch exists: ToggleImageListItemView.OnUpdate",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(ToggleImageListItemView), "OnUpdate")
                    )
            );
            ctx.Run(
                "Patch exists: MovieScriptEditorView.OnShow",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(MovieScriptEditorView), "OnShow")
                    )
            );
            ctx.Run(
                "Patch exists: MovieScriptEditorView.AddGenre",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(MovieScriptEditorView), "AddGenre")
                    )
            );
            ctx.Run(
                "Patch exists: MovieScriptEditorView.OnTabSelected",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(MovieScriptEditorView), "OnTabSelected")
                    )
            );
            ctx.Run(
                "Patch exists: MovieScriptEditorView.OnGenreTagFractionUpdated",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(
                            typeof(MovieScriptEditorView),
                            "OnGenreTagFractionUpdated"
                        )
                    )
            );
            ctx.Run(
                "Patch exists: MovieScriptEditorView.CheckGenreQueue",
                () =>
                    RequirePatched(
                        harmony,
                        AccessTools.Method(typeof(MovieScriptEditorView), "CheckGenreQueue")
                    )
            );

            log.LogInfo(
                $"[SynergyHighlight][InGameTest] Completed. Passed={ctx.Passed}, Failed={ctx.Failed}"
            );
        }

        private static void OverlaySmokeTest()
        {
            var root = new GameObject("__Synergy_InGameTest_OverlayRoot__");
            try
            {
                root.AddComponent<RectTransform>();
                var green = new Color(0f, 1f, 0f, 0.25f);

                SynergyOverlay.Apply(root, green);
                var overlay = root.transform.Find("__SynergyOverlay__");
                Require(overlay != null, "Overlay object was not created.");
                Require(
                    overlay.gameObject.activeSelf,
                    "Overlay should be active for non-clear color."
                );

                SynergyOverlay.Apply(root, Color.clear);
                Require(
                    !overlay.gameObject.activeSelf,
                    "Overlay should be hidden for clear color."
                );

                SynergyOverlay.ApplyBorder(root, green);
                var border = root.transform.Find("__SynergyBorder__");
                Require(border != null, "Border object was not created.");
                Require(
                    border.gameObject.activeSelf,
                    "Border should be active for non-clear color."
                );

                SynergyOverlay.ApplyBorder(root, Color.clear);
                Require(!border.gameObject.activeSelf, "Border should be hidden for clear color.");
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static void TrackerSmokeTest()
        {
            SynergyTracker.Clear();
            int calls = 0;
            Action handler = () => calls++;
            SynergyTracker.OnGenresChanged += handler;
            try
            {
                SynergyTracker.SetGenre("Action", true);
                SynergyTracker.SetGenre("Action", true);
                SynergyTracker.SetGenre("Action", false);

                Require(calls == 2, $"Expected 2 genre-change events but got {calls}.");
            }
            finally
            {
                SynergyTracker.OnGenresChanged -= handler;
                SynergyTracker.Clear();
            }
        }

        private static void LogicSmokeTest()
        {
            var compat = new Dictionary<string, Dictionary<string, float>>(
                StringComparer.OrdinalIgnoreCase
            )
            {
                {
                    "TagA",
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Action", 4f },
                        { "Comedy", 2f },
                    }
                },
            };

            float? score = SynergyLogic.GetSynergyScore(
                compat,
                "TagA",
                new[] { "Action", "Comedy" }
            );
            Require(score.HasValue, "Expected non-null synergy score.");
            Require(
                Math.Abs(score.Value - 3f) < 0.0001f,
                $"Expected score 3.0 but got {score.Value}."
            );
        }

        private static void RequirePatched(Harmony harmony, System.Reflection.MethodBase method)
        {
            Require(
                method != null,
                "Target method was not found. Game update likely changed signature."
            );

            var patchInfo = Harmony.GetPatchInfo(method);
            bool patchedByThisMod = patchInfo != null && patchInfo.Owners.Contains(harmony.Id);
            Require(
                patchedByThisMod,
                $"Method {method.DeclaringType?.Name}.{method.Name} is not patched by {harmony.Id}."
            );
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
