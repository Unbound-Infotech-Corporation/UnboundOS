# UnboundOS license database

Minimal PlanetScale database for **Free core + $14.99 Pro lifetime** entitlement after Stripe Checkout.

## Live database

| Field | Value |
|---|---|
| Organization | `akindabigdeal` |
| Database | `unbound-licenses` |
| Engine | **Postgres 18** (PlanetScale) |
| Branch | `main` |
| Cluster | PS-5 single-node, AWS us-east-1 (~$5/mo) |
| Dashboard | https://app.planetscale.com/akindabigdeal/unbound-licenses |

Vitess/MySQL was available but starts at ~$39/mo; Postgres PS-5 single-node is enough for license keys.

## Workload assumptions

| Aspect | Expectation |
|---|---|
| Reads | App activation / validate (point lookup by key hash) |
| Writes | Stripe `checkout.session.completed`, activate, deactivate, refund revoke |
| Volume | Low (thousands of licenses initially) |
| Latency | Activate API under 200ms p95 is plenty |

## Flow

1. Buyer pays via Payment Link: `https://buy.stripe.com/9B6aEXafQ6ym9Kg3R3dQQ01`
2. Webhook `checkout.session.completed` → create `customers` + `licenses` (store **SHA-256** of key only)
3. Email / success page shows the raw license key **once**
4. UnboundOS calls `POST /v1/activate` with key + machine fingerprint
5. Server checks hash, status, activation cap (default **3** devices), upserts `license_activations`

## Security rules

- Never persist the raw license key in the database
- Store `license_key_hash` (SHA-256) + short `license_key_prefix` for support
- Stripe webhook handler must insert `stripe_events` first (unique) for idempotency
- On refund/dispute → set `licenses.status = 'refunded'` and reject future activates

## Files

- `001_license_activation.sql` — original MySQL/InnoDB draft (reference)
- `002_license_activation_postgres.sql` — **use this** on PlanetScale Postgres

## Schema status

Applied on `main` (2026-07-19): `products`, `customers`, `licenses`, `license_activations`, `stripe_events`, plus UnboundOS Pro seed row.

## Licensing API

Scaffolded at `src/UnboundOS.Licensing.Api`. See that folder’s README for run + Stripe webhook steps.

## Next steps

1. Copy `.env.example` → `.env` and fill secrets locally (never paste into chat)
2. `dotnet run --project src\UnboundOS.Licensing.Api`
3. Point a Stripe webhook at `/webhooks/stripe` (use ngrok for local tests)
4. Wire UnboundOS Pro unlock UI to `POST /v1/activate`

