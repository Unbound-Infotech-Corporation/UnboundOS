# Install UnboundOS as the daily driver (BIGDEAL)

C L path: blank 4 TB NVMe → genuine Windows 11 → UnboundOS as the
primary UI. Default install is a **normal WinUI app** (Explorer stays
the shell). Lead opt-in is `-XboxModeHome` (FSE home app, falls back to
HKCU Run + fullscreen). `-ReplaceShell` is the Stage 2 HKCU `Shell=`
fallback. Never HKLM. Never a stripped ISO, IoT LTSC, or Microsoft
`.msu` / `.cab`. Plan: [os-replacement-plan.md](os-replacement-plan.md).
Xbox mode: [xbox-mode-home.md](xbox-mode-home.md).

Locked:

- Genuine Windows only. No custom bootloader. No dual-boot as default.
- WinUI shell only. Diavlo for shell text. Teal (`#00F0FF`) only for focus.
- Rainmeter is optional and **off** — not in this product path.
- Anticheat in Xbox mode or Shell= is **unverified**. Use Desktop mode
  or `-RestoreExplorer` if a title fails. Run the matrix in
  [test-checklist.md](test-checklist.md) H.

## 0. On the current PC, publish the shell

From a Windows 11 machine with the .NET 8 SDK and Windows App SDK
(BIGDEAL after Windows is installed, or a build box):

```powershell
git clone <this-repo>
cd UnboundOS
git checkout cursor/unboundos-brand-restyle-583c
dotnet test tests\UnboundOS.Tests\UnboundOS.Tests.csproj --filter "FullyQualifiedName!~NetworkDirectorMetric"
pwsh -File scripts\Publish-UnboundOS.ps1
```

Output: `artifacts\UnboundOS-Release\UnboundOS.App.exe` (self-contained
x64, ReadyToRun, **not** trimmed — trim is unsafe for WinUI).

Copy that folder onto a USB stick if you published elsewhere.

## 1. Genuine Windows on the new NVMe

1. Unplug **every other drive** (SATA and extra NVMe). Leave only the
   blank 4 TB NVMe so Setup cannot hide Windows on the old disk.
2. Boot Microsoft Windows 11 installation media (Media Creation Tool /
   official ISO). Not a tiny/lite/gamer ISO.
3. Install Windows 11 Pro (or Home — Update Guard is best-effort on Home)
   onto the 4 TB NVMe. Let it use the whole disk. BitLocker later is fine.
4. Complete Microsoft OOBE. Sign in. Do **not** replace Explorer.
5. Plug other drives back in **after** the first successful sign-in.
6. In BIOS/UEFI, confirm the 4 TB NVMe is the first boot device.

## 2. Get online (NIC driver pack)

UnboundOS does not bundle NIC binaries in git.

- If Ethernet/Wi‑Fi already works: skip this.
- If the board has no driver yet: use the **offline NIC pack** the image
  build fetched from **official vendor pages** (Intel first, then Realtek,
  MediaTek, Qualcomm). Layout and 5 GB cap: [offline-nic-pack.md](offline-nic-pack.md).
  `scripts/fetch-offline-nic-pack.ps1` records vendor URLs; it does not
  scrape unofficial dumps.
- After the PC can reach the network, Settings → Finish setup can delete
  the leftover pack folder (image owns the full OOBE wipe).

Install the **vendor GPU driver** from NVIDIA / AMD / Intel. UnboundOS
does not bundle GPU packs. Microsoft Basic Display Adapter fails the
health check.

## 3. First-boot UnboundOS

```powershell
pwsh -File scripts\Install-UnboundOS.ps1
```

Default:

- Copies the published folder to
  `%LocalAppData%\Unbound Infotech Corporation\UnboundOS\app`
