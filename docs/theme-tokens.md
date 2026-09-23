# Theme tokens (tweak these)

First-party Gik0n-like HUD. No Rainmeter. No galaxy.

Keep these three in sync:

| Place | What |
|-------|------|
| `src/UnboundOS.Core/Brand/UnboundTokens.cs` | C# constants |
| `src/UnboundOS.App/Assets/Cube/index.html` `:root` | Home HUD / tabs |
| `src/UnboundOS.App/Themes/UnboundTheme.xaml` | Inner pages |

| Token | Hex | Use |
|-------|-----|-----|
| Obsidian | `#05070A` | Field |
| Cyan | `#00F0FF` | Tab tips, viz, focus, inner pulse |
| Paper | `#F7FAFC` | Clock / light glyphs |
| Star | `#F4EFE2` | List titles |
| Muted | `#8A8A96` | Kickers |
| Date ink | `#C8D0D8` | Date line |
| Weather ink | `#8A96A2` | Weather stub |

HUD pieces live in `#hud` (clock, date, weather, viz, media, launcher,
calendar). `body.hud-off` or Settings → Home HUD hides them.
`body.reduce-motion` freezes viz/tab breath. `body.is-overlay` shrinks
the clock, hides the launcher, keeps chrome while a category list is
open.

Session presets: `JsonProfileStore.CreateDefaults()` and
`%LocalAppData%\Unbound Infotech Corporation\UnboundOS\profiles.json`.
