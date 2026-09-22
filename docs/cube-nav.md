# Home navigation galaxy

UnboundOS Home is an **edge-on linear galaxy** on a true-black field —
the user-supplied plate brought to life — not a spiral sticker and not a
3D cube as the hero. Bright clusters along the disk are the menu nodes.
The WinUI 3 shell stays the product. Category lists ease in over the
living galaxy so the field stays visible.

## Art direction

**User plate + living stars.** `Assets/Cube/home-plate.png` is the
owner-supplied Home background (creamy core, dust lanes, cooler blue
outer arms, dense halo). It is packaged as shipped. **No NASA or
observatory credit is claimed**; do not invent one. Procedural Three.js
layers sit on top: parallax starfields, slow differential rotation /
orbital shear around the plane, and gentle drift toward the locked
node cores. Never random twinkle spam.

Games sits on the bright core. Tools and Mods lock to the right-hand
clusters; Network, Files, and Hardware lock to the left-hand arm.
Nodes stay on the horizontal. Company cyan (`#00F0FF`) stays a brand
token for **inner pages**.

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
| Tools | Desktop kit catalog. |
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
- **WebView2 + Three.js r158** plus `home-plate.png`.
- If WebView2 or WebGL is missing, Enter still opens the WinUI page.
- Quiet Home HUD (clock / temps) and SET / PRFL glyphs stay optional.

Look-dev: `Assets/Cube/index.html?preview=1`.
`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture.
