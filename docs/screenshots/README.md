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
- Home hero is a **biomechanical 3/4 cube** on a bone pedestal in a
  cavity bay. Group logos are carved bone glyphs (Games, Tools, Network,
  Mods, Files, Hardware), not neon stickers.
- Enter on Games **unfolds** into a block carousel of library games.
  Settings / Files / Hardware / Network land on list UI.
- On a turn: short tendon filaments, then settle; accent stays in the
  organic family (vein / bruise / bile / blood / bone).
- Interface motion On: springy rotate, filaments, idle, organic unfold.
  Off (or a live session): instant snap, no volumetrics, no idle, no open FX.
- Inner-page chrome keeps obsidian `#05070A` and cyan pulse `#00F0FF`.
  Home HUD/glyphs are bone plaques. Inter UI + JetBrains Mono telemetry —
  matching [unboundinfotech.com](https://unboundinfotech.com) for the
  product identity, not the Home materials.
