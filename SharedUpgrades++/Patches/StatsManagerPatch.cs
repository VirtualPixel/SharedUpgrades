using HarmonyLib;
using SharedUpgrades__.Services;
using UnityEngine;

namespace SharedUpgrades__.Patches
{
    [HarmonyPatch(typeof(StatsManager), "Start")]
    public class StatsManagerPatch
    {
        private static NetworkCallbackService? _callbackService;

        [HarmonyPostfix]
        public static void Postfix(StatsManager __instance)
        {
            var discovered = DiscoveryService.DiscoveredUpgrades(__instance);
            RegistryService.Instance.Clear();
            RegistryService.Instance.RegisterAll(discovered);

            if (_callbackService == null)
            {
                var go = new GameObject("NetworkCallbackService");
                _callbackService = go.AddComponent<NetworkCallbackService>();
                go.AddComponent<WatermarkService>();
                Object.DontDestroyOnLoad(go);
            }
        }
    }
}
