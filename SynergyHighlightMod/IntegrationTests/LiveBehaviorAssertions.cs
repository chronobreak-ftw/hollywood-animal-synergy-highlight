using System;
using System.Collections.Generic;
using System.Linq;
using Data.GameObject;
using HarmonyLib;
using UI.Common.Lists.ItemView;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace SynergyHighlightMod
{
    internal static class LiveBehaviorAssertions
    {
        private static readonly HashSet<string> _reportedFailures = new HashSet<string>();
        private static readonly object _lock = new object();
        private static BepInEx.Logging.ManualLogSource _log;
        private static bool _enabled;

        internal static void Initialize(BepInEx.Logging.ManualLogSource log, bool enabled)
        {
            _log = log;
            _enabled = enabled;

            if (_enabled)
            {
                _log.LogInfo(
                    "[SynergyHighlight][BehaviorTest] Live behavior assertions enabled (stale-color/deselection checks)."
                );
            }
        }

        internal static void ValidateSettingCardColor(
            ToggleImageListItemView instance,
            string tagId
        )
        {
            if (!_enabled || instance == null || string.IsNullOrEmpty(tagId))
                return;

            var genres = SynergyTracker.SelectedGenreIds;
            Color expected =
                genres.Count == 0
                    ? Color.clear
                    : SynergyOverlay.ScoreToColor(
                        SynergyDatabase.GetSynergyScore(tagId, genres),
                        SynergyOverlay.OverlayAlphaSetting
                    );

            ValidateOverlayColor(instance.gameObject, expected, $"SettingTag:{tagId}");
        }

        internal static void ValidateContentCardColor(
            ContentTagCardItemView instance,
            string tagId,
            bool selectedInSelectorPanel,
            bool interactable
        )
        {
            if (!_enabled || instance == null || string.IsNullOrEmpty(tagId))
                return;

            if (selectedInSelectorPanel)
            {
                // SynergyOverlay.Remove uses Object.Destroy, which is applied at frame end.
                // Validating immediate absence here creates false failures.
                return;
            }

            Color expected;
            if (!interactable || SynergyTracker.SelectedGenreIds.Count == 0)
            {
                expected = Color.clear;
            }
            else
            {
                expected = SynergyOverlay.ScoreToColor(
                    SynergyDatabase.GetSynergyScore(tagId, SynergyTracker.SelectedGenreIds),
                    SynergyOverlay.OverlayAlphaContent
                );
            }

            ValidateOverlayColor(instance.gameObject, expected, $"ContentTag:{tagId}");
        }

        internal static void ValidateGenreCardBorder(
            GenreTagItemView instance,
            string genreId,
            bool isSelected
        )
        {
            if (!_enabled || instance == null || string.IsNullOrEmpty(genreId))
                return;

            Color expected;
            var selected = SynergyTracker.SelectedGenreIds;
            if (isSelected || selected.Count == 0)
            {
                expected = Color.clear;
            }
            else
            {
                expected = SynergyOverlay.GenrePairScoreToColor(
                    SynergyDatabase.GetBestGenrePairScore(genreId, selected),
                    SynergyOverlay.OverlayAlphaGenre
                );
            }

            ValidateBorderColor(instance.gameObject, expected, $"GenreTag:{genreId}");
        }

        internal static void ValidateTrackerVsMovieGenres(MovieScriptEditorView instance)
        {
            if (!_enabled || instance == null)
                return;

            try
            {
                var movieWrapper = Traverse
                    .Create(instance)
                    .Field("movieWrapper")
                    .GetValue<object>();
                if (movieWrapper == null)
                    return;

                var genres = Traverse
                    .Create(movieWrapper)
                    .Property("Genres")
                    .GetValue<List<TagData>>();
                if (genres == null)
                    return;

                var movieSet = new HashSet<string>(
                    genres.Where(g => g != null && !string.IsNullOrEmpty(g.Id)).Select(g => g.Id),
                    StringComparer.OrdinalIgnoreCase
                );
                var trackerSet = new HashSet<string>(
                    SynergyTracker.SelectedGenreIds,
                    StringComparer.OrdinalIgnoreCase
                );

                bool mismatch = !movieSet.SetEquals(trackerSet);
                if (mismatch)
                {
                    ReportOnce(
                        $"TrackerMovieMismatch:{string.Join(",", movieSet.OrderBy(x => x))}:{string.Join(",", trackerSet.OrderBy(x => x))}",
                        $"Tracker/movie genre mismatch. Movie=[{string.Join(", ", movieSet)}], Tracker=[{string.Join(", ", trackerSet)}]"
                    );
                }
            }
            catch (Exception ex)
            {
                ReportOnce(
                    "ValidateTrackerVsMovieGenresException",
                    $"Exception while validating tracker vs movie genres: {ex}"
                );
            }
        }

        private static void ValidateOverlayColor(GameObject cardGO, Color expected, string context)
        {
            Transform overlay = cardGO.transform.Find("__SynergyOverlay__");

            if (expected.a <= 0.001f)
            {
                bool invalidVisible = overlay != null && overlay.gameObject.activeSelf;
                ReportIf(
                    invalidVisible,
                    $"OverlayShouldBeClear:{context}",
                    $"{context} expected clear overlay but overlay is active."
                );
                return;
            }

            bool missing = overlay == null || !overlay.gameObject.activeSelf;
            ReportIf(
                missing,
                $"OverlayMissing:{context}",
                $"{context} expected visible overlay but none is active."
            );
            if (missing)
                return;

            var img = overlay.GetComponent<Image>();
            if (img == null)
            {
                ReportOnce(
                    $"OverlayImageMissing:{context}",
                    $"{context} overlay object exists but has no Image component."
                );
                return;
            }

            ReportIf(
                !ColorsApproximatelyEqual(img.color, expected),
                $"OverlayColorMismatch:{context}:{ColorFingerprint(expected)}:{ColorFingerprint(img.color)}",
                $"{context} overlay color mismatch. Expected={expected}, Actual={img.color}"
            );
        }

        private static void ValidateBorderColor(GameObject cardGO, Color expected, string context)
        {
            Transform border = cardGO.transform.Find("__SynergyBorder__");

            if (expected.a <= 0.001f)
            {
                bool invalidVisible = border != null && border.gameObject.activeSelf;
                ReportIf(
                    invalidVisible,
                    $"BorderShouldBeClear:{context}",
                    $"{context} expected clear border but border is active."
                );
                return;
            }

            bool missing = border == null || !border.gameObject.activeSelf;
            ReportIf(
                missing,
                $"BorderMissing:{context}",
                $"{context} expected visible border but none is active."
            );
            if (missing)
                return;

            var firstStrip = border.GetComponentInChildren<Image>();
            if (firstStrip == null)
            {
                ReportOnce(
                    $"BorderImageMissing:{context}",
                    $"{context} border object exists but has no Image strips."
                );
                return;
            }

            Color expectedBorder = new Color(expected.r, expected.g, expected.b, 0.90f);
            ReportIf(
                !ColorsApproximatelyEqual(firstStrip.color, expectedBorder),
                $"BorderColorMismatch:{context}:{ColorFingerprint(expectedBorder)}:{ColorFingerprint(firstStrip.color)}",
                $"{context} border color mismatch. Expected={expectedBorder}, Actual={firstStrip.color}"
            );
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            const float eps = 0.015f;
            return Math.Abs(a.r - b.r) < eps
                && Math.Abs(a.g - b.g) < eps
                && Math.Abs(a.b - b.b) < eps
                && Math.Abs(a.a - b.a) < eps;
        }

        private static string ColorFingerprint(Color c) =>
            $"{Mathf.RoundToInt(c.r * 1000f)}-{Mathf.RoundToInt(c.g * 1000f)}-{Mathf.RoundToInt(c.b * 1000f)}-{Mathf.RoundToInt(c.a * 1000f)}";

        private static void ReportIf(bool condition, string key, string message)
        {
            if (!condition)
                return;
            ReportOnce(key, message);
        }

        private static void ReportOnce(string key, string message)
        {
            if (_log == null)
                return;

            lock (_lock)
            {
                if (_reportedFailures.Contains(key))
                    return;
                _reportedFailures.Add(key);
            }

            _log.LogError($"[SynergyHighlight][BehaviorTest][FAIL] {message}");
        }
    }
}
