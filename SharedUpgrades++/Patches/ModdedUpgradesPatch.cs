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

            SharedUpgrades__.LogVerbose($"[ModdedPatch] {dictionaryName} ({key}) — value={value}, player={player?.playerName ?? "not found"}, distributing={DistributionService.IsDistributing}");

            // Visual effects (all clients)
            if (player != null && ConfigService.IsShareNotificationEnabled())
            {
                SharedUpgrades__.LogVerbose($"[ModdedPatch] running effects for {player.playerName}");

                if (player.isLocal)
                {
                    SharedUpgrades__.LogVerbose($"[ModdedPatch] local player, triggering StatsUI + CameraGlitch.");
                    StatsUI.instance.Fetch();
                    StatsUI.instance.ShowStats();
                    CameraGlitch.Instance.PlayUpgrade();
                }
                else
                {
                    SharedUpgrades__.LogVerbose($"[ModdedPatch] remote player, shaking camera.");
                    GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, player.transform.position, 0.2f);
                }

                if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                {
                    SharedUpgrades__.LogVerbose($"[ModdedPatch] applying upgrade material effect to {player.playerName}.");
                    player.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
                }
            }

            // Distribution (master only, no re-entry)
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (DistributionService.IsDistributing)
            {
                SharedUpgrades__.LogVerbose($"[ModdedPatch] already distributing, skipping {dictionaryName}.");
                return;
            }

            if (player == null || player.photonView == null)
            {
                SharedUpgrades__.Logger.LogWarning($"[ModdedPatch] no PlayerAvatar found for {key}, can't distribute {dictionaryName}.");
                return;
            }

            string playerName = (string)_playerName.GetValue(player);
            SharedUpgrades__.LogAlways($"[ModdedPatch] {playerName} bought {dictionaryName}, distributing...");

            var context = new UpgradeContext(
                steamID: key,
                viewID: player.photonView.ViewID,
                playerName: playerName,
                levelsBefore: new Dictionary<string, int>()
            );

            // TODO: Update this to support dynamic difference, not hard coded
            DistributionService.DistributeUpgrade(
                context: context,
                upgradeKey: dictionaryName,
                difference: 1,
                currentValue: value
            );
        }
    }
}
