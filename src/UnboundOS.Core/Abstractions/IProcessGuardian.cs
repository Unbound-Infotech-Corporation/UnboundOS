using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IProcessGuardian
{
    Task<IReadOnlyList<string>> ListRunningSuspectsAsync(IEnumerable<string> denylist, CancellationToken ct = default);
    Task<IReadOnlyList<TerminatedProcessRecord>> TerminateAsync(IEnumerable<string> processNames, IEnumerable<string> protectNames, CancellationToken ct = default);
    int CountBackgroundSuspects(IEnumerable<string> denylist);
}
