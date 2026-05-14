using HarmonyLib;
using Photon.Pun;
using SharedUpgradesPlus.Services;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyPatch(typeof(PunManager), "TesterUpgradeCommandRPC")]
    internal class PlayerUpgradeEffectPatch
    {
        [HarmonyPostfix]
        public static void Postfix(string _steamID, string upgradeName, int upgradeNum, PhotonMessageInfo _info)
        {
            bool isHealth = upgradeName == "Health";
            PlayerAvatar playerAvatar = SemiFunc.PlayerAvatarGetFromSteamID(_steamID);

            if (playerAvatar == null)
            {
                SharedUpgradesPlus.Logger.LogError($"[Effects] TesterUpgradeCommandRPC fired for {_steamID} but no PlayerAvatar found, skipping effects.");
                return;
            }

            SharedUpgradesPlus.LogVerbose($"[Effects] {playerAvatar.playerName} got {upgradeName} x{upgradeNum} (local={playerAvatar.isLocal})");

            if (ConfigService.IsShareNotificationEnabled())
                EffectsService.PlayShareEffect(playerAvatar);

            // Heal the recipient of a shared health upgrade to match their new max health
            if (isHealth
                && SemiFunc.IsMasterClientOrSingleplayer()
                && ConfigService.IsSharedUpgradeHealEnabled())
            {
                int difference = playerAvatar.playerHealth.maxHealth + (20 * upgradeNum) - playerAvatar.playerHealth.health;
                SharedUpgradesPlus.LogVerbose($"[Effects] healing {playerAvatar.playerName}: max={playerAvatar.playerHealth.maxHealth}, current={playerAvatar.playerHealth.health}, healing={difference}");

                if (difference > 0)
                    playerAvatar.playerHealth.HealOther(difference, false);
            }
        }
    }
}
