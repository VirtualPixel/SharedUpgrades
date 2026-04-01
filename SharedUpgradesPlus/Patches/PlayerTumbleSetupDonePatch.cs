using HarmonyLib;
using SharedUpgradesPlus.Services;
using WebSocketSharp;

namespace SharedUpgradesPlus.Patches
{
    [HarmonyLib.HarmonyPatch(typeof(PlayerTumble), "SetupDone")]
    internal class PlayerTumbleSetupDonePatch
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerTumble __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (__instance.playerAvatar.steamID.IsNullOrEmpty() || !NetworkCallbackService.IsPlayerPendingSync(__instance.playerAvatar.photonView.Owner)) return;
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
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
