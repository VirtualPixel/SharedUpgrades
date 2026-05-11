using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SharedUpgradesPlus.Configuration;
using SharedUpgradesPlus.Services;
using UnityEngine;

namespace SharedUpgradesPlus
{
    [BepInPlugin("Vippy.SharedUpgradesPlus", "SharedUpgradesPlus", BuildInfo.Version)]
    public class SharedUpgradesPlus : BaseUnityPlugin
    {
        internal static SharedUpgradesPlus Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance.BaseLogger;
        private ManualLogSource BaseLogger => base.Logger;
        internal Harmony? Harmony { get; set; }

        private void Awake()
        {
            Instance = this;

            // Prevent the plugin from being deleted
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;

            // Initialize Config
            PluginConfig.Init(Config);

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded! LogLevel={PluginConfig.LoggingLevel.Value}");
        }

        internal static void LogAlways(string msg) => Logger.LogInfo(msg);

        internal static void LogInfo(string msg)
        {
            if (ConfigService.IsDebugLoggingEnabled())
                Logger.LogInfo(msg);
        }

        internal static void LogVerbose(string msg)
        {
            if (ConfigService.IsVerboseLoggingEnabled())
                Logger.LogDebug(msg);
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }
    }
}