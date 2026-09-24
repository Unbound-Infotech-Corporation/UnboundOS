# Bundled fonts

UnboundOS ships Latin subsets of the Unbound Infotech site fonts so the
WinUI shell matches [unboundinfotech.com](https://unboundinfotech.com)
without a machine-wide install, plus **Diavlo** for Home category lists.

| File | Family name (after `#`) | Use |
|------|-------------------------|-----|
| `Inter-Regular.ttf` | `Inter` | Body UI |
| `Inter-SemiBold.ttf` | `Inter SemiBold` | Section titles, buttons |
| `Inter-Bold.ttf` | `Inter` | Hero / brand display |
| `JetBrainsMono-Regular.ttf` | `JetBrains Mono` | Telemetry, logs, crop math |
| `Diavlo_BOOK_II_37.otf` | `Diavlo Book` | Home list titles (regular) |
| `Diavlo_MEDIUM_II_37.otf` | `Diavlo Medium` | Home list headers / node label |
| `Diavlo_BOLD_II_37.otf` | `Diavlo Bold` | Home list selection |

## Diavlo (exljbris / Jos Buivenga)

Home labels, clock, date, category lists, headers, and empty states
use **Diavlo**.

- Official page: [https://www.exljbris.com/diavlo.html](https://www.exljbris.com/diavlo.html)
- Free for personal and commercial use. Embedding in programs and PDFs
  is allowed. The font files may not be modified or sold.
- **These OTFs are embedded in UnboundOS only.** Do not redistribute
  them as a standalone downloadable asset outside the app.
- Full vendor text: `LICENSE-Diavlo.txt`.

WinUI `FontFamily` syntax:

```
ms-appx:///Assets/Fonts/Diavlo_BOOK_II_37.otf#Diavlo Book
ms-appx:///Assets/Fonts/Diavlo_MEDIUM_II_37.otf#Diavlo Medium
ms-appx:///Assets/Fonts/Diavlo_BOLD_II_37.otf#Diavlo Bold
```

WebView `@font-face` exposes one CSS family named `Diavlo` (weights
400 / 500 / 700) so the list can switch Book → Bold on selection.

WebView Home (`Assets/Cube/index.html`) loads the same files via
`@font-face` (comment required by the foundry:
`/* A font by Jos Buivenga (exljbris) -> www.exljbris.com */`).
Cube virtual host maps the Assets root, so `../Fonts/` resolves.

If a packaged file fails to load, Windows / the WebView falls back to
**Segoe UI Variable**.

## Inter and JetBrains Mono

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
