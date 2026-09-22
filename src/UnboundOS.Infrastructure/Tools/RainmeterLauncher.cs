using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Discovers and opens Rainmeter.exe. Never rewrites rainmeter.ini or
/// skin configs — Rainmeter stays in charge of layouts.
/// </summary>
public sealed class RainmeterLauncher
{
    private readonly DesktopToolDiscoverySettings _settings;
    private readonly Func<string, string, bool> _start;

    public RainmeterLauncher(DesktopToolDiscoverySettings? settings = null)
        : this(settings ?? new DesktopToolDiscoverySettings(), DefaultStart)
    {
    }

    public RainmeterLauncher(DesktopToolDiscoverySettings settings, Func<string, string, bool> start)
    {
        _settings = settings;
        _start = start;
    }

    public string? FindExecutable()
    {
        if (_settings.ForcedExecutables.TryGetValue(DesktopToolIds.Rainmeter, out var forced))
        {
            return string.IsNullOrWhiteSpace(forced) ? null : forced;
        }

        if (!_settings.UseDefaultWindowsLocations)
        {
            return null;
        }

        return DesktopAppLocator.FindFirstExisting(
            DesktopAppLocator.Combine(
                    DesktopAppLocator.ProgramRoots(),
                    Path.Combine("Rainmeter", "Rainmeter.exe"))
                .Concat(DesktopAppLocator.UninstallExecutables("Rainmeter")
                    .SelectMany(path => DesktopAppLocator.ExpandInstallLocation(path, "Rainmeter.exe")))
                .Concat(DesktopAppLocator.StartMenuExecutables("Rainmeter.exe")));
    }

    public ToolLaunchResult Open()
    {
        var executable = FindExecutable();
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            return ToolLaunchResult.Fail(
                "Rainmeter not found. Use Get for the official Rainmeter page, then Get Phenix or Minimalistic Clock.");
        }

        try
        {
            if (!_start(executable, string.Empty))
            {
                return ToolLaunchResult.Fail("Rainmeter did not start.");
            }
        }
        catch (Exception error)
        {
            return ToolLaunchResult.Fail($"Rainmeter did not start: {error.Message}");
        }

        return ToolLaunchResult.Ok("Opened Rainmeter. Skins stay under Rainmeter — UnboundOS does not rewrite them.");
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
}
