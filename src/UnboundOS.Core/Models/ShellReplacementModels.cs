namespace UnboundOS.Core.Models;

public sealed record FileOpResult(bool Ok, string Message);

public sealed record RunningApp(
    string Id,
    string Title,
    string ProcessName,
    int ProcessId,
    bool IsFullscreenLikely);

public sealed record LauncherApp(
    string Id,
    string Title,
    string Source,
    string Target,
    string Kind);

public sealed record TrayApp(
    string Id,
    string Title,
    string ProcessName,
    bool IsRunning);

public sealed record OsSettingsEntry(
    string Id,
    string Title,
    string Hint,
    string Uri,
    bool HasFirstParty);

public sealed record PowerPlanInfo(string Guid, string Name, bool IsActive);

public sealed record InstalledAppInfo(
    string Id,
    string DisplayName,
    string? UninstallCommand,
    string Publisher);

public sealed record OsAdapterStatus(
    string Name,
    string Type,
    bool IsUp,
    string Detail);

public sealed record StorageVolumeInfo(
    string Name,
    string Label,
    long FreeBytes,
    long TotalBytes,
    bool IsReady,
    bool CanEject);
