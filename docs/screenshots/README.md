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
- Home hero is a **3D cube**, not a flat START tile row. The old Settings
  tile monogram “I” must not return.
- Front face cyan hairline + L-ticks (one circuit amber corner). Other
  faces recede with perspective.
- Interface motion On: springy rotate, optional tiny idle yaw. Off (or a
  live session): instant snap, no idle spin.
- Obsidian `#05070A`, cyan pulse `#00F0FF`, circuit amber `#E4B53C` spare,
  Inter UI + JetBrains Mono telemetry — matching
  [unboundinfotech.com](https://unboundinfotech.com).
