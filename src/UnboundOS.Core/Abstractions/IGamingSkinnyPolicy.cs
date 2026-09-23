using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

/// <summary>
/// Session-scoped Game Mode / Game DVR / visual-effects posture.
/// Never disables Defender, Windows Update, or VBS.
/// HAGS is an informed toggle — not forced on enter.
/// </summary>
public interface IGamingSkinnyPolicy
{
    Task<GamingSkinnySnapshot> ApplyAsync(SessionProfile profile, CancellationToken ct = default);

    Task<(bool Succeeded, string Message)> RestoreAsync(
        GamingSkinnySnapshot? snapshot,
        CancellationToken ct = default);

    Task<(bool Succeeded, string Message)> TrySetHagsAsync(bool enabled, CancellationToken ct = default);
}
