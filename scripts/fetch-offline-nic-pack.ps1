# Fetch layout for the UnboundOS offline NIC driver pack.
# Does not commit binaries. Official vendor pages only. Cap: 5 GB.
# Usage:
#   pwsh -File scripts/fetch-offline-nic-pack.ps1 -OutDir .\artifacts\OfflineNicDrivers
#   pwsh -File scripts/fetch-offline-nic-pack.ps1 -OutDir .\artifacts\OfflineNicDrivers -Download
[CmdletBinding()]
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\artifacts\OfflineNicDrivers"),
    [switch]$Download
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$vendors = @(
    @{ Id = "intel";    Priority = 1; Kind = "Ethernet+WiFi"; Url = "https://www.intel.com/content/www/us/en/download-center/home.html"; Note = "PROSet/Wireless + Ethernet. First pick." }
    @{ Id = "realtek";  Priority = 2; Kind = "Ethernet+WiFi"; Url = "https://www.realtek.com/Download/List?cate_id=584"; Note = "PCIe/USB Ethernet + Wi-Fi. Second pick." }
    @{ Id = "mediatek"; Priority = 3; Kind = "WiFi"; Url = "https://www.mediatek.com/products/broadband-wifi"; Note = "RZ616-class. Third pick." }
    @{ Id = "qualcomm"; Priority = 4; Kind = "WiFi"; Url = "https://www.qualcomm.com/products/technology/wi-fi"; Note = "Atheros/WCN. Fourth pick." }
    @{ Id = "killer";   Priority = 5; Kind = "Ethernet+WiFi"; Url = "https://www.killernetworking.com/driver-downloads/"; Note = "Only if still under 5 GB." }
    @{ Id = "broadcom"; Priority = 6; Kind = "Ethernet+WiFi"; Url = "https://www.broadcom.com/support/download-search"; Note = "Only when redistribution is explicit." }
)

$sources = @("# Offline NIC pack sources", "", "Do not scrape unofficial driver dumps.", "")
foreach ($v in $vendors) {
    $dir = Join-Path $OutDir $v.Id
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $readme = @(
        "# $($v.Id)",
        "",
        "Priority: $($v.Priority)",
        "Kind: $($v.Kind)",
        "Official: $($v.Url)",
        $v.Note,
        "",
        "Place vendor INF/driver trees here. Keep the whole pack under 5 GB."
    ) -join "`n"
    Set-Content -Path (Join-Path $dir "README.md") -Value $readme -Encoding utf8
    $sources += "- $($v.Id) (p$($v.Priority) $($v.Kind)): $($v.Url) — $($v.Note)"
    if ($Download) {
        Write-Host "Open $($v.Url) and drop redistributable packs into $dir (EULA review required)."
    }
}

$sources += ""
$sources += "Runtime path after image stage: setup-leftovers/OfflineNicDrivers"
$sources += "Deleted after online-ok.flag (SetupCleanupService)."
Set-Content -Path (Join-Path $OutDir "SOURCES.md") -Value ($sources -join "`n") -Encoding utf8

$manifest = [ordered]@{
    name = "UnboundOS offline NIC pack"
    maxBytes = 5368709120
    years = "approx 2023-2026 chipsets"
    approach = "image-build fetch, not git binaries"
    vendors = $vendors
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $OutDir "manifest.json") -Encoding utf8
Write-Host "Layout ready at $OutDir (no multi-GB binaries committed)."
