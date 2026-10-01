namespace UnboundOS.Core.Models;

/// <summary>
/// Xbox mode / Full Screen Experience home-app health. Microsoft does not
/// document third-party registration, so every state except Selected is a
/// graceful fallback — never a hard install failure.
/// </summary>
public enum XboxModeHomeState
{
    Unavailable = 0,
    PackageMissing = 1,
    RegisteredNotSelected = 2,
    Selected = 3,
    FallbackRun = 4
}

public sealed record XboxModeHomeSnapshot(
    XboxModeHomeState State,
    bool Wanted,
    bool FullscreenAtStartup,
    bool PackagePresent,
    bool UserConfirmedSelected,
    string Detail);

public sealed record XboxModeHomePreference(
    bool Wanted,
    bool FullscreenAtStartup,
    string State,
    string Detail,
    bool UserConfirmedSelected,
    DateTimeOffset UpdatedUtc);
