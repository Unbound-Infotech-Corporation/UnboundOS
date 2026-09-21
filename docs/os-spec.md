# UnboundOS product spec (OS-level)

Source of truth for the Unbound Infotech **UnboundOS** shell and the
WinUnbound **Windows image / OOBE** work that sits under it.

UnboundOS is a **console-style session shell on Windows**. It does **not**
replace NT, Explorer, or the Win32 world that Easy Anti-Cheat, BattlEye,
and Vanguard expect.

This document is the full spec. The current UnboundOS PR ships a **first
slice** only (called out per section). Image/OOBE owners pick up the rest.

Brand stays Unbound Infotech first: obsidian, cyan pulse, cobalt, spare
circuit amber. Restrained professional buttons. A hint of cyberpunk
atmosphere — not a neon costume. No ads. No silent overclock. Treat
**x64** as the WinUI platform.

Home navigation is a **3D cube** (WinUI Composition, not Unreal). Faces:
Session, Tools, Network, Mods, Files, Hardware. Settings, Profiles, and
Overlay stay in the top chrome.

## 1. OOBE / initial setup last step

**Image / OOBE (source of the real wipe):** after a successful online
check, the last setup step cleans unneeded files:

- Windows and installer temp
- Installer leftovers
- The **offline NIC driver pack** once the machine has confirmed it can
  reach the network and does not need the pack

**This UnboundOS slice:** Settings → **Finish setup / clean leftover
install files**. It only deletes **known Unbound leftover folders**
(setup staging, installer logs, an offline NIC pack directory **after**
an `online-ok` marker exists). Honest copy: the Windows image owns the
full wipe.

## 2. Every 24 hours — startup audit

Scan and keep only what is **needed** or what the **user explicitly
chose** (pin / allow):

| Source | This slice | Image / later |
|--------|------------|----------------|
| HKCU Run | Audit + optional disable of Review items | Same |
| HKLM Run | Audit (report only) | Admin / image policy |
| Startup folder | Audit + optional disable of Review items | Same |
| Scheduled Tasks | Audit (report-only, readable task files) | `docs/startup-audit-task.xml` registers a daily task |
| Common overlay / bloat names | Classify as Review | Tune the list on the image |

**Never silently kill:** anticheat, GPU vendor services, Vortex, OBS,
or `ProcessGuardian` HardProtect names (including `explorer`). User can
pin/allow anything.

**This UnboundOS slice:** in-app audit, persisted allowlist
(`startup-allowlist.json`), Settings report, Apply recommended (user
Run + Startup folder only).

**Image:** import `docs/startup-audit-task.xml` (or equivalent) so a
daily task can launch UnboundOS with `/audit-startup` later. A full
Windows service is **not** in this pass.

## 3. File explorer

We **will** ship our own clean, intuitive explorer.

**This UnboundOS slice:** **Files** page — browse Home, Desktop,
Downloads, and the drive list, with Unbound styling. Open a folder
inside Unbound Files; **Show in Explorer** stays one click away.

**Do not (this pass or any pass that ships games):**

- Replace Windows Explorer
- Set `Shell=`
- Hook File Explorer for game launches

**Honest copy:** Unbound Files is the daily file UI. Explorer remains
for compatibility (EAC / BE / Vanguard / anything that expects the NT
shell).

**Image / later:** richer columns, copy/move, archive tools, search.
Still not a Shell replacement.

## 4. Display settings

Do **not** invent a display stack.

**This UnboundOS slice:** Settings → **Display** discovers and **launches**
the GPU vendor app if present:

- NVIDIA App / NVIDIA Control Panel
- AMD Adrenalin / Radeon Software
- Intel Arc Control (when the path is clean)

Missing tools show **Get** (official HTTPS). Launch only.

## 5. Overclocking

**This UnboundOS slice:** Settings → **Overclocking** is a hub that
discovers and launches manufacturer tools:

- NVIDIA App (OC / Fine-tune UI)
- AMD Adrenalin / WattMan
- AMD Ryzen Master
- Intel XTU / Extreme Tuning

**Launch only.** UnboundOS must **not** write GPU or CPU clocks
(unsafe). Honest empty state if none are installed.

## 6. Hardware

A clean in-depth hardware view in the **HWiNFO class**: sensors, temps,
clocks, RAM, disks, GPU — as much as we can read via **WMI**,
**LibreHardwareMonitor**, or similar. Do **not** bundle pirated HWiNFO.

**This UnboundOS slice:** **Hardware** page — inventory from this PC
(registry CPU/GPU/BIOS, DriveInfo disks, GC memory). Live sensor graphs,
WMI depth, and LibreHardwareMonitor are **later**. Tools catalog:
optional **Open HWiNFO** if installed (official Get otherwise).

## What this UnboundOS PR must keep

- Brand restyle, Vortex read-only, Tools marketplace
- Settings motion toggle, restrained buttons, cyberpunk hint
- No ads, no silent overclock, x64

## Related

- Overlay addon (optional, off): `docs/overlay-addon.md`
- Daily startup task stub: `docs/startup-audit-task.xml`
