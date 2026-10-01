# Stage 3 scaffold — genuine Windows image (DISM)

Not implemented in this PR. UnboundOS does **not** ship a custom ISO.

When the image/OOBE owner picks this up:

1. Start from **genuine** Windows 11 media (Media Creation Tool / VL).
2. Use DISM `/Mount-Image`, `/Add-Driver` (vendor NIC/GPU from official
   packs), `/Add-ProvisionedAppxPackage` only if licensed.
3. Do **not** strip WinSxS, do **not** remove Defender, do **not**
   disable Windows Update, do **not** redistribute `.msu` / `.cab`.
4. Offline NIC pack: [offline-nic-pack.md](offline-nic-pack.md).
5. Post-install cleanup: Settings → Finish setup + image OOBE wipe.
6. 24h startup audit: [startup-audit-task.xml](startup-audit-task.xml).
7. Optional: after first sign-in, run
   `Install-UnboundOS.ps1 -ReplaceShell` for that user only.

UnboundOS remains a WinUI shell + watchdog. It is not a bootloader.
