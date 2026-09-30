# Remove UnboundOS autostart, Start Menu shortcut, and the installed app folder.
# Does not touch Explorer, Shell=, Windows Update, or Defender.
# Usage:
#   pwsh -File scripts/Uninstall-UnboundOS.ps1
#   pwsh -File scripts/Uninstall-UnboundOS.ps1 -RemoveData
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS\app"),
    [switch]$RemoveData
)

$ErrorActionPreference = "Stop"

Get-Process -Name "UnboundOS.App" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

$runPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $runPath -Name "UnboundOS" -ErrorAction SilentlyContinue
Write-Host "Removed HKCU Run UnboundOS (if it existed). Explorer was never replaced."

$shortcutPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Unbound Infotech Corporation\UnboundOS.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
    Write-Host "Removed $InstallDir"
}

if ($RemoveData) {
    $data = Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS"
    if (Test-Path $data) {
        Remove-Item $data -Recurse -Force
        Write-Host "Removed $data (settings, health logs, profiles)."
    }
} else {
    Write-Host "Kept LocalAppData settings/logs. Pass -RemoveData to wipe them."
}

Write-Host "UnboundOS uninstalled. Sign out or reboot if a leftover window is stuck. Windows desktop is unchanged."
