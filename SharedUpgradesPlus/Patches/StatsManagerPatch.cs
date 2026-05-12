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
            SharedUpgradesPlus.LogVerbose("StatsManager.Start: initial discovery.");
            RefreshRegistry(__instance);

            if (_callbackService == null)
            {
                var go = new GameObject("SharedUpgradesPlus_Services");
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

        internal static void RefreshRegistry(StatsManager statsManager)
        {
            var discovered = DiscoveryService.DiscoveredUpgrades(statsManager);
            SharedUpgradesPlus.LogVerbose($"Found {discovered.Vanilla.Count} vanilla and {discovered.Modded.Count} modded upgrade(s).");

            RegistryService.Instance.Clear();
            RegistryService.Instance.RegisterAll(discovered);
            ConfigService.LoadModsIntoConfig();
        }
    }

    // REPOLib registers its modded upgrades on StatsManager.RunStartStats postfix.
    // HarmonyAfter("REPOLib") ensures we re-discover after their keys are in the dict.
    [HarmonyPatch(typeof(StatsManager), "RunStartStats")]
    [HarmonyAfter("REPOLib")]
    internal class StatsManagerRunStartStatsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(StatsManager __instance)
        {
            SharedUpgradesPlus.LogVerbose("StatsManager.RunStartStats: re-discovering after REPOLib.");
            StatsManagerPatch.RefreshRegistry(__instance);
        }
    }

    // Save loads can pull in modded upgrade keys persisted from previous runs,
    // so refresh once load completes.
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.LoadGame))]
    internal class StatsManagerLoadGamePatch
    {
        [HarmonyPostfix]
        public static void Postfix(StatsManager __instance)
        {
            SharedUpgradesPlus.LogVerbose("StatsManager.LoadGame: re-discovering after save load.");
            StatsManagerPatch.RefreshRegistry(__instance);
        }
    }
}
