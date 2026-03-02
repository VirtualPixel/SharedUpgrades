using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SharedUpgrades__.Services
{
    public static class SyncService
    {
        private static readonly FieldInfo _playerName = AccessTools.Field(typeof(PlayerAvatar), "playerName");

        public static IEnumerator ApplyTeamSnapshot(PlayerAvatar player, string steamID, Dictionary<string, int> teamSnapshot)
        {
            if (StatsManager.instance == null || PunManager.instance == null) yield break;

            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgrades__.Logger.LogWarning("Late Join: PhotonView not found on PunManager.");
                yield break;
            }

            string playerName = (string)_playerName.GetValue(player);
            SharedUpgrades__.Logger.LogInfo($"Late Join: Starting sync for {playerName} ({steamID}).");

            foreach (var kvp in teamSnapshot)
            {
                int upgradeLimit = ConfigService.UpgradeShareLimit(kvp.Key);
                bool isVanilla = RegistryService.Instance.IsVanilla(kvp.Key);

                // If modded upgrade and modded upgrades are disabled, skip it
                if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled()) continue;
                if (!ConfigService.IsUpgradeEnabled(kvp.Key)) continue;

                int playerLevel = StatsManager.instance.dictionaryOfDictionaries
                    .TryGetValue(kvp.Key, out var upgradeDict)
                    ? upgradeDict.GetValueOrDefault(steamID, 0)
                    : 0;

                if (upgradeLimit > 0
                    && upgradeLimit <= playerLevel)
                {
                    SharedUpgrades__.Logger.LogInfo($"{kvp.Key} has reached the share limit set of: {upgradeLimit}");
                    continue;
                }

                int value = kvp.Value;
                int difference = value - playerLevel;

                // Cap difference based on Share limit, should not exceed this
                if (upgradeLimit > 0)
                    difference = Math.Min(difference, upgradeLimit - playerLevel);

                difference = SimulateRealisticLevelling(difference);
                value = playerLevel + difference;
                if (difference <= 0) continue;

                if (isVanilla)
                    photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, new Upgrade(kvp.Key).CleanName, difference);
                else
                    photonView.RPC("UpdateStatRPC", RpcTarget.All, kvp.Key, steamID, value);
            }

            SharedUpgrades__.Logger.LogInfo($"Late Join: Sync complete for {playerName}.");
        }

        private static int SimulateRealisticLevelling(int value)
        {
            int chance = ConfigService.SharedUpgradesChancePercentage();
            if (chance >= 100 || value <= 0) return value;
            int simulatedValue = 0;

            for (int i = 0; i < value; i++)
            {
                if (ConfigService.RollSharedUpgradesChance())
                    simulatedValue++;
            }

            return simulatedValue;
        }
    }
}
