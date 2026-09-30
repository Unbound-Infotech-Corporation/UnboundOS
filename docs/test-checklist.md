# UnboundOS main-OS test checklist (BIGDEAL)

Use after [install-main-os.md](install-main-os.md). Mark each row on the
4 TB NVMe install. Send `health-latest.txt` with the notes.

## A. Install

- [ ] Other drives unplugged during Windows Setup
- [ ] Genuine Windows 11 (not a stripped ISO)
- [ ] First boot on the new NVMe only, then other drives replugged
- [ ] `pwsh -File scripts\Publish-UnboundOS.ps1` produced `UnboundOS.App.exe`
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1` (autostart still **off**)
- [ ] Start Menu shortcut opens UnboundOS as a normal window
- [ ] Super Clean Home: Diavlo labels, clock on the right, teal only on focus
- [ ] Motion kill-switch in Options still snaps; On still eases 320/380/260

## B. Reboot

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
- [ ] Update Guard: quality on / Defender on. Pin is optional
- [ ] Free disk reported; 40 GB+ left for quality updates and games
- [ ] `health-latest.txt` copied off-box

## D. Gaming (anticheat)

Pick a title that uses Easy Anti-Cheat, BattlEye, or Vanguard.

- [ ] Launch from Steam / the game’s own exe, **not** by replacing Explorer
- [ ] Competitive session: overlays strip, Explorer still running
- [ ] Game + anticheat start. No “explorer missing” / shell-host failures
- [ ] Exit session restores the desktop
- [ ] UnboundOS closed (Alt+F4) during a game still leaves the game alive

## E. Sleep / wake

- [ ] Sleep from Start, wake, UnboundOS still usable (or reopen from Start)
- [ ] Clock on Home is correct after wake
- [ ] No stuck WebView (if Home is blank, Alt+F4 and reopen)

## F. Update cycle

- [ ] Options → Updates: Apply Update Guard (elevated if needed)
- [ ] Check for quality updates opens Microsoft Windows Update
- [ ] Quality/LCU can install. Feature/optional is deferred, not blocked WU
- [ ] Defender still running (`Get-MpComputerStatus` or Windows Security)
- [ ] Session enter did **not** turn Update Guard off

## G. Uninstall / rollback

- [ ] `pwsh -File scripts\Uninstall-UnboundOS.ps1`
- [ ] HKCU Run has no UnboundOS value
- [ ] Start Menu shortcut gone
- [ ] Reboot → normal Explorer desktop
- [ ] Optional: `-RemoveData` wipes LocalAppData UnboundOS

## Must not happen

- `Shell=` set under Winlogon
- `explorer.exe` replaced or renamed
- Windows Update service or Defender disabled by default
- Microsoft `.msu` / `.cab` redistributed by UnboundOS
- Rainmeter required for Home
- Dual-boot or a custom bootloader as the default path

## Timing (optional)

```powershell
$env:UNBOUNDOS_PERF = "1"
```

Restart the shell, open Home / Settings / Files / Tools, then attach
`perf.log`. Expect `home.scene.start`, `home.catalogs`, `nav.warm-hubs`,
`nav.Settings` (cache hit after warm). Home timers should be quiet when
a session is live or Home is hidden.
