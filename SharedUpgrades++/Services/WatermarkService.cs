using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SharedUpgrades__.Services
{
    internal class WatermarkService : MonoBehaviour
    {
        internal const string RoomKey = "su__v1";
        private static readonly string? OwnerID = LoadOwnerID();

        private static string? LoadOwnerID()
        {
            try
            {
                var path = Path.Combine(Paths.ConfigPath, "SharedUpgrades++.owner");
                if (!File.Exists(path)) return null;
                return File.ReadAllText(path).Trim();
            }
            catch { return null; }
        }

        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        private bool show;
        private bool polling;
        private UnityEngine.Object? lastLevel;
        private string? lastRoom;
        private GUIStyle? style;

        private void Update()
        {
            if (RunManager.instance == null) return;
            var current = RunManager.instance.levelCurrent;
            var currentRoom = PhotonNetwork.CurrentRoom?.Name;

            if (current == lastLevel && currentRoom == lastRoom) return;
            lastLevel = current;
            lastRoom = currentRoom;

            if (currentRoom == null) show = false;
            if (current == RunManager.instance.levelLobbyMenu)
            {
                show = false;
                if (!polling) StartCoroutine(Poll());
            }
        }

        private IEnumerator Poll()
        {
            polling = true;
            const float timeout = 10f;
            const float interval = 1f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                yield return new WaitForSeconds(interval);
                elapsed += interval;

                try
                {
                    if (!PhotonNetwork.InRoom) continue;
                    if (string.IsNullOrEmpty(OwnerID)) break;

                    PlayerAvatar? localPlayer = null;
                    foreach (var p in SemiFunc.PlayerGetAll())
                    {
                        if (p?.photonView != null && p.photonView.IsMine)
                        {
                            localPlayer = p;
                            break;
                        }
                    }

                    if (localPlayer == null) continue;
                    if ((string)_steamID.GetValue(localPlayer) != OwnerID) break;

                    bool isHost = PhotonNetwork.IsMasterClient;
                    bool modPresent = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomKey);

                    if (isHost || modPresent)
                    {
                        show = !isHost && modPresent;
                        SharedUpgrades__.Logger.LogInfo($"[Watermark] Showing. isHost={isHost}, modPresent={modPresent}");
                        break;
                    } else
                    {
                        SharedUpgrades__.Logger.LogInfo($"[Watermark] Not showing. isHost={isHost}, modPresent={modPresent}");
                    }
                }
                catch { }
            }

            polling = false;
        }

        private void OnGUI()
        {
            if (!show) return;
            try
            {
                style ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.15f) }
                };
                GUI.Label(new Rect(6, Screen.height - 72, 160, 56), "S++", style);
            }
            catch { }
        }
    }
}
