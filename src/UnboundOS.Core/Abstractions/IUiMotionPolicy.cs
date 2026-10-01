using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

/// <summary>
/// Resolves whether the shell may play interface motion.
/// Honors the Settings toggle, Windows animation preference, and a live session.
/// </summary>
public interface IUiMotionPolicy
{
    bool UserWantsMotion { get; }

    bool AllowMotion { get; }

    bool IsSessionLive { get; }

    MotionSuppression Suppression { get; }

    string StatusText { get; }

    event EventHandler? Changed;

    Task InitializeAsync(CancellationToken ct = default);

    Task SetUserWantsMotionAsync(bool enabled, CancellationToken ct = default);
}
