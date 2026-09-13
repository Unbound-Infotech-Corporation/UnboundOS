using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DotNetEnv;
using Npgsql;
using Stripe;
using Stripe.Checkout;
using UnboundOS.Licensing.Api;

// Load secrets from repo-root .env if present (never commit that file).
var envPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"));
if (System.IO.File.Exists(envPath))
{
    Env.Load(envPath);
}
else if (System.IO.File.Exists(".env"))
{
    Env.Load(".env");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LicenseStore>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    product = "UnboundOS Licensing API",
    company = "Unbound Infotech Corporation",
    endpoints = new[] { "GET /health", "POST /webhooks/stripe", "POST /v1/activate" }
}));

app.MapGet("/health", async (LicenseStore store, CancellationToken ct) =>
{
    await store.PingAsync(ct);
    return Results.Ok(new { status = "ok" });
});

app.MapPost("/webhooks/stripe", async (HttpRequest request, LicenseStore store, ILogger<Program> logger, CancellationToken ct) =>
{
    var json = await new StreamReader(request.Body).ReadToEndAsync(ct);
    var signature = request.Headers["Stripe-Signature"].ToString();
    var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
    if (string.IsNullOrWhiteSpace(webhookSecret))
    {
        return Results.Problem("STRIPE_WEBHOOK_SECRET is not configured.", statusCode: 500);
    }

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Invalid Stripe webhook signature.");
        return Results.BadRequest();
    }

    if (stripeEvent.Type != EventTypes.CheckoutSessionCompleted)
    {
        return Results.Ok(new { ignored = stripeEvent.Type });
    }

    if (await store.HasProcessedEventAsync(stripeEvent.Id, ct))
    {
        return Results.Ok(new { duplicate = true });
    }

    if (stripeEvent.Data.Object is not Session session)
    {
        return Results.BadRequest();
    }

    var email = session.CustomerDetails?.Email
                ?? session.CustomerEmail
                ?? throw new InvalidOperationException("Checkout session has no customer email.");

    var stripeCustomerId = session.CustomerId;
    var checkoutSessionId = session.Id;
    var paymentIntentId = session.PaymentIntentId;

    var issued = await store.IssueLifetimeLicenseAsync(
        email,
        stripeCustomerId,
        checkoutSessionId,
        paymentIntentId,
        stripeEvent.Id,
        stripeEvent.Type,
        ct);

    // Raw key is returned only to the webhook caller (your mailer / success page later).
    // It is never stored in PlanetScale — only the SHA-256 hash is.
    logger.LogInformation(
        "Issued license {Prefix}… for {Email} (checkout {Checkout})",
        issued.KeyPrefix,
        email,
        checkoutSessionId);

    return Results.Ok(new
    {
        ok = true,
        email,
        licenseKey = issued.RawKey,
        keyPrefix = issued.KeyPrefix,
        message = "Deliver this licenseKey to the buyer once (email/success page). It is not stored in cleartext."
    });
});

app.MapPost("/v1/activate", async (ActivateRequest body, LicenseStore store, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(body.LicenseKey) || string.IsNullOrWhiteSpace(body.MachineFingerprint))
    {
        return Results.BadRequest(new { ok = false, error = "licenseKey and machineFingerprint are required." });
    }

    var result = await store.ActivateAsync(
        body.LicenseKey.Trim(),
        body.MachineFingerprint.Trim(),
        body.MachineLabel,
        body.AppVersion,
        ct);

    return result.Ok
        ? Results.Ok(new { ok = true, tier = "pro", activationsUsed = result.ActivationsUsed, maxActivations = result.MaxActivations })
        : Results.Json(new { ok = false, error = result.Error }, statusCode: result.StatusCode);
});

app.Run();

namespace UnboundOS.Licensing.Api
{
    public sealed record ActivateRequest(
        string LicenseKey,
        string MachineFingerprint,
        string? MachineLabel,
        string? AppVersion);

    public sealed record IssuedLicense(string RawKey, string KeyPrefix);

    public sealed record ActivateResult(bool Ok, string? Error, int StatusCode, int ActivationsUsed, int MaxActivations)
    {
        public static ActivateResult Fail(string error, int status) => new(false, error, status, 0, 0);
        public static ActivateResult Success(int used, int max) => new(true, null, 200, used, max);
    }

    public sealed class LicenseStore
    {
        private readonly string _connectionString;
        private const string ProductKey = "unboundos_pro";

        public LicenseStore()
        {
            var host = Require("DATABASE_HOST");
            var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
            var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "postgres";
            var username = Require("DATABASE_USERNAME");
            var password = Require("DATABASE_PASSWORD");

            // PlanetScale Postgres requires SSL.
            _connectionString =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=VerifyFull;Timeout=15";

            StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        }

