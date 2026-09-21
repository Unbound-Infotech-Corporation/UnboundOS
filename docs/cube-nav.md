# Home navigation cube

UnboundOS Home is a **control altar** — a physical 3/4 cube sitting in a
cavity bay — not a face-on tile and not a letter-plate HUD. The WinUI 3
shell stays the product. Pages behind list faces stay native WinUI.
Games (and Tools/Mods when items exist) stay inside the cube as a block
browse.

## Art direction

**Scorn-inspired, original assets.** The look is a Giger-adjacent
cathedral of bone, cartilage, oxidized metal, and sparse bioluminescent
vein light. It is **inspiration only**: no Scorn / Ebb Software meshes,
textures, logos, audio, or trademarks. No Sony/PlayStation, no CDPR.
Unbound Infotech remains the product identity.

Materials: polished bone ivory, ossified ridges, dried tendon cables,
rivets through bone, mechanical socket apertures (abstract, not crude).
Palette: bone cream, bruise purple, bile amber, dried blood, ash brown,
deep cavity black. Company cyan (`#00F0FF`) stays a brand token for
inner pages; on Home it becomes a **sick teal vein** in recesses
(`#6FA896`) — sparse, not matrix neon.

Lighting is low-key cinematic (warm cavity glow, cool rim on bone,
wet clearcoat). Volumetrics, spore motes, and idle pulse run only when
interface motion is on. Motion-off (or a live session) keeps the still
beauty and snaps transforms.

## Open

| Face | Group | Destination |
|------|--------|-------------|
| Session (front) | **Games** | Cube stays open. Each block is a library game. Cycle with arrows / gamepad / click; Enter launches. |
| Tools | **Tools** | Mosaic of kit/utilities if discovery returned items; otherwise the Tools list page. |
| Mods | **Mods** | Mosaic of discovered Workshop/Vortex games if any; otherwise the Mods list page. |
| Network, Files, Hardware | config | Short organic unfold, then the native **list** page. |
| SET glyph | **Options / Settings** | Settings list of groups (motion, Home HUD, display, OC, startup, cleanup). Not a block carousel. |

Selecting a socket **unfolds** plates and segments. Games (and Tools/Mods
mosaic) cycling does **not** snap like a carousel reel. The focused block
eases forward; neighbors stagger aside with overlapping springs.
Motion-off copies the new layout instantly.

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
- **WebView2 + Three.js r158** is the visual path (MeshPhysical bone,
  original canvas maps — no ripped game textures).
- If WebView2 or WebGL is missing, the host shows a static face card and
  Enter still opens the WinUI page for that group.
- **Home chrome is cube-only.** Settings (`SET`) and Profiles (`PRFL`)
  are bone-plaque corner glyphs. A quiet **Home HUD** can show local time
  (top right) and CPU/GPU temps (top left) as engraved gauges when
  sensors answer — dashes until then. Settings → Home HUD toggles the
  strip, clock, and temps independently (persisted in `settings.json`).
  Inner pages restore the chrome; HOME resets the scene.

The rest camera sits farther back (FOV 26, ~5.7 units) so the cube fits
a typical ultrawide/desktop without clipping. Glyphs stay readable as
carved bone marks, not neon stickers.

Logical pose is still four yaw steps and ±90° pitch, with a rest bias
(+28° yaw, −17° pitch) so three faces read at once.

`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture.
Look-dev: `Assets/Cube/index.html?preview=1`.
