# Copy a published UnboundOS Release folder into LocalAppData and optionally
# register HKCU Run autostart. Never sets Shell=. Explorer stays the Windows shell.
# Usage:
#   pwsh -File scripts/Install-UnboundOS.ps1
#   pwsh -File scripts/Install-UnboundOS.ps1 -Autostart
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot "..\artifacts\UnboundOS-Release"),
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS\app"),
    [switch]$Autostart
)

$ErrorActionPreference = "Stop"
$exeName = "UnboundOS.App.exe"
$sourceExe = Join-Path $Source $exeName
if (-not (Test-Path $sourceExe)) {
    throw "Publish first: pwsh -File scripts/Publish-UnboundOS.ps1  (missing $sourceExe)"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Write-Host "Installing UnboundOS -> $InstallDir"
Copy-Item -Path (Join-Path $Source "*") -Destination $InstallDir -Recurse -Force

$exe = Join-Path $InstallDir $exeName
$startDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Unbound Infotech Corporation"
New-Item -ItemType Directory -Force -Path $startDir | Out-Null
$shortcutPath = Join-Path $startDir "UnboundOS.lnk"
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "UnboundOS — gaming session shell. Alt+F4 returns to the Windows desktop."
$shortcut.Save()

$runPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
if ($Autostart) {
    $quoted = if ($exe -match "\s") { "`"$exe`"" } else { $exe }
    New-Item -Path $runPath -Force | Out-Null
    New-ItemProperty -Path $runPath -Name "UnboundOS" -Value $quoted -PropertyType String -Force | Out-Null
    Write-Host "Autostart ON via HKCU Run UnboundOS. Explorer is still the shell. No Shell=."
} else {
    Write-Host "Autostart OFF (default). Enable later in Options → Startup audit, or re-run with -Autostart."
}

Write-Host "Escape hatch: Alt+F4, or Start Menu → Windows desktop / Explorer. Uninstall: scripts/Uninstall-UnboundOS.ps1"
Write-Host "Health: & `"$exe`" --health"
Write-Host "Do not set HKCU\...\Winlogon Shell= and do not replace explorer.exe."
