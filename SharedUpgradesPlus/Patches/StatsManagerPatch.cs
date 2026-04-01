using HarmonyLib;
using SharedUpgradesPlus.Services;
using UnityEngine;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyPatch(typeof(StatsManager), "Start")]
    internal class StatsManagerPatch
    {
        private static NetworkCallbackService? _callbackService;

        [HarmonyPostfix]
        public static void Postfix(StatsManager __instance)
        {
            SharedUpgradesPlus.LogVerbose("StatsManager.Start — discovering upgrades.");

            var discovered = DiscoveryService.DiscoveredUpgrades(__instance);

            SharedUpgradesPlus.LogVerbose($"Found {discovered.Vanilla.Count} vanilla and {discovered.Modded.Count} modded upgrade(s).");

            RegistryService.Instance.Clear();
            RegistryService.Instance.RegisterAll(discovered);
            ConfigService.LoadModsIntoConfig();

            if (_callbackService == null)
            {
                var go = new GameObject("NetworkCallbackService");
                _callbackService = go.AddComponent<NetworkCallbackService>();
                go.AddComponent<WatermarkService>();
                Object.DontDestroyOnLoad(go);
                SharedUpgradesPlus.LogVerbose("Created NetworkCallbackService and WatermarkService.");
            }
            else
            {
                SharedUpgradesPlus.LogVerbose("NetworkCallbackService already exists, skipping.");
            }
        }
    }
}
