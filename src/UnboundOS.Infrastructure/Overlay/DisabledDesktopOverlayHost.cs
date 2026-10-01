using UnboundOS.Core.Overlay;

namespace UnboundOS.Infrastructure.Overlay;

/// <summary>
/// Default overlay host: present so addons have a DI seat, inert so
/// session/network/process logic stays independent of any skin layer.
/// </summary>
public sealed class DisabledDesktopOverlayHost : IDesktopOverlayHost
{
    public bool IsEnabled => false;

    public string DisplayName => "Desktop overlay (optional, off)";

    public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; } = [];

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
