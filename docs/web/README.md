# Unbound Infotech — /software presentation page

`apps.html` is a fully self-contained page (inline CSS + JS, Google Fonts via CDN) presenting the
Unbound Infotech product line with **UnboundOS** as the flagship. It matches the corporate site
theme: near-black `hsl(220 30% 2%)` background, neon-cyan `hsl(186 100% 50%)` primary,
Inter + JetBrains Mono.

## Preview locally

Just open the file in a browser:

```powershell
start docs\web\apps.html
```

## Placeholders to replace before going live

| Placeholder | Where | Replace with |
|---|---|---|
| UnboundOS Pro Payment Link | Already wired in `apps.html` Buy buttons | Live link: `https://buy.stripe.com/9B6aEXafQ6ym9Kg3R3dQQ01` ($14.99 lifetime Pro) |
| `#stripe-payment-link-insurevault` | InsureVault card | Waitlist / preorder Payment Link (or a signup form URL) |
| `https://unboundinfotech.com/` on the Heirloom card | Heirloom Unbound card | The actual Heirloom Unbound page URL |
| `hello@unboundinfotech.com` | Bottom CTA "Talk to us" | Real contact address |

Pricing model: **Free core** (library + basic session tools) + **UnboundOS Pro $14.99 lifetime** (dual-NIC, ultrawide stream tools, Workshop/mod profiles). No ads.

Creating additional Payment Links: Stripe Dashboard → Payment Links → **+ New**.

## Pasting into Base44

1. Create (or open) the page you want, e.g. `/software` or `/apps`.
2. Add a **Custom Code / Embed HTML** block (full-width).
3. Paste the entire contents of `apps.html`.
   - If Base44 rejects full documents, paste only what's between `<body>...</body>` plus the
     `<style>` block and the two `<link>` font tags from `<head>`.
4. Swap the Stripe Payment Link hrefs as listed above.
5. Publish. No build step, no external CSS/JS files needed.

## Hosting as static HTML

Rename to `index.html` and drop it in any static host (Netlify, Cloudflare Pages, GitHub Pages,
S3). It has no dependencies beyond Google Fonts.
