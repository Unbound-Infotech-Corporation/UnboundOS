# Home navigation (studio tabs)

UnboundOS Home is a **super clean black field** with a row of **narrow
vertical tabs** along the bottom. No galaxy, no nebulas, no suns, no
star-system nodes. Negative space is the point.

The WinUI 3 shell stays the product. Category lists ease in over the
same Obsidian field. A **packaged first-party HUD** (clock, date,
weather stub, visualizer, media strip, launcher, calendar) sits on
that field on Home **and** every main-menu list. Home is a full-bleed
Obsidian / black shell — not opaque WinUI chrome.

## Art direction

**Machined instruments.** Runtime Home is HTML/CSS in the WebView
(Three.js stays vendored, unused):

- Background is Obsidian `#05070A`. Empty museum floor. Calm.
- Seven blades sit as a compact bottom group in a **shallow arc**
  (center tallest). Hairline bevels, a vertical metal gradient, and a
  single drifting specular so they read machined — not flat bars.
- Each **tip** is a small crystal cap in brand cyan `#00F0FF` (facet
  highlight, restrained glow). No neon flood.
- Focus lifts and slightly scales the blade, deepens the studio
  shadow, and lets the tip breathe. Motion-off freezes breath and
  sheen; focus still snaps.
- Only the **focused** tab shows its name **above** in Diavlo, with a
  designed drop shadow and open tracking. Unfocused tabs stay unlabeled.

Tabs, left to right: Hardware, Files, Network, Games, Options, Tools,
Mods. Games is the default. Options is a first-class tab (also opened
by the Home **SET** glyph). Profiles (`PRFL`) stays a discreet corner
glyph.

Opening a category (Enter / Up / Down) shows a **titles-only** Diavlo
list on the left over a translucent black wash. No camera zoom. Escape
eases back to the tab row. Motion-off snaps focus and list states
(no lift ease).

Company cyan `#00F0FF` is the tab-tip material, HUD accent, and the
inner-page token. It is not a full-screen wash.

## Packaged HUD

Default Home ships a first-party desktop suite — original Unbound UI,
inspired by clean Rainmeter desktops, **not** a Rainmeter install:

- Large thin clock (local time) + date
- Weather-style line (offline stub until a live source exists)
- Vertical cyan visualizer (freezes when Motion is off)
- Slim now-playing strip (binds later; idle copy for now)
- Text launcher: Games / Tools / Options
- Right-side month calendar

Settings → **Home HUD** hides or shows this chrome. `?hud=0` hides it
in the browser preview. The HUD stays up when a category list opens
so Hardware…Mods keep one theme.

## Keys

| Input | Idle Home | Category list |
|-------|-----------|---------------|
| **Left / Right** | Move focus to the next tab | — |
| **Up** | Open **this tab’s** list, selection starts at the **bottom** | Move up the list |
| **Down** | Open **this tab’s** list, selection starts at the **top** | Move down the list |
| **Enter** | Open this tab’s list from the top | Launch / open the focused row |
| **Escape** | — | Collapse back to Home |

Settings (`SET`) focuses the Options tab and opens its list. Enter on
an Options row still lands on the native Settings page.

| Tab | List |
|-----|------|
| Games | Steam library (`steam://rungameid`) via `IGameLibraryCatalog` / `ILibraryLaunchService`. Empty library pads with Session engine + Steam/Playnite. |
| Tools | Desktop kit catalog (including Rainmeter, MusicBee, Store, Xbox). |
| Mods | Workshop / Vortex discovery. |
| Options | Six settings rows (`kind: settings`). Enter → Settings page. |
| Network, Files, Hardware | Honest page rows over Home; Enter lands on the native WinUI list. |

SessionEngine is not rewritten. The overlay is HTML inside the WebView
so WinUI-over-HWND airspace does not hide Home. List motion uses
console-grade curves (`cubic-bezier(0.16, 1, 0.3, 1)`).

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard, launch)
                 │
                 ├─ C#  HomeGalaxy / CubePose / CubeCatalog / CubeBrowse
                 │      owns tab focus (incl. Options), list origin, page nav
                 │
                 └─ WebView2  →  packaged HTML studio tabs (Assets/Cube)
                        JSON: state / open / focus / close / reset
                              ready / turn / pick / dragEnd / activate /
                              opened / cycle / select / back
                        open.origin = "top" | "bottom"
                        open.front = "Settings" + node = -1 → Options tab
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + HTML studio tabs.** `three.min.js` and `home-plate.png`
  remain unused look-dev / leftover vendor. No WebGL required for Home.
- If WebView2 is missing, Enter still opens the WinUI page.
- SET / PRFL glyphs stay discreet. Packaged HUD is on by default
  (Settings → Home HUD). Movable temp plaques remain optional.

Look-dev: `Assets/Cube/index.html?preview=1`.
Options: `?preview=1&open=options` (KeyO in the browser preview).
Games / Tools: `?preview=1&open=games` or `?open=tools`.

## Rainmeter (optional)

Rainmeter is **not** required for this look. See
[overlay-addon.md](overlay-addon.md). Home stays a black full-bleed
WebView so optional skins can still sit on top if the user installs
them later.

## Home widgets

The **packaged HUD** is the default theme. Light-grey **movable**
temp/load plaques remain as extras on the right (`X = 0.84`). Master
visibility is Settings → Home HUD.

List type is **Diavlo** (Jos Buivenga / exljbris), bundled under
`Assets/Fonts`. See `src/UnboundOS.App/Assets/Fonts/README.md` and
`LICENSE-Diavlo.txt`. Do not redistribute the font files as a
standalone download.
