using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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

                int difference = kvp.Value - playerLevel;
                if (difference <= 0) continue;

                if (!ConfigService.RollLateJoinSyncChance())
                {
                    SharedUpgrades__.Logger.LogInfo($"Late Join: Roll failed for {kvp.Key} ({steamID}), skipping.");
                    continue;
                }

                if (isVanilla)
                    photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, new Upgrade(kvp.Key).CleanName, difference);
                else
                    photonView.RPC("UpdateStatRPC", RpcTarget.All, kvp.Key, steamID, kvp.Value);
            }

            SharedUpgrades__.Logger.LogInfo($"Late Join: Sync complete for {playerName}.");
        }
    }
}
