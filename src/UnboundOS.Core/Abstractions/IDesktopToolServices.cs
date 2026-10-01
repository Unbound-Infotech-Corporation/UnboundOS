using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IDesktopToolCatalog
{
    Task<IReadOnlyList<DesktopTool>> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface IDesktopToolLauncher
{
    Task<ToolLaunchResult> LaunchAsync(DesktopTool tool, CancellationToken cancellationToken = default);

    Task<ToolLaunchResult> OpenGetPathAsync(DesktopTool tool, CancellationToken cancellationToken = default);
}
