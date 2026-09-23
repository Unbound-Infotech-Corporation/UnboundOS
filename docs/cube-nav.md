# Home navigation galaxy

UnboundOS Home is an **original procedural galaxy** on a deep void —
not a static photograph, not a spiral sticker, and not a 3D cube as
the hero. The **horizontal OS bar** is slightly pitched toward the
camera so nodes read larger, still clearly a nav axis. A JWST-class
deep field (tiny pinpricks, copper/gold/lavender filaments, spiked
foreground stars) fills the frame behind it and must never compete
with node readability. Observatory photos are look-dev only and are
**not** loaded at runtime. Quiet star systems along the disk are the
menu sites. The WinUI 3 shell stays the product. Category lists ease
in over the living galaxy so the field stays visible.

## Art direction

**Procedural living galaxy.** Runtime Home is handcrafted in Three.js:
a restrained tilted OS band with **dramatic in-band nebula** (deep
blues / violets / rose dust, a quiet winding energy trail) plus a
starfield that leaves **black breathing room** above and below so
systems and HUD stay clean. This is not a full-bleed Wallpaper Engine
fill. Camera sits back (telephoto) with a slight disk pitch/roll.
**Every star moves a little** — differential rotation, orbital shear,
and slow nebula drift. Rare **shooting stars** and rarer **supernova**
flashes can cross the field; Motion-off freezes drift and those
events. Never a locked plate. No bloom spam, no noisy particle spray.

`Assets/Cube/home-plate.png` may remain as a look-dev reference. Flux
stills, JWST-class imagery, and a live Wallpaper Engine desktop may
inform volume and color offline. **None of those plates or workshop
files are loaded or shipped.** **No NASA or observatory credit is
claimed**; do not invent one.

Games sits on the bright core. Tools and Mods lock to the right-hand
arm; Network, Files, and Hardware lock to the left. Category sites
are **quiet star systems** — a small sun, orbiting planets, a modest
local cluster — not bright UI hot-dots. At rest the bar still reads
as one galaxy. Focus only brightens that system gently. **Only the
focused system** shows a label — large, high-contrast, screen-aligned
type that does not follow the disk tilt. Other systems stay unlabeled.
A **very faint black-hole suggestion** sits far in the upper-left
void (soft silhouette + dim accretion, not a cartoon). Company cyan
(`#00F0FF`) stays a brand token for **inner pages**.

Opening a category (Games, Tools, Options, …) **zooms the camera into
that system's sun** and parks a slow-rotating **procedural photosphere**
on the **right**. Each category has its own star (gold 171-like Options,
continuum-orange Games, rose-gold Tools, and so on): multi-scale
granulation, limb darkening, faculae, compact spots, a filament corona
sheet with loops and prominences. Motion-off freezes the crawl and
bursts. The universe stays visible around it. A **titles-only**
Diavlo list sits on the **left** over a reasonably transparent black
wash — not an opaque slab. Options is a dedicated sun just beyond
Mods (not on the L/R cycle); the Home **SET** glyph opens it instead
of jumping to a flat Settings window. Enter on an Options row still
lands on the native Settings page. Up/Down still choose list origin
(bottom/top focus); Left/Right still change nodes. The camera ease
and list fade respect Motion Settings (instant/cut when off). Escape
zooms back to the quiet galaxy.

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

Settings (`SET`) zooms to Options over the galaxy. Profiles (`PRFL`)
stays a discreet Home corner glyph.

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
                        JSON: state / open / focus / close / reset
                              ready / turn / pick / dragEnd / activate /
                              opened / cycle / select / back
                        open.origin = "top" | "bottom"
                        open.front = "Settings" + node = -1 → Options sun
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + Three.js r158.** Original procedural galaxy at runtime.
  `home-plate.png` is look-dev only.
- If WebView2 or WebGL is missing, Enter still opens the WinUI page.
- Quiet Home HUD (movable clock / temps / load) and SET / PRFL glyphs stay optional.

Look-dev: `Assets/Cube/index.html?preview=1`.
Options zoom: `?preview=1&open=options` (KeyO in the browser preview).
Games / Tools: `?preview=1&open=games` or `?open=tools`.
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
0.48) so plaques rest beside the zoomed sun instead of the titles
list. Widgets stay visible while a list is open and **dim** (layer
opacity) so they do not fight the sun or the list. Master visibility
is Settings → Home HUD.

List type is **Diavlo** (Jos Buivenga / exljbris), bundled under
`Assets/Fonts` for embedding in the shell only. See
`src/UnboundOS.App/Assets/Fonts/README.md` and `LICENSE-Diavlo.txt`.
Do not redistribute the font files as a standalone download.

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
