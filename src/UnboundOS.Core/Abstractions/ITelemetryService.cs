using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface ITelemetryService
{
    Task<SystemTelemetry> SampleAsync(IEnumerable<string> suspectProcessNames, CancellationToken ct = default);
}
