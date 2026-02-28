using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System;
using System.Reflection;

namespace SharedUpgrades__.Services
{
    public static class DistributionService
    {
        public static bool IsDistributing { get; private set; }
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        public static void DistributeUpgrade(UpgradeContext context, string upgradeKey, int difference, int currentValue)
        {
            int upgradeLimit = ConfigService.UpgradeShareLimit(upgradeKey);
            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgrades__.Logger.LogWarning("PhotonView not found on PunManager.");
                return;
            }

            bool isVanilla = RegistryService.Instance.IsVanilla(upgradeKey);
            if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled()) return;
            if (!ConfigService.IsUpgradeEnabled(upgradeKey))
            {
                SharedUpgrades__.Logger.LogInfo($"{upgradeKey} is disabled in config, skipping distribution.");
                return;
            }
            string? upgradeSuffix = isVanilla ? new Upgrade(upgradeKey).CleanName : null;

            var allPlayers = SemiFunc.PlayerGetAll();

            IsDistributing = true;

            try { 
                foreach (PlayerAvatar player in allPlayers)
                {
                    if (player == null || player.photonView == null) continue;
                    if (player.photonView.ViewID == context.ViewID) continue;
                
                    string steamID = (string)_steamID.GetValue(player);
                    if (string.IsNullOrEmpty(steamID)) continue;

                    int playerLevel = 0;
                    if (StatsManager.instance.dictionaryOfDictionaries.TryGetValue(upgradeKey, out var upgradeDict))
                    {
                        upgradeDict.TryGetValue(steamID, out playerLevel);
                    }

                    if (upgradeLimit > 0 
                        && upgradeLimit <= playerLevel)
                    {
                        SharedUpgrades__.Logger.LogInfo($"{upgradeKey} has reached the share limit set of: {upgradeLimit}");
                        continue;
                    }

                    if (!ConfigService.RollSharedUpgradesChance())
                    {
                        SharedUpgrades__.Logger.LogInfo($"Skipping {player.playerName} due to failed roll chance.");
                        continue;
                    }

                    if (isVanilla)
                    {
                        photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, upgradeSuffix, difference);
                        SharedUpgrades__.Logger.LogInfo($"Distributed {upgradeKey} (+{difference}) to {steamID}.");
                    }
                    else
                    {
                        photonView.RPC("UpdateStatRPC", RpcTarget.All, upgradeKey, steamID, currentValue);

                        SharedUpgrades__.Logger.LogInfo($"Distributed modded {upgradeKey} (={currentValue}) to {steamID}.");
                    }
                }
            }
            catch (Exception e)
            {
                SharedUpgrades__.Logger.LogError($"Failed to distribute {upgradeKey} for player: {context.PlayerName}. Exception: {e.Message}");
            }
            finally
            {
                IsDistributing = false;
            }

            // Only buyer should get the heal, not everyone getting distributed to
            HealBuyer(context, upgradeKey, difference);
        }
    
        // Heal the buyer of a health upgrade to match their new max health
        private static void HealBuyer(UpgradeContext context, string upgradeKey, int difference)
        {
            if (upgradeKey == "playerUpgradeHealth"
                && ConfigService.IsSharedUpgradeHealEnabled())
            {
                PlayerAvatar buyer = SemiFunc.PlayerAvatarGetFromSteamID(context.SteamID);

                if (buyer != null)
                {
                    int healDiff = buyer.playerHealth.maxHealth + (20 * difference) - buyer.playerHealth.health;

                    if (healDiff > 0)
                    {
                        buyer.playerHealth.HealOther(healDiff, false);
                    }
                }
            }
        }
    }
}
