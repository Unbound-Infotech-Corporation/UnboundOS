# UnboundOS

**Cut the noise. Own the frame.**

A Windows 11 gaming session shell by **Unbound Infotech Corporation**.

UnboundOS does not replace Windows. It puts Windows into a focused gaming / streaming posture — then restores your desktop when you exit.

## What it does

| Module | Purpose |
|--------|---------|
| **Session Engine** | Enter a profile: snapshot NIC metrics, terminate denylist background apps, protect games/anticheat |
| **Network Director** | Prefer a game NIC (low metric) and park stream/bulk traffic on a second NIC |
| **Stream Canvas** | Ultrawide → center 16:9 → Twitch 1080p60 crop math + OBS filter guide (default 5110×1400) |
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

## Build & run

```powershell
cd C:\Users\akind\gAMEos\gAMEos
$env:Path = "C:\Program Files\dotnet;" + $env:Path
dotnet build UnboundOS.sln -c Debug -p:Platform=x64
dotnet run --project src\UnboundOS.App\UnboundOS.App.csproj -c Debug -p:Platform=x64
```

> NIC metric changes via `netsh` work best when UnboundOS is run elevated. Process cleanup works without elevation for most user-level apps.

## Default profiles

Created on first launch at:

`%LocalAppData%\Unbound Infotech Corporation\UnboundOS\profiles.json`

1. **Competitive Edge** — aggressive denylist, anticheat protect list  
2. **Streamer Split** — gentler cleanup + 5110×1400 → 1920×1080 stream plan  
3. **Living Room Shell** — calm big-picture focus  

## Mods + Steam Workshop

The **Mods + Workshop** page discovers local content without signing into Steam or reading
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

### Safety and limitations

Steam remains responsible for Workshop subscription, download, update, and removal.
UnboundOS deliberately does **not** alter Workshop folders, impersonate the Steam client,
scrape credentials, or claim that one generic enable/load-order method works for every game.

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

`SteamWorkshopReadOnlyAdapter` is the safe fallback for every game without a dedicated adapter.

## Design notes

- Brand-first shell UI (signal lime on ink — not generic purple)
- CommunityToolkit.Mvvm + Microsoft.Extensions.DependencyInjection
- Safe process guardian with hard-protect for critical Windows processes
- Stream pipeline never asks you to lower monitor resolution

## Roadmap (next)

- Custom shell / Assigned Access gaming user switch
- Per-game profile editor UI
- Virtual 16:9 capture output
- One-click OBS scene import
- Bufferbloat / QoS helper hooks
- First-party adapters for selected games with documented mod configuration formats

---

© Unbound Infotech Corporation
