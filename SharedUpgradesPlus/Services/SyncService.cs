using Photon.Pun;
using SharedUpgradesPlus.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharedUpgradesPlus.Services
{
    public static class SyncService
    {
        public static IEnumerator ApplyTeamSnapshot(PlayerAvatar player, string steamID, Dictionary<string, int> teamSnapshot)
        {
            if (StatsManager.instance == null || PunManager.instance == null) yield break;

            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgradesPlus.Logger.LogWarning("[LateJoin] PhotonView not found on PunManager, skipping sync.");
                yield break;
            }

            string playerName = player.playerName;

            int chance = ConfigService.SharedUpgradesChancePercentage();
            SharedUpgradesPlus.LogAlways($"[LateJoin] syncing {playerName}: {teamSnapshot.Count} upgrade(s), chance={chance}%");

            int sent = 0;
            int skipped = 0;

            // Late-join writes echo back into the modded upgrade detection patch
            // (PlayerUpgrade.ApplyUpgrade fires on the host when SetLevel runs).
            // Without this guard the joining player going from 0 -> teamMax would
            // be treated as a fresh purchase and re-distributed to everyone.
            DistributionService.EnterDistributing();
            try
            {
                foreach (var kvp in teamSnapshot)
                {
                    int upgradeLimit = ConfigService.UpgradeShareLimit(kvp.Key);
                    bool isVanilla = RegistryService.Instance.IsVanilla(kvp.Key);

                    SharedUpgradesPlus.LogVerbose($"[LateJoin]   {kvp.Key}: teamMax={kvp.Value}, isVanilla={isVanilla}, limit={upgradeLimit}");

                    // If modded upgrade and modded upgrades are disabled, skip it
                    if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled())
                    {
                        SharedUpgradesPlus.LogVerbose($"[LateJoin]   {kvp.Key}: skipped (modded upgrades disabled).");
                        skipped++;
                        continue;
                    }
                    if (!ConfigService.IsUpgradeEnabled(kvp.Key))
                    {
                        SharedUpgradesPlus.LogVerbose($"[LateJoin]   {kvp.Key}: skipped (disabled in config).");
                        skipped++;
                        continue;
                    }

                    int playerLevel = StatsManager.instance.dictionaryOfDictionaries
                        .TryGetValue(kvp.Key, out var upgradeDict)
                        ? upgradeDict.GetValueOrDefault(steamID, 0)
                        : 0;

                    if (upgradeLimit > 0 && upgradeLimit <= playerLevel)
                    {
                        SharedUpgradesPlus.LogInfo($"[LateJoin]   {kvp.Key}: {playerName} hit share limit ({upgradeLimit}), skipping.");
                        skipped++;
                        continue;
                    }

                    int value = kvp.Value;
                    int difference = value - playerLevel;

                    // Cap difference based on share limit
                    if (upgradeLimit > 0)
                        difference = Math.Min(difference, upgradeLimit - playerLevel);

                    SharedUpgradesPlus.LogVerbose($"[LateJoin]   {kvp.Key}: level={playerLevel}, teamMax={kvp.Value}, diff={difference} (pre-roll)");

                    difference = SimulateRealisticLevelling(difference);
                    value = playerLevel + difference;

                    if (difference <= 0)
                    {
                        SharedUpgradesPlus.LogInfo($"[LateJoin]   {kvp.Key}: rolled 0 after chance simulation, skipping.");
                        skipped++;
                        continue;
                    }

                    if (isVanilla)
                    {
                        SharedUpgradesPlus.LogVerbose($"[LateJoin]   {kvp.Key}: sending TesterUpgradeCommandRPC to {playerName}");
                        photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, new Upgrade(kvp.Key).CleanName, difference);
                    }
                    else if (!RepoLibInterop.TrySetLevel(kvp.Key, steamID, value))
                    {
                        SharedUpgradesPlus.Logger.LogWarning($"[LateJoin]   {kvp.Key}: SetLevel failed for {playerName}, falling back to UpdateStatRPC.");
                        photonView.RPC("UpdateStatRPC", RpcTarget.All, kvp.Key, steamID, value);
                    }

                    SharedUpgradesPlus.LogVerbose($"[LateJoin]   sent {kvp.Key} (+{difference}) to {playerName}.");
                    sent++;
                }
            }
            finally
            {
                DistributionService.ExitDistributing();
            }

            SharedUpgradesPlus.LogAlways($"[LateJoin] done {playerName}: sent={sent}, skipped={skipped}");
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

            SharedUpgradesPlus.LogVerbose($"[LateJoin] roll simulation: input={value}, chance={chance}%, result={simulatedValue}");
            return simulatedValue;
        }
    }
}
