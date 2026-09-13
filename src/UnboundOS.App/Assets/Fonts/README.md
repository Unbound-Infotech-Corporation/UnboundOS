# Bundled fonts

UnboundOS ships Latin subsets of the Unbound Infotech site fonts so the
WinUI shell matches [unboundinfotech.com](https://unboundinfotech.com)
without a machine-wide install.

| File | Family name (after `#`) | Use |
|------|-------------------------|-----|
| `Inter-Regular.ttf` | `Inter` | Body UI |
| `Inter-SemiBold.ttf` | `Inter SemiBold` | Section titles, buttons |
| `Inter-Bold.ttf` | `Inter` | Hero / brand display |
| `JetBrainsMono-Regular.ttf` | `JetBrains Mono` | Telemetry, logs, crop math |

Sources: [Fontsource](https://fontsource.org/) builds of
[Inter](https://github.com/rsms/inter) and
[JetBrains Mono](https://github.com/JetBrains/JetBrainsMono).
Both are SIL Open Font License — see `LICENSE-Inter.txt` and
`LICENSE-JetBrainsMono.txt`.

WinUI `FontFamily` syntax:

```
ms-appx:///Assets/Fonts/Inter-Regular.ttf#Inter
ms-appx:///Assets/Fonts/JetBrainsMono-Regular.ttf#JetBrains Mono
```

If a packaged file fails to load, Windows falls back to
**Segoe UI Variable** / **Cascadia Mono**.
