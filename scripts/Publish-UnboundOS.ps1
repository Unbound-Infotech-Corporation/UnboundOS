# Publish a Release x64 UnboundOS shell for BIGDEAL.
# WinUI trim is unsafe — this script keeps PublishTrimmed=false and ReadyToRun on.
# Usage (from repo root, on Windows with .NET 8 + Windows App SDK):
#   pwsh -File scripts/Publish-UnboundOS.ps1
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutDir = (Join-Path $PSScriptRoot "..\artifacts\UnboundOS-Release")
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\UnboundOS.App\UnboundOS.App.csproj"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Write-Host "Publishing UnboundOS ($Configuration $Runtime) -> $OutDir"
dotnet publish "$project" `
    -c $Configuration `
    -p:Platform=x64 `
    -r $Runtime `
    --self-contained true `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=true `
    -p:WindowsPackageType=None `
    -o "$OutDir"

Write-Host "Published. Next: pwsh -File scripts/Install-UnboundOS.ps1"
Write-Host "Health: & `"$OutDir\UnboundOS.App.exe`" --health"
Write-Host "Perf log (optional): `$env:UNBOUNDOS_PERF='1'"
