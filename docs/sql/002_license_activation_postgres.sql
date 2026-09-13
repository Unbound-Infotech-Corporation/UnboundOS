-- PlanetScale Postgres schema for UnboundOS Pro licenses
-- Database: unbound-licenses (org: akindabigdeal, branch: main)
-- Note: foreign_keys_enabled=false on this database, so referential integrity is app-enforced.

CREATE TABLE IF NOT EXISTS products (
  id                BIGSERIAL PRIMARY KEY,
  product_key       VARCHAR(64)  NOT NULL,
  display_name      VARCHAR(128) NOT NULL,
  stripe_product_id VARCHAR(64)  NULL,
  stripe_price_id   VARCHAR(64)  NULL,
  max_activations   SMALLINT     NOT NULL DEFAULT 3,
  is_active         BOOLEAN      NOT NULL DEFAULT TRUE,
  created_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  updated_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_products_product_key UNIQUE (product_key),
  CONSTRAINT uq_products_stripe_price UNIQUE (stripe_price_id)
);

CREATE TABLE IF NOT EXISTS customers (
  id                 BIGSERIAL PRIMARY KEY,
  public_id          UUID         NOT NULL DEFAULT gen_random_uuid(),
  email              VARCHAR(320) NOT NULL,
  stripe_customer_id VARCHAR(64)  NULL,
  created_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  updated_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_customers_public_id UNIQUE (public_id),
  CONSTRAINT uq_customers_stripe UNIQUE (stripe_customer_id)
);
CREATE INDEX IF NOT EXISTS idx_customers_email ON customers (email);

CREATE TABLE IF NOT EXISTS licenses (
  id                         BIGSERIAL PRIMARY KEY,
  public_id                  UUID         NOT NULL DEFAULT gen_random_uuid(),
  customer_id                BIGINT       NOT NULL,
  product_id                 BIGINT       NOT NULL,
  license_key_hash           BYTEA        NOT NULL,
  license_key_prefix         CHAR(8)      NOT NULL,
  status                     VARCHAR(16)  NOT NULL DEFAULT 'active',
  stripe_checkout_session_id VARCHAR(128) NULL,
  stripe_payment_intent_id   VARCHAR(128) NULL,
  purchased_at               TIMESTAMPTZ  NOT NULL,
  revoked_at                 TIMESTAMPTZ  NULL,
  created_at                 TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  updated_at                 TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_licenses_public_id UNIQUE (public_id),
  CONSTRAINT uq_licenses_key_hash UNIQUE (license_key_hash),
  CONSTRAINT uq_licenses_checkout UNIQUE (stripe_checkout_session_id)
);
CREATE INDEX IF NOT EXISTS idx_licenses_customer_product ON licenses (customer_id, product_id);
CREATE INDEX IF NOT EXISTS idx_licenses_status_purchased ON licenses (status, purchased_at);

CREATE TABLE IF NOT EXISTS license_activations (
  id                  BIGSERIAL PRIMARY KEY,
  license_id          BIGINT      NOT NULL,
  machine_fingerprint BYTEA       NOT NULL,
  machine_label       VARCHAR(128) NULL,
  app_version         VARCHAR(32)  NULL,
  activated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  last_seen_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deactivated_at      TIMESTAMPTZ NULL,
  CONSTRAINT uq_activation_license_machine UNIQUE (license_id, machine_fingerprint)
);
CREATE INDEX IF NOT EXISTS idx_activations_last_seen ON license_activations (license_id, last_seen_at);

CREATE TABLE IF NOT EXISTS stripe_events (
  id              BIGSERIAL PRIMARY KEY,
  stripe_event_id VARCHAR(128) NOT NULL,
  event_type      VARCHAR(64)  NOT NULL,
  processed_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_stripe_events_event_id UNIQUE (stripe_event_id)
);
CREATE INDEX IF NOT EXISTS idx_stripe_events_type_processed ON stripe_events (event_type, processed_at);

INSERT INTO products (product_key, display_name, stripe_product_id, stripe_price_id, max_activations)
VALUES (
  'unboundos_pro',
  'UnboundOS Pro — Lifetime License',
  'prod_UugEfVRxuBGmrO',
  'price_1TuqraGsA7WZDU3ukFapJcYi',
  3
)
ON CONFLICT (product_key) DO NOTHING;
