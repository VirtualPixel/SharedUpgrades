using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SharedUpgradesPlus.Configuration;
using SharedUpgradesPlus.Patches;
using SharedUpgradesPlus.Services;
using System;
using System.Reflection;
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
            PatchRepoLib();
        }

        // REPOLib's PlayerUpgrade lives in another assembly we don't reference at
        // compile time, so we resolve the targets reflectively and register with
        // Harmony's manual API. If REPOLib isn't installed everything below
        // resolves to null and we just skip; the mod still works for vanilla.
        private void PatchRepoLib()
        {
            MethodInfo? setLevel = RepoLibInterop.SetLevelMethod;
            MethodInfo? applyUpgrade = RepoLibInterop.ApplyUpgradeMethod;
            if (setLevel == null || applyUpgrade == null)
            {
                Logger.LogInfo("REPOLib not detected; skipping modded upgrade patches.");
                return;
            }

            Harmony!.Patch(setLevel,
                prefix: HarmonyMethodFor(nameof(PlayerUpgradePatches.SetLevelPrefix)),
                postfix: HarmonyMethodFor(nameof(PlayerUpgradePatches.SetLevelPostfix)));

            Harmony!.Patch(applyUpgrade,
                prefix: HarmonyMethodFor(nameof(PlayerUpgradePatches.ApplyUpgradePrefix)),
                postfix: HarmonyMethodFor(nameof(PlayerUpgradePatches.ApplyUpgradePostfix)));

            Logger.LogInfo("Patched REPOLib PlayerUpgrade.SetLevel and ApplyUpgrade.");
        }

        private static HarmonyMethod HarmonyMethodFor(string name)
        {
            MethodInfo method = typeof(PlayerUpgradePatches).GetMethod(
                name, BindingFlags.Static | BindingFlags.Public)
                ?? throw new MissingMethodException(typeof(PlayerUpgradePatches).FullName, name);
            return new HarmonyMethod(method);
        }
    }
}