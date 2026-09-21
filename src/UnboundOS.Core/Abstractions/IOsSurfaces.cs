using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IFileBrowser
{
    FileBrowsePage OpenPlaces();

    FileBrowsePage OpenPath(string path);
}

public interface IHardwareInventory
{
    Task<HardwareSnapshot> SampleAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional live thermal readout for the Home HUD. Empty when sensors are absent.
/// Must not invent temperatures.
/// </summary>
public interface IThermalProbe
{
    Task<ThermalSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}

public interface ISetupCleanup
{
    Task<SetupCleanupResult> CleanLeftoversAsync(CancellationToken cancellationToken = default);
}
