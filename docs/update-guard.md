# Update Guard

Legal “security-quality without feature bloat” for UnboundOS. Genuine
Windows only. **UnboundOS does not host or redistribute Microsoft
`.msu` / `.cab` binaries.** Each PC downloads from Microsoft Windows
Update.

## What we claim (honest)

| We do | We do not |
|-------|-----------|
| Monthly **security quality** (LCU) from Microsoft | “CVE-only patches” |
| Defer / pin **feature** updates to the current display version | Disable Windows Update, Defender, or VBS |
| Turn off optional preview / “get latest as soon as available” | Filter solely on the legacy SecurityUpdates `CategoryIDs` GUID |
| Queue restart — never reboot mid Competitive session | Push Windows binaries from an Unbound CDN |

Microsoft’s monthly LCU includes **security + some nonsecurity**
content by design. That is still the supported channel.

## Architecture

```
Options → Updates
    │
    ├─ settings.json   updateGuardEnabled, updateGuardTargetRelease
    │
    └─ IUpdateGuardPolicy / WindowsUpdateGuardPolicy
           elevated HKLM write (same pattern as HAGS)
           each PC still talks to Microsoft WU
```

Unbound’s own shell channel (GitHub Releases later) is separate:
`UnboundProduct.ShellVersion` is a placeholder in v1.

Curated KB metadata (not a download pipe):
`docs/update-guard-bulletin.sample.json`.

## Policy (Pro / Education / Enterprise)

Supported client policies under
`HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate`:

- `TargetReleaseVersion` = 1
- `TargetReleaseVersionInfo` = **current** `DisplayVersion` (read from
  the OS; not a stale hardcoded build forever)
- `ProductVersion` = `Windows 11` or `Windows 10`
- `DeferFeatureUpdates` = 1, period 365 days
- Preview builds managed off
- Quality deferrals **not** set (AU stays on; `NoAutoUpdate` = 0)
- `AU\NoAutoRebootWithLoggedOnUsers` = 1
- UX `IsContinuousInnovationOptedIn` = 0

Apply / Restore need an **elevated** HKLM write. Preference is still
saved if the write is denied.

## Home edition

Official policy surface is limited. Consumer Home devices can still
receive automatic feature updates near end of servicing. Update Guard
on Home is **best-effort**: we write the same keys and tell the user
Microsoft may ignore them. Prefer Pro for a managed ring.

## Session-aware install

Session enter **does not** flip Update Guard or reboot. Competitive /
Streamer / Living Room only apply skinny HKCU + power + NIC posture.
If Windows reports `RebootRequired`, Options shows **needs restart**.
UnboundOS will not reboot during a live session — finish the match,
then restart from Windows.

## Restore

1. Options → Updates → **Restore** (or toggle Off)
2. Run elevated if HKLM denied
3. Windows Update service, Defender, and VBS stay on
4. Feature-deferral values are deleted; quality AU stays allowed

## Check for quality updates

Opens `ms-settings:windowsupdate` on this PC. Search and install stay
on Microsoft’s agent. Status probe is registry best-effort
(unknown / needs restart).

See [competitive-landscape.md](competitive-landscape.md) and
[gaming-skinny.md](gaming-skinny.md).
