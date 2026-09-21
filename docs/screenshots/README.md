# Screenshots

Drop a PNG here named `shell.png` after a local Windows run of the
UnboundOS shell. The README references that file.

This Linux CI agent cannot compile or render WinUI XAML (no
`XamlCompiler.exe`). Capture on Windows:

```powershell
dotnet build UnboundOS.sln -c Debug -p:Platform=x64
dotnet run --project src\UnboundOS.App\UnboundOS.App.csproj -c Debug -p:Platform=x64
```

Visual check:

- **Home is cube-only** — no top chrome, brand block, tagline, or START
  row. Settings / Profiles are discreet corner glyphs, not cube faces.
  Inner pages restore HOME / SESSION / NETWORK / TOOLS / FILES /
  HARDWARE / MODS / PROFILES / SETTINGS.
- Home hero is a **volcanic 3/4 cube** on a wet floor (recessed glowing
  grooves, haze, no face letters), not a flat START tile or a CSS cube.
  The old Settings tile monogram “I” must not return.
- Enter / front-click **disassembles** the cube into an uneven gunmetal
  block assembly with iridescent seam light, then lands in that page.
- On a turn: short electric arcs, then settle; accent hue drifts per
  destination (cyan / cobalt; magenta and amber as seasoning).
- Interface motion On: springy rotate, arcs, idle, open transform.
  Off (or a live session): instant snap, no arcs, no idle, no open FX.
- Obsidian `#05070A`, cyan pulse `#00F0FF`, circuit amber `#E4B53C` spare,
  Inter UI + JetBrains Mono telemetry — matching
  [unboundinfotech.com](https://unboundinfotech.com).
