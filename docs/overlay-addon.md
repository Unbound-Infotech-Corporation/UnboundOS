# Desktop overlay / skins addon (optional)

UnboundOS is a **session shell**, not a desktop replacement. A later
Rainmeter-style overlay can draw widgets on the desktop **without forking**
the session, network, or process engines.

This hook is **off by default**. No overlay is built in this tree.

## Contract

```
UnboundOS.Core.Overlay.IDesktopOverlayHost
UnboundOS.Core.Overlay.OverlayWidgetDescriptor
UnboundOS.Core.Overlay.OverlayHostOptions
```

Default implementation: `DisabledDesktopOverlayHost` (no-op, `IsEnabled = false`).

The shell:

- Registers the host with `TryAddSingleton`, so an addon can register first.
- Starts/stops the host from the app window only when `IsEnabled` is true.
- Shows an Overlay nav item and stub page only when `IsEnabled` is true.

`SessionEngine`, `NetworkDirector`, and `ProcessGuardian` do **not**
reference this contract.

## Adding an addon later

1. Implement `IDesktopOverlayHost` in a separate project (skins, widgets, Emergent visuals).
2. Register it **before** `AddUnboundOs()`:

```csharp
services.AddSingleton<IDesktopOverlayHost, EmergentSkinOverlayHost>();
services.AddUnboundOs();
```

3. Keep widget rendering in the addon. Read telemetry through existing
   `ITelemetryService` if needed — do not take a dependency on session enter/exit
   internals.
4. Leave `IsEnabled` false unless the user opts in.

## Starter Rainmeter skin

Recommended / default Get-Open over galaxy Home is **Phenix**, not a
clock-only skin:

- Rainmeter (host): [https://www.rainmeter.net/](https://www.rainmeter.net/)
- Phenix theme: [https://visualskins.com/skin/phenix](https://visualskins.com/skin/phenix)

Tools → **Rainmeter** Opens the installed host or Gets the official
Rainmeter page. Tools → **Phenix** Gets the official Phenix page.
Link / Get / Open only. **Do not** redistribute the `.rmskin` in this
tree unless the license clearly allows it (it is not bundled today).

Home already ships first-party **movable** clock / temp / load widgets
(`IHomeWidgetCatalog` / `IHomeWidgetSource`). That is shell chrome on
the galaxy, not this desktop overlay. An addon can append extra Home
widgets through `IHomeWidgetSource` without enabling this host.

## What not to do

- Do not start the overlay from `SessionEngine`.
- Do not scrape Steam credentials or edit Workshop payload folders.
- Do not lower monitor resolution from overlay tools.
