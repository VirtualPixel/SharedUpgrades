using System.Collections.Generic;

namespace SharedUpgrades__.Models
{
    public sealed class UpgradeContext
    {
        public string SteamID { get; }
        public int ViewID { get; }
        public string PlayerName { get; }
        public Dictionary<string, int> LevelsBefore { get; }

        public UpgradeContext(string steamID, int viewID, string playerName, Dictionary<string, int> levelsBefore)
        {
            SteamID = steamID;
            ViewID = viewID;
            PlayerName = playerName;
            LevelsBefore = levelsBefore;
        }
    }
}
