using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SharedUpgrades__.Configuration;
using SharedUpgrades__.Services;
using UnityEngine;

namespace SharedUpgrades__
{
    [BepInPlugin("Vippy.SharedUpgradesPlus", "SharedUpgradesPlus", BuildInfo.Version)]
    public class SharedUpgrades__ : BaseUnityPlugin
    {
        internal static SharedUpgrades__ Instance { get; private set; } = null!;
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

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }
    }
}