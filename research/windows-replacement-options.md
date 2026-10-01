# Windows replacement options (research note)

Lead path is **Windows 11 Xbox mode / Full Screen Experience (FSE)** as
the home app. HKCU `Shell=` stays a fallback. Do **not** use debloated
ISOs or IoT LTSC.

## What Microsoft ships

- Xbox mode / FSE rolled out to PCs (2026-04-30). KB5070297.
- News: https://news.xbox.com/en-us/2026/04/30/xbox-mode-pc-windows-11/
- KB: https://support.microsoft.com/en-us/servicing/os/windows/docs/2025/10/windows-gaming-full-screen-experience
- FSE defers taskbar / Start / desktop and suppresses startup apps.
  Kernel, drivers, and anticheat are untouched.
- **Full Handheld** edition: Settings → Gaming → Xbox mode →
  “Choose home app” + “Enter on startup”.
- **Limited PC** edition: those pickers are missing. Community tool
  [XFSET](https://github.com/8bit2qubit/XboxFullscreenExperienceTool)
  switches editions. UnboundOS does **not** ship or copy that tool.

## How community home apps register (exact)

Microsoft does **not** document third-party home-app registration.
OmniConsole, AnyFSE, Playnite-FSE-Launcher, and
[FullScreenExperienceShell](https://github.com/driver1998/FullScreenExperienceShell)
all use the same **packaged** pattern (studied 2026-10-01):

1. MSIX / APPX package (unpackaged Win32 **cannot** appear in Choose home app).
2. AppxManifest application extension:

```xml
<uap3:Extension Category="windows.appExtension">
  <uap3:AppExtension Name="windows.gamingApp" Id="App"
    DisplayName="UnboundOS" Description="UnboundOS FSE home"
    PublicFolder="Public"/>
</uap3:Extension>
```

3. Restricted + custom capabilities:

```xml
<rescap:Capability Name="runFullTrust" />
<uap4:CustomCapability Name="Microsoft.appCategory.gamingHome_8wekyb3d8bbwe" />
```

4. `CustomCapability.SCCD` (AllowAny, Catalog FFFF) so sideload in
   Developer Mode can declare `gamingHome`.

5. User picks the app in Settings → Gaming → Xbox mode (FSE) and
   enables “Enter Xbox mode on startup”.

UnboundOS templates: `packaging/fse/`. Install `-XboxModeHome` tries
sideload, then always enables Stage 0 (HKCU Run + fullscreen) if FSE
is missing or the user never selects the package.

## Other options (not the lead)

| Path | Notes |
|------|--------|
| HKCU Run + fullscreen | Stage 0. Works on Pro. Explorer still the shell. |
| HKCU Winlogon Shell= | Fallback. Watchdog + Shift + crash → Explorer. |
| Shell Launcher v2 | Enterprise/Education only. Separate console user. |
| Assigned Access / kiosk | Too locked for Steam/anticheat. |
| Debloat / LTSC IoT | Do not recommend. |

## Anticheat

FSE and Shell= both leave NT / DX / anticheat services alone. Still
**unverified** until the checklist matrix is run (Valorant/Vanguard,
EAC, BattlEye, EA Javelin, FACEIT). No game inject, no D3D/Vulkan
hooks, no drivers.
