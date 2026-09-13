using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface ISessionEngine
{
    SessionState State { get; }
    SessionProfile? ActiveProfile { get; }
    SessionSnapshot? LastSnapshot { get; }
    event EventHandler<SessionState>? StateChanged;

    Task<SessionMutationResult> EnterAsync(SessionProfile profile, CancellationToken ct = default);
    Task<SessionMutationResult> ExitAsync(CancellationToken ct = default);
}
