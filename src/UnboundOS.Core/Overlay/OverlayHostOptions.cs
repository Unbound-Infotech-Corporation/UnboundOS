namespace UnboundOS.Core.Overlay;

/// <summary>
/// Feature flag for a future desktop overlay addon. Off by default.
/// Addons can bind this to config without touching session/network code.
/// </summary>
public sealed class OverlayHostOptions
{
    public static OverlayHostOptions Disabled { get; } = new();

    /// <summary>Master switch. Leave false unless an overlay addon is installed.</summary>
    public bool Enabled { get; set; }
}
