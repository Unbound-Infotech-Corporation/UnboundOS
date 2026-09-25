namespace UnboundOS.Core.Models;

public enum StartupSource
{
    RunKeyUser = 0,
    RunKeyMachine = 1,
    StartupFolder = 2,
    ScheduledTask = 3,
    Overlay = 4
}

public enum StartupDisposition
{
    Protected = 0,
    Pinned = 1,
    Keep = 2,
    Review = 3
}

public sealed record StartupCandidate(
    string Id,
    StartupSource Source,
    string Name,
    string Command,
    string Location);

public sealed record StartupEntry(
    string Id,
    StartupSource Source,
    string Name,
    string Command,
    string Location,
    StartupDisposition Disposition,
    string Reason,
    bool IsPinned)
{
    public string SourceLabel => Source switch
    {
        StartupSource.RunKeyUser => "RUN (USER)",
        StartupSource.RunKeyMachine => "RUN (MACHINE)",
        StartupSource.StartupFolder => "STARTUP FOLDER",
        StartupSource.ScheduledTask => "TASK",
        StartupSource.Overlay => "OVERLAY",
        _ => "STARTUP"
    };

    public string DispositionLabel => Disposition switch
    {
        StartupDisposition.Protected => "PROTECTED",
        StartupDisposition.Pinned => "PINNED",
        StartupDisposition.Keep => "KEEP",
        StartupDisposition.Review => "REVIEW",
        _ => "KEEP"
    };

    public bool CanApplyDisable =>
        Disposition == StartupDisposition.Review &&
        Source is StartupSource.RunKeyUser or StartupSource.StartupFolder;
}

public sealed record StartupAllowlist
{
    public IReadOnlyList<string> PinnedIds { get; init; } = [];

    public static StartupAllowlist Empty { get; } = new();
}

public sealed record StartupAuditReport(
    IReadOnlyList<StartupEntry> Entries,
    int ReviewCount,
    int ProtectedCount,
    string Summary);
