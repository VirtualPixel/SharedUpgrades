using BepInEx.Configuration;

namespace SharedUpgrades__.Configuration
{
    public enum VerbosityLevel
    {
        Off = 0,
        Debug = 1,
        Verbose = 2
    }

    internal static class PluginConfig
    {
        public static ConfigEntry<bool> EnableSharedUpgrades = null!;
        public static ConfigEntry<int> SharedUpgradeChance = null!;
        public static ConfigEntry<bool> EnableLateJoinSync = null!;
        public static ConfigEntry<bool> EnableModdedUpgrades = null!;
        public static ConfigEntry<bool> EnableSharedUpgradeHeal = null!;
        public static ConfigEntry<bool> EnableShareNotification = null!;
        public static ConfigEntry<VerbosityLevel> LoggingLevel = null!;
        public static ConfigFile? ConfigFile;

        public static void Init(ConfigFile config)
        {
            ConfigFile = config;

            EnableSharedUpgrades = config.Bind<bool>(
                "General",
                "EnableSharedUpgrades",
                true,
                "Enable or disable all upgrade sharing"
            );

            SharedUpgradeChance = config.Bind<int>(
                "General",
                "SharedUpgradesChance",
                100,
                new ConfigDescription(
                    "Chance per upgrade level to be shared with each player",
                    new AcceptableValueRange<int>(0, 100)
                )
            );

            EnableLateJoinSync = config.Bind<bool>(
                "General",
                "LateJoinSync",
                true,
                "Sync upgrades to players who join mid-run"
            );

            EnableModdedUpgrades = config.Bind<bool>(
                "General",
                "EnableModdedUpgrades",
                true,
                "Sync upgrades added by other mods"
            );

            EnableSharedUpgradeHeal = config.Bind<bool>(
                "Effects",
                "EnableSharedUpgradeHeal",
                false,
                "Heal players to full HP when receiving a shared health upgrade"
            );

            EnableShareNotification = config.Bind<bool>(
                "Effects",
                "EnableShareNotification",
                true,
                "Provide a visual effect when upgrades are shared with you"
            );

            LoggingLevel = config.Bind<VerbosityLevel>(
                "General",
                "LogLevel",
                VerbosityLevel.Off,
                "Off: key events only (sync start/result, purchases). Debug: per-player distribution results and skips. Verbose: full trace of every step."
            );
        }
    }
}
