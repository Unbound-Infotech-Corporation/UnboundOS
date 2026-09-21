# Home navigation cube

UnboundOS Home is a **physical 3/4 cube**, not a face-on tile and not a
letter-plate HUD. The WinUI 3 shell stays the product. Pages behind each
face stay native WinUI.

## Look

Rest (product still): a single heavy volcanic cube on a dark wet floor.
Recessed circuit grooves pulse in the Unbound family — cyan primary,
cobalt secondary, controlled magenta and circuit amber as seasoning, not
a pink costume. Haze, negative space, no chrome or face lettering. The
cube is the only focal point on Home.

Activate (Enter / click the front): the hull **disassembles** into an
uneven gunmetal block assembly (Tetris / greeble) with iridescent light
in the seams (teal/cyan, magenta, amber), then the shell lands in that
face’s native page. Motion-off skips the transform and navigates at once.

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard)
                 │
                 ├─ C#  CubePose / CubeCatalog / CubeInput / CubeAtmosphere
                 │      owns snapping, destinations, motion policy, page nav
                 │
                 └─ WebView2  →  packaged Three.js scene (Assets/Cube)
                        local virtual host https://unboundos.cube/Cube/
                        JSON: state / open / reset / ready / activate /
                              opened / turn / pick / dragEnd
```

- **Not Unreal.** UE is not a runtime dependency.
- **WebView2 + Three.js r158** is the visual path.
- If WebView2 or WebGL is missing, the host shows a static face card.
  Keyboard, narrator, and Enter-to-open still work.
- **Home chrome is cube-only.** Top nav, brand block, tagline, and scan
  grid hide on Home. Settings and Profiles are discreet corner glyphs
  (`SET` / `PRFL`). Inner pages restore the chrome; HOME resets the scene.

Cube faces: Session, Tools, Network, Mods, Files, Hardware.

## Pose

Logical pose is still four yaw steps and ±90° pitch. A **rest bias**
(+28° yaw, −17° pitch) is applied only in the renderer so the cube sits
in classic 3/4: Session leads, Tools on the right, Mods as the top plate.

Each completed turn lerps the emissive accent inside the Unbound brand
family. Motion-on spawns short electric arc filaments, then they die out.

## Motion toggle

`IUiMotionPolicy.AllowMotion` is false when:

1. Settings → Interface motion is Off
2. Windows animation effects are Off
3. A session is live

The scene then **snaps**, kills arcs, idle, motes, and the open
disassemble, drops to a single frame, and stays usable as a static
angled cube. That path is the cheap one for gaming performance.

## Bridge

C# → scene: `CubeBridge.State` JSON (`yaw`, `pitch`, `restYaw`,
`restPitch`, `front`, `motion`, `burst`, palettes, face catalog).
Activate posts `CubeBridge.Open`; returning Home posts `CubeBridge.Reset`.

Scene → C#: `ready`, `turn`, `pick`, `activate`, `opened`, `dragEnd`.

`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture
of the shell. The HTML scene can also be opened with `?preview=1` in a
browser for look-dev without WinUI.
