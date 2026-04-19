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
        private static ItemContainerData _savedContentSelectorData;

        internal static void Initialize(BepInEx.Logging.ManualLogSource log, bool enabled)
        {
            _log = log;
            _enabled = enabled;
            _isRunning = false;
            _completedForSession = false;
            _savedContentSelectorData = null;
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
            var checkGenres = genreTags.Take(4).ToList();
            TagData targetSetting = settingTags[0];
            string targetContentId = null;

            // ---- Phase 1: Zero genres — setting and content overlays must be clear ----
            _log.LogInfo("[SynergyHighlight][AutoScenario] Phase 1: zero genres.");
            yield return ApplyGenreSet(view, genreTags, Array.Empty<TagData>());

            yield return SelectTab(view, TagTypes.Setting);
            AssertCardOverlayMatchesSetting(view, targetSetting.Id, Color.clear);

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureContentCardsVisible(view);
            targetContentId = FindFirstVisibleContentCardTagId(view);
            if (string.IsNullOrEmpty(targetContentId))
            {
                int available = GetContentCardsFromSelectorPanel(view).Count;
                int sceneTotal = UnityEngine
                    .Object.FindObjectsOfType<ContentTagCardItemView>(true)
                    .Length;
                Fail(
                    $"No visible content card found on Content tab. ListCards={available}, SceneCards={sceneTotal}."
                );
                yield break;
            }
            AssertCardOverlayMatchesContent(view, targetContentId, Color.clear);

            // ---- Phase 2: Single genre — overlays must reflect one-genre score ----
            _log.LogInfo("[SynergyHighlight][AutoScenario] Phase 2: single genre.");
            yield return ApplyGenreSet(view, genreTags, new[] { setA[0] });

            yield return SelectTab(view, TagTypes.Setting);
            AssertCardOverlayMatchesSetting(
                view,
                targetSetting.Id,
                CaptureExpectedOverlayColor(targetSetting.Id, SynergyOverlay.OverlayAlphaSetting)
            );

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            AssertCardOverlayMatchesContent(
                view,
                targetContentId,
                CaptureExpectedOverlayColor(targetContentId, SynergyOverlay.OverlayAlphaContent)
            );

            // ---- Phase 3: Two genres (setA) — genre borders, setting overlay, content overlay ----
            _log.LogInfo(
                "[SynergyHighlight][AutoScenario] Phase 3: two genres + border check (setA)."
            );
            yield return ApplyGenreSet(view, genreTags, setA);

            yield return SelectTab(view, TagTypes.Genre);
            AssertGenreBordersForList(checkGenres);

            yield return SelectTab(view, TagTypes.Setting);
            Color settingA = CaptureExpectedOverlayColor(
                targetSetting.Id,
                SynergyOverlay.OverlayAlphaSetting
            );
            AssertCardOverlayMatchesSetting(view, targetSetting.Id, settingA);

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            Color contentA = CaptureExpectedOverlayColor(
                targetContentId,
                SynergyOverlay.OverlayAlphaContent
            );
            AssertCardOverlayMatchesContent(view, targetContentId, contentA);

            // ---- Phase 4: Switch to setB — stale-color check A→B ----
            _log.LogInfo(
                "[SynergyHighlight][AutoScenario] Phase 4: switch to setB, stale-color check."
            );
            yield return SelectTab(view, TagTypes.Genre);
            yield return ApplyGenreSet(view, genreTags, setB);
            AssertGenreBordersForList(checkGenres);

            yield return SelectTab(view, TagTypes.Setting);
            Color settingB = CaptureExpectedOverlayColor(
                targetSetting.Id,
                SynergyOverlay.OverlayAlphaSetting
            );
            {
                bool ok = AssertCardOverlayMatchesSetting(view, targetSetting.Id, settingB);
                if (ok && settingA != settingB)
                    Pass(
                        "Setting stale-color check passed: overlay updated when switching genres A→B."
                    );
                else if (ok)
                    _log.LogInfo(
                        "[SynergyHighlight][AutoScenario] Setting A→B produced same color (correctness still validated)."
                    );
            }

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            Color contentB = CaptureExpectedOverlayColor(
                targetContentId,
                SynergyOverlay.OverlayAlphaContent
            );
            {
                bool ok = AssertCardOverlayMatchesContent(view, targetContentId, contentB);
                if (ok && contentA != contentB)
                    Pass(
                        "Content stale-color check passed: overlay updated when switching genres A→B."
                    );
                else if (ok)
                    _log.LogInfo(
                        "[SynergyHighlight][AutoScenario] Content A→B produced same color (correctness still validated)."
                    );
            }

            // ---- Phase 5: Switch back to setA — stale-color check B→A ----
            _log.LogInfo(
                "[SynergyHighlight][AutoScenario] Phase 5: switch back to setA, stale-color check."
            );
            yield return SelectTab(view, TagTypes.Genre);
            yield return ApplyGenreSet(view, genreTags, setA);

            yield return SelectTab(view, TagTypes.Setting);
            Color settingA2 = CaptureExpectedOverlayColor(
                targetSetting.Id,
                SynergyOverlay.OverlayAlphaSetting
            );
            {
                bool ok = AssertCardOverlayMatchesSetting(view, targetSetting.Id, settingA2);
                if (ok && settingB != settingA2)
                    Pass(
                        "Setting stale-color check passed: overlay updated when switching genres B→A."
                    );
                else if (ok)
                    _log.LogInfo(
                        "[SynergyHighlight][AutoScenario] Setting B→A produced same color (correctness still validated)."
                    );
            }

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            Color contentA2 = CaptureExpectedOverlayColor(
                targetContentId,
                SynergyOverlay.OverlayAlphaContent
            );
            {
                bool ok = AssertCardOverlayMatchesContent(view, targetContentId, contentA2);
                if (ok && contentB != contentA2)
                    Pass(
                        "Content stale-color check passed: overlay updated when switching genres B→A."
                    );
                else if (ok)
                    _log.LogInfo(
                        "[SynergyHighlight][AutoScenario] Content B→A produced same color (correctness still validated)."
                    );
            }

            // ---- Phase 6: Deselect all — tracker empty, all overlays clear ----
            _log.LogInfo("[SynergyHighlight][AutoScenario] Phase 6: deselect all.");
            yield return SelectTab(view, TagTypes.Genre);
            yield return ApplyGenreSet(view, genreTags, Array.Empty<TagData>());

            AssertGenreBordersForList(checkGenres);

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
                Pass("Deselection check passed (movie genres and tracker both empty).");
            else
                Fail(
                    $"Deselection mismatch. MovieGenres={movieGenres?.Count ?? -1}, TrackerGenres={SynergyTracker.SelectedGenreIds.Count}"
                );

            yield return SelectTab(view, TagTypes.Setting);
            AssertCardOverlayMatchesSetting(view, targetSetting.Id, Color.clear);

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            AssertCardOverlayMatchesContent(view, targetContentId, Color.clear);

            // ---- Phase 7: Re-select setA — overlays restored, no stale state from deselect ----
            _log.LogInfo(
                "[SynergyHighlight][AutoScenario] Phase 7: re-select setA, verify no stale state."
            );
            yield return ApplyGenreSet(view, genreTags, setA);

            yield return SelectTab(view, TagTypes.Setting);
            {
                Color expected = CaptureExpectedOverlayColor(
                    targetSetting.Id,
                    SynergyOverlay.OverlayAlphaSetting
                );
                bool ok = AssertCardOverlayMatchesSetting(view, targetSetting.Id, expected);
                if (ok)
                    Pass("Re-selection check passed for setting: overlay restored after deselect.");
            }

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            {
                Color expected = CaptureExpectedOverlayColor(
                    targetContentId,
                    SynergyOverlay.OverlayAlphaContent
                );
                bool ok = AssertCardOverlayMatchesContent(view, targetContentId, expected);
                if (ok)
                    Pass("Re-selection check passed for content: overlay restored after deselect.");
            }

            // ---- Phase 8: All visible content cards with setA genres ----
            _log.LogInfo(
                "[SynergyHighlight][AutoScenario] Phase 8: all visible content cards check."
            );
            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            AssertAllContentCardOverlays(view);

            // ---- Phase 9: Three genres — overlays update for genre borders, setting, content ----
            _log.LogInfo("[SynergyHighlight][AutoScenario] Phase 9: three genres.");
            var setABC = new[] { genreTags[0], genreTags[1], genreTags[2] };
            yield return ApplyGenreSet(view, genreTags, setABC);

            yield return SelectTab(view, TagTypes.Genre);
            AssertGenreBordersForList(checkGenres);

            yield return SelectTab(view, TagTypes.Setting);
            {
                Color expected = CaptureExpectedOverlayColor(
                    targetSetting.Id,
                    SynergyOverlay.OverlayAlphaSetting
                );
                bool ok = AssertCardOverlayMatchesSetting(view, targetSetting.Id, expected);
                if (ok)
                    Pass("Three-genre setting overlay correct.");
            }

            yield return SelectTab(view, TagTypes.Content);
            yield return EnsureTargetContentCardVisible(view, targetContentId);
            {
                Color expected = CaptureExpectedOverlayColor(
                    targetContentId,
                    SynergyOverlay.OverlayAlphaContent
                );
                bool ok = AssertCardOverlayMatchesContent(view, targetContentId, expected);
                if (ok)
                    Pass("Three-genre content overlay correct.");
            }

            // Clean up: restore setA so teardown is predictable
            yield return ApplyGenreSet(view, genreTags, setA);

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

        // Opens the content selector and saves the slot data used, so subsequent calls can reopen the same slot.
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
                _savedContentSelectorData = selectorData;
                Traverse
                    .Create(view)
                    .Method("OnContentCardSlotSelected", new object[] { selectorData })
                    .GetValue();
            }

            for (int i = 0; i < 20; i++)
                yield return null;

            LogContentCardCandidates(view, "after opening content selector");
        }

        // Reopens the same slot used during the first EnsureContentCardsVisible call so we can
        // find the same target card even when the active slot has changed between tab visits.
        private static IEnumerator EnsureTargetContentCardVisible(
            MovieScriptEditorView view,
            string targetId
        )
        {
            if (FindContentCard(view, targetId) != null)
                yield break;

            LogContentCardCandidates(view, "before re-opening content selector");

            if (_savedContentSelectorData != null)
            {
                Traverse
                    .Create(view)
                    .Method("OnContentCardSlotSelected", new object[] { _savedContentSelectorData })
                    .GetValue();

                for (int i = 0; i < 20; i++)
                    yield return null;
            }

            LogContentCardCandidates(view, "after re-opening content selector");
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

        private static Color CaptureExpectedBorderColor(string genreId)
        {
            var selected = SynergyTracker.SelectedGenreIds;
            if (selected.Count == 0)
                return Color.clear;
            float? pairScore = SynergyDatabase.GetBestGenrePairScore(genreId, selected);
            return SynergyOverlay.GenrePairScoreToColor(
                pairScore,
                SynergyOverlay.OverlayAlphaGenre
            );
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

        private static bool AssertGenreBordersForList(List<TagData> genres)
        {
            var selected = SynergyTracker.SelectedGenreIds;
            bool allOk = true;
            foreach (var genre in genres)
            {
                var card = FindGenreCard(genre.Id);
                if (card == null)
                {
                    _log.LogInfo(
                        $"[SynergyHighlight][AutoScenario] GenreBorder: card not found for '{genre.Id}' (may not be visible), skipping."
                    );
                    continue;
                }

                Color expected;
                if (genre.Selected || selected.Count == 0)
                    expected = Color.clear;
                else
                    expected = CaptureExpectedBorderColor(genre.Id);

                if (!AssertBorderColor(card.gameObject, expected, $"GenreBorder:{genre.Id}"))
                    allOk = false;
            }
            return allOk;
        }

        private static void AssertAllContentCardOverlays(MovieScriptEditorView view)
        {
            var cards = GetContentCardsFromSelectorPanel(view);
            int checked_ = 0;
            int failed = 0;
            foreach (var card in cards)
            {
                if (!card.gameObject.activeInHierarchy || !card.Interactable)
                    continue;
                var tag = Traverse.Create(card).Property("TagData").GetValue<TagData>();
                if (tag?.Id == null || tag.Selected)
                    continue;

                Color expected = CaptureExpectedOverlayColor(
                    tag.Id,
                    SynergyOverlay.OverlayAlphaContent
                );
                if (!AssertOverlayColor(card.gameObject, expected, $"Content:{tag.Id}"))
                    failed++;
                checked_++;
            }

            if (checked_ == 0)
                _log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Phase 8: no interactable unselected content cards found."
                );
            else if (failed == 0)
                Pass($"Phase 8: all {checked_} content card overlays correct for current genres.");
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

        private static GenreTagItemView FindGenreCard(string tagId)
        {
            foreach (var v in UnityEngine.Object.FindObjectsOfType<GenreTagItemView>(true))
            {
                var data = Traverse.Create(v).Property("Data").GetValue<ItemContainerData>();
                var display = data?.GetData<GenreTagItemView.GenreDisplayData>();
                if (
                    display?.tag != null
                    && string.Equals(display.tag.Id, tagId, StringComparison.OrdinalIgnoreCase)
                )
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

        // Border strips use an internal BORDER_ALPHA (0.90f) regardless of the passed base color alpha,
        // so we compare RGB only to avoid false failures from the alpha difference.
        private static bool AssertBorderColor(GameObject cardGO, Color expected, string context)
        {
            Transform border = cardGO.transform.Find("__SynergyBorder__");

            if (expected.a <= 0.001f)
            {
                if (border != null && border.gameObject.activeSelf)
                {
                    Fail($"{context} expected clear border but border is visible.");
                    return false;
                }
                Pass($"{context} border correctly clear.");
                return true;
            }

            if (border == null || !border.gameObject.activeSelf)
            {
                Fail($"{context} expected visible border but border missing or hidden.");
                return false;
            }

            if (border.childCount == 0)
            {
                Fail($"{context} border container has no strip children.");
                return false;
            }

            var img = border.GetChild(0).GetComponent<Image>();
            if (img == null)
            {
                Fail($"{context} border strip has no Image component.");
                return false;
            }

            const float eps = 0.015f;
            bool rgbMatch =
                Math.Abs(img.color.r - expected.r) < eps
                && Math.Abs(img.color.g - expected.g) < eps
                && Math.Abs(img.color.b - expected.b) < eps;

            if (!rgbMatch)
            {
                Fail(
                    $"{context} border color mismatch. ExpectedRGB=({expected.r:F2},{expected.g:F2},{expected.b:F2}), ActualRGB=({img.color.r:F2},{img.color.g:F2},{img.color.b:F2})"
                );
                return false;
            }

            Pass($"{context} border matches expected.");
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
