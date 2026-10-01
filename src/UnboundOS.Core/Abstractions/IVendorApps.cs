using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IVendorAppCatalog
{
    Task<IReadOnlyList<VendorApp>> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface IVendorAppLauncher
{
    Task<ToolLaunchResult> LaunchAsync(VendorApp app, CancellationToken cancellationToken = default);

    Task<ToolLaunchResult> OpenGetPathAsync(VendorApp app, CancellationToken cancellationToken = default);
}
