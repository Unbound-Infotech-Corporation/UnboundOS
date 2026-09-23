using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

/// <summary>
/// Configures Windows Update for monthly quality/LCU from Microsoft while
/// deferring feature and optional preview churn. Never hosts .msu/.cab.
/// Session enter must not call this.
/// </summary>
public interface IUpdateGuardPolicy
{
    string ShellVersion { get; }

    UpdateGuardTarget DescribeTarget();

    Task<(bool Succeeded, string Message)> TryApplyUpdateGuardAsync(CancellationToken ct = default);

    Task<(bool Succeeded, string Message)> TryClearUpdateGuardAsync(CancellationToken ct = default);

    Task<UpdateGuardStatus> ProbeQualityStatusAsync(CancellationToken ct = default);

    Task<(bool Succeeded, string Message)> OpenWindowsUpdateSettingsAsync(CancellationToken ct = default);
}
