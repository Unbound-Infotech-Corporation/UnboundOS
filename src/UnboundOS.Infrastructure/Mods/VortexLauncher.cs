using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Opens Vortex.exe with --game / --profile. Never mutates Vortex state.
/// </summary>
public sealed class VortexLauncher
{
    private readonly VortexDiscoverySettings _settings;
    private readonly Func<string, string, bool> _start;

    public VortexLauncher(VortexDiscoverySettings? settings = null)
        : this(settings ?? new VortexDiscoverySettings(), DefaultStart)
    {
    }

    public VortexLauncher(VortexDiscoverySettings settings, Func<string, string, bool> start)
    {
        _settings = settings;
        _start = start;
    }

    public ModOperationResult Open(string? vortexGameId, string? vortexProfileId)
    {
        var executable = _settings.FindExecutable();
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            return new ModOperationResult(
                false,
                "Vortex not found. Install Vortex or launch it from the Start menu.",
                []);
        }

        var arguments = VortexLocator.BuildLaunchArguments(vortexGameId, vortexProfileId);
        if (VortexLocator.LooksLikeWriteSwitch(arguments))
        {
            return new ModOperationResult(false, "Refusing to launch Vortex with a write switch.", []);
        }

        try
        {
            if (!_start(executable, arguments))
            {
                return new ModOperationResult(false, "Vortex did not start.", []);
            }
        }
        catch (Exception error)
        {
            return new ModOperationResult(false, $"Vortex did not start: {error.Message}", []);
        }

        var target = string.IsNullOrWhiteSpace(vortexGameId)
            ? "Vortex"
            : $"Vortex for {VortexCatalogService.DisplayNameFor(vortexGameId)}";
        return new ModOperationResult(true, $"Opened {target}.", [executable, arguments]);
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
