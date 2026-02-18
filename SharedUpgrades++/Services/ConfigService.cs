using SharedUpgrades__.Configuration;

namespace SharedUpgrades__.Services
{
    public static class ConfigService
    {
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
    }
}
