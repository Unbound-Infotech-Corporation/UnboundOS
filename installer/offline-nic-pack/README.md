# Offline NIC pack (installer tree)

This folder is a **placeholder**. Driver binaries are not in git.

1. On the image-build machine run
   `scripts/fetch-offline-nic-pack.ps1`.
2. Download only redistributable vendor packs (see
   `docs/offline-nic-pack.md`).
3. Stage the result as `OfflineNicDrivers` next to `online-ok.flag`.
4. After first successful online, `SetupCleanupService` may delete it.

Cap: **5 GB**. Priority: Intel → Realtek → MediaTek → Qualcomm → others.
