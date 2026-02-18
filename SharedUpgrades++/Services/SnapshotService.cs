using System.Collections.Generic;
using System.Linq;

namespace SharedUpgrades__.Services
{
    public static class SnapshotService
    {
        public static Dictionary<string, int> SnapshotPlayerStats(string steamID)
        {
            if (string.IsNullOrEmpty(steamID) || StatsManager.instance is null)
                return new Dictionary<string, int>();

            return StatsManager.instance.dictionaryOfDictionaries
                .Where(kvp => kvp.Key.StartsWith("playerUpgrade"))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetValueOrDefault(steamID, 0));
        }

        public static Dictionary<string, int> SnapshotTeamMaxLevels()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            if (StatsManager.instance is null) return result;

            foreach (var kvp in StatsManager.instance.dictionaryOfDictionaries
                .Where(k => k.Key.StartsWith("playerUpgrade")))
            {
                result[kvp.Key] = kvp.Value.Values.DefaultIfEmpty(0).Max();
            }

            return result;
        }
    }
}
