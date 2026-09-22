# Home navigation galaxy

UnboundOS Home is an **original procedural galaxy** on a deep void —
not a static photograph, not a spiral sticker, and not a 3D cube as
the hero. The **horizontal OS bar** is slightly pitched toward the
camera so nodes read larger, still clearly a nav axis. A JWST-class
deep field (tiny pinpricks, copper/gold/lavender filaments, spiked
foreground stars) fills the frame behind it and must never compete
with node readability. Observatory photos are look-dev only and are
**not** loaded at runtime. Bright clusters along the disk are the
menu nodes. The WinUI 3 shell stays the product. Category lists ease
in over the living galaxy so the field stays visible.

## Art direction

**Procedural living galaxy.** Runtime Home is handcrafted in Three.js:
a canvas-painted luminous bar (creamy core, dusty copper lanes, cooler
indigo arms) plus a full-frame deep field, halo / disk / shear
starfields, and slow filament drift. Camera sits back (telephoto) with
a slight disk pitch/roll. **Every star moves a little** — differential
rotation, orbital shear, and slow parallax. Motion-off freezes that
drift. Never a locked plate with a few twinkles.

`Assets/Cube/home-plate.png` may remain as a look-dev reference. JWST
imagery may inform color and density offline. Neither is the runtime
wallpaper. **No NASA or observatory credit is claimed**; do not invent
one.

Games sits on the bright core. Tools and Mods lock to the right-hand
clusters; Network, Files, and Hardware lock to the left-hand arm.
Nodes stay on the horizontal. Focused node: a quiet star-select
(soft core lift, slight local dust, no neon rings). **Only the
focused node** shows a label — large, high-contrast, screen-aligned
type that does not follow the disk tilt. Other nodes stay unlabeled.
Company cyan (`#00F0FF`) stays a brand token for **inner pages**.

Category lists sit on the **left half** of the screen over a
translucent scrim. The scrim **vanishes completely** when Escape
collapses back to the node bar.

Motion-off (or a live session) freezes star drift and heavy postFX;
lists still open (instant or a short fade). The previous control-altar
cube is not the Home hero.

## Keys

| Input | Idle galaxy | Category list |
|-------|-------------|---------------|
| **Left / Right** | Shift focus along the disk (nodes stay on the bar) | — |
| **Up** | Open **this node’s** list, selection starts at the **bottom** | Move up the list |
| **Down** | Open **this node’s** list, selection starts at the **top** | Move down the list |
| **Enter** | Open this node’s list from the top | Launch / open the focused row |
| **Escape** | — | Collapse back to the galaxy |

Settings (`SET`) and Profiles (`PRFL`) stay discreet Home corner glyphs.

| Node | List |
|------|------|
| Games | Steam library (`steam://rungameid`) via `IGameLibraryCatalog` / `ILibraryLaunchService`. Empty library pads with Session engine + Steam/Playnite. |
| Tools | Desktop kit catalog (including Rainmeter, MusicBee, Store, Xbox). |
| Mods | Workshop / Vortex discovery. |
| Network, Files, Hardware | Honest page rows over the galaxy; Enter lands on the native WinUI list. |

SessionEngine is not rewritten. The overlay is HTML inside the WebView
so WinUI-over-HWND airspace does not hide the galaxy. List motion uses
console-grade curves (`cubic-bezier(0.16, 1, 0.3, 1)`).

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard, launch)
                 │
                 ├─ C#  HomeGalaxy / CubePose / CubeCatalog / CubeBrowse
                 │      owns node focus, list origin (top/bottom), page nav
                 │
                 └─ WebView2  →  packaged Three.js scene (Assets/Cube)
                        JSON: state / open / focus / reset
                              ready / turn / pick / dragEnd / activate /
                              opened / cycle / select / back
                        open.origin = "top" | "bottom"
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + Three.js r158.** Original procedural galaxy at runtime.
  `home-plate.png` is look-dev only.
- If WebView2 or WebGL is missing, Enter still opens the WinUI page.
- Quiet Home HUD (movable clock / temps / load) and SET / PRFL glyphs stay optional.

Look-dev: `Assets/Cube/index.html?preview=1`.
`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture.

## Home widgets

Light-grey **movable** plaques for honest basics: clock, CPU temp, GPU
temp, package temp when it differs from CPU, and CPU load when the OS
counter exists (`CpuUsageAvailable` — a missing counter is `—`, not a
fake 0%). No invented sensors.

Drag to reposition. Positions and visibility persist in `settings.json`
(`homeWidgetPlacements`, `homeWidgetAppearance`). Looks: **glass**
(default light grey), **dim**, **compact** — not rainbow skins.
Motion-off still lets you drag; no fancy drag animation is required.

Default stack sits on the **right** (`X = 0.84`, past `ListKeepoutX`
0.48) so plaques stay clear of the left-half category list. Widgets
stay visible while a list is open and **dim** (layer opacity) so they
do not fight the scrim. Master visibility is Settings → Home HUD.

Custom widgets later: implement `IHomeWidgetSource` and register it
before `AddUnboundOs()`. First-party catalog is
`IHomeWidgetCatalog`. This is not a Rainmeter clone. **Rainmeter** is
the customizable overlay path over this galaxy (Tools → Rainmeter /
Phenix / Visualizer pack). The in-tree host is
`RainmeterDesktopOverlayHost`, on unless `OverlayHostOptions.Enabled`
is set false. Overlay nav Opens Rainmeter and Gets Phenix.
See [overlay-addon.md](overlay-addon.md). Recommended theme is
**Phenix**; recommended clock skin is **Minimalistic Clock**
(Get the official pages; do not ship the `.rmskin`).
