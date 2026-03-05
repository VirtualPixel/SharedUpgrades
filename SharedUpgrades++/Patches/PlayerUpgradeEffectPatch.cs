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
                SharedUpgrades__.Logger.LogError($"[Effects] TesterUpgradeCommandRPC fired for {_steamID} but no PlayerAvatar found — skipping effects.");
                return;
            }

            SharedUpgrades__.LogVerbose($"[Effects] {playerAvatar.playerName} got {upgradeName} x{upgradeNum} (local={playerAvatar.isLocal})");

            if (ConfigService.IsShareNotificationEnabled())
            {
                if (playerAvatar.isLocal)
                {
                    SharedUpgrades__.LogVerbose($"[Effects] {playerAvatar.playerName} is local — StatsUI + CameraGlitch.");
                    StatsUI.instance.Fetch();
                    StatsUI.instance.ShowStats();
                    CameraGlitch.Instance.PlayUpgrade();
                }
                else
                {
                    SharedUpgrades__.LogVerbose($"[Effects] {playerAvatar.playerName} is remote — camera shake.");
                    GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, playerAvatar.transform.position, 0.2f);
                }

                if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                {
                    SharedUpgrades__.LogVerbose($"[Effects] applying upgrade material effect to {playerAvatar.playerName}.");
                    playerAvatar.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
                }
            }

            SharedUpgrades__.LogVerbose($"[Effects] {playerAvatar.playerName} effects done.");

            // Heal the recipient of a shared health upgrade to match their new max health
            if (isHealth
                && SemiFunc.IsMasterClientOrSingleplayer()
                && ConfigService.IsSharedUpgradeHealEnabled())
            {
                int difference = playerAvatar.playerHealth.maxHealth + (20 * upgradeNum) - playerAvatar.playerHealth.health;
                SharedUpgrades__.LogVerbose($"[Effects] healing {playerAvatar.playerName} — max={playerAvatar.playerHealth.maxHealth}, current={playerAvatar.playerHealth.health}, healing={difference}");

                if (difference > 0)
                    playerAvatar.playerHealth.HealOther(difference, false);
            }
        }
    }
}
