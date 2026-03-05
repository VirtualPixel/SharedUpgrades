using SharedUpgrades__.Models;
using System.Collections.Generic;
using System.Linq;

namespace SharedUpgrades__.Services
{
    public sealed class RegistryService
    {
        public IReadOnlyCollection<Upgrade> VanillaUpgrades => vanillaUpgrades;
        public IReadOnlyCollection<Upgrade> ModdedUpgrades => moddedUpgrades;
        private HashSet<Upgrade> vanillaUpgrades = null!;
        private HashSet<Upgrade> moddedUpgrades = null!;
        private static readonly RegistryService instance = new();

        public static RegistryService Instance => instance;

        private RegistryService()
        {
            vanillaUpgrades = [];
            moddedUpgrades = [];
        }

        public void RegisterAll(DiscoveredUpgradesResult result)
        {
            vanillaUpgrades.UnionWith(result.Vanilla.Select(MakeUpgradeFromKey));
            moddedUpgrades.UnionWith(result.Modded.Select(MakeUpgradeFromKey));

            SharedUpgrades__.Logger.LogInfo($"Discovered {vanillaUpgrades.Count} vanilla and {moddedUpgrades.Count} modded upgrade(s).");

            if (result.Vanilla.Count > 0)
                SharedUpgrades__.LogVerbose($"Vanilla: {string.Join(", ", result.Vanilla)}");
            if (result.Modded.Count > 0)
                SharedUpgrades__.LogVerbose($"Modded: {string.Join(", ", result.Modded)}");
        }

        public void Clear()
        {
            SharedUpgrades__.LogVerbose($"Registry cleared ({vanillaUpgrades.Count} vanilla, {moddedUpgrades.Count} modded).");
            vanillaUpgrades.Clear();
            moddedUpgrades.Clear();
        }

        public bool IsVanilla(string key) => vanillaUpgrades.Any(upgrade => upgrade.Name.Equals(key));

        public bool IsRegistered(string key) => IsVanilla(key) || moddedUpgrades.Any(upgrade => upgrade.Name.Equals(key));

        private Upgrade MakeUpgradeFromKey(string key) => new Upgrade(Name: key);
    }
}
