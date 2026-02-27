using HarmonyLib;
using Photon.Pun;
using SharedUpgrades__.Services;

namespace SharedUpgrades__.Patches
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
                SharedUpgrades__.Logger.LogError($"TesterUpgradeCommandRPC(): PlayerAvatar not found for steamID: {_steamID}");
                return;
            }

            if (playerAvatar.isLocal)
            {
                StatsUI.instance.Fetch();
                StatsUI.instance.ShowStats();
                CameraGlitch.Instance.PlayUpgrade();
            }
            else
            {
                GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, playerAvatar.transform.position, 0.2f);
            }

            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
            {
                playerAvatar.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
            }

            // Heal the recipient of a shared health upgrade to match their new max health
            if (isHealth
                && SemiFunc.IsMasterClientOrSingleplayer()
                && ConfigService.IsSharedUpgradeHealEnabled())
            {
                int difference = playerAvatar.playerHealth.maxHealth + (20 * upgradeNum) - playerAvatar.playerHealth.health;

                if (difference > 0)
                {
                    playerAvatar.playerHealth.HealOther(difference, false);
                }
            }
        }
    }
}