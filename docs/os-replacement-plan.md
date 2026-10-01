# UnboundOS as the Windows shell (replacement plan)

C L direction: UnboundOS is the **primary UI** on the daily-driver PC.
Genuine Windows stays underneath (NT, drivers, DirectX, Store, Steam,
Easy Anti-Cheat / BattlEye / Vanguard binaries). UnboundOS becomes
**the shell** — not an app that sits on an Explorer desktop.

This is **opt-in**, **per-user**, and **reversible**. Default install
is still a normal app (Explorer remains `Shell=`).

Locked:

- Super Clean / Diavlo / teal (`#00F0FF`) on focus only
- Motion kill-switch
- No Rainmeter in the product path
- No Sony assets
- No redistributed Microsoft `.msu` / `.cab`
- No custom bootloader
- Update Guard stays: quality from Microsoft, Defender on
- Genuine Windows only

## Anticheat (read this before Shell=)

Setting `HKCU\...\Winlogon\Shell` to the UnboundOS watchdog **does**:

- Change what this **user** gets after sign-in instead of `explorer.exe`
- Remove the Explorer desktop, taskbar, and Start by default
- Leave NT, Win32, Win32k, CSRSS, DWM, and the game’s own processes alone

It **does not**:

- Replace, rename, or hook `explorer.exe`
- Change HKLM Winlogon (other users still get Explorer)
- Disable Windows Update, Defender, VBS, or anticheat services
- Install a custom bootloader
- Claim EAC / BattlEye / Vanguard are verified in this mode

Some anticheat stacks **look for Explorer / the NT desktop**. That is
**unverified** until C L runs a title in shell-replacement mode. If a
game fails, use **Desktop mode** (starts `explorer.exe` on demand) or
**-RestoreExplorer**. Manual checklist: [test-checklist.md](test-checklist.md)
section H.

## Stage 1 — Boot-to-shell (this PR)

| Piece | Design |
|-------|--------|
| Replacement | **HKCU only** `Software\Microsoft\Windows NT\CurrentVersion\Winlogon` `Shell` = `"UnboundOS.App.exe" --watchdog`. Never HKLM. Never rewrite `explorer.exe`. |
| Opt-in | `Install-UnboundOS.ps1 -ReplaceShell` or Options → Desktop / shell |
| Reverse | `Install-UnboundOS.ps1 -RestoreExplorer`, Uninstall, or `--restore-explorer` |
| Watchdog | Same exe, `--watchdog`. Restarts the UI. **3 exits in 2 minutes** → `explorer.exe` and a log. |
| Shift bypass | Hold **Shift** at sign-in; watchdog starts Explorer instead |
| Recovery | Logon task + `scripts/Restore-ExplorerShell.ps1 -IfMissing` if the exe is gone |
| Desktop mode | Options → Desktop mode starts `explorer.exe`. Close/return focuses UnboundOS. |
| Power | Lock / sleep / restart / shutdown / sign out / switch user from Options |
| Switcher | Running windows list (no taskbar). Activate / close. |
| Volume | Hardware volume keys (Windows OSD when the OS still hosts it) |
| Brightness | Deep-link `ms-settings:display` (no invented display stack) |
| Wallpaper | Shell field is Obsidian; not the Explorer desktop bitmap |
| Toasts / tray | Windows notification services stay; Action Center UI is Explorer. Tray page lists known background apps. |
| Win+keys | App registers Win+E (Files), Win+I (Settings), Win+Tab (Switcher), Win+D (Desktop). **Win+L** stays OS. **Alt+Tab** is DWM. |
| Manual recovery | Ctrl+Shift+Esc → File → Run `explorer.exe`. Safe Mode. `-RestoreExplorer`. |

Default install **does not** set Shell=. Autostart via HKCU Run is the
app-on-desktop path.

## Stage 2 — First-party core UI (this PR)

| Surface | First-party now | Fallback |
|---------|-----------------|----------|
| Files | Browse, copy, move, delete, open, open-with, USB eject | Desktop mode / Explorer |
| Wi-Fi / Ethernet | Adapter up/down list | `ms-settings:network` |
| Bluetooth | Status | `ms-settings:bluetooth` |
| Audio | Volume up/down/mute | `ms-settings:sound` |
| Display | Vendor app launch (existing) | `ms-settings:display` |
| Controllers | — | `ms-settings:gaming-gamebar` / devices |
| Power plans | `powercfg` list + set | `ms-settings:powersleep` |
| Storage | Drive free space | `ms-settings:storagesense` |
| Apps | Installed list + uninstall string | `ms-settings:appsfeatures` |
| Time / language | — | `ms-settings:dateandtime` / `regionlanguage` |
| Windows Update | Update Guard (existing) | `ms-settings:windowsupdate` |
| Accounts | — | `ms-settings:accounts` |
| Launcher | Start Menu `.lnk`, Steam, Epic, GOG, Xbox/Store links | Store URI |
| Switcher | EnumWindows | Alt+Tab |
| Tray stand-in | Discord / Steam / OBS / Vortex process list | Desktop mode |

Lock screen / sign-in stay **Windows**. Auto sign-in is a Windows
setting (`netplwiz` / Settings → Accounts). Documented, not shipped
as a custom credential provider.

## Stage 3 — Image / OOBE (scaffold + docs only)

- Offline NIC pack (already: [offline-nic-pack.md](offline-nic-pack.md))
- Post-install cleanup (Settings → Finish setup)
- 24h startup audit task ([startup-audit-task.xml](startup-audit-task.xml))
- DISM customize **genuine** Windows media only — [dism-image.md](dism-image.md)
- No slim ISOs, no unpaid Microsoft binaries in git

## Safety rules (code)

- HKCU Winlogon Shell only. Hygiene tests fail HKLM Shell writes.
- Watchdog never deletes the Explorer file.
- Missing exe → Explorer.
- Shift at launch → Explorer.
- 3 crashes / 2 min → Explorer + `watchdog.log`.
- Uninstall always restores Explorer.
- No `using var _` (CS1656).

## What this PR does **not** claim

- Pixel-perfect Explorer parity (Jump Lists, full Action Center, all
  Win+ chords, Explorer namespace extensions)
- Verified anticheat in Shell= mode
- A custom logon UI
- HKLM / all-users replacement
