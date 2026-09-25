using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.Infrastructure.Overlay;

/// <summary>
/// Rainmeter is an optional overlay path over Home.
/// Off unless <see cref="OverlayHostOptions.Enabled"/> is set true.
/// Opens Rainmeter.exe with empty args. Does not rewrite configs.
/// First-party Home widgets stay as the built-in fallback.
/// </summary>
public sealed class RainmeterDesktopOverlayHost : IDesktopOverlayHost
{
    private readonly OverlayHostOptions _options;
    private readonly RainmeterLauncher _rainmeter;

    public RainmeterDesktopOverlayHost(
        OverlayHostOptions? options = null,
        RainmeterLauncher? rainmeter = null)
    {
        _options = options ?? new OverlayHostOptions();
        _rainmeter = rainmeter ?? new RainmeterLauncher();
    }

    public bool IsEnabled => _options.Enabled;

    public string DisplayName =>
        _rainmeter.FindExecutable() is { Length: > 0 }
            ? "Rainmeter"
            : "Rainmeter (not installed)";

    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; } =
    [
        new("phenix", "Phenix", "skin", "Recommended starter theme over Home. Get the official page — do not ship the .rmskin."),
        new("minimalistic-clock", "Minimalistic Clock", "skin", "Recommended clock skin over Home. Get the official VisualSkins page — do not ship the .rmskin."),
        new("clock-temps", "Clock + temps", "skin", "Use Minimalistic Clock plus Phenix or built-in Home widgets if Rainmeter is off."),
        new("monstercat", "Monstercat Visualizer", "visualizer", "Free Rainmeter visualizer pack. Official GitHub handoff only.")
    ];

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        _ = _rainmeter.Open();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
