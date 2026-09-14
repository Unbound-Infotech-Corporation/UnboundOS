namespace UnboundOS.Core;

/// <summary>Honest product copy for OS-level surfaces. Tests lock these claims.</summary>
public static class OsProductCopy
{
    public const string FilesHonesty =
        "Unbound Files is the daily file UI. Windows Explorer stays installed for games, Easy Anti-Cheat, BattlEye, Vanguard, and anything that expects the NT shell. UnboundOS does not replace Explorer or set Shell=.";

    public const string DisplayHonesty =
        "UnboundOS does not invent a display stack. Settings → Display opens the GPU vendor app when it is installed.";

    public const string OverclockHonesty =
        "UnboundOS never writes GPU or CPU clocks. This hub only discovers and launches manufacturer tools.";

    public const string CleanupHonesty =
        "This clears leftover Unbound setup files in known folders. The Windows image owns the full OOBE wipe (temp, installer leftovers, offline NIC driver pack after a successful online check).";

    public const string StartupHonesty =
        "Startup audit reports Run keys, the Startup folder, scheduled tasks, and common overlay/bloat names. It never silently disables anticheat, GPU vendor services, Vortex, OBS, or HardProtect. Pin anything you want to keep.";

    public const string HardwareHonesty =
        "Hardware inventory is read from this PC (registry, DriveInfo, GC memory). Live sensors, WMI depth, and LibreHardwareMonitor are later. Optional Open HWiNFO uses the official app if installed — UnboundOS does not bundle HWiNFO.";
}
