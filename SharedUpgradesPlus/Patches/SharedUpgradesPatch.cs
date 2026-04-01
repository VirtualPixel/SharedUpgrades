using HarmonyLib;
using SharedUpgradesPlus.Models;
using SharedUpgradesPlus.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyPatch(typeof(ItemUpgrade), "PlayerUpgrade")]
    internal class SharedUpgradesPatch
    {
        private static PlayerAvatar? GetUpgradePlayer(ItemUpgrade instance, out int viewID)
        {
            viewID = 0;

            if (instance.itemToggle is not ItemToggle { toggleState: true })
                return null;

            viewID = instance.itemToggle.playerTogglePhotonID;
            return SemiFunc.PlayerAvatarGetFromPhotonID(viewID);
        }

        [HarmonyPrefix]
        public static void Prefix(ItemUpgrade __instance, out UpgradeContext? __state)
        {
            __state = default;

            if (!ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            PlayerAvatar? avatar = GetUpgradePlayer(__instance, out int viewID);
            if (avatar is null)
            {
                SharedUpgradesPlus.LogVerbose("[Purchase] upgrade interaction fired but couldn't find a player, skipping.");
                return;
            }

            string steamID = avatar.steamID;
            if (string.IsNullOrEmpty(steamID)) return;

            string? itemName = null;
            if (__instance.itemAttributes is ItemAttributes attrs && attrs.item != null)
                itemName = attrs.item.name;

            SharedUpgradesPlus.LogVerbose($"[Purchase] {avatar.playerName} is buying '{itemName}'");

            // Track upgrade levels before the purchase goes through
            __state = new UpgradeContext
            (
                steamID: steamID,
                playerName: avatar.playerName,
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

            SharedUpgradesPlus.LogVerbose($"[Purchase] checking what {__state.PlayerName} just bought (item='{__state.ItemName}')");

            bool distributed = false;
            var playerUpgrades = StatsManager.instance.dictionaryOfDictionaries.Where(key => RegistryService.Instance.IsRegistered(key.Key));

            foreach (KeyValuePair<string, Dictionary<string, int>> kvp in playerUpgrades)
            {
                kvp.Value.TryGetValue(__state.SteamID, out int currentValue);
                __state.LevelsBefore.TryGetValue(kvp.Key, out int previousValue);

                SharedUpgradesPlus.LogVerbose($"[Purchase]   {kvp.Key}: {previousValue} → {currentValue}");

                if (currentValue <= previousValue) continue;
                int difference = currentValue - previousValue;

                distributed = true;
                SharedUpgradesPlus.LogAlways($"[Purchase] {__state.PlayerName} bought {kvp.Key} (+{difference}), distributing...");
                DistributionService.DistributeUpgrade(
                    context: __state,
                    upgradeKey: kvp.Key,
                    difference: difference,
                    currentValue: currentValue
                );
            }

            SharedUpgradesPlus.LogVerbose($"[Purchase] vanilla scan done, distributed={distributed}");

            // Match the item name against registered modded upgrades to identify the purchase
            if (!distributed && __state.ItemName != null && ConfigService.IsModdedUpgradesEnabled())
            {
                SharedUpgradesPlus.LogVerbose($"[Purchase] no vanilla upgrades changed — checking modded match for '{__state.ItemName}'");

                string? matchedKey = MatchItemNameToModdedUpgrade(__state.ItemName);
                if (matchedKey != null)
                {
                    __state.LevelsBefore.TryGetValue(matchedKey, out int prevLevel);
                    int newLevel = prevLevel + 1;

                    SharedUpgradesPlus.LogInfo($"[Purchase] {__state.PlayerName} ({__state.SteamID}) bought modded {matchedKey} (+1), distributing...");
                    DistributionService.DistributeUpgrade(
                        context: __state,
                        upgradeKey: matchedKey,
                        difference: 1,
                        currentValue: newLevel
                    );
                }
                else
                {
                    SharedUpgradesPlus.LogVerbose($"[Purchase] no match for '{__state.ItemName}', nothing to distribute.");
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
