namespace UnboundOS.Core.Overlay;

/// <summary>
/// Optional desktop overlay / skin host. In-tree path is Rainmeter
/// (<c>RainmeterDesktopOverlayHost</c>), off unless
/// <see cref="OverlayHostOptions.Enabled"/>. Session, network, and
/// process engines must never depend on this type.
/// An addon can still replace <see cref="IDesktopOverlayHost"/> in DI
/// before <c>AddUnboundOs()</c> (or uses <c>TryAdd</c> so the addon wins).
/// </summary>
public interface IDesktopOverlayHost
{
    /// <summary>When false, the shell hides overlay UI and never starts the host.</summary>
    bool IsEnabled { get; }

    string DisplayName { get; }

    IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
