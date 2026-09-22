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
- Home hero is an **original procedural edge-on galaxy**: black void,
  creamy core, dust-lane ribbon, cooler blue arms, dense halo.
  All stars move a little (shear / differential rotation / parallax);
  node clusters stay locked on the bar. Focused node lifts like a
  selected star. Lists sit on the left half; the scrim vanishes on Escape.
- **Left/Right** shift nodes. **Up** opens that node’s list from the
  bottom; **Down** opens it from the top. The galaxy stays visible.
- Interface motion On: differential rotation, parallax, silky list ease.
  Off (or a live session): freeze drift / heavy postFX; lists still open.
- Inner-page chrome keeps obsidian `#05070A` and cyan pulse `#00F0FF`.
