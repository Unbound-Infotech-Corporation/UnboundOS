# UnboundOS main-OS test checklist (BIGDEAL)

Use after [install-main-os.md](install-main-os.md) and
[os-replacement-plan.md](os-replacement-plan.md). Send `health-latest.txt`
plus `watchdog.log` if Shell= is on.

## A. Install (app mode)

- [ ] Other drives unplugged during Windows Setup
- [ ] Genuine Windows 11 (not a stripped ISO)
- [ ] First boot on the new NVMe only, then other drives replugged
- [ ] `pwsh -File scripts\Publish-UnboundOS.ps1` produced `UnboundOS.App.exe`
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1` (autostart **off**, Shell= **off**)
- [ ] Start Menu shortcut opens UnboundOS as a normal window
- [ ] Super Clean Home: Diavlo labels, clock on the right, teal only on focus
- [ ] Motion kill-switch in Options still snaps; On still eases 320/380/260

## B. Reboot (app mode)

- [ ] Autostart still off: reboot lands on Explorer desktop, not UnboundOS
- [ ] Enable autostart in Options → Startup audit, reboot, UnboundOS opens
      **and** Explorer is still the shell (Task Manager → explorer.exe)
- [ ] Alt+F4 returns to the Windows desktop
- [ ] Disable autostart, reboot, desktop only

## C. Health

- [ ] Options → Health check → Run (or `--health`)
- [ ] Network OK
- [ ] GPU driver is the vendor package (not Microsoft Basic Display)
- [ ] Autostart state matches the toggle
- [ ] Shell replacement reports Off (unless you opted in)
- [ ] Update Guard: quality on / Defender on. Pin is optional
- [ ] Free disk reported; 40 GB+ left for quality updates and games
- [ ] `health-latest.txt` copied off-box

## D. Gaming (app mode, Explorer still the shell)

Pick a title that uses Easy Anti-Cheat, BattlEye, or Vanguard.

- [ ] Launch from Steam / the game’s own exe
- [ ] Competitive session: overlays strip, Explorer still running
- [ ] Game + anticheat start. No “explorer missing” / shell-host failures
- [ ] Exit session restores the desktop
- [ ] UnboundOS closed (Alt+F4) during a game still leaves the game alive

## E. Sleep / wake

- [ ] Sleep from Options → Desktop / shell → Sleep, wake, shell usable
- [ ] Clock on Home is correct after wake
- [ ] No stuck WebView (if Home is blank, reopen)

## F. Update cycle

- [ ] Options → Updates: Apply Update Guard (elevated if needed)
- [ ] Check for quality updates opens Microsoft Windows Update
- [ ] Quality/LCU can install. Feature/optional is deferred, not blocked WU
- [ ] Defender still running (`Get-MpComputerStatus` or Windows Security)
- [ ] Session enter did **not** turn Update Guard off

## G. Stage 1 — shell replacement (opt-in)

- [ ] `pwsh -File scripts\Install-UnboundOS.ps1 -ReplaceShell`
- [ ] HKCU `Winlogon\Shell` is `UnboundOS.App.exe --watchdog` (not HKLM)
- [ ] Sign out. UnboundOS comes up as the shell (no Explorer desktop)
- [ ] Hold **Shift** at the next sign-in → Explorer instead
- [ ] Sign out again without Shift → UnboundOS
- [ ] Options → Desktop / shell → Desktop mode starts explorer.exe
- [ ] Return focuses UnboundOS
- [ ] Power: Lock, then unlock. Sleep/wake if you are willing
- [ ] SWITCH page lists running windows; FOCUS works
- [ ] Volume + / − buttons change volume
- [ ] Kill UnboundOS.App three times quickly → Explorer comes back
- [ ] Rename/move the exe, sign in → recovery starts Explorer
- [ ] Manual: Ctrl+Shift+Esc → Run `explorer.exe`
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1 -RestoreExplorer` then sign out → Explorer shell

## H. Anticheat in shell-replacement mode (required, unverified)

Do **not** tick these as “compatible” unless they actually pass.

What Shell= **does:** this user no longer gets Explorer as the desktop
host after sign-in. What it **does not:** rename explorer.exe, touch
HKLM, disable WU/Defender/VBS, or replace NT.

- [ ] With HKCU Shell= still on, launch an **Easy Anti-Cheat** title
- [ ] Launch a **BattlEye** title
- [ ] Launch a **Vanguard** title (or skip if you do not own one)
- [ ] Note: game starts / anticheat splash / “explorer missing” / ban-risk UI
- [ ] If it fails: Desktop mode, then retry. If it still fails: `-RestoreExplorer`
- [ ] Write the title name + result next to `health-latest.txt`

UnboundOS does **not** claim these pass until this section is filled.

## I. Stage 2 — first-party UI

- [ ] Files: copy, paste, delete a test file. Eject a USB if you have one
- [ ] Files: Open with… on a text file
- [ ] APPS launcher: Start Menu item launches. Steam/Epic/GOG if installed
- [ ] Options → System settings: Wi-Fi/Ethernet list. Open Bluetooth (ms-settings)
- [ ] Audio volume keys. Display still offers NVIDIA/AMD
- [ ] Power plans list. Storage list. Apps list
- [ ] Windows Update tile still Update Guard (quality on)

## J. Uninstall / rollback

- [ ] `pwsh -File scripts\Uninstall-UnboundOS.ps1`
- [ ] HKCU Run has no UnboundOS value
- [ ] HKCU Winlogon has no Shell value
- [ ] Start Menu shortcut gone
- [ ] Reboot → normal Explorer desktop
- [ ] Optional: `-RemoveData` wipes LocalAppData UnboundOS

## Must not happen

- HKLM `Winlogon\Shell` written
- `explorer.exe` replaced or renamed
- Windows Update service or Defender disabled by default
- Microsoft `.msu` / `.cab` redistributed by UnboundOS
- Rainmeter required for Home
- Dual-boot or a custom bootloader as the default path
- A claim that EAC/Vanguard “just works” without section H

## Timing (optional)

```powershell
$env:UNBOUNDOS_PERF = "1"
```

Restart the shell, open Home / Settings / Files / Tools, then attach
`perf.log`. Expect `home.scene.start`, `home.catalogs`, `nav.warm-hubs`.
