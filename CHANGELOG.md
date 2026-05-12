## 1.4.2

- **Fixed:** Quieter startup. `RepoLibInterop` was using `HarmonyLib.AccessTools.Property` to look up REPOLib's `PlayerUpgrade.UpgradeId` and the lookup failing (which the surrounding code already handles) was making HarmonyX log a `Could not find property` warning on every cold start. Switched to vanilla `Type.GetProperty` with explicit `BindingFlags` so the lookup is silent.
- **Changed:** README and CHANGELOG cleaned up. No code behavior change.

---

## 1.4.1

- **Fixed:** Modded upgrades weren't being detected on R.E.P.O. v0.4. The mod thought there were zero modded upgrades to share, so things like MoreUpgrades' Sprint Usage or Map Zoom silently never distributed. Discovery now picks them up correctly, and also reads REPOLib's API directly as a backup in case the key prefix changes again the way MoreUpgrades 1.6.7 did.

---

## 1.4.0

- **Fixed:** Modded upgrade sharing was snapping teammates to the host's full upgrade level instead of giving them the same +1 the host just bought. A friend with no magnet upgrades would jump straight to wherever the host happened to be sitting. Teammates now gain the same amount the host did, the way vanilla sharing has always worked.

---

## 1.3.3

- **Updated:** Rebuilt for R.E.P.O. v0.4. 1.3.2 will not load on v0.4. The stats container changed type internally (`Dictionary` to `SortedDictionary`), which is a binary break even though the mod's source did not need to change
- **Note:** v0.4 adds an in-world UpgradeStand kiosk and an auto-strip rule for stat keys prefixed `playerUpgrade*`. If upgrades disappear or stand purchases don't sync, open an issue with your config and BepInEx log

---

## 1.3.2

- **Fixed:** Updated Late Join Sync functionality, it no longer has an arbitrary 12 second timeout. This was replaced with a patch that runs after each player is ready.
- **Changed:** Replaced Reflection usage around the code base with calls using the publicizer

---

## 1.3.1

- **Fixed:** Late join sync could give the joining player more upgrades than teammates had. Caused by the joining player's own save data inflating the team snapshot and a race condition with the game's stat initialization
- **Added:** Extensive logging, along with a toggle to increase or decrease logging

---

## 1.3.0

- **Changed:** Late join sync now simulates per-level chance rather than a single all-or-nothing roll. Late joiners receive a realistic spread instead of full upgrades or none
- **Changed:** `LateJoinSyncChance` removed; `SharedUpgradesChance` now applies to both real-time sharing and late join sync
- **Changed:** Config sections consolidated into General and Effects for cleaner layout
- **Changed:** Updated README to be more consistent with naming - SharedUpgradesPlus
- **New:** Watermark displays the host's running mod version during debug testing
- **Fixed:** LateJoinSync rolls lower than 100% now adhere to share limiting

---

## 1.2.5

- **New:** Added toggle for enabling/disabling shared upgrade notifications for those running the mod
- **New:** REPOLib is now better supported, it's a soft dependency so it's not required but if users have upgrades using REPOLib it will grab them
- **Changed:** Player Upgrade Object Value and Object Durability from MoreUpgrades default to disabled, these are item upgrades and not technically player upgrades. Could have unintended consequences if enabled
- **Fixed:** Upgrade registry could persist stale data across runs

---

## 1.2.4

- **Fixed:** Modded upgrades weren't being shared when a non-host player bought them
- **Fixed:** Visual effects didn't play when receiving a shared modded upgrade

---

## 1.2.3

- **New:** Added toggle to heal the buyer when purchasing a health upgrade (off by default)

---

## 1.2.2

- **Fixed:** Health logic is more accurate now, no longer using stale values 

---

## 1.2.1

- **Fixed:** Late-join strength race condition problem when playerUpgradeStrength doesn't contain the player's steamID

---

## 1.2.0

- **New:** Share limiting! Limit how many times each upgrade can be shared with others. Default is infinite
- **Fixed:** Late-join modded upgrade sync

---

## 1.1.4

- **Fixed:** Non-host player not healed when taking a health upgrade

---

## 1.1.3

- **New:** Upgrading health now heals other players
- **New:** Visual effect for each player, just like the player who used the upgrade

---

## 1.1.2

- **Hotfix:** Late join sync with some upgrades not always working if not initialized yet

---

## 1.1.1

- **Fixed:** Late Join Sync when combined with return to lobby mods

---

## 1.1.0

- **New:** Per-upgrade toggle, enable or disable sharing per upgrade type in the config file

---

## 1.0.3

- **Fixed:** Late join sync could apply upgrades at the wrong time
- **Fixed:** Watermark display font

---

## 1.0.2

- **Fixed:** Late join sync now snapshots team state the moment a player joins, making catch-up more reliable
- **Fixed:** RNG rolls were being wasted when upgrade sharing conditions weren't met

---

## 1.0.1

- **Fixed:** Improved compatibility with latest BepInEx release

---

## 1.0.0

- Initial release
