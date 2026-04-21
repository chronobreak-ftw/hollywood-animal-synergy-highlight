using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
#if DEBUG
using BepInEx.Configuration;
#endif

namespace SynergyHighlightMod
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
#if DEBUG
        private ConfigEntry<bool> _enableAutomatedBehaviorScenarios;
#endif
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;
#if DEBUG
            _enableAutomatedBehaviorScenarios = Config.Bind(
                "Testing",
                "EnableAutomatedBehaviorScenarios",
                false,
                "Automatically runs scripted tab/genre behavior scenarios in Movie Script Editor."
            );
#endif

            string streamingAssets = Path.Combine(Application.streamingAssetsPath);
            SynergyDatabase.Load(
                streamingAssets,
                msg => Log.LogError(msg),
                msg => Log.LogWarning(msg)
            );

            if (!SynergyDatabase.IsLoaded)
            {
                Log.LogError(
                    "[SynergyHighlight] WARNING: Failed to load synergy data files. "
                        + "Verify TagCompatibilityData.json and GenrePairs.json exist in "
                        + $"{streamingAssets}\\Data\\Configs\\. Mod will not function correctly."
                );
            }

            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll();
#if DEBUG
            AutomatedBehaviorScenarioTests.Initialize(Log, _enableAutomatedBehaviorScenarios.Value);
#endif

            Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");

#if DEBUG
            Log.LogInfo(
                $"[SynergyHighlight][Testing] EnableAutomatedBehaviorScenarios={_enableAutomatedBehaviorScenarios.Value}"
            );
            if (_enableAutomatedBehaviorScenarios.Value)
            {
                Log.LogInfo(
                    "[SynergyHighlight][AutoScenario] Waiting for MovieScriptEditorView to open before running scenarios."
                );
            }
#endif
        }
    }

    internal static class PluginInfo
    {
        public const string GUID = "hollywoodanimal.synergyhighlight";
        public const string Name = "Synergy Highlight";
        public const string Version = "1.1.0";
    }
}
