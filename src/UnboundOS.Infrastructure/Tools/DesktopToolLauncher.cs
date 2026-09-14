using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Mods;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Launches installed kit apps or opens an official Get URI.
/// Never downloads or bundles installers.
/// </summary>
public sealed class DesktopToolLauncher : IDesktopToolLauncher
{
    private readonly VortexLauncher _vortex;
    private readonly Func<string, string, bool> _start;
    private readonly Func<string, bool> _openUri;

    public DesktopToolLauncher(VortexLauncher? vortex = null)
        : this(vortex ?? new VortexLauncher(), DefaultStart, DefaultOpenUri)
    {
    }

    public DesktopToolLauncher(
        VortexLauncher vortex,
        Func<string, string, bool> start,
        Func<string, bool> openUri)
    {
        _vortex = vortex;
        _start = start;
        _openUri = openUri;
    }

    public Task<ToolLaunchResult> LaunchAsync(DesktopTool tool, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tool);

        if (string.Equals(tool.Id, DesktopToolIds.Vortex, StringComparison.OrdinalIgnoreCase))
        {
            var vortex = _vortex.Open(null, null);
            return Task.FromResult(new ToolLaunchResult(vortex.Succeeded, vortex.Message));
        }

        if (!tool.IsInstalled ||
            string.IsNullOrWhiteSpace(tool.ExecutablePath) ||
            !File.Exists(tool.ExecutablePath))
        {
            return Task.FromResult(ToolLaunchResult.Fail(
                $"{tool.DisplayName} is not installed. Use Get for the official download page."));
        }

        try
        {
            if (!_start(tool.ExecutablePath, string.Empty))
            {
                return Task.FromResult(ToolLaunchResult.Fail($"{tool.DisplayName} did not start."));
            }
        }
        catch (Exception error)
        {
            return Task.FromResult(ToolLaunchResult.Fail($"{tool.DisplayName} did not start: {error.Message}"));
        }

        return Task.FromResult(ToolLaunchResult.Ok($"Opened {tool.DisplayName}."));
    }

    public Task<ToolLaunchResult> OpenGetPathAsync(DesktopTool tool, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(tool);

        if (!IsSafeGetUri(tool.GetPath.Uri))
        {
            return Task.FromResult(ToolLaunchResult.Fail(
                "Refusing to open an unofficial or unsafe Get path."));
        }

        try
        {
            if (!_openUri(tool.GetPath.Uri))
            {
                return Task.FromResult(ToolLaunchResult.Fail($"Could not open {tool.GetPath.Label}."));
            }
        }
        catch (Exception error)
        {
            return Task.FromResult(ToolLaunchResult.Fail($"Could not open Get path: {error.Message}"));
        }

        return Task.FromResult(ToolLaunchResult.Ok(
            $"Opened the official {tool.DisplayName} page. UnboundOS does not download or install it."));
    }

    public static bool IsSafeGetUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        return parsed.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               parsed.Scheme.Equals("ms-windows-store", StringComparison.OrdinalIgnoreCase) ||
               parsed.Scheme.Equals("winget", StringComparison.OrdinalIgnoreCase);
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
        var info = new System.Diagnostics.ProcessStartInfo(uri)
        {
            UseShellExecute = true
        };

        return System.Diagnostics.Process.Start(info) is not null;
    }
}
