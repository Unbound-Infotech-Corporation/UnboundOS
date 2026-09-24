# Theme tokens (tweak these)

Super Clean first-party Home. No Rainmeter. No galaxy. No Gik0n HUD.

Keep these three in sync:

| Place | What |
|-------|------|
| `src/UnboundOS.Core/Brand/UnboundTokens.cs` | C# constants |
| `src/UnboundOS.App/Assets/Cube/index.html` `:root` | Super Clean Home labels |
| `src/UnboundOS.App/Themes/UnboundTheme.xaml` | Inner pages |

| Token | Hex | Use |
|-------|-----|-----|
| Obsidian / void | `#05070A` | Field + detail surface |
| Cyan | `#00F0FF` | Focus hairline, list focus, inner pulse |
| Paper | `#F7FAFC` | Focused label |
| Quiet | `#5C646C` | Unfocused labels |
| Star | `#F4EFE2` | Inner-page list titles |
| Muted | `#8A8A96` | Kickers |

Home type is Diavlo throughout (`--home-font`: Book / Medium / Bold).
Labels live in `#labelStack`. Focus uses Diavlo Medium plus a cyan
hairline. `--d` (distance from focus) drives neighbor size, padding,
and opacity so the stack pushes as one. `#clock` is the right-side
time + date. `body.reduce-motion` freezes enlarge/push and list ease.
`body.is-overlay` hides the stack while the all-black category surface
is open; the clock stays.

Session presets: `JsonProfileStore.CreateDefaults()` and
`%LocalAppData%\Unbound Infotech Corporation\UnboundOS\profiles.json`.
