using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using System.Reflection;

namespace SharedUpgrades__.Services
{
    public static class DistributionService
    {
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        public static void DistributeUpgrade(UpgradeContext context, string upgradeKey, int difference, int currentValue)
        {
            PhotonView photonView = PunManager.instance.GetComponent<PhotonView>();
            if (photonView == null)
            {
                SharedUpgrades__.Logger.LogWarning("SharedUpgrades: PhotonView not found on PunManager.");
                return;
            }

            bool isVanilla = RegistryService.Instance.IsVanilla(upgradeKey);
            if (!isVanilla && !ConfigService.IsModdedUpgradesEnabled()) return;
            string? upgradeSuffix = isVanilla ? upgradeKey["playerUpgrade".Length..] : null;

            foreach (PlayerAvatar player in SemiFunc.PlayerGetAll())
            {
                if (player == null || player.photonView == null) continue;
                if (player.photonView.ViewID == context.ViewID) continue;

                string steamID = (string)_steamID.GetValue(player);
                if (string.IsNullOrEmpty(steamID)) continue;

                if (!ConfigService.RollSharedUpgradesChance()) continue;

                if (isVanilla)
                {
                    photonView.RPC("TesterUpgradeCommandRPC", RpcTarget.All, steamID, upgradeSuffix, difference);
                    SharedUpgrades__.Logger.LogInfo($"SharedUpgrades: Distributed {upgradeKey} (+{difference}) to {steamID}.");
                }
                else
                {
                    photonView.RPC("UpdateStatRPC", RpcTarget.All, upgradeKey, steamID, currentValue);
                    SharedUpgrades__.Logger.LogInfo($"SharedUpgrades: Distributed modded {upgradeKey} (={currentValue}) to {steamID}.");
                }
            }
        }
    }
}
