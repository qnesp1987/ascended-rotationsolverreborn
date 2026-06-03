# [Ascended Rotation Solver Reborn](https://github.com/jkleinne/ascended-rotationsolverreborn)

[![](https://raw.githubusercontent.com/jkleinne/ascended-rotationsolverreborn/main/Images/Logo.png)](https://github.com/jkleinne/ascended-rotationsolverreborn)

![Github License](https://img.shields.io/github/license/jkleinne/ascended-rotationsolverreborn.svg?label=License&style=for-the-badge)

Ascended Rotation Solver Reborn is a personal fork of [RotationSolverReborn](https://github.com/FFXIV-CombatReborn/RotationSolverReborn). It adds focused PvP targeting, PvP action policy work, and an Ascended Bard PvE rotation for Patch 7.5.

This is a third party Dalamud plugin. Square Enix, Dalamud, and the upstream RotationSolverReborn project do not provide support for this fork. Use it only if you understand the risks of third party tools in Final Fantasy XIV.

## Quick Install

Add the Ascended plugin repository to Dalamud:

```text
https://raw.githubusercontent.com/jkleinne/ascended-plugins/main/pluginmaster.json
```

1. Open `/xlsettings` in chat.
2. Go to the Experimental tab.
3. Find Custom Plugin Repositories.
4. Paste the URL above into an empty entry.
5. Click `+`, enable the new entry, and save.
6. Open Dalamud's plugin installer and install `Ascended Rotation Solver Reborn`.

Do not run upstream RSR at the same time. This fork has its own internal plugin identity, but it still registers `/rotation` and `/rsr`, so the two plugins can conflict over chat commands.

## Highlights

### PvPSmart Targeting

* Scores PvP targets instead of cycling by raw current HP.
* Accounts for role value, effective HP, mitigation, range, target stickiness, isolation, threat pressure, and limit break casts.
* Skips invulnerable or effectively invulnerable targets instead of spending pressure into them.

### PvP Action Discipline

* Conserves burst when the selected target is protected or low value.
* Adds focused Bard and Machinist ranged job policy for better kill conversion and fewer wasted high impact actions.
* Includes a PvPSmart debug overlay for tuning and live review.

### BRD Ascended PvE

* Adds `BRD Ascended`, a Patch 7.5 Bard PvE rotation option implemented as `BRD_Ascended`.
* Intended as a general Bard PvE rotation with standard, advanced, and custom timing options.
* Uses the Bard PvE spec work for song timing, DoT thresholds, burst alignment, potion timing, resource cap safety, level sync fallback, and resolved target AoE behavior.

### Separate Ascended Package

* Installs from the `ascended-plugins` repository.
* Uses a distinct internal plugin name from upstream RSR.
* Tracks upstream selectively while keeping fork specific behavior here.

## Quick Setup

For PvP:

1. Open RSR settings.
2. Go to `Target` > `Hostile`.
3. Put `PvPSmart` first in the PvP hostile targeting list.
4. Choose a PvP scoring preset. `Ranked` is the intended default.
5. Leave burst conservation enabled unless you are deliberately testing raw damage behavior.
6. Use the PvPSmart debug overlay when tuning target scores or reviewing live target choices.

For Bard PvE:

1. Select `BRD Ascended` from the Bard PvE rotations.
2. Use standard timing for general content.
3. Use advanced timing or custom settings only when you are planning around a specific encounter timeline.

## Feature Notes

### PvPSmart

PvPSmart is designed for Crystalline Conflict style target selection. It favors vulnerable high value enemies, avoids protected targets, and tries to reduce target swapping when candidates are close in value.

Crystal carrier and objective weighting are structurally present, but the carrier status signal is not verified in this fork yet. Until that signal is populated, carrier specific scoring should be treated as inactive.

### PvP Burst Conservation

Burst conservation is intended to trade some raw scoreboard damage for better kill windows. It can hold high impact actions when the target has Guard, invulnerability, heavy mitigation, or poor kill value. Some actions may still spend before a charge cap or ready timer would be wasted.

### BRD Ascended

`BRD_Ascended` is built as a complete Bard PvE package rather than a raid only opener script. It supports raid burst alignment, capped resource fallback, dungeon and alliance raid AoE based on resolved action targets, target time to kill aware DoTs, and level sync fallbacks.

No automated rotation can be universally optimal for every party comp, kill time, downtime pattern, or future patch. Recheck behavior after FFXIV updates and tune planned fight settings when a fight timeline matters.

### Inherited RSR Behavior

This fork keeps the broader RotationSolverReborn foundation: rotation selection, action guidance, configuration UI, IPC surface, and inherited job support. If you only want upstream behavior without the Ascended changes, install upstream RotationSolverReborn instead.

## Known Limits

* Upstream RSR and this fork should not run together because both register `/rotation` and `/rsr`.
* Crystal carrier scoring is inactive until the carrier status signal is verified.
* PvP decisions need live match validation after FFXIV patches.
* PvE rotations need review after Bard balance, action, or potion changes.
* Local development builds require Dalamud development assemblies, usually via `DALAMUD_HOME` or the XIVLauncher dev path.
* Third party plugins are prohibited by the FFXIV terms and support policy. Use at your own risk.

## Links

* Ascended plugin repository: https://github.com/jkleinne/ascended-plugins
* This fork: https://github.com/jkleinne/ascended-rotationsolverreborn
* Upstream RotationSolverReborn: https://github.com/FFXIV-CombatReborn/RotationSolverReborn
* Upstream Discord: https://discord.gg/p54TZMPnC9
