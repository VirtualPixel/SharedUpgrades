using SharedUpgrades__.Configuration;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace SharedUpgrades__.Services
{
    public static class ConfigService
    {
        private static readonly Dictionary<string, ConfigEntry<bool>> upgradeToggles = new();

        public static bool IsSharedUpgradesEnabled()
        {
            return PluginConfig.EnableSharedUpgrades.Value;
        }

        public static bool IsModdedUpgradesEnabled()
        {
            return PluginConfig.EnableModdedUpgrades.Value;
        }

        public static bool IsLateJoinSyncEnabled()
        {
            return PluginConfig.EnableLateJoinSync.Value;
        }

        public static bool RollSharedUpgradesChance()
        {
            return Roll(PluginConfig.SharedUpgradeChance.Value);
        }

        public static bool RollLateJoinSyncChance()
        {
            return Roll(PluginConfig.LateJoinSyncChance.Value);
        }

        private static bool Roll(int chance)
        {
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        public static bool IsUpgradeEnabled(string upgradeKey)
        {
            if (upgradeToggles.TryGetValue(upgradeKey, out var toggle))
                return toggle.Value;

            return true;
        }

        public static void LoadModsIntoConfig()
        {
            if (PluginConfig.ConfigFile == null) return;

            RegisterToggles(RegistryService.Instance.VanillaKeys, "Vanilla Upgrades");
            RegisterToggles(RegistryService.Instance.ModdedKeys, "Modded Upgrades");
        }

        private static void RegisterToggles(IEnumerable<string> keys, string section)
        {
            foreach (string key in keys)
            {
                if (upgradeToggles.ContainsKey(key)) continue;

                string displayName = key.StartsWith("playerUpgrade")
                    ? key.Substring("playerUpgrade".Length)
                    : key;

                upgradeToggles[key] = PluginConfig.ConfigFile!.Bind(
                    section,
                    displayName,
                    true,
                    $"Enable sharing for {displayName}"
                );
            }
        }
    }
}
