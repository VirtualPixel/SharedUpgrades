using HarmonyLib;
using SharedUpgrades__.Models;
using SharedUpgrades__.Services;
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

            // Track upgrade levels before the purchase goes through
            __state = new UpgradeContext
            (
                steamID: steamID,
                playerName: (string)_playerName.GetValue(avatar),
                viewID: viewID,
                levelsBefore: SnapshotService.SnapshotPlayerStats(steamID)
            );
        }

        [HarmonyPostfix]
        public static void Postfix(UpgradeContext? __state)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (__state is null) return;

            var playerUpgrades = StatsManager.instance.dictionaryOfDictionaries.Where(key => key.Key.StartsWith("playerUpgrade"));

            foreach (KeyValuePair<string, Dictionary<string, int>> kvp in playerUpgrades)
            {
                kvp.Value.TryGetValue(__state.SteamID, out int currentValue);
                __state.LevelsBefore.TryGetValue(kvp.Key, out int previousValue);

                if (currentValue <= previousValue) continue;

                SharedUpgrades__.Logger.LogInfo($"SharedUpgrades: {__state.PlayerName} purchased {kvp.Key} (+{currentValue - previousValue}), distributing...");

                DistributionService.DistributeUpgrade
                    (
                        context: __state,
                        upgradeKey: kvp.Key,
                        difference: currentValue - previousValue,
                        currentValue: currentValue
                    );
            }
        }
    }
}
