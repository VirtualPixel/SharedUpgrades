using HarmonyLib;
using SharedUpgrades__.Models;
using SharedUpgrades__.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SharedUpgrades__.Patches
{
    [HarmonyPatch(typeof(ItemUpgrade), "PlayerUpgrade")]
    internal class SharedUpgradesPatch
    {
        private static readonly FieldInfo _itemToggle = AccessTools.Field(typeof(ItemUpgrade), "itemToggle");
        private static readonly FieldInfo _playerTogglePhotonId = AccessTools.Field(typeof(ItemToggle), "playerTogglePhotonID");
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");
        private static readonly FieldInfo _playerName = AccessTools.Field(typeof(PlayerAvatar), "playerName");
        private static readonly FieldInfo _itemAttributes = AccessTools.Field(typeof(ItemUpgrade), "itemAttributes");

        private static PlayerAvatar? GetUpgradePlayer(ItemUpgrade instance, out int viewID)
        {
            viewID = 0;

            if (_itemToggle.GetValue(instance) is not ItemToggle { toggleState: true } toggle)
                return null;

            viewID = (int)_playerTogglePhotonId.GetValue(toggle);
            return SemiFunc.PlayerAvatarGetFromPhotonID(viewID);
        }

        [HarmonyPrefix]
        public static void Prefix(ItemUpgrade __instance, out UpgradeContext? __state)
        {
            __state = default;

            if (!ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            PlayerAvatar? avatar = GetUpgradePlayer(__instance, out int viewID);
            if (avatar is null) return;

            string steamID = (string)_steamID.GetValue(avatar);
            if (string.IsNullOrEmpty(steamID)) return;

            string? itemName = null;
            if (_itemAttributes.GetValue(__instance) is ItemAttributes attrs && attrs.item != null)
                itemName = attrs.item.name;

            // Track upgrade levels before the purchase goes through
            __state = new UpgradeContext
            (
                steamID: steamID,
                playerName: (string)_playerName.GetValue(avatar),
                viewID: viewID,
                levelsBefore: SnapshotService.SnapshotPlayerStats(steamID),
                itemName: itemName
            );
        }

        [HarmonyPostfix]
        public static void Postfix(UpgradeContext? __state)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (__state is null) return;

            bool distributed = false;
            var playerUpgrades = StatsManager.instance.dictionaryOfDictionaries.Where(key => RegistryService.Instance.IsRegistered(key.Key));

            foreach (KeyValuePair<string, Dictionary<string, int>> kvp in playerUpgrades)
            {
                kvp.Value.TryGetValue(__state.SteamID, out int currentValue);
                __state.LevelsBefore.TryGetValue(kvp.Key, out int previousValue);

                if (currentValue <= previousValue) continue;
                int difference = currentValue - previousValue;

                distributed = true;
                SharedUpgrades__.Logger.LogInfo($"{__state.PlayerName} purchased {kvp.Key} (+{difference}), distributing...");
                DistributionService.DistributeUpgrade
                    (
                        context: __state,
                        upgradeKey: kvp.Key,
                        difference: difference,
                        currentValue: currentValue
                    );
            }

            // Fallback: REPOLib modded upgrades update only the local client's dictionary,
            // so the master's snapshot won't detect changes for non-host purchases.
            // Match the item name against registered modded upgrades to identify the purchase.
            if (!distributed && __state.ItemName != null && ConfigService.IsModdedUpgradesEnabled())
            {
                string? matchedKey = MatchItemNameToModdedUpgrade(__state.ItemName);
                if (matchedKey != null)
                {
                    __state.LevelsBefore.TryGetValue(matchedKey, out int prevLevel);
                    int newLevel = prevLevel + 1;

                    SharedUpgrades__.Logger.LogInfo($"{__state.PlayerName} purchased {matchedKey} (+1), distributing...");
                    DistributionService.DistributeUpgrade
                        (
                            context: __state,
                            upgradeKey: matchedKey,
                            difference: 1,
                            currentValue: newLevel
                        );
                }
            }
        }

        private static string? MatchItemNameToModdedUpgrade(string itemName)
        {
            // Normalize spaces so "ValuableDensity" matches "Valuable Density" in item names
            string normalizedItemName = itemName.Replace(" ", "");
            foreach (Upgrade upgrade in RegistryService.Instance.ModdedUpgrades)
            {
                string normalizedCleanName = upgrade.CleanName.Replace(" ", "");
                if (normalizedItemName.EndsWith(normalizedCleanName, StringComparison.OrdinalIgnoreCase))
                    return upgrade.Name;
            }
            return null;
        }
    }
}
