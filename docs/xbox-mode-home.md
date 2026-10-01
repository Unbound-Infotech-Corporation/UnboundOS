# Xbox mode (FSE) home app — UnboundOS

Lead path for “boots as the OS UI”. Microsoft FSE hides taskbar/Start
and suppresses startup apps. Kernel and anticheat stay Windows.

Microsoft does **not** document third-party home-app registration.
This file records the **community-legal** mechanism (same as OmniConsole,
AnyFSE, Playnite FSE, FullScreenExperienceShell) and how UnboundOS
uses it.

## Registration (exact)

A packaged (MSIX) identity with:

| Piece | Value |
|-------|--------|
| Extension | `uap3:AppExtension` `Name="windows.gamingApp"` |
| Capability | `uap4:CustomCapability` `Microsoft.appCategory.gamingHome_8wekyb3d8bbwe` |
| Trust | `rescap:Capability` `runFullTrust` |
| SCCD | `packaging/fse/CustomCapability.SCCD` (`AllowAny`, Catalog `FFFF`) |

Templates: `packaging/fse/Package.appxmanifest`.

Unpackaged `UnboundOS.App.exe` **cannot** appear in “Choose home app”.
Sideload needs Windows Developer Mode (`Add-AppxPackage`). After
install, the user (or script) opens Settings → Gaming → Xbox mode and
selects **UnboundOS**, then enables **Enter Xbox mode on startup**.

On **Limited PC** FSE the picker is missing. Use Microsoft’s rollout
or (user choice) XFSET. UnboundOS does not ship XFSET and does not
write unofficial HKLM edition switches.

## Install

```powershell
pwsh -File scripts\Install-UnboundOS.ps1 -XboxModeHome
```

That always enables **Stage 0**: HKCU Run + fullscreen. Then it tries
to sideload `packaging/fse` if `Add-AppxPackage` works. On any failure
it stays in Run+fullscreen and health reports **fallback**.

Reverse: Options → Desktop / shell → Xbox mode Off, or Uninstall.

## Detect / health

`--health` line `xbox`: Unavailable / PackageMissing / RegisteredNotSelected /
Selected / FallbackRun. Never fails the rest of the report.

Settings URI (try in order): `ms-settings:gaming-fullscreen`,
`ms-settings:gaming-xboxmode`, `ms-settings:gaming`.

## Auto sign-in (Windows, not UnboundOS)

Lock screen stays Windows. Auto sign-in:

1. Settings → Accounts → Sign-in options → “If you’ve been away, when
   should Windows require you to sign in again?” → Never (weaker).
2. Or `netplwiz` → uncheck “Users must enter a user name and password”.

Do not ship a custom credential provider.

## Safety

- No game inject, no D3D/Vulkan hooks, no drivers.
- Update Guard / Defender stay on.
- Genuine Windows only. No debloat ISO, no IoT LTSC.
