using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.Infrastructure.Vendor;

/// <summary>Launch manufacturer display/OC tools or open official Get URIs. Never writes clocks.</summary>
public sealed class VendorAppLauncher : IVendorAppLauncher
{
    private readonly Func<string, string, bool> _start;
    private readonly Func<string, bool> _openUri;

    public VendorAppLauncher()
        : this(DefaultStart, DefaultOpenUri)
    {
    }

    public VendorAppLauncher(Func<string, string, bool> start, Func<string, bool> openUri)
    {
        _start = start;
        _openUri = openUri;
    }

    public Task<ToolLaunchResult> LaunchAsync(VendorApp app, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(app);

        if (!app.IsInstalled ||
            string.IsNullOrWhiteSpace(app.ExecutablePath) ||
            !File.Exists(app.ExecutablePath))
        {
            return Task.FromResult(ToolLaunchResult.Fail(
                $"{app.DisplayName} is not installed. Use Get for the official page. UnboundOS does not write GPU or CPU clocks."));
        }

        try
        {
            if (!_start(app.ExecutablePath, string.Empty))
            {
                return Task.FromResult(ToolLaunchResult.Fail($"{app.DisplayName} did not start."));
            }
        }
        catch (Exception error)
        {
            return Task.FromResult(ToolLaunchResult.Fail($"{app.DisplayName} did not start: {error.Message}"));
        }

        return Task.FromResult(ToolLaunchResult.Ok($"Opened {app.DisplayName}. UnboundOS did not change clocks."));
    }

    public Task<ToolLaunchResult> OpenGetPathAsync(VendorApp app, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(app);

        if (!DesktopToolLauncher.IsSafeGetUri(app.GetPath.Uri))
        {
            return Task.FromResult(ToolLaunchResult.Fail("Refusing to open an unofficial or unsafe Get path."));
        }

        try
        {
            if (!_openUri(app.GetPath.Uri))
            {
                return Task.FromResult(ToolLaunchResult.Fail($"Could not open {app.GetPath.Label}."));
            }
        }
        catch (Exception error)
        {
            return Task.FromResult(ToolLaunchResult.Fail($"Could not open Get path: {error.Message}"));
        }

        return Task.FromResult(ToolLaunchResult.Ok(
            $"Opened the official {app.DisplayName} page. UnboundOS does not download or install it."));
    }

    private static bool DefaultStart(string executable, string arguments)
    {
        var info = new System.Diagnostics.ProcessStartInfo(executable)
        {
            Arguments = arguments,
            UseShellExecute = true
        };

        return System.Diagnostics.Process.Start(info) is not null;
    }

    private static bool DefaultOpenUri(string uri)
    {
        var info = new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true };
        return System.Diagnostics.Process.Start(info) is not null;
    }
}
