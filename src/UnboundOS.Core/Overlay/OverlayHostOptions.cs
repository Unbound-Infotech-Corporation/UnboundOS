namespace UnboundOS.Core.Overlay;

/// <summary>
/// Feature flag for the optional Rainmeter overlay host.
/// Off by default — Super Clean Home does not require Rainmeter.
/// Bind this to config without touching session/network code.
/// </summary>
public sealed class OverlayHostOptions
{
    public static OverlayHostOptions Disabled { get; } = new() { Enabled = false };

    /// <summary>Master switch. False so Rainmeter is not part of default Home.</summary>
    public bool Enabled { get; set; }
}
