# Home navigation (studio tabs)

UnboundOS Home is a **super clean black field** with a row of **narrow
vertical tabs** along the bottom. No galaxy, no nebulas, no suns, no
star-system nodes. Negative space is the point.

The WinUI 3 shell stays the product. Category lists ease in over the
same black field so Rainmeter skins (Phenix clock/temps, visualizers)
remain visible. Home is a full-bleed Obsidian / black shell — not
opaque WinUI chrome.

## Art direction

**Studio product shot.** Runtime Home is HTML/CSS in the WebView
(Three.js stays vendored, unused):

- Background is Obsidian `#05070A` (or true black). Empty and calm.
- Tabs sit along the **bottom** as tall thin blades that rise toward
  the top. Soft key + fill drop shadows under/around each blade —
  grounded product shot, not flat UI chrome.
- The **tip** of every tab is brand cyan-teal `#00F0FF`. The body is
  dark metal / charcoal. No neon flood.
- Focus lifts the blade a few millimeters, strengthens the shadow, and
  brightens the teal tip. Restrained.
- Only the **focused** tab shows its name **above** in Diavlo, with a
  soft drop shadow. Unfocused tabs stay unlabeled.

Tabs, left to right: Hardware, Files, Network, Games, Options, Tools,
Mods. Games is the default. Options is a first-class tab (also opened
by the Home **SET** glyph). Profiles (`PRFL`) stays a discreet corner
glyph.

Opening a category (Enter / Up / Down) shows a **titles-only** Diavlo
list on the left over a translucent black wash. No camera zoom. Escape
eases back to the tab row. Motion-off snaps focus and list states
(no lift ease).

Company cyan `#00F0FF` is the tab-tip material and the inner-page
token. It is not a full-screen wash.

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
- Quiet Home HUD (movable clock / temps / load) and SET / PRFL glyphs stay optional.

Look-dev: `Assets/Cube/index.html?preview=1`.
Options: `?preview=1&open=options` (KeyO in the browser preview).
Games / Tools: `?preview=1&open=games` or `?open=tools`.

## Rainmeter

Home stays a **transparent / black full-bleed** WebView so Rainmeter
overlays sit on top of the empty field. Do not paint an opaque WinUI
slab over Home. See [overlay-addon.md](overlay-addon.md). Recommended
theme is **Phenix**; recommended clock skin is **Minimalistic Clock**.

## Home widgets

Light-grey **movable** plaques for honest basics. Default stack sits
on the **right** (`X = 0.84`, past `ListKeepoutX` 0.48) so plaques
rest beside the left titles list and above the tab row. Master
visibility is Settings → Home HUD.

List type is **Diavlo** (Jos Buivenga / exljbris), bundled under
`Assets/Fonts`. See `src/UnboundOS.App/Assets/Fonts/README.md` and
`LICENSE-Diavlo.txt`. Do not redistribute the font files as a
standalone download.
