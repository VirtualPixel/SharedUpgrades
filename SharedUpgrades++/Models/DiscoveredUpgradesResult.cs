using System.Collections.Generic;

namespace SharedUpgrades__.Models
{
    public sealed class DiscoveredUpgradesResult
    {
        public HashSet<string> Vanilla { get; }
        public HashSet<string> Modded { get; }

        public DiscoveredUpgradesResult(HashSet<string> vanilla, HashSet<string> modded)
        {
            Vanilla = vanilla;
            Modded = modded;
        }
    }
}
