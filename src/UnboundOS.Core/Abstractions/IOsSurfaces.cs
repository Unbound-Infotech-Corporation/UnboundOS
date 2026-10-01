using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IFileBrowser
{
    FileBrowsePage OpenPlaces();

    FileBrowsePage OpenPath(string path);

    FileOpResult Copy(string source, string destDir);

    FileOpResult Move(string source, string destDir);

    FileOpResult Delete(string path);

    FileOpResult Eject(string root);

    FileOpResult OpenItem(string path);

    FileOpResult OpenWith(string path);
}

public interface IHardwareInventory
{
    Task<HardwareSnapshot> SampleAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional live thermal readout for leftover Home extras. Empty when sensors are absent.
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
