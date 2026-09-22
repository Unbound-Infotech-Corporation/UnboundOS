namespace UnboundOS.Core.Overlay;

/// <summary>
/// Feature flag for the Rainmeter overlay host. On by default —
/// Rainmeter is the in-tree Home overlay path. Bind this to config
/// without touching session/network code.
/// </summary>
public sealed class OverlayHostOptions
{
    public static OverlayHostOptions Disabled { get; } = new() { Enabled = false };

    /// <summary>Master switch. True so Rainmeter is the live overlay path.</summary>
    public bool Enabled { get; set; } = true;
}
