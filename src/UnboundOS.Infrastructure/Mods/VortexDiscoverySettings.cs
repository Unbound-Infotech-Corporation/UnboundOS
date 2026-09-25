namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Testable Vortex discovery roots. Default Windows locations stay on
/// unless a test supplies isolated temp folders.
/// </summary>
public sealed record VortexDiscoverySettings
{
    public IReadOnlyList<string> UserDataRoots { get; init; } = [];

    public IReadOnlyList<string> ExtraStagingRoots { get; init; } = [];

    public bool UseDefaultWindowsLocations { get; init; } = true;

    public Func<string?> FindExecutable { get; init; } = VortexLocator.FindExecutable;

    public Func<bool> IsVortexRunning { get; init; } = VortexLocator.IsRunning;

    public Func<string, string, TimeSpan, string?> TryReadState { get; init; } =
        VortexLocator.TryGetState;
}
