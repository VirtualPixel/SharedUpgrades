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

        public static RegistryService Instance
        {
            get
            {
                return instance;
            }
        }

        private RegistryService()
        {
            vanillaUpgrades = [];
            moddedUpgrades = [];
        }

        public void RegisterAll(DiscoveredUpgradesResult result)
        {
            vanillaUpgrades.UnionWith(result.Vanilla.Select(MakeUpgradeFromKey));
            moddedUpgrades.UnionWith(result.Modded.Select(MakeUpgradeFromKey));
            SharedUpgrades__.Logger.LogInfo($"Discovered {vanillaUpgrades.Count} vanilla upgrades and {moddedUpgrades.Count} modded upgrades.");
        }
        public void Clear()
        {
            vanillaUpgrades.Clear();
            moddedUpgrades.Clear();
        }

        public bool IsVanilla(string key)
        {
            return vanillaUpgrades
                .Any(upgrade => upgrade.Name.Equals(key));
        }

        public bool IsRegistered(string key)
        {
            return IsVanilla(key)
                || moddedUpgrades.Any(upgrade => upgrade.Name.Equals(key));
        }

        private Upgrade MakeUpgradeFromKey(string key)
        {
            return new Upgrade(Name: key);
        }
    }
}