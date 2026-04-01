using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace SharedUpgradesPlus.Services
{
    public class NetworkCallbackService : MonoBehaviourPunCallbacks
    {
        public static NetworkCallbackService? Instance { get; private set; }
        private static readonly HashSet<Player> _pendingSync = [];

        public override void OnJoinedRoom()
        {
            SharedUpgradesPlus.LogVerbose($"OnJoinedRoom (isMaster={PhotonNetwork.IsMasterClient})");
            try
            {
                if (!PhotonNetwork.IsMasterClient) return;
                var props = new ExitGames.Client.Photon.Hashtable { { WatermarkService.RoomKey, BuildInfo.Version } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                SharedUpgradesPlus.LogVerbose($"Set room property: {WatermarkService.RoomKey}={BuildInfo.Version}");
            }
            catch (Exception e)
            {
                SharedUpgradesPlus.Logger.LogError($"Couldn't set room properties: {e.Message}");
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            SharedUpgradesPlus.LogVerbose($"OnPlayerEnteredRoom: {newPlayer.NickName} (isMaster={SemiFunc.IsMasterClientOrSingleplayer()}, activeRun={IsActiveRun()}, lateJoin={ConfigService.IsLateJoinSyncEnabled()})");

            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            _pendingSync.Add(newPlayer);
            SharedUpgradesPlus.LogAlways($"Deferred sync: {newPlayer.NickName} joined, queued. ({_pendingSync.Count} pending)");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            bool removed = _pendingSync.Remove(otherPlayer);
            SharedUpgradesPlus.LogVerbose($"OnPlayerLeftRoom: {otherPlayer.NickName} (was pending: {removed}, pending count: {_pendingSync.Count})");
        }

        public static bool IsPlayerPendingSync(Player player)
        {
            return _pendingSync.Contains(player);
        }

        public IEnumerator LateSyncPlayer(PlayerAvatar avatar, string steamID, Dictionary<string, int> teamSnapshot)
        {
            yield return SyncService.ApplyTeamSnapshot(avatar, steamID, teamSnapshot);
            _pendingSync.Remove(avatar.photonView.Owner);
        }

        private void Awake()
        {
            Instance = this;
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
    }
}
