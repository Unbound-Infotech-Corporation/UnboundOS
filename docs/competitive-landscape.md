# UnboundOS vs the Windows-lite field

Legal path only: **stock Windows + this shell/playbook**. UnboundOS does
not ship a pirate ISO, a custom `install.wim`, or Ghost Spectre-class
images.

## What they claim

| Player | Edge | Gap Unbound fills |
|--------|------|-------------------|
| **AtlasOS** | Auditable playbook, optional security toggles | No living-room console HUD; not a session engine |
| **ReviOS** | Aggressive gaming strip | Higher breakage; overlays/anticheat risk |
| **Tiny11** | Small footprint | Not a gaming tuner; Wi-Fi often missing on fresh boxes |
| **Ghost Spectre / random ISOs** | “FPS packs” | Supply-chain and ToS risk. We will not follow |
| **SteamOS / Gamescope** | Boot-to-games UX | Kernel anticheat (Valorant/Fortnite) fails. We stay Windows-native |
| **Xbox full-screen** | Living-room dashboard | Still a full desktop; Unbound is branded (HUD, Tools, Mods, Hardware) |

## What we ship that they do not

1. **Console shell on genuine Windows** — first-party Gik0n-like HUD
   (clock, date, weather stub, viz, media, launcher) plus teal-tip
   tabs. No Rainmeter required. Theme tokens in
   [theme-tokens.md](theme-tokens.md).
2. **Anticheat-safe Competitive session** — strip overlays (Rainmeter
   etc.); **never** replace Explorer or set `Shell=` for EAC/Vanguard.
3. **Reversible session profiles** — Living Room / Streamer /
   Competitive. Game Mode, Ultimate Performance while live, Game DVR
   off in Competitive, visual-effects Performance. Exit restores.
   Defender / Update / VBS stay on. [Update Guard](update-guard.md)
   keeps monthly Microsoft quality/LCU on and defers feature churn —
   we do not host `.msu` files.
4. **HAGS as an informed toggle** — Options → Session skinny. Test
   frametimes. Not forced on enter.
5. **Startup / AppX discipline** — 24h startup audit + pin allowlist.
   Store-reinstallable trim only. See [os-spec.md](os-spec.md).
6. **Get-online OOBE** — offline NIC pack ≤ 5 GB, deleted after
   `online-ok.flag`. [offline-nic-pack.md](offline-nic-pack.md).
7. **Tools + Hardware truth** — OBS/Discord/Playnite/Vortex/Store/Xbox
   marketplace; vendor display/OC launch; HWiNFO-class inventory.

## What we refuse

- Placebo registry megapacks
- HPET / timer resolution / BCDEdit hacks
- Irreversible Defender or VBS nukes
- Hosting Microsoft LCU binaries or claiming CVE-only patches
- Global NetworkThrottlingIndex / interrupt-moderation as a default
- Shipping a modified Windows ISO

Skinny toggle reference: [gaming-skinny.md](gaming-skinny.md).
Update Guard: [update-guard.md](update-guard.md).
