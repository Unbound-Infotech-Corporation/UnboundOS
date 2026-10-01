using UnboundOS.Core;
using Diag = System.Diagnostics;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsDesktopMode : IDesktopMode
{
    public bool ExplorerRunning =>
        Diag.Process.GetProcessesByName("explorer").Length > 0;

    public Task<(bool Succeeded, string Message)> StartDesktopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Diag.Process.Start(new Diag.ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
            return Task.FromResult((true, "Explorer desktop started. " + OsProductCopy.DesktopModeHonesty));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, ex.Message));
        }
    }

    public Task<(bool Succeeded, string Message)> ReturnToShellAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((true, "Focus UnboundOS. Explorer can stay running. " + OsProductCopy.DesktopModeHonesty));
    }
}
