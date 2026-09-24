# Bundled fonts

UnboundOS ships **Diavlo** as the Super Clean shell typeface for every
menu, page, and Home surface, plus Inter / JetBrains Mono as unused
fallbacks that stay in the tree for license completeness.

| File | Family name (after `#`) | Use |
|------|-------------------------|-----|
| `Diavlo_BOOK_II_37.otf` | `Diavlo Book` | Body, unfocused lists, help |
| `Diavlo_MEDIUM_II_37.otf` | `Diavlo Medium` | Focus, titles, clock |
| `Diavlo_BOLD_II_37.otf` | `Diavlo Bold` | Strong display if needed |
| `Inter-*.ttf` / `JetBrainsMono-Regular.ttf` | unused in Super Clean | License files stay |

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
