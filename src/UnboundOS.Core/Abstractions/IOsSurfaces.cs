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

public interface ISetupCleanup
{
    Task<SetupCleanupResult> CleanLeftoversAsync(CancellationToken cancellationToken = default);
}
