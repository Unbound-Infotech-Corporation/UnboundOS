-- UnboundOS Pro — license & activation schema (MySQL 8 / InnoDB / utf8mb4)
-- Workload: low writes (Stripe webhooks + activations), point lookups by key hash / machine id.
-- Hosting recommendation: PlanetScale (Vitess). Self-hosted MySQL 8+ is fine too.
--
-- Money stays in Stripe; this DB tracks entitlement + device activation only.

CREATE DATABASE IF NOT EXISTS unbound_licenses
  DEFAULT CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE unbound_licenses;

-- ---------------------------------------------------------------------------
-- Products (mirror of sellable SKUs; Stripe remains source of truth for price)
-- ---------------------------------------------------------------------------
CREATE TABLE products (
  id            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
  product_key   VARCHAR(64)     NOT NULL,          -- e.g. unboundos_pro
  display_name  VARCHAR(128)    NOT NULL,
  stripe_product_id VARCHAR(64) NULL,
  stripe_price_id   VARCHAR(64) NULL,
  max_activations TINYINT UNSIGNED NOT NULL DEFAULT 3,
  is_active     TINYINT(1)      NOT NULL DEFAULT 1,
  created_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uq_products_product_key (product_key),
  UNIQUE KEY uq_products_stripe_price (stripe_price_id)
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Customers (created from Stripe Checkout customer)
-- ---------------------------------------------------------------------------
CREATE TABLE customers (
  id                 BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
  public_id          BINARY(16)      NOT NULL,       -- app-facing UUID (store with UUID_TO_BIN)
  email              VARCHAR(320)    NOT NULL,
  stripe_customer_id VARCHAR(64)     NULL,
  created_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uq_customers_public_id (public_id),
  UNIQUE KEY uq_customers_stripe (stripe_customer_id),
  KEY idx_customers_email (email)
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Licenses
-- Never store the raw key long-term. Show once at purchase; persist SHA-256 only.
-- license_key_prefix helps support lookups without exposing the full secret.
-- ---------------------------------------------------------------------------
CREATE TABLE licenses (
  id                 BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
  public_id          BINARY(16)      NOT NULL,
  customer_id        BIGINT UNSIGNED NOT NULL,
  product_id         BIGINT UNSIGNED NOT NULL,
  license_key_hash   BINARY(32)      NOT NULL,       -- SHA-256 of normalized key
  license_key_prefix CHAR(8)         NOT NULL,       -- first 8 of displayed key
  status             VARCHAR(16)     NOT NULL DEFAULT 'active',
  -- status values (app-enforced): active | revoked | refunded | expired
  stripe_checkout_session_id VARCHAR(128) NULL,
  stripe_payment_intent_id   VARCHAR(128) NULL,
  purchased_at       DATETIME        NOT NULL,
  revoked_at         DATETIME        NULL,
  created_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uq_licenses_public_id (public_id),
  UNIQUE KEY uq_licenses_key_hash (license_key_hash),
  UNIQUE KEY uq_licenses_checkout (stripe_checkout_session_id),
  KEY idx_licenses_customer_product (customer_id, product_id),
  KEY idx_licenses_status_purchased (status, purchased_at),
  CONSTRAINT fk_licenses_customer
    FOREIGN KEY (customer_id) REFERENCES customers (id),
  CONSTRAINT fk_licenses_product
    FOREIGN KEY (product_id) REFERENCES products (id)
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Device activations (hardware / install binding)
-- machine_fingerprint = app-derived hash (not raw HWID PII in cleartext)
-- ---------------------------------------------------------------------------
CREATE TABLE license_activations (
  id                   BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
  license_id           BIGINT UNSIGNED NOT NULL,
  machine_fingerprint  BINARY(32)      NOT NULL,
  machine_label        VARCHAR(128)    NULL,       -- e.g. "DESKTOP-ABC"
  app_version          VARCHAR(32)     NULL,
  activated_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  last_seen_at         DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  deactivated_at       DATETIME        NULL,
  UNIQUE KEY uq_activation_license_machine (license_id, machine_fingerprint),
  KEY idx_activations_last_seen (license_id, last_seen_at),
  CONSTRAINT fk_activations_license
    FOREIGN KEY (license_id) REFERENCES licenses (id)
) ENGINE=InnoDB;

-- ---------------------------------------------------------------------------
-- Stripe webhook idempotency (prevent double-issue on retries)
-- ---------------------------------------------------------------------------
CREATE TABLE stripe_events (
  id              BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
  stripe_event_id VARCHAR(128)    NOT NULL,
  event_type      VARCHAR(64)     NOT NULL,
  processed_at    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_stripe_events_event_id (stripe_event_id),
  KEY idx_stripe_events_type_processed (event_type, processed_at)
) ENGINE=InnoDB;

-- Seed UnboundOS Pro (IDs from live Stripe account created earlier in this project)
INSERT INTO products (product_key, display_name, stripe_product_id, stripe_price_id, max_activations)
VALUES (
  'unboundos_pro',
  'UnboundOS Pro — Lifetime License',
  'prod_UugEfVRxuBGmrO',
  'price_1TuqraGsA7WZDU3ukFapJcYi',
  3
);