- Adds a Start Menu shortcut
- **Autostart off** (no HKCU Run value yet)
- **Xbox mode off** (no FSE home registration)
- **Shell replacement off** (Explorer is still this user's shell)

Optional sign-in launch (Stage 0, still Explorer, fullscreen):

```powershell
pwsh -File scripts\Install-UnboundOS.ps1 -Autostart
```

Lead path — **Xbox mode home** (sideload FSE package if Developer Mode
allows it; always enables Stage 0 Run + fullscreen on failure):

```powershell
pwsh -File scripts\Install-UnboundOS.ps1 -XboxModeHome
```

Auto sign-in stays Windows: Settings → Accounts, or `netplwiz`.
UnboundOS does not ship a credential provider.

Opt-in **per-user shell replacement** (Stage 2 fallback, HKCU Shell= watchdog, never HKLM):

```powershell
pwsh -File scripts\Install-UnboundOS.ps1 -ReplaceShell
```

Sign out. Hold **Shift** during sign-in to get Explorer instead.
3 crashes in 2 minutes fall back to Explorer. Recovery task starts
Explorer if `UnboundOS.App.exe` is missing.

Do **not**:

- Write HKLM Winlogon Shell
- Write unofficial HKLM FSE edition switches
- Rename or replace `explorer.exe`
- Recommend a debloat ISO or IoT LTSC
- Disable Windows Update or Microsoft Defender to “make FPS go up”

## 4. Update Guard defaults

Microsoft quality / LCU stays **on**. Defender stays **on**. Feature /
optional preview churn can be deferred.

Options → Updates → Apply Update Guard (may need elevation for HKLM).
Honest copy: [update-guard.md](update-guard.md). UnboundOS never
redistributes Windows patches; this PC downloads from Microsoft.

Session enter does **not** flip Update Guard.

## 5. Startup audit

Options → Startup audit → Scan now. Pin GPU vendor, anticheat, Vortex,
OBS. Apply recommended only disables Review items in HKCU Run / Startup
folder. It never silently kills EAC, Vanguard, BattlEye, or Explorer.

Daily image task (later): [startup-audit-task.xml](startup-audit-task.xml)
launches `UnboundOS.App.exe --audit-startup`.

## 6. Health check (send this log back)

In the shell: Options → Health check → Run health check.

Headless (no window):

```powershell
& "$env:LOCALAPPDATA\Unbound Infotech Corporation\UnboundOS\app\UnboundOS.App.exe" --health
```

Writes:

- `%LocalAppData%\Unbound Infotech Corporation\UnboundOS\health-YYYYMMDD-HHMMSS.txt`
- `health-latest.txt` (same folder)

Probes: network up, GPU driver (not Basic Display), autostart, Xbox
mode home, shell replacement, Update Guard, free disk (≥ 40 GB warned).
Does not change WU or Defender.

Optional timing log (off by default):

```powershell
$env:UNBOUNDOS_PERF = "1"
# Restart UnboundOS, then read perf.log in the same LocalAppData folder.
```

## 7. Escape hatch (always)

| Want | Do |
|------|----|
| Windows desktop right now | Options → Desktop / shell → **Desktop mode** (starts explorer.exe) |
| Explorer as the shell again | `pwsh -File scripts\Install-UnboundOS.ps1 -RestoreExplorer` then sign out |
| Watchdog still up, UI dead | Hold **Shift** at sign-in, or Ctrl+Shift+Esc → Run `explorer.exe` |
| Xbox mode off | Options → Desktop / shell → Xbox mode Off, or Uninstall |
| Uninstall | `pwsh -File scripts\Uninstall-UnboundOS.ps1` (clears HKCU Shell= and FSE package) |
| Wipe settings too | `pwsh -File scripts\Uninstall-UnboundOS.ps1 -RemoveData` |

Anticheat in replacement mode is unverified. If EAC / Vanguard / BattlEye
fail, use Desktop mode or `-RestoreExplorer`. See [test-checklist.md](test-checklist.md) H.

## 8. Rollback

1. Close UnboundOS.
2. Run `Uninstall-UnboundOS.ps1`.
3. Confirm HKCU Run has no `UnboundOS` value and HKCU Winlogon has no `Shell`.
4. Reboot. You should get a stock Windows desktop.

Windows itself is not rolled back. Quality updates stay on Microsoft WU.

## Related

- Replacement plan: [os-replacement-plan.md](os-replacement-plan.md)
- Xbox mode home: [xbox-mode-home.md](xbox-mode-home.md)
- Product spec: [os-spec.md](os-spec.md)
- QA loop: [test-checklist.md](test-checklist.md)
- Session skinny: [gaming-skinny.md](gaming-skinny.md)
