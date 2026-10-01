using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

public sealed class StartupAuditService : IStartupAuditService
{
    private readonly IStartupInventory _inventory;
    private readonly IStartupAllowlistStore _store;
    private readonly IStartupMutator _mutator;

    public StartupAuditService(
        IStartupInventory inventory,
        IStartupAllowlistStore store,
        IStartupMutator mutator)
    {
        _inventory = inventory;
        _store = store;
        _mutator = mutator;
    }

    public async Task<StartupAuditReport> AuditAsync(CancellationToken ct = default)
    {
        var allowlist = await _store.LoadAsync(ct).ConfigureAwait(false);
        var pinned = new HashSet<string>(allowlist.PinnedIds, StringComparer.OrdinalIgnoreCase);
        var entries = _inventory.List()
            .Select(candidate => StartupPolicy.Classify(candidate, pinned))
            .OrderBy(entry => entry.Disposition)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var review = entries.Count(entry => entry.Disposition == StartupDisposition.Review);
        var protectedCount = entries.Count(entry => entry.Disposition == StartupDisposition.Protected);
        var summary =
            $"{entries.Count} startup items. {protectedCount} protected. {review} to review. Pin to keep; apply recommended only drops user Run / Startup folder Review items.";

        return new StartupAuditReport(entries, review, protectedCount, summary);
    }

    public async Task PinAsync(string id, bool pinned, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Startup id is required.", nameof(id));
        }

        var current = await _store.LoadAsync(ct).ConfigureAwait(false);
        var set = new HashSet<string>(current.PinnedIds, StringComparer.OrdinalIgnoreCase);
        if (pinned)
        {
            set.Add(id);
        }
        else
        {
            set.Remove(id);
        }

        await _store.SaveAsync(current with { PinnedIds = set.OrderBy(x => x, StringComparer.Ordinal).ToArray() }, ct)
            .ConfigureAwait(false);
    }

    public async Task<string> ApplyRecommendedAsync(CancellationToken ct = default)
    {
        var report = await AuditAsync(ct).ConfigureAwait(false);
        var applied = 0;
        var skipped = 0;
        foreach (var entry in report.Entries.Where(item => item.CanApplyDisable))
        {
            ct.ThrowIfCancellationRequested();
            var message = await _mutator.DisableAsync(entry, ct).ConfigureAwait(false);
            if (message.StartsWith("Disabled", StringComparison.OrdinalIgnoreCase))
            {
                applied++;
            }
            else
            {
                skipped++;
            }
        }

        return applied == 0 && skipped == 0
            ? "Nothing to apply. Review items in HKLM or Scheduled Tasks stay report-only in this slice."
            : $"Disabled {applied} user startup item(s). {skipped} skipped. Protected and pinned items were not touched.";
    }
}
