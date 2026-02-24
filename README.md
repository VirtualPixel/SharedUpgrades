# SharedUpgrades++

Share upgrades with your whole team in R.E.P.O.

**HOST ONLY** — only the host needs this installed. Your friends play completely vanilla.

---

## Features

- **Chance-based sharing** — set a % chance that each upgrade gets shared. 100% for full team sharing, lower if you want it to feel earned
- **Shared Upgrade Limiting** — Set a total number of times individual upgrades can be distributed while not limiting how high one person can take a stat 
- **Per-upgrade toggle** — enable or disable sharing individually for each upgrade type directly in the config file
- **Late join sync** — players joining mid-run or between runs (with ReturnToLobby) receive the team's upgrades automatically
- **Vanilla + modded upgrade support** — works with MoreUpgrades and other upgrade mods out of the box
- **Auto-discovery** — detects new upgrades automatically, won't break when R.E.P.O. updates

## Config

| Setting | Default | Description |
|---|---|---|
| Shared Upgrades | `true` | Share upgrades when any player purchases one |
| Share Chance | `100` | % chance each upgrade is shared with teammates |
| Late Join Sync | `true` | Catch up players who join mid-run |
| Late Join Chance | `100` | % chance each upgrade a late joiner receives |
| Modded Upgrades | `true` | Include upgrades from other mods |

Supports **REPOConfig** for live in-game config changes without restarting.

## Standing on the shoulders of giants

Mods like [SharedUpgrades](https://thunderstore.io/c/repo/p/Traktool/SharedUpgrades/) and [BetterTeamUpgrades](https://thunderstore.io/c/repo/p/MrBytesized/BetterTeamUpgrades/) showed the community what shared upgrades could be. SharedUpgrades++ builds on that foundation.

## Roadmap

- **Per-player sharing** — choose which players in your lobby receive shared upgrades

---

**Bugs or suggestions?** [Open an issue on GitHub](https://github.com/VirtualPixel/SharedUpgrades/issues)
