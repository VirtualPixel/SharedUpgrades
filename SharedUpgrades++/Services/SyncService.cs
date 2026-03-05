using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                SharedUpgrades__.Logger.LogWarning("[LateJoin] PhotonView not found on PunManager, skipping sync.");
                yield break;
            }

            string playerName = (string)_playerName.GetValue(player);

            int chance = ConfigService.SharedUpgradesChancePercentage();
            SharedUpgrades__.LogInfo($"[LateJoin] syncing {playerName} — {teamSnapshot.Count} upgrade(s), chance={chance}%");

            int sent = 0;
            int skipped = 0;

            foreach (var kvp in teamSnapshot)
            {
                int upgradeLimit = ConfigService.UpgradeShareLimit(kvp.Key);
                bool isVanilla = RegistryService.Instance.IsVanilla(kvp.Key);

                SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — teamMax={kvp.Value}, isVanilla={isVanilla}, limit={upgradeLimit}");

                // If modded upgrade and modded upgrades are disabled, skip it
                if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled())
                {
                    SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — skipped (modded upgrades disabled).");
                    skipped++;
                    continue;
                }
                if (!ConfigService.IsUpgradeEnabled(kvp.Key))
                {
                    SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — skipped (disabled in config).");
                    skipped++;
                    continue;
                }

                int playerLevel = StatsManager.instance.dictionaryOfDictionaries
                    .TryGetValue(kvp.Key, out var upgradeDict)
                    ? upgradeDict.GetValueOrDefault(steamID, 0)
                    : 0;

                if (upgradeLimit > 0 && upgradeLimit <= playerLevel)
                {
                    SharedUpgrades__.LogInfo($"[LateJoin]   {kvp.Key} — {playerName} hit share limit ({upgradeLimit}), skipping.");
                    skipped++;
                    continue;
                }

                int value = kvp.Value;
                int difference = value - playerLevel;

                // Cap difference based on share limit
                if (upgradeLimit > 0)
                    difference = Math.Min(difference, upgradeLimit - playerLevel);

                SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — level={playerLevel}, teamMax={kvp.Value}, diff={difference} (pre-roll)");

                difference = SimulateRealisticLevelling(difference);
                value = playerLevel + difference;

                if (difference <= 0)
                {
                    SharedUpgrades__.LogInfo($"[LateJoin]   {kvp.Key} — rolled 0 after chance simulation, skipping.");
                    skipped++;
                    continue;
                }

                if (isVanilla)
                {
                    SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — sending TesterUpgradeCommandRPC to {playerName}");
                    photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, new Upgrade(kvp.Key).CleanName, difference);
                }
                else
                {
                    SharedUpgrades__.LogVerbose($"[LateJoin]   {kvp.Key} — sending UpdateStatRPC to {playerName}");
                    photonView.RPC("UpdateStatRPC", RpcTarget.All, kvp.Key, steamID, value);
                }

                SharedUpgrades__.LogVerbose($"[LateJoin]   sent {kvp.Key} (+{difference}) to {playerName}.");
                sent++;
            }

            SharedUpgrades__.LogInfo($"[LateJoin] done — {playerName}: sent={sent}, skipped={skipped}");
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

            SharedUpgrades__.LogVerbose($"[LateJoin] roll simulation — input={value}, chance={chance}%, result={simulatedValue}");
            return simulatedValue;
        }
    }
}
