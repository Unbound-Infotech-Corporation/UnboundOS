# UnboundOS

**Cut the noise. Own the frame.**

A public Windows 11 gaming session shell by **[Unbound Infotech Corporation](https://unboundinfotech.com)**.

UnboundOS does **not** replace Windows. It applies a focused gaming / streaming posture — snapshot NICs, clear denylist junk, protect games and anticheat — then restores the desktop when you exit.

**Screenshot placeholder:** add `docs/screenshots/shell.png` after a local Windows run (obsidian `#05070A`, steel `#0B121D`, cyan pulse `#00F0FF`). See [docs/screenshots/README.md](docs/screenshots/README.md).

## What it is

A WinUI 3 + MVVM shell on top of Windows. Session, network, and process logic stay in the existing engines. A later Rainmeter-style desktop overlay can plug in without forking those engines — see [docs/overlay-addon.md](docs/overlay-addon.md). The overlay is **optional and off**.

## What it does

| Module | Purpose |
|--------|---------|
| **Session Engine** | Enter a profile: snapshot NIC metrics, terminate denylist background apps, protect games/anticheat |
| **Network Director** | Prefer a game NIC (low metric) and park stream/bulk traffic on a second NIC |
| **Tools** | Small local kit marketplace: detect OBS, Vortex, Discord, Playnite, Steam; Launch or official Get. Ultrawide crop recipe lives on the OBS tile |
| **Profiles** | JSON profiles in LocalAppData (`Competitive`, `Streamer`, `Living Room`) |
| **Mods + Workshop** | Local Steam Workshop discovery, per-game mod profiles, safe adapter-based apply/restore |
| **Telemetry** | Live CPU / memory / process / suspect counts in the shell header |

## Solution layout

```
UnboundOS.sln
└─ src/
   ├─ UnboundOS.App/             WinUI 3 shell (MVVM)
   ├─ UnboundOS.Core/            Models + contracts + crop math
   └─ UnboundOS.Infrastructure/  Windows process/network/session services
```

## Requirements

- Windows 11 (or Windows 10 1809+)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows App SDK runtime (pulled via NuGet on build)
- Treat **x64** as the WinUI platform

## Build & run

```powershell
dotnet build UnboundOS.sln -c Debug -p:Platform=x64
dotnet test UnboundOS.sln
dotnet run --project src\UnboundOS.App\UnboundOS.App.csproj -c Debug -p:Platform=x64
```

> NIC metric changes via `netsh` work best when UnboundOS is run elevated. Process cleanup works without elevation for most user-level apps.

## Default profiles

Created on first launch at:

`%LocalAppData%\Unbound Infotech Corporation\UnboundOS\profiles.json`

1. **Competitive Edge** — aggressive denylist, anticheat protect list
2. **Streamer Split** — gentler cleanup + 5110×1400 → 1920×1080 stream plan
3. **Living Room Shell** — calm big-picture focus

## Tools marketplace

The **Tools** page is a local kit catalog, not a store and not a streaming product.

- Detects OBS Studio, Vortex, Discord, Playnite, and Steam from install paths, Start Menu, and uninstall registry.
- **Launch** starts the real app. The Vortex tile uses the same read-only Vortex handoff as Mods (`--game` / `--profile` when known; opening Vortex itself from Tools).
- **Get** opens an official HTTPS page (or a `ms-windows-store` / `winget` URI). UnboundOS does not download or bundle those binaries.
- If OBS is missing, the tile says Get. The ultrawide crop recipe still explains the filters and that they need OBS. Play native — never lower monitor resolution.
- Streamer protects OBS, Discord, Vortex, and Steam. Competitive can still terminate Discord. Those lists are not merged.

## Mods + Steam Workshop + Vortex

The **Mods + Workshop + Vortex** page discovers local content without signing into Steam or Nexus or reading
credentials. It reads Steam's local `libraryfolders.vdf`, `appmanifest_*.acf`, and
`appworkshop_*.acf` files, then inspects numeric folders under
`steamapps\workshop\content\<app-id>`.

Current behavior:

- Finds Workshop-enabled games across configured Steam library folders.
- Shows locally installed item IDs, paths, size/update metadata, enabled draft state,
  dependencies/conflicts, update state, and load order.
- Saves named per-game mod profiles to
  `%LocalAppData%\Unbound Infotech Corporation\UnboundOS\mod-profiles.json`.
- Opens game Workshop and item pages with supported `steam://` URLs.
- Provides a clearly labeled read-only preview when no Workshop content is installed.
- Discovers Vortex-managed Nexus games from `%APPDATA%\Vortex` and known staging roots
  (`F:\Vortex Mods`, `F:\vmods`, plus paths Vortex records via `--get` only when Vortex is not running).
- Offers **Open in Vortex** (`--game` / `--profile`). Vortex stays the source of truth.
- Protects the `Vortex` process on default session profiles and in the hard-protect list so Competitive cleanup cannot kill it.

### Safety and limitations

Steam remains responsible for Workshop subscription, download, update, and removal.
Vortex remains responsible for Nexus install, enable, deploy, and profiles.
UnboundOS deliberately does **not** alter Workshop folders, impersonate the Steam client,
scrape Nexus credentials, write Vortex `state.v2`, mutate staging folders, or use
Vortex `--set` / `--del` / `--restore` / `--merge`.

Profile toggles and ordering are safe drafts until a game-specific adapter reports support.
Unsupported **Apply** operations are disabled and explained in the UI. Every writable adapter
must inherit the transactional adapter foundation (or provide equivalent guarantees):

1. Identify only the documented configuration files for that game.
2. Back them up under
   `%LocalAppData%\Unbound Infotech Corporation\UnboundOS\ModBackups`.
3. Apply configuration atomically.
4. Restore the backup automatically if applying fails.

### Adding a game adapter

Implement `IGameModAdapter` in `UnboundOS.Infrastructure`:

- `CanHandle` must narrowly identify the game/provider.
- `GetCapabilities` must state exactly what is supported (`CanToggle`, `CanReorder`, etc.).
- Prefer deriving from `TransactionalFileModAdapter`; implement
  `GetConfigurationPaths` and `ApplyCoreAsync` for the game's documented format.
- Register the adapter as `IGameModAdapter` in `DependencyInjection.cs`.
- Never edit Workshop payload directories to simulate disabling a mod.

`SteamWorkshopReadOnlyAdapter` is the safe fallback for every Workshop game without a dedicated adapter.
`VortexReadOnlyAdapter` only hands off to Vortex — it never applies UnboundOS drafts to staging.

## Brand

Shell UI matches [unboundinfotech.com](https://unboundinfotech.com):

| Token | Value |
|-------|--------|
| Obsidian background | `#05070A` |
| Steel panels | `#0B121D` |
| Cyan pulse (primary) | `#00F0FF` |
| Cobalt | `#1E40AF` |
| UI type | Inter (bundled Latin subset) |
| Telemetry type | JetBrains Mono (bundled Latin subset) |

Fonts ship as Content under `src/UnboundOS.App/Assets/Fonts` (SIL OFL). If a file fails to load, Windows falls back to Segoe UI Variable / Cascadia Mono. See `Assets/Fonts/README.md`.

## Design notes

- CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection
- Safe process guardian with hard-protect for critical Windows processes
- OBS crop recipe never asks you to lower monitor resolution
- Tools marketplace does not sell apps, bundle installers, or scrape accounts
- No ads. Overlay / skins stay a later optional layer

## Roadmap (next)

- Optional desktop overlay / skins addon (contract only today)
- Custom shell / Assigned Access gaming user switch
- Per-game profile editor UI
- Virtual 16:9 capture output (OBS recipe already copies crop/downscale/sharpen)
- One-click OBS scene import
- Bufferbloat / QoS helper hooks
- First-party adapters for selected games with documented mod configuration formats

---

© Unbound Infotech Corporation · [unboundinfotech.com](https://unboundinfotech.com)
