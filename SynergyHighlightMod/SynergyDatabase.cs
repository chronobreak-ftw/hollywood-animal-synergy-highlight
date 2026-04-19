using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace SynergyHighlightMod
{
    public static class SynergyDatabase
    {
        private static Dictionary<string, Dictionary<string, float>> _compat;
        private static Dictionary<string, Dictionary<string, (float art, float com)>> _genrePairs;

        public static bool IsLoaded => _compat != null;

        public static void Load(string streamingAssetsPath, ManualLogSource log)
        {
            LoadCompatibility(streamingAssetsPath, log);
            LoadGenrePairs(streamingAssetsPath, log);
        }

        private static void LoadCompatibility(string root, ManualLogSource log)
        {
            try
            {
                string path = Path.Combine(root, "Data", "Configs", "TagCompatibilityData.json");
                var raw = JsonConvert.DeserializeObject<
                    Dictionary<string, Dictionary<string, string>>
                >(File.ReadAllText(path));

                _compat = new Dictionary<string, Dictionary<string, float>>(
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var outer in raw)
                {
                    var inner = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in outer.Value)
                        if (
                            float.TryParse(
                                kv.Value,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out float f
                            )
                        )
                            inner[kv.Key] = f;
                    _compat[outer.Key] = inner;
                }
                log.LogInfo($"[SynergyDB] Compatibility: {_compat.Count} tag entries.");
            }
            catch (Exception ex)
            {
                log.LogError("[SynergyDB] Failed to load TagCompatibilityData.json: " + ex);
            }
        }

        private static void LoadGenrePairs(string root, ManualLogSource log)
        {
            try
            {
                string path = Path.Combine(root, "Data", "Configs", "GenrePairs.json");
                var raw = JsonConvert.DeserializeObject<
                    Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                >(File.ReadAllText(path));

                _genrePairs = new Dictionary<string, Dictionary<string, (float, float)>>(
                    StringComparer.OrdinalIgnoreCase
                );
                foreach (var outer in raw)
                {
                    var inner = new Dictionary<string, (float, float)>(
                        StringComparer.OrdinalIgnoreCase
                    );
                    foreach (var kv in outer.Value)
                    {
                        float art = 0f,
                            com = 0f;
                        if (kv.Value.TryGetValue("Item1", out string a))
                            float.TryParse(
                                a,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out art
                            );
                        if (kv.Value.TryGetValue("Item2", out string c))
                            float.TryParse(
                                c,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out com
                            );
                        inner[kv.Key] = (art, com);
                    }
                    _genrePairs[outer.Key] = inner;
                }
                log.LogInfo($"[SynergyDB] GenrePairs: {_genrePairs.Count} genre entries.");
            }
            catch (Exception ex)
            {
                log.LogError("[SynergyDB] Failed to load GenrePairs.json: " + ex);
            }
        }

        public static float GetCompatibility(string a, string b) =>
            SynergyLogic.GetCompatibility(_compat, a, b);

        public static float? GetSynergyScore(string tagId, IEnumerable<string> genreIds) =>
            SynergyLogic.GetSynergyScore(_compat, tagId, genreIds);

        public static float Normalize(float score) => SynergyLogic.Normalize(score);

        public static float GetGenrePairSum(string genre1, string genre2) =>
            SynergyLogic.GetGenrePairSum(_genrePairs, genre1, genre2);

        public static float? GetBestGenrePairScore(
            string candidateGenre,
            IEnumerable<string> selectedGenreIds
        ) => SynergyLogic.GetBestGenrePairScore(_genrePairs, candidateGenre, selectedGenreIds);
    }
}
