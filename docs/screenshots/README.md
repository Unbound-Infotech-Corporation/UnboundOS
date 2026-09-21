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

- Slim top chrome (HOME / SESSION / NETWORK / TOOLS / FILES / HARDWARE /
  MODS / PROFILES / SETTINGS). Settings and Profiles are **not** cube faces.
- Home hero is a **3D cube** in classic 3/4 (three faces readable at
  rest), not a flat START tile or a face-on CSS cube. The old Settings
  tile monogram “I” must not return.
- Obsidian metal plates, cyan/cobalt seams, circuit etch. On a turn:
  short electric arcs, then settle; accent hue drifts per destination.
- Interface motion On: springy rotate, arcs, optional tiny idle yaw.
  Off (or a live session): instant snap, no arcs, no idle. Still angled.
- Obsidian `#05070A`, cyan pulse `#00F0FF`, circuit amber `#E4B53C` spare,
  Inter UI + JetBrains Mono telemetry — matching
  [unboundinfotech.com](https://unboundinfotech.com).
