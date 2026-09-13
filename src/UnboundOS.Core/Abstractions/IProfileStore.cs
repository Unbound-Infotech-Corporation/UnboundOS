using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IProfileStore
{
    Task<IReadOnlyList<SessionProfile>> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(IEnumerable<SessionProfile> profiles, CancellationToken ct = default);
    Task EnsureDefaultsAsync(CancellationToken ct = default);
}
