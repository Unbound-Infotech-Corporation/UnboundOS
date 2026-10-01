namespace UnboundOS.Infrastructure.Library;

/// <summary>
/// Testable Steam library roots. Default Windows locations stay on
/// unless a test supplies isolated steamapps folders.
/// </summary>
public sealed record SteamLibraryDiscoverySettings
{
    public bool UseDefaultWindowsLocations { get; init; } = true;

    public IReadOnlyList<string> ExtraLibraryFolders { get; init; } = [];
}
