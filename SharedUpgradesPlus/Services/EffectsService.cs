using Photon.Pun;

namespace SharedUpgradesPlus.Services
{
    public static class EffectsService
    {
        public static void PlayShareEffect(PlayerAvatar player)
        {
            if (player.isLocal)
            {
                StatsUI.instance.Fetch();
                StatsUI.instance.ShowStats();
                CameraGlitch.Instance.PlayUpgrade();
            }
            else
            {
                GameDirector.instance.CameraImpact.ShakeDistance(5f, 1f, 6f, player.transform.position, 0.2f);
            }

            // Host-gated so the material flash only fires once across the room.
            if (!GameManager.Multiplayer() || PhotonNetwork.IsMasterClient)
                player.playerHealth.MaterialEffectOverride(PlayerHealth.Effect.Upgrade);
        }
    }
}
