using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SharedUpgradesPlus.Services
{
    // All access to REPOLib goes through here. REPOLib is a soft dependency:
    // resolution failures are silent and the rest of the mod degrades to vanilla
    // behavior. Vanilla BindingFlags lookups (rather than AccessTools.Field /
    // .Property / .Method) keep HarmonyX from logging "Could not find member"
    // warnings on cold start when REPOLib's shape moves between releases.
    internal static class RepoLibInterop
    {
        private const string KeyPrefix = "playerUpgrade";

        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.Static;

        private static bool _resolved;
        private static Type? _playerUpgradeType;
        private static FieldInfo? _upgradeIdField;
        private static FieldInfo? _playerDictionaryField;
        private static MethodInfo? _applyUpgradeMethod;
        private static MethodInfo? _setLevelMethod;
        private static FieldInfo? _playerUpgradesDictField;
        private static PropertyInfo? _playerUpgradesProp;

        public static MethodInfo? ApplyUpgradeMethod
        {
            get { Resolve(); return _applyUpgradeMethod; }
        }

        public static MethodInfo? SetLevelMethod
        {
            get { Resolve(); return _setLevelMethod; }
        }

        public static HashSet<string> GetModdedUpgradeKeys()
        {
            var keys = new HashSet<string>();
            Resolve();
            if (_playerUpgradesProp == null || _upgradeIdField == null) return keys;

            try
            {
                if (_playerUpgradesProp.GetValue(null) is not IEnumerable list) return keys;
                foreach (var upgrade in list)
                {
                    if (upgrade == null) continue;
                    if (_upgradeIdField.GetValue(upgrade) is string id && !string.IsNullOrEmpty(id))
                        keys.Add(KeyPrefix + id);
                }
            }
            catch (Exception e)
            {
                SharedUpgradesPlus.LogVerbose($"[RepoLibInterop] reading PlayerUpgrades failed: {e.Message}");
            }

            return keys;
        }

        public static bool TryReadUpgradeKey(object playerUpgrade, out string upgradeKey)
        {
            upgradeKey = string.Empty;
            Resolve();
            if (_upgradeIdField == null) return false;

            if (_upgradeIdField.GetValue(playerUpgrade) is not string id || string.IsNullOrEmpty(id))
                return false;

            upgradeKey = KeyPrefix + id;
            return true;
        }

        public static int ReadCurrentLevel(object playerUpgrade, string steamId)
        {
            Resolve();
            if (_playerDictionaryField == null) return 0;
            if (_playerDictionaryField.GetValue(playerUpgrade) is not IDictionary dict) return 0;
            if (!dict.Contains(steamId)) return 0;
            return dict[steamId] is int level ? level : 0;
        }

        // Drives a level change through REPOLib's own pipeline: writes the dict
        // on the caller, fires the upgrade action locally, broadcasts to other
        // clients via the REPOLib Upgrade NetworkedEvent. UpdateStatRPC alone
        // moves the number but never invokes the action.
        public static bool TrySetLevel(string upgradeKey, string steamId, int level)
        {
            Resolve();
            if (_setLevelMethod == null || _playerUpgradesDictField == null) return false;
            if (!upgradeKey.StartsWith(KeyPrefix)) return false;

            string upgradeId = upgradeKey[KeyPrefix.Length..];

            if (_playerUpgradesDictField.GetValue(null) is not IDictionary dict) return false;
            if (!dict.Contains(upgradeId)) return false;

            object? playerUpgrade = dict[upgradeId];
            if (playerUpgrade == null) return false;

            try
            {
                _setLevelMethod.Invoke(playerUpgrade, new object[] { steamId, level });
                return true;
            }
            catch (Exception e)
            {
                // Reflection.Invoke wraps the real exception in TargetInvocationException.
                Exception inner = (e as TargetInvocationException)?.InnerException ?? e;
                SharedUpgradesPlus.Logger.LogWarning($"[RepoLibInterop] SetLevel({upgradeId}, {steamId}, {level}) threw {inner.GetType().Name}: {inner.Message}");
                return false;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            var upgradesType = AccessTools.TypeByName("REPOLib.Modules.Upgrades");
            if (upgradesType == null) return;

            _playerUpgradeType = AccessTools.TypeByName("REPOLib.Modules.PlayerUpgrade");
            if (_playerUpgradeType == null) return;

            _playerUpgradesProp = upgradesType.GetProperty("PlayerUpgrades", MemberFlags);
            _playerUpgradesDictField = upgradesType.GetField("_playerUpgrades", MemberFlags);

            _upgradeIdField = _playerUpgradeType.GetField("UpgradeId", MemberFlags);
            _playerDictionaryField = _playerUpgradeType.GetField("PlayerDictionary", MemberFlags);

            _applyUpgradeMethod = _playerUpgradeType.GetMethod(
                "ApplyUpgrade",
                MemberFlags,
                binder: null,
                types: new[] { typeof(string), typeof(int) },
                modifiers: null);

            _setLevelMethod = _playerUpgradeType.GetMethod(
                "SetLevel",
                MemberFlags,
                binder: null,
                types: new[] { typeof(string), typeof(int) },
                modifiers: null);
        }
    }
}
