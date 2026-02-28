## 1.2.5

- **New:** Added toggle for enabling/disabling shared upgrade notifications for those running the mod
- **New:** REPOLib is now better supported, it's a soft dependency so it's not required but if users have upgrades using REPOLib it will grab them
- **Changed** Player Upgrade Object Value and Object Durability from MoreUpgrades default to disabled, these are item upgrades and not techinically player upgrades. Could have unintended consequences if enabled
- **Fixed** Upgrade registry could persdist stale data across runs

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

- **New:** Per-upgrade toggle — enable or disable sharing per upgrade type in the config file

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

- **Fixed:** Improved compatibity with latest BepInEx release

---

## 1.0.0

- Initial release
