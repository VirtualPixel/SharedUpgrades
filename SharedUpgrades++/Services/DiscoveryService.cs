using HarmonyLib;
using SharedUpgrades__.Models;
using System.Collections.Generic;
using System.Linq;

namespace SharedUpgrades__.Services
{
    public static class DiscoveryService
    {
        public static DiscoveredUpgradesResult DiscoveredUpgrades(StatsManager statsManager)
        {
            var vanillaUpgrades = new HashSet<string>();
            var moddedUpgrades = new HashSet<string>();

            foreach (var key in statsManager.dictionaryOfDictionaries.Keys
                .Where(key => key.StartsWith("playerUpgrade")))
            {
                if (AccessTools.Field(typeof(StatsManager), key) != null)
                    vanillaUpgrades.Add(key);
                else
                    moddedUpgrades.Add(key);
            }

            return new DiscoveredUpgradesResult(vanilla: vanillaUpgrades, modded: moddedUpgrades);
        }
    }
}
