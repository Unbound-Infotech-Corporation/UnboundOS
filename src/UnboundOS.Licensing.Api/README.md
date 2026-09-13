# UnboundOS Licensing API

Issues UnboundOS Pro lifetime keys after Stripe Checkout, and activates them on a machine.

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/health` | DB connectivity check |
| POST | `/webhooks/stripe` | Stripe `checkout.session.completed` |
| POST | `/v1/activate` | Unlock Pro in the desktop app |

## One-time setup (you)

### 1. Create `.env` at the repo root

```powershell
copy .env.example .env
notepad .env
```

Fill in PlanetScale + Stripe values. Do **not** paste those into chat.

### 2. Run the API locally

```powershell
dotnet run --project src\UnboundOS.Licensing.Api
```

It listens on `http://localhost:5xxx` (see the console URL).

### 3. Expose it for Stripe (local testing)

Install [ngrok](https://ngrok.com/), then:

```powershell
ngrok http 5xxx
```

Copy the `https://....ngrok-free.app` URL.

### 4. Add Stripe webhook

1. Stripe Dashboard → **Developers → Webhooks → Add endpoint**
2. URL: `https://YOUR-NGROK-URL/webhooks/stripe`
3. Event: `checkout.session.completed`
4. Copy **Signing secret** → put in `.env` as `STRIPE_WEBHOOK_SECRET`
5. Restart the API

### 5. Test a purchase

Open your Payment Link:  
https://buy.stripe.com/9B6aEXafQ6ym9Kg3R3dQQ01  

After payment, check the API logs — it will print that a key was issued.  
(Later we wire email delivery; for now the key is also in the webhook JSON response Stripe doesn’t show you — check API logs / add email next.)

### 6. Test activation

```powershell
curl -X POST http://localhost:5xxx/v1/activate `
  -H "Content-Type: application/json" `
  -d "{\"licenseKey\":\"UBOS-....\",\"machineFingerprint\":\"pc-test-1\",\"machineLabel\":\"Dev PC\",\"appVersion\":\"0.1.0\"}"
```

## Safety

- Raw license keys are **never** stored in PlanetScale (SHA-256 only).
- Stripe event IDs are recorded so retries don’t double-issue.
- Default max activations: **3** devices (`products.max_activations`).
