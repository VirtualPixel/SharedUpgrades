using System.Collections.Generic;

namespace SharedUpgrades__.Models
{
    public sealed class DiscoveredUpgradesResult(HashSet<string> vanilla, HashSet<string> modded)
    {
        public HashSet<string> Vanilla { get; } = vanilla;
        public HashSet<string> Modded { get; } = modded;
    }
}
