using System.Collections.Generic;
using System.Linq;

namespace SharedUpgrades__.Services
{
    public static class SnapshotService
    {
        public static Dictionary<string, int> SnapshotPlayerStats(string steamID)
        {
            if (string.IsNullOrEmpty(steamID) || StatsManager.instance is null)
                return [];

            var result = StatsManager.instance.dictionaryOfDictionaries
                .Where(kvp => RegistryService.Instance.IsRegistered(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetValueOrDefault(steamID, 0));

            SharedUpgrades__.LogVerbose($"[Snapshot] Player snapshot for {steamID} — {result.Count} upgrade(s).");
            return result;
        }

        public static Dictionary<string, int> SnapshotTeamMaxLevels()
        {
            Dictionary<string, int> result = [];
            if (StatsManager.instance is null) return result;

            foreach (var kvp in StatsManager.instance.dictionaryOfDictionaries
                .Where(k => RegistryService.Instance.IsRegistered(k.Key)))
            {
                result[kvp.Key] = kvp.Value.Values.DefaultIfEmpty(0).Max();
            }

            SharedUpgrades__.LogVerbose($"[Snapshot] Team snapshot — {result.Count} upgrade(s).");
            return result;
        }
    }
}
