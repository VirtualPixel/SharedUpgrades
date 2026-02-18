using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SharedUpgrades__.Services
{
    public class WatermarkService : MonoBehaviour
    {
        internal const string RoomKey = "su__v1";
        private const string OwnerID = "";

        private static readonly FieldInfo _steamID = AccessTools.Field(typeof(PlayerAvatar), "steamID");

        private bool show = false;
        private GUIStyle? style;

        private void Start()
        {
            StartCoroutine(Poll());
        }

        private IEnumerator Poll()
        {
            while (true)
            {
                try
                {
                    bool isOwner = false;
                    foreach (var p in SemiFunc.PlayerGetAll())
                    {
                        if (p?.photonView != null && p.photonView.IsMine)
                        {
                            isOwner = (string)_steamID.GetValue(p) == OwnerID;
                            break;
                        }
                    }

                    bool modPresent = PhotonNetwork.InRoom &&
                        PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(RoomKey);

                    show = isOwner && modPresent;
                }
                catch { }

                yield return new WaitForSeconds(3f);
            }
        }

        private void OnGUI()
        {
            if (!show) return;
            try
            {
                style ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 9,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.15f) }
                };
                GUI.Label(new Rect(6, Screen.height - 18, 40, 14), "S++", style);
            }
            catch { }
        }
    }
}
