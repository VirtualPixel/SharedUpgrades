using BepInEx.Configuration;

namespace SharedUpgrades__.Configuration
{
    internal static class PluginConfig
    {
        public static ConfigEntry<bool> EnableSharedUpgrades = null!;
        public static ConfigEntry<int> SharedUpgradeChance = null!;
        public static ConfigEntry<bool> EnableLateJoinSync = null!;
        public static ConfigEntry<int> LateJoinSyncChance = null!;
        public static ConfigEntry<bool> EnableModdedUpgrades = null!;

        public static void Init(ConfigFile config)
        {
            EnableSharedUpgrades = config.Bind<bool>(
                "Shared Upgrades",
                "EnableSharedUpgrades",
                true,
                "Toggle for Shared Upgrades"
            );

            SharedUpgradeChance = config.Bind<int>(
                "Shared Upgrades Chance",
                "SharedUpgradesChance",
                100,
                new ConfigDescription(
                    "0-100% chance the upgrade will be applied to other players",
                    new AcceptableValueRange<int>(0, 100)
                )
            );

            EnableLateJoinSync = config.Bind<bool>(
                "Late Join Sync",
                "LateJoinSync",
                true,
                "If players join late, should the previous upgrades be applied?"
            );

            LateJoinSyncChance = config.Bind<int>(
                "Late Join Sync Chance",
                "LateJoinSyncChance",
                100,
                new ConfigDescription(
                    "0-100% chance for previously applied upgrades to apply to players who join late",
                    new AcceptableValueRange<int>(0,100)
                )
            );

            EnableModdedUpgrades = config.Bind<bool>(
                "Modded Upgrade Sync",
                "EnableModdedUpgrades",
                true,
                "This feature will sync upgrades introduced by mods."
            );
        }
    }
}
