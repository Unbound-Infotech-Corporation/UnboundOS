namespace UnboundOS.Core;

/// <summary>Honest product copy for OS-level surfaces. Tests lock these claims.</summary>
public static class OsProductCopy
{
    public const string FilesHonesty =
        "Unbound Files is the daily file UI (browse, copy, move, delete, open-with, eject). Explorer.exe stays on disk. Desktop mode can start it. HKCU Shell= is opt-in only — never HKLM, never a renamed explorer.exe.";

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

    public const string UpdateGuardHonesty =
        "Monthly security quality from Microsoft; feature/optional churn blocked. LCUs include security plus some nonsecurity content by Microsoft design — not CVE-only patches. UnboundOS does not redistribute Windows .msu/.cab. Each PC downloads from Microsoft. Home edition is best-effort. Session enter does not flip Update Guard.";

    public const string AutostartHonesty =
        "HKCU Run launches UnboundOS as a normal app with Explorer still the shell. That is the default. Opt-in replacement is a separate HKCU Winlogon Shell= to the watchdog — never HKLM.";

    public const string HealthHonesty =
        "Health check reports network, GPU driver, autostart, shell replacement, Update Guard, and free disk, then writes a log you can send back. It does not change Windows Update or Defender.";

    public const string ShellReplacementHonesty =
        "Shell replacement is per-user HKCU Winlogon Shell= pointing at UnboundOS.App.exe --watchdog. Hold Shift at sign-in, 3 crashes in 2 minutes, a missing exe, -RestoreExplorer, or Uninstall returns Explorer. Never HKLM. Never rewrite explorer.exe.";

    public const string AnticheatHonesty =
        "HKCU Shell= changes this user's desktop host only. NT, drivers, DirectX, Store, Steam, and anticheat services stay Windows. UnboundOS does not rename explorer.exe. Easy Anti-Cheat, BattlEye, and Vanguard in shell-replacement mode are unverified — run the checklist before calling this a daily driver.";

    public const string DesktopModeHonesty =
        "Desktop mode starts explorer.exe on demand so the Windows desktop, taskbar, and tray return. Close Explorer or focus UnboundOS to come back. Win+L, Alt+Tab, and lock/sign-in stay Windows.";
}
