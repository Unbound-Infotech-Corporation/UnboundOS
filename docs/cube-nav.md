# Home navigation cube

UnboundOS Home is a **physical 3/4 cube**, not a face-on tile and not a
letter-plate HUD. The WinUI 3 shell stays the product. Pages behind list
faces stay native WinUI. Games (and Tools/Mods when items exist) stay
inside the cube as a block browse.

## Look

Rest: a single heavy volcanic cube on a dark wet floor. Recessed circuit
grooves pulse in the Unbound family — cyan primary, cobalt secondary,
controlled magenta and circuit amber as seasoning. Each **face carries
an etched group logo** (Games, Tools, Network, Mods, Files, Hardware)
plus a small title — not letter soup. Haze, negative space, cube-only
Home chrome.

## Open

| Face | Group | Destination |
|------|--------|-------------|
| Session (front) | **Games** | Cube stays open. Each block is a library game. Cycle with arrows / gamepad / click; Enter launches. |
| Tools | **Tools** | Mosaic of kit/utilities if discovery returned items; otherwise the Tools list page. |
| Mods | **Mods** | Mosaic of discovered Workshop/Vortex games if any; otherwise the Mods list page. |
| Network, Files, Hardware | config | Short transform, then the native **list** page. |
| SET glyph | **Options / Settings** | Settings list of groups (motion, Home HUD, display, OC, startup, cleanup). Not a block carousel. |

Motion-off skips the heavy transform and goes straight to that destination
(browse pose for Games, list page for Settings/Files/…). Escape (or
Gamepad B) returns from a cube browse to rest.

Open Games (and Tools/Mods mosaic) cycling does **not** snap like a
carousel reel. The focused block eases forward, slightly larger and
brighter; neighbors stagger aside with overlapping springs so the
assembly redistributes mass. Motion-off copies the new layout instantly.

Games come from the local Steam `appmanifest_*.acf` scan
(`IGameLibraryCatalog` / `SteamGameLibraryCatalog`). Launch is
`steam://rungameid/{id}` via `ILibraryLaunchService` — SessionEngine is
not rewritten. If the library is empty, the carousel pads with **Session
engine** plus Steam/Playnite from the existing Tools catalog. Those
stubs are honest entry points, not fake installs.

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard, launch)
                 │
                 ├─ C#  CubePose / CubeCatalog / CubeBrowse / CubeAtmosphere
                 │      owns snapping, destinations, motion policy, page nav
                 │
                 └─ WebView2  →  packaged Three.js scene (Assets/Cube)
                        JSON: state / open / focus / reset
                              ready / turn / pick / dragEnd / activate /
                              opened / cycle / select / back
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + Three.js r158** is the visual path.
- If WebView2 or WebGL is missing, the host shows a static face card and
  Enter still opens the WinUI page for that group.
- **Home chrome is cube-only.** Settings (`SET`) and Profiles (`PRFL`)
  are discreet corner glyphs. A quiet **Home HUD** can show local time
  (top right) and CPU/GPU temps (top left) when sensors answer — dashes
  until then. Settings → Home HUD toggles the strip, clock, and temps
  independently (persisted in `settings.json`). Inner pages restore the
  chrome; HOME resets the scene.

The rest camera sits farther back (FOV 26, ~5.7 units) so the cube fits
a typical ultrawide/desktop without clipping. Logos stay readable.

Logical pose is still four yaw steps and ±90° pitch, with a rest bias
(+28° yaw, −17° pitch) so three faces read at once.

`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture.
Look-dev: `Assets/Cube/index.html?preview=1`.
