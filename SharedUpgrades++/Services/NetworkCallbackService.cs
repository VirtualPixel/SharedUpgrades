using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SharedUpgrades__.Services
{
    public class NetworkCallbackService : MonoBehaviourPunCallbacks
    {
        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");
        private readonly HashSet<Player> _pendingSync = new();

        public override void OnJoinedRoom()
        {
            try
            {
                if (!PhotonNetwork.IsMasterClient) return;
                var props = new ExitGames.Client.Photon.Hashtable { { WatermarkService.RoomKey, true } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
            catch { }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (!IsActiveRun())
            {
                _pendingSync.Add(newPlayer);
                SharedUpgrades__.Logger.LogDebug($"Deferred Sync: Queued {newPlayer.NickName} for sync on next active level.");
                return;
            }

            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels();
            StartCoroutine(WaitAndSync(newPlayer, teamSnapshot));
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            _pendingSync.Remove(otherPlayer);
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

        public override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_pendingSync.Count == 0) return;
            if (!IsActiveRun()) return;
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels();

            SharedUpgrades__.Logger.LogInfo($"Deferred Sync: Processing {_pendingSync.Count} queued player(s) on level load.");

            foreach (var player in _pendingSync)
            {
                StartCoroutine(WaitAndSync(player, teamSnapshot));
            }

            _pendingSync.Clear();
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