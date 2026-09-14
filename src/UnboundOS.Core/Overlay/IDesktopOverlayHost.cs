namespace UnboundOS.Core.Overlay;

/// <summary>
/// Optional Rainmeter-style desktop overlay / skin host.
/// Session, network, and process engines must never depend on this type.
/// The default registration is a no-op; an addon replaces
/// <see cref="IDesktopOverlayHost"/> in DI before <c>AddUnboundOs()</c>
/// (or uses <c>TryAdd</c> so the addon wins).
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
