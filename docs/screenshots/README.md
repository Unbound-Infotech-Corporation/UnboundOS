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

- **Home is galaxy-only** — no top chrome, brand block, tagline, or START
  row. Settings / Profiles are discreet corner glyphs, not galaxy nodes.
  Inner pages restore HOME / SESSION / NETWORK / TOOLS / FILES /
  HARDWARE / MODS / PROFILES / SETTINGS.
- Home hero is a **linear galaxy** on a true-black field: elongated
  luminous dust, living stars, six node cores. Original procedural
  WebGL — not a flat spiral sticker, not a bone cube.
- **Up** opens a translucent horizontal games list over the galaxy.
  **Down** opens Settings. Left/right pan nodes.
- Interface motion On: star drift, orbits around nodes, bloom, god-dust.
  Off (or a live session): freeze drift / heavy postFX; still navigable.
- Inner-page chrome keeps obsidian `#05070A` and cyan pulse `#00F0FF`.
  Home HUD/glyphs stay quiet on the black field. Inter UI + JetBrains
  Mono telemetry — matching [unboundinfotech.com](https://unboundinfotech.com)
  for the product identity, not the Home materials.
