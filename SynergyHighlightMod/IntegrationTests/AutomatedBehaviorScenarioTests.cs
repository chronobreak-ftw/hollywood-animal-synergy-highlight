using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.GameObject;
using Data.GameObject.Movie;
using Enums;
using HarmonyLib;
using PP.OptimizedList;
using UI.Common.Lists.ItemData;
using UI.Common.Lists.ItemView;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace SynergyHighlightMod
{
    internal static class AutomatedBehaviorScenarioTests
    {
        private static BepInEx.Logging.ManualLogSource _log;
        private static bool _enabled;
        private static bool _isRunning;
        private static bool _completedForSession;

        internal static void Initialize(BepInEx.Logging.ManualLogSource log, bool enabled)
        {
            _log = log;
            _enabled = enabled;
            _isRunning = false;
            _completedForSession = false;
            if (_enabled)
            {
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Automated behavior scenarios enabled."
                );
            }
        }

        internal static void TrySchedule(MovieScriptEditorView view)
        {
            if (!_enabled)
            {
                _log?.LogInfo("[SynergyHighlight][AutoScenario] Not scheduling: feature disabled.");
                return;
            }
            if (_isRunning)
            {
                _log?.LogInfo(
                    "[SynergyHighlight][AutoScenario] Not scheduling: scenario already running."
                );
                return;
            }
            if (_completedForSession)
                return;
            if (view == null)
            {
                _log?.LogError("[SynergyHighlight][AutoScenario] Not scheduling: view is null.");
                return;
            }

            _isRunning = true;
            // Run on the editor view itself to avoid plugin-instance lifecycle edge cases.
            view.StartCoroutine(RunSafe(view));
            _log?.LogInfo("[SynergyHighlight][AutoScenario] Coroutine scheduled successfully.");
        }

        private static IEnumerator RunSafe(MovieScriptEditorView view)
        {
            var inner = RunCore(view);
            while (true)
            {
                object current;
                try
                {
                    if (!inner.MoveNext())
                        break;
                    current = inner.Current;
                }
                catch (Exception ex)
                {
                    Fail($"Unhandled exception during automated scenario: {ex}");
                    break;
                }

                yield return current;
            }

            TryRestoreEditorInteractivity(view);

            if (WasScenarioSuccessful)
            {
                _completedForSession = true;
            }

            _isRunning = false;
        }

        private static bool WasScenarioSuccessful { get; set; }

        private static IEnumerator RunCore(MovieScriptEditorView view)
        {
            WasScenarioSuccessful = false;
            _log.LogInfo("[SynergyHighlight][AutoScenario] Starting automated scenario run...");

            bool ready = false;
            for (int i = 0; i < 3600; i++)
            {
                if (
                    Traverse.Create(view).Field("movieWrapper").GetValue<object>() != null
                    && Traverse.Create(view).Field("tagsByType").GetValue<object>() != null
                )
                {
                    ready = true;
                    break;
                }
                yield return null;
            }

            if (!ready)
            {
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Deferring run: editor not ready yet. Will retry on next tab change."
                );
                yield break;
            }

            var tagsByType = Traverse
                .Create(view)
                .Field("tagsByType")
                .GetValue<Dictionary<TagTypes, List<TagData>>>();
            if (tagsByType == null)
            {
                Fail("tagsByType is null.");
                yield break;
            }

            if (
                !tagsByType.TryGetValue(TagTypes.Genre, out var genreTags)
                || genreTags == null
                || genreTags.Count < 4
            )
            {
                Fail("Need at least 4 genre tags to run automated scenario.");
                yield break;
            }

            if (
                !tagsByType.TryGetValue(TagTypes.Setting, out var settingTags)
                || settingTags == null
                || settingTags.Count == 0
            )
            {
                Fail("No setting tags available for scenario.");
                yield break;
            }

            var setA = new[] { genreTags[0], genreTags[1] };
            var setB = new[] { genreTags[2], genreTags[3] };

            TagData targetSetting = settingTags[0];
            string targetContentId = null;

            yield return ApplyGenreSet(view, genreTags, setA);
            yield return SelectTab(view, TagTypes.Setting);
            Color settingA = CaptureExpectedOverlayColor(
                targetSetting.Id,
                SynergyOverlay.OverlayAlphaSetting
            );

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureContentCardsVisible(view);
            targetContentId = FindFirstVisibleContentCardTagId(view);
            if (string.IsNullOrEmpty(targetContentId))
            {
                int availableCards = GetContentCardsFromSelectorPanel(view).Count;
                int totalCardsInScene = UnityEngine
                    .Object.FindObjectsOfType<ContentTagCardItemView>(true)
                    .Length;
                Fail(
                    $"No visible content card found on Content tab. ListCards={availableCards}, SceneCards={totalCardsInScene}."
                );
                yield break;
            }
            Color contentA = CaptureExpectedOverlayColor(
                targetContentId,
                SynergyOverlay.OverlayAlphaContent
            );

            yield return SelectTab(view, TagTypes.Genre);
            yield return ApplyGenreSet(view, genreTags, setB);

            yield return SelectTab(view, TagTypes.Setting);
            Color settingB = CaptureExpectedOverlayColor(
                targetSetting.Id,
                SynergyOverlay.OverlayAlphaSetting
            );
            bool settingOverlayOk = AssertCardOverlayMatchesSetting(
                view,
                targetSetting.Id,
                settingB
            );

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureContentCardsVisible(view);
            Color contentB = CaptureExpectedOverlayColor(
                targetContentId,
                SynergyOverlay.OverlayAlphaContent
            );
            bool contentOverlayOk = AssertCardOverlayMatchesContent(
                view,
                targetContentId,
                contentB
            );

            if (settingOverlayOk && settingA != settingB)
                Pass("Stale color check passed for setting card.");
            else if (settingOverlayOk)
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Setting sample produced same color for both genre sets (still validated correctness)."
                );

            if (contentOverlayOk && contentA != contentB)
                Pass("Stale color check passed for content card.");
            else if (contentOverlayOk)
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Content sample produced same color for both genre sets (still validated correctness)."
                );

            yield return SelectTab(view, TagTypes.Genre);
            yield return ApplyGenreSet(view, genreTags, Array.Empty<TagData>());

            var movieWrapper = Traverse.Create(view).Field("movieWrapper").GetValue<object>();
            var movieGenres = Traverse
                .Create(movieWrapper)
                .Property("Genres")
                .GetValue<List<TagData>>();

            if (
                movieGenres != null
                && movieGenres.Count == 0
                && SynergyTracker.SelectedGenreIds.Count == 0
            )
            {
                Pass("Deselection check passed (movie genres and tracker both empty).");
            }
            else
            {
                Fail(
                    $"Deselection mismatch. MovieGenres={movieGenres?.Count ?? -1}, TrackerGenres={SynergyTracker.SelectedGenreIds.Count}"
                );
            }

            _log.LogInfo("[SynergyHighlight][AutoScenario] Automated scenario run completed.");
            WasScenarioSuccessful = true;
        }

        private static IEnumerator SelectTab(MovieScriptEditorView view, TagTypes tab)
        {
            string tabId = tab.ToString().ToUpper();
            bool switchedThroughTabSystem = false;

            try
            {
                var tabSystem = Traverse.Create(view).Field("tagTabsSystem").GetValue<object>();
                if (tabSystem != null)
                {
                    Traverse
                        .Create(tabSystem)
                        .Method("SelectTab", new object[] { tabId, false })
                        .GetValue();
                    switchedThroughTabSystem = true;
                }
            }
            catch (Exception ex)
            {
                _log.LogInfo(
                    $"[SynergyHighlight][AutoScenario] SelectTab via tab system failed for '{tabId}', fallback to direct call. {ex.GetType().Name}: {ex.Message}"
                );
            }

            if (!switchedThroughTabSystem)
            {
                Traverse.Create(view).Method("OnTabSelected", new object[] { tabId }).GetValue();
            }

            _log.LogInfo($"[SynergyHighlight][AutoScenario] Requested tab '{tabId}'.");

            if (tab == TagTypes.Setting || tab == TagTypes.Content)
            {
                Traverse.Create(view).Method("OnResultsStateChanged").GetValue();
                ForceListUpdate(Traverse.Create(view).Field("settingsList").GetValue<object>());
                ForceListUpdate(
                    Traverse
                        .Create(view)
                        .Field("contentSupportingCharactersList")
                        .GetValue<object>()
                );
                ForceListUpdate(
                    Traverse.Create(view).Field("contentThemesAndEventsList").GetValue<object>()
                );
            }

            for (int i = 0; i < 8; i++)
                yield return null;
        }

        private static void ForceListUpdate(object list)
        {
            if (list == null)
                return;
            Traverse.Create(list).Method("UpdateViews", new object[] { false }).GetValue();
        }

        private static IEnumerator EnsureContentCardsVisible(MovieScriptEditorView view)
        {
            if (!string.IsNullOrEmpty(FindFirstVisibleContentCardTagId(view)))
                yield break;

            LogContentCardCandidates(view, "before opening content selector");

            var selectorView = Traverse
                .Create(view)
                .Field("contentThemesAndEventsSelector")
                .GetValue<object>();
            var selectorData = Traverse
                .Create(selectorView)
                .Property("Data")
                .GetValue<ItemContainerData>();
            if (selectorData != null)
            {
                Traverse
                    .Create(view)
                    .Method("OnContentCardSlotSelected", new object[] { selectorData })
                    .GetValue();
            }

            for (int i = 0; i < 20; i++)
                yield return null;

            LogContentCardCandidates(view, "after opening content selector");
        }

        private static IEnumerator ApplyGenreSet(
            MovieScriptEditorView view,
            List<TagData> allGenres,
            TagData[] desired
        )
        {
            var desiredIds = new HashSet<string>(
                desired.Select(g => g.Id),
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var tag in allGenres)
            {
                bool wantSelected = desiredIds.Contains(tag.Id);
                if (tag.Selected == wantSelected)
                    continue;

                tag.Selected = wantSelected;

                var displayData = new GenreTagItemView.GenreDisplayData
                {
                    tag = tag,
                    fromMovie = Traverse
                        .Create(view)
                        .Field("movieWrapper")
                        .GetValue<MovieDataWrapper>(),
                    maxAvailableGenres = allGenres.Count,
                };
                var item = new SelectableObjectItemData(displayData, 0);
                Traverse
                    .Create(view)
                    .Method("OnGenreAddedOrRemoved", new object[] { item })
                    .GetValue();
            }

            for (int i = 0; i < 120; i++)
            {
                var queue = Traverse
                    .Create(view)
                    .Field("genreAdditionAndRemovalQueue")
                    .GetValue<System.Collections.ICollection>();
                if (queue == null || queue.Count == 0)
                    break;
                yield return null;
            }

            for (int i = 0; i < 10; i++)
                yield return null;
        }

        private static Color CaptureExpectedOverlayColor(string tagId, float alpha)
        {
            if (SynergyTracker.SelectedGenreIds.Count == 0)
                return Color.clear;
            float? score = SynergyDatabase.GetSynergyScore(tagId, SynergyTracker.SelectedGenreIds);
            return SynergyOverlay.ScoreToColor(score, alpha);
        }

        private static bool AssertCardOverlayMatchesSetting(
            MovieScriptEditorView view,
            string tagId,
            Color expected
        )
        {
            var card = FindSettingCard(tagId);
            if (card == null)
            {
                Fail($"Setting card not found for tag '{tagId}'.");
                return false;
            }
            return AssertOverlayColor(card.gameObject, expected, $"Setting:{tagId}");
        }

        private static bool AssertCardOverlayMatchesContent(
            MovieScriptEditorView view,
            string tagId,
            Color expected
        )
        {
            var card = FindContentCard(view, tagId);
            if (card == null)
            {
                Fail($"Content card not found for tag '{tagId}'.");
                return false;
            }
            return AssertOverlayColor(card.gameObject, expected, $"Content:{tagId}");
        }

        private static ToggleImageListItemView FindSettingCard(string tagId)
        {
            foreach (var v in UnityEngine.Object.FindObjectsOfType<ToggleImageListItemView>(true))
            {
                if (!(v is SettingTagItemView))
                    continue;
                var data = Traverse.Create(v).Property("Data").GetValue<ItemContainerData>();
                var tag = data?.GetData<TagData>();
                if (tag != null && string.Equals(tag.Id, tagId, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }

        private static ContentTagCardItemView FindContentCard(
            MovieScriptEditorView view,
            string tagId
        )
        {
            foreach (var v in GetContentCardsFromSelectorPanel(view))
            {
                var tag = Traverse.Create(v).Property("TagData").GetValue<TagData>();
                if (tag != null && string.Equals(tag.Id, tagId, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }

        private static string FindFirstVisibleContentCardTagId(MovieScriptEditorView view)
        {
            foreach (var v in GetContentCardsFromSelectorPanel(view))
            {
                if (!v.gameObject.activeInHierarchy)
                    continue;
                var tag = Traverse.Create(v).Property("TagData").GetValue<TagData>();
                if (tag?.Id == null)
                    continue;
                if (tag.Selected)
                    continue;
                return tag.Id;
            }
            return null;
        }

        private static void LogContentCardCandidates(MovieScriptEditorView view, string phase)
        {
            var cards =
                view == null
                    ? new List<ContentTagCardItemView>()
                    : GetContentCardsFromSelectorPanel(view);
            var samples = new List<string>();
            int max = Math.Min(cards.Count, 12);
            for (int i = 0; i < max; i++)
            {
                var c = cards[i];
                var t = Traverse.Create(c).Property("TagData").GetValue<TagData>();
                string id = t?.Id ?? "<null>";
                string cat = t?.Category.ToString() ?? "<null>";
                samples.Add(
                    $"{id}({cat}) active={c.gameObject.activeInHierarchy} interactable={c.Interactable} selected={(t?.Selected ?? false)}"
                );
            }

            _log.LogInfo(
                $"[SynergyHighlight][AutoScenario] Content card candidates {phase}: total={cards.Count}; sample=[{string.Join("; ", samples)}]"
            );
        }

        private static List<ContentTagCardItemView> GetContentCardsFromSelectorPanel(
            MovieScriptEditorView view
        )
        {
            var cards = new List<ContentTagCardItemView>();
            var panel = Traverse.Create(view).Field("contentSelectorPanel").GetValue<object>();
            if (panel == null)
                return cards;

            AddCardsFromList(
                Traverse.Create(panel).Field("selectionList").GetValue<object>(),
                cards
            );
            AddCardsFromList(Traverse.Create(panel).Field("entityList").GetValue<object>(), cards);
            AddCardsFromList(
                Traverse.Create(panel).Field("archetypeList").GetValue<object>(),
                cards
            );
            return cards;
        }

        private static void AddCardsFromList(object list, List<ContentTagCardItemView> cards)
        {
            if (list == null)
                return;

            var containers = Traverse.Create(list).Field("activeItemContainers").GetValue<IList>();
            if (containers == null)
                return;

            foreach (var container in containers)
            {
                object viewObj = Traverse.Create(container).Property("ItemView").GetValue<object>();
                if (viewObj is ContentTagCardItemView card)
                {
                    cards.Add(card);
                }
            }
        }

        private static void TryRestoreEditorInteractivity(MovieScriptEditorView view)
        {
            try
            {
                Traverse.Create(view).Method("OnDeselectContentSlot").GetValue();
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Restored editor interactivity by deselecting content slot/sidebar."
                );
            }
            catch (Exception ex)
            {
                _log.LogInfo(
                    $"[SynergyHighlight][AutoScenario] Could not auto-restore interactivity: {ex.GetType().Name}: {ex.Message}"
                );
            }
        }

        private static bool AssertOverlayColor(GameObject cardGO, Color expected, string context)
        {
            Transform overlay = cardGO.transform.Find("__SynergyOverlay__");

            if (expected.a <= 0.001f)
            {
                if (overlay != null && overlay.gameObject.activeSelf)
                {
                    Fail($"{context} expected clear overlay but overlay is visible.");
                    return false;
                }
                Pass($"{context} overlay correctly clear for current genres.");
                return true;
            }

            if (overlay == null || !overlay.gameObject.activeSelf)
            {
                Fail($"{context} expected visible overlay but overlay missing.");
                return false;
            }

            var img = overlay.GetComponent<Image>();
            if (img == null)
            {
                Fail($"{context} expected overlay image component but none exists.");
                return false;
            }

            if (!ColorsApproximatelyEqual(img.color, expected))
            {
                Fail($"{context} overlay mismatch. Expected={expected}, Actual={img.color}");
                return false;
            }

            Pass($"{context} overlay matches expected genres.");
            return true;
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            const float eps = 0.015f;
            return Math.Abs(a.r - b.r) < eps
                && Math.Abs(a.g - b.g) < eps
                && Math.Abs(a.b - b.b) < eps
                && Math.Abs(a.a - b.a) < eps;
        }

        private static void Pass(string message) =>
            _log.LogInfo($"[SynergyHighlight][AutoScenario][PASS] {message}");

        private static void Fail(string message) =>
            _log.LogError($"[SynergyHighlight][AutoScenario][FAIL] {message}");
    }
}
