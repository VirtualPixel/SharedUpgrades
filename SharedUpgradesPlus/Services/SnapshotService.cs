using System.Collections.Generic;
using System.Linq;

namespace SharedUpgradesPlus.Services
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

            SharedUpgradesPlus.LogVerbose($"[Snapshot] Player snapshot for {steamID}: {result.Count} upgrade(s).");
            return result;
        }

        public static Dictionary<string, int> SnapshotTeamMaxLevels(string? excludeSteamID = null)
        {
            Dictionary<string, int> result = [];
            if (StatsManager.instance is null) return result;

            foreach (var kvp in StatsManager.instance.dictionaryOfDictionaries
                .Where(k => RegistryService.Instance.IsRegistered(k.Key)))
            {
                var values = string.IsNullOrEmpty(excludeSteamID)
                    ? kvp.Value.Values
                    : kvp.Value.Where(p => p.Key != excludeSteamID).Select(p => p.Value);
                result[kvp.Key] = values.DefaultIfEmpty(0).Max();
            }

            SharedUpgradesPlus.LogVerbose($"[Snapshot] Team snapshot (exclude={excludeSteamID ?? "none"}): {result.Count} upgrade(s).");
            return result;
        }
    }
}
