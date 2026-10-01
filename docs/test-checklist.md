# UnboundOS main-OS test checklist (BIGDEAL)

Use after [install-main-os.md](install-main-os.md) and
[os-replacement-plan.md](os-replacement-plan.md). Send `health-latest.txt`
plus `watchdog.log` if Shell= is on.

## A. Install (app mode)

- [ ] Other drives unplugged during Windows Setup
- [ ] Genuine Windows 11 (not a stripped ISO, not IoT LTSC)
- [ ] First boot on the new NVMe only, then other drives replugged
- [ ] `pwsh -File scripts\Publish-UnboundOS.ps1` produced `UnboundOS.App.exe`
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1` (autostart **off**, Xbox **off**, Shell= **off**)
- [ ] Start Menu shortcut opens UnboundOS as a normal window
- [ ] Super Clean Home: Diavlo labels, clock on the right, teal only on focus
- [ ] Motion kill-switch in Options still snaps; On still eases 320/380/260

## B. Reboot (app mode)

- [ ] Autostart still off: reboot lands on Explorer desktop, not UnboundOS
- [ ] Enable autostart in Options → Startup audit, reboot, UnboundOS opens
      **fullscreen** and Explorer is still the shell (Task Manager → explorer.exe)
- [ ] Alt+F4 returns to the Windows desktop
- [ ] Disable autostart, reboot, desktop only

## C. Health

- [ ] Options → Health check → Run (or `--health`)
- [ ] Network OK
- [ ] GPU driver is the vendor package (not Microsoft Basic Display)
- [ ] Autostart state matches the toggle
- [ ] **xbox** line present (Unavailable / PackageMissing / RegisteredNotSelected / Selected / FallbackRun)
- [ ] Shell replacement reports Off (unless you opted in)
- [ ] Update Guard: quality on / Defender on. Pin is optional
- [ ] Free disk reported; 40 GB+ left for quality updates and games
- [ ] `health-latest.txt` copied off-box
- [ ] Export diagnostics writes a folder (health, xbox preference, last session)

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
- [ ] Last game still running is focused (or Guide → Resume last game)
- [ ] No stuck WebView (if Home is blank, reopen)

## F. Update cycle

- [ ] Options → Updates: Apply Update Guard (elevated if needed)
- [ ] Check for quality updates opens Microsoft Windows Update
- [ ] Quality/LCU can install. Feature/optional is deferred, not blocked WU
- [ ] Defender still running (`Get-MpComputerStatus` or Windows Security)
- [ ] Session enter did **not** turn Update Guard off

## G. Stage 0 — Run + fullscreen + own settings/picker

- [ ] Auto sign-in via `netplwiz` (optional, weaker security). Lock screen stays Windows
- [ ] Files: gamepad picker ▲/▼, on-screen KEYBOARD types a filter
- [ ] Options pages stay inside UnboundOS (system catalog, power, Xbox toggle)
- [ ] Guide (F1 / GamepadView) opens volume / network / Bluetooth / HDR / overlay / power

## G2. Stage 1 — Xbox mode home (lead)

- [ ] Developer Mode on if you want sideload
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1 -XboxModeHome`
- [ ] HKCU Run is `UnboundOS.App.exe --fullscreen`
- [ ] Health `xbox` is FallbackRun **or** RegisteredNotSelected **or** Selected — never a failed install
- [ ] If the picker exists: Settings → Gaming → Xbox mode → UnboundOS → Enter on startup
- [ ] Options → I PICKED UNBOUNDOS, health becomes Selected
- [ ] Limited PC (no picker): stay on FallbackRun. Do **not** run XFSET unless you choose to
- [ ] Toggle Xbox mode Off. Preference cleared. Run still reversible
- [ ] Uninstall removes the FSE package if it was registered

## G3. Stage 2 — shell replacement (opt-in fallback)

