using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SharedUpgrades__.Services
{
    public class NetworkCallbackService : MonoBehaviourPunCallbacks
    {
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        public override void OnJoinedRoom()
        {
            try
            {
                if (!SemiFunc.IsMasterClient()) return;
                var props = new ExitGames.Client.Photon.Hashtable { { WatermarkService.RoomKey, true } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
            catch { }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (!IsActiveRun()) return;

            // Grab team upgrades before the new player's data shows up
            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels();
            StartCoroutine(WaitAndSync(newPlayer, teamSnapshot));
        }

        private static bool IsActiveRun()
        {
            if (RunManager.instance == null) return false;
            var level = RunManager.instance.levelCurrent;
            return level != RunManager.instance.levelMainMenu
                && level != RunManager.instance.levelLobbyMenu
                && level != RunManager.instance.levelRecording
                && level != RunManager.instance.levelSplashScreen;
        }

        private IEnumerator WaitAndSync(Player joiningPlayer, Dictionary<string, int> teamSnapshot)
        {
            const float maxWait = 12f;
            const float checkInterval = 0.3f;
            float elapsed = 0f;

            PlayerAvatar? avatar = null;
            string steamID = string.Empty;

            while (elapsed < maxWait)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsed += checkInterval;

                avatar ??= SemiFunc.PlayerGetAll()
                    .FirstOrDefault(p => p.photonView != null && p.photonView.Owner?.ActorNumber == joiningPlayer.ActorNumber);

                if (avatar != null)
                    steamID = (string)_steamID.GetValue(avatar);

                if (!string.IsNullOrEmpty(steamID))
                    break;
            }

            if (avatar == null || string.IsNullOrEmpty(steamID))
            {
                SharedUpgrades__.Logger.LogWarning($"Late Join: Timed out waiting for {joiningPlayer.NickName}. Skipping.");
                yield break;
            }

            yield return SyncService.ApplyTeamSnapshot(avatar, steamID, teamSnapshot);
        }
    }
}