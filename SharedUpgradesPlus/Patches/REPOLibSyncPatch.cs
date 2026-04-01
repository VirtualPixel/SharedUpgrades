using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyPatch(typeof(StatsManager), nameof(StatsManager.RunStartStats))]
    internal class REPOLibSyncPatch
    {
        private static readonly Type _upgradeType = AccessTools.TypeByName("REPOLib.Modules.Upgrades");
        private static readonly Type _playerUpgradeType = AccessTools.TypeByName("REPOLib.Modules.PlayerUpgrade");
        private static readonly FieldInfo? _playerDictionaryField = _playerUpgradeType != null ? AccessTools.Field(_playerUpgradeType, "PlayerDictionary") : null;
        private static readonly FieldInfo? _playerUpgradesField = _upgradeType != null ? AccessTools.Field(_upgradeType, "_playerUpgrades") : null;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix()
        {
            if (_playerDictionaryField == null || _playerUpgradesField == null || StatsManager.instance == null) return;

            if (_playerUpgradesField.GetValue(null) is not IDictionary playerUpgrades) return;

            SharedUpgradesPlus.LogVerbose($"Syncing {playerUpgrades.Count} REPOLib upgrade(s) to StatsManager.");

            int synced = 0;
            foreach (DictionaryEntry entry in playerUpgrades)
            {
                if (entry.Value == null) continue;

                string fullKey = $"playerUpgrade{entry.Key}";

                if (!StatsManager.instance.dictionaryOfDictionaries.TryGetValue(fullKey, out var upgradeDict)) continue;

                _playerDictionaryField.SetValue(entry.Value, upgradeDict);
                SharedUpgradesPlus.LogInfo($"Synced PlayerDictionary for {fullKey}.");
                synced++;
            }

            SharedUpgradesPlus.LogVerbose($"REPOLib sync done — {synced}/{playerUpgrades.Count} upgrade(s).");
        }
    }
}
