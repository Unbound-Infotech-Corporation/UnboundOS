using UnboundOS.Infrastructure.Mods;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Testable desktop-tool roots. Default Windows locations stay on
/// unless a test supplies isolated executables.
/// </summary>
public sealed record DesktopToolDiscoverySettings
{
    public bool UseDefaultWindowsLocations { get; init; } = true;

    public IReadOnlyDictionary<string, string?> ForcedExecutables { get; init; } =
        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public Func<string?> FindVortex { get; init; } = VortexLocator.FindExecutable;
}
