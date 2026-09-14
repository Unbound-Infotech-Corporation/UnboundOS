namespace UnboundOS.Core.Models;

public enum ModProvider
{
    Unknown,
    SteamWorkshop,
    Local,
    NexusMods,
    Thunderstore,
    BuiltIn
}

public enum ModManagementLevel
{
    DiscoveryOnly,
    ExternalHandoff,
    ProfileAware,
    LoadOrder,
    Full
}

public enum ModUpdateState
{
    Unknown,
    Current,
    UpdateAvailable,
    Downloading,
    Missing,
    Failed
}

public sealed record ModCapabilities(
    bool CanDiscover,
    bool CanToggle,
    bool CanReorder,
    bool CanResolveDependencies,
    bool CanInstall,
    bool CanUninstall,
    ModManagementLevel ManagementLevel,
    string Explanation)
{
    public static ModCapabilities SteamDiscoveryOnly { get; } = new(
        true, false, false, false, false, false,
        ModManagementLevel.ExternalHandoff,
        "Workshop files are discovered locally. Subscribe, install, update, and remove through Steam.");

    public static ModCapabilities VortexDiscoveryOnly { get; } = new(
        true, false, false, false, false, false,
        ModManagementLevel.ExternalHandoff,
        "Vortex owns Nexus install, enable, and deploy. UnboundOS only discovers local staging and can open Vortex.");
}

public sealed record ModGame(
    string GameId,
    string DisplayName,
    string? InstallPath,
    ModProvider Provider,
    ModCapabilities Capabilities,
    IReadOnlyList<InstalledMod> Mods,
    string? AdapterId = null,
    string? SteamAppId = null,
    string? VortexGameId = null,
    string? VortexProfileId = null);

public sealed record InstalledMod(
    string Id,
    string GameId,
    string DisplayName,
    ModProvider Provider,
    string InstallPath,
    bool IsEnabled,
    int LoadOrder,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> Conflicts,
    ModUpdateState UpdateState,
    string? Version = null,
    DateTimeOffset? UpdatedAt = null,
    string? WorkshopUrl = null,
    string? ProfileId = null,
    long SizeBytes = 0);

public sealed record ModProfile(
    string Id,
    string GameId,
    string Name,
    IReadOnlyList<ModProfileEntry> Entries,
    DateTimeOffset UpdatedAt,
    string? Description = null);

public sealed record ModProfileEntry(
    string ModId,
    bool IsEnabled,
    int LoadOrder);

public sealed record ModOperationResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<string> Actions,
    string? BackupPath = null)
{
    public static ModOperationResult Unsupported(string explanation) =>
        new(false, explanation, []);
}

public sealed record ModConfigurationBackup(
    string Id,
    string GameId,
    string AdapterId,
    string BackupPath,
    DateTimeOffset CreatedAt,
    string Reason);
