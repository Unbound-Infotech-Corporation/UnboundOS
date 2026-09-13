using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface INetworkDirector
{
    Task<IReadOnlyList<NetworkAdapterInfo>> ListAdaptersAsync(CancellationToken ct = default);
    NetworkPlan RecommendPlan(IReadOnlyList<NetworkAdapterInfo> adapters, SessionProfile profile);
    Task<IReadOnlyDictionary<string, int>> CaptureMetricsAsync(CancellationToken ct = default);
    Task<NetworkMutationResult> ApplyTrafficSeparationAsync(
        string? gameAdapterId,
        string? streamAdapterId,
        bool allowElevation = true,
        CancellationToken ct = default);
    Task<NetworkMutationResult> RestoreMetricsAsync(
        IReadOnlyDictionary<string, int> originalMetrics,
        bool allowElevation = true,
        CancellationToken ct = default);
}
