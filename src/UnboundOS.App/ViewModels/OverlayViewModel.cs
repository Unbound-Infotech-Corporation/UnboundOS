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
        Headline = host.IsEnabled ? "Overlay host connected" : "Overlay addon is optional";
        Detail = host.IsEnabled
            ? "Widgets render in the addon process. Session, network, and process engines stay independent."
            : "No desktop overlay is installed. This page is a reserved hook — skins stay off until an addon registers IDesktopOverlayHost.";
    }

    public bool IsEnabled { get; }
    public string DisplayName { get; }
    public string Headline { get; }
    public string Detail { get; }
    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; }
}
