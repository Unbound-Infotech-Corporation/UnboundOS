# Copy a published UnboundOS Release folder into LocalAppData.
# Default: normal app (Explorer stays the shell).
#   pwsh -File scripts/Install-UnboundOS.ps1
#   pwsh -File scripts/Install-UnboundOS.ps1 -Autostart
# Lead path — Xbox mode / FSE home (falls back to Run + fullscreen):
#   pwsh -File scripts/Install-UnboundOS.ps1 -XboxModeHome
# Opt-in per-user shell replacement (HKCU Shell= watchdog, never HKLM):
#   pwsh -File scripts/Install-UnboundOS.ps1 -ReplaceShell
# Reverse replacement:
#   pwsh -File scripts/Install-UnboundOS.ps1 -RestoreExplorer
[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot "..\artifacts\UnboundOS-Release"),
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Unbound Infotech Corporation\UnboundOS\app"),
    [switch]$Autostart,
    [switch]$XboxModeHome,
    [switch]$ReplaceShell,
    [switch]$RestoreExplorer
)

$ErrorActionPreference = "Stop"
$runPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$winlogon = "HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon"
$taskName = "UnboundOS Shell Recovery"
$dataRoot = Split-Path $InstallDir -Parent

function Restore-ExplorerShellValue {
    if (Test-Path $winlogon) {
        Remove-ItemProperty -Path $winlogon -Name "Shell" -ErrorAction SilentlyContinue
    }
    schtasks /Delete /TN $taskName /F 2>$null | Out-Null
    Write-Host "HKCU Winlogon Shell cleared. Explorer is this user's shell after sign-out."
}

function Enable-Stage0Run {
    param([string]$Exe)
    $quoted = if ($Exe -match "\s") { "`"$Exe`"" } else { $Exe }
    New-Item -Path $runPath -Force | Out-Null
    New-ItemProperty -Path $runPath -Name "UnboundOS" -Value "$quoted --fullscreen" -PropertyType String -Force | Out-Null
}

function Write-XboxPreference {
    param([string]$State, [string]$Detail)
    New-Item -ItemType Directory -Force -Path $dataRoot | Out-Null
    $pref = Join-Path $dataRoot "xbox-mode-home.json"
    $json = @{
        Wanted = $true
        FullscreenAtStartup = $true
        State = $State
        Detail = $Detail
        UserConfirmedSelected = $false
        UpdatedUtc = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json
    Set-Content -Path $pref -Value $json -Encoding UTF8
}

if ($RestoreExplorer) {
    Restore-ExplorerShellValue
    try { Start-Process explorer.exe } catch { }
    Write-Host "Recovery: Ctrl+Shift+Esc → Run explorer.exe. Hold Shift at sign-in if watchdog is still set."
    return
}

$exeName = "UnboundOS.App.exe"
$sourceExe = Join-Path $Source $exeName
if (-not (Test-Path $sourceExe)) {
    throw "Publish first: pwsh -File scripts/Publish-UnboundOS.ps1  (missing $sourceExe)"
}

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Write-Host "Installing UnboundOS -> $InstallDir"
Copy-Item -Path (Join-Path $Source "*") -Destination $InstallDir -Recurse -Force

$restoreSrc = Join-Path $PSScriptRoot "Restore-ExplorerShell.ps1"
if (Test-Path $restoreSrc) {
    Copy-Item $restoreSrc -Destination (Join-Path $InstallDir "Restore-ExplorerShell.ps1") -Force
}

$fseSrc = Join-Path $PSScriptRoot "..\packaging\fse"
if (Test-Path $fseSrc) {
    Copy-Item -Path (Join-Path $fseSrc "*") -Destination $InstallDir -Force
}

$exe = Join-Path $InstallDir $exeName
$startDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Unbound Infotech Corporation"
New-Item -ItemType Directory -Force -Path $startDir | Out-Null
$shortcutPath = Join-Path $startDir "UnboundOS.lnk"
$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "UnboundOS. Alt+F4 closes the UI; watchdog restarts it if Shell= is set. Desktop mode starts Explorer."
$shortcut.Save()

if ($ReplaceShell) {
    New-Item -Path $winlogon -Force | Out-Null
    $quoted = if ($exe -match "\s") { "`"$exe`"" } else { $exe }
    New-ItemProperty -Path $winlogon -Name "Shell" -Value "$quoted --watchdog" -PropertyType String -Force | Out-Null
    Remove-ItemProperty -Path $runPath -Name "UnboundOS" -ErrorAction SilentlyContinue
    $restore = Join-Path $InstallDir "Restore-ExplorerShell.ps1"
    $tr = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$restore`" -IfMissing"
    schtasks /Create /TN $taskName /SC ONLOGON /RL LIMITED /F /TR $tr | Out-Null
    Write-Host "HKCU Shell= watchdog (Stage 2 fallback). Hold Shift at sign-in for Explorer. Never HKLM. Recovery task: $taskName"
    Write-Host "Anticheat: Shell= is unverified. Use Desktop mode or -RestoreExplorer if a title fails."
} elseif ($XboxModeHome) {
    Enable-Stage0Run -Exe $exe
    $state = "FallbackRun"
    $detail = "Stage 0 Run + fullscreen is on."
    $manifest = Join-Path $InstallDir "Package.appxmanifest"
    if (Test-Path $manifest) {
        try {
            Add-AppxPackage -Register $manifest -ErrorAction Stop
            $state = "RegisteredNotSelected"
            $detail = "Package registered. Pick UnboundOS in Settings -> Gaming -> Xbox mode if the picker exists."
            Write-Host $detail
        } catch {
            $state = "FallbackRun"
            $detail = "Sideload failed ($($_.Exception.Message)). Staying on HKCU Run + fullscreen. Enable Developer Mode and retry, or ignore."
            Write-Host $detail
        }
    } else {
        Write-Host "packaging/fse manifest not copied. Stage 0 Run + fullscreen still on."
    }
    Write-XboxPreference -State $state -Detail $detail
    foreach ($uri in @("ms-settings:gaming-fullscreen", "ms-settings:gaming-xboxmode", "ms-settings:gaming")) {
        try {
            Start-Process $uri
            break
        } catch { }
    }
    Write-Host "Xbox mode home requested. Microsoft does not document third-party registration. Health line: xbox."
    Write-Host "Auto sign-in is Windows (netplwiz / Settings -> Accounts). UnboundOS does not ship a credential provider."
} elseif ($Autostart) {
    Enable-Stage0Run -Exe $exe
    Write-Host "Autostart ON via HKCU Run + fullscreen (Stage 0). Explorer is still the shell."
} else {
    Write-Host "App mode (default). Explorer stays the shell. -Autostart, -XboxModeHome, or -ReplaceShell to opt in."
}

Write-Host "Desktop mode: Options -> Desktop / shell. Uninstall: scripts/Uninstall-UnboundOS.ps1"
Write-Host "Health: & `"$exe`" --health"
Write-Host "Restore Explorer: pwsh -File scripts/Install-UnboundOS.ps1 -RestoreExplorer"
