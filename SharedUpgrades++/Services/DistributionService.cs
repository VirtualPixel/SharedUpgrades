using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SharedUpgrades__.Services
{
    public static class DistributionService
    {
        public static bool IsDistributing { get; private set; }
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        public static void DistributeUpgrade(UpgradeContext context, string upgradeKey, int difference, int currentValue)
        {
            SharedUpgrades__.LogVerbose($"[Distribute] {context.PlayerName} bought {upgradeKey} (+{difference})");

            int upgradeLimit = ConfigService.UpgradeShareLimit(upgradeKey);
            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgrades__.Logger.LogWarning("[Distribute] PhotonView not found on PunManager, can't distribute.");
                return;
            }

            bool isVanilla = RegistryService.Instance.IsVanilla(upgradeKey);
            if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled())
            {
                SharedUpgrades__.LogInfo($"[Distribute] {upgradeKey} is modded and modded upgrades are off, skipping.");
                return;
            }
            if (!ConfigService.IsUpgradeEnabled(upgradeKey))
            {
                SharedUpgrades__.LogInfo($"[Distribute] {upgradeKey} is disabled in config, skipping.");
                return;
            }

            string? upgradeSuffix = isVanilla ? new Upgrade(upgradeKey).CleanName : null;
            var allPlayers = SemiFunc.PlayerGetAll();
            int chance = ConfigService.SharedUpgradesChancePercentage();

            SharedUpgrades__.LogVerbose($"[Distribute] {upgradeKey} (+{difference}) — {allPlayers.Count} player(s), limit={upgradeLimit}, chance={chance}%");

            IsDistributing = true;
            int sent = 0;
            int skipped = 0;

            try
            {
                foreach (PlayerAvatar player in allPlayers)
                {
                    if (player == null || player.photonView == null) continue;
                    if (player.photonView.ViewID == context.ViewID) continue;

                    string steamID = (string)_steamID.GetValue(player);
                    if (string.IsNullOrEmpty(steamID)) continue;

                    int playerLevel = 0;
                    if (StatsManager.instance.dictionaryOfDictionaries.TryGetValue(upgradeKey, out var upgradeDict))
                        upgradeDict.TryGetValue(steamID, out playerLevel);

                    SharedUpgrades__.LogVerbose($"[Distribute]   {player.playerName} — level={playerLevel}, limit={upgradeLimit}");

                    if (upgradeLimit > 0 && upgradeLimit <= playerLevel)
                    {
                        SharedUpgrades__.LogInfo($"[Distribute]   {player.playerName} hit share limit ({upgradeLimit}), skipping.");
                        skipped++;
                        continue;
                    }

                    if (!ConfigService.RollSharedUpgradesChance())
                    {
                        SharedUpgrades__.LogInfo($"[Distribute]   {player.playerName} roll failed ({chance}%), skipping.");
                        skipped++;
                        continue;
                    }

                    if (isVanilla)
                    {
                        SharedUpgrades__.LogVerbose($"[Distribute]   sending TesterUpgradeCommandRPC to {player.playerName}");
                        photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, upgradeSuffix, difference);
                    }
                    else
                    {
                        SharedUpgrades__.LogVerbose($"[Distribute]   sending UpdateStatRPC to {player.playerName}");
                        photonView.RPC("UpdateStatRPC", RpcTarget.All, upgradeKey, steamID, currentValue);
                    }

                    SharedUpgrades__.LogInfo($"[Distribute]   sent {upgradeKey} (+{difference}) to {player.playerName}.");
                    sent++;
                }
            }
            catch (Exception e)
            {
                SharedUpgrades__.Logger.LogError($"[Distribute] exception distributing {upgradeKey} for {context.PlayerName}: {e.Message}");
            }
            finally
            {
                IsDistributing = false;
            }

            SharedUpgrades__.LogVerbose($"[Distribute] done — {upgradeKey}: sent={sent}, skipped={skipped}");

            // Only buyer should get the heal, not everyone getting distributed to
            HealBuyer(context, upgradeKey, difference);
        }

        // Heal the buyer of a health upgrade to match their new max health
        private static void HealBuyer(UpgradeContext context, string upgradeKey, int difference)
        {
            if (upgradeKey != "playerUpgradeHealth" || !ConfigService.IsSharedUpgradeHealEnabled()) return;

            PlayerAvatar buyer = SemiFunc.PlayerAvatarGetFromSteamID(context.SteamID);
            if (buyer == null) return;

            int healDiff = buyer.playerHealth.maxHealth + (20 * difference) - buyer.playerHealth.health;
            SharedUpgrades__.LogVerbose($"[Distribute] healing {context.PlayerName} — max={buyer.playerHealth.maxHealth}, current={buyer.playerHealth.health}, healing={healDiff}");

            if (healDiff > 0)
                buyer.playerHealth.HealOther(healDiff, false);
        }
    }
}
