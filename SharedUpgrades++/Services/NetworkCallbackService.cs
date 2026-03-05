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
        private static readonly FieldInfo _tumble = AccessTools.Field(typeof(PlayerAvatar), "tumble");
        private static readonly FieldInfo _physGrabber = AccessTools.Field(typeof(PlayerAvatar), "physGrabber");
        private static readonly FieldInfo _playerHealth = AccessTools.Field(typeof(PlayerAvatar), "playerHealth");
        private readonly HashSet<Player> _pendingSync = [];

        public override void OnJoinedRoom()
        {
            SharedUpgrades__.LogVerbose($"OnJoinedRoom (isMaster={PhotonNetwork.IsMasterClient})");
            try
            {
                if (!PhotonNetwork.IsMasterClient) return;
                var props = new ExitGames.Client.Photon.Hashtable { { WatermarkService.RoomKey, BuildInfo.Version } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                SharedUpgrades__.LogVerbose($"Set room property: {WatermarkService.RoomKey}={BuildInfo.Version}");
            }
            catch (Exception e)
            {
                SharedUpgrades__.Logger.LogError($"Couldn't set room properties: {e.Message}");
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            SharedUpgrades__.LogVerbose($"OnPlayerEnteredRoom: {newPlayer.NickName} (isMaster={SemiFunc.IsMasterClientOrSingleplayer()}, activeRun={IsActiveRun()}, lateJoin={ConfigService.IsLateJoinSyncEnabled()})");

            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            if (!IsActiveRun())
            {
                _pendingSync.Add(newPlayer);
                SharedUpgrades__.LogInfo($"Deferred sync: {newPlayer.NickName} joined outside of a level, queued. ({_pendingSync.Count} pending)");
                return;
            }

            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels();
            SharedUpgrades__.LogVerbose($"Starting immediate sync for {newPlayer.NickName} ({teamSnapshot.Count} upgrade(s) in snapshot).");
            StartCoroutine(WaitAndSync(newPlayer, teamSnapshot));
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            bool removed = _pendingSync.Remove(otherPlayer);
            SharedUpgrades__.LogVerbose($"OnPlayerLeftRoom: {otherPlayer.NickName} (was pending: {removed}, pending count: {_pendingSync.Count})");
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
            SharedUpgrades__.LogVerbose($"OnSceneLoaded: {scene.name} ({_pendingSync.Count} pending, activeRun={IsActiveRun()})");

            if (_pendingSync.Count == 0) return;
            if (!IsActiveRun()) return;
            if (!ConfigService.IsLateJoinSyncEnabled() || !ConfigService.IsSharedUpgradesEnabled()) return;
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

            var teamSnapshot = SnapshotService.SnapshotTeamMaxLevels();

            SharedUpgrades__.LogInfo($"Deferred sync: {scene.name} loaded, processing {_pendingSync.Count} queued player(s).");

            foreach (var player in _pendingSync)
            {
                SharedUpgrades__.LogVerbose($"Starting deferred sync for {player.NickName}.");
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

            SharedUpgrades__.LogVerbose($"Waiting for {joiningPlayer.NickName} to be ready (max {maxWait}s)...");

            while (elapsed < maxWait)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsed += checkInterval;

                avatar ??= SemiFunc.PlayerGetAll()
                    .FirstOrDefault(p => p.photonView != null && p.photonView.Owner?.ActorNumber == joiningPlayer.ActorNumber);

                if (avatar != null)
                    steamID = (string)_steamID.GetValue(avatar);

                bool tumbleReady = avatar != null && _tumble.GetValue(avatar) != null;
                bool grabberReady = avatar != null && _physGrabber.GetValue(avatar) != null;
                bool healthReady = avatar != null && _playerHealth.GetValue(avatar) != null;
                bool statsReady = !string.IsNullOrEmpty(steamID) && StatsManager.instance.playerUpgradeStrength.ContainsKey(steamID);

                SharedUpgrades__.LogVerbose($"{joiningPlayer.NickName} not ready yet ({elapsed:F1}s) — tumble={tumbleReady}, grabber={grabberReady}, health={healthReady}, stats={statsReady}");

                if (!string.IsNullOrEmpty(steamID) && tumbleReady && grabberReady && healthReady && statsReady)
                    break;
            }

            if (avatar == null || string.IsNullOrEmpty(steamID)
                || _tumble.GetValue(avatar) == null
                || _physGrabber.GetValue(avatar) == null
                || _playerHealth.GetValue(avatar) == null
                || !StatsManager.instance.playerUpgradeStrength.ContainsKey(steamID))
            {
                SharedUpgrades__.Logger.LogWarning($"Late join: timed out waiting for {joiningPlayer.NickName} after {maxWait}s, skipping sync.");
                yield break;
            }

            SharedUpgrades__.LogVerbose($"{joiningPlayer.NickName} ({steamID}) is ready, starting sync.");
            yield return SyncService.ApplyTeamSnapshot(avatar, steamID, teamSnapshot);
        }
    }
}
