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
- Home hero is a **volcanic 3/4 cube** with etched group logos (Games,
  Tools, Network, Mods, Files, Hardware), wet floor, no face-letter soup.
- Enter on Games **disassembles** into a block carousel of library games.
  Settings / Files / Hardware / Network land on list UI.
- On a turn: short electric arcs, then settle; accent hue drifts per
  destination (cyan / cobalt; magenta and amber as seasoning).
- Interface motion On: springy rotate, arcs, idle, open transform.
  Off (or a live session): instant snap, no arcs, no idle, no open FX.
- Obsidian `#05070A`, cyan pulse `#00F0FF`, circuit amber `#E4B53C` spare,
  Inter UI + JetBrains Mono telemetry — matching
  [unboundinfotech.com](https://unboundinfotech.com).
