namespace UnboundOS.Core.Models;

public sealed class TuningSnapshot
{
    public IReadOnlyList<RegistryValueSnapshot> RegistryValues { get; init; } = [];
    public IReadOnlyList<ServiceStateSnapshot> Services { get; init; } = [];
    public string? AppCompatExePath { get; init; }
    public string? AppCompatOriginalValue { get; init; }
    public bool AppCompatValueExisted { get; init; }
    public IReadOnlyList<int> PowerThrottleDisabledPids { get; init; } = [];
}

public sealed record RegistryValueSnapshot(
    string Hive,
    string KeyPath,
    string ValueName,
    string Kind,
    string? OriginalValue,
    bool Existed);

public sealed record ServiceStateSnapshot(
    string ServiceName,
    bool WasRunning,
    bool StoppedBySession);
