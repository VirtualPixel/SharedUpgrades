using HarmonyLib;
using Photon.Pun;
using SharedUpgradesPlus.Models;
using SharedUpgradesPlus.Services;
using System.Collections.Generic;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyPatch(typeof(PunManager), nameof(PunManager.UpdateStatRPC))]
    internal class ModdedUpgradesPatch
    {
        // Capture the pre-update value so the postfix can compute the real delta.
        // Modded upgrades may not always increment by 1.
        [HarmonyPrefix]
        public static void Prefix(string dictionaryName, string key, out int __state)
        {
            __state = 0;
            if (StatsManager.instance == null) return;
            if (StatsManager.instance.dictionaryOfDictionaries.TryGetValue(dictionaryName, out var dict))
                dict.TryGetValue(key, out __state);
        }

        [HarmonyPostfix]
        public static void Postfix(string dictionaryName, string key, int value, int __state)
        {
            if (!ConfigService.IsSharedUpgradesEnabled()) return;
            if (!ConfigService.IsModdedUpgradesEnabled()) return;
            if (!RegistryService.Instance.IsRegistered(dictionaryName)) return;
            if (RegistryService.Instance.IsVanilla(dictionaryName)) return;
            if (!ConfigService.IsUpgradeEnabled(dictionaryName)) return;

            PlayerAvatar player = SemiFunc.PlayerAvatarGetFromSteamID(key);
            int difference = value - __state;

            SharedUpgradesPlus.LogVerbose($"[ModdedPatch] {dictionaryName} ({key}) — {__state} → {value} (+{difference}), player={player?.playerName ?? "not found"}, distributing={DistributionService.IsDistributing}");

            // Visual effects (all clients)
            if (player != null && ConfigService.IsShareNotificationEnabled())
            {
                SharedUpgradesPlus.LogVerbose($"[ModdedPatch] running effects for {player.playerName}");

                if (player.isLocal)
                {
                    SharedUpgradesPlus.LogVerbose($"[ModdedPatch] local player, triggering StatsUI + CameraGlitch.");
                    StatsUI.instance.Fetch();
                    StatsUI.instance.ShowStats();
                    CameraGlitch.Instance.PlayUpgrade();
                }
                else
                {
                    SharedUpgradesPlus.LogVerbose($"[ModdedPatch] remote player, shaking camera.");
                    GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, player.transform.position, 0.2f);
                }

                if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                {
                    SharedUpgradesPlus.LogVerbose($"[ModdedPatch] applying upgrade material effect to {player.playerName}.");
                    player.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
                }
            }

            // Distribution (master only, no re-entry, only on actual increase)
            if (difference <= 0) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (DistributionService.IsDistributing)
            {
                SharedUpgradesPlus.LogVerbose($"[ModdedPatch] already distributing, skipping {dictionaryName}.");
                return;
            }

            if (player == null || player.photonView == null)
            {
                SharedUpgradesPlus.Logger.LogWarning($"[ModdedPatch] no PlayerAvatar found for {key}, can't distribute {dictionaryName}.");
                return;
            }

            string playerName = player.playerName;
            SharedUpgradesPlus.LogAlways($"[ModdedPatch] {playerName} bought {dictionaryName}: {__state} → {value} (+{difference}), distributing...");

            var context = new UpgradeContext(
                steamID: key,
                viewID: player.photonView.ViewID,
                playerName: playerName,
                levelsBefore: new Dictionary<string, int>()
            );

            DistributionService.DistributeUpgrade(
                context: context,
                upgradeKey: dictionaryName,
                difference: difference
            );
        }
    }
}
