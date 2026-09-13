using System.Diagnostics;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Telemetry;

public sealed class WindowsTelemetryService : ITelemetryService
{
    private readonly IProcessGuardian _guardian;
    private readonly PerformanceCounter? _cpu;

    public WindowsTelemetryService(IProcessGuardian guardian)
    {
        _guardian = guardian;
        try
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = _cpu.NextValue();
        }
        catch
        {
            _cpu = null;
        }
    }

    public Task<SystemTelemetry> SampleAsync(IEnumerable<string> suspectProcessNames, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        double cpu = 0;
        if (_cpu is not null)
        {
            try { cpu = _cpu.NextValue(); }
            catch { cpu = 0; }
        }

        var gcInfo = GC.GetGCMemoryInfo();
        var total = gcInfo.TotalAvailableMemoryBytes / (1024d * 1024d * 1024d);
        var used = gcInfo.MemoryLoadBytes / (1024d * 1024d * 1024d);
        if (total <= 0)
        {
            total = 16;
            used = Environment.WorkingSet / (1024d * 1024d * 1024d);
        }

        var processCount = System.Diagnostics.Process.GetProcesses().Length;
        var suspects = _guardian.CountBackgroundSuspects(suspectProcessNames);

        return Task.FromResult(new SystemTelemetry
        {
            CpuUsagePercent = Math.Clamp(cpu, 0, 100),
            MemoryUsedGb = Math.Round(used, 2),
            MemoryTotalGb = Math.Round(total, 2),
            ProcessCount = processCount,
            BackgroundSuspectCount = suspects,
            SampledAt = DateTimeOffset.Now
        });
    }
}
