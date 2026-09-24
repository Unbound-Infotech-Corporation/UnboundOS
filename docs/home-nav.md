# Home navigation (Super Clean)

UnboundOS Home is a **super clean black field** with **category labels
stacked on the left**. No galaxy, no nebulas, no suns, no star-system
nodes, no teal-tip tab bar, no Rainmeter HUD. Negative space is the
point.

The WinUI 3 shell stays the product. Selecting a focused label opens
an **all-black** titles-only list of that group’s real destinations.
Home is a full-bleed Obsidian / black shell — not opaque WinUI chrome.

## Art direction

**Super Clean.** Runtime Home is HTML/CSS in the WebView (Three.js
stays vendored, unused):

- Background is Obsidian `#05070A`. Empty studio floor. Calm.
- Seven labels sit in a left stack. Unfocused names stay small and
  quiet (`#5C646C`).
- The **focused** label enlarges in **Diavlo Medium** (`#F7FAFC`) with
  a 2px cyan `#00F0FF` hairline. Neighbors push apart on the same
  stack so the larger type has room — shared motion, not floating
  chips.
- Motion-off freezes enlarge/push; focus still snaps.

Labels, top to bottom: Games, Tools, Options, Mods, Network, Files,
Hardware. Games is the default. Options is a first-class label (also
opened by the Home **SET** glyph). Profiles (`PRFL`) stays a discreet
corner glyph. Store / Xbox stay under Tools.

Opening a category (**Enter** / click the focused label) shows a
**titles-only** Diavlo list on a new all-black surface. Escape eases
back to the label stack. Motion-off snaps focus and list states.

Company cyan `#00F0FF` is the focus hairline, list focus, and the
inner-page token. It is not a full-screen wash.

## Keys

| Input | Idle Home | Category list |
|-------|-----------|---------------|
| **Up / Down** | Move the focused label (D-pad, stick, arrows, mouse wheel) | Move the list |
| **Left / Right** | Same as Up / Down (label list) | — |
| **Enter** | Open this label’s all-black options surface | Launch / open the focused row |
| **Escape** | — | Return to the left-label Home |

Settings (`SET`) focuses the Options label and opens its list. Enter on
an Options row still lands on the native Settings page.

| Label | List |
|-------|------|
| Games | Steam library (`steam://rungameid`) via `IGameLibraryCatalog` / `ILibraryLaunchService`. Empty library pads with Session engine + Steam/Playnite. |
| Tools | Desktop kit catalog (including optional Rainmeter, MusicBee, Store, Xbox). |
| Mods | Workshop / Vortex discovery. |
| Options | Eight settings rows (`kind: settings`), including Updates / Update Guard. Enter → Settings page. |
| Network, Files, Hardware | Honest page rows over Home; Enter lands on the native WinUI list. |

SessionEngine is not rewritten. The overlay is HTML inside the WebView
so WinUI-over-HWND airspace does not hide Home. List motion uses
console-grade curves (`cubic-bezier(0.16, 1, 0.3, 1)`).

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard, launch)
                 │
                 ├─ C#  HomeGalaxy / CubePose / CubeCatalog / CubeBrowse
                 │      owns label focus (incl. Options), list origin, page nav
                 │
                 └─ WebView2  →  packaged HTML Super Clean Home (Assets/Cube)
                        JSON: state / open / focus / close / reset
                              ready / turn / pick / activate /
                              opened / cycle / select / back
                        open.front = "Settings" + node = -1 → Options
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + HTML Super Clean Home.** `three.min.js` and leftover
  look-dev plates remain unused. No WebGL required for Home.
- If WebView2 is missing, Enter still opens the WinUI page.
- SET / PRFL glyphs stay discreet. Leftover movable plaques are **off**
  by default (Settings → Home extras).

Look-dev: `Assets/Cube/index.html?preview=1`.
Options: `?preview=1&open=options` (KeyO in the browser preview).
Games / Tools: `?preview=1&open=games` or `?open=tools`.

## Rainmeter (optional, off)

Rainmeter is **not** required and is **not** part of this theme. The
overlay host is off by default (`OverlayHostOptions.Enabled = false`).
See [overlay-addon.md](overlay-addon.md). Home stays a black full-bleed
WebView so optional skins can still sit on top if a user turns the
addon on later.

## Home extras

The Super Clean label stack is the default theme. Light-grey **movable**
temp/load plaques remain as opt-in extras (`X = 0.84`). Master
visibility is Settings → Home extras (off unless the user turns it on).

List type is **Diavlo** (Jos Buivenga / exljbris), bundled under
`Assets/Fonts`. See `src/UnboundOS.App/Assets/Fonts/README.md` and
`LICENSE-Diavlo.txt`. Do not redistribute the font files as a
standalone download.
