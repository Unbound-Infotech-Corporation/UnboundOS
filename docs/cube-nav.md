# Home navigation galaxy

UnboundOS Home is a **linear galaxy** on a true-black field — an elongated
ribbon of luminous dust and stars with six destination cores — not a
spiral sticker and not a 3D cube as the hero. The WinUI 3 shell stays
the product. Pages behind list nodes stay native WinUI. Games stay
inside the scene as a **translucent horizontal list** so the galaxy
remains visible underneath.

## Art direction

**Original procedural / WebGL only.** Packaged Three.js paints the ribbon
from canvas sprites and particle fields. No ripped NASA plates, no
third-party game textures. No Sony/PlayStation, no CDPR. Unbound Infotech
remains the product identity.

The field is filmic black. The galaxy is a thin, warped bar of warm dust
and cool starlight, with luminous node cores, soft god-dust shafts,
parallax star layers, and ACES tone mapping. Stars bound to a node
orbit it slowly; dust between cores eases toward the nearest core —
gentle gravity, never chaotic noise.

Company cyan (`#00F0FF`) stays a brand token for **inner pages**. Home
does not wear it as neon. Motion-off (or a live session) freezes star
drift and heavy postFX; nodes still pan and the list still opens.

The previous biomechanical cube / control-altar scene is no longer the
Home hero.

## Keys

| Input | Idle galaxy | Games overlay |
|-------|-------------|---------------|
| **Up** | Open the horizontal **Games** list over the galaxy | (ignored) |
| **Down** | Open **Settings** (WinUI list) | Close overlay, return to the galaxy |
| **Left / Right** | Shift focus along the nodes (galaxy pans) | Cycle games |
| **Enter** | Open the focused node (Games stays as overlay; others navigate) | Launch the focused library title |
| **Escape** | — | Close overlay |

Nodes, left to right: **Games**, Tools, Mods, Network, Files, Hardware.
Settings (`SET`) and Profiles (`PRFL`) stay discreet Home corner glyphs.
Down from idle is the fast path into Settings.

Games come from the local Steam `appmanifest_*.acf` scan
(`IGameLibraryCatalog` / `SteamGameLibraryCatalog`). Launch is
`steam://rungameid/{id}` via `ILibraryLaunchService` — SessionEngine is
not rewritten. If the library is empty, the list pads with **Session
engine** plus Steam/Playnite from the existing Tools catalog. Those
stubs are honest entry points, not fake installs.

The overlay is HTML inside the WebView so WinUI-over-HWND airspace does
not hide the galaxy.

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard, launch)
                 │
                 ├─ C#  HomeGalaxy / CubePose / CubeCatalog / CubeBrowse
                 │      owns node focus, overlay, motion policy, page nav
                 │
                 └─ WebView2  →  packaged Three.js scene (Assets/Cube)
                        JSON: state / open / focus / reset
                              ready / turn / pick / dragEnd / activate /
                              opened / cycle / select / back
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + Three.js r158** is the visual path (Points + additive
  sprites — original canvas maps).
- If WebView2 or WebGL is missing, the host shows a static card and
  Enter still opens the WinUI page for that group.
- **Home chrome stays quiet.** Settings (`SET`) and Profiles (`PRFL`)
  are corner glyphs on the black field. A quiet **Home HUD** can show
  local time (top right) and CPU/GPU temps (top left) when sensors
  answer — dashes until then. Settings → Home HUD toggles the strip,
  clock, and temps independently (persisted in `settings.json`).
  Inner pages restore the chrome; HOME resets the scene.

The rest camera sits far enough back (FOV 30, ~8.6 units) that the
ribbon reads on a typical ultrawide. Left/right eases focus along
the bar without yanking the whole field off-screen.

Logical destinations still map through `CubePose` so existing page
navigation stays intact. Idle keys use `HomeGalaxy.FromTurn` instead of
pitching the old cube: Up opens games, Down opens Settings, Left/Right
pan nodes.

`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture.
Look-dev: `Assets/Cube/index.html?preview=1`.
