namespace UnboundOS.Core.Models;

public sealed class SessionSnapshot
{
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.Now;
    public required string ProfileId { get; init; }
    public IReadOnlyList<TerminatedProcessRecord> TerminatedProcesses { get; init; } = [];
    public IReadOnlyDictionary<string, int> OriginalAdapterMetrics { get; init; } =
        new Dictionary<string, int>();
    public string? OriginalPowerSchemeGuid { get; init; }
    public GamingSkinnySnapshot? Skinny { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class TerminatedProcessRecord
{
    public required string ProcessName { get; init; }
    public int ProcessId { get; init; }
    public string? FileName { get; init; }
}

public sealed class SessionMutationResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Actions { get; init; } = [];
    public SessionSnapshot? Snapshot { get; init; }
}
