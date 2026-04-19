using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SynergyHighlightMod
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = base.Logger;

            string streamingAssets = Path.Combine(Application.streamingAssetsPath);
            SynergyDatabase.Load(streamingAssets, Log);

            new Harmony(PluginInfo.GUID).PatchAll();

            Log.LogInfo($"{PluginInfo.Name} v{PluginInfo.Version} loaded.");
        }
    }

    internal static class PluginInfo
    {
        public const string GUID = "hollywoodanimal.synergyhighlight";
        public const string Name = "Synergy Highlight";
        public const string Version = "1.0.0";
    }
}
