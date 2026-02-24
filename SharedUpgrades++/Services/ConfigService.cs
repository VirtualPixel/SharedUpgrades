using SharedUpgrades__.Configuration;
using System.Collections.Generic;
using BepInEx.Configuration;
using SharedUpgrades__.Models;

namespace SharedUpgrades__.Services
{
    public static class ConfigService
    {
        private static readonly Dictionary<string, ConfigEntry<bool>> upgradeToggles = [];
        private static readonly Dictionary<string, ConfigEntry<int>> limitSliders = [];

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

        public static int UpgradeShareLimit(string upgradeKey)
        {
            if (limitSliders.TryGetValue(upgradeKey, out var limit))
                return limit.Value;
            return 0;
        }

        public static void LoadModsIntoConfig()
        {
            if (PluginConfig.ConfigFile == null) return;

            RegisterToggles(RegistryService.Instance.VanillaUpgrades, "Vanilla Upgrades");
            RegisterToggles(RegistryService.Instance.ModdedUpgrades, "Modded Upgrades");
        }

        private static void RegisterToggles(IEnumerable<Upgrade> upgrades, string section)
        {
            foreach (Upgrade upgrade in upgrades)
            {
                if (upgradeToggles.ContainsKey(upgrade.Name)) continue;

                upgradeToggles[upgrade.Name] = PluginConfig.ConfigFile!.Bind(
                    section,
                    upgrade.CleanName,
                    true,
                    $"Enable sharing for {upgrade.CleanName}"
                );

                limitSliders[upgrade.Name] = PluginConfig.ConfigFile!.Bind(
                    section,
                    upgrade.CleanName + " Share Limit",
                    0,
                    new ConfigDescription(
                        "Max shared level for this upgrade (0 = unlimited)",
                        new AcceptableValueRange<int>(0, 100)
                    )
                );
            }
        }
    }
}
