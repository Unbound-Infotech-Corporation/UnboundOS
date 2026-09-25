using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IStartupInventory
{
    IReadOnlyList<StartupCandidate> List();
}

public interface IStartupAllowlistStore
{
    Task<StartupAllowlist> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(StartupAllowlist allowlist, CancellationToken ct = default);
}

public interface IStartupMutator
{
    Task<string> DisableAsync(StartupEntry entry, CancellationToken ct = default);
}

public interface IStartupAuditService
{
    Task<StartupAuditReport> AuditAsync(CancellationToken ct = default);

    Task PinAsync(string id, bool pinned, CancellationToken ct = default);

    Task<string> ApplyRecommendedAsync(CancellationToken ct = default);
}
