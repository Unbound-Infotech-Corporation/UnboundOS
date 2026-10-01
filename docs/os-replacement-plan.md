# UnboundOS as the Windows shell (replacement plan)

C L direction: UnboundOS is the **primary UI** on the daily-driver PC.
Genuine Windows stays underneath (NT, drivers, DirectX, Store, Steam,
anticheat binaries). UnboundOS becomes **the session UI** — not an app
that sits on an Explorer desktop.

**Lead path is Windows 11 Xbox mode / Full Screen Experience (FSE).**
HKCU `Shell=` is a **fallback**. Default install is still a normal app.

Locked:

- Super Clean / Diavlo / teal (`#00F0FF`) on focus only
- Motion kill-switch
- No Rainmeter in the product path
- No Sony assets
- No redistributed Microsoft `.msu` / `.cab`
- No custom bootloader
- Update Guard stays: quality from Microsoft, Defender on
- Genuine Windows only — **no debloat ISO, no IoT LTSC**
- No game inject, no D3D/Vulkan hooks, no drivers

## Anticheat (read this first)

Xbox mode / FSE and HKCU `Shell=` both **leave** NT, Win32, DWM,
drivers, and the game’s own processes alone. They **do not** rename
`explorer.exe`, disable WU/Defender/VBS, or install a bootloader.

Some anticheat stacks **look for Explorer / the NT desktop**. That is
**unverified** until C L runs the matrix in [test-checklist.md](test-checklist.md)
section H (Valorant/Vanguard, EAC, BattlEye, EA Javelin, FACEIT).

If a title fails: **Desktop mode**, or turn Xbox mode / Shell= off.

## Stage 0 — now (this PR)

Works on Home/Pro. Explorer stays the Windows shell.

| Piece | Design |
|-------|--------|
| Auto sign-in | Windows only (`netplwiz` / Settings → Accounts). Documented. No custom credential provider. |
| Startup | Existing **HKCU Run** launches `UnboundOS.App.exe --fullscreen` |
| Settings | Own Options pages (system catalog, Xbox toggle, power, Desktop mode) |
| Files | Own gamepad file picker + on-screen QWERTY |

## Stage 1 — main bet (this PR)

Register UnboundOS as the **Xbox mode home app**. Microsoft rolled FSE
out to PCs (2026-04-30; KB5070297). FSE defers taskbar/Start/desktop
and suppresses startup apps. Kernel / drivers / anticheat stay Windows.

Microsoft does **not** document third-party home-app registration.
Community apps (OmniConsole, AnyFSE, Playnite FSE,
FullScreenExperienceShell) all use the same **packaged** pattern. Exact
XML: [xbox-mode-home.md](xbox-mode-home.md) and `packaging/fse/`.

| Piece | Design |
|-------|--------|
| Package | MSIX identity `UnboundInfotech.UnboundOS.FseHome` |
| Extension | `uap3:AppExtension` `Name="windows.gamingApp"` |
| Capability | `uap4:CustomCapability` `Microsoft.appCategory.gamingHome_8wekyb3d8bbwe` |
| SCCD | `AllowAny`, Catalog `FFFF` (sideload in Developer Mode) |
| Opt-in | `Install-UnboundOS.ps1 -XboxModeHome` or Options → Desktop / shell |
| Reverse | Same toggle Off, or Uninstall (removes package + preference) |
| Failure | Health `xbox` = FallbackRun. Stage 0 Run + fullscreen stays |
| Limited PC | Picker may be missing. We do **not** ship XFSET or write unofficial HKLM edition switches |

`--health` line `xbox`: Unavailable / PackageMissing /
RegisteredNotSelected / Selected / FallbackRun. Never fails the rest of
the report.

## Stage 2 — optional fallback (this PR + later)

| Piece | Design |
|-------|--------|
| HKCU Shell= | Already implemented. Watchdog, Shift bypass, 3 crashes / 2 min → Explorer |
| Shell Launcher v2 | Enterprise/Education only, **not Pro**. Separate “console” user. Crash fallback to Explorer. Helper for Switch to Desktop. Scaffold/docs only on Pro. |
| Assigned Access | Too locked for Steam/anticheat. Do not use. |

Default install **does not** set Shell= or Xbox mode.

## First-party surfaces (priority order)

1. Boots straight in (Stage 0/1) and **resume last game after sleep**
2. Unified library (Steam + Epic/GOG scans + Xbox/Store links + custom apps)
3. Guide-button quick menu (volume, network, Bluetooth, HDR, overlay, power)
4. System settings inside the shell (`ms-settings:` when we do not have a page)
5. Controller-first: on-screen keyboard, gamepad file picker, stick-as-mouse (UI only)
6. One-game-at-a-time switcher with close
7. Seamless switch to desktop (existing Desktop mode)
8. Recovery (watchdog, Shift, `-RestoreExplorer`)
9. Shader pre-cache **plan** / background notes / per-game profiles (no inject)
10. Themes leftover / widgets leftover / diagnostics export

## Safety rules (code)

- HKCU Winlogon Shell only. Hygiene tests fail HKLM Shell writes.
- No unofficial HKLM FSE edition switches.
- Watchdog never deletes the Explorer file.
- Xbox mode failure → FallbackRun, not a failed install.
- Uninstall restores Explorer and removes the FSE package if we added it.
- No `using var _` (CS1656).
- No D3D/Vulkan hooks. No game inject. No drivers.

## What this PR does **not** claim

- Pixel-perfect Explorer parity
- Verified anticheat in FSE or Shell= mode
- A custom logon UI
- HKLM / all-users replacement
- That Limited PC edition exposes “Choose home app”
- Debloated Windows or IoT LTSC as a path
