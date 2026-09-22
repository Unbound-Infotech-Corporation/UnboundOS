using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.Infrastructure.Overlay;

/// <summary>
/// Rainmeter is the customizable overlay path over galaxy Home.
/// Off until <see cref="OverlayHostOptions.Enabled"/> is set.
/// Does not rewrite Rainmeter configs. First-party Home widgets stay
/// as the built-in fallback.
/// </summary>
public sealed class RainmeterDesktopOverlayHost : IDesktopOverlayHost
{
    private readonly OverlayHostOptions _options;
    private readonly RainmeterLauncher _rainmeter;

    public RainmeterDesktopOverlayHost(
        OverlayHostOptions? options = null,
        RainmeterLauncher? rainmeter = null)
    {
        _options = options ?? OverlayHostOptions.Disabled;
        _rainmeter = rainmeter ?? new RainmeterLauncher();
    }

    public bool IsEnabled => _options.Enabled;

    public string DisplayName =>
        _rainmeter.FindExecutable() is { Length: > 0 }
            ? "Rainmeter"
            : "Rainmeter (not installed)";

    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; } =
    [
        new("phenix", "Phenix", "skin", "Recommended starter over Home. Get the official page — do not ship the .rmskin."),
        new("clock-temps", "Clock + temps", "skin", "Use Phenix or another official skin. Built-in Home widgets remain if Rainmeter is off."),
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