        public async Task PingAsync(CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            _ = await cmd.ExecuteScalarAsync(ct);
        }

        public async Task<bool> HasProcessedEventAsync(string stripeEventId, CancellationToken ct)
        {
            await using var conn = await OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT 1 FROM stripe_events WHERE stripe_event_id = @id LIMIT 1", conn);
            cmd.Parameters.AddWithValue("id", stripeEventId);
            return await cmd.ExecuteScalarAsync(ct) is not null;
        }

        public async Task<IssuedLicense> IssueLifetimeLicenseAsync(
            string email,
            string? stripeCustomerId,
            string checkoutSessionId,
            string? paymentIntentId,
            string stripeEventId,
            string eventType,
            CancellationToken ct)
        {
            var rawKey = GenerateLicenseKey();
            var hash = Sha256(rawKey);
            var prefix = rawKey.Length >= 8 ? rawKey[..8] : rawKey;

            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            await using (var eventCmd = new NpgsqlCommand(
                             """
                             INSERT INTO stripe_events (stripe_event_id, event_type)
                             VALUES (@id, @type)
                             ON CONFLICT (stripe_event_id) DO NOTHING
                             """, conn, tx))
            {
                eventCmd.Parameters.AddWithValue("id", stripeEventId);
                eventCmd.Parameters.AddWithValue("type", eventType);
                if (await eventCmd.ExecuteNonQueryAsync(ct) == 0)
                {
                    // Another worker already processed this event.
                    await tx.RollbackAsync(ct);
                    return new IssuedLicense("(already-issued)", prefix);
                }
            }

            long customerId;
            if (!string.IsNullOrWhiteSpace(stripeCustomerId))
            {
                await using var upsert = new NpgsqlCommand(
                    """
                    INSERT INTO customers (email, stripe_customer_id)
                    VALUES (@email, @stripe_customer_id)
                    ON CONFLICT (stripe_customer_id) DO UPDATE
                      SET email = EXCLUDED.email, updated_at = NOW()
                    RETURNING id
                    """, conn, tx);
                upsert.Parameters.AddWithValue("email", email);
                upsert.Parameters.AddWithValue("stripe_customer_id", stripeCustomerId);
                customerId = Convert.ToInt64(await upsert.ExecuteScalarAsync(ct));
            }
            else
            {
                await using var find = new NpgsqlCommand(
                    "SELECT id FROM customers WHERE email = @email ORDER BY id DESC LIMIT 1", conn, tx);
                find.Parameters.AddWithValue("email", email);
                var existing = await find.ExecuteScalarAsync(ct);
                if (existing is not null)
                {
                    customerId = Convert.ToInt64(existing);
                }
                else
                {
                    await using var insert = new NpgsqlCommand(
                        """
                        INSERT INTO customers (email, stripe_customer_id)
                        VALUES (@email, NULL)
                        RETURNING id
                        """, conn, tx);
                    insert.Parameters.AddWithValue("email", email);
                    customerId = Convert.ToInt64(await insert.ExecuteScalarAsync(ct));
                }
            }

            long productId;
            await using (var productCmd = new NpgsqlCommand(
                             "SELECT id FROM products WHERE product_key = @key AND is_active = TRUE LIMIT 1",
                             conn, tx))
            {
                productCmd.Parameters.AddWithValue("key", ProductKey);
                productId = Convert.ToInt64(await productCmd.ExecuteScalarAsync(ct)
                                            ?? throw new InvalidOperationException("unboundos_pro product missing."));
            }

            await using (var licenseCmd = new NpgsqlCommand(
                             """
                             INSERT INTO licenses (
                               customer_id, product_id, license_key_hash, license_key_prefix,
                               status, stripe_checkout_session_id, stripe_payment_intent_id, purchased_at)
                             VALUES (
                               @customer_id, @product_id, @hash, @prefix,
                               'active', @checkout, @payment_intent, NOW())
                             ON CONFLICT (stripe_checkout_session_id) DO NOTHING
                             """, conn, tx))
            {
                licenseCmd.Parameters.AddWithValue("customer_id", customerId);
                licenseCmd.Parameters.AddWithValue("product_id", productId);
                licenseCmd.Parameters.AddWithValue("hash", hash);
                licenseCmd.Parameters.AddWithValue("prefix", prefix);
                licenseCmd.Parameters.AddWithValue("checkout", checkoutSessionId);
                licenseCmd.Parameters.AddWithValue("payment_intent", (object?)paymentIntentId ?? DBNull.Value);
                await licenseCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
            return new IssuedLicense(rawKey, prefix);
        }

        public async Task<ActivateResult> ActivateAsync(
            string licenseKey,
            string machineFingerprint,
            string? machineLabel,
            string? appVersion,
            CancellationToken ct)
        {
            var hash = Sha256(NormalizeKey(licenseKey));
            var fingerprint = Sha256Bytes(machineFingerprint);

            await using var conn = await OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            long licenseId;
            string status;
            int maxActivations;

            await using (var find = new NpgsqlCommand(
                             """
                             SELECT l.id, l.status, p.max_activations
                             FROM licenses l
                             JOIN products p ON p.id = l.product_id
                             WHERE l.license_key_hash = @hash
                             LIMIT 1
                             """, conn, tx))
            {
                find.Parameters.AddWithValue("hash", hash);
                await using var reader = await find.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct))
                {
                    return ActivateResult.Fail("Invalid license key.", 404);
                }

                licenseId = reader.GetInt64(0);
                status = reader.GetString(1);
                maxActivations = reader.GetInt16(2);
            }

