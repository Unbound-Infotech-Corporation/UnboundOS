namespace UnboundOS.Core.Overlay;

/// <summary>
/// Describes a widget an overlay addon can render on the desktop.
/// The session engine never reads this list.
/// </summary>
public sealed record OverlayWidgetDescriptor(
    string Id,
    string Title,
    string Kind,
    string? Description = null);
