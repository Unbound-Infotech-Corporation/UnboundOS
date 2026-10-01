using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>Guide-button quick menu. In-shell only — no D3D/Vulkan hook.</summary>
public static class GuideMenuCatalog
{
    public static IReadOnlyList<GuideAction> All { get; } =
    [
        new("volume-up", "Volume up", "Hardware volume keys when Windows still hosts the OSD.", "volume", null),
        new("volume-down", "Volume down", "Step the session volume down.", "volume", null),
        new("mute", "Mute", "Toggle mute.", "volume", null),
        new("network", "Network", "Adapter list in Options. Pairing UI is Windows.", "settings", "ms-settings:network"),
        new("bluetooth", "Bluetooth", "Pairing stays in Windows Settings.", "settings", "ms-settings:bluetooth"),
        new("hdr", "HDR / display", "HDR and refresh stay in Windows Display settings.", "settings", "ms-settings:display"),
        new("perf", "Performance overlay", "In-shell CPU/RAM telemetry. Not a game inject and not a D3D hook.", "overlay", null),
        new("lock", "Lock", "Win+L stays Windows.", "power", null),
        new("sleep", "Sleep", "Sleep this PC. The running game is left alone.", "power", null),
        new("desktop", "Switch to desktop", "Start explorer.exe on demand.", "desktop", null),
        new("shutdown", "Shut down", "Windows shutdown.", "power", null)
    ];

    public static GuideAction? Find(string id) =>
        All.FirstOrDefault(action => string.Equals(action.Id, id, StringComparison.OrdinalIgnoreCase));
}
