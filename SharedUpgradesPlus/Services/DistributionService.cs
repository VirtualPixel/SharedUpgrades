using Photon.Pun;
using SharedUpgradesPlus.Models;
using System;

namespace SharedUpgradesPlus.Services
{
    public static class DistributionService
    {
        public static bool IsDistributing { get; private set; }

        public static void DistributeUpgrade(UpgradeContext context, string upgradeKey, int difference)
        {
            SharedUpgradesPlus.LogVerbose($"[Distribute] {context.PlayerName} bought {upgradeKey} (+{difference})");

            int upgradeLimit = ConfigService.UpgradeShareLimit(upgradeKey);
            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgradesPlus.Logger.LogWarning("[Distribute] PhotonView not found on PunManager, can't distribute.");
                return;
            }

            bool isVanilla = RegistryService.Instance.IsVanilla(upgradeKey);
            if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled())
            {
                SharedUpgradesPlus.LogInfo($"[Distribute] {upgradeKey} is modded and modded upgrades are off, skipping.");
                return;
            }
            if (!ConfigService.IsUpgradeEnabled(upgradeKey))
            {
                SharedUpgradesPlus.LogInfo($"[Distribute] {upgradeKey} is disabled in config, skipping.");
                return;
            }

            string? upgradeSuffix = isVanilla ? new Upgrade(upgradeKey).CleanName : null;
            var allPlayers = SemiFunc.PlayerGetAll();
            int chance = ConfigService.SharedUpgradesChancePercentage();

            SharedUpgradesPlus.LogVerbose($"[Distribute] {upgradeKey} (+{difference}) — {allPlayers.Count} player(s), limit={upgradeLimit}, chance={chance}%");

            IsDistributing = true;
            int sent = 0;
            int skipped = 0;

            try
            {
                foreach (PlayerAvatar player in allPlayers)
                {
                    if (player == null || player.photonView == null) continue;
                    if (player.photonView.ViewID == context.ViewID) continue;

                    string steamID = player.steamID;
                    if (string.IsNullOrEmpty(steamID)) continue;

                    int playerLevel = 0;
                    if (StatsManager.instance.dictionaryOfDictionaries.TryGetValue(upgradeKey, out var upgradeDict))
                        upgradeDict.TryGetValue(steamID, out playerLevel);

                    SharedUpgradesPlus.LogVerbose($"[Distribute]   {player.playerName} — level={playerLevel}, limit={upgradeLimit}");

                    if (upgradeLimit > 0 && upgradeLimit <= playerLevel)
                    {
                        SharedUpgradesPlus.LogInfo($"[Distribute]   {player.playerName} hit share limit ({upgradeLimit}), skipping.");
                        skipped++;
                        continue;
                    }

                    if (!ConfigService.RollSharedUpgradesChance())
                    {
                        SharedUpgradesPlus.LogInfo($"[Distribute]   {player.playerName} roll failed ({chance}%), skipping.");
                        skipped++;
                        continue;
                    }

                    int newLevel = playerLevel + difference;
                    SharedUpgradesPlus.LogAlways($"[Distribute]   {player.playerName}: {playerLevel} → {newLevel} (+{difference})");

                    if (isVanilla)
                    {
                        photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, upgradeSuffix, difference);
                    }
                    else
                    {
                        // Send the teammate's new total, not the host's — otherwise teammates snap to host's level.
                        photonView.RPC("UpdateStatRPC", RpcTarget.All, upgradeKey, steamID, newLevel);
                    }

                    sent++;
                }
            }
            catch (Exception e)
            {
                SharedUpgradesPlus.Logger.LogError($"[Distribute] exception distributing {upgradeKey} for {context.PlayerName}: {e.Message}");
            }
            finally
            {
                IsDistributing = false;
            }

            SharedUpgradesPlus.LogVerbose($"[Distribute] done — {upgradeKey}: sent={sent}, skipped={skipped}");

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
            SharedUpgradesPlus.LogVerbose($"[Distribute] healing {context.PlayerName} — max={buyer.playerHealth.maxHealth}, current={buyer.playerHealth.health}, healing={healDiff}");

            if (healDiff > 0)
                buyer.playerHealth.HealOther(healDiff, false);
        }
    }
}
