# Theme tokens (tweak these)

Super Clean is **shell-wide**: Home, Settings hubs, Tools, Games lists,
Files, Hardware, Mods, Session, dialogs, and empty states. No Rainmeter.
No galaxy. No Gik0n HUD. No Inter/Segoe body copy on these surfaces.

Keep these three in sync:

| Place | What |
|-------|------|
| `src/UnboundOS.Core/Brand/UnboundTokens.cs` | C# constants |
| `src/UnboundOS.App/Assets/Cube/index.html` `:root` | Super Clean Home |
| `src/UnboundOS.App/Themes/UnboundTheme.xaml` | Shared WinUI resources |

| Token | Hex | Use |
|-------|-----|-----|
| Obsidian / void | `#05070A` | Every page field |
| Cyan | `#00F0FF` | Focus hairline and active only |
| Paper | `#F7FAFC` | Focused / primary type |
| Quiet / Muted | `#5C646C` | Unfocused copy |

`UiFontFamily`, `UiFontSemiboldFamily`, `DisplayFontFamily`, and
`MonoFontFamily` all resolve to packaged Diavlo. Vertical lists use
`SuperCleanListViewStyle` / `SuperCleanListItemStyle` (enlarge + pad
neighbors). Tile rows keep cyan hairline on focus. Motion-off snaps
scale with no compositor animation.

Home clock is `#clock` in the WebView. Inner top-level pages show the
same Diavlo clock/date in chrome (`ShellViewModel.ClockText`).
