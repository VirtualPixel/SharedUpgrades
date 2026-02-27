using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Models;
using SharedUpgrades__.Services;
using System.Collections.Generic;
using System.Reflection;

namespace SharedUpgrades__.Patches
{
    [HarmonyPatch(typeof(PunManager), nameof(PunManager.UpdateStatRPC))]
    internal class ModdedUpgradesPatch
    {
        private static readonly FieldInfo _playerName = AccessTools.Field(typeof(PlayerAvatar), "playerName");

        [HarmonyPostfix]
        public static void Postfix(string dictionaryName, string key, int value)
        {
            if (!ConfigService.IsSharedUpgradesEnabled()) return;
            if (!ConfigService.IsModdedUpgradesEnabled()) return;
            if (!RegistryService.Instance.IsRegistered(dictionaryName)) return;
            if (RegistryService.Instance.IsVanilla(dictionaryName)) return;
            if (!ConfigService.IsUpgradeEnabled(dictionaryName)) return;

            PlayerAvatar player = SemiFunc.PlayerAvatarGetFromSteamID(key);

            // Visual effects (all clients) 
            if (player != null)
            {
                if (player.isLocal)
                {
                    StatsUI.instance.Fetch();
                    StatsUI.instance.ShowStats();
                    CameraGlitch.Instance.PlayUpgrade();
                }
                else
                {
                    GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, player.transform.position, 0.2f);
                }

                if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                {
                    player.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
                }
            }

            // Distribution (master only, no re-entry)
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (DistributionService.IsDistributing) return;

            if (player == null || player.photonView == null) return;

            string playerName = (string)_playerName.GetValue(player);
            SharedUpgrades__.Logger.LogInfo($"{playerName} purchased {dictionaryName} (+1), distributing...");

            var context = new UpgradeContext(
                steamID: key,
                viewID: player.photonView.ViewID,
                playerName: playerName,
                levelsBefore: new Dictionary<string, int>()
            );

            DistributionService.DistributeUpgrade(
                context: context,
                upgradeKey: dictionaryName,
                difference: 1,
                currentValue: value
            );
        }
    }
}