            if (!string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            {
                return ActivateResult.Fail($"License is {status}.", 403);
            }

            // Already activated on this machine?
            await using (var existing = new NpgsqlCommand(
                             """
                             SELECT id FROM license_activations
                             WHERE license_id = @license_id
                               AND machine_fingerprint = @fp
                               AND deactivated_at IS NULL
                             LIMIT 1
                             """, conn, tx))
            {
                existing.Parameters.AddWithValue("license_id", licenseId);
                existing.Parameters.AddWithValue("fp", fingerprint);
                var existingId = await existing.ExecuteScalarAsync(ct);
                if (existingId is not null)
                {
                    await using var touch = new NpgsqlCommand(
                        """
                        UPDATE license_activations
                        SET last_seen_at = NOW(),
                            machine_label = COALESCE(@label, machine_label),
                            app_version = COALESCE(@version, app_version)
                        WHERE id = @id
                        """, conn, tx);
                    touch.Parameters.AddWithValue("id", Convert.ToInt64(existingId));
                    touch.Parameters.AddWithValue("label", (object?)machineLabel ?? DBNull.Value);
                    touch.Parameters.AddWithValue("version", (object?)appVersion ?? DBNull.Value);
                    await touch.ExecuteNonQueryAsync(ct);

                    var usedExisting = await CountActiveAsync(conn, tx, licenseId, ct);
                    await tx.CommitAsync(ct);
                    return ActivateResult.Success(usedExisting, maxActivations);
                }
            }

            var used = await CountActiveAsync(conn, tx, licenseId, ct);
            if (used >= maxActivations)
            {
                return ActivateResult.Fail($"Activation limit reached ({maxActivations}).", 403);
            }

            await using (var insert = new NpgsqlCommand(
                             """
                             INSERT INTO license_activations (
                               license_id, machine_fingerprint, machine_label, app_version)
                             VALUES (@license_id, @fp, @label, @version)
                             """, conn, tx))
            {
                insert.Parameters.AddWithValue("license_id", licenseId);
                insert.Parameters.AddWithValue("fp", fingerprint);
                insert.Parameters.AddWithValue("label", (object?)machineLabel ?? DBNull.Value);
                insert.Parameters.AddWithValue("version", (object?)appVersion ?? DBNull.Value);
                await insert.ExecuteNonQueryAsync(ct);
            }

            used = await CountActiveAsync(conn, tx, licenseId, ct);
            await tx.CommitAsync(ct);
            return ActivateResult.Success(used, maxActivations);
        }

        private static async Task<int> CountActiveAsync(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            long licenseId,
            CancellationToken ct)
        {
            await using var cmd = new NpgsqlCommand(
                """
                SELECT COUNT(*)::int FROM license_activations
                WHERE license_id = @id AND deactivated_at IS NULL
                """, conn, tx);
            cmd.Parameters.AddWithValue("id", licenseId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }

        private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct)
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            return conn;
        }

        private static string Require(string name) =>
            Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"{name} is missing. Copy .env.example to .env and fill it in.");

        private static string GenerateLicenseKey()
        {
            Span<byte> bytes = stackalloc byte[10];
            RandomNumberGenerator.Fill(bytes);
            var token = Convert.ToHexString(bytes);
            return $"UBOS-{token[..4]}-{token[4..8]}-{token[8..12]}-{token[12..16]}-{token[16..20]}";
        }

        private static string NormalizeKey(string key) =>
            key.Trim().ToUpperInvariant().Replace(" ", "", StringComparison.Ordinal);

        private static byte[] Sha256(string value)
        {
            var normalized = NormalizeKey(value);
            return SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        }

        private static byte[] Sha256Bytes(string value) =>
            SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
    }
}
