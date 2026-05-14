using SharedUpgradesPlus.Models;
using SharedUpgradesPlus.Services;
using System;
using System.Collections.Generic;

namespace SharedUpgradesPlus.Patches
{
    // REPOLib 4.0 moved upgrade sync onto its own NetworkedEvent. Both the buyer
    // (via SetLevel) and the receivers (via HandleUpgradeEvent) call ApplyUpgrade,
    // so that's where we hook. SetLevel writes the dict before ApplyUpgrade fires
    // though, so the buyer's prior level has to be stashed via SetLevel's prefix.
    // Registered manually from SharedUpgradesPlus.Patch() since REPOLib is a soft dep.
    internal static class PlayerUpgradePatches
    {
        [ThreadStatic] private static int? _stashedPrior;

        public static void SetLevelPrefix(object __instance, string steamId)
        {
            _stashedPrior = RepoLibInterop.ReadCurrentLevel(__instance, steamId);
        }

        public static void SetLevelPostfix()
        {
            // Defensive: if SetLevel threw before reaching ApplyUpgrade the stash
            // would otherwise leak into the next unrelated ApplyUpgrade call.
            _stashedPrior = null;
        }

        public static void ApplyUpgradePrefix(object __instance, string steamId, out int __state)
        {
            if (_stashedPrior.HasValue)
            {
                __state = _stashedPrior.Value;
                _stashedPrior = null;
            }
            else
            {
                __state = RepoLibInterop.ReadCurrentLevel(__instance, steamId);
            }
        }

        public static void ApplyUpgradePostfix(object __instance, string steamId, int level, int __state)
        {
            if (!ConfigService.IsSharedUpgradesEnabled()) return;
            if (!ConfigService.IsModdedUpgradesEnabled()) return;
            if (!RepoLibInterop.TryReadUpgradeKey(__instance, out string upgradeKey)) return;
            if (!RegistryService.Instance.IsRegistered(upgradeKey)) return;
            if (RegistryService.Instance.IsVanilla(upgradeKey)) return;
            if (!ConfigService.IsUpgradeEnabled(upgradeKey)) return;

            int difference = level - __state;
            PlayerAvatar player = SemiFunc.PlayerAvatarGetFromSteamID(steamId);

            SharedUpgradesPlus.LogVerbose($"[Modded] {upgradeKey} ({steamId}): {__state} -> {level} (+{difference}), player={player?.playerName ?? "not found"}, distributing={DistributionService.IsDistributing}");

            // Visual effect fires on every client that sees the change, including
            // the recipients of a host-driven distribution. Matches what
            // PlayerUpgradeEffectPatch does for vanilla upgrades.
            if (player != null && difference > 0 && ConfigService.IsShareNotificationEnabled())
                EffectsService.PlayShareEffect(player);

            // Distribute on the host, only on a real increase, never re-entrantly.
            if (difference <= 0) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (DistributionService.IsDistributing) return;

            if (player == null || player.photonView == null)
            {
                SharedUpgradesPlus.Logger.LogWarning($"[Modded] no PlayerAvatar for {steamId}, can't distribute {upgradeKey}.");
                return;
            }

            SharedUpgradesPlus.LogAlways($"[Modded] {player.playerName} bought {upgradeKey} (+{difference}), distributing...");

            var context = new UpgradeContext(
                steamID: steamId,
                viewID: player.photonView.ViewID,
                playerName: player.playerName,
                levelsBefore: new Dictionary<string, int>()
            );

            DistributionService.DistributeUpgrade(
                context: context,
                upgradeKey: upgradeKey,
                difference: difference
            );
        }
    }
}
