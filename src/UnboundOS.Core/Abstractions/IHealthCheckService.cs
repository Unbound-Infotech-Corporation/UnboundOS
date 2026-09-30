using UnboundOS.Core.Diagnostics;

namespace UnboundOS.Core.Abstractions;

public interface IHealthCheckService
{
    Task<HealthReport> RunAsync(CancellationToken cancellationToken = default);
}
