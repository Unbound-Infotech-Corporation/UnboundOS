namespace UnboundOS.Core.Models;

public sealed class SystemTelemetry
{
    public double CpuUsagePercent { get; init; }
    public double MemoryUsedGb { get; init; }
    public double MemoryTotalGb { get; init; }
    public int ProcessCount { get; init; }
    public int BackgroundSuspectCount { get; init; }
    public DateTimeOffset SampledAt { get; init; } = DateTimeOffset.Now;
}
