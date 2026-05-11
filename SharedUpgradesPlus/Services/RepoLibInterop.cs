using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SharedUpgradesPlus.Services
{
    // Reads REPOLib's Upgrades.PlayerUpgrades list via reflection so we don't
    // need a compile-time reference to REPOLib. Returns empty if it isn't loaded.
    internal static class RepoLibInterop
    {
        private static bool _resolved;
        private static PropertyInfo? _playerUpgradesProp;
        private static PropertyInfo? _upgradeIdProp;

        public static HashSet<string> GetModdedUpgradeKeys()
        {
            var keys = new HashSet<string>();
            if (!ResolveReflection()) return keys;

            try
            {
                if (_playerUpgradesProp!.GetValue(null) is not IEnumerable list) return keys;
                foreach (var upgrade in list)
                {
                    if (upgrade == null) continue;
                    if (_upgradeIdProp!.GetValue(upgrade) is string id && !string.IsNullOrEmpty(id))
                        keys.Add("playerUpgrade" + id);
                }
            }
            catch (Exception e)
            {
                SharedUpgradesPlus.LogVerbose($"[RepoLibInterop] reflection failed: {e.Message}");
            }

            return keys;
        }

        private static bool ResolveReflection()
        {
            if (_resolved) return _playerUpgradesProp != null && _upgradeIdProp != null;
            _resolved = true;

            var upgradesType = AccessTools.TypeByName("REPOLib.Modules.Upgrades");
            if (upgradesType == null) return false;

            _playerUpgradesProp = AccessTools.Property(upgradesType, "PlayerUpgrades");
            if (_playerUpgradesProp == null) return false;

            var playerUpgradeType = AccessTools.TypeByName("REPOLib.Modules.PlayerUpgrade");
            if (playerUpgradeType == null) return false;

            _upgradeIdProp = AccessTools.Property(playerUpgradeType, "UpgradeId");
            return _upgradeIdProp != null;
        }
    }
}
