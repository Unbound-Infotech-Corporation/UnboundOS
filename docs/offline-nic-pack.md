# Offline NIC driver pack (image / OOBE)

UnboundOS does **not** ship multi-GB driver binaries in git. The
Windows image build fetches a redistributable Ethernet + Wi‑Fi pack
into `OfflineNicDrivers`, then Setup can delete that folder after a
successful online check (`online-ok.flag`). See `docs/os-spec.md` §1
and `SetupCleanupService`.

## Layout

```
installer/offline-nic-pack/          # this repo: README + manifest only
  README.md
  manifest.json
scripts/fetch-offline-nic-pack.ps1   # image-build fetch (no binaries)
%LocalAppData%\Unbound Infotech Corporation\UnboundOS\setup-leftovers\
  OfflineNicDrivers\                 # runtime pack the image stages
  online-ok.flag                     # set after first successful online
```

DISM / Windows Setup consume vendor INF trees under
`OfflineNicDrivers\<vendor>\<year>\`.

## Budget

**Hard cap: 5 GB** uncompressed. If the full set exceeds that, keep
this order and stop:

1. Intel Ethernet + Wi‑Fi (most OEM laptops/desktops)
2. Realtek Ethernet + Wi‑Fi
3. MediaTek / RZ616-class Wi‑Fi
4. Qualcomm / Atheros Wi‑Fi
5. Killer (Intel-based) only if still under budget
6. Broadcom / Marvell only when the vendor pack is clearly
   redistributable

Exclude: scraped “driver packs” from warez mirrors, modified INF
bundles, and anything without a public vendor license.

## Approach chosen

**Fetch at image-build time** — not Git LFS, not a GitHub Release
blob. `scripts/fetch-offline-nic-pack.ps1` records official vendor
download pages and a local folder layout. The image pipeline runs the
script, then copies the folder into the OOBE leftovers path.

Size estimate (typical current public packs, ~2023–2026 chipsets):

| Vendor | What | Est. |
|--------|------|------|
| Intel | Ethernet + Wi‑Fi PROSet/driver | 0.8–1.4 GB |
| Realtek | Ethernet + Wi‑Fi | 0.4–0.8 GB |
| MediaTek | Wi‑Fi | 0.2–0.4 GB |
| Qualcomm | Wi‑Fi | 0.2–0.5 GB |
| Killer / others | only if budget remains | ≤ 0.4 GB |
| **Total target** | | **~2–3.5 GB** (headroom to 5 GB) |

## Build

```powershell
pwsh -File scripts/fetch-offline-nic-pack.ps1 -OutDir .\artifacts\OfflineNicDrivers
```

The script creates the vendor folders and a `SOURCES.md` of official
URLs. It does **not** silently download multi-GB blobs unless
`-Download` is passed on a machine that is allowed to pull vendor
packages. Review each vendor EULA before redistributing on the image.

## Cleanup

`SetupCleanupService` deletes `OfflineNicDrivers` only when
`online-ok.flag` exists. The image owns the real OOBE wipe.
