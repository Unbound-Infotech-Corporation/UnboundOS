# Desktop overlay / skins addon (optional)

UnboundOS is a **session shell**, not a desktop replacement. Home is a
**black full-bleed studio field** with vertical tabs **and a packaged
first-party HUD** (clock, date, viz, media strip, launcher, calendar).
That HUD is the default theme. It does **not** require Rainmeter.

**Rainmeter** remains an **optional** overlay path for users who want
their own skins on top of Home. First-party movable temp plaques
(`IHomeWidgetCatalog`) stay as extras.

This host is **on by default** (`OverlayHostOptions.Enabled = true`).
The Overlay nav item is the Rainmeter surface: Open Rainmeter, Get Phenix.
Set `Enabled = false` to hide it. The app window starts/stops the host;
`SessionEngine` never does.

Home must stay a transparent / black WebView shell so skins remain
visible. Do not paint opaque WinUI chrome across Home.

## Contract

```
UnboundOS.Core.Overlay.IDesktopOverlayHost
UnboundOS.Core.Overlay.OverlayWidgetDescriptor
UnboundOS.Core.Overlay.OverlayHostOptions
```

Default implementation: `RainmeterDesktopOverlayHost` (`IsEnabled` follows
`OverlayHostOptions`). `DisabledDesktopOverlayHost` remains a no-op for
tests and addons that want an inert host.

The shell:

- Registers Rainmeter as `IDesktopOverlayHost` with `TryAddSingleton`,
  so an addon can register first and win.
- Starts/stops the host from the app window only when `IsEnabled` is true.
- Shows an Overlay nav item (Open Rainmeter / Get Phenix) when `IsEnabled` is true.
- **Opens** `Rainmeter.exe` with empty arguments. It never writes
  `rainmeter.ini`, never sends Rainmeter bangs, and never rewrites skins.

`SessionEngine`, `NetworkDirector`, and `ProcessGuardian` do **not**
reference this contract.

## Vortex-like handoff

Same pattern as Vortex: discover the install, Open the real app, leave
its state alone.

1. Tools → **Rainmeter** — Open launches `Rainmeter.exe` if found
   (Program Files, uninstall registry, Start Menu). Get is
   [https://www.rainmeter.net/](https://www.rainmeter.net/).
2. Tools / Overlay → **Phenix** — recommended starter **theme** over Home.
   Open or Get [https://visualskins.com/skin/phenix](https://visualskins.com/skin/phenix).
3. Tools → **Minimalistic Clock** — recommended **clock** skin over Home.
   Get [https://visualskins.com/skin/minimalistic-clock](https://visualskins.com/skin/minimalistic-clock).
4. Rainmeter stays in charge of which skins load and where they sit.
5. Streamer and Living Room **protect** `Rainmeter`. Competitive
   **terminates** it (strip overlays). Rainmeter is not HardProtect.

## How to add skins

1. Install Rainmeter from the official site (Tools → Rainmeter → Get).
2. Get a skin page (Phenix, Minimalistic Clock, Monstercat Visualizer,
   or any license-clear skin). Download the `.rmskin` **from that site**,
   not from this repo.
3. Double-click the `.rmskin`. Rainmeter's installer applies it.
4. In Rainmeter, load / unload / drag skins as usual.
5. Tools → Rainmeter → Open brings the host back if it was closed.

Do **not** drop `.rmskin` files into this tree unless the license
clearly allows redistribution. None are bundled today.

## Starter Rainmeter skin (optional)

The default Home theme is the **packaged Unbound HUD**. Rainmeter is
not required. If you still want third-party skins on top:

- Rainmeter (host): [https://www.rainmeter.net/](https://www.rainmeter.net/)
- Phenix theme: [https://visualskins.com/skin/phenix](https://visualskins.com/skin/phenix)
- Minimalistic Clock: [https://visualskins.com/skin/minimalistic-clock](https://visualskins.com/skin/minimalistic-clock)

Link / Get / Open only.

If Rainmeter is off, use the built-in movable Home widgets (clock,
CPU/GPU/package temps, honest CPU load).

## Music player + visualizer pack

- Tools → **MusicBee** — reputable third-party player. Open if installed;
  Get [https://getmusicbee.com/downloads/](https://getmusicbee.com/downloads/).
- Tools → **Visualizer pack** — official free
  [Monstercat Visualizer](https://github.com/MarcoPixel/Monstercat-Visualizer)
  for Rainmeter. Install Rainmeter first, then that `.rmskin` from GitHub
  Releases. Pair it with MusicBee (or another player) for audio.
- Optional extra free pack (docs only, not a tile):
  [Fountain of Colors](https://github.com/alatsombath/Fountain-of-Colors).

Do not pirate paid visualizer packs. UnboundOS does not ship them.

## Store + Xbox

- Tools → **Microsoft Store** — Open `ms-windows-store://home`.
- Tools → **Xbox** — Open `xbox:`. Get is the official Store product
  page (`ms-windows-store://pdp/?ProductId=9MV0B5HZVK9Z`).

## Disabling the Rainmeter host

```csharp
services.AddSingleton(OverlayHostOptions.Disabled);
services.AddUnboundOs();
```

Or register a different `IDesktopOverlayHost` **before** `AddUnboundOs()`.

To keep widget rendering in an addon, read telemetry through
`ITelemetryService` if needed — do not take a dependency on session
enter/exit internals.

## What not to do

- Do not start the overlay from `SessionEngine`.
- Do not rewrite `rainmeter.ini` or skin configs.
- Do not scrape Steam credentials or edit Workshop payload folders.
- Do not lower monitor resolution from overlay tools.
- Do not replace Home with skins. Skins sit **over** the black tab field.
