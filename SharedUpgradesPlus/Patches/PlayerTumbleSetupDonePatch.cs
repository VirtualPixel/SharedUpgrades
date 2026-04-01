using HarmonyLib;
using SharedUpgradesPlus.Services;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(PlayerTumble), "SetupDone")]
    internal class PlayerTumbleSetupDonePatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerTumble __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (string.IsNullOrEmpty(__instance.playerAvatar.steamID) || !NetworkCallbackService.IsPlayerPendingSync(__instance.playerAvatar.photonView.Owner)) return;
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;

            SharedUpgradesPlus.LogAlways($"[LateJoin] SetupDone fired for {__instance.playerAvatar.steamID}, triggering sync.");
            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels(excludeSteamID: __instance.playerAvatar.steamID);

            if (NetworkCallbackService.Instance == null)
            {
                SharedUpgradesPlus.Logger.LogError("NetworkCallbackService instance is null. Cannot sync player stats.");
                return;
            }

            NetworkCallbackService.Instance.StartCoroutine
                (NetworkCallbackService.Instance.LateSyncPlayer(__instance.playerAvatar, __instance.playerAvatar.steamID, teamSnapshot)
            );
        }
    }
}
