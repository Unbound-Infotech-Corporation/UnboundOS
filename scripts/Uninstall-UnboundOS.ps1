# Remove UnboundOS, HKCU Run, and HKCU Shell=. Never touches HKLM, WU, or Defender.
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

$winlogon = "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon"
if (Test-Path $winlogon) {
    Remove-ItemProperty -Path $winlogon -Name "Shell" -ErrorAction SilentlyContinue
}
schtasks /Delete /TN "UnboundOS Shell Recovery" /F 2>$null | Out-Null
try {
    Get-AppxPackage -Name "UnboundInfotech.UnboundOS.FseHome" -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue
} catch { }
$pref = Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS\xbox-mode-home.json"
if (Test-Path $pref) {
    Remove-Item $pref -Force
}
Write-Host "Removed HKCU Run, HKCU Winlogon Shell, and Xbox mode home preference (if they existed). Explorer is this user's shell."

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

try { Start-Process explorer.exe } catch { }
Write-Host "UnboundOS uninstalled. Sign out if the desktop is blank. Windows Update and Defender were not touched."
