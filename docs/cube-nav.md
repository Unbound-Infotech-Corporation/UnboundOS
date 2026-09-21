# Home navigation cube

UnboundOS Home is a **physical 3/4 cube**, not a face-on tile. The WinUI 3
shell stays the product. Pages behind each face stay native WinUI.

## Architecture

```
MainPage  →  NavigationCubeView (WinUI host, a11y, keyboard)
                 │
                 ├─ C#  CubePose / CubeCatalog / CubeInput / CubeAtmosphere
                 │      owns snapping, destinations, motion policy, page nav
                 │
                 └─ WebView2  →  packaged Three.js scene (Assets/Cube)
                        local virtual host https://unboundos.cube/Cube/
                        JSON messages for pose, pick, drag, activate
```

- **Not Unreal.** UE is not a runtime dependency. A later optional Unreal
  splash / cinema pass can play as a separate process or pre-rendered
  sequence; it must not be in-process with the gaming OS shell.
- **WebView2 + Three.js r158** is the visual path (perspective camera,
  obsidian metal plates, circuit etch, cyan/cobalt seams).
- If WebView2 or WebGL is missing, the host shows a static face card.
  Keyboard, narrator, and Enter-to-open still work.
- Settings / Profiles / Overlay stay in the top chrome. Cube faces:
  Session, Tools, Network, Mods, Files, Hardware.

## Pose

Logical pose is still four yaw steps and ±90° pitch. A **rest bias**
(+28° yaw, −17° pitch) is applied only in the renderer so the cube sits
in classic 3/4: Session leads, Tools on the right, Mods as the top plate.

Each completed turn lerps the emissive accent inside the Unbound brand
family (cyan pulse, cobalt, steel, restrained teal/ice). Motion-on
spawns short electric arc filaments from the seams, then they die out.

## Motion toggle

`IUiMotionPolicy.AllowMotion` is false when:

1. Settings → Interface motion is Off
2. Windows animation effects are Off
3. A session is live

The scene then **snaps**, kills arcs, idle, and motes, drops to a single
frame, and stays usable as a static angled cube. That path is the cheap
one for gaming performance.

## Bridge

C# → scene: `CubeBridge.State` JSON (`yaw`, `pitch`, `restYaw`,
`restPitch`, `front`, `motion`, `burst`, palettes, face catalog).

Scene → C#: `ready`, `turn`, `pick`, `activate`, `dragEnd`.

`docs/screenshots/README.md` still wants a Windows `Debug|x64` capture
of the shell. The HTML scene can also be opened with `?preview=1` in a
browser for look-dev without WinUI.
