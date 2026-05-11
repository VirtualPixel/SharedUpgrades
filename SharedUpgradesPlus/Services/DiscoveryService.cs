using HarmonyLib;
using SharedUpgradesPlus.Models;
using System.Collections.Generic;
using System.Linq;

namespace SharedUpgradesPlus.Services
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

            // Also pull from REPOLib's API in case dict-population is late
            // or the key prefix changes again like MoreUpgrades 1.6.7 did.
            foreach (var key in RepoLibInterop.GetModdedUpgradeKeys())
            {
                if (!vanillaUpgrades.Contains(key))
                    moddedUpgrades.Add(key);
            }

            return new DiscoveredUpgradesResult(vanilla: vanillaUpgrades, modded: moddedUpgrades);
        }
    }
}
