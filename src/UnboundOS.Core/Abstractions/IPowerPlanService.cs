namespace UnboundOS.Core.Abstractions;

public interface IPowerPlanService
{
    Task<string?> GetActiveSchemeGuidAsync(CancellationToken ct = default);
    Task<(bool Succeeded, string Message)> ActivateHighPerformanceAsync(CancellationToken ct = default);
    Task<(bool Succeeded, string Message)> RestoreSchemeAsync(string? schemeGuid, CancellationToken ct = default);
}
