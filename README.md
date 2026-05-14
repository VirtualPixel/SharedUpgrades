# SharedUpgradesPlus

Share upgrades with your whole team in R.E.P.O.

**HOST ONLY.** Only the host needs this installed. Your friends play completely vanilla.

> **Heads up:** REPOLib 4.0 moved upgrade sync onto a private NetworkedEvent and the old detection patch stopped firing, so earlier builds silently stopped sharing modded upgrades. Fixed in this release.

---

## Features

- **Chance-based sharing.** Set a % chance that each upgrade level gets shared. 100% for full team sharing, lower if you want it to feel earned.
- **Shared upgrade limiting.** Cap how many times a given upgrade type can be redistributed to the team without limiting how high any one person can take it.
- **Per-upgrade toggle.** Enable or disable sharing individually for each upgrade type in the config file.
- **Late join sync.** Players joining mid-run or between runs (with ReturnToLobby) receive the team's upgrades automatically.
- **Vanilla and modded upgrade support.** Works with MoreUpgrades and other upgrade mods out of the box.
- **Auto-discovery.** Picks up new upgrades automatically, won't break when R.E.P.O. ships new ones.

## Config

**General**

| Setting | Default | Description |
|---|---|---|
| Enable Shared Upgrades | `true` | Enable or disable all upgrade sharing |
| Shared Upgrades Chance | `100` | % chance per upgrade level to be shared with each player |
| Late Join Sync | `true` | Sync upgrades to players who join mid-run |
| Enable Modded Upgrades | `true` | Sync upgrades added by other mods |
| Log Level | `Off`, `Debug`, `Verbose` | Verbosity of logging within the terminal |

**Effects**

| Setting | Default | Description |
|---|---|---|
| Enable Shared Upgrade Heal | `false` | Heal players to full HP when receiving a shared health upgrade |
| Enable Share Notification | `true` | Visual effect when upgrades are shared with you |

**Per-Upgrade** *(one entry per upgrade type, auto-generated)*

| Setting | Default | Description |
|---|---|---|
| [Upgrade Name] | `true` | Enable sharing for this upgrade |
| [Upgrade Name] Share Limit | `0` | Others won't receive this upgrade past this level (0 = unlimited) |

Supports **REPOConfig** for live in-game config changes without restarting.

## Standing on the shoulders of giants

Mods like [SharedUpgrades](https://thunderstore.io/c/repo/p/Traktool/SharedUpgrades/) and [BetterTeamUpgrades](https://thunderstore.io/c/repo/p/MrBytesized/BetterTeamUpgrades/) showed the community what shared upgrades could be. SharedUpgradesPlus builds on that foundation.

## Maintenance

SharedUpgradesPlus is feature complete as far as I'm concerned. I'm not planning new features but I'll keep it maintained, so if you run into a bug or have a feature idea that makes sense, open an issue on GitHub and I'll take a look.

---

## Contact

| Purpose | Where |
|---|---|
| Bug reports & suggestions | [GitHub Issues](https://github.com/VirtualPixel/SharedUpgrades/issues) |
| Community & discussion | [SharedUpgradesPlus Discord](https://discord.gg/9fDzZ9sk95) |
| R.E.P.O. Modding community | [#released-mods post](https://discord.com/channels/1344557689979670578/1474853628056571964) |

<a href="https://ko-fi.com/vippydev" target="_blank">
<img src="https://storage.ko-fi.com/cdn/brandasset/v2/support_me_on_kofi_dark.png" alt="Ko-Fi" width="200px"/>
</a>
