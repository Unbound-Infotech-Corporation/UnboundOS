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

- **Home is a black studio field** — no top chrome, brand block, tagline,
  or START row. Settings / Profiles are discreet corner glyphs.
  Inner pages restore HOME / SESSION / NETWORK / TOOLS / FILES /
  HARDWARE / MODS / PROFILES / SETTINGS.
- Home hero is a **row of narrow vertical tabs** on Obsidian `#05070A`:
  dark metal blades, cyan-teal `#00F0FF` tips, soft drop shadows on a
  dark floor. No galaxy, nebula, or sun. Only the focused tab shows a
  Diavlo name above. Opening a list keeps that field; titles-only
  Diavlo rows sit on the left over translucent black. SET opens Options.
  Escape returns.
- **Left/Right** move tabs. **Up** opens that tab’s list from the
  bottom; **Down** opens it from the top. The black field stays visible
  so Rainmeter overlays still read.
- Interface motion On: quiet tab lift and silky list ease.
  Off (or a live session): instant focus; lists still open.
- Inner-page chrome keeps obsidian `#05070A` and cyan pulse `#00F0FF`.
