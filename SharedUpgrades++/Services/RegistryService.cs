using SharedUpgrades__.Models;
using System.Collections.Generic;

namespace SharedUpgrades__.Services
{
    public sealed class RegistryService
    {
        private static HashSet<string> vanillaKeys = null!;
        private static HashSet<string> moddedKeys = null!;
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
            vanillaKeys = [];
            moddedKeys = [];
        }

        public void RegisterAll(DiscoveredUpgradesResult result)
        {
            vanillaKeys.UnionWith(result.Vanilla);
            moddedKeys.UnionWith(result.Modded);
            SharedUpgrades__.Logger.LogInfo($"Discovered {vanillaKeys.Count} vanilla upgrades and {moddedKeys.Count} modded upgrades.");
        }
        public void Clear()
        {
            vanillaKeys.Clear();
            moddedKeys.Clear();
        }

        public bool IsVanilla(string key)
        {
            return vanillaKeys.Contains(key);
        }
    }
}
