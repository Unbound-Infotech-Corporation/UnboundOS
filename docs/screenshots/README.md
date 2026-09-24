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
- Home hero is a **left stack of category labels** on Obsidian `#05070A`.
  Unfocused names stay small and quiet. The focused name enlarges in
  Diavlo and pushes neighbors, with a cyan `#00F0FF` hairline. No
  galaxy, nebula, sun, teal-tip tabs, or Rainmeter HUD. Opening a
  group replaces Home with an all-black titles-only list. SET opens
  Options. Escape returns.
- **Up/Down** (D-pad, stick, arrows, wheel) move labels. **Enter**
  opens that group. Left/Right also move the label list.
- Interface motion On: shared enlarge/push and silky list ease.
  Off (or a live session): instant focus; lists still open.
- Inner-page chrome keeps obsidian `#05070A` and cyan pulse `#00F0FF`.
