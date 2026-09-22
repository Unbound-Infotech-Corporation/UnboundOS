using UnboundOS.Core.Overlay;

namespace UnboundOS.App.ViewModels;

/// <summary>
/// Stub surface for the optional desktop overlay addon.
/// Does not start, stop, or own session/network/process behavior.
/// </summary>
public sealed class OverlayViewModel
{
    public OverlayViewModel(IDesktopOverlayHost host)
    {
        IsEnabled = host.IsEnabled;
        DisplayName = host.DisplayName;
        Widgets = host.Widgets;
        Headline = host.IsEnabled ? "Rainmeter overlay connected" : "Rainmeter overlay is optional";
        Detail = host.IsEnabled
            ? "Rainmeter skins sit over galaxy Home. UnboundOS opens Rainmeter; it does not rewrite rainmeter.ini. Built-in Home widgets stay as the fallback."
            : "Rainmeter is the overlay path (Tools → Rainmeter / Phenix / Visualizer pack). This host stays off until OverlayHostOptions.Enabled. Built-in Home widgets are the fallback. UnboundOS does not ship .rmskin files.";
    }

    public bool IsEnabled { get; }
    public string DisplayName { get; }
    public string Headline { get; }
    public string Detail { get; }
    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; }
}
