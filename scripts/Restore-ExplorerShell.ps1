# Restore HKCU Winlogon Shell to Explorer. Never touches HKLM.
# Usage:
#   pwsh -File scripts/Restore-ExplorerShell.ps1
#   pwsh -File scripts/Restore-ExplorerShell.ps1 -IfMissing
[CmdletBinding()]
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS\app"),
    [switch]$IfMissing
)

$ErrorActionPreference = "Stop"
$exe = Join-Path $InstallDir "UnboundOS.App.exe"
if ($IfMissing -and (Test-Path $exe)) {
    Write-Host "UnboundOS.App.exe is present. Leaving HKCU Shell as-is."
    exit 0
}

$winlogon = "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon"
if (Test-Path $winlogon) {
    Remove-ItemProperty -Path $winlogon -Name "Shell" -ErrorAction SilentlyContinue
}

Write-Host "HKCU Winlogon Shell removed. Explorer is this user's shell after sign-out."
Write-Host "Manual: Ctrl+Shift+Esc → File → Run new task → explorer.exe. Safe Mode also works."
try {
    Start-Process explorer.exe
} catch {
    Write-Host "Could not start explorer.exe: $($_.Exception.Message)"
}
