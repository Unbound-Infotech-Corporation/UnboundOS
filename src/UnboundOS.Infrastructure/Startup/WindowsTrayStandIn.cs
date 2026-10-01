using UnboundOS.Core.Abstractions;
using Diag = System.Diagnostics;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Process-list stand-in for the Explorer notification area.
/// Not a NotifyIcon host — Desktop mode restores the real tray.
/// </summary>
public sealed class WindowsTrayStandIn : ITrayStandIn
{
    private static readonly (string Id, string Title, string Process)[] Known =
    [
        ("discord", "Discord", "Discord"),
        ("steam", "Steam", "steam"),
        ("obs", "OBS", "obs64"),
        ("vortex", "Vortex", "Vortex")
    ];

    public Task<IReadOnlyList<TrayApp>> ListAsync(CancellationToken cancellationToken = default)
    {
        var running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var process in Diag.Process.GetProcesses())
            {
                try
                {
                    running.Add(process.ProcessName);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception)
        {
            // Process enumeration can fail under tight ACL. Show known-off.
        }

        IReadOnlyList<TrayApp> items = Known
            .Select(item => new TrayApp(item.Id, item.Title, item.Process, running.Contains(item.Process)))
            .ToArray();
        return Task.FromResult(items);
    }
}
