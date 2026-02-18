using System.Collections.Generic;

namespace SharedUpgrades__.Models
{
    public sealed class UpgradeContext(string steamID, int viewID, string playerName, Dictionary<string, int> levelsBefore)
    {
        public string SteamID { get; } = steamID;
        public int ViewID { get; } = viewID;
        public string PlayerName { get; } = playerName;
        public Dictionary<string, int> LevelsBefore { get; } = levelsBefore;
    }
}
