# UnboundOS product spec (OS-level)

Source of truth for the Unbound Infotech **UnboundOS** shell and the
WinUnbound **Windows image / OOBE** work that sits under it.

UnboundOS is a **console-style session shell on Windows**. NT, drivers,
DirectX, Store, and Steam stay Microsoft. **Opt-in** per-user
`HKCU` `Shell=` (watchdog) can make UnboundOS the sign-in UI. The
**lead** path is Windows 11 Xbox mode / FSE home
([xbox-mode-home.md](xbox-mode-home.md)). Default install still leaves
Explorer as the shell. See [os-replacement-plan.md](os-replacement-plan.md).
Anticheat in FSE or Shell= mode is **unverified**.

This document is the full spec. The current UnboundOS PR ships a **first
slice** only (called out per section). Image/OOBE owners pick up the rest.

Brand stays Unbound Infotech first: obsidian, cyan pulse, cobalt, spare
circuit amber on **inner pages**. Home is a separate bay — a black
studio field with Super Clean left-stack labels. Restrained professional
buttons. No ads. No silent overclock. Treat **x64** as the WinUI
platform.

Home navigation is a **left-label stack** on black (WebView2 hosting
HTML, not Unreal). Labels top to bottom: Games, Tools, Options, Mods,
Network, Files, Hardware. **Up/Down** (D-pad, stick, arrows, wheel)
move the focused label; the focus name enlarges in Diavlo and pushes
neighbors. **Enter** opens an all-black titles-only list of that
group’s real destinations. Escape returns to the labels. Quiet
**movable** leftover plaques stay opt-in (Settings → Home extras, off
by default). Architecture: [docs/home-nav.md](home-nav.md). Offline
NIC pack: [docs/offline-nic-pack.md](offline-nic-pack.md). Main-OS
install on a blank NVMe: [docs/install-main-os.md](install-main-os.md).
QA loop: [docs/test-checklist.md](test-checklist.md).

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

**This pass:** Files can copy, move, delete, open-with, and eject.
Desktop mode still starts `explorer.exe`. `Shell=` is opt-in HKCU only.

**Do not:**

- Write HKLM Winlogon Shell
- Rename or replace `explorer.exe`
- Hook File Explorer for game launches
- Claim EAC / Vanguard work in replacement mode without the checklist

**Honest copy:** [os-replacement-plan.md](os-replacement-plan.md).

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
Rainmeter is an optional overlay addon (off by default; discover /
Open / protect; no config rewrite). MusicBee + official free
visualizer pack, plus Microsoft Store and Xbox URI tiles.

## What this UnboundOS PR must keep

- Brand restyle, Vortex read-only, Tools marketplace
- Settings motion toggle, restrained buttons, cyberpunk hint
- No ads, no silent overclock, x64

## Related

- Overlay addon (optional, off): `docs/overlay-addon.md`
- Daily startup task stub: `docs/startup-audit-task.xml`
- Competitive landscape: `docs/competitive-landscape.md`
- Session skinny: `docs/gaming-skinny.md`
- Theme tokens: `docs/theme-tokens.md`
- Offline NIC pack: `docs/offline-nic-pack.md`
- Update Guard (quality yes / feature deferred): `docs/update-guard.md`