- [ ] `pwsh -File scripts\Install-UnboundOS.ps1 -ReplaceShell`
- [ ] HKCU `Winlogon\Shell` is `UnboundOS.App.exe --watchdog` (not HKLM)
- [ ] Sign out. UnboundOS comes up as the shell (no Explorer desktop)
- [ ] Hold **Shift** at the next sign-in → Explorer instead
- [ ] Sign out again without Shift → UnboundOS
- [ ] Options → Desktop / shell → Desktop mode starts explorer.exe
- [ ] Return focuses UnboundOS
- [ ] Power: Lock, then unlock. Sleep/wake if you are willing
- [ ] SWITCH page lists running windows; FOCUS / CLOSE work; Games list is one-at-a-time
- [ ] Volume + / − buttons change volume
- [ ] Kill UnboundOS.App three times quickly → Explorer comes back
- [ ] Rename/move the exe, sign in → recovery starts Explorer
- [ ] Manual: Ctrl+Shift+Esc → Run `explorer.exe`
- [ ] `pwsh -File scripts\Install-UnboundOS.ps1 -RestoreExplorer` then sign out → Explorer shell
- [ ] Shell Launcher v2: skip on Pro (Enterprise/Education only)

## H. Anticheat matrix (required, unverified)

Do **not** tick these as “compatible” unless they actually pass.
Run once in **app mode**, once in **Xbox mode / FallbackRun**, once in
**Shell=** if you opted in. Write the title + result next to
`health-latest.txt`.

What FSE and Shell= **do:** hide or skip Explorer as this user’s
desktop host. What they **do not:** rename explorer.exe, touch HKLM
Winlogon, disable WU/Defender/VBS, inject into the game, or hook
D3D/Vulkan.

| Stack | Title you own | App mode | Xbox / FallbackRun | Shell= | Notes |
|-------|---------------|----------|--------------------|--------|-------|
| Riot Vanguard | Valorant (or another Vanguard title) | [ ] | [ ] | [ ] | |
| Easy Anti-Cheat | | [ ] | [ ] | [ ] | |
| BattlEye | | [ ] | [ ] | [ ] | |
| EA Javelin | | [ ] | [ ] | [ ] | |
| FACEIT | | [ ] | [ ] | [ ] | |

- [ ] If it fails: Desktop mode, then retry. If it still fails: turn Xbox mode off or `-RestoreExplorer`

UnboundOS does **not** claim these pass until this table is filled.

## I. First-party UI

- [ ] Files: copy, paste, delete a test file. Eject a USB if you have one
- [ ] Files: Open with… on a text file. Gamepad picker + keyboard
- [ ] APPS / Games: Steam + Epic/GOG if installed + custom app JSON
- [ ] Options → System settings: Wi-Fi/Ethernet list. Open Bluetooth (ms-settings)
- [ ] Audio volume keys. Display still offers NVIDIA/AMD
- [ ] Power plans list. Storage list. Apps list
- [ ] Windows Update tile still Update Guard (quality on)
- [ ] Guide overlay is in-shell telemetry, not a game hook

## J. Uninstall / rollback

- [ ] `pwsh -File scripts\Uninstall-UnboundOS.ps1`
- [ ] HKCU Run has no UnboundOS value
- [ ] HKCU Winlogon has no Shell value
- [ ] Xbox preference gone; FSE package gone if we added it
- [ ] Start Menu shortcut gone
- [ ] Reboot → normal Explorer desktop
- [ ] Optional: `-RemoveData` wipes LocalAppData UnboundOS

## Must not happen

- HKLM `Winlogon\Shell` written
- Unofficial HKLM FSE edition switches written by UnboundOS
- `explorer.exe` replaced or renamed
- Windows Update service or Defender disabled by default
- Microsoft `.msu` / `.cab` redistributed by UnboundOS
- Debloat ISO or IoT LTSC recommended
- Rainmeter required for Home
- Dual-boot or a custom bootloader as the default path
- Game inject / D3D / Vulkan hooks / a filter driver
- A claim that EAC/Vanguard/Javelin/FACEIT “just works” without section H

## Timing (optional)

```powershell
$env:UNBOUNDOS_PERF = "1"
```

Restart the shell, open Home / Settings / Files / Tools, then attach
`perf.log`. Expect `home.scene.start`, `home.catalogs`, `nav.warm-hubs`.
